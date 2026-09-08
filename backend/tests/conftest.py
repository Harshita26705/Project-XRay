from __future__ import annotations

import sys
from pathlib import Path

import pytest

BACKEND_ROOT = Path(__file__).resolve().parents[1]
REPO_ROOT = BACKEND_ROOT.parent
if str(BACKEND_ROOT) not in sys.path:
    sys.path.insert(0, str(BACKEND_ROOT))

from app.graph.builder import build_graph  # noqa: E402
from app.graph.store import InMemoryGraphStore  # noqa: E402


@pytest.fixture(scope="session")
def demo_root() -> Path:
    root = REPO_ROOT / "demo-app"
    assert root.exists(), "demo-app must exist for the parser tests"
    return root


@pytest.fixture(scope="session")
def demo_graph(demo_root: Path):
    store = InMemoryGraphStore()
    report = build_graph(demo_root, store)
    return store, report
