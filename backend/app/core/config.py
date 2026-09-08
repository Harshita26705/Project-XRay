from __future__ import annotations

from functools import lru_cache
from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict

BACKEND_ROOT = Path(__file__).resolve().parents[2]
REPO_ROOT = BACKEND_ROOT.parent


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=BACKEND_ROOT / ".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    xray_mode: str = "demo"
    xray_ai_backend: str = "mock"

    xray_max_depth: int = 6
    xray_confidence_threshold: float = 0.70
    xray_parse_failure_unknown_ratio: float = 0.20

    foundry_project_endpoint: str = ""
    foundry_model_deployment: str = ""

    openai_compatible_endpoint: str = ""
    openai_compatible_deployment: str = ""
    openai_compatible_api_version: str = ""

    azure_devops_org_url: str = ""
    azure_devops_project: str = ""
    azure_devops_repository: str = ""

    azure_search_endpoint: str = ""
    azure_search_index_name: str = ""

    sqlserver_connection_string: str = ""
    teams_webhook_url: str = ""

    state_dir: Path = BACKEND_ROOT / ".xray"


@lru_cache
def get_settings() -> Settings:
    settings = Settings()
    settings.state_dir.mkdir(parents=True, exist_ok=True)
    return settings
