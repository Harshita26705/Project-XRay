import { Panel } from 'reactflow';

export const GRAPH_TYPE_COLORS: Record<string, string> = {
  FRONTEND_COMPONENT: '#3b82f6',
  FRONTEND_MODULE: '#60a5fa',
  API: '#f59e0b',
  CONTROLLER: '#ef4444',
  SERVICE: '#22c55e',
  INTERFACE: '#a78bfa',
  REPOSITORY: '#f97316',
  DATABASE_TABLE: '#38bdf8',
  EXTERNAL: '#64748b'
};

export const GRAPH_RISK_COLORS: Record<string, string> = {
  CRITICAL: '#ef4444',
  RISKY: '#f59e0b',
  SAFE: '#22c55e',
  UNKNOWN: '#64748b'
};

const TYPE_LABELS: Record<string, string> = {
  FRONTEND_COMPONENT: 'Frontend component',
  FRONTEND_MODULE: 'Frontend module',
  API: 'API',
  CONTROLLER: 'Controller',
  SERVICE: 'Service',
  INTERFACE: 'Interface',
  REPOSITORY: 'Repository',
  DATABASE_TABLE: 'Database table',
  EXTERNAL: 'External'
};

export function GraphLegend({ mode }: { mode: 'architecture' | 'analysis' }) {
  const entries = mode === 'architecture'
    ? Object.entries(GRAPH_TYPE_COLORS).map(([key, color]) => ({ label: TYPE_LABELS[key] ?? key, color }))
    : Object.entries(GRAPH_RISK_COLORS).map(([key, color]) => ({ label: key[0] + key.slice(1).toLowerCase(), color }));

  return (
    <Panel position="bottom-left" className="!m-3 rounded border border-border bg-card/95 px-3 py-2 shadow-lg">
      <div className="mb-1 text-[10px] font-semibold uppercase tracking-wide text-text-muted">
        {mode === 'architecture' ? 'Node types' : 'Impact risk'}
      </div>
      <div className="grid grid-cols-2 gap-x-3 gap-y-1">
        {entries.map((entry) => (
          <span key={entry.label} className="flex items-center gap-1.5 text-[10px] text-text-secondary">
            <span className="h-2 w-2 rounded-full" style={{ backgroundColor: entry.color }} />
            {entry.label}
          </span>
        ))}
      </div>
    </Panel>
  );
}
