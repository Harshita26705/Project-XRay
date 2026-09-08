"""Repository ingestion: decide which files X-Ray is allowed to look at."""

from __future__ import annotations

from dataclasses import dataclass
from enum import Enum
from pathlib import Path

IGNORED_DIRS = {
    ".git",
    ".vs",
    ".vscode",
    ".idea",
    "node_modules",
    "bin",
    "obj",
    "dist",
    "build",
    "out",
    "coverage",
    "__pycache__",
    ".venv",
    "venv",
    "packages",
    "TestResults",
    ".xray",
}

IGNORED_SUFFIXES = {
    ".dll", ".exe", ".pdb", ".so", ".dylib", ".zip", ".gz", ".7z", ".tar",
    ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".webp", ".bmp",
    ".woff", ".woff2", ".ttf", ".eot", ".otf",
    ".pdf", ".xlsx", ".docx", ".pptx",
    ".min.js", ".map", ".lock",
}

MAX_FILE_BYTES = 1_500_000


class FileKind(str, Enum):
    CSHARP = "CSHARP"
    TYPESCRIPT = "TYPESCRIPT"
    SQL = "SQL"
    CONFIG = "CONFIG"
    DOCS = "DOCS"
    OTHER = "OTHER"


@dataclass
class SourceFile:
    path: str
    """Repository-relative, forward slashes."""
    absolute: Path
    kind: FileKind
    text: str


def classify(path: str) -> FileKind:
    lower = path.lower()
    if lower.endswith(".cs"):
        return FileKind.CSHARP
    if lower.endswith((".ts", ".tsx", ".js", ".jsx")):
        return FileKind.TYPESCRIPT
    if lower.endswith(".sql"):
        return FileKind.SQL
    if lower.endswith((".json", ".yaml", ".yml", ".config", ".csproj", ".props")):
        return FileKind.CONFIG
    if lower.endswith((".md", ".txt")):
        return FileKind.DOCS
    return FileKind.OTHER


def _is_ignored(rel_parts: tuple[str, ...], name: str) -> bool:
    if any(part in IGNORED_DIRS for part in rel_parts):
        return True
    lower = name.lower()
    return any(lower.endswith(suffix) for suffix in IGNORED_SUFFIXES)


def walk_repository(root: Path, include: list[str] | None = None) -> list[SourceFile]:
    """Collect parseable source files under `root`.

    `include` optionally restricts ingestion to a list of relative subtrees,
    which is how a large target repository is scoped down to a few modules.
    """
    root = root.resolve()
    collected: list[SourceFile] = []

    for candidate in sorted(root.rglob("*")):
        if not candidate.is_file():
            continue
        rel = candidate.relative_to(root)
        rel_posix = rel.as_posix()
        if _is_ignored(rel.parts[:-1], candidate.name):
            continue
        if include and not any(
            rel_posix == inc or rel_posix.startswith(inc.rstrip("/") + "/") for inc in include
        ):
            continue
        kind = classify(rel_posix)
        if kind is FileKind.OTHER:
            continue
        try:
            if candidate.stat().st_size > MAX_FILE_BYTES:
                continue
            text = candidate.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeDecodeError):
            continue
        collected.append(
            SourceFile(path=rel_posix, absolute=candidate, kind=kind, text=text)
        )

    return collected
