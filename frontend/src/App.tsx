import { useMemo, useState } from 'react';
import TabNav, { type TabId } from './components/TabNav';
import DependencyGraphTab from './tabs/DependencyGraphTab';
import LandingPage from './tabs/LandingPage';
import PRReportTab from './tabs/PRReportTab';
import SecurityTab from './tabs/SecurityTab';
import SetupTab from './tabs/SetupTab';
import XRayTab from './tabs/XRayTab';
import type { AnalysisResult, GraphPayload } from './types';

const EMPTY_GRAPH: GraphPayload = { nodes: [], edges: [] };

export default function App() {
  const [showLanding, setShowLanding] = useState(true);
  const [activeTab, setActiveTab] = useState<TabId>('setup');
  const [projectId, setProjectId] = useState('cgone-demo');
  const [root, setRoot] = useState('../demo-app');
  const [critical, setCritical] = useState('PaymentService');
  const [graph, setGraph] = useState<GraphPayload>(EMPTY_GRAPH);
  const [analysis, setAnalysis] = useState<AnalysisResult | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [impactedOnly, setImpactedOnly] = useState(true);
  const [runSecurity, setRunSecurity] = useState(true);

  const ingested = graph.nodes.length > 0;
  const securityCount = analysis?.security.findings.length ?? 0;

  const tabs = useMemo(
    () => [
      { id: 'setup' as TabId, label: '1. Set Up', hint: 'Configure and ingest the project' },
      {
        id: 'graph' as TabId,
        label: '2. Dependency Graph',
        hint: 'Explore the full dependency graph',
        disabled: !ingested
      },
      {
        id: 'xray' as TabId,
        label: '3. X-Ray',
        hint: 'Link a requirement and analyze its impact',
        disabled: !ingested
      },
      {
        id: 'pr' as TabId,
        label: '4. PR Report',
        hint: 'Attach a pull request and analyze its impact',
        disabled: !ingested
      },
      {
        id: 'security' as TabId,
        label: '5. Security',
        hint: 'Review security findings from the latest analysis',
        disabled: !ingested,
        badge: securityCount
      }
    ],
    [ingested, securityCount]
  );

  function handleIngested(next: GraphPayload) {
    setGraph(next);
    setAnalysis(null);
    setSelectedId(null);
    setActiveTab('graph');
  }

  function handleAnalysis(next: AnalysisResult) {
    setAnalysis(next);
    setSelectedId(null);
  }

  if (showLanding) {
    return <LandingPage onGetStarted={() => setShowLanding(false)} />;
  }

  return (
    <div className="flex h-screen flex-col overflow-hidden bg-slate-950 text-slate-100">
      <header className="shrink-0 border-b border-slate-800 bg-slate-900 px-4 py-3 sm:px-6">
        <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
          <button
            type="button"
            onClick={() => setShowLanding(true)}
            className="text-lg font-bold tracking-tight hover:text-amber-400"
          >
            PROJECT X-RAY
          </button>
          <span className="text-xs text-slate-400">
            Change impact analysis — the dependency graph is authoritative
          </span>
          {ingested && (
            <span className="ml-auto rounded-full border border-slate-700 px-2.5 py-0.5 text-[11px] text-slate-400">
              Project: <span className="font-mono text-slate-200">{projectId}</span>
            </span>
          )}
        </div>
      </header>

      <TabNav tabs={tabs} active={activeTab} onChange={setActiveTab} />

      <main className="min-h-0 flex-1 overflow-y-auto">
        {activeTab === 'setup' && (
          <SetupTab
            projectId={projectId}
            onProjectId={setProjectId}
            root={root}
            onRoot={setRoot}
            critical={critical}
            onCritical={setCritical}
            onIngested={handleIngested}
            ingested={ingested}
          />
        )}
        {activeTab === 'graph' && (
          <DependencyGraphTab
            graph={graph}
            analysis={analysis}
            impactedOnly={impactedOnly}
            onImpactedOnly={setImpactedOnly}
            selectedId={selectedId}
            onSelect={setSelectedId}
          />
        )}
        {activeTab === 'xray' && (
          <XRayTab
            projectId={projectId}
            ingested={ingested}
            runSecurity={runSecurity}
            onRunSecurity={setRunSecurity}
            analysis={analysis}
            onAnalysis={handleAnalysis}
            selectedId={selectedId}
            onSelect={setSelectedId}
          />
        )}
        {activeTab === 'pr' && (
          <PRReportTab
            projectId={projectId}
            ingested={ingested}
            runSecurity={runSecurity}
            onRunSecurity={setRunSecurity}
            analysis={analysis}
            onAnalysis={handleAnalysis}
            selectedId={selectedId}
            onSelect={setSelectedId}
          />
        )}
        {activeTab === 'security' && <SecurityTab analysis={analysis} />}
      </main>
    </div>
  );
}
