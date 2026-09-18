import { useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import ReactFlow, { Background, Controls, Panel, type Edge, type Node } from 'reactflow';
import 'reactflow/dist/style.css';
import { api } from '../api/endpoints';
import type { AnalysisResponse, ExplainResponse, SecurityFindingResponse } from '../api/types';
import { Card } from '../components/Card';
import { Loader } from '../components/Loader';
import { RiskBadge } from '../components/RiskBadge';
import { Button } from '../components/Button';
import { GraphLegend, GRAPH_RISK_COLORS } from '../components/GraphLegend';

export default function AnalysisResultsPage() {
  const { analysisId } = useParams();
  const [analysis, setAnalysis] = useState<AnalysisResponse | null>(null);
  const [findings, setFindings] = useState<SecurityFindingResponse[]>([]);
  const [explanation, setExplanation] = useState<ExplainResponse | null>(null);
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [focusNodeId, setFocusNodeId] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    if (!analysisId) return;
    void api.getAnalysis(analysisId).then(setAnalysis);
    void api.getAnalysisSecurityFindings(analysisId).then(setFindings);
    void api.explain(analysisId).then(setExplanation);
  }, [analysisId]);

  const { nodes, edges } = useMemo(() => buildGraph(analysis, focusNodeId), [analysis, focusNodeId]);
  const selectedNode = analysis?.nodes.find((node) => node.graphNodeId === (selectedNodeId ?? analysis.nodes[0]?.graphNodeId)) ?? analysis?.nodes[0] ?? null;

  if (!analysis) return <Loader label="Loading analysis..." fullHeight />;

  const exportAnalysis = async () => {
    setExporting(true);
    try {
      const report = await api.generateReport(analysis.analysisId);
      const blob = await api.downloadReport(report.reportId, 'pdf');
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `analysis-${analysis.analysisId.slice(0, 8)}.html`;
      anchor.click();
      URL.revokeObjectURL(url);
    } finally {
      setExporting(false);
    }
  };

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
          <Button onClick={() => void exportAnalysis()} disabled={exporting}>{exporting ? 'Exporting...' : 'Export PDF'}</Button>
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
            <ReactFlow
              nodes={nodes}
              edges={edges}
              fitView
              onNodeClick={(_, node) => setSelectedNodeId(String(node.id))}
              onNodeDoubleClick={(_, node) => setFocusNodeId(String(node.id))}
              onPaneClick={() => setFocusNodeId(null)}
              proOptions={{ hideAttribution: true }}
            >
              <Background color="#1e2337" gap={20} />
              <Controls />
              <Panel position="top-right" className="!m-3 rounded border border-border bg-card/95 px-3 py-2 text-[11px] text-text-secondary">
                {focusNodeId ? <button type="button" onClick={() => setFocusNodeId(null)} className="text-primary hover:underline">Reset focus</button> : 'Double-click a node to focus its neighborhood'}
              </Panel>
              <GraphLegend mode="analysis" />
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

          {selectedNode && (
            <Card className="p-4">
              <h3 className="mb-2 text-xs font-semibold uppercase text-text-muted">Selected Node</h3>
              <div className="space-y-2 text-sm text-text-secondary">
                <div className="flex items-center justify-between gap-3">
                  <span className="text-text-muted">Rule</span>
                  <span className="font-medium text-text-primary">{selectedNode.ruleCode ?? 'N/A'}</span>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <span className="text-text-muted">Distance</span>
                  <span className="font-medium text-text-primary">{selectedNode.distance ?? '—'}</span>
                </div>
                <div className="flex items-center justify-between gap-3">
                  <span className="text-text-muted">Confidence</span>
                  <span className="font-medium text-text-primary">{selectedNode.minPathConfidence ? `${selectedNode.minPathConfidence.toFixed(2)}` : '—'}</span>
                </div>
              </div>
              <p className="mt-3 text-sm leading-relaxed text-text-secondary">{selectedNode.recommendation ?? 'Review this node and validate the dependency path before merge.'}</p>
            </Card>
          )}

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

function buildGraph(analysis: AnalysisResponse | null, focusNodeId: string | null): { nodes: Node[]; edges: Edge[] } {
  if (!analysis) return { nodes: [], edges: [] };

  const impacted = analysis.nodes.filter((node) => node.riskState !== 'SAFE');
  const focusIds = focusNodeId
    ? new Set((analysis.edges ?? []).filter((edge) => edge.sourceNodeId === focusNodeId || edge.targetNodeId === focusNodeId).flatMap((edge) => [edge.sourceNodeId, edge.targetNodeId]))
    : null;
  const visible = focusIds ? impacted.filter((node) => focusIds.has(node.graphNodeId)) : impacted;
  const nodes: Node[] = visible.slice(0, 20).map((node, index) => ({
    id: node.graphNodeId,
    position: { x: (index % 4) * 230, y: Math.floor(index / 4) * 150 },
    data: { label: node.displayName },
    style: {
      background: '#161b32',
      border: `2px solid ${GRAPH_RISK_COLORS[node.riskState] ?? '#334155'}`,
      borderRadius: 8,
      color: '#e2e8f0',
      fontSize: 11,
      padding: 8,
      width: 180
    }
  }));

  const visibleIds = new Set(nodes.map((node) => node.id));
  const edges: Edge[] = (analysis.edges ?? []).filter((edge) => visibleIds.has(edge.sourceNodeId) && visibleIds.has(edge.targetNodeId)).map((edge) => ({
    id: `${edge.sourceNodeId}-${edge.targetNodeId}`,
    source: edge.sourceNodeId,
    target: edge.targetNodeId,
    animated: true,
    label: `${edge.edgeType} (${edge.confidence.toFixed(2)})`,
    labelStyle: { fill: '#cbd5e1', fontSize: 10 },
    style: { stroke: '#64748b', strokeWidth: 1.5 },
    markerEnd: { type: 'arrowclosed' as any, color: '#64748b' }
  } as Edge));

  return { nodes, edges };
}
