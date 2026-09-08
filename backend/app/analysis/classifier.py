"""The classification decision table.

Rules are evaluated in order and the first match wins. GREEN is reachable only
through R12, which requires a fully parsed node with a proven absence of any
path from the change. "Not analysed" therefore can never become GREEN -- it
becomes UNKNOWN.
"""

from __future__ import annotations

from app.analysis.blast_radius import BlastRadius
from app.contracts.analysis import (
    ClassificationRule,
    EvidenceStep,
    ImpactType,
    NodeResult,
    RiskState,
)
from app.contracts.graph import GraphNode, ParseStatus
from app.contracts.security import SecurityFinding, Severity, severity_rank
from app.graph.store import GraphStore, Reach

_HIGH_SEVERITIES = {Severity.HIGH, Severity.CRITICAL}

_IMPACT_BY_RULE = {
    ClassificationRule.R1_PARSE_FAILED: ImpactType.UNDETERMINED,
    ClassificationRule.R2_MODIFIED_PARTIAL_PARSE: ImpactType.UNDETERMINED,
    ClassificationRule.R3_MODIFIED: ImpactType.MODIFIED,
    ClassificationRule.R4_SECURITY_HIGH: ImpactType.DIRECT,
    ClassificationRule.R5_DIRECT_CONFIDENT: ImpactType.DIRECT,
    ClassificationRule.R6_DIRECT_LOW_CONFIDENCE: ImpactType.DIRECT,
    ClassificationRule.R7_CRITICAL_IN_RADIUS: ImpactType.INDIRECT,
    ClassificationRule.R8_INDIRECT_WITHIN_DEPTH: ImpactType.INDIRECT,
    ClassificationRule.R9_DYNAMIC_PATH: ImpactType.UNDETERMINED,
    ClassificationRule.R10_PARTIAL_PARSE: ImpactType.UNDETERMINED,
    ClassificationRule.R11_BEYOND_DEPTH_HORIZON: ImpactType.INDIRECT,
    ClassificationRule.R12_UNREACHABLE_COMPLETE: ImpactType.NONE,
}

_STATE_BY_RULE = {
    ClassificationRule.R1_PARSE_FAILED: RiskState.UNKNOWN,
    ClassificationRule.R2_MODIFIED_PARTIAL_PARSE: RiskState.UNKNOWN,
    ClassificationRule.R3_MODIFIED: RiskState.RED,
    ClassificationRule.R4_SECURITY_HIGH: RiskState.RED,
    ClassificationRule.R5_DIRECT_CONFIDENT: RiskState.RED,
    ClassificationRule.R6_DIRECT_LOW_CONFIDENCE: RiskState.YELLOW,
    ClassificationRule.R7_CRITICAL_IN_RADIUS: RiskState.RED,
    ClassificationRule.R8_INDIRECT_WITHIN_DEPTH: RiskState.YELLOW,
    ClassificationRule.R9_DYNAMIC_PATH: RiskState.UNKNOWN,
    ClassificationRule.R10_PARTIAL_PARSE: RiskState.UNKNOWN,
    ClassificationRule.R11_BEYOND_DEPTH_HORIZON: RiskState.YELLOW,
    ClassificationRule.R12_UNREACHABLE_COMPLETE: RiskState.GREEN,
}


def _steps(reach: Reach | None) -> list[EvidenceStep]:
    if reach is None:
        return []
    return [
        EvidenceStep(
            source=edge.source,
            target=edge.target,
            relationship=edge.relationship,
            rule_id=edge.rule_id,
            confidence=edge.confidence,
            source_file=edge.source_file,
            line=edge.line,
            dynamic=edge.dynamic,
        )
        for edge in reach.path
    ]


def _select_rule(
    node: GraphNode,
    modified: bool,
    reach: Reach | None,
    max_severity: Severity | None,
    max_depth: int,
    confidence_threshold: float,
) -> ClassificationRule:
    distance = reach.distance if reach else None
    min_conf = reach.min_confidence if reach else None
    dynamic = reach.dynamic if reach else False

    if node.parse_status is ParseStatus.FAILED:
        return ClassificationRule.R1_PARSE_FAILED
    if modified and node.parse_status is ParseStatus.PARTIAL:
        return ClassificationRule.R2_MODIFIED_PARTIAL_PARSE
    if modified:
        return ClassificationRule.R3_MODIFIED
    if max_severity in _HIGH_SEVERITIES:
        return ClassificationRule.R4_SECURITY_HIGH
    if distance is not None and distance <= 1:
        # distance 0 without being modified means the node is an interface bound
        # to a modified implementation -- still a direct impact.
        if min_conf is not None and min_conf >= confidence_threshold:
            return ClassificationRule.R5_DIRECT_CONFIDENT
        return ClassificationRule.R6_DIRECT_LOW_CONFIDENCE
    if node.critical and distance is not None and distance <= max_depth:
        return ClassificationRule.R7_CRITICAL_IN_RADIUS
    if distance is not None and 2 <= distance <= max_depth:
        if dynamic:
            return ClassificationRule.R9_DYNAMIC_PATH
        return ClassificationRule.R8_INDIRECT_WITHIN_DEPTH
    if dynamic:
        return ClassificationRule.R9_DYNAMIC_PATH
    if node.parse_status is ParseStatus.PARTIAL:
        return ClassificationRule.R10_PARTIAL_PARSE
    if distance is not None and distance > max_depth:
        return ClassificationRule.R11_BEYOND_DEPTH_HORIZON
    return ClassificationRule.R12_UNREACHABLE_COMPLETE


def classify(
    store: GraphStore,
    radius: BlastRadius,
    findings_by_node: dict[str, list[SecurityFinding]],
    max_depth: int,
    confidence_threshold: float,
) -> list[NodeResult]:
    modified = set(radius.changed_node_ids)
    results: list[NodeResult] = []

    for node in store.nodes():
        reach = radius.reachable.get(node.id)
        findings = findings_by_node.get(node.id, [])
        max_severity = (
            max((f.severity for f in findings), key=severity_rank) if findings else None
        )

        rule = _select_rule(
            node=node,
            modified=node.id in modified,
            reach=reach,
            max_severity=max_severity,
            max_depth=max_depth,
            confidence_threshold=confidence_threshold,
        )

        flags: list[str] = []
        if rule is ClassificationRule.R11_BEYOND_DEPTH_HORIZON:
            flags.append("beyond_depth_horizon")
        if reach and reach.dynamic:
            flags.append("dynamic_path")
        if node.id in radius.data_reach:
            flags.append("downstream_data_reach")
        if node.attributes.get("resolved") == "false":
            flags.append("unresolved_reference")

        results.append(
            NodeResult(
                node_id=node.id,
                node_type=node.type.value,
                name=node.name,
                state=_STATE_BY_RULE[rule],
                rule_applied=rule,
                impact_type=_IMPACT_BY_RULE[rule],
                distance=reach.distance if reach else None,
                min_edge_confidence=reach.min_confidence if reach else None,
                parse_status=node.parse_status,
                critical=node.critical,
                path=_steps(reach),
                security_findings=findings,
                source_files=[node.source_file] if node.source_file else [],
                flags=flags,
            )
        )

    order = {RiskState.RED: 0, RiskState.YELLOW: 1, RiskState.UNKNOWN: 2, RiskState.GREEN: 3}
    results.sort(key=lambda r: (order[r.state], r.distance if r.distance is not None else 99, r.name))
    return results
