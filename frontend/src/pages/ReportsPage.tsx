import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useProjects } from '../state/ProjectContext';
import { api } from '../api/endpoints';
import type { ReportResponse } from '../api/types';
import { Card } from '../components/Card';
import { EmptyState } from '../components/States';

export default function ReportsPage() {
  const { currentProject } = useProjects();
  const [reports, setReports] = useState<ReportResponse[]>([]);

  useEffect(() => {
    if (!currentProject) return;
    void api.listReports(currentProject.projectId).then(setReports);
  }, [currentProject]);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Reports</h1>
        <p className="mt-1 text-sm text-text-secondary">Review historical static analysis profiles, risk scorecards, and blast radius changes.</p>
      </div>

      <Card>
        {reports.length === 0 ? (
          <div className="p-5">
            <EmptyState title="No reports generated" message="Reports summarize structural trends and are created automatically on analysis cycles." />
          </div>
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border text-[11px] uppercase text-text-muted">
                <th className="px-4 py-2 font-medium">Date</th>
                <th className="px-4 py-2 font-medium">Change</th>
                <th className="px-4 py-2 font-medium">Format</th>
              </tr>
            </thead>
            <tbody>
              {reports.map((r) => (
                <tr key={r.reportId} className="border-b border-border/60 last:border-0 hover:bg-cardMuted/50">
                  <td className="px-4 py-2.5 text-text-muted">{new Date(r.createdAtUtc).toLocaleString()}</td>
                  <td className="px-4 py-2.5">
                    <Link to={`/reports/${r.reportId}`} className="font-medium text-primary hover:underline">{r.title}</Link>
                  </td>
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
