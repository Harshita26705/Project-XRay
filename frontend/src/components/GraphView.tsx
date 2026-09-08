import { useMemo } from 'react';
import ReactFlow, {
  Background,
  Controls,
  MiniMap,
  MarkerType,
  type Edge,
  type Node
} from 'reactflow';
import 'reactflow/dist/style.css';
import type { AnalysisResult, GraphPayload, RiskState } from '../types';

const STATE_STYLE: Record<RiskState, { bg: string; border: string }> = {
  RED: { bg: '#7f1d1d', border: '#ef4444' },
  YELLOW: { bg: '#78350f', border: '#f59e0b' },
  GREEN: { bg: '#14532d', border: '#22c55e' },
  UNKNOWN: { bg: '#334155', border: '#94a3b8' }
};

// Left-to-right architectural ordering: UI, then transport, then domain, then data.
const LAYER_ORDER = [
  'FRONTEND_COMPONENT',
  'FRONTEND_MODULE',
  'API',
  'CLASS',
  'INTERFACE',
  'SERVICE',
  'TABLE',
  'DATABASE',
  'EXTERNAL_SERVICE'
];

const COLUMN_WIDTH = 300;
const ROW_HEIGHT = 78;

interface Props {
  graph: GraphPayload;
  analysis: AnalysisResult | null;
  impactedOnly: boolean;
  selectedId: string | null;
  onSelect: (nodeId: string) => void;
}

export default function GraphView({
  graph,
  analysis,
  impactedOnly,
  selectedId,
  onSelect
}: Props) {
  const { nodes, edges } = useMemo(() => {
    const stateById = new Map<string, RiskState>();
    const distanceById = new Map<string, number | null>();
    analysis?.nodes.forEach((n) => {
      stateById.set(n.node_id, n.state);
      distanceById.set(n.node_id, n.distance);
    });

    const visible = graph.nodes.filter((n) => {
      if (!impactedOnly || !analysis) return true;
      return stateById.get(n.id) !== 'GREEN';
    });
    const visibleIds = new Set(visible.map((n) => n.id));

    const layers = new Map<number, string[]>();
    visible.forEach((n) => {
      const index = LAYER_ORDER.indexOf(n.type);
      const layer = index === -1 ? LAYER_ORDER.length : index;
      const bucket = layers.get(layer) ?? [];
      bucket.push(n.id);
      layers.set(layer, bucket);
    });
    layers.forEach((bucket) => bucket.sort());

    const position = new Map<string, { x: number; y: number }>();
    layers.forEach((bucket, layer) => {
      bucket.forEach((id, row) => {
        position.set(id, { x: layer * COLUMN_WIDTH, y: row * ROW_HEIGHT });
      });
    });

    const flowNodes: Node[] = visible.map((n) => {
      const state = (stateById.get(n.id) ?? 'UNKNOWN') as RiskState;
      const style = STATE_STYLE[state];
      const distance = distanceById.get(n.id);
      return {
        id: n.id,
        position: position.get(n.id) ?? { x: 0, y: 0 },
        data: {
          label: (
            <div className="text-left">
              <div className="text-[11px] uppercase tracking-wide opacity-70">
                {n.type.replace(/_/g, ' ')}
              </div>
              <div className="text-sm font-semibold leading-tight">{n.name}</div>
              {analysis && (
                <div className="text-[11px] opacity-80">
                  {state}
                  {distance !== null && distance !== undefined ? ` · ${distance} hop(s)` : ''}
                </div>
              )}
            </div>
          )
        },
        style: {
          background: style.bg,
          border: `2px solid ${
            selectedId === n.id ? '#ffffff' : style.border
          }`,
          borderRadius: 8,
          color: '#f8fafc',
          padding: 8,
          width: 240,
          fontSize: 12
        }
      };
    });

    const flowEdges: Edge[] = graph.edges
      .filter((e) => visibleIds.has(e.source) && visibleIds.has(e.target))
      .map((e, index) => {
        const impacted =
          analysis !== null &&
          stateById.get(e.source) !== 'GREEN' &&
          stateById.get(e.target) !== 'GREEN';
        return {
          id: `${e.source}->${e.target}-${e.relationship}-${index}`,
          source: e.source,
          target: e.target,
          label: e.relationship,
          className: impacted ? 'impacted' : undefined,
          animated: impacted,
          labelStyle: { fill: '#94a3b8', fontSize: 9 },
          labelBgStyle: { fill: '#0f172a' },
          markerEnd: { type: MarkerType.ArrowClosed, color: '#475569' }
        };
      });

    return { nodes: flowNodes, edges: flowEdges };
  }, [graph, analysis, impactedOnly, selectedId]);

  return (
    <ReactFlow
      // Remount when the rendered set changes so fitView re-runs against the
      // final container size instead of a zero-width one on first paint.
      key={`${nodes.length}-${analysis?.analysis_id ?? 'none'}-${impactedOnly}`}
      nodes={nodes}
      edges={edges}
      onNodeClick={(_, node) => onSelect(node.id)}
      fitView
      fitViewOptions={{ padding: 0.15, maxZoom: 1 }}
      minZoom={0.05}
      proOptions={{ hideAttribution: true }}
    >
      <Background color="#1e293b" gap={20} />
      <MiniMap
        className="!hidden lg:!block"
        pannable
        zoomable
        maskColor="rgba(2,6,23,0.8)"
        nodeColor={(n) => (n.style?.border as string) ?? '#475569'}
      />
      <Controls />
    </ReactFlow>
  );
}
