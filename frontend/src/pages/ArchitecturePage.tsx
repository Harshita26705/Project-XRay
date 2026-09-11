import { useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import ReactFlow, { Background, Controls, type Edge, type Node } from 'reactflow';
import 'reactflow/dist/style.css';
import clsx from 'clsx';
import { api } from '../api/endpoints';
import type { GraphResponse } from '../api/types';

const LAYER_ORDER = [
  'FRONTEND_COMPONENT', 'FRONTEND_MODULE', 'FRONTEND',
  'API',
  'CONTROLLER',
  'SERVICE', 'INTERFACE',
  'REPOSITORY',
  'DATABASE_TABLE', 'DATABASE', 'STORED_PROCEDURE',
  'EXTERNAL', 'UNKNOWN'
];

const TYPE_COLORS: Record<string, string> = {
  FRONTEND_COMPONENT: '#3b82f6',
  FRONTEND_MODULE: '#60a5fa',
  API: '#f59e0b',
  CONTROLLER: '#ef4444',
  SERVICE: '#22c55e',
  INTERFACE: '#a78bfa',
  REPOSITORY: '#f97316',
  DATABASE_TABLE: '#38bdf8',
  EXTERNAL: '#64748b'
};

// Node filter pills map to one or more ComponentType codes (see XRay.Parsers.ComponentTypeCodes).
const NODE_FILTERS: Record<string, string[]> = {
  Frontend: ['FRONTEND', 'FRONTEND_MODULE', 'FRONTEND_COMPONENT'],
  API: ['API'],
  Controllers: ['CONTROLLER'],
  Services: ['SERVICE', 'INTERFACE'],
  Repositories: ['REPOSITORY'],
  Database: ['DATABASE', 'DATABASE_TABLE', 'DATABASE_COLUMN', 'STORED_PROCEDURE'],
  External: ['EXTERNAL', 'UNKNOWN']
};

// Edge filter pills map to GraphEdgeType codes; edge types not listed here (EXPOSES, INHERITS,
// REFERENCES, USES, BINDS) are structural backbone and always shown regardless of edge filter state.
const EDGE_FILTERS: Record<string, string> = {
  Calls: 'CALLS',
  'Depends on': 'DEPENDS_ON',
  Imports: 'IMPORTS',
  Reads: 'READS',
  Writes: 'WRITES'
};

export default function ArchitecturePage() {
  const { projectId } = useParams();
  const [graph, setGraph] = useState<GraphResponse | null>(null);
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [activeNodeFilters, setActiveNodeFilters] = useState(() => new Set(Object.keys(NODE_FILTERS)));
  const [activeEdgeFilters, setActiveEdgeFilters] = useState(() => new Set(Object.keys(EDGE_FILTERS)));

  useEffect(() => {
    if (!projectId) return;
    void api.getGraph(projectId).then(setGraph);
  }, [projectId]);

  const toggleNodeFilter = (label: string) => {
    setActiveNodeFilters((prev) => {
      const next = new Set(prev);
      if (next.has(label)) next.delete(label);
      else next.add(label);
      return next;
    });
  };

  const toggleEdgeFilter = (label: string) => {
    setActiveEdgeFilters((prev) => {
      const next = new Set(prev);
      if (next.has(label)) next.delete(label);
      else next.add(label);
      return next;
    });
  };

  const filteredGraph = useMemo(
    () => applyFilters(graph, activeNodeFilters, activeEdgeFilters, search),
    [graph, activeNodeFilters, activeEdgeFilters, search]
  );

  const { nodes, edges } = useMemo(() => buildLayout(filteredGraph), [filteredGraph]);
  const selectedNode = graph?.nodes.find((n) => n.nodeId === selectedNodeId);

  return (
    <div className="flex h-full flex-col gap-4">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold">CGOne Architecture</h1>
          <p className="mt-1 text-sm text-text-secondary">Explore dependencies across frontend, backend and database schemas.</p>
        </div>
        <div className="flex items-center gap-3 text-xs">
          <Legend color="#ef4444" label="Critical" />
          <Legend color="#f59e0b" label="Risky" />
          <Legend color="#22c55e" label="Safe" />
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-3 rounded-lg border border-border bg-card px-4 py-2 text-xs">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search nodes..."
          className="w-48 rounded border border-border bg-cardMuted px-2 py-1 text-xs placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-primary"
        />
        <span className="text-text-muted">NODES:</span>
        {Object.keys(NODE_FILTERS).map((label) => (
          <FilterPill key={label} label={label} active={activeNodeFilters.has(label)} onClick={() => toggleNodeFilter(label)} />
        ))}
        <span className="ml-4 text-text-muted">EDGES:</span>
        {Object.keys(EDGE_FILTERS).map((label) => (
          <FilterPill key={label} label={label} active={activeEdgeFilters.has(label)} onClick={() => toggleEdgeFilter(label)} />
        ))}
      </div>

      <div className="flex flex-1 gap-4">
        <div className="flex-1 overflow-hidden rounded-lg border border-border bg-card">
          {!graph || graph.nodes.length === 0 ? (
            <div className="flex h-full items-center justify-center text-sm text-text-muted">
              No graph yet — ingest this project from the Projects page.
            </div>
          ) : nodes.length === 0 ? (
            <div className="flex h-full items-center justify-center text-sm text-text-muted">
              No nodes match the current filters/search.
            </div>
          ) : (
            <ReactFlow
              nodes={nodes}
              edges={edges}
              onNodeClick={(_, node) => setSelectedNodeId(node.id)}
              fitView
              proOptions={{ hideAttribution: true }}
            >
              <Background color="#1e2337" gap={20} />
              <Controls />
            </ReactFlow>
          )}
        </div>

        {selectedNode && (
          <div className="w-72 shrink-0 rounded-lg border border-border bg-card p-4 text-sm">
            <h3 className="font-semibold">{selectedNode.displayName}</h3>
            <p className="mt-1 text-xs uppercase tracking-wide text-text-muted">{selectedNode.componentType}</p>
            {selectedNode.filePath && <p className="mt-3 break-all font-mono text-[11px] text-text-secondary">{selectedNode.filePath}</p>}
          </div>
        )}
      </div>
    </div>
  );
}

function FilterPill({ label, active, onClick }: { label: string; active: boolean; onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className={clsx(
        'rounded-full border px-2 py-0.5 transition-colors',
        active ? 'border-primary/50 bg-primary/15 text-primary' : 'border-border text-text-muted hover:text-text-secondary'
      )}
    >
      {label}
    </button>
  );
}

function Legend({ color, label }: { color: string; label: string }) {
  return (
    <span className="flex items-center gap-1.5">
      <span className="h-2 w-2 rounded-full" style={{ backgroundColor: color }} />
      {label}
    </span>
  );
}

function applyFilters(
  graph: GraphResponse | null,
  activeNodeFilters: Set<string>,
  activeEdgeFilters: Set<string>,
  search: string
): GraphResponse | null {
  if (!graph) return null;

  const allowedNodeTypes = new Set(Object.entries(NODE_FILTERS).filter(([label]) => activeNodeFilters.has(label)).flatMap(([, codes]) => codes));
  const allowedEdgeTypes = new Set(Object.entries(EDGE_FILTERS).filter(([label]) => activeEdgeFilters.has(label)).map(([, code]) => code));
  const alwaysShownEdgeTypes = new Set(['EXPOSES', 'INHERITS', 'REFERENCES', 'USES', 'BINDS']);
  const query = search.trim().toLowerCase();

  const nodes = graph.nodes.filter((n) => {
    if (!allowedNodeTypes.has(n.componentType)) return false;
    if (query && !n.displayName.toLowerCase().includes(query) && !n.filePath?.toLowerCase().includes(query)) return false;
    return true;
  });
  const nodeIds = new Set(nodes.map((n) => n.nodeId));

  const edges = graph.edges.filter((e) => {
    if (!nodeIds.has(e.sourceNodeId) || !nodeIds.has(e.targetNodeId)) return false;
    return alwaysShownEdgeTypes.has(e.edgeType) || allowedEdgeTypes.has(e.edgeType);
  });

  return { snapshotId: graph.snapshotId, nodes, edges };
}

function buildLayout(graph: GraphResponse | null): { nodes: Node[]; edges: Edge[] } {
  if (!graph) return { nodes: [], edges: [] };

  const layerIndex = (type: string) => {
    const idx = LAYER_ORDER.indexOf(type);
    return idx === -1 ? LAYER_ORDER.length : idx;
  };

  const byLayer = new Map<number, typeof graph.nodes>();
  for (const node of graph.nodes) {
    const layer = layerIndex(node.componentType);
    if (!byLayer.has(layer)) byLayer.set(layer, []);
    byLayer.get(layer)!.push(node);
  }

  const nodes: Node[] = [];
  const sortedLayers = [...byLayer.keys()].sort((a, b) => a - b);
  sortedLayers.forEach((layer, layerIdx) => {
    const items = byLayer.get(layer)!;
    items.forEach((n, i) => {
      nodes.push({
        id: n.nodeId,
        position: { x: i * 220, y: layerIdx * 130 },
        data: { label: n.displayName },
        style: {
          background: '#161b32',
          border: `1px solid ${TYPE_COLORS[n.componentType] ?? '#334155'}`,
          borderRadius: 8,
          color: '#e2e8f0',
          fontSize: 11,
          padding: 8,
          width: 180
        }
      });
    });
  });

  const edges: Edge[] = graph.edges.map((e) => ({
    id: e.edgeId,
    source: e.sourceNodeId,
    target: e.targetNodeId,
    label: e.edgeType,
    animated: e.isRuntimeResolved,
    style: { stroke: '#384157' },
    labelStyle: { fill: '#64748b', fontSize: 9 }
  }));

  return { nodes, edges };
}
