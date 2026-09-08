from __future__ import annotations

import logging
from pathlib import Path

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel, Field

from app.ai.backends import get_reasoner
from app.analysis.engine import analyse
from app.contracts.analysis import AnalysisResult
from app.contracts.change import ChangedFile, ChangeSet
from app.core.config import get_settings
from app.core.registry import registry
from app.reporting.markdown import render
from app.security import scanners
from app.sources.change_source import GitDiffChangeSource

logger = logging.getLogger(__name__)
router = APIRouter()


class CreateProjectRequest(BaseModel):
    id: str
    name: str
    root: str
    include: list[str] = Field(default_factory=list)
    critical: list[str] = Field(default_factory=list)


class AnalyzeRequest(BaseModel):
    project_id: str
    change: ChangeSet | None = None
    git_base: str | None = None
    git_head: str = "HEAD"
    run_security: bool = True


@router.get("/health")
def health() -> dict:
    """Must succeed with no Azure resource, no AI endpoint and no database."""
    settings = get_settings()
    return {
        "status": "ok",
        "mode": settings.xray_mode,
        "ai_backend": settings.xray_ai_backend,
        "projects": len(registry.list_projects()),
    }


@router.post("/projects")
def create_project(request: CreateProjectRequest) -> dict:
    root = Path(request.root).expanduser()
    if not root.exists():
        raise HTTPException(status_code=400, detail=f"Path does not exist: {root}")
    project = registry.create_project(
        project_id=request.id,
        name=request.name,
        root=root,
        include=request.include,
        critical=request.critical,
    )
    return {"id": project.id, "name": project.name, "root": str(project.root)}


@router.get("/projects")
def list_projects() -> list[dict]:
    return [
        {
            "id": p.id,
            "name": p.name,
            "root": str(p.root),
            "ingested": p.build is not None,
            "nodes": p.build.node_count if p.build else 0,
            "edges": p.build.edge_count if p.build else 0,
        }
        for p in registry.list_projects()
    ]


@router.post("/projects/{project_id}/ingest")
def ingest(project_id: str) -> dict:
    project = registry.get_project(project_id)
    if project is None:
        raise HTTPException(status_code=404, detail="Unknown project")
    report = registry.ingest(project)
    return {
        "files_ingested": report.files_ingested,
        "files_failed": report.files_failed,
        "parse_failure_ratio": round(report.parse_failure_ratio, 4),
        "nodes": report.node_count,
        "edges": report.edge_count,
        "unresolved_apis": report.unresolved_apis,
        "secret_hits": report.secret_hits,
    }


@router.get("/projects/{project_id}/graph")
def get_graph(project_id: str) -> dict:
    project = registry.get_project(project_id)
    if project is None:
        raise HTTPException(status_code=404, detail="Unknown project")
    return {
        "nodes": [n.model_dump(mode="json") for n in project.store.nodes()],
        "edges": [e.model_dump(mode="json") for e in project.store.edges()],
    }


@router.post("/analyze/change", response_model=AnalysisResult)
def analyze_change(request: AnalyzeRequest) -> AnalysisResult:
    project = registry.get_project(request.project_id)
    if project is None:
        raise HTTPException(status_code=404, detail="Unknown project")
    if project.build is None:
        raise HTTPException(
            status_code=409, detail="Project has not been ingested. POST /projects/{id}/ingest first."
        )

    if request.change is not None:
        change = request.change
    elif request.git_base:
        try:
            change = GitDiffChangeSource(
                project.root, request.git_base, request.git_head
            ).load()
        except Exception as exc:
            raise HTTPException(status_code=400, detail=f"git diff failed: {exc}") from exc
    else:
        raise HTTPException(
            status_code=400, detail="Provide either `change` or `git_base`."
        )

    settings = get_settings()
    security = None
    if request.run_security:
        try:
            security = scanners.run(project.root)
        except Exception:
            logger.exception("security scan failed")
            security = None

    reasoner, is_live = get_reasoner(settings)
    result = analyse(
        project_id=project.id,
        change=change,
        store=project.store,
        build=project.build,
        reasoner=reasoner,
        ai_is_live=is_live,
        security=security,
        max_depth=settings.xray_max_depth,
        confidence_threshold=settings.xray_confidence_threshold,
        parse_failure_unknown_ratio=settings.xray_parse_failure_unknown_ratio,
        repo_root=project.root,
    )
    registry.save_analysis(result)
    return result


@router.get("/analysis/{analysis_id}", response_model=AnalysisResult)
def get_analysis(analysis_id: str) -> AnalysisResult:
    result = registry.get_analysis(analysis_id)
    if result is None:
        raise HTTPException(status_code=404, detail="Unknown analysis")
    return result


@router.get("/reports/{analysis_id}")
def get_report(analysis_id: str) -> dict:
    result = registry.get_analysis(analysis_id)
    if result is None:
        raise HTTPException(status_code=404, detail="Unknown analysis")
    return {"analysis_id": analysis_id, "markdown": render(result)}


class ChangeFileInput(BaseModel):
    path: str
    added_lines: int = 0
    removed_lines: int = 0


def build_change(title: str, files: list[ChangeFileInput]) -> ChangeSet:
    return ChangeSet(
        source="manual",
        title=title,
        files=[
            ChangedFile(
                path=f.path, added_lines=f.added_lines, removed_lines=f.removed_lines
            )
            for f in files
        ],
    )
