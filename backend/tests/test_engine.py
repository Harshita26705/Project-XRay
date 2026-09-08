"""End-to-end test of the MVP success criterion.

CGOne-style code change -> dependency graph -> deterministic blast radius ->
security evidence -> explanation -> RED/YELLOW/GREEN/UNKNOWN.
"""

from __future__ import annotations

import pytest

from app.ai.reasoner import MockReasoner
from app.analysis.engine import analyse
from app.contracts.analysis import ImpactType, RiskState
from app.contracts.change import ChangedFile, ChangeSet
from app.contracts.security import (
    FindingType,
    ScannerStatus,
    SecurityFinding,
    SecurityReport,
    Severity,
)

VALIDATOR = "CgOne.Demo.Api/Services/PaymentValidator.cs"
SERVICE = "CgOne.Demo.Api/Services/PaymentService.cs"


def _change(*paths: str) -> ChangeSet:
    return ChangeSet(
        source="manual",
        title="Change payment validation",
        description="Add additional validation to the payment processing flow.",
        reference="ADO-1234",
        files=[ChangedFile(path=p, added_lines=12, removed_lines=3) for p in paths],
    )


def _analyse(demo_graph, change, security=None):
    store, build = demo_graph
    return analyse(
        project_id="cgone-demo",
        change=change,
        store=store,
        build=build,
        reasoner=MockReasoner(),
        ai_is_live=False,
        security=security,
        max_depth=6,
        confidence_threshold=0.70,
    )


@pytest.fixture(scope="module")
def payment_analysis(demo_graph):
    return _analyse(demo_graph, _change(VALIDATOR, SERVICE))


def _state(result, name: str) -> RiskState:
    node = next(n for n in result.nodes if n.name == name)
    return node.state


def test_overall_result_is_red(payment_analysis):
    assert payment_analysis.overall_state is RiskState.RED
    assert payment_analysis.risk.score >= 60
    assert payment_analysis.risk.factors


def test_modified_components_are_red(payment_analysis):
    assert _state(payment_analysis, "PaymentValidator") is RiskState.RED
    assert _state(payment_analysis, "PaymentService") is RiskState.RED
    modified = [n.name for n in payment_analysis.nodes if n.impact_type is ImpactType.MODIFIED]
    # The interfaces are declared in the same files as their implementations, so
    # they are genuinely part of the change.
    assert set(modified) == {
        "PaymentValidator",
        "IPaymentValidator",
        "PaymentService",
        "IPaymentService",
    }


def test_dependents_are_escalated(payment_analysis):
    assert _state(payment_analysis, "IPaymentService") is RiskState.RED
    assert _state(payment_analysis, "PaymentsController") in (RiskState.RED, RiskState.YELLOW)
    assert _state(payment_analysis, "OrderService") in (RiskState.RED, RiskState.YELLOW)


def test_frontend_is_reached_through_the_api(payment_analysis):
    checkout = next(n for n in payment_analysis.nodes if n.name == "CheckoutPage")
    assert checkout.state is RiskState.YELLOW
    assert checkout.distance is not None and checkout.distance >= 3
    hops = [f"{s.source}->{s.target}" for s in checkout.path]
    assert any("API:" in hop for hop in hops), hops


def test_unrelated_components_are_green(payment_analysis):
    assert _state(payment_analysis, "UserService") is RiskState.GREEN
    assert _state(payment_analysis, "InventoryService") is RiskState.GREEN


def test_every_non_green_node_has_an_evidence_chain(payment_analysis):
    for node in payment_analysis.nodes:
        if node.state is RiskState.GREEN:
            continue
        if node.impact_type is ImpactType.MODIFIED:
            assert node.source_files, node.name
        else:
            assert node.path, f"{node.name} has no evidence path"
            assert all(step.rule_id for step in node.path)


def test_explanation_cannot_reference_unknown_nodes(payment_analysis):
    valid = {n.node_id for n in payment_analysis.nodes}
    assert set(payment_analysis.explanation.per_node).issubset(valid)
    assert payment_analysis.explanation.backend == "mock"


def test_security_unavailable_never_becomes_safe(payment_analysis):
    assert payment_analysis.security.status is ScannerStatus.UNAVAILABLE
    assert any("UNKNOWN" in note for note in payment_analysis.notes)
    assert payment_analysis.confidence < 0.95


def test_high_severity_finding_forces_red(demo_graph):
    report = SecurityReport(
        status=ScannerStatus.OK,
        scanners_run=["test-scanner"],
        findings=[
            SecurityFinding(
                severity=Severity.HIGH,
                type=FindingType.SAST,
                file="CgOne.Demo.Api/Services/SupportServices.cs",
                line=1,
                description="Injection candidate",
                source="test-scanner",
            )
        ],
    )
    result = _analyse(demo_graph, _change(VALIDATOR), security=report)
    inventory = next(n for n in result.nodes if n.name == "InventoryService")
    assert inventory.state is RiskState.RED
    assert inventory.security_findings


def test_unmappable_change_is_unknown_not_green(demo_graph):
    result = _analyse(demo_graph, _change("some/unknown/File.kt"))
    assert result.overall_state is RiskState.UNKNOWN
    assert result.unmapped_files == ["some/unknown/File.kt"]
    assert result.confidence < 0.8


def test_empty_change_is_unknown(demo_graph):
    result = _analyse(demo_graph, ChangeSet(source="manual", title="nothing"))
    assert result.overall_state is RiskState.UNKNOWN
