"""C#/.NET parser.

Deliberately structural rather than semantic: it resolves types by simple name
across the ingested file set. That is accurate for a conventional layered
ASP.NET Core codebase (controller -> interface -> service -> repository ->
DbContext) and it degrades honestly -- anything it cannot resolve becomes a
low-confidence or dynamic edge, which the classifier turns into YELLOW or
UNKNOWN rather than a confident GREEN.

Known limitation: types are keyed by simple name, so two same-named classes in
different namespaces collapse into one node.
"""

from __future__ import annotations

import re
from dataclasses import dataclass, field

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
    match_block,
    normalise_route,
    strip_comments,
)

TYPE_DECL = re.compile(
    r"(?m)^[ \t]*(?:(?:public|internal|protected|private|sealed|abstract|static|partial|file|new)[ \t]+)*"
    r"(class|interface|record|struct)[ \t]+([A-Za-z_]\w*)"
)
ATTRIBUTE_LINE = re.compile(r"(?m)^[ \t]*\[[^\]]*\][ \t]*$")
ROUTE_ATTR = re.compile(r'\[\s*Route\s*\(\s*"([^"]*)"\s*\)\s*\]', re.IGNORECASE)
HTTP_ATTR = re.compile(
    r'\[\s*Http(Get|Post|Put|Delete|Patch)\s*(?:\(\s*"([^"]*)"\s*\))?\s*\]',
    re.IGNORECASE,
)
METHOD_AFTER_ATTR = re.compile(
    r"[\s\S]{0,400}?\b(?:public|internal|protected|private)\s+(?:async\s+)?"
    r"[A-Za-z_][\w<>,\[\]\.\? ]*\s+([A-Za-z_]\w*)\s*\("
)
FIELD_DECL = re.compile(
    r"(?m)^[ \t]*(?:private|protected|public|internal)[ \t]+(?:static[ \t]+)?(?:readonly[ \t]+)?"
    r"([A-Za-z_][\w\.]*(?:<[^>;=()]*>)?)[ \t]+([A-Za-z_]\w*)[ \t]*(?:=[^;]*)?;"
)
DBSET_DECL = re.compile(
    r"\bDbSet<\s*([A-Za-z_][\w\.]*)\s*>\s+([A-Za-z_]\w*)"
)
DI_REGISTRATION = re.compile(
    r"\.Add(?:Scoped|Transient|Singleton)\s*<\s*([A-Za-z_][\w\.]*)\s*,\s*([A-Za-z_][\w\.]*)\s*>"
)
INVOCATION = re.compile(r"\b([A-Za-z_]\w*)\s*\.\s*([A-Za-z_]\w*)\s*\(")
MEMBER_ACCESS = re.compile(r"\b([A-Za-z_]\w*)\s*\.\s*([A-Za-z_]\w*)\b")
OBJECT_CREATION = re.compile(r"\bnew\s+([A-Za-z_][\w\.]*)\s*[\(\{]")
STRING_LITERAL = re.compile(r'@?"((?:[^"\\]|\\.)*)"')
SQL_TABLE = re.compile(
    r"(?is)\b(?:from|join|into|update)\s+((?:\[?\w+\]?\s*\.\s*)?\[?\w+\]?)"
)
SQL_KEYWORDS = re.compile(r"(?is)\b(select|insert\s+into|update|delete\s+from)\b")
WRITE_HINT = re.compile(r"\.(Add|AddAsync|Update|Remove|RemoveRange|AddRange)\s*\(|SaveChanges")

SERVICE_SUFFIXES = ("Service", "Validator", "Manager", "Handler", "Processor", "Provider", "Client")
FRAMEWORK_TYPES = {"var", "string", "int", "bool", "Task", "List", "Guid", "DateTime", "decimal"}


@dataclass
class TypeInfo:
    name: str
    keyword: str
    node_type: NodeType
    file: str
    line: int
    body_start: int
    body_end: int
    attributes: str
    base_types: list[str] = field(default_factory=list)


def _classify(name: str, keyword: str) -> NodeType:
    if keyword == "interface":
        return NodeType.INTERFACE
    if name.endswith(SERVICE_SUFFIXES):
        return NodeType.SERVICE
    return NodeType.CLASS


