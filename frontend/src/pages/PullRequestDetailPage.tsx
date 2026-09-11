import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/endpoints';
import type { ChangeResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { RiskBadge } from '../components/RiskBadge';

export default function PullRequestDetailPage() {
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
          <p className="mt-1 text-xs text-text-muted">
            Author: {change.author ?? '—'} &middot; Created {new Date(change.createdAtUtc).toLocaleString()} &middot; {change.changedFilePaths.length} file(s) changed
          </p>
        </div>
        <div className="flex gap-2">
          <Button
            variant="primary"
            onClick={async () => {
              const analysis = await api.createAnalysis(change.changeId);
              navigate(`/analyses/${analysis.analysisId}`);
            }}
          >
            Re-Run Scan
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-5">
        <Card className="col-span-2 p-4">
          <h3 className="mb-2 text-sm font-semibold">Change Summary</h3>
          <p className="text-sm text-text-secondary">{change.description ?? 'No description provided for this change.'}</p>
        </Card>
        <Card className="p-4">
          <h3 className="mb-2 text-sm font-semibold">Changed Files ({change.changedFilePaths.length})</h3>
          <ul className="space-y-1 font-mono text-xs text-text-secondary">
            {change.changedFilePaths.map((f) => (
              <li key={f} className="truncate">{f}</li>
            ))}
          </ul>
        </Card>
      </div>
    </div>
  );
}
