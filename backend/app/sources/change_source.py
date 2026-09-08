"""Change sources.

The core analysis never depends on any single integration, so a local git diff
or a hand-written JSON payload is a first-class input alongside Azure DevOps.
"""

from __future__ import annotations

import logging
import re
import subprocess
from pathlib import Path
from typing import Protocol

from app.contracts.change import ChangedFile, ChangeKind, ChangeSet

logger = logging.getLogger(__name__)

NUMSTAT = re.compile(r"^(\d+|-)\t(\d+|-)\t(.+)$")
STATUS = re.compile(r"^([A-Z])\d*\t(.+)$")

_KIND_BY_STATUS = {
    "A": ChangeKind.ADDED,
    "M": ChangeKind.MODIFIED,
    "D": ChangeKind.DELETED,
    "R": ChangeKind.RENAMED,
}


class ChangeSource(Protocol):
    def load(self) -> ChangeSet: ...


class ManualChangeSource:
    """A change described directly by the caller. Used by the API and tests."""

    def __init__(self, change: ChangeSet) -> None:
        self._change = change

    def load(self) -> ChangeSet:
        return self._change


class GitDiffChangeSource:
    """Reads a diff from a local repository."""

    def __init__(self, repo: Path, base: str = "HEAD~1", head: str = "HEAD") -> None:
        self._repo = repo
        self._base = base
        self._head = head

    def load(self) -> ChangeSet:
        files = self._numstat()
        title = self._subject()
        return ChangeSet(
            source="git-diff",
            title=title,
            description=f"{self._base}..{self._head}",
            reference=f"{self._base}..{self._head}",
            files=files,
        )

    def _run(self, args: list[str]) -> str:
        result = subprocess.run(
            ["git", *args],
            cwd=self._repo,
            capture_output=True,
            text=True,
            timeout=60,
            shell=False,
        )
        if result.returncode != 0:
            raise RuntimeError(result.stderr.strip() or "git command failed")
        return result.stdout

    def _subject(self) -> str:
        try:
            return self._run(["log", "-1", "--pretty=%s", self._head]).strip()
        except (OSError, RuntimeError, subprocess.SubprocessError):
            return ""

    def _numstat(self) -> list[ChangedFile]:
        raw = self._run(["diff", "--numstat", f"{self._base}..{self._head}"])
        statuses = self._statuses()
        files: list[ChangedFile] = []
        for line in raw.splitlines():
            match = NUMSTAT.match(line.rstrip())
            if not match:
                continue
            added, removed, path = match.groups()
            files.append(
                ChangedFile(
                    path=path.replace("\\", "/"),
                    kind=statuses.get(path, ChangeKind.MODIFIED),
                    added_lines=int(added) if added.isdigit() else 0,
                    removed_lines=int(removed) if removed.isdigit() else 0,
                )
            )
        return files

    def _statuses(self) -> dict[str, ChangeKind]:
        try:
            raw = self._run(["diff", "--name-status", f"{self._base}..{self._head}"])
        except (OSError, RuntimeError, subprocess.SubprocessError):
            return {}
        result: dict[str, ChangeKind] = {}
        for line in raw.splitlines():
            match = STATUS.match(line.rstrip())
            if match:
                result[match.group(2)] = _KIND_BY_STATUS.get(
                    match.group(1), ChangeKind.MODIFIED
                )
        return result
