"""AI reasoning adapter.

The reasoner may only *explain* a result that the deterministic core already
produced. It cannot introduce nodes, change a state, alter a distance, or
assert a security finding. `enforce_guardrails` is applied to every backend's
output, including future ones.
"""

from __future__ import annotations

import json
import logging
from typing import Protocol

from app.contracts.analysis import (
    AnalysisResult,
    Explanation,
    ImpactType,
    RiskState,
)

logger = logging.getLogger(__name__)

SYSTEM_INSTRUCTION = """You are Project X-Ray, a software change-impact analysis assistant.

You are given a dependency analysis that has ALREADY been computed
deterministically from a parsed dependency graph. Your job is to explain it in
plain language for a developer reviewing the change.

Rules:
- The dependency graph is authoritative. Never invent, infer, or remove a
  dependency relationship.
- Never introduce a component that is not present in the provided data.
- Never invent a security finding. Only interpret the findings supplied.
- Never claim a test passed or failed without supplied test results.
- If information is missing, list it under missing_information.
- Be specific about which tests should be run for the risky components.
- Reply with JSON only, matching the schema you are given.
"""

RESPONSE_SCHEMA_HINT = """{
  "summary": "string",
  "per_node": { "<node_id>": "string" },
  "recommendations": ["string"],
  "missing_information": ["string"]
}"""


class AIReasoner(Protocol):
    name: str

    def explain(self, result: AnalysisResult) -> Explanation: ...


def build_prompt(result: AnalysisResult) -> str:
    """Serialise only the facts. No source code is sent."""
    payload = {
        "change": result.change,
        "overall_state": result.overall_state.value,
        "risk_score": result.risk.score,
        "risk_factors": [f.model_dump() for f in result.risk.factors],
        "nodes": [
            {
                "node_id": n.node_id,
                "name": n.name,
                "type": n.node_type,
                "state": n.state.value,
                "rule_applied": n.rule_applied.value,
                "impact_type": n.impact_type.value,
                "distance": n.distance,
                "path": [
                    f"{s.source} --{s.relationship.value}--> {s.target} "
                    f"({s.rule_id} @ {s.source_file}:{s.line})"
                    for s in n.path
                ],
            }
            for n in result.nodes
            if n.state is not RiskState.GREEN
        ],
        "security_findings": [f.model_dump() for f in result.security.findings],
        "security_status": result.security.status.value,
        "unmapped_files": result.unmapped_files,
        "response_schema": json.loads(json.dumps(RESPONSE_SCHEMA_HINT)),
    }
    return json.dumps(payload, indent=2, default=str)


def enforce_guardrails(explanation: Explanation, result: AnalysisResult) -> Explanation:
    """Drop anything the model invented."""
    valid_ids = {n.node_id for n in result.nodes}
    filtered = {k: v for k, v in explanation.per_node.items() if k in valid_ids}
    dropped = len(explanation.per_node) - len(filtered)
    if dropped:
        logger.warning("discarded %d AI explanation(s) for unknown nodes", dropped)
    explanation.per_node = filtered
    return explanation


class MockReasoner:
    """Deterministic, offline explanation.

    Used as the default and as the fallback when a live backend fails. Results
    produced by this backend are labelled so they are never presented as live
    model output.
    """

    name = "mock"

    def explain(self, result: AnalysisResult) -> Explanation:
        red = result.by_state(RiskState.RED)
        yellow = result.by_state(RiskState.YELLOW)
        unknown = result.by_state(RiskState.UNKNOWN)

        modified = [n for n in result.nodes if n.impact_type is ImpactType.MODIFIED]
        headline = (
            f"The change modifies {len(modified)} component(s) and reaches "
            f"{len(red) + len(yellow)} further component(s) through the dependency graph."
        )
        if not modified:
            headline = (
                "No modified file could be mapped to a component in the dependency graph, "
                "so impact could not be determined."
            )

        per_node: dict[str, str] = {}
        for node in red + yellow + unknown:
            if node.impact_type is ImpactType.MODIFIED:
                per_node[node.node_id] = "This component is directly modified by the change."
            elif node.path:
                first = node.path[-1]
                per_node[node.node_id] = (
                    f"Reached in {node.distance} hop(s); "
                    f"{first.source.split(':', 1)[-1]} depends on "
                    f"{first.target.split(':', 1)[-1]} "
                    f"({first.rule_id} at {first.source_file}:{first.line})."
                )
            else:
                per_node[node.node_id] = (
                    "State could not be determined from the available evidence."
                )

        recommendations: list[str] = []
        if red:
            recommendations.append(
                "Run integration tests covering: " + ", ".join(n.name for n in red[:5])
            )
        if yellow:
            recommendations.append(
                "Manually review downstream behaviour in: "
                + ", ".join(n.name for n in yellow[:5])
            )
        if result.security.scanners_unavailable:
            recommendations.append(
                "Security coverage is incomplete; unavailable scanners: "
                + ", ".join(result.security.scanners_unavailable)
            )

        missing: list[str] = []
        if result.unmapped_files:
            missing.append(
                f"{len(result.unmapped_files)} changed file(s) could not be mapped to a component."
            )
        if unknown:
            missing.append(f"{len(unknown)} component(s) could not be classified.")
        if not result.security.usable:
            missing.append("No security scanner produced results.")

        return Explanation(
            summary=headline,
            per_node=per_node,
            recommendations=recommendations,
            missing_information=missing,
            backend=self.name,
        )
