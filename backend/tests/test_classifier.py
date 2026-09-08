"""One test per classification rule.

The decision table is the contract users are shown, so every branch is pinned
here -- especially the ones that must NOT produce GREEN.
"""

from __future__ import annotations

from app.analysis import blast_radius, classifier
from app.contracts.analysis import ClassificationRule, RiskState
from app.contracts.graph import (
    GraphEdge,
    GraphNode,
    NodeType,
    ParseStatus,
    RelationshipType,
)
from app.contracts.security import FindingType, SecurityFinding, Severity
from app.graph.store import InMemoryGraphStore

MAX_DEPTH = 6
THRESHOLD = 0.70


def _node(name: str, parse=ParseStatus.COMPLETE, critical=False) -> GraphNode:
    return GraphNode(
        id=f"SERVICE:{name}",
        type=NodeType.SERVICE,
        name=name,
        source_file=f"src/{name}.cs",
        line=1,
        parse_status=parse,
        critical=critical,
    )


def _edge(source: str, target: str, confidence=0.95, dynamic=False, rel=RelationshipType.DEPENDS_ON):
    return GraphEdge(
        source=f"SERVICE:{source}",
        target=f"SERVICE:{target}",
        relationship=rel,
        rule_id="test.edge",
        confidence=confidence,
        source_file=f"src/{source}.cs",
        line=10,
        dynamic=dynamic,
    )


def _classify(store, changed, findings=None, max_depth=MAX_DEPTH):
    radius = blast_radius.compute(store, changed, max_depth)
    return {
        r.name: r
        for r in classifier.classify(
            store=store,
            radius=radius,
            findings_by_node=findings or {},
            max_depth=max_depth,
            confidence_threshold=THRESHOLD,
        )
    }


def test_r1_parse_failed_is_unknown():
    store = InMemoryGraphStore()
    store.add_node(_node("Broken", parse=ParseStatus.FAILED))
    results = _classify(store, [])
    assert results["Broken"].rule_applied is ClassificationRule.R1_PARSE_FAILED
    assert results["Broken"].state is RiskState.UNKNOWN


def test_r2_modified_but_partially_parsed_is_unknown():
    store = InMemoryGraphStore()
    store.add_node(_node("Half", parse=ParseStatus.PARTIAL))
    results = _classify(store, ["SERVICE:Half"])
    assert results["Half"].rule_applied is ClassificationRule.R2_MODIFIED_PARTIAL_PARSE
    assert results["Half"].state is RiskState.UNKNOWN


def test_r3_modified_is_red():
    store = InMemoryGraphStore()
    store.add_node(_node("Changed"))
    results = _classify(store, ["SERVICE:Changed"])
    assert results["Changed"].rule_applied is ClassificationRule.R3_MODIFIED
    assert results["Changed"].state is RiskState.RED


def test_r4_high_severity_security_finding_is_red():
    store = InMemoryGraphStore()
    store.add_node(_node("Isolated"))
    findings = {
        "SERVICE:Isolated": [
            SecurityFinding(
                severity=Severity.HIGH,
                type=FindingType.SECRET,
                file="src/Isolated.cs",
                description="secret",
                source="test",
            )
        ]
    }
    results = _classify(store, [], findings)
    assert results["Isolated"].rule_applied is ClassificationRule.R4_SECURITY_HIGH
    assert results["Isolated"].state is RiskState.RED


def test_r5_confident_direct_dependent_is_red():
    store = InMemoryGraphStore()
    store.add_node(_node("Core"))
    store.add_node(_node("Caller"))
    store.add_edge(_edge("Caller", "Core", confidence=0.95))
    results = _classify(store, ["SERVICE:Core"])
    assert results["Caller"].rule_applied is ClassificationRule.R5_DIRECT_CONFIDENT
    assert results["Caller"].state is RiskState.RED
    assert results["Caller"].distance == 1
    assert len(results["Caller"].path) == 1


def test_r6_low_confidence_direct_dependent_is_yellow():
    store = InMemoryGraphStore()
    store.add_node(_node("Core"))
    store.add_node(_node("Caller"))
    store.add_edge(_edge("Caller", "Core", confidence=0.4))
    results = _classify(store, ["SERVICE:Core"])
    assert results["Caller"].rule_applied is ClassificationRule.R6_DIRECT_LOW_CONFIDENCE
    assert results["Caller"].state is RiskState.YELLOW


