"""Transparent risk scoring.

These are declared heuristics, not calibrated probabilities. Every point that
contributes to the score is returned as a named factor so the UI can explain
the number instead of asserting it.
"""

from __future__ import annotations

from app.contracts.analysis import (
    ImpactType,
    NodeResult,
    RiskFactor,
    RiskScore,
    RiskState,
)
from app.contracts.change import ChangeSet
from app.contracts.security import SecurityReport, Severity

POINTS_MODIFIED = 20
POINTS_DIRECT = 25
POINTS_INDIRECT = 15
POINTS_CRITICAL = 20
POINTS_CHURN = 15
POINTS_SECURITY_HIGH = 25
POINTS_SECURITY_MEDIUM = 12

CHURN_FILE_THRESHOLD = 5
CHURN_LINE_THRESHOLD = 100

GREEN_MAX = 29
YELLOW_MAX = 59


def score(
    nodes: list[NodeResult],
    change: ChangeSet,
    security: SecurityReport,
) -> RiskScore:
    factors: list[RiskFactor] = []

    modified = [n for n in nodes if n.impact_type is ImpactType.MODIFIED]
    direct = [n for n in nodes if n.impact_type is ImpactType.DIRECT]
    indirect = [n for n in nodes if n.impact_type is ImpactType.INDIRECT]
    critical = [n for n in nodes if n.critical and n.state in (RiskState.RED, RiskState.YELLOW)]

    if modified:
        factors.append(
            RiskFactor(
                factor="Components directly modified",
                points=POINTS_MODIFIED,
                detail=", ".join(n.name for n in modified[:5]),
            )
        )
    if direct:
        factors.append(
            RiskFactor(
                factor="Direct dependency impact",
                points=POINTS_DIRECT,
                detail=f"{len(direct)} component(s) depend directly on the change",
            )
        )
    if indirect:
        factors.append(
            RiskFactor(
                factor="Indirect dependency impact",
                points=POINTS_INDIRECT,
                detail=f"{len(indirect)} component(s) reachable through the graph",
            )
        )
    if critical:
        factors.append(
            RiskFactor(
                factor="Critical component in blast radius",
                points=POINTS_CRITICAL,
                detail=", ".join(n.name for n in critical[:5]),
            )
        )

    if len(change.files) >= CHURN_FILE_THRESHOLD or change.total_churn >= CHURN_LINE_THRESHOLD:
        factors.append(
            RiskFactor(
                factor="Change size",
                points=POINTS_CHURN,
                detail=f"{len(change.files)} file(s), {change.total_churn} line(s) changed",
            )
        )

    if security.usable:
        highest = max((f.severity for f in security.findings), default=None, key=_rank)
        if highest in (Severity.HIGH, Severity.CRITICAL):
            factors.append(
                RiskFactor(
                    factor="High-severity security finding",
                    points=POINTS_SECURITY_HIGH,
                    detail=f"{len(security.findings)} finding(s) reported by scanners",
                )
            )
        elif highest is Severity.MEDIUM:
            factors.append(
                RiskFactor(
                    factor="Medium-severity security finding",
                    points=POINTS_SECURITY_MEDIUM,
                    detail=f"{len(security.findings)} finding(s) reported by scanners",
                )
            )

    total = min(100, sum(f.points for f in factors))
    if total <= GREEN_MAX:
        level = RiskState.GREEN
    elif total <= YELLOW_MAX:
        level = RiskState.YELLOW
    else:
        level = RiskState.RED

    return RiskScore(score=total, level=level, factors=factors)


def _rank(severity: Severity | None) -> int:
    if severity is None:
        return -1
    return {
        Severity.INFO: 0,
        Severity.LOW: 1,
        Severity.MEDIUM: 2,
        Severity.HIGH: 3,
        Severity.CRITICAL: 4,
    }[severity]
