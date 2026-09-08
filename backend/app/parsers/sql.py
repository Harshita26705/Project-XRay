"""T-SQL DDL parser: tables and foreign keys."""

from __future__ import annotations

import re

from app.contracts.graph import GraphEdge, ParseStatus, RelationshipType
from app.ingestion.walker import FileKind, SourceFile
from app.parsers.base import ParseOutput, line_of
from app.parsers.csharp import table_node

CREATE_TABLE = re.compile(
    r"(?is)\bcreate\s+table\s+((?:\[?\w+\]?\s*\.\s*)?\[?\w+\]?)\s*\("
)
REFERENCES = re.compile(
    r"(?is)\breferences\s+((?:\[?\w+\]?\s*\.\s*)?\[?\w+\]?)"
)


def _table_body(text: str, open_paren: int) -> tuple[str, int]:
    depth = 0
    for i in range(open_paren, len(text)):
        if text[i] == "(":
            depth += 1
        elif text[i] == ")":
            depth -= 1
            if depth == 0:
                return text[open_paren + 1 : i], i
    return text[open_paren + 1 :], len(text)


class SqlParser:
    kind = FileKind.SQL

    def parse(self, files: list[SourceFile]) -> ParseOutput:
        out = ParseOutput()
        for source in (f for f in files if f.kind is FileKind.SQL):
            text = source.text
            out.file_status[source.path] = ParseStatus.COMPLETE
            for match in CREATE_TABLE.finditer(text):
                node = table_node(match.group(1))
                node.source_file = source.path
                node.line = line_of(text, match.start())
                out.nodes.append(node)

                body, _ = _table_body(text, match.end() - 1)
                for referenced in REFERENCES.findall(body):
                    parent = table_node(referenced)
                    if parent.id == node.id:
                        continue
                    out.nodes.append(parent)
                    out.edges.append(
                        GraphEdge(
                            source=node.id,
                            target=parent.id,
                            relationship=RelationshipType.DEPENDS_ON,
                            rule_id="sql.foreign_key",
                            confidence=0.95,
                            source_file=source.path,
                            line=node.line,
                        )
                    )
        return out
