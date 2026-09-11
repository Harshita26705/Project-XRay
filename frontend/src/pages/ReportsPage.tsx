import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useProjects } from '../state/ProjectContext';
import { api } from '../api/endpoints';
import type { ReportResponse } from '../api/types';
import { Card } from '../components/Card';
import { RiskBadge } from '../components/RiskBadge';
import { FilterSelect } from '../components/FilterSelect';
import { EmptyState } from '../components/States';

export default function ReportsPage() {
  const { currentProject } = useProjects();
  const [reports, setReports] = useState<ReportResponse[]>([]);
  const [search, setSearch] = useState('');
  const [riskFilter, setRiskFilter] = useState('ALL');
  const [formatFilter, setFormatFilter] = useState('ALL');
  const [dateRange, setDateRange] = useState('ALL');

  useEffect(() => {
    if (!currentProject) return;
    void api.listReports(currentProject.projectId).then(setReports);
  }, [currentProject]);

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
      <div>
        <h1 className="text-xl font-bold">Reports</h1>
        <p className="mt-1 text-sm text-text-secondary">Review historical static analysis profiles, risk scorecards, and blast radius changes.</p>
      </div>

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
        {filtered.length === 0 ? (
          <div className="p-5">
            <EmptyState title="No reports generated" message="Reports summarize structural trends and are created automatically on analysis cycles." />
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
