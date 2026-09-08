"""Graph construction: ingest -> parse -> link -> store."""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from pathlib import Path

from app.contracts.graph import GraphNode, NodeType, ParseStatus, node_id
from app.graph.store import GraphStore
from app.ingestion import secrets
from app.ingestion.walker import SourceFile, walk_repository
from app.parsers.base import ParseOutput
from app.parsers.csharp import CSharpParser
from app.parsers.sql import SqlParser
from app.parsers.typescript import TypeScriptParser

logger = logging.getLogger(__name__)

PARSERS = (CSharpParser(), TypeScriptParser(), SqlParser())


@dataclass
class BuildReport:
    root: str
    files_ingested: int = 0
    files_failed: int = 0
    node_count: int = 0
    edge_count: int = 0
    unresolved_apis: list[str] = field(default_factory=list)
    secret_hits: int = 0
    file_status: dict[str, ParseStatus] = field(default_factory=dict)

    @property
    def parse_failure_ratio(self) -> float:
        if not self.files_ingested:
            return 1.0
        return self.files_failed / self.files_ingested


def build_graph(
    root: Path,
    store: GraphStore,
    include: list[str] | None = None,
    critical_names: list[str] | None = None,
) -> BuildReport:
    store.clear()
    files = walk_repository(root, include=include)
    report = BuildReport(root=str(root), files_ingested=len(files))

    # Mask before anything is stored so X-Ray never becomes a leak path.
    safe_files: list[SourceFile] = []
    for source in files:
        hits = secrets.scan(source.text)
        report.secret_hits += len(hits)
        text = secrets.mask(source.text) if hits else source.text
        safe_files.append(
            SourceFile(
                path=source.path, absolute=source.absolute, kind=source.kind, text=text
            )
        )

    combined = ParseOutput()
    for parser in PARSERS:
        owned = [f.path for f in safe_files if f.kind is parser.kind]
        try:
            combined.extend(parser.parse(safe_files))
        except Exception:  # a broken parser must not fake a clean graph
            logger.exception("parser %s failed", type(parser).__name__)
            for path in owned:
                combined.file_status[path] = ParseStatus.FAILED

    for source in safe_files:
        combined.file_status.setdefault(source.path, ParseStatus.PARTIAL)

    for node in combined.nodes:
        store.add_node(node)

    known = {n.id for n in store.nodes()}
    for edge in combined.edges:
        for endpoint in (edge.source, edge.target):
            if endpoint not in known:
                # Never drop an edge silently: materialise the endpoint as an
                # explicitly unparsed node so it classifies UNKNOWN.
                store.add_node(
                    GraphNode(
                        id=endpoint,
                        type=NodeType.EXTERNAL_SERVICE,
                        name=endpoint.split(":", 1)[-1],
                        parse_status=ParseStatus.FAILED,
                        attributes={"resolved": "false"},
                    )
                )
                known.add(endpoint)
        store.add_edge(edge)

    critical = {name.lower() for name in (critical_names or [])}
    for node in store.nodes():
        if node.type is NodeType.API and node.attributes.get("resolved") != "true":
            node.parse_status = ParseStatus.PARTIAL
            report.unresolved_apis.append(node.id)
        if critical and (
            node.name.lower() in critical or (node.qualified_name or "").lower() in critical
        ):
            node.critical = True

    report.file_status = combined.file_status
    report.files_failed = sum(
        1 for status in combined.file_status.values() if status is ParseStatus.FAILED
    )
    report.node_count = len(store.nodes())
    report.edge_count = len(store.edges())
    return report


def project_node_id(name: str) -> str:
    return node_id(NodeType.PROJECT, name)
