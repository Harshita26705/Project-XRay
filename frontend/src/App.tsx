import { useMemo, useState } from 'react';
import ControlPanel from './components/ControlPanel';
import GraphView from './components/GraphView';
import NodeDetails from './components/NodeDetails';
import SummaryBar from './components/SummaryBar';
import type { AnalysisResult, GraphPayload } from './types';

const EMPTY_GRAPH: GraphPayload = { nodes: [], edges: [] };

export default function App() {
  const [graph, setGraph] = useState<GraphPayload>(EMPTY_GRAPH);
  const [analysis, setAnalysis] = useState<AnalysisResult | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [impactedOnly, setImpactedOnly] = useState(true);

  const selected = useMemo(
    () => analysis?.nodes.find((node) => node.node_id === selectedId) ?? null,
    [analysis, selectedId]
  );

  return (
    <div className="flex h-full flex-col">
      <header className="flex items-baseline gap-3 border-b border-slate-800 bg-slate-900 px-4 py-3">
        <h1 className="text-lg font-bold tracking-tight">PROJECT X-RAY</h1>
        <span className="text-xs text-slate-400">
          Change impact analysis — the dependency graph is authoritative
        </span>
      </header>

      <ControlPanel
        onGraph={(next) => {
          setGraph(next);
          setAnalysis(null);
          setSelectedId(null);
        }}
        onAnalysis={setAnalysis}
        impactedOnly={impactedOnly}
        onImpactedOnly={setImpactedOnly}
      />

      <SummaryBar analysis={analysis} />

      <div className="flex min-h-0 flex-1">
        <main className="min-w-0 flex-1 bg-slate-950">
          {graph.nodes.length === 0 ? (
            <div className="flex h-full items-center justify-center text-sm text-slate-500">
              Ingest a repository to build the dependency graph.
            </div>
          ) : (
            <GraphView
              graph={graph}
              analysis={analysis}
              impactedOnly={impactedOnly}
              selectedId={selectedId}
              onSelect={setSelectedId}
            />
          )}
        </main>

        <aside className="flex w-[26rem] shrink-0 flex-col overflow-y-auto border-l border-slate-800 bg-slate-900">
          <NodeDetails node={selected} />
          {analysis && (
            <div className="mt-auto space-y-3 border-t border-slate-800 p-4 text-xs">
              <div>
                <h3 className="mb-1 font-semibold uppercase tracking-wide text-slate-400">
                  Risk factors
                </h3>
                <ul className="space-y-1">
                  {analysis.risk.factors.map((factor) => (
                    <li key={factor.factor} className="flex justify-between gap-2">
                      <span className="text-slate-300">{factor.factor}</span>
                      <span className="font-mono text-slate-400">+{factor.points}</span>
                    </li>
                  ))}
                </ul>
              </div>
              {analysis.explanation.recommendations.length > 0 && (
                <div>
                  <h3 className="mb-1 font-semibold uppercase tracking-wide text-slate-400">
                    Recommendations
                  </h3>
                  <ul className="list-disc space-y-1 pl-4 text-slate-300">
                    {analysis.explanation.recommendations.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                </div>
              )}
              {analysis.explanation.missing_information.length > 0 && (
                <div>
                  <h3 className="mb-1 font-semibold uppercase tracking-wide text-slate-400">
                    Missing information
                  </h3>
                  <ul className="list-disc space-y-1 pl-4 text-amber-300">
                    {analysis.explanation.missing_information.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                </div>
              )}
            </div>
          )}
        </aside>
      </div>
    </div>
  );
}
