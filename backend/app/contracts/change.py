"""Change-set contracts.

A ChangeSet is whatever X-Ray was asked to analyse. It is produced by a
ChangeSource (local git diff, manual JSON, Azure DevOps PR/work item) so the
core analysis never depends on any single integration.
"""

from __future__ import annotations

from enum import Enum

from pydantic import BaseModel, Field


class ChangeKind(str, Enum):
    ADDED = "ADDED"
    MODIFIED = "MODIFIED"
    DELETED = "DELETED"
    RENAMED = "RENAMED"


class ChangedFile(BaseModel):
    path: str
    """Repository-relative path, forward slashes."""
    kind: ChangeKind = ChangeKind.MODIFIED
    added_lines: int = 0
    removed_lines: int = 0
    changed_line_numbers: list[int] = Field(default_factory=list)

    @property
    def churn(self) -> int:
        return self.added_lines + self.removed_lines


class ChangeSet(BaseModel):
    source: str
    """Which ChangeSource produced this, e.g. 'git-diff', 'manual', 'azure-devops'."""
    title: str = ""
    description: str = ""
    author: str | None = None
    reference: str | None = None
    """PR id, work item id, commit sha, or a free-form label."""
    files: list[ChangedFile] = Field(default_factory=list)

    @property
    def total_churn(self) -> int:
        return sum(f.churn for f in self.files)
