"""Live AI backends.

Both backends are optional. If the SDK is not installed or the endpoint is not
configured, `get_reasoner` falls back to MockReasoner and the analysis records
the explanation subsystem as DEGRADED -- the deterministic result is unaffected.

VERIFY BEFORE PRODUCTION USE: the exact client call surface for Microsoft
Foundry should be confirmed against the installed `azure-ai-projects` version.
This module uses the OpenAI-compatible client returned by
`AIProjectClient.get_openai_client()`, which is why a plain Azure OpenAI
endpoint can be substituted without changing any calling code.
"""

from __future__ import annotations

import json
import logging

from app.ai.reasoner import (
    RESPONSE_SCHEMA_HINT,
    SYSTEM_INSTRUCTION,
    AIReasoner,
    MockReasoner,
    build_prompt,
)
from app.contracts.analysis import AnalysisResult, Explanation
from app.core.config import Settings

logger = logging.getLogger(__name__)


def _parse(raw: str, backend: str) -> Explanation:
    payload = json.loads(raw)
    return Explanation(
        summary=str(payload.get("summary", "")),
        per_node={str(k): str(v) for k, v in (payload.get("per_node") or {}).items()},
        recommendations=[str(x) for x in (payload.get("recommendations") or [])],
        missing_information=[str(x) for x in (payload.get("missing_information") or [])],
        backend=backend,
    )


def _messages(result: AnalysisResult) -> list[dict[str, str]]:
    return [
        {"role": "system", "content": f"{SYSTEM_INSTRUCTION}\n\nSchema:\n{RESPONSE_SCHEMA_HINT}"},
        {"role": "user", "content": build_prompt(result)},
    ]


class FoundryReasoner:
    name = "foundry"

    def __init__(self, endpoint: str, deployment: str) -> None:
        from azure.ai.projects import AIProjectClient
        from azure.identity import DefaultAzureCredential

        self._deployment = deployment
        self._project = AIProjectClient(
            endpoint=endpoint, credential=DefaultAzureCredential()
        )
        self._client = self._project.get_openai_client()

    def explain(self, result: AnalysisResult) -> Explanation:
        response = self._client.chat.completions.create(
            model=self._deployment,
            messages=_messages(result),
            response_format={"type": "json_object"},
            temperature=0,
        )
        return _parse(response.choices[0].message.content or "{}", self.name)


class OpenAICompatibleReasoner:
    name = "openai_compatible"

    def __init__(self, endpoint: str, deployment: str, api_version: str) -> None:
        from azure.identity import DefaultAzureCredential, get_bearer_token_provider
        from openai import AzureOpenAI

        self._deployment = deployment
        self._client = AzureOpenAI(
            azure_endpoint=endpoint,
            api_version=api_version,
            azure_ad_token_provider=get_bearer_token_provider(
                DefaultAzureCredential(), "https://cognitiveservices.azure.com/.default"
            ),
        )

    def explain(self, result: AnalysisResult) -> Explanation:
        response = self._client.chat.completions.create(
            model=self._deployment,
            messages=_messages(result),
            response_format={"type": "json_object"},
            temperature=0,
        )
        return _parse(response.choices[0].message.content or "{}", self.name)


def get_reasoner(settings: Settings) -> tuple[AIReasoner, bool]:
    """Return (reasoner, is_live). Never raises: falls back to the mock."""
    backend = (settings.xray_ai_backend or "mock").strip().lower()
    try:
        if backend == "foundry" and settings.foundry_project_endpoint:
            return (
                FoundryReasoner(
                    settings.foundry_project_endpoint, settings.foundry_model_deployment
                ),
                True,
            )
        if backend == "openai_compatible" and settings.openai_compatible_endpoint:
            return (
                OpenAICompatibleReasoner(
                    settings.openai_compatible_endpoint,
                    settings.openai_compatible_deployment,
                    settings.openai_compatible_api_version,
                ),
                True,
            )
    except Exception:
        logger.exception("AI backend '%s' unavailable, falling back to mock", backend)
    return MockReasoner(), False
