"""Graph storage abstraction.

The application only ever talks to `GraphStore`. The in-memory implementation
here can be replaced by a persistent one (SQLite, Cosmos, ...) without touching
any caller. Traversal is implemented once on the base class so every backend
gets identical, deterministic semantics.
"""

from __future__ import annotations

import heapq
from abc import ABC, abstractmethod
from collections import OrderedDict
from dataclasses import dataclass, field

from app.contracts.graph import GraphEdge, GraphNode, RelationshipType

ZERO_COST_RELATIONSHIPS = {RelationshipType.BINDS}
"""An interface and the implementation it is bound to are the same logical
component for impact purposes, so traversing that edge must not consume a hop.
Without this, dependency-injection indirection doubles every distance and the
depth thresholds stop describing real architectural distance."""


@dataclass
class Reach:
    """Result of a traversal to one node."""

    node_id: str
    distance: int
    path: list[GraphEdge] = field(default_factory=list)
    min_confidence: float = 1.0
    dynamic: bool = False


class GraphStore(ABC):
    # --- primitives -----------------------------------------------------
    @abstractmethod
    def add_node(self, node: GraphNode) -> None: ...

    @abstractmethod
    def add_edge(self, edge: GraphEdge) -> None: ...

    @abstractmethod
    def get_node(self, node_id: str) -> GraphNode | None: ...

    @abstractmethod
    def nodes(self) -> list[GraphNode]: ...

    @abstractmethod
    def edges(self) -> list[GraphEdge]: ...

    @abstractmethod
    def outgoing(self, node_id: str) -> list[GraphEdge]:
        """Edges where `node_id` is the dependent (its dependencies)."""

    @abstractmethod
    def incoming(self, node_id: str) -> list[GraphEdge]:
        """Edges where `node_id` is the dependency (its dependents)."""

    @abstractmethod
    def clear(self) -> None: ...

    def has_node(self, node_id: str) -> bool:
        return self.get_node(node_id) is not None

    # --- traversal ------------------------------------------------------
    def _walk(
        self,
        seeds: list[str],
        max_depth: int,
        reverse: bool,
    ) -> dict[str, Reach]:
        """Bottleneck shortest-path walk.

        reverse=True follows incoming edges, i.e. finds *dependents* -- the
        nodes that could break when a seed changes.

        Nodes are settled in order of (hops, then highest minimum edge
        confidence), so the evidence chain shown to the user is the shortest
        path and, among equally short paths, the strongest-evidence one.
        """
        settled: OrderedDict[str, Reach] = OrderedDict()
        counter = 0
        queue: list[tuple[int, float, int, str, tuple[GraphEdge, ...], float, bool]] = []
        for seed in seeds:
            if not self.has_node(seed):
                continue
            counter += 1
            heapq.heappush(queue, (0, -1.0, counter, seed, (), 1.0, False))

        while queue:
            distance, _, _, current, path, min_conf, dynamic = heapq.heappop(queue)
            if current in settled:
                continue
            settled[current] = Reach(
                node_id=current,
                distance=distance,
                path=list(path),
                min_confidence=min_conf,
                dynamic=dynamic,
            )
            if distance >= max_depth:
                continue
            edges = self.incoming(current) if reverse else self.outgoing(current)
            for edge in edges:
                neighbour = edge.source if reverse else edge.target
                if neighbour in settled:
                    continue
                cost = 0 if edge.relationship in ZERO_COST_RELATIONSHIPS else 1
                next_distance = distance + cost
                if next_distance > max_depth:
                    continue
                next_conf = min(min_conf, edge.confidence)
                counter += 1
                heapq.heappush(
                    queue,
                    (
                        next_distance,
                        -next_conf,
                        counter,
                        neighbour,
                        (*path, edge),
                        next_conf,
                        dynamic or edge.dynamic,
                    ),
                )

        return settled

    def find_dependents(self, node_ids: list[str], max_depth: int = 6) -> dict[str, Reach]:
        return self._walk(node_ids, max_depth, reverse=True)

    def find_dependencies(self, node_ids: list[str], max_depth: int = 6) -> dict[str, Reach]:
        return self._walk(node_ids, max_depth, reverse=False)

    def find_path(self, source: str, target: str, max_depth: int = 12) -> list[GraphEdge] | None:
        reach = self._walk([source], max_depth, reverse=False).get(target)
        return reach.path if reach else None

    def nodes_for_file(self, path: str) -> list[GraphNode]:
        normalised = path.replace("\\", "/").lstrip("./").lower()
        return [
            n
            for n in self.nodes()
            if n.source_file and n.source_file.replace("\\", "/").lower() == normalised
        ]


class InMemoryGraphStore(GraphStore):
    def __init__(self) -> None:
        self._nodes: OrderedDict[str, GraphNode] = OrderedDict()
        self._edges: list[GraphEdge] = []
        self._out: dict[str, list[GraphEdge]] = {}
        self._in: dict[str, list[GraphEdge]] = {}
        self._edge_keys: set[tuple[str, str, str]] = set()

    def add_node(self, node: GraphNode) -> None:
        existing = self._nodes.get(node.id)
        if existing is None:
            self._nodes[node.id] = node
            return
        # The same node can be emitted by several parsers -- a controller
        # declares an endpoint while a React module only references it. Keep the
        # definition that carries a source location and never let a bare
        # reference downgrade it.
        if existing.source_file is None and node.source_file is not None:
            richer, poorer = node, existing
        else:
            richer, poorer = existing, node
        richer.attributes = {**poorer.attributes, **richer.attributes}
        richer.critical = existing.critical or node.critical
        self._nodes[node.id] = richer

    def add_edge(self, edge: GraphEdge) -> None:
        key = (edge.source, edge.target, edge.relationship.value)
        if key in self._edge_keys:
            return
        self._edge_keys.add(key)
        self._edges.append(edge)
        self._out.setdefault(edge.source, []).append(edge)
        self._in.setdefault(edge.target, []).append(edge)

    def get_node(self, node_id: str) -> GraphNode | None:
        return self._nodes.get(node_id)

    def nodes(self) -> list[GraphNode]:
        return list(self._nodes.values())

    def edges(self) -> list[GraphEdge]:
        return list(self._edges)

    def outgoing(self, node_id: str) -> list[GraphEdge]:
        return list(self._out.get(node_id, []))

    def incoming(self, node_id: str) -> list[GraphEdge]:
        return list(self._in.get(node_id, []))

    def clear(self) -> None:
        self._nodes.clear()
        self._edges.clear()
        self._out.clear()
        self._in.clear()
        self._edge_keys.clear()
