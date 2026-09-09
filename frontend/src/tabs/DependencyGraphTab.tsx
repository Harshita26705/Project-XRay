import GraphView from '../components/GraphView';
import NodeDetails from '../components/NodeDetails';
import type { AnalysisResult, GraphPayload } from '../types';

interface Props {
  graph: GraphPayload;
  analysis: AnalysisResult | null;
  impactedOnly: boolean;
  onImpactedOnly: (value: boolean) => void;
  selectedId: string | null;
  onSelect: (id: string) => void;
}

function GraphNodeSummary({ graph, nodeId }: { graph: GraphPayload; nodeId: string }) {
  const node = graph.nodes.find((n) => n.id === nodeId);
  if (!node) return null;
  const outgoing = graph.edges.filter((e) => e.source === nodeId);
  const incoming = graph.edges.filter((e) => e.target === nodeId);

  return (
    <div className="space-y-4 p-4 text-sm">
      <div>
        <h2 className="text-lg font-semibold">{node.name}</h2>
        <p className="text-xs text-slate-400">{node.type.replace(/_/g, ' ')}</p>
      </div>
      <p className="text-xs text-slate-400">
        Run an X-Ray or PR analysis to see this component's risk state and evidence chain.
      </p>
      <section>
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
          Depends on ({outgoing.length})
        </h3>
        {outgoing.length === 0 ? (
          <p className="text-xs text-slate-500">None.</p>
        ) : (
          <ul className="space-y-1 font-mono text-[11px] text-slate-300">
            {outgoing.map((edge, index) => (
              <li key={index}>
                <span className="text-amber-400">{edge.relationship}</span>{' '}
                {graph.nodes.find((n) => n.id === edge.target)?.name ?? edge.target}
              </li>
            ))}
          </ul>
        )}
      </section>
      <section>
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
          Depended on by ({incoming.length})
        </h3>
        {incoming.length === 0 ? (
          <p className="text-xs text-slate-500">None.</p>
        ) : (
          <ul className="space-y-1 font-mono text-[11px] text-slate-300">
            {incoming.map((edge, index) => (
              <li key={index}>
                <span className="text-amber-400">{edge.relationship}</span>{' '}
                {graph.nodes.find((n) => n.id === edge.source)?.name ?? edge.source}
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}

export default function DependencyGraphTab({
  graph,
  analysis,
  impactedOnly,
  onImpactedOnly,
  selectedId,
  onSelect
}: Props) {
  const selected = analysis?.nodes.find((node) => node.node_id === selectedId) ?? null;

  if (graph.nodes.length === 0) {
    return (
      <div className="flex h-full items-center justify-center px-6 text-center text-sm text-slate-500">
        Ingest a repository from the Set Up tab to build the dependency graph.
      </div>
    );
  }

  return (
    <div className="flex h-full min-h-0 flex-col lg:flex-row">
      <div className="flex min-h-[20rem] flex-1 flex-col">
        <div className="flex flex-wrap items-center gap-3 border-b border-slate-800 bg-slate-900/60 px-4 py-2">
          <span className="text-xs uppercase tracking-wide text-slate-500">
            {graph.nodes.length} components · {graph.edges.length} relationships
          </span>
          <label className="ml-auto flex items-center gap-1.5 text-xs text-slate-300">
            <input
              type="checkbox"
              checked={impactedOnly}
              onChange={(event) => onImpactedOnly(event.target.checked)}
              disabled={!analysis}
            />
            Impacted components only
          </label>
        </div>
        <div className="min-h-[16rem] flex-1 bg-slate-950">
          <GraphView
            graph={graph}
            analysis={analysis}
            impactedOnly={impactedOnly}
            selectedId={selectedId}
            onSelect={onSelect}
          />
        </div>
      </div>

      <aside className="flex max-h-56 shrink-0 flex-col overflow-y-auto border-t border-slate-800 bg-slate-900 lg:max-h-none lg:h-auto lg:w-96 lg:border-l lg:border-t-0">
        {selected ? (
          <NodeDetails node={selected} />
        ) : selectedId ? (
          <GraphNodeSummary graph={graph} nodeId={selectedId} />
        ) : (
          <NodeDetails node={null} />
        )}
      </aside>
    </div>
  );
}
