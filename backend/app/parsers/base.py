"""Shared parser plumbing."""

from __future__ import annotations

import re
from dataclasses import dataclass, field

from app.contracts.graph import GraphEdge, GraphNode, NodeType, ParseStatus, node_id

_ROUTE_PARAM = re.compile(r"\{[^}]*\}")
_TEMPLATE_PARAM = re.compile(r"\$\{[^}]*\}")


@dataclass
class ParseOutput:
    nodes: list[GraphNode] = field(default_factory=list)
    edges: list[GraphEdge] = field(default_factory=list)
    file_status: dict[str, ParseStatus] = field(default_factory=dict)

    def extend(self, other: "ParseOutput") -> None:
        self.nodes.extend(other.nodes)
        self.edges.extend(other.edges)
        self.file_status.update(other.file_status)


def line_of(text: str, offset: int) -> int:
    return text.count("\n", 0, offset) + 1


def strip_comments(text: str) -> str:
    """Blank out comments while preserving every character offset."""
    out = list(text)
    i = 0
    n = len(text)
    in_line_comment = False
    in_block_comment = False
    in_string: str | None = None
    verbatim = False

    while i < n:
        ch = text[i]
        nxt = text[i + 1] if i + 1 < n else ""

        if in_line_comment:
            if ch == "\n":
                in_line_comment = False
            else:
                out[i] = " "
            i += 1
            continue

        if in_block_comment:
            if ch == "*" and nxt == "/":
                out[i] = out[i + 1] = " "
                i += 2
                in_block_comment = False
                continue
            if ch != "\n":
                out[i] = " "
            i += 1
            continue

        if in_string is not None:
            if verbatim:
                if ch == '"' and nxt == '"':
                    i += 2
                    continue
                if ch == '"':
                    in_string = None
                    verbatim = False
            else:
                if ch == "\\":
                    i += 2
                    continue
                if ch == in_string:
                    in_string = None
            i += 1
            continue

        if ch == "/" and nxt == "/":
            out[i] = out[i + 1] = " "
            i += 2
            in_line_comment = True
            continue
        if ch == "/" and nxt == "*":
            out[i] = out[i + 1] = " "
            i += 2
            in_block_comment = True
            continue
        if ch == "@" and nxt == '"':
            in_string = '"'
            verbatim = True
            i += 2
            continue
        if ch in "\"'`":
            in_string = ch
            i += 1
            continue
        i += 1

    return "".join(out)


def match_block(text: str, open_index: int) -> int:
    """Return the index just past the `}` matching the `{` at open_index."""
    depth = 0
    for i in range(open_index, len(text)):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                return i + 1
    return len(text)


def normalise_route(method: str, path: str) -> str:
    """Collapse a route to a comparable signature.

    `/api/Payments/{id:int}` and `` `/api/payments/${id}` `` both become
    `/api/payments/{}`, which is what lets the React parser and the C# parser
    agree on a single API node.
    """
    cleaned = _TEMPLATE_PARAM.sub("{}", path)
    cleaned = _ROUTE_PARAM.sub("{}", cleaned)
    cleaned = cleaned.split("?", 1)[0]
    cleaned = re.sub(r"/{2,}", "/", cleaned)
    cleaned = "/" + cleaned.strip("/")
    return f"{method.upper()} {cleaned.lower()}"


def api_node_id(method: str, path: str) -> str:
    return node_id(NodeType.API, normalise_route(method, path))