def _attributes_before(text: str, start: int) -> str:
    """Collect the `[Attribute]` lines immediately above a declaration."""
    line_start = text.rfind("\n", 0, start) + 1
    collected: list[str] = []
    cursor = line_start
    while cursor > 0:
        prev_end = cursor - 1
        prev_start = text.rfind("\n", 0, prev_end) + 1
        line = text[prev_start:prev_end]
        if line.strip() == "":
            cursor = prev_start
            continue
        if line.strip().startswith("[") and line.strip().endswith("]"):
            collected.insert(0, line.strip())
            cursor = prev_start
            continue
        break
    return "\n".join(collected)


def _normalise_table(raw: str) -> tuple[str, str]:
    cleaned = raw.replace("[", "").replace("]", "").replace(" ", "")
    parts = [p for p in cleaned.split(".") if p]
    if len(parts) >= 2:
        schema, table = parts[-2], parts[-1]
    else:
        schema, table = "dbo", parts[-1] if parts else "unknown"
    display = f"{schema}.{table}"
    return display, display.lower()


def table_node(raw: str) -> GraphNode:
    display, key = _normalise_table(raw)
    return GraphNode(
        id=node_id(NodeType.TABLE, key),
        type=NodeType.TABLE,
        name=display,
        qualified_name=display,
    )


