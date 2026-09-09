import { useState } from 'react';
import * as api from '../api';
import StatusMessage from '../components/StatusMessage';
import type { GraphPayload } from '../types';

interface Props {
  projectId: string;
  onProjectId: (value: string) => void;
  root: string;
  onRoot: (value: string) => void;
  critical: string;
  onCritical: (value: string) => void;
  onIngested: (graph: GraphPayload) => void;
  ingested: boolean;
}

export default function SetupTab({
  projectId,
  onProjectId,
  root,
  onRoot,
  critical,
  onCritical,
  onIngested,
  ingested
}: Props) {
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState('');
  const [error, setError] = useState('');

  async function handleIngest() {
    setBusy(true);
    setError('');
    setStatus('Ingesting repository...');
    try {
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
      onIngested(graph);
      setStatus(
        `Ingested ${report.files_ingested} file(s): ${report.nodes} nodes, ` +
          `${report.edges} edges, ${report.files_failed} parse failure(s), ` +
          `${report.secret_hits} secret hit(s).`
      );
    } catch (exception) {
      setError(String(exception));
      setStatus('');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto grid max-w-6xl grid-cols-1 gap-6 px-4 py-6 sm:px-6 lg:grid-cols-3 lg:px-8">
      <div className="rounded-lg border border-slate-800 bg-slate-900 p-5 shadow-sm sm:p-6 lg:col-span-2">
        <h2 className="text-base font-semibold text-slate-100">Set up your project</h2>
        <p className="mt-1 text-sm text-slate-400">
          Point X-Ray at a repository so it can build the dependency graph that every other
          tab relies on.
        </p>

        <div className="mt-5 grid grid-cols-1 gap-4 sm:grid-cols-2">
          <label className="block">
            <span className="text-xs font-medium uppercase tracking-wide text-slate-400">
              Project id
            </span>
            <input
              className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm focus:border-amber-500 focus:outline-none"
              value={projectId}
              onChange={(event) => onProjectId(event.target.value)}
            />
          </label>
          <label className="block">
            <span className="text-xs font-medium uppercase tracking-wide text-slate-400">
              Critical components (comma separated)
            </span>
            <input
              className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm focus:border-amber-500 focus:outline-none"
              value={critical}
              onChange={(event) => onCritical(event.target.value)}
              placeholder="PaymentService"
            />
          </label>
        </div>

        <label className="mt-4 block">
          <span className="text-xs font-medium uppercase tracking-wide text-slate-400">
            Repository root (server-side path)
          </span>
          <input
            className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs focus:border-amber-500 focus:outline-none"
            value={root}
            onChange={(event) => onRoot(event.target.value)}
          />
        </label>

        <div className="mt-6 flex flex-wrap items-center gap-3">
          <button
            className="rounded-md bg-amber-600 px-4 py-2 text-sm font-semibold text-slate-950 transition-colors hover:bg-amber-500 disabled:cursor-not-allowed disabled:opacity-50"
            onClick={handleIngest}
            disabled={busy || !projectId || !root}
          >
            {busy ? 'Ingesting…' : ingested ? 'Re-ingest repository' : 'Ingest repository'}
          </button>
          {ingested && (
            <span className="rounded-full border border-green-700 bg-green-950 px-2.5 py-1 text-xs font-medium text-green-300">
              ✓ Repository ingested — dependency graph is ready
            </span>
          )}
        </div>

        <div className="mt-3">
          <StatusMessage status={status} error={error} />
        </div>
      </div>

      <div className="h-fit rounded-lg border border-slate-800 bg-slate-900/60 p-5 text-sm text-slate-400">
        <h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-slate-500">
          Next steps
        </h3>
        <ol className="list-decimal space-y-1 pl-4">
          <li>Ingest the repository above to build the dependency graph.</li>
          <li>Open the Dependency Graph tab to explore components and relationships.</li>
          <li>Use the X-Ray tab to analyze a requirement, or the PR Report tab to analyze a pull request.</li>
        </ol>
      </div>
    </div>
  );
}
