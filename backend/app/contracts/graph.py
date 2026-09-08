"""Graph contracts.

Edge direction convention, used everywhere in X-Ray:

    A --relationship--> B   means   "A depends on B" / "A uses B"

Therefore the *dependents* of a changed node B are its predecessors, and blast
radius is computed by walking edges in reverse. See analysis/blast_radius.py.
"""

from __future__ import annotations

from enum import Enum

from pydantic import BaseModel, Field


class NodeType(str, Enum):
    PROJECT = "PROJECT"
    FRONTEND_COMPONENT = "FRONTEND_COMPONENT"
    FRONTEND_MODULE = "FRONTEND_MODULE"
    API = "API"
    SERVICE = "SERVICE"
    CLASS = "CLASS"
    INTERFACE = "INTERFACE"
    FUNCTION = "FUNCTION"
    DATABASE = "DATABASE"
    TABLE = "TABLE"
    EXTERNAL_SERVICE = "EXTERNAL_SERVICE"
    DOCUMENT = "DOCUMENT"


class RelationshipType(str, Enum):
    CALLS = "CALLS"
    IMPORTS = "IMPORTS"
    USES = "USES"
    READS = "READS"
    WRITES = "WRITES"
    EXPOSES = "EXPOSES"
    DEPENDS_ON = "DEPENDS_ON"
    BINDS = "BINDS"
    DOCUMENTS = "DOCUMENTS"


class ParseStatus(str, Enum):
    """How much the parser understood about a node.

    PARTIAL and FAILED propagate into UNKNOWN classification rather than GREEN.
    """

    COMPLETE = "COMPLETE"
    PARTIAL = "PARTIAL"
    FAILED = "FAILED"


class GraphNode(BaseModel):
    id: str
    type: NodeType
    name: str
    qualified_name: str | None = None
    source_file: str | None = None
    line: int | None = None
    parse_status: ParseStatus = ParseStatus.COMPLETE
    critical: bool = False
    attributes: dict[str, str] = Field(default_factory=dict)

    def __hash__(self) -> int:  # allows use in sets
        return hash(self.id)


class GraphEdge(BaseModel):
    source: str
    target: str
    relationship: RelationshipType
    rule_id: str
    confidence: float = Field(ge=0.0, le=1.0)
    source_file: str | None = None
    line: int | None = None
    dynamic: bool = False
    """True when the relationship is resolved at runtime (reflection, dynamic
    dispatch, unresolved DI). Forces UNKNOWN instead of a confident state."""


def node_id(node_type: NodeType, qualified_name: str) -> str:
    return f"{node_type.value}:{qualified_name}"