def test_r7_critical_component_in_radius_is_red():
    store = InMemoryGraphStore()
    store.add_node(_node("Core"))
    store.add_node(_node("Mid"))
    store.add_node(_node("Billing", critical=True))
    store.add_edge(_edge("Mid", "Core"))
    store.add_edge(_edge("Billing", "Mid"))
    results = _classify(store, ["SERVICE:Core"])
    assert results["Billing"].rule_applied is ClassificationRule.R7_CRITICAL_IN_RADIUS
    assert results["Billing"].state is RiskState.RED


def test_r8_indirect_dependent_is_yellow():
    store = InMemoryGraphStore()
    store.add_node(_node("Core"))
    store.add_node(_node("Mid"))
    store.add_node(_node("Far"))
    store.add_edge(_edge("Mid", "Core"))
    store.add_edge(_edge("Far", "Mid"))
    results = _classify(store, ["SERVICE:Core"])
    assert results["Far"].rule_applied is ClassificationRule.R8_INDIRECT_WITHIN_DEPTH
    assert results["Far"].state is RiskState.YELLOW
    assert results["Far"].distance == 2


def test_r9_dynamic_path_is_unknown():
    store = InMemoryGraphStore()
    store.add_node(_node("Core"))
    store.add_node(_node("Mid"))
    store.add_node(_node("Far"))
    store.add_edge(_edge("Mid", "Core"))
    store.add_edge(_edge("Far", "Mid", dynamic=True))
    results = _classify(store, ["SERVICE:Core"])
    assert results["Far"].rule_applied is ClassificationRule.R9_DYNAMIC_PATH
    assert results["Far"].state is RiskState.UNKNOWN
    assert "dynamic_path" in results["Far"].flags


def test_r10_partially_parsed_unreachable_is_unknown_not_green():
    store = InMemoryGraphStore()
    store.add_node(_node("Core"))
    store.add_node(_node("Murky", parse=ParseStatus.PARTIAL))
    results = _classify(store, ["SERVICE:Core"])
    assert results["Murky"].rule_applied is ClassificationRule.R10_PARTIAL_PARSE
    assert results["Murky"].state is RiskState.UNKNOWN


def test_r11_beyond_depth_horizon_is_yellow_not_green():
    store = InMemoryGraphStore()
    for name in ("Core", "A", "B", "C"):
        store.add_node(_node(name))
    store.add_edge(_edge("A", "Core"))
    store.add_edge(_edge("B", "A"))
    store.add_edge(_edge("C", "B"))
    results = _classify(store, ["SERVICE:Core"], max_depth=2)
    assert results["C"].rule_applied is ClassificationRule.R11_BEYOND_DEPTH_HORIZON
    assert results["C"].state is RiskState.YELLOW
    assert "beyond_depth_horizon" in results["C"].flags


def test_r12_unreachable_and_fully_parsed_is_green():
    store = InMemoryGraphStore()
    store.add_node(_node("Core"))
    store.add_node(_node("Unrelated"))
    results = _classify(store, ["SERVICE:Core"])
    assert results["Unrelated"].rule_applied is ClassificationRule.R12_UNREACHABLE_COMPLETE
    assert results["Unrelated"].state is RiskState.GREEN


def test_binding_edges_do_not_consume_a_hop():
    store = InMemoryGraphStore()
    store.add_node(_node("Impl"))
    store.add_node(_node("IFace"))
    store.add_node(_node("Consumer"))
    store.add_edge(_edge("IFace", "Impl", rel=RelationshipType.BINDS))
    store.add_edge(_edge("Consumer", "IFace"))
    results = _classify(store, ["SERVICE:Impl"])
    assert results["IFace"].distance == 0
    assert results["Consumer"].distance == 1
    assert results["Consumer"].state is RiskState.RED


def test_evidence_path_is_recorded_with_source_locations():
    store = InMemoryGraphStore()
    store.add_node(_node("Core"))
    store.add_node(_node("Mid"))
    store.add_node(_node("Far"))
    store.add_edge(_edge("Mid", "Core"))
    store.add_edge(_edge("Far", "Mid"))
    results = _classify(store, ["SERVICE:Core"])
    path = results["Far"].path
    assert [step.source for step in path] == ["SERVICE:Mid", "SERVICE:Far"]
    assert all(step.source_file and step.line for step in path)
    assert all(step.rule_id for step in path)
