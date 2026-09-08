import { useState } from 'react';
import * as api from '../api';
import type { AnalysisResult, GraphPayload } from '../types';

const DEFAULT_ROOT = '../demo-app';
const DEFAULT_FILES = [
  'CgOne.Demo.Api/Services/PaymentValidator.cs',
  'CgOne.Demo.Api/Services/PaymentService.cs'
].join('\n');

interface Props {
  onGraph: (graph: GraphPayload) => void;
  onAnalysis: (analysis: AnalysisResult) => void;
  impactedOnly: boolean;
  onImpactedOnly: (value: boolean) => void;
}

export default function ControlPanel({
  onGraph,
  onAnalysis,
  impactedOnly,
  onImpactedOnly
}: Props) {
  const [projectId, setProjectId] = useState('cgone-demo');
  const [root, setRoot] = useState(DEFAULT_ROOT);
  const [critical, setCritical] = useState('');
  const [files, setFiles] = useState(DEFAULT_FILES);
  const [title, setTitle] = useState('Change payment validation');
  const [runSecurity, setRunSecurity] = useState(true);
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState('');
  const [error, setError] = useState('');

  async function run<T>(label: string, action: () => Promise<T>): Promise<T | null> {
    setBusy(true);
    setError('');
    setStatus(`${label}...`);
    try {
      const result = await action();
      setStatus(`${label} complete.`);
      return result;
    } catch (exception) {
      setError(String(exception));
      setStatus('');
      return null;
    } finally {
      setBusy(false);
    }
  }

  async function handleIngest() {
    await run('Ingesting', async () => {
      await api
        .createProject({
          id: projectId,
          name: projectId,
          root,
          critical: critical
            .split(',')
            .map((value) => value.trim())
            .filter(Boolean)
        })
        .catch(() => undefined);
      const report = await api.ingest(projectId);
      const graph = await api.getGraph(projectId);
      onGraph(graph);
      setStatus(
        `Ingested ${report.files_ingested} file(s): ${report.nodes} nodes, ` +
          `${report.edges} edges, ${report.files_failed} parse failure(s), ` +
          `${report.secret_hits} secret hit(s).`
      );
      return report;
    });
  }

  async function handleAnalyze() {
    await run('Analyzing', async () => {
      const analysis = await api.analyze({
        project_id: projectId,
        run_security: runSecurity,
        change: {
          source: 'manual',
          title,
          files: files
            .split('\n')
            .map((line) => line.trim())
            .filter(Boolean)
            .map((path) => ({ path, added_lines: 12, removed_lines: 3 }))
        }
      });
      onAnalysis(analysis);
      return analysis;
    });
  }

  return (
    <details className="border-b border-slate-800 bg-slate-900" open>
      <summary className="cursor-pointer select-none px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-400 hover:text-slate-200">
        Analysis setup
      </summary>
      <div className="space-y-3 px-4 pb-4 text-sm">
      <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
        <label className="block">
          <span className="text-xs text-slate-400">Project id</span>
          <input
            className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-2 py-1"
            value={projectId}
            onChange={(event) => setProjectId(event.target.value)}
          />
        </label>
        <label className="block md:col-span-2">
          <span className="text-xs text-slate-400">Repository root (server-side path)</span>
          <input
            className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-2 py-1 font-mono text-xs"
            value={root}
            onChange={(event) => setRoot(event.target.value)}
          />
        </label>
      </div>

      <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
        <label className="block">
          <span className="text-xs text-slate-400">Change title</span>
          <input
            className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-2 py-1"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
          />
        </label>
        <label className="block">
          <span className="text-xs text-slate-400">
            Critical components (comma separated, optional)
          </span>
          <input
            className="mt-1 w-full rounded border border-slate-700 bg-slate-950 px-2 py-1"
            value={critical}
            onChange={(event) => setCritical(event.target.value)}
            placeholder="PaymentService"
          />
        </label>
      </div>

      <label className="block">
        <span className="text-xs text-slate-400">Changed files (one per line)</span>
        <textarea
          className="mt-1 h-20 w-full rounded border border-slate-700 bg-slate-950 px-2 py-1 font-mono text-xs"
          value={files}
          onChange={(event) => setFiles(event.target.value)}
        />
      </label>

      <div className="flex flex-wrap items-center gap-3">
        <button
          className="rounded bg-slate-700 px-3 py-1.5 font-medium hover:bg-slate-600 disabled:opacity-50"
          onClick={handleIngest}
          disabled={busy}
        >
          1. Ingest repository
        </button>
        <button
          className="rounded bg-amber-700 px-3 py-1.5 font-medium hover:bg-amber-600 disabled:opacity-50"
          onClick={handleAnalyze}
          disabled={busy}
        >
          2. Run X-Ray analysis
        </button>
        <label className="flex items-center gap-1 text-xs">
          <input
            type="checkbox"
            checked={runSecurity}
            onChange={(event) => setRunSecurity(event.target.checked)}
          />
          Run security scanners
        </label>
        <label className="flex items-center gap-1 text-xs">
          <input
            type="checkbox"
            checked={impactedOnly}
            onChange={(event) => onImpactedOnly(event.target.checked)}
          />
          Impacted components only
        </label>
      </div>

      {status && <p className="text-xs text-slate-400">{status}</p>}
      {error && <p className="text-xs text-red-400">{error}</p>}
      </div>
    </details>
  );
}