class CSharpParser:
    kind = FileKind.CSHARP

    def parse(self, files: list[SourceFile]) -> ParseOutput:
        sources = [f for f in files if f.kind is FileKind.CSHARP]
        out = ParseOutput()
        if not sources:
            return out

        cleaned: dict[str, str] = {f.path: strip_comments(f.text) for f in sources}
        types: dict[str, TypeInfo] = {}
        type_files: dict[str, list[TypeInfo]] = {}

        # Pass 1 -- discover every declared type so that pass 2 only emits edges
        # for types we actually own, instead of framework noise.
        for source in sources:
            text = cleaned[source.path]
            for match in TYPE_DECL.finditer(text):
                keyword, name = match.group(1), match.group(2)
                brace = text.find("{", match.end())
                if brace == -1:
                    continue
                body_end = match_block(text, brace)
                header = text[match.end():brace]
                bases = [
                    b.strip().split("<")[0]
                    for b in header.split(":", 1)[1].split(",")
                ] if ":" in header else []
                info = TypeInfo(
                    name=name,
                    keyword=keyword,
                    node_type=_classify(name, keyword),
                    file=source.path,
                    line=line_of(text, match.start()),
                    body_start=brace + 1,
                    body_end=body_end - 1,
                    attributes=_attributes_before(text, match.start()),
                    base_types=[b for b in bases if b],
                )
                types[name] = info
                type_files.setdefault(source.path, []).append(info)

        ids = {name: node_id(info.node_type, name) for name, info in types.items()}
        dbset_tables: dict[str, dict[str, str]] = {}

        for name, info in types.items():
            out.nodes.append(
                GraphNode(
                    id=ids[name],
                    type=info.node_type,
                    name=name,
                    qualified_name=name,
                    source_file=info.file,
                    line=info.line,
                    parse_status=ParseStatus.COMPLETE,
                    attributes={"keyword": info.keyword},
                )
            )

        # Pass 2 -- members and relationships.
        for source in sources:
            text = cleaned[source.path]
            out.file_status[source.path] = ParseStatus.COMPLETE

            for interface, implementation in DI_REGISTRATION.findall(text):
                iface, impl = interface.split(".")[-1], implementation.split(".")[-1]
                if iface in ids and impl in ids:
                    out.edges.append(
                        GraphEdge(
                            source=ids[iface],
                            target=ids[impl],
                            relationship=RelationshipType.BINDS,
                            rule_id="csharp.di_registration",
                            confidence=0.98,
                            source_file=source.path,
                            line=line_of(text, text.find(f"{interface}, {implementation}")),
                        )
                    )

            for info in type_files.get(source.path, []):
                self._parse_type(info, text, ids, types, dbset_tables, out)

        self._infer_missing_bindings(types, ids, out)
        return out

    # ------------------------------------------------------------------
    def _parse_type(
        self,
        info: TypeInfo,
        text: str,
        ids: dict[str, str],
        types: dict[str, TypeInfo],
        dbset_tables: dict[str, dict[str, str]],
        out: ParseOutput,
    ) -> None:
        body = text[info.body_start : info.body_end]
        offset = info.body_start
        self_id = ids[info.name]
        var_types: dict[str, str] = {}

        for base in info.base_types:
            if base in ids and base != info.name:
                out.edges.append(
                    GraphEdge(
                        source=self_id,
                        target=ids[base],
                        relationship=RelationshipType.DEPENDS_ON,
                        rule_id="csharp.base_type",
                        confidence=0.9,
                        source_file=info.file,
                        line=info.line,
                    )
                )

        # Constructor injection -- the dominant dependency signal in ASP.NET Core.
        ctor = re.search(rf"(?m)^[ \t]*public[ \t]+{re.escape(info.name)}[ \t]*\(", body)
        if ctor:
            open_paren = body.find("(", ctor.start())
            close_paren = body.find(")", open_paren)
            params = body[open_paren + 1 : close_paren] if close_paren != -1 else ""
            for param in params.split(","):
                tokens = param.strip().split()
                if len(tokens) < 2:
                    continue
                param_type = tokens[-2].split("<")[0].split(".")[-1]
                param_name = tokens[-1]
                if param_type in ids:
                    var_types[param_name] = param_type
                    out.edges.append(
                        GraphEdge(
                            source=self_id,
                            target=ids[param_type],
                            relationship=RelationshipType.DEPENDS_ON,
                            rule_id="csharp.ctor_injection",
                            confidence=0.95,
                            source_file=info.file,
                            line=line_of(text, offset + ctor.start()),
                        )
                    )

        for match in FIELD_DECL.finditer(body):
            field_type = match.group(1).split("<")[0].split(".")[-1]
            field_name = match.group(2)
            if field_type in FRAMEWORK_TYPES:
                continue
            if field_type in ids:
                var_types[field_name] = field_type
                out.edges.append(
                    GraphEdge(
                        source=self_id,
                        target=ids[field_type],
                        relationship=RelationshipType.DEPENDS_ON,
                        rule_id="csharp.field_declaration",
                        confidence=0.9,
                        source_file=info.file,
                        line=line_of(text, offset + match.start()),
                    )
                )

        for match in DBSET_DECL.finditer(body):
            node = table_node(match.group(2))
            out.nodes.append(node)
            dbset_tables.setdefault(info.name, {})[match.group(2)] = node.id
            out.edges.append(
                GraphEdge(
                    source=self_id,
                    target=node.id,
                    relationship=RelationshipType.USES,
                    rule_id="csharp.ef_dbset",
                    confidence=0.95,
                    source_file=info.file,
                    line=line_of(text, offset + match.start()),
                )
            )

        self._parse_endpoints(info, text, body, offset, self_id, out)

        for match in INVOCATION.finditer(body):
            receiver, _method = match.group(1), match.group(2)
            target_type = var_types.get(receiver)
            rule = "csharp.invocation_on_injected_member"
            confidence = 0.95
            if target_type is None and receiver in ids and receiver != info.name:
                target_type = receiver
                rule = "csharp.static_invocation"
                confidence = 0.85
            if target_type and target_type != info.name:
                out.edges.append(
                    GraphEdge(
                        source=self_id,
                        target=ids[target_type],
                        relationship=RelationshipType.CALLS,
                        rule_id=rule,
                        confidence=confidence,
                        source_file=info.file,
                        line=line_of(text, offset + match.start()),
                    )
                )

        for match in OBJECT_CREATION.finditer(body):
            created = match.group(1).split(".")[-1]
            if created in ids and created != info.name:
                out.edges.append(
                    GraphEdge(
                        source=self_id,
                        target=ids[created],
                        relationship=RelationshipType.DEPENDS_ON,
                        rule_id="csharp.object_creation",
                        confidence=0.9,
                        source_file=info.file,
                        line=line_of(text, offset + match.start()),
                    )
                )

        self._parse_dbset_access(info, text, body, offset, self_id, var_types, dbset_tables, out)
        self._parse_raw_sql(info, text, body, offset, self_id, out)

    # ------------------------------------------------------------------
    def _parse_endpoints(
        self,
        info: TypeInfo,
        text: str,
        body: str,
        offset: int,
        self_id: str,
        out: ParseOutput,
    ) -> None:
        route_match = ROUTE_ATTR.search(info.attributes)
        if not route_match and not HTTP_ATTR.search(body):
            return
        base_route = route_match.group(1) if route_match else ""
        controller_token = info.name[:-10] if info.name.endswith("Controller") else info.name
        base_route = re.sub(
            r"\[controller\]", controller_token, base_route, flags=re.IGNORECASE
        )

        for match in HTTP_ATTR.finditer(body):
            verb = match.group(1).upper()
            template = match.group(2) or ""
            method_match = METHOD_AFTER_ATTR.match(body, match.end())
            method_name = method_match.group(1) if method_match else "unknown"
            path = f"{base_route}/{template}" if template else base_route
            signature = normalise_route(verb, path)
            endpoint_id = api_node_id(verb, path)
            line = line_of(text, offset + match.start())
            out.nodes.append(
                GraphNode(
                    id=endpoint_id,
                    type=NodeType.API,
                    name=signature,
                    qualified_name=signature,
                    source_file=info.file,
                    line=line,
                    attributes={"handler": f"{info.name}.{method_name}", "resolved": "true"},
                )
            )
            # The endpoint's behaviour is provided by the controller, so the
            # endpoint depends on the controller.
            out.edges.append(
                GraphEdge(
                    source=endpoint_id,
                    target=self_id,
                    relationship=RelationshipType.EXPOSES,
                    rule_id="csharp.api_exposed_by_controller",
                    confidence=0.98,
                    source_file=info.file,
                    line=line,
                )
            )

    # ------------------------------------------------------------------
    def _parse_dbset_access(
        self,
        info: TypeInfo,
        text: str,
        body: str,
        offset: int,
        self_id: str,
        var_types: dict[str, str],
        dbset_tables: dict[str, dict[str, str]],
        out: ParseOutput,
    ) -> None:
        for match in MEMBER_ACCESS.finditer(body):
            receiver, member = match.group(1), match.group(2)
            owner = var_types.get(receiver)
            if not owner:
                continue
            table_id = dbset_tables.get(owner, {}).get(member)
            if not table_id:
                continue
            window = body[match.start() : match.start() + 240]
            writes = bool(WRITE_HINT.search(window))
            out.edges.append(
                GraphEdge(
                    source=self_id,
                    target=table_id,
                    relationship=RelationshipType.WRITES if writes else RelationshipType.READS,
                    rule_id="csharp.ef_dbset_access",
                    confidence=0.9,
                    source_file=info.file,
                    line=line_of(text, offset + match.start()),
                )
            )

    def _parse_raw_sql(
        self,
        info: TypeInfo,
        text: str,
        body: str,
        offset: int,
        self_id: str,
        out: ParseOutput,
    ) -> None:
        for literal in STRING_LITERAL.finditer(body):
            content = literal.group(1)
            if not SQL_KEYWORDS.search(content):
                continue
            for raw_table in SQL_TABLE.findall(content):
                node = table_node(raw_table)
                if node.name.lower().endswith(".select"):
                    continue
                out.nodes.append(node)
                writes = bool(re.search(r"(?is)\b(insert|update|delete)\b", content))
                out.edges.append(
                    GraphEdge(
                        source=self_id,
                        target=node.id,
                        relationship=RelationshipType.WRITES if writes else RelationshipType.READS,
                        rule_id="csharp.raw_sql_reference",
                        confidence=0.7,
                        source_file=info.file,
                        line=line_of(text, offset + literal.start()),
                    )
                )

    # ------------------------------------------------------------------
    def _infer_missing_bindings(
        self,
        types: dict[str, TypeInfo],
        ids: dict[str, str],
        out: ParseOutput,
    ) -> None:
        """Bind interfaces to implementations that were never registered in DI.

        These are inferences, not facts, so they get a confidence below the
        classifier threshold -- a dependent reached only through an inferred
        binding lands on YELLOW rather than RED.
        """
        registered = {
            (e.source, e.target)
            for e in out.edges
            if e.relationship is RelationshipType.BINDS
        }
        for name, info in types.items():
            if info.keyword == "interface":
                continue
            for base in info.base_types:
                if base not in types or types[base].keyword != "interface":
                    continue
                pair = (ids[base], ids[name])
                if pair in registered:
                    continue
                out.edges.append(
                    GraphEdge(
                        source=ids[base],
                        target=ids[name],
                        relationship=RelationshipType.BINDS,
                        rule_id="csharp.interface_implementation_inferred",
                        confidence=0.65,
                        source_file=info.file,
                        line=info.line,
                        dynamic=True,
                    )
                )
