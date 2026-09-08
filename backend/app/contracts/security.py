"""Security contracts.

Findings are produced by real scanners only. The AI reasoner may interpret
these, never author them.
"""

from __future__ import annotations

from enum import Enum

from pydantic import BaseModel, Field


class Severity(str, Enum):
    CRITICAL = "CRITICAL"
    HIGH = "HIGH"
    MEDIUM = "MEDIUM"
    LOW = "LOW"
    INFO = "INFO"


_SEVERITY_ORDER = {
    Severity.INFO: 0,
    Severity.LOW: 1,
    Severity.MEDIUM: 2,
    Severity.HIGH: 3,
    Severity.CRITICAL: 4,
}


def severity_rank(severity: Severity) -> int:
    return _SEVERITY_ORDER[severity]


class FindingType(str, Enum):
    SECRET = "SECRET"
    SAST = "SAST"
    DEPENDENCY = "DEPENDENCY"
    IAC = "IAC"


class ScannerStatus(str, Enum):
    OK = "OK"
    UNAVAILABLE = "UNAVAILABLE"
    FAILED = "FAILED"


class SecurityFinding(BaseModel):
    severity: Severity
    type: FindingType
    file: str
    line: int | None = None
    description: str
    source: str
    """Name of the scanner that produced this, e.g. 'detect-secrets'."""
    node_id: str | None = None
    """Graph node this finding is attributed to, when it could be mapped."""


class SecurityReport(BaseModel):
    status: ScannerStatus = ScannerStatus.UNAVAILABLE
    findings: list[SecurityFinding] = Field(default_factory=list)
    scanners_run: list[str] = Field(default_factory=list)
    scanners_unavailable: list[str] = Field(default_factory=list)

    @property
    def usable(self) -> bool:
        return self.status is ScannerStatus.OK
