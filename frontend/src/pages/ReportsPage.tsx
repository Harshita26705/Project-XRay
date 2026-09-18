import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useProjects } from '../state/ProjectContext';
import { api } from '../api/endpoints';
import type { AnalysisResponse, ReportResponse } from '../api/types';
import { Card } from '../components/Card';
import { RiskBadge } from '../components/RiskBadge';
import { FilterSelect } from '../components/FilterSelect';
import { EmptyState } from '../components/States';
import { Loader } from '../components/Loader';
import { Button } from '../components/Button';

export default function ReportsPage() {
  const { currentProject } = useProjects();
  const [reports, setReports] = useState<ReportResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [riskFilter, setRiskFilter] = useState('ALL');
  const [formatFilter, setFormatFilter] = useState('ALL');
  const [dateRange, setDateRange] = useState('ALL');

  const [analyses, setAnalyses] = useState<AnalysisResponse[]>([]);
  const [showGenerate, setShowGenerate] = useState(false);
  const [selectedAnalysisId, setSelectedAnalysisId] = useState('');
  const [isGenerating, setIsGenerating] = useState(false);
  const [generateError, setGenerateError] = useState<string | null>(null);

  const loadReports = async () => {
    if (!currentProject) return;
    setIsLoading(true);
    try {
      setReports(await api.listReports(currentProject.projectId));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadReports();
  }, [currentProject]);

  const openGenerate = async () => {
    setGenerateError(null);
    setShowGenerate(true);
    if (!currentProject) return;
    const list = await api.listAnalyses(currentProject.projectId);
    setAnalyses(list);
    setSelectedAnalysisId(list[0]?.analysisId ?? '');
  };

  const handleGenerate = async () => {
    if (!selectedAnalysisId) return;
    setIsGenerating(true);
    setGenerateError(null);
    try {
      await api.generateReport(selectedAnalysisId);
      await loadReports();
      setShowGenerate(false);
    } catch (reason: unknown) {
      setGenerateError(reason instanceof Error ? reason.message : 'Unable to generate report.');
    } finally {
      setIsGenerating(false);
    }
  };

  const formats = [...new Set(reports.map((r) => r.format))];

  const filtered = reports.filter((r) => {
    if (search && !r.title.toLowerCase().includes(search.toLowerCase()) && !r.analysisId.startsWith(search)) return false;
    if (riskFilter !== 'ALL' && (r.overallRiskState ?? 'UNKNOWN') !== riskFilter) return false;
    if (formatFilter !== 'ALL' && r.format !== formatFilter) return false;
    if (dateRange !== 'ALL') {
      const days = dateRange === '7' ? 7 : 30;
      const cutoff = Date.now() - days * 24 * 60 * 60 * 1000;
      if (new Date(r.createdAtUtc).getTime() < cutoff) return false;
    }
    return true;
  });

  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold">Reports</h1>
          <p className="mt-1 text-sm text-text-secondary">Review historical static analysis profiles, risk scorecards, and blast radius changes.</p>
        </div>
        <Button variant="primary" onClick={() => void openGenerate()} disabled={!currentProject}>Generate Report</Button>
      </div>

      {showGenerate && (
        <Card className="p-4">
          <p className="mb-2 text-sm font-semibold">Generate a report from an analysis</p>
          <p className="mb-3 text-xs text-text-secondary">A report summarizes what was analyzed (the change), the risk classification of every affected component, and the security findings tied to it — pick which completed analysis to summarize.</p>
          {analyses.length === 0 ? (
            <p className="text-xs text-text-muted">No analyses found for this project yet. Run an analysis first from the Analyses screen.</p>
          ) : (
            <div className="flex flex-wrap items-center gap-2">
              <select
                value={selectedAnalysisId}
                onChange={(e) => setSelectedAnalysisId(e.target.value)}
                className="rounded border border-border bg-cardMuted px-2 py-1.5 text-xs"
              >
                {analyses.map((a) => (
                  <option key={a.analysisId} value={a.analysisId}>
                    {a.changeTitle} · {a.status} · {new Date(a.createdAtUtc).toLocaleString()}
                  </option>
                ))}
              </select>
              <Button variant="primary" onClick={() => void handleGenerate()} disabled={isGenerating || !selectedAnalysisId}>
                {isGenerating ? 'Generating…' : 'Generate'}
              </Button>
              <Button onClick={() => setShowGenerate(false)} disabled={isGenerating}>Cancel</Button>
            </div>
          )}
          {generateError && <p className="mt-2 text-xs text-risk-critical">{generateError}</p>}
        </Card>
      )}

      <div className="flex flex-wrap items-center gap-2">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search change ID or description..."
          className="w-64 rounded border border-border bg-cardMuted px-2 py-1.5 text-xs placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-primary"
        />
        <FilterSelect value={riskFilter} onChange={setRiskFilter} label="Risk Level" options={['ALL', 'CRITICAL', 'RISKY', 'SAFE', 'UNKNOWN']} />
        <FilterSelect value={formatFilter} onChange={setFormatFilter} label="Type" options={['ALL', ...formats]} />
        <FilterSelect
          value={dateRange}
          onChange={setDateRange}
          label="Date Range"
          options={['ALL', '7', '30']}
          renderOption={(o) => (o === 'ALL' ? 'All time' : `Last ${o} days`)}
        />
      </div>

      <Card>
        {isLoading ? (
          <div className="p-5"><Loader label="Loading reports..." /></div>
        ) : filtered.length === 0 ? (
          <div className="p-5">
            <EmptyState title="No reports generated" message="Click “Generate Report” above and pick an analysis, or reports are created automatically on analysis cycles." />
          </div>
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border text-[11px] uppercase text-text-muted">
                <th className="px-4 py-2 font-medium">Date</th>
                <th className="px-4 py-2 font-medium">Change</th>
                <th className="px-4 py-2 font-medium">Risk</th>
                <th className="px-4 py-2 font-medium">Format</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((r) => (
                <tr key={r.reportId} className="border-b border-border/60 last:border-0 hover:bg-cardMuted/50">
                  <td className="px-4 py-2.5 text-text-muted">{new Date(r.createdAtUtc).toLocaleString()}</td>
                  <td className="px-4 py-2.5">
                    <Link to={`/reports/${r.reportId}`} className="font-medium text-primary hover:underline">{r.title}</Link>
                  </td>
                  <td className="px-4 py-2.5"><RiskBadge level={r.overallRiskState} /></td>
                  <td className="px-4 py-2.5 text-text-secondary">{r.format}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
