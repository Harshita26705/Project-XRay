"""In-process project and analysis registry.

Deliberately simple: the MVP keeps one graph per project in memory. Swapping to
persistent storage means replacing the GraphStore implementation here, not
changing any caller.
"""

from __future__ import annotations

import threading
from dataclasses import dataclass, field
from pathlib import Path

from app.contracts.analysis import AnalysisResult
from app.graph.builder import BuildReport, build_graph
from app.graph.store import GraphStore, InMemoryGraphStore


@dataclass
class Project:
    id: str
    name: str
    root: Path
    include: list[str] = field(default_factory=list)
    critical: list[str] = field(default_factory=list)
    store: GraphStore = field(default_factory=InMemoryGraphStore)
    build: BuildReport | None = None


class Registry:
    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._projects: dict[str, Project] = {}
        self._analyses: dict[str, AnalysisResult] = {}

    def create_project(
        self,
        project_id: str,
        name: str,
        root: Path,
        include: list[str] | None = None,
        critical: list[str] | None = None,
    ) -> Project:
        with self._lock:
            project = Project(
                id=project_id,
                name=name,
                root=root,
                include=include or [],
                critical=critical or [],
            )
            self._projects[project_id] = project
            return project

    def get_project(self, project_id: str) -> Project | None:
        return self._projects.get(project_id)

    def list_projects(self) -> list[Project]:
        return list(self._projects.values())

    def ingest(self, project: Project) -> BuildReport:
        with self._lock:
            report = build_graph(
                root=project.root,
                store=project.store,
                include=project.include or None,
                critical_names=project.critical or None,
            )
            project.build = report
            return report

    def save_analysis(self, result: AnalysisResult) -> None:
        with self._lock:
            self._analyses[result.analysis_id] = result

    def get_analysis(self, analysis_id: str) -> AnalysisResult | None:
        return self._analyses.get(analysis_id)


registry = Registry()
