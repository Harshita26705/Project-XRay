import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/endpoints';
import type { AnalysisResponse } from '../api/types';
import { Card } from '../components/Card';
import { ProgressBar } from '../components/ProgressBar';

const STAGES = [
  'Reading Azure DevOps work item and changesets',
  'Identifying affected file contexts & core targets',
  'Building multi-tiered dependency blast radius paths',
  'Checking static assemblies and credentials security',
  'Retrieving downstream project environment context',
  'Running dependency rule validation engine',
  'Generating cryptographic blast radius report'
];

export default function AnalysisProgressPage() {
  const { analysisId } = useParams();
  const navigate = useNavigate();
  const [analysis, setAnalysis] = useState<AnalysisResponse | null>(null);
  const [visibleStage, setVisibleStage] = useState(0);

  useEffect(() => {
    if (!analysisId) return;
    void api.getAnalysis(analysisId).then(setAnalysis);
  }, [analysisId]);

  useEffect(() => {
    // The engine runs synchronously on the backend, so this screen replays the pipeline visually
    // rather than polling real intermediate state — see repo memory for the background-job follow-up.
    const interval = setInterval(() => {
      setVisibleStage((s) => {
        if (s >= STAGES.length) {
          clearInterval(interval);
          if (analysisId) navigate(`/analyses/${analysisId}`, { replace: true });
          return s;
        }
        return s + 1;
      });
    }, 260);
    return () => clearInterval(interval);
  }, [analysisId, navigate]);

  const percent = Math.min(100, Math.round((visibleStage / STAGES.length) * 100));

  return (
    <div className="grid grid-cols-2 gap-6">
      <Card className="p-5">
        <h3 className="mb-4 text-sm font-semibold">Foundry Engine Pipeline</h3>
        <div className="space-y-3">
          {STAGES.map((stage, i) => (
            <div key={stage} className="flex items-center gap-3 text-sm">
              <span
                className={
                  i < visibleStage ? 'text-risk-safe' : i === visibleStage ? 'animate-pulseGlow text-primary' : 'text-text-muted'
                }
              >
                {i < visibleStage ? '\u2713' : i === visibleStage ? '\u25CF' : '\u25CB'}
              </span>
              <span className={i <= visibleStage ? 'text-text-primary' : 'text-text-muted'}>{stage}</span>
            </div>
          ))}
        </div>
      </Card>

      <Card className="p-5">
        <h3 className="mb-4 text-sm font-semibold">Engine Tracer Telemetry</h3>
        <p className="text-[11px] uppercase text-text-muted">Total Components Traced</p>
        <p className="text-lg font-semibold">
          {analysis ? analysis.criticalCount + analysis.riskyCount + analysis.safeCount + analysis.unknownCount : '—'}
        </p>
        <p className="mt-3 text-[11px] uppercase text-text-muted">Estimated Recursive Dependency Chains</p>
        <p className="text-lg font-semibold">{analysis ? analysis.riskyCount + analysis.criticalCount : '—'} traced</p>

        <div className="mt-4 rounded-md border border-border bg-page p-3 font-mono text-[11px] text-text-secondary">
          <p className="text-risk-safe">[INFO] Connecting to Foundry database...</p>
          <p className="text-risk-safe">[SUCCESS] Identified {analysis?.nodes.length ?? 0} components</p>
          <p className="text-primary">[TRACE] Tracing dependency paths...</p>
          <p className="text-risk-risky">[SCANNING] Static security taint analysis...</p>
        </div>
      </Card>

      <div className="col-span-2">
        <p className="mb-1 flex items-center justify-between text-sm">
          <span>Analyzing Change</span>
          <span className="text-primary">{percent}% Completed</span>
        </p>
        <ProgressBar percent={percent} />
      </div>
    </div>
  );
}
