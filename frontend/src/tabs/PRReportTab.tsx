import { useState } from 'react';
import * as api from '../api';
import NodeDetails from '../components/NodeDetails';
import StatusMessage from '../components/StatusMessage';
import SummaryBar from '../components/SummaryBar';
import type { AnalysisResult, NodeResult, RiskState } from '../types';

const BADGE: Record<RiskState, string> = {
  RED: 'bg-red-900 text-red-200 border-red-500',
  YELLOW: 'bg-amber-900 text-amber-100 border-amber-500',
  GREEN: 'bg-green-900 text-green-200 border-green-500',
  UNKNOWN: 'bg-slate-700 text-slate-200 border-slate-400'
};

interface Props {
  projectId: string;
  ingested: boolean;
  runSecurity: boolean;
  onRunSecurity: (value: boolean) => void;
  analysis: AnalysisResult | null;
  onAnalysis: (analysis: AnalysisResult) => void;
  selectedId: string | null;
  onSelect: (id: string) => void;
}

export default function PRReportTab({
  projectId,
  ingested,
  runSecurity,
  onRunSecurity,
  analysis,
  onAnalysis,
  selectedId,
  onSelect
}: Props) {
  const [reference, setReference] = useState('');
  const [title, setTitle] = useState('');
  const [diffText, setDiffText] = useState('');
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState('');
  const [error, setError] = useState('');
  const [report, setReport] = useState('');
  const [reportOpen, setReportOpen] = useState(false);

  const selected = analysis?.nodes.find((node) => node.node_id === selectedId) ?? null;

  async function handleExtract() {
    if (!diffText.trim()) {
      setError('Paste the PR diff to extract its code changes.');
      return;
    }
    setBusy(true);
    setError('');
    setReport('');
    setReportOpen(false);
    setStatus('Extracting code changes and running X-Ray...');
    try {
      const result = await api.analyzePr({
        project_id: projectId,
        title: title || undefined,
        reference: reference || undefined,
        diff_text: diffText,
        run_security: runSecurity
      });
      onAnalysis(result);
      setStatus(
        `Extracted ${result.change.files?.length ?? 0} changed file(s) from the PR diff.`
      );
    } catch (exception) {
      setError(String(exception));
      setStatus('');
    } finally {
      setBusy(false);
    }
  }

  async function toggleReport() {
    if (!analysis) return;
    if (!reportOpen && !report) {
      try {
        const result = await api.getReport(analysis.analysis_id);
        setReport(result.markdown);
      } catch (exception) {
        setError(String(exception));
        return;
      }
    }
    setReportOpen((prev) => !prev);
  }

  if (!ingested) {
    return (
      <div className="flex h-full items-center justify-center px-6 text-center text-sm text-slate-500">
        Ingest a repository from the Set Up tab before analyzing a pull request.
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6 px-4 py-6 sm:px-6 lg:px-8">
      <div className="rounded-lg border border-slate-800 bg-slate-900 p-5 shadow-sm sm:p-6">
        <h2 className="text-base font-semibold text-slate-100">Analyze a pull request</h2>
        <p className="mt-1 text-sm text-slate-400">
          Paste the unified diff for a PR. X-Ray extracts the changed files and churn, then
          runs the same impact analysis against the dependency graph.
        </p>

        <div className="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
          <label className="block">
            <span className="text-xs font-medium uppercase tracking-wide text-slate-400">
              PR reference (id or URL, optional)
            </span>
            <input
              className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm focus:border-amber-500 focus:outline-none"
              value={reference}
              onChange={(event) => setReference(event.target.value)}
              placeholder="e.g. PR #128"
            />
          </label>
          <label className="block">
            <span className="text-xs font-medium uppercase tracking-wide text-slate-400">
              Title (optional)
            </span>
            <input
              className="mt-1 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 text-sm focus:border-amber-500 focus:outline-none"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
            />
          </label>
        </div>

        <label className="mt-4 block">
          <span className="text-xs font-medium uppercase tracking-wide text-slate-400">
            PR diff (unified diff / `git diff` output)
          </span>
          <textarea
            className="mt-1 h-56 w-full rounded-md border border-slate-700 bg-slate-950 px-3 py-2 font-mono text-xs focus:border-amber-500 focus:outline-none"
            value={diffText}
            onChange={(event) => setDiffText(event.target.value)}
            placeholder={'diff --git a/Services/PaymentService.cs b/Services/PaymentService.cs\n--- a/Services/PaymentService.cs\n+++ b/Services/PaymentService.cs\n@@ ...'}
          />
        </label>

        <div className="mt-5 flex flex-wrap items-center gap-3">
          <button
            className="rounded-md bg-amber-600 px-4 py-2 text-sm font-semibold text-slate-950 transition-colors hover:bg-amber-500 disabled:cursor-not-allowed disabled:opacity-50"
            onClick={handleExtract}
            disabled={busy}
          >
            {busy ? 'Extracting…' : 'Extract & run X-Ray'}
          </button>
          <label className="flex items-center gap-1.5 text-xs text-slate-300">
            <input
              type="checkbox"
              checked={runSecurity}
              onChange={(event) => onRunSecurity(event.target.checked)}
            />
            Run security scanners
          </label>
          {analysis && (
            <button
              type="button"
              onClick={toggleReport}
              className="ml-auto rounded-md border border-slate-700 px-3 py-1.5 text-xs font-medium text-slate-300 hover:text-slate-100"
            >
              {reportOpen ? 'Hide report' : 'View markdown report'}
            </button>
          )}
        </div>

        <div className="mt-3">
          <StatusMessage status={status} error={error} />
        </div>

        {reportOpen && (
          <pre className="mt-4 max-h-96 overflow-auto rounded-md border border-slate-700 bg-slate-950 p-3 text-xs text-slate-300">
            {report}
          </pre>
        )}
      </div>

      {analysis && (
        <div className="space-y-4">
          <SummaryBar analysis={analysis} />
          <div className="grid grid-cols-1 gap-4 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <div className="rounded-lg border border-slate-800 bg-slate-900">
                <h3 className="border-b border-slate-800 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
                  Changed files
                </h3>
                <ul className="max-h-40 divide-y divide-slate-800 overflow-y-auto font-mono text-xs">
                  {(analysis.change.files ?? []).map((file) => (
                    <li key={file.path} className="px-4 py-1.5 text-slate-300">
                      {file.path}
                    </li>
                  ))}
                </ul>
                <h3 className="border-y border-slate-800 px-4 py-2 text-xs font-semibold uppercase tracking-wide text-slate-400">
                  Impacted components
                </h3>
                <ul className="max-h-96 divide-y divide-slate-800 overflow-y-auto">
                  {analysis.nodes.map((node: NodeResult) => (
                    <li key={node.node_id}>
                      <button
                        type="button"
                        onClick={() => onSelect(node.node_id)}
                        className={`flex w-full items-center justify-between gap-3 px-4 py-2 text-left text-sm hover:bg-slate-800/60 ${
                          selectedId === node.node_id ? 'bg-slate-800/80' : ''
                        }`}
                      >
                        <span className="min-w-0 truncate">{node.name}</span>
                        <span
                          className={`shrink-0 rounded border px-2 py-0.5 text-[10px] font-semibold ${BADGE[node.state]}`}
                        >
                          {node.state}
                        </span>
                      </button>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
            <div className="rounded-lg border border-slate-800 bg-slate-900">
              <NodeDetails node={selected} />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
