"""Analysis contracts.

UNKNOWN is a first-class state. GREEN is only ever produced by rule R12, which
requires a fully parsed node with a proven absence of any path from the change.
"Not analysed" can never become GREEN.
"""

from __future__ import annotations

from datetime import datetime, timezone
from enum import Enum

from pydantic import BaseModel, Field

from app.contracts.graph import ParseStatus, RelationshipType
from app.contracts.security import SecurityFinding, SecurityReport


class RiskState(str, Enum):
    RED = "RED"
    YELLOW = "YELLOW"
    GREEN = "GREEN"
    UNKNOWN = "UNKNOWN"


class ImpactType(str, Enum):
    MODIFIED = "MODIFIED"
    DIRECT = "DIRECT"
    INDIRECT = "INDIRECT"
    NONE = "NONE"
    UNDETERMINED = "UNDETERMINED"


class ClassificationRule(str, Enum):
    """The ordered decision table. First match wins."""

    R1_PARSE_FAILED = "R1_parse_failed"
    R2_MODIFIED_PARTIAL_PARSE = "R2_modified_partial_parse"
    R3_MODIFIED = "R3_modified"
    R4_SECURITY_HIGH = "R4_security_high"
    R5_DIRECT_CONFIDENT = "R5_direct_confident"
    R6_DIRECT_LOW_CONFIDENCE = "R6_direct_low_confidence"
    R7_CRITICAL_IN_RADIUS = "R7_critical_in_radius"
    R8_INDIRECT_WITHIN_DEPTH = "R8_indirect_within_depth"
    R9_DYNAMIC_PATH = "R9_dynamic_path"
    R10_PARTIAL_PARSE = "R10_partial_parse"
    R11_BEYOND_DEPTH_HORIZON = "R11_beyond_depth_horizon"
    R12_UNREACHABLE_COMPLETE = "R12_unreachable_complete"


class SubsystemStatus(str, Enum):
    OK = "OK"
    UNAVAILABLE = "UNAVAILABLE"
    DEGRADED = "DEGRADED"
    NOT_CONFIGURED = "NOT_CONFIGURED"


class EvidenceStep(BaseModel):
    """One hop of a dependency path. Produced by graph traversal only."""

    source: str
    target: str
    relationship: RelationshipType
    rule_id: str
    confidence: float
    source_file: str | None = None
    line: int | None = None
    dynamic: bool = False


class NodeResult(BaseModel):
    node_id: str
    node_type: str
    name: str
    state: RiskState
    rule_applied: ClassificationRule
    impact_type: ImpactType
    distance: int | None = None
    """Hops from the nearest changed node. None means unreachable."""
    min_edge_confidence: float | None = None
    parse_status: ParseStatus = ParseStatus.COMPLETE
    critical: bool = False
    path: list[EvidenceStep] = Field(default_factory=list)
    """The exact chain of evidence, changed node first. Never AI-authored."""
    security_findings: list[SecurityFinding] = Field(default_factory=list)
    knowledge_refs: list[str] = Field(default_factory=list)
    source_files: list[str] = Field(default_factory=list)
    explanation: str | None = None
    explanation_source: str | None = None
    flags: list[str] = Field(default_factory=list)


class RiskFactor(BaseModel):
    factor: str
    points: int
    detail: str | None = None


class RiskScore(BaseModel):
    score: int = Field(ge=0, le=100)
    level: RiskState
    factors: list[RiskFactor] = Field(default_factory=list)


class AnalysisStatuses(BaseModel):
    graph: SubsystemStatus = SubsystemStatus.OK
    change_source: SubsystemStatus = SubsystemStatus.OK
    security: SubsystemStatus = SubsystemStatus.NOT_CONFIGURED
    knowledge: SubsystemStatus = SubsystemStatus.NOT_CONFIGURED
    database_metadata: SubsystemStatus = SubsystemStatus.NOT_CONFIGURED
    explanation: SubsystemStatus = SubsystemStatus.NOT_CONFIGURED


class Explanation(BaseModel):
    """The only AI-authored content in the system."""

    summary: str = ""
    per_node: dict[str, str] = Field(default_factory=dict)
    recommendations: list[str] = Field(default_factory=list)
    missing_information: list[str] = Field(default_factory=list)
    backend: str = "none"


class AnalysisResult(BaseModel):
    analysis_id: str
    project_id: str
    created_at: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))
    change: dict = Field(default_factory=dict)

    overall_state: RiskState
    risk: RiskScore
    confidence: float = Field(ge=0.0, le=1.0)

    changed_node_ids: list[str] = Field(default_factory=list)
    unmapped_files: list[str] = Field(default_factory=list)
    nodes: list[NodeResult] = Field(default_factory=list)

    security: SecurityReport = Field(default_factory=SecurityReport)
    explanation: Explanation = Field(default_factory=Explanation)
    statuses: AnalysisStatuses = Field(default_factory=AnalysisStatuses)
    notes: list[str] = Field(default_factory=list)

    def by_state(self, state: RiskState) -> list[NodeResult]:
        return [n for n in self.nodes if n.state is state]

    @property
    def counts(self) -> dict[str, int]:
        return {s.value: len(self.by_state(s)) for s in RiskState}
