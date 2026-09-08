"""Secret detection and masking.

Runs before any content is stored, logged, indexed, or sent to a model. This is
a coarse pre-filter, not a replacement for a real secret scanner -- the real
scanner lives in app/security and produces findings; this module exists so that
X-Ray itself never becomes a secret exfiltration path.
"""

from __future__ import annotations

import re
from dataclasses import dataclass

MASK = "***REDACTED***"

_PATTERNS: list[tuple[str, re.Pattern[str]]] = [
    (
        "connection-string-password",
        re.compile(r"(?i)\b(password|pwd)\s*=\s*[^;\"'\s]{3,}"),
    ),
    (
        "assignment-secret",
        re.compile(
            r"(?i)\b(api[_-]?key|apikey|secret|client[_-]?secret|access[_-]?token|"
            r"auth[_-]?token|password|passwd|pwd|connection[_-]?string)\b"
            r"\s*[:=]\s*[\"']([^\"']{6,})[\"']"
        ),
    ),
    ("bearer-token", re.compile(r"(?i)bearer\s+[A-Za-z0-9\-._~+/]{20,}=*")),
    ("aws-access-key", re.compile(r"\bAKIA[0-9A-Z]{16}\b")),
    ("private-key-block", re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----")),
    ("jwt", re.compile(r"\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b")),
]


@dataclass
class SecretHit:
    rule: str
    line: int


def scan(text: str) -> list[SecretHit]:
    hits: list[SecretHit] = []
    for index, line in enumerate(text.splitlines(), start=1):
        for rule, pattern in _PATTERNS:
            if pattern.search(line):
                hits.append(SecretHit(rule=rule, line=index))
    return hits


def mask(text: str) -> str:
    masked = text
    for _, pattern in _PATTERNS:
        masked = pattern.sub(MASK, masked)
    return masked


def mask_line(line: str) -> str:
    return mask(line)
