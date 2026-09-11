import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/endpoints';
import type { ChangeResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { RiskBadge } from '../components/RiskBadge';

export default function WorkItemDetailPage() {
  const { changeId } = useParams();
  const navigate = useNavigate();
  const [change, setChange] = useState<ChangeResponse | null>(null);

  useEffect(() => {
    if (!changeId) return;
    void api.getChange(changeId).then(setChange);
  }, [changeId]);

  if (!change) return <div className="text-sm text-text-muted">Loading...</div>;

  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between">
        <div>
          <RiskBadge level={change.riskState} />
          <h1 className="mt-2 text-xl font-bold">{change.title}</h1>
          <p className="mt-1 text-xs text-text-muted">{change.description}</p>
        </div>
        <Button
          variant="primary"
          onClick={async () => {
            const analysis = await api.createAnalysis(change.changeId);
            navigate(`/analyses/${analysis.analysisId}`);
          }}
        >
          Predict Impact
        </Button>
      </div>

      <div className="grid grid-cols-3 gap-5">
        <Card className="col-span-2 p-4">
          <h3 className="mb-3 text-sm font-semibold">X-RAY Predicted Impact</h3>
          <p className="text-xs text-text-muted">
            This is a prediction based on file paths only — run a full analysis for verified evidence-backed results.
          </p>
          <ul className="mt-3 space-y-1 font-mono text-xs text-text-secondary">
            {change.changedFilePaths.map((f) => (
              <li key={f}>{f}</li>
            ))}
          </ul>
        </Card>
        <Card className="p-4">
          <h3 className="mb-2 text-sm font-semibold">Predicted Blast Radius</h3>
          <p className="text-xs text-text-muted">Run the full analysis to view a live dependency graph.</p>
        </Card>
      </div>
    </div>
  );
}
