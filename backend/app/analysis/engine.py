"""Analysis orchestration.

Order matters: the deterministic result is fully formed and valid before any
optional subsystem is consulted. Security, knowledge retrieval and AI
explanation only ever *add* to it.
"""

from __future__ import annotations

import logging
import uuid
from pathlib import Path

from app.ai.reasoner import AIReasoner, enforce_guardrails
from app.analysis import blast_radius, classifier, risk_engine
from app.analysis.change_mapper import map_changes
from app.contracts.analysis import (
    AnalysisResult,
    AnalysisStatuses,
    Explanation,
    RiskState,
    SubsystemStatus,
)
from app.contracts.change import ChangeSet
from app.contracts.security import ScannerStatus, SecurityReport
from app.graph.builder import BuildReport
from app.graph.store import GraphStore

logger = logging.getLogger(__name__)

BASE_CONFIDENCE = 0.95
PENALTY_SECURITY_UNAVAILABLE = 0.15
PENALTY_KNOWLEDGE_UNAVAILABLE = 0.10
PENALTY_UNKNOWN_NODES = 0.15
PENALTY_UNMAPPED_FILES = 0.20
PENALTY_DYNAMIC = 0.05


def analyse(
    *,
    project_id: str,
    change: ChangeSet,
    store: GraphStore,
    build: BuildReport | None,
    reasoner: AIReasoner,
    ai_is_live: bool,
    security: SecurityReport | None = None,
    max_depth: int = 6,
    confidence_threshold: float = 0.70,
    parse_failure_unknown_ratio: float = 0.20,
    repo_root: Path | None = None,
) -> AnalysisResult:
    statuses = AnalysisStatuses()
    notes: list[str] = []

    mapping = map_changes(change, store)
    radius = blast_radius.compute(store, mapping.changed_node_ids, max_depth)

    security_report = security or SecurityReport()
    if security_report.status is ScannerStatus.OK:
        statuses.security = SubsystemStatus.OK
        if security_report.scanners_unavailable:
            statuses.security = SubsystemStatus.DEGRADED
            notes.append(
                "Partial security coverage: "
                + ", ".join(security_report.scanners_unavailable)
                + " did not run."
            )
    else:
        statuses.security = SubsystemStatus.UNAVAILABLE
        notes.append("No security scanner produced results. Security status is UNKNOWN.")

    file_to_nodes: dict[str, list[str]] = {}
    for node in store.nodes():
        if node.source_file:
            file_to_nodes.setdefault(node.source_file, []).append(node.id)
    findings_by_node: dict[str, list] = {}
    for finding in security_report.findings:
        for node_id in file_to_nodes.get(finding.file, []):
            finding.node_id = node_id
            findings_by_node.setdefault(node_id, []).append(finding)

    nodes = classifier.classify(
        store=store,
        radius=radius,
        findings_by_node=findings_by_node,
        max_depth=max_depth,
        confidence_threshold=confidence_threshold,
    )
    risk = risk_engine.score(nodes, change, security_report)

    # ---- analysis-level UNKNOWN gates -------------------------------
    overall = risk.level
    if not store.nodes():
        overall = RiskState.UNKNOWN
        statuses.graph = SubsystemStatus.UNAVAILABLE
        notes.append("The dependency graph is empty. Run ingestion before analysing.")
    elif not mapping.mapped:
        overall = RiskState.UNKNOWN
        notes.append(
            "No changed file could be mapped to a component in the dependency graph."
        )
    elif build and build.parse_failure_ratio > parse_failure_unknown_ratio:
        overall = RiskState.UNKNOWN
        statuses.graph = SubsystemStatus.DEGRADED
        notes.append(
            f"{build.parse_failure_ratio:.0%} of ingested files failed to parse, "
            "which exceeds the configured threshold."
        )
    if not change.files:
        overall = RiskState.UNKNOWN
        statuses.change_source = SubsystemStatus.UNAVAILABLE
        notes.append("The change set is empty.")

    confidence = BASE_CONFIDENCE
    if statuses.security is SubsystemStatus.UNAVAILABLE:
        confidence -= PENALTY_SECURITY_UNAVAILABLE
    elif statuses.security is SubsystemStatus.DEGRADED:
        confidence -= PENALTY_SECURITY_UNAVAILABLE / 2
    if statuses.knowledge in (SubsystemStatus.UNAVAILABLE, SubsystemStatus.NOT_CONFIGURED):
        confidence -= PENALTY_KNOWLEDGE_UNAVAILABLE
    if any(n.state is RiskState.UNKNOWN for n in nodes):
        confidence -= PENALTY_UNKNOWN_NODES
    if mapping.unmapped_files:
        confidence -= PENALTY_UNMAPPED_FILES
    if any("dynamic_path" in n.flags for n in nodes):
        confidence -= PENALTY_DYNAMIC
    confidence = max(0.05, min(0.99, confidence))

    result = AnalysisResult(
        analysis_id=uuid.uuid4().hex,
        project_id=project_id,
        change=change.model_dump(mode="json"),
        overall_state=overall,
        risk=risk,
        confidence=round(confidence, 2),
        changed_node_ids=mapping.changed_node_ids,
        unmapped_files=mapping.unmapped_files,
        nodes=nodes,
        security=security_report,
        statuses=statuses,
        notes=notes,
    )
    result.notes.append(
        f"Data objects in scope (downstream of the change): {len(radius.data_reach)}."
    )

    # ---- optional: AI explanation -----------------------------------
    try:
        explanation = reasoner.explain(result)
        statuses.explanation = (
            SubsystemStatus.OK if ai_is_live else SubsystemStatus.DEGRADED
        )
    except Exception:
        logger.exception("AI reasoner failed; keeping deterministic result")
        explanation = Explanation(
            summary="AI reasoning unavailable. The deterministic dependency analysis below is unaffected.",
            backend="none",
        )
        statuses.explanation = SubsystemStatus.UNAVAILABLE

    result.explanation = enforce_guardrails(explanation, result)
    for node in result.nodes:
        if node.node_id in result.explanation.per_node:
            node.explanation = result.explanation.per_node[node.node_id]
            node.explanation_source = result.explanation.backend
    result.statuses = statuses
    return result
