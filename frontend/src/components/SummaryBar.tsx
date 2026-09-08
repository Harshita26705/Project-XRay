import type { AnalysisResult, RiskState } from '../types';

const TONE: Record<RiskState, string> = {
  RED: 'border-red-500 text-red-300',
  YELLOW: 'border-amber-500 text-amber-200',
  GREEN: 'border-green-500 text-green-300',
  UNKNOWN: 'border-slate-400 text-slate-300'
};

function Counter({ label, value, tone }: { label: string; value: number; tone: string }) {
  return (
    <div className={`rounded border bg-slate-900 px-3 py-2 text-center ${tone}`}>
      <div className="text-lg font-semibold leading-none">{value}</div>
      <div className="mt-1 text-[10px] uppercase tracking-wide opacity-80">{label}</div>
    </div>
  );
}

export default function SummaryBar({ analysis }: { analysis: AnalysisResult | null }) {
  if (!analysis) {
    return (
      <div className="border-b border-slate-800 bg-slate-900 px-4 py-3 text-sm text-slate-400">
        No analysis yet. Ingest a project and run an analysis.
      </div>
    );
  }

  const counts = analysis.nodes.reduce<Record<RiskState, number>>(
    (acc, node) => {
      acc[node.state] += 1;
      return acc;
    },
    { RED: 0, YELLOW: 0, GREEN: 0, UNKNOWN: 0 }
  );

  return (
    <div className="border-b border-slate-800 bg-slate-900 px-4 py-3">
      <div className="flex flex-wrap items-center gap-4">
        <div className={`rounded border px-3 py-2 ${TONE[analysis.overall_state]}`}>
          <div className="text-[10px] uppercase tracking-wide opacity-80">Overall</div>
          <div className="text-xl font-bold leading-none">{analysis.overall_state}</div>
        </div>
        <div className="text-sm">
          <div className="text-slate-400">
            Risk score <span className="font-semibold text-slate-100">{analysis.risk.score}/100</span>
          </div>
          <div className="text-slate-400">
            Confidence{' '}
            <span className="font-semibold text-slate-100">
              {Math.round(analysis.confidence * 100)}%
            </span>
          </div>
        </div>
        <div className="grid grid-cols-4 gap-2">
          <Counter label="Red" value={counts.RED} tone={TONE.RED} />
          <Counter label="Yellow" value={counts.YELLOW} tone={TONE.YELLOW} />
          <Counter label="Unknown" value={counts.UNKNOWN} tone={TONE.UNKNOWN} />
          <Counter label="Green" value={counts.GREEN} tone={TONE.GREEN} />
        </div>
        <div className="ml-auto max-w-md text-xs text-slate-400">
          <div>
            Security: <span className="text-slate-200">{analysis.security.status}</span>
            {analysis.security.scanners_unavailable.length > 0 &&
              ` (unavailable: ${analysis.security.scanners_unavailable.join(', ')})`}
          </div>
          <div>
            AI backend: <span className="text-slate-200">{analysis.explanation.backend}</span> —
            explanatory only
          </div>
        </div>
      </div>
      {analysis.explanation.summary && (
        <p className="mt-3 text-sm text-slate-300">{analysis.explanation.summary}</p>
      )}
    </div>
  );
}
