import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { api } from '../api/endpoints';
import type { EvidenceResponse } from '../api/types';
import { Card } from '../components/Card';
import { EmptyState } from '../components/States';

const BADGE_STYLE: Record<string, string> = {
  DIRECT_CHANGE: 'bg-risk-critical/15 text-risk-critical border-risk-critical/40',
  STRUCTURAL_DEPENDENCY: 'bg-risk-risky/15 text-risk-risky border-risk-risky/40',
  DATABASE_ACCESS: 'bg-primary/15 text-primary border-primary/40',
  SECURITY_ALERT: 'bg-risk-critical/15 text-risk-critical border-risk-critical/40',
  GRAPH_PATH: 'bg-risk-unknown/15 text-risk-unknown border-risk-unknown/40'
};

export default function TraceEvidencePage() {
  const { analysisId } = useParams();
  const [evidence, setEvidence] = useState<EvidenceResponse[] | null>(null);

  useEffect(() => {
    if (!analysisId) return;
    void api.getEvidence(analysisId).then(setEvidence);
  }, [analysisId]);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Trace Evidence</h1>
        <p className="mt-1 text-sm text-text-secondary">Review verifiable structural and semantic static analysis proving X-Ray's conclusions.</p>
      </div>

      {evidence && evidence.length === 0 && (
        <EmptyState title="No evidence yet" message="Evidence is generated once a change has been analyzed." />
      )}

      <div className="space-y-4">
        {evidence?.map((item) => (
          <Card key={item.evidenceId} className="p-4">
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-2">
                <span className={`rounded border px-2 py-0.5 text-[10px] font-semibold uppercase ${BADGE_STYLE[item.evidenceType] ?? 'border-border text-text-secondary'}`}>
                  {item.evidenceType.replace(/_/g, ' ')}
                </span>
                <span className="font-mono text-sm text-text-primary">{item.filePath ?? item.title}</span>
              </div>
              {item.lineStart && (
                <span className="text-[11px] text-text-muted">
                  Lines: {item.lineStart}
                  {item.lineEnd && item.lineEnd !== item.lineStart ? `-${item.lineEnd}` : ''}
                </span>
              )}
            </div>
            <p className="mt-2 text-sm text-text-secondary">{item.description ?? item.title}</p>
            {item.confidence !== null && item.confidence !== undefined && (
              <p className="mt-1 text-[11px] text-text-muted">Confidence: {(item.confidence * 100).toFixed(0)}%</p>
            )}
          </Card>
        ))}
      </div>
    </div>
  );
}
