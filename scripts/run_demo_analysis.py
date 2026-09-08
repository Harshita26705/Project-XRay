"""Run the canonical X-Ray demo analysis end to end, offline.

    python scripts/run_demo_analysis.py

Ingests demo-app, simulates the "Change payment validation" ticket, runs the
deterministic analysis plus real security scanners, and prints the report.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO_ROOT / "backend"))

from app.ai.backends import get_reasoner  # noqa: E402
from app.analysis.engine import analyse  # noqa: E402
from app.contracts.analysis import RiskState  # noqa: E402
from app.contracts.change import ChangedFile, ChangeSet  # noqa: E402
from app.core.config import get_settings  # noqa: E402
from app.graph.builder import build_graph  # noqa: E402
from app.graph.store import InMemoryGraphStore  # noqa: E402
from app.reporting.markdown import render  # noqa: E402
from app.security import scanners  # noqa: E402

DEFAULT_FILES = [
    "CgOne.Demo.Api/Services/PaymentValidator.cs",
    "CgOne.Demo.Api/Services/PaymentService.cs",
]

COLOUR = {
    RiskState.RED: "\033[91m",
    RiskState.YELLOW: "\033[93m",
    RiskState.GREEN: "\033[92m",
    RiskState.UNKNOWN: "\033[90m",
}
RESET = "\033[0m"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", default=str(REPO_ROOT / "demo-app"))
    parser.add_argument("--file", action="append", dest="files")
    parser.add_argument("--critical", action="append", default=[])
    parser.add_argument("--skip-security", action="store_true")
    parser.add_argument("--markdown", action="store_true")
    args = parser.parse_args()

    root = Path(args.root).resolve()
    settings = get_settings()

    store = InMemoryGraphStore()
    build = build_graph(root, store, critical_names=args.critical or None)
    print(
        f"Ingested {build.files_ingested} file(s) -> "
        f"{build.node_count} node(s), {build.edge_count} edge(s), "
        f"{build.files_failed} parse failure(s), {build.secret_hits} secret hit(s)"
    )

    change = ChangeSet(
        source="manual",
        title="Change payment validation",
        description="Add additional validation to the payment processing flow.",
        reference="ADO-1234",
        files=[
            ChangedFile(path=p, added_lines=12, removed_lines=3)
            for p in (args.files or DEFAULT_FILES)
        ],
    )

    security = None if args.skip_security else scanners.run(root)
    reasoner, is_live = get_reasoner(settings)

    result = analyse(
        project_id="cgone-demo",
        change=change,
        store=store,
        build=build,
        reasoner=reasoner,
        ai_is_live=is_live,
        security=security,
        max_depth=settings.xray_max_depth,
        confidence_threshold=settings.xray_confidence_threshold,
        parse_failure_unknown_ratio=settings.xray_parse_failure_unknown_ratio,
        repo_root=root,
    )

    if args.markdown:
        print(render(result))
        return 0

    counts = result.counts
    print()
    print(f"Overall: {result.overall_state.value}   "
          f"score {result.risk.score}/100   confidence {result.confidence:.0%}")
    print(f"RED {counts['RED']}  YELLOW {counts['YELLOW']}  "
          f"UNKNOWN {counts['UNKNOWN']}  GREEN {counts['GREEN']}")
    print(f"AI backend: {result.explanation.backend} "
          f"({result.statuses.explanation.value})")
    print()

    for node in result.nodes:
        colour = COLOUR[node.state]
        distance = node.distance if node.distance is not None else "-"
        print(
            f"{colour}{node.state.value:<8}{RESET} {node.name:<48} "
            f"{node.node_type:<20} hops={distance!s:<3} {node.rule_applied.value}"
        )

    print()
    print("Evidence for the deepest impacted component:")
    impacted = [n for n in result.nodes if n.path]
    if impacted:
        deepest = max(impacted, key=lambda n: n.distance or 0)
        print(f"  {deepest.name} ({deepest.state.value}, {deepest.distance} hops)")
        for step in deepest.path:
            print(
                f"    {step.source} --{step.relationship.value}--> {step.target}"
                f"   [{step.rule_id} @ {step.source_file}:{step.line}"
                f" conf={step.confidence:.2f}]"
            )

    print()
    for note in result.notes:
        print(f"note: {note}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
