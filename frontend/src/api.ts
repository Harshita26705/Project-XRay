import type { AnalysisResult, GraphPayload } from './types';

const BASE = '/api';

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE}${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...(init?.headers ?? {}) }
  });
  if (!response.ok) {
    const detail = await response.text();
    throw new Error(`${response.status}: ${detail}`);
  }
  return response.json() as Promise<T>;
}

export function health() {
  return request<{ status: string; mode: string; ai_backend: string }>('/health');
}

export function createProject(payload: {
  id: string;
  name: string;
  root: string;
  critical?: string[];
}) {
  return request<{ id: string }>('/projects', {
    method: 'POST',
    body: JSON.stringify({ ...payload, include: [], critical: payload.critical ?? [] })
  });
}

export function ingest(projectId: string) {
  return request<{
    files_ingested: number;
    files_failed: number;
    nodes: number;
    edges: number;
    secret_hits: number;
    unresolved_apis: string[];
  }>(`/projects/${projectId}/ingest`, { method: 'POST' });
}

export function getGraph(projectId: string) {
  return request<GraphPayload>(`/projects/${projectId}/graph`);
}

export function analyze(payload: {
  project_id: string;
  change: {
    source: string;
    title: string;
    description?: string;
    reference?: string;
    author?: string;
    files: { path: string; added_lines: number; removed_lines: number }[];
  };
  run_security: boolean;
}) {
  return request<AnalysisResult>('/analyze/change', {
    method: 'POST',
    body: JSON.stringify(payload)
  });
}

export function analyzePr(payload: {
  project_id: string;
  title?: string;
  description?: string;
  reference?: string;
  author?: string;
  diff_text: string;
  run_security: boolean;
}) {
  return request<AnalysisResult>('/analyze/pr', {
    method: 'POST',
    body: JSON.stringify(payload)
  });
}

export function getReport(analysisId: string) {
  return request<{ markdown: string }>(`/reports/${analysisId}`);
}
