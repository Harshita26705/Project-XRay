import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useProjects } from '../state/ProjectContext';
import { api } from '../api/endpoints';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import type { ChangeType } from '../api/types';

const TABS: { key: ChangeType; label: string }[] = [
  { key: 'WORK_ITEM', label: 'Work Item' },
  { key: 'PULL_REQUEST', label: 'Pull Request' },
  { key: 'MANUAL', label: 'Manual Change' }
];

export default function AnalyzeChangePage() {
  const { currentProject } = useProjects();
  const navigate = useNavigate();
  const [tab, setTab] = useState<ChangeType>('WORK_ITEM');
  const [externalId, setExternalId] = useState('9214');
  const [title, setTitle] = useState('Add payment validation checks');
  const [description, setDescription] = useState('');
  const [filesText, setFilesText] = useState('');
  const [scope, setScope] = useState({
    scanDirectDependencies: true,
    traceTransitiveDependencies: true,
    includeExternalBindings: false,
    runStaticSecurityAnalysis: true
  });
  const [busy, setBusy] = useState(false);

  const run = async () => {
    if (!currentProject) return;
    setBusy(true);
    try {
      const changedFilePaths = filesText
        .split('\n')
        .map((f) => f.trim())
        .filter(Boolean);

      const change = await api.createChange(currentProject.projectId, {
        changeType: tab,
        title,
        description,
        externalChangeId: tab === 'MANUAL' ? undefined : externalId,
        changedFilePaths
      });

      const analysis = await api.createAnalysis(change.changeId, scope);
      navigate(`/analyses/${analysis.analysisId}`);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-bold">Analyze a Change</h1>
        <p className="mt-1 text-sm text-text-secondary">Trace potential blast radius and find breaking dependency chains before pushing your code.</p>
      </div>

      <div className="grid grid-cols-3 gap-6">
        <Card className="col-span-2 p-5">
          <div className="mb-4 flex gap-1 border-b border-border">
            {TABS.map((t) => (
              <button
                key={t.key}
                onClick={() => setTab(t.key)}
                className={`border-b-2 px-3 py-2 text-sm font-medium ${
                  tab === t.key ? 'border-primary text-text-primary' : 'border-transparent text-text-muted hover:text-text-secondary'
                }`}
              >
                {t.label}
              </button>
            ))}
          </div>

          {tab === 'WORK_ITEM' && (
            <div className="space-y-3">
              <label className="block text-xs text-text-secondary">
                Azure DevOps Work Item ID
                <input value={externalId} onChange={(e) => setExternalId(e.target.value)} className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
              </label>
              <p className="text-xs text-risk-safe">&#9679; Azure DevOps Connected</p>
            </div>
          )}
          {tab === 'PULL_REQUEST' && (
            <label className="block text-xs text-text-secondary">
              Pull Request Number
              <input value={externalId} onChange={(e) => setExternalId(e.target.value)} className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
            </label>
          )}

          <label className="mt-3 block text-xs text-text-secondary">
            Title
            <input value={title} onChange={(e) => setTitle(e.target.value)} className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
          </label>
          <label className="mt-3 block text-xs text-text-secondary">
            Description
            <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm" />
          </label>
          <label className="mt-3 block text-xs text-text-secondary">
            Changed file paths (one per line, relative to repository root)
            <textarea
              value={filesText}
              onChange={(e) => setFilesText(e.target.value)}
              rows={4}
              placeholder={'CgOne.Demo.Api/Services/PaymentService.cs'}
              className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 font-mono text-xs"
            />
          </label>

          <Button variant="primary" className="mt-4" onClick={run} disabled={busy || !currentProject}>
            {busy ? 'Running X-Ray Analysis...' : 'Run X-Ray Analysis'}
          </Button>
        </Card>

        <div className="space-y-4">
          <Card className="p-4">
            <h3 className="mb-2 text-xs font-semibold uppercase text-text-muted">Context</h3>
            <p className="text-[11px] text-text-muted">TARGET PROJECT</p>
            <p className="text-sm font-medium">{currentProject?.name ?? 'No project selected'} (Azure DevOps)</p>
            <p className="mt-2 text-[11px] text-text-muted">WORKING BRANCH</p>
            <p className="text-sm text-primary">main</p>
          </Card>

          <Card className="p-4">
            <h3 className="mb-2 text-xs font-semibold uppercase text-text-muted">Analysis Scope</h3>
            <div className="space-y-2 text-sm">
              <ScopeCheckbox label="Scan direct dependencies" checked={scope.scanDirectDependencies} onChange={(v) => setScope((s) => ({ ...s, scanDirectDependencies: v }))} />
              <ScopeCheckbox label="Trace recursive transitive dependencies" checked={scope.traceTransitiveDependencies} onChange={(v) => setScope((s) => ({ ...s, traceTransitiveDependencies: v }))} />
              <ScopeCheckbox label="Include external API bindings" checked={scope.includeExternalBindings} onChange={(v) => setScope((s) => ({ ...s, includeExternalBindings: v }))} />
              <ScopeCheckbox label="Perform static security analysis" checked={scope.runStaticSecurityAnalysis} onChange={(v) => setScope((s) => ({ ...s, runStaticSecurityAnalysis: v }))} />
            </div>
          </Card>
        </div>
      </div>
    </div>
  );
}

function ScopeCheckbox({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <label className="flex items-center gap-2">
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} className="h-3.5 w-3.5 accent-primary" />
      <span className="text-text-secondary">{label}</span>
    </label>
  );
}
