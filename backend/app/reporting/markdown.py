"""Markdown report rendering."""

from __future__ import annotations

from app.contracts.analysis import AnalysisResult, RiskState

_ICON = {
    RiskState.RED: "RED",
    RiskState.YELLOW: "YELLOW",
    RiskState.GREEN: "GREEN",
    RiskState.UNKNOWN: "UNKNOWN",
}


def render(result: AnalysisResult) -> str:
    counts = result.counts
    lines: list[str] = [
        "# Project X-Ray Report",
        "",
        f"**Analysis:** `{result.analysis_id}`  ",
        f"**Project:** {result.project_id}  ",
        f"**Change:** {result.change.get('title') or '(untitled)'}  ",
        f"**Source:** {result.change.get('source')}",
        "",
        "## 1. Result",
        "",
        f"- Overall state: **{result.overall_state.value}**",
        f"- Risk score: **{result.risk.score}/100** ({result.risk.level.value})",
        f"- Confidence: **{result.confidence:.0%}**",
        f"- RED {counts['RED']} / YELLOW {counts['YELLOW']} / "
        f"UNKNOWN {counts['UNKNOWN']} / GREEN {counts['GREEN']}",
        "",
        "## 2. Risk factors",
        "",
    ]

    if result.risk.factors:
        lines.append("| Factor | Points | Detail |")
        lines.append("| --- | ---: | --- |")
        for factor in result.risk.factors:
            lines.append(f"| {factor.factor} | {factor.points} | {factor.detail or ''} |")
    else:
        lines.append("No scoring factors were triggered.")

    lines += ["", "## 3. Blast radius", ""]
    impacted = [n for n in result.nodes if n.state is not RiskState.GREEN]
    if impacted:
        lines.append("| State | Component | Type | Impact | Hops | Rule |")
        lines.append("| --- | --- | --- | --- | ---: | --- |")
        for node in impacted:
            lines.append(
                f"| {_ICON[node.state]} | {node.name} | {node.node_type} | "
                f"{node.impact_type.value} | "
                f"{node.distance if node.distance is not None else '-'} | "
                f"`{node.rule_applied.value}` |"
            )
    else:
        lines.append("No impacted components were identified.")

    lines += ["", "## 4. Evidence chains", ""]
    for node in impacted[:15]:
        lines.append(f"### {node.name} — {node.state.value}")
        lines.append("")
        lines.append(f"- Rule applied: `{node.rule_applied.value}`")
        lines.append(f"- Parse status: `{node.parse_status.value}`")
        if node.min_edge_confidence is not None:
            lines.append(f"- Minimum edge confidence: {node.min_edge_confidence:.2f}")
        if node.path:
            lines.append("- Path:")
            for step in node.path:
                lines.append(
                    f"  - `{step.source}` --{step.relationship.value}--> `{step.target}` "
                    f"({step.rule_id} @ {step.source_file}:{step.line}, "
                    f"confidence {step.confidence:.2f})"
                )
        else:
            lines.append("- Path: none (the component is directly modified or unreachable)")
        if node.explanation:
            lines.append(f"- Explanation ({node.explanation_source}): {node.explanation}")
        lines.append("")

    lines += ["## 5. Security", ""]
    lines.append(f"- Scanner status: **{result.security.status.value}**")
    lines.append(f"- Scanners run: {', '.join(result.security.scanners_run) or 'none'}")
    lines.append(
        f"- Scanners unavailable: {', '.join(result.security.scanners_unavailable) or 'none'}"
    )
    if result.security.findings:
        lines += ["", "| Severity | Type | File | Description |", "| --- | --- | --- | --- |"]
        for finding in result.security.findings[:25]:
            lines.append(
                f"| {finding.severity.value} | {finding.type.value} | "
                f"{finding.file}:{finding.line or '-'} | {finding.description} |"
            )

    lines += ["", "## 6. Recommendations", ""]
    for item in result.explanation.recommendations or ["None generated."]:
        lines.append(f"- {item}")

    lines += ["", "## 7. Missing information", ""]
    for item in result.explanation.missing_information or ["None reported."]:
        lines.append(f"- {item}")

    lines += ["", "## 8. Notes", ""]
    for item in result.notes or ["None."]:
        lines.append(f"- {item}")

    lines += [
        "",
        "---",
        "",
        "GREEN means no impact was detected with the available evidence. It is not a "
        "guarantee of safety. UNKNOWN means the analysis could not decide and requires "
        "human review.",
        "",
        f"AI explanation backend: `{result.explanation.backend}` "
        f"(status: {result.statuses.explanation.value}).",
    ]
    return "\n".join(lines)
