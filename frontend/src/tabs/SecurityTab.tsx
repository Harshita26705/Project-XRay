import { useMemo, useState } from 'react';
import type { AnalysisResult, SecurityFinding } from '../types';

const SEVERITY_TONE: Record<string, string> = {
  CRITICAL: 'border-red-600 bg-red-950 text-red-200',
  HIGH: 'border-red-700 bg-red-950/70 text-red-300',
  MEDIUM: 'border-amber-600 bg-amber-950 text-amber-200',
  LOW: 'border-slate-600 bg-slate-800 text-slate-300'
};

function tone(severity: string) {
  return SEVERITY_TONE[severity.toUpperCase()] ?? SEVERITY_TONE.LOW;
}

export default function SecurityTab({ analysis }: { analysis: AnalysisResult | null }) {
  const [severityFilter, setSeverityFilter] = useState<string>('ALL');

  const findings = useMemo(() => analysis?.security.findings ?? [], [analysis]);
  const severities = useMemo(
    () => Array.from(new Set(findings.map((f) => f.severity.toUpperCase()))),
    [findings]
  );
  const visible = useMemo(
    () =>
      severityFilter === 'ALL'
        ? findings
        : findings.filter((f) => f.severity.toUpperCase() === severityFilter),
    [findings, severityFilter]
  );

  if (!analysis) {
    return (
      <div className="flex h-full items-center justify-center px-6 text-center text-sm text-slate-500">
        Run an X-Ray or PR analysis with security scanners enabled to see findings here.
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-6xl space-y-4 px-4 py-6 sm:px-6 lg:px-8">
      <div className="rounded-lg border border-slate-800 bg-slate-900 p-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h2 className="text-base font-semibold text-slate-100">Security findings</h2>
            <p className="mt-1 text-xs text-slate-400">
              Status: <span className="text-slate-200">{analysis.security.status}</span> ·
              Scanners run: {analysis.security.scanners_run.join(', ') || 'none'}
              {analysis.security.scanners_unavailable.length > 0 &&
                ` · Unavailable: ${analysis.security.scanners_unavailable.join(', ')}`}
            </p>
          </div>
          <div className="text-2xl font-bold text-slate-100">{findings.length}</div>
        </div>

        {severities.length > 0 && (
          <div className="mt-4 flex flex-wrap gap-2">
            <button
              type="button"
              onClick={() => setSeverityFilter('ALL')}
              className={`rounded-full border px-3 py-1 text-xs font-medium ${
                severityFilter === 'ALL'
                  ? 'border-amber-500 bg-amber-950 text-amber-200'
                  : 'border-slate-700 text-slate-400 hover:text-slate-200'
              }`}
            >
              All ({findings.length})
            </button>
            {severities.map((severity) => (
              <button
                key={severity}
                type="button"
                onClick={() => setSeverityFilter(severity)}
                className={`rounded-full border px-3 py-1 text-xs font-medium ${
                  severityFilter === severity
                    ? 'border-amber-500 bg-amber-950 text-amber-200'
                    : 'border-slate-700 text-slate-400 hover:text-slate-200'
                }`}
              >
                {severity} ({findings.filter((f) => f.severity.toUpperCase() === severity).length})
              </button>
            ))}
          </div>
        )}
      </div>

      {visible.length === 0 ? (
        <div className="rounded-lg border border-slate-800 bg-slate-900 p-5 text-sm text-slate-400">
          No security findings for the current filter.
        </div>
      ) : (
        <ul className="space-y-2">
          {visible.map((finding: SecurityFinding, index) => (
            <li
              key={index}
              className={`rounded-lg border p-4 text-sm ${tone(finding.severity)}`}
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="font-semibold">
                  {finding.severity.toUpperCase()} · {finding.type}
                </span>
                <span className="text-[11px] uppercase tracking-wide opacity-70">
                  {finding.source}
                </span>
              </div>
              <p className="mt-1 text-slate-200">{finding.description}</p>
              <p className="mt-1 font-mono text-[11px] opacity-80">
                {finding.file}
                {finding.line !== null ? `:${finding.line}` : ''}
              </p>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
