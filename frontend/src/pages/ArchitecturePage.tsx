import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useParams, useSearchParams } from 'react-router-dom';
import ReactFlow, { Background, Controls, MiniMap, Panel, type Edge, type Node } from 'reactflow';
import 'reactflow/dist/style.css';
import clsx from 'clsx';
import dagre from 'dagre';
import { api } from '../api/endpoints';
import type { GraphResponse, ProjectNodeDetailResponse } from '../api/types';
import { Loader } from '../components/Loader';
import { useProjects } from '../state/ProjectContext';
import { GraphLegend, GRAPH_TYPE_COLORS } from '../components/GraphLegend';

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
  const [searchParams] = useSearchParams();
  const { currentBranch } = useProjects();
  const [selectedNodeId, setSelectedNodeId] = useState<string | null>(null);
  const [search, setSearch] = useState(() => searchParams.get('q') ?? '');
  const [activeNodeFilters, setActiveNodeFilters] = useState(() => new Set(Object.keys(NODE_FILTERS)));
  const [activeEdgeFilters, setActiveEdgeFilters] = useState(() => new Set(Object.keys(EDGE_FILTERS)));
  const [focusNodeId, setFocusNodeId] = useState<string | null>(null);

  const graphQuery = useQuery({
    queryKey: ['graph', projectId, currentBranch?.name ?? null],
    queryFn: () => api.getGraph(projectId!, currentBranch?.name),
    enabled: Boolean(projectId)
  });
  const graph: GraphResponse | null = graphQuery.data ?? null;
  const selectedNodeQuery = useQuery({
    queryKey: ['graph-node-detail', projectId, selectedNodeId],
    queryFn: () => api.getGraphNodeDetail(projectId!, selectedNodeId!),
    enabled: Boolean(projectId) && Boolean(selectedNodeId),
    staleTime: 30_000
  });
  const selectedNodeDetail: ProjectNodeDetailResponse | null = selectedNodeQuery.data ?? null;
  const nodeExplainQuery = useQuery({
    queryKey: ['graph-node-explain', projectId, selectedNodeId],
    queryFn: () => api.explainNode(projectId!, selectedNodeId!),
    enabled: false,
    staleTime: 60_000
  });
  const loading = graphQuery.isLoading;
  const error = graphQuery.error instanceof Error ? graphQuery.error.message : null;

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
    () => applyFilters(graph, activeNodeFilters, activeEdgeFilters, search, focusNodeId),
    [graph, activeNodeFilters, activeEdgeFilters, search, focusNodeId]
  );

  const { nodes, edges } = useMemo(() => buildLayout(filteredGraph), [filteredGraph]);
  const selectedNode = graph?.nodes.find((n) => n.nodeId === selectedNodeId);
  const detailNodeLabel = selectedNodeDetail?.displayName ?? selectedNode?.displayName ?? 'Node';

  return (
    <div className="flex h-full flex-col gap-4">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold">CGOne Architecture</h1>
          <p className="mt-1 text-sm text-text-secondary">Explore dependencies across frontend, backend and database schemas.</p>
          {currentBranch && <p className="mt-1 text-xs text-primary">Branch: {currentBranch.name} · {currentBranch.headCommitSha?.slice(0, 8) ?? 'working tree'}</p>}
        </div>
        <div className="flex items-center gap-3 text-xs">
          {focusNodeId && <button type="button" onClick={() => setFocusNodeId(null)} className="text-primary hover:underline">Reset focus</button>}
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
          {loading ? (
            <Loader label="Loading architecture..." fullHeight />
          ) : error ? (
            <div className="flex h-full flex-col items-center justify-center gap-2 px-6 text-center text-sm text-text-muted">
              <span className="font-medium text-text-primary">Architecture unavailable</span>
              <span>{error}</span>
            </div>
          ) : !graph || graph.nodes.length === 0 ? (
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
              onNodeDoubleClick={(_, node) => setFocusNodeId(node.id)}
              onPaneClick={() => setFocusNodeId(null)}
              fitView
              fitViewOptions={{ padding: 0.2, minZoom: 0.35, maxZoom: 1.2 }}
              nodesConnectable={false}
              defaultEdgeOptions={{ type: 'smoothstep', animated: false }}
              proOptions={{ hideAttribution: true }}
            >
              <Background color="#252b42" gap={24} size={1} />
              <Controls />
              <MiniMap
                nodeColor={(node) => String(node.data.color ?? '#64748b')}
                maskColor="rgba(10, 14, 28, 0.72)"
                className="!border !border-border !bg-card"
              />
              <Panel position="top-right" className="!m-3 rounded border border-border bg-card/95 px-3 py-2 text-[11px] text-text-secondary shadow-lg">
                <span className="font-semibold text-text-primary">{nodes.length}</span> components
                <span className="mx-1.5 text-text-muted">/</span>
                <span className="font-semibold text-text-primary">{edges.length}</span> relationships
              </Panel>
              <GraphLegend mode="architecture" />
            </ReactFlow>
          )}
        </div>

        {(selectedNode || selectedNodeDetail) && (
          <div className="w-80 shrink-0 rounded-lg border border-border bg-card p-4 text-sm">
            <div className="flex items-start justify-between gap-2">
              <div>
                <h3 className="font-semibold">{detailNodeLabel}</h3>
                <p className="mt-1 text-xs uppercase tracking-wide text-text-muted">{selectedNodeDetail?.componentType ?? selectedNode?.componentType ?? 'Unknown'}</p>
              </div>
              {selectedNodeDetail?.filePath && (
                <span className="rounded bg-primary/10 px-2 py-0.5 text-[10px] uppercase tracking-wide text-primary">parsed</span>
              )}
            </div>

            {(selectedNodeDetail?.filePath ?? selectedNode?.filePath) && (
              <p className="mt-3 break-all font-mono text-[11px] text-text-secondary">
                {(selectedNodeDetail?.filePath ?? selectedNode?.filePath) as string}
              </p>
            )}

            {selectedNodeDetail?.contains && selectedNodeDetail.contains.length > 0 && (
              <div className="mt-4">
                <p className="mb-2 text-[10px] font-semibold uppercase tracking-wide text-text-muted">Contains</p>
                <ul className="space-y-1.5">
                  {selectedNodeDetail.contains.slice(0, 8).map((item) => (
                    <li key={item} className="rounded border border-border bg-cardMuted px-2 py-1 text-[11px] text-text-secondary">
                      {item}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {selectedNodeDetail && (
              <>
                <div className="mt-4 space-y-3">
                  <div>
                    <p className="mb-1 text-[10px] font-semibold uppercase tracking-wide text-text-muted">Depended on by</p>
                    {selectedNodeDetail.incoming.length === 0 ? (
                      <p className="text-[11px] text-text-muted">No incoming dependencies.</p>
                    ) : (
                      <ul className="space-y-1.5">
                        {selectedNodeDetail.incoming.slice(0, 6).map((edge) => (
                          <li key={`${edge.neighborNodeId}-${edge.edgeType}-incoming`}>
                            <button
                              onClick={() => setSelectedNodeId(edge.neighborNodeId)}
                              className="w-full rounded border border-border bg-cardMuted px-2 py-1 text-left text-[11px] text-text-secondary transition hover:border-primary/40 hover:text-text-primary"
                            >
                              <span className="font-medium text-text-primary">{edge.neighborName}</span>
                              <span className="ml-2 text-text-muted">{edge.edgeType}</span>
                              <span className="ml-2 text-primary">{edge.confidence.toFixed(2)}</span>
                            </button>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>

                  <div>
                    <p className="mb-1 text-[10px] font-semibold uppercase tracking-wide text-text-muted">Depends on</p>
                    {selectedNodeDetail.outgoing.length === 0 ? (
                      <p className="text-[11px] text-text-muted">No outgoing dependencies.</p>
                    ) : (
                      <ul className="space-y-1.5">
                        {selectedNodeDetail.outgoing.slice(0, 6).map((edge) => (
                          <li key={`${edge.neighborNodeId}-${edge.edgeType}-outgoing`}>
                            <button
                              onClick={() => setSelectedNodeId(edge.neighborNodeId)}
                              className="w-full rounded border border-border bg-cardMuted px-2 py-1 text-left text-[11px] text-text-secondary transition hover:border-primary/40 hover:text-text-primary"
                            >
                              <span className="font-medium text-text-primary">{edge.neighborName}</span>
                              <span className="ml-2 text-text-muted">{edge.edgeType}</span>
                              <span className="ml-2 text-primary">{edge.confidence.toFixed(2)}</span>
                            </button>
                          </li>
                        ))}
                      </ul>
                    )}
                  </div>
                </div>

                {selectedNodeDetail.whyThisMatters && (
                  <div className="mt-4 rounded border border-primary/20 bg-primary/5 p-2 text-[11px] text-primary">
                    <p className="font-semibold uppercase tracking-wide">Why this matters</p>
                    <p className="mt-1 leading-relaxed">{selectedNodeDetail.whyThisMatters}</p>
                  </div>
                )}

                <div className="mt-4">
                  <button
                    type="button"
                    onClick={() => void nodeExplainQuery.refetch()}
                    disabled={nodeExplainQuery.isFetching}
                    className="w-full rounded border border-primary/40 bg-primary/10 px-2 py-1.5 text-[11px] font-medium text-primary transition hover:bg-primary/20 disabled:opacity-60"
                  >
                    {nodeExplainQuery.isFetching ? 'Summarizing\u2026' : '\u2728 Explain'}
                  </button>
                  {nodeExplainQuery.isFetching && <Loader size="sm" />}
                  {nodeExplainQuery.data && !nodeExplainQuery.isFetching && (
                    <div className="mt-2 rounded border border-border bg-cardMuted p-2 text-[11px] leading-relaxed text-text-secondary">
                      {nodeExplainQuery.data.degraded && (
                        <p className="mb-1 text-[10px] uppercase tracking-wide text-text-muted">Simplified summary (AI not configured)</p>
                      )}
                      {nodeExplainQuery.data.summary}
                    </div>
                  )}
                </div>
              </>
            )}
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

function applyFilters(
  graph: GraphResponse | null,
  activeNodeFilters: Set<string>,
  activeEdgeFilters: Set<string>,
  search: string,
  focusNodeId: string | null
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
  const focusNeighbors = focusNodeId
    ? new Set(graph.edges.filter((edge) => edge.sourceNodeId === focusNodeId || edge.targetNodeId === focusNodeId).flatMap((edge) => [edge.sourceNodeId, edge.targetNodeId]))
    : null;
  const focusedNodes = focusNeighbors ? nodes.filter((node) => focusNeighbors.has(node.nodeId)) : nodes;
  const nodeIds = new Set(focusedNodes.map((n) => n.nodeId));

  const edges = graph.edges.filter((e) => {
    if (!nodeIds.has(e.sourceNodeId) || !nodeIds.has(e.targetNodeId)) return false;
    return alwaysShownEdgeTypes.has(e.edgeType) || allowedEdgeTypes.has(e.edgeType);
  });

  return { snapshotId: graph.snapshotId, nodes: focusedNodes, edges };
}

function buildLayout(graph: GraphResponse | null): { nodes: Node[]; edges: Edge[] } {
  if (!graph) return { nodes: [], edges: [] };

  const layerForType = (type: string) => {
    if (type.startsWith('FRONTEND')) return 'Frontend';
    if (type === 'API' || type === 'CONTROLLER') return 'API';
    if (type === 'SERVICE' || type === 'INTERFACE') return 'Services';
    if (type === 'REPOSITORY') return 'Data access';
    if (type.startsWith('DATABASE') || type === 'STORED_PROCEDURE') return 'Database';
    if (type === 'EXTERNAL') return 'External';
    return 'Other';
  };

  const layerOrder = ['Frontend', 'API', 'Services', 'Data access', 'Database', 'External', 'Other'];
  const byLayer = new Map<string, typeof graph.nodes>();
  for (const node of graph.nodes) {
    const layer = layerForType(node.componentType);
    if (!byLayer.has(layer)) byLayer.set(layer, []);
    byLayer.get(layer)!.push(node);
  }

  const nodes: Node[] = [];
  const sortedLayers = layerOrder.filter((layer) => byLayer.has(layer));
  sortedLayers.forEach((layer) => {
    const items = byLayer.get(layer)!;
    [...items].sort((a, b) => a.displayName.localeCompare(b.displayName)).forEach((n) => {
      const color = GRAPH_TYPE_COLORS[n.componentType] ?? '#64748b';
      nodes.push({
        id: n.nodeId,
        position: { x: 0, y: 0 },
        data: { label: n.displayName, color },
        style: {
          background: '#161b32',
          border: `1px solid ${color}`,
          borderRadius: 8,
          color: '#e2e8f0',
          fontSize: 11,
          padding: 8,
          width: 180
        }
      });
    });
  });

  const layoutGraph = new dagre.graphlib.Graph().setDefaultEdgeLabel(() => ({}));
  layoutGraph.setGraph({ rankdir: 'LR', ranksep: 120, nodesep: 36, marginx: 24, marginy: 24 });
  for (const node of nodes) layoutGraph.setNode(node.id, { width: 180, height: 48 });
  for (const edge of graph.edges) {
    if (layoutGraph.hasNode(edge.sourceNodeId) && layoutGraph.hasNode(edge.targetNodeId)) layoutGraph.setEdge(edge.sourceNodeId, edge.targetNodeId);
  }
  dagre.layout(layoutGraph);
  for (const node of nodes) {
    const position = layoutGraph.node(node.id);
    if (position) node.position = { x: position.x - 90, y: position.y - 24 };
  }

  const edges: Edge[] = graph.edges.map((e) => ({
    id: e.edgeId,
    source: e.sourceNodeId,
    target: e.targetNodeId,
    animated: e.isRuntimeResolved,
    style: { stroke: e.isRuntimeResolved ? '#f59e0b' : '#475569', strokeWidth: e.isRuntimeResolved ? 1.5 : 1 }
  }));

  return { nodes, edges };
}
