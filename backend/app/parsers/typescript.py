"""React / TypeScript parser.

Resolves module imports and HTTP call sites. Call sites are normalised into the
same route signature the C# parser produces, which is what allows a frontend
component to be linked to the controller that serves it.
"""

from __future__ import annotations

import re
from pathlib import PurePosixPath

from app.contracts.graph import (
    GraphEdge,
    GraphNode,
    NodeType,
    ParseStatus,
    RelationshipType,
    node_id,
)
from app.ingestion.walker import FileKind, SourceFile
from app.parsers.base import (
    ParseOutput,
    api_node_id,
    line_of,
    normalise_route,
    strip_comments,
)

IMPORT_FROM = re.compile(r"""(?m)^\s*import\s+(?:[\s\S]*?\s+from\s+)?['"]([^'"]+)['"]""")
FETCH_CALL = re.compile(r"""\bfetch\s*\(\s*([`'"])([^`'"]*)\1""")
CLIENT_CALL = re.compile(
    r"""\b(?:axios|api|http|client|apiClient|httpClient)\s*\.\s*"""
    r"""(get|post|put|delete|patch)\s*(?:<[^>]*>)?\s*\(\s*([`'"])([^`'"]*)\2""",
    re.IGNORECASE,
)
METHOD_OPTION = re.compile(r"""method\s*:\s*['"](\w+)['"]""", re.IGNORECASE)
COMPONENT_EXPORT = re.compile(
    r"(?m)^\s*export\s+(?:default\s+)?(?:function\s+([A-Z]\w*)|const\s+([A-Z]\w*)\s*[:=])"
)
RESOLVE_EXTENSIONS = (".ts", ".tsx", ".js", ".jsx")


def _looks_like_api(url: str) -> bool:
    return url.startswith("/") or url.startswith("${") or "/api/" in url


class TypeScriptParser:
    kind = FileKind.TYPESCRIPT

    def parse(self, files: list[SourceFile]) -> ParseOutput:
        sources = [f for f in files if f.kind is FileKind.TYPESCRIPT]
        out = ParseOutput()
        if not sources:
            return out

        by_path = {f.path: f for f in sources}
        node_ids: dict[str, str] = {}

        for source in sources:
            text = strip_comments(source.text)
            component = COMPONENT_EXPORT.search(text)
            name = None
            if component:
                name = component.group(1) or component.group(2)
            is_component = bool(name) and source.path.endswith((".tsx", ".jsx"))
            node_type = (
                NodeType.FRONTEND_COMPONENT if is_component else NodeType.FRONTEND_MODULE
            )
            identifier = node_id(node_type, source.path)
            node_ids[source.path] = identifier
            out.nodes.append(
                GraphNode(
                    id=identifier,
                    type=node_type,
                    name=name or PurePosixPath(source.path).name,
                    qualified_name=source.path,
                    source_file=source.path,
                    line=1,
                    parse_status=ParseStatus.COMPLETE,
                )
            )
            out.file_status[source.path] = ParseStatus.COMPLETE

        for source in sources:
            text = strip_comments(source.text)
            self_id = node_ids[source.path]

            for match in IMPORT_FROM.finditer(text):
                specifier = match.group(1)
                if not specifier.startswith("."):
                    continue  # third-party package, not a first-party dependency
                resolved = self._resolve(source.path, specifier, by_path)
                if resolved is None:
                    continue
                out.edges.append(
                    GraphEdge(
                        source=self_id,
                        target=node_ids[resolved],
                        relationship=RelationshipType.IMPORTS,
                        rule_id="typescript.import",
                        confidence=0.98,
                        source_file=source.path,
                        line=line_of(text, match.start()),
                    )
                )

            for match in FETCH_CALL.finditer(text):
                url = match.group(2)
                if not _looks_like_api(url):
                    continue
                # The verb lives in the options object after the URL; scan a
                # bounded window rather than trying to brace-match nested opts.
                window = text[match.end() : match.end() + 300]
                verb_match = METHOD_OPTION.search(window)
                verb = verb_match.group(1) if verb_match else "GET"
                self._emit_call(out, self_id, source.path, text, match.start(), verb, url)

            for match in CLIENT_CALL.finditer(text):
                url = match.group(3)
                if not _looks_like_api(url):
                    continue
                self._emit_call(
                    out, self_id, source.path, text, match.start(), match.group(1), url
                )

        return out

    # ------------------------------------------------------------------
    def _emit_call(
        self,
        out: ParseOutput,
        self_id: str,
        path: str,
        text: str,
        offset: int,
        verb: str,
        url: str,
    ) -> None:
        signature = normalise_route(verb, url)
        identifier = api_node_id(verb, url)
        line = line_of(text, offset)
        # Stub node: the linker upgrades this if a controller declares the same
        # signature, and marks it unresolved if nothing does.
        out.nodes.append(
            GraphNode(
                id=identifier,
                type=NodeType.API,
                name=signature,
                qualified_name=signature,
                parse_status=ParseStatus.PARTIAL,
                attributes={"resolved": "false"},
            )
        )
        out.edges.append(
            GraphEdge(
                source=self_id,
                target=identifier,
                relationship=RelationshipType.CALLS,
                rule_id="typescript.http_call",
                confidence=0.9,
                source_file=path,
                line=line,
            )
        )

    def _resolve(
        self, importer: str, specifier: str, by_path: dict[str, SourceFile]
    ) -> str | None:
        base = PurePosixPath(importer).parent
        target = (base / specifier).as_posix()
        target = PurePosixPath(target)
        # Normalise ".." segments.
        parts: list[str] = []
        for part in target.parts:
            if part == "..":
                if parts:
                    parts.pop()
            elif part != ".":
                parts.append(part)
        candidate = "/".join(parts)

        if candidate in by_path:
            return candidate
        for ext in RESOLVE_EXTENSIONS:
            if candidate + ext in by_path:
                return candidate + ext
        for ext in RESOLVE_EXTENSIONS:
            if f"{candidate}/index{ext}" in by_path:
                return f"{candidate}/index{ext}"
        return None
