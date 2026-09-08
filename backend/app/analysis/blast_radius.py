"""Deterministic blast radius.

Pure graph traversal. No AI, no network, no heuristics beyond the edge
confidences the parsers recorded.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from app.contracts.graph import NodeType
from app.graph.store import GraphStore, Reach


@dataclass
class BlastRadius:
    changed_node_ids: list[str] = field(default_factory=list)
    dependents: dict[str, Reach] = field(default_factory=dict)
    """Dependents within the configured depth."""
    reachable: dict[str, Reach] = field(default_factory=dict)
    """Dependents at unbounded depth. Needed so a node sitting just beyond the
    horizon is reported as beyond-horizon instead of falsely GREEN."""
    data_reach: dict[str, Reach] = field(default_factory=dict)
    """Data stores written or read downstream of the change. Informational --
    these are dependencies, not dependents, so they are reported separately and
    never drive the risk state."""

    def distance(self, node_id: str) -> int | None:
        reach = self.reachable.get(node_id)
        return reach.distance if reach else None


DATA_NODE_TYPES = {NodeType.TABLE, NodeType.DATABASE, NodeType.EXTERNAL_SERVICE}
UNBOUNDED = 10_000


def compute(store: GraphStore, changed_node_ids: list[str], max_depth: int) -> BlastRadius:
    reachable = store.find_dependents(changed_node_ids, max_depth=UNBOUNDED)
    dependents = {
        node_id: reach
        for node_id, reach in reachable.items()
        if reach.distance <= max_depth
    }
    downstream = store.find_dependencies(changed_node_ids, max_depth=max_depth)

    data_reach = {
        node_id: reach
        for node_id, reach in downstream.items()
        if node_id not in changed_node_ids
        and (node := store.get_node(node_id)) is not None
        and node.type in DATA_NODE_TYPES
    }

    return BlastRadius(
        changed_node_ids=list(changed_node_ids),
        dependents=dependents,
        reachable=reachable,
        data_reach=data_reach,
    )
