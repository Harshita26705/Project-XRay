"""Parse a raw unified diff (e.g. pasted from a pull request) into ChangedFile records.

This lets the PR Report tab extract code changes from a diff the caller pastes
in, without depending on any specific source-control provider's API.
"""

from __future__ import annotations

import re

from app.contracts.change import ChangedFile, ChangeKind

_FILE_HEADER = re.compile(r"^\+\+\+ (?:b/)?(.+)$")
_OLD_FILE_HEADER = re.compile(r"^--- (?:a/)?(.+)$")
_HUNK_HEADER = re.compile(r"^@@ -\d+(?:,\d+)? \+(\d+)(?:,\d+)? @@")
_DEV_NULL = "/dev/null"


def parse_unified_diff(text: str) -> list[ChangedFile]:
    """Parse unified-diff text into one ChangedFile per touched path.

    Best-effort: unparseable or empty input yields an empty list rather than
    raising, since the caller treats this as "no files detected".
    """
    files: list[ChangedFile] = []
    current_path: str | None = None
    old_path: str | None = None
    added = 0
    removed = 0
    changed_lines: list[int] = []
    next_new_line = 0

    def flush() -> None:
        if current_path is None:
            return
        kind = ChangeKind.MODIFIED
        if old_path == _DEV_NULL:
            kind = ChangeKind.ADDED
        elif current_path == _DEV_NULL:
            kind = ChangeKind.DELETED
        path = old_path if current_path == _DEV_NULL else current_path
        if not path or path == _DEV_NULL:
            return
        files.append(
            ChangedFile(
                path=path.strip(),
                kind=kind,
                added_lines=added,
                removed_lines=removed,
                changed_line_numbers=changed_lines[:200],
            )
        )

    for line in text.splitlines():
        old_header = _OLD_FILE_HEADER.match(line)
        new_header = _FILE_HEADER.match(line)
        if old_header:
            old_path = old_header.group(1)
            continue
        if new_header:
            flush()
            current_path = new_header.group(1)
            added = 0
            removed = 0
            changed_lines = []
            continue
        hunk = _HUNK_HEADER.match(line)
        if hunk:
            next_new_line = int(hunk.group(1))
            continue
        if current_path is None:
            continue
        if line.startswith("+++") or line.startswith("---"):
            continue
        if line.startswith("+"):
            added += 1
            changed_lines.append(next_new_line)
            next_new_line += 1
        elif line.startswith("-"):
            removed += 1
        else:
            next_new_line += 1

    flush()
    return files
