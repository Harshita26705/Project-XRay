"""Security evidence.

Real scanners only. Each scanner is independent and any of them may be
unavailable; unavailability produces UNKNOWN, never an implied "safe".
"""

from __future__ import annotations

import json
import logging
import shutil
import subprocess
from pathlib import Path

from app.contracts.security import (
    FindingType,
    ScannerStatus,
    SecurityFinding,
    SecurityReport,
    Severity,
)
from app.ingestion import secrets
from app.ingestion.walker import walk_repository

logger = logging.getLogger(__name__)

SUBPROCESS_TIMEOUT = 180


def _severity_from_npm(key: str) -> Severity:
    return {
        "critical": Severity.CRITICAL,
        "high": Severity.HIGH,
        "moderate": Severity.MEDIUM,
        "low": Severity.LOW,
        "info": Severity.INFO,
    }.get(key.lower(), Severity.LOW)


def scan_secrets(root: Path, scoped_files: list[str] | None = None) -> list[SecurityFinding]:
    """Built-in secret detection. Always available, so it never returns UNKNOWN."""
    findings: list[SecurityFinding] = []
    for source in walk_repository(root):
        if scoped_files is not None and source.path not in scoped_files:
            continue
        for hit in secrets.scan(source.text):
            findings.append(
                SecurityFinding(
                    severity=Severity.HIGH,
                    type=FindingType.SECRET,
                    file=source.path,
                    line=hit.line,
                    description=f"Possible hard-coded secret ({hit.rule}). Value redacted.",
                    source="xray-secret-scan",
                )
            )
    return findings


def scan_npm(root: Path) -> tuple[list[SecurityFinding], bool]:
    package_dirs = [p.parent for p in root.rglob("package.json") if "node_modules" not in p.parts]
    if not package_dirs or shutil.which("npm") is None:
        return [], False
    findings: list[SecurityFinding] = []
    ran = False
    for directory in package_dirs:
        if not (directory / "node_modules").exists():
            continue  # `npm audit` without an install tree is not meaningful
        try:
            result = subprocess.run(
                ["npm", "audit", "--json"],
                cwd=directory,
                capture_output=True,
                text=True,
                timeout=SUBPROCESS_TIMEOUT,
                shell=False,
            )
            payload = json.loads(result.stdout or "{}")
        except (OSError, subprocess.SubprocessError, json.JSONDecodeError):
            logger.warning("npm audit unavailable in %s", directory)
            continue
        ran = True
        for name, advisory in (payload.get("vulnerabilities") or {}).items():
            findings.append(
                SecurityFinding(
                    severity=_severity_from_npm(str(advisory.get("severity", "low"))),
                    type=FindingType.DEPENDENCY,
                    file=str((directory / "package.json").relative_to(root)).replace("\\", "/"),
                    description=f"Vulnerable npm dependency: {name}",
                    source="npm-audit",
                )
            )
    return findings, ran


def scan_dotnet(root: Path) -> tuple[list[SecurityFinding], bool]:
    projects = [p for p in root.rglob("*.csproj") if "obj" not in p.parts]
    if not projects or shutil.which("dotnet") is None:
        return [], False
    findings: list[SecurityFinding] = []
    ran = False
    for project in projects:
        try:
            result = subprocess.run(
                [
                    "dotnet",
                    "list",
                    str(project),
                    "package",
                    "--vulnerable",
                    "--include-transitive",
                ],
                capture_output=True,
                text=True,
                timeout=SUBPROCESS_TIMEOUT,
                shell=False,
            )
        except (OSError, subprocess.SubprocessError):
            logger.warning("dotnet list package unavailable for %s", project)
            continue
        if result.returncode != 0:
            continue
        ran = True
        rel = str(project.relative_to(root)).replace("\\", "/")
        for line in result.stdout.splitlines():
            stripped = line.strip()
            if not stripped.startswith(">"):
                continue
            parts = stripped.split()
            severity = Severity.MEDIUM
            for token in parts:
                lowered = token.lower()
                if lowered in {"critical", "high", "moderate", "low"}:
                    severity = _severity_from_npm(lowered)
            findings.append(
                SecurityFinding(
                    severity=severity,
                    type=FindingType.DEPENDENCY,
                    file=rel,
                    description=f"Vulnerable NuGet package: {' '.join(parts[1:3])}",
                    source="dotnet-list-package",
                )
            )
    return findings, ran


def run(root: Path, scoped_files: list[str] | None = None) -> SecurityReport:
    report = SecurityReport()

    report.findings.extend(scan_secrets(root, scoped_files))
    report.scanners_run.append("xray-secret-scan")

    npm_findings, npm_ran = scan_npm(root)
    report.findings.extend(npm_findings)
    (report.scanners_run if npm_ran else report.scanners_unavailable).append("npm-audit")

    dotnet_findings, dotnet_ran = scan_dotnet(root)
    report.findings.extend(dotnet_findings)
    (report.scanners_run if dotnet_ran else report.scanners_unavailable).append(
        "dotnet-list-package"
    )

    # The built-in secret scan always runs, so the report is usable; the
    # unavailable scanners are still listed so confidence can be reduced and the
    # gap is visible to the user.
    report.status = ScannerStatus.OK if report.scanners_run else ScannerStatus.UNAVAILABLE
    return report


def attribute(report: SecurityReport, file_to_nodes: dict[str, list[str]]) -> dict[str, list[SecurityFinding]]:
    by_node: dict[str, list[SecurityFinding]] = {}
    for finding in report.findings:
        for node_id in file_to_nodes.get(finding.file, []):
            finding.node_id = node_id
            by_node.setdefault(node_id, []).append(finding)
    return by_node
