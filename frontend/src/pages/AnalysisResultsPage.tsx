import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import ReactFlow, { Background, Controls, type Edge, type Node } from 'reactflow';
import 'reactflow/dist/style.css';
import { api } from '../api/endpoints';
import type { AnalysisResponse, ExplainResponse, SecurityFindingResponse } from '../api/types';
import { Card } from '../components/Card';
import { RiskBadge } from '../components/RiskBadge';
import { Button } from '../components/Button';

const RISK_COLOR: Record<string, string> = {
  CRITICAL: '#ef4444',
  RISKY: '#f59e0b',
  SAFE: '#22c55e',
  UNKNOWN: '#64748b'
};

export default function AnalysisResultsPage() {
  const { analysisId } = useParams();
  const [analysis, setAnalysis] = useState<AnalysisResponse | null>(null);
  const [findings, setFindings] = useState<SecurityFindingResponse[]>([]);
  const [explanation, setExplanation] = useState<ExplainResponse | null>(null);

  useEffect(() => {
    if (!analysisId) return;
    void api.getAnalysis(analysisId).then(setAnalysis);
    void api.getAnalysisSecurityFindings(analysisId).then(setFindings);
    void api.explain(analysisId).then(setExplanation);
  }, [analysisId]);

  const { nodes, edges } = useMemo(() => buildGraph(analysis), [analysis]);

  if (!analysis) return <div className="text-sm text-text-muted">Loading analysis...</div>;

  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between">
        <div>
          <div className="mb-1 flex items-center gap-2">
            <RiskBadge level={analysis.overallRiskState} />
            <span className="text-xs text-text-muted">Analysis #{analysis.analysisId.slice(0, 8)}</span>
          </div>
          <h1 className="text-xl font-bold">{analysis.changeTitle}</h1>
          <p className="mt-1 text-sm text-text-secondary">Identified potential breaking dependency paths on high-traffic endpoints.</p>
        </div>
        <div className="flex gap-2">
          <Button>Re-Run Scan</Button>
          <Button variant="primary">View Pull Request</Button>
        </div>
      </div>

      <div className="grid grid-cols-4 gap-4">
        <RiskCountCard label="Critical" count={analysis.criticalCount} color="critical" />
        <RiskCountCard label="Risky" count={analysis.riskyCount} color="risky" />
        <RiskCountCard label="Safe" count={analysis.safeCount} color="safe" />
        <RiskCountCard label="Unknown" count={analysis.unknownCount} color="unknown" />
      </div>

      <div className="grid grid-cols-3 gap-5">
        <Card className="col-span-2 overflow-hidden">
          <div className="border-b border-border px-4 py-3 text-xs font-semibold uppercase text-text-muted">Blast Radius Mapping</div>
          <div className="h-[420px]">
            <ReactFlow nodes={nodes} edges={edges} fitView proOptions={{ hideAttribution: true }}>
              <Background color="#1e2337" gap={20} />
              <Controls />
            </ReactFlow>
          </div>
        </Card>

        <div className="space-y-4">
          <Card className="p-4">
            <h3 className="mb-3 text-xs font-semibold uppercase text-text-muted">X-Ray Findings</h3>
            <FindingRow label="Structural Impact" value={`${analysis.criticalCount} Critical, ${analysis.riskyCount} Risky`} />
            <FindingRow label="Security Warnings" value={`${findings.length} findings`} />
            <FindingRow label="Failed Unit Tests" value="0" />
          </Card>

          <Card className="p-4">
            <h3 className="mb-2 flex items-center gap-1.5 text-xs font-semibold uppercase text-primary">
              <span>&#9889;</span> Expert Explanation
            </h3>
            <p className="text-sm text-text-secondary">{explanation?.summary ?? 'Generating explanation...'}</p>
            {explanation?.degraded && <p className="mt-2 text-[11px] text-risk-risky">AI is running in fallback mode (no Foundry endpoint configured).</p>}
          </Card>

          <Link to={`/analyses/${analysis.analysisId}/evidence`}>
            <Button variant="primary" className="w-full justify-center">View Traceable Evidence</Button>
          </Link>
        </div>
      </div>
    </div>
  );
}

function RiskCountCard({ label, count, color }: { label: string; count: number; color: 'critical' | 'risky' | 'safe' | 'unknown' }) {
  const colorClass = { critical: 'text-risk-critical', risky: 'text-risk-risky', safe: 'text-risk-safe', unknown: 'text-risk-unknown' }[color];
  return (
    <Card className="p-4 text-center">
      <p className={`text-2xl font-bold ${colorClass}`}>{count}</p>
      <p className="mt-1 text-xs text-text-muted">{label}</p>
    </Card>
  );
}

function FindingRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between border-b border-border/60 py-1.5 text-xs last:border-0">
      <span className="text-text-muted">{label}</span>
      <span className="font-medium text-text-primary">{value}</span>
    </div>
  );
}

function buildGraph(analysis: AnalysisResponse | null): { nodes: Node[]; edges: Edge[] } {
  if (!analysis) return { nodes: [], edges: [] };
  const impacted = analysis.nodes.filter((n) => n.riskState !== 'SAFE');
  const nodes: Node[] = impacted.slice(0, 20).map((n, i) => ({
    id: n.graphNodeId,
    position: { x: (i % 4) * 200, y: Math.floor(i / 4) * 120 },
    data: { label: n.displayName },
    style: {
      background: '#161b32',
      border: `2px solid ${RISK_COLOR[n.riskState] ?? '#334155'}`,
      borderRadius: 8,
      color: '#e2e8f0',
      fontSize: 11,
      padding: 8,
      width: 170
    }
  }));
  return { nodes, edges: [] };
}
