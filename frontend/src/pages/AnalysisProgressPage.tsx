import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/endpoints';
import { Card } from '../components/Card';
import { ProgressBar } from '../components/ProgressBar';

export default function AnalysisProgressPage() {
  const { analysisId } = useParams();
  const navigate = useNavigate();
  const [statusCode, setStatusCode] = useState('RUNNING');
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!analysisId) return;
    let cancelled = false;
    const poll = async () => {
      try {
        const [nextAnalysis, nextProgress] = await Promise.all([
          api.getAnalysis(analysisId),
          api.getAnalysisProgress(analysisId)
        ]);
        if (cancelled) return;
        setStatusCode(nextProgress.statusCode);
        if (nextProgress.statusCode === 'COMPLETED' || nextProgress.statusCode === 'FAILED' || nextAnalysis.status === 'COMPLETED' || nextAnalysis.status === 'FAILED') {
          navigate(`/analyses/${analysisId}`, { replace: true });
        }
      } catch (reason: unknown) {
        if (!cancelled) setError(reason instanceof Error ? reason.message : 'Unable to read analysis progress.');
      }
    };
    void poll();
    const interval = window.setInterval(() => void poll(), 1000);
    return () => {
      cancelled = true;
      window.clearInterval(interval);
    };
  }, [analysisId]);
  const isRunning = statusCode === 'RUNNING';

  return (
    <div className="mx-auto max-w-xl">
      {error && <div className="mb-4 rounded-md border border-risk-critical/40 bg-risk-critical/10 p-3 text-sm text-risk-critical">{error}</div>}
      <Card className="p-6">
        <div className="flex items-center gap-3">
          <span className={isRunning ? 'h-3 w-3 animate-pulse rounded-full bg-primary' : 'h-3 w-3 rounded-full bg-text-muted'} />
          <h3 className="text-sm font-semibold">{isRunning ? 'Running deterministic analysis...' : statusCode}</h3>
        </div>
        <p className="mt-3 text-sm text-text-secondary">
          The analysis runs as one synchronous operation. Results will open automatically when it finishes.
        </p>
        <ProgressBar indeterminate={isRunning} className="mt-6" />
      </Card>
    </div>
  );
}
