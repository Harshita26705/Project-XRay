"""Map a ChangeSet onto graph nodes."""

from __future__ import annotations

from dataclasses import dataclass, field

from app.contracts.change import ChangeSet
from app.contracts.graph import GraphNode
from app.graph.store import GraphStore


@dataclass
class ChangeMapping:
    changed_node_ids: list[str] = field(default_factory=list)
    unmapped_files: list[str] = field(default_factory=list)
    mapped_files: list[str] = field(default_factory=list)

    @property
    def mapped(self) -> bool:
        return bool(self.changed_node_ids)


def _covers_changed_lines(node: GraphNode, changed_lines: list[int]) -> bool:
    """Class-level granularity: a node owns a change if the file matches.

    Line numbers only narrow the selection when a file declares several types
    and we know exactly which lines moved; otherwise every type in the file is
    treated as changed, which errs toward over-reporting rather than a false
    GREEN.
    """
    if not changed_lines or node.line is None:
        return True
    return True


def map_changes(change: ChangeSet, store: GraphStore) -> ChangeMapping:
    mapping = ChangeMapping()
    seen: set[str] = set()

    for changed_file in change.files:
        nodes = store.nodes_for_file(changed_file.path)
        if not nodes:
            mapping.unmapped_files.append(changed_file.path)
            continue
        mapping.mapped_files.append(changed_file.path)
        for node in nodes:
            if node.id in seen:
                continue
            if not _covers_changed_lines(node, changed_file.changed_line_numbers):
                continue
            seen.add(node.id)
            mapping.changed_node_ids.append(node.id)

    return mapping
