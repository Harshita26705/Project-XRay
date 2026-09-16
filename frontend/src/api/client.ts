// Empty string means "use the Vite dev proxy" (relative /api/... requests) — see vite.config.ts.
const API_BASE = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, '') ?? '/api';

let tokenGetter: (() => Promise<string | null>) | null = null;

/** Wired once from AuthProvider at app startup so apiFetch can attach a bearer token when available. */
export function registerTokenGetter(getter: () => Promise<string | null>) {
  tokenGetter = getter;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string
  ) {
    super(message);
  }
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const token = tokenGetter ? await tokenGetter() : null;
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(init?.headers as Record<string, string> | undefined)
  };
  if (token) headers.Authorization = `Bearer ${token}`;

  const response = await fetch(`${API_BASE}${path}`, { ...init, headers });

  if (!response.ok) {
    let detail = response.statusText;
    try {
      const body = await response.json();
      detail = body.error ?? body.title ?? detail;
    } catch {
      // ignore, use statusText
    }
    throw new ApiError(response.status, detail);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const get = <T>(path: string) => apiFetch<T>(path);
export const post = <T>(path: string, body?: unknown) =>
  apiFetch<T>(path, { method: 'POST', body: body === undefined ? undefined : JSON.stringify(body) });
export const put = <T>(path: string, body?: unknown) =>
  apiFetch<T>(path, { method: 'PUT', body: body === undefined ? undefined : JSON.stringify(body) });
export const del = <T = undefined>(path: string) => apiFetch<T>(path, { method: 'DELETE' });

export async function download(path: string): Promise<Blob> {
  const token = tokenGetter ? await tokenGetter() : null;
  const headers: Record<string, string> = token ? { Authorization: `Bearer ${token}` } : {};
  const response = await fetch(`${API_BASE}${path}`, { headers });
  if (!response.ok) throw new ApiError(response.status, response.statusText);
  return response.blob();
}
