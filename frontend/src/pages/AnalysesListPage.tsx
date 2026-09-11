import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useProjects } from '../state/ProjectContext';
import { api } from '../api/endpoints';
import type { AnalysisResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { RiskBadge } from '../components/RiskBadge';
import { StatusBadge } from '../components/StatusBadge';
import { EmptyState } from '../components/States';

export default function AnalysesListPage() {
  const { currentProject } = useProjects();
  const [analyses, setAnalyses] = useState<AnalysisResponse[]>([]);

  useEffect(() => {
    if (!currentProject) return;
    void api.listAnalyses(currentProject.projectId).then(setAnalyses);
  }, [currentProject]);

  return (
    <div className="space-y-5">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-xl font-bold">Analyses</h1>
          <p className="mt-1 text-sm text-text-secondary">All change-impact analyses run against {currentProject?.name ?? 'this project'}.</p>
        </div>
        <Link to="/analyses/new"><Button variant="primary">Analyze a Change</Button></Link>
      </div>

      <Card>
        {analyses.length === 0 ? (
          <div className="p-5">
            <EmptyState title="No analyses yet" message="Run your first X-Ray dependency sweep to determine high-risk areas of a change." />
          </div>
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border text-[11px] uppercase text-text-muted">
                <th className="px-4 py-2 font-medium">Change</th>
                <th className="px-4 py-2 font-medium">Risk</th>
                <th className="px-4 py-2 font-medium">Critical</th>
                <th className="px-4 py-2 font-medium">Risky</th>
                <th className="px-4 py-2 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {analyses.map((a) => (
                <tr key={a.analysisId} className="border-b border-border/60 last:border-0 hover:bg-cardMuted/50">
                  <td className="px-4 py-2.5">
                    <Link to={`/analyses/${a.analysisId}`} className="font-medium text-primary hover:underline">{a.changeTitle}</Link>
                  </td>
                  <td className="px-4 py-2.5"><RiskBadge level={a.overallRiskState} /></td>
                  <td className="px-4 py-2.5 text-risk-critical">{a.criticalCount}</td>
                  <td className="px-4 py-2.5 text-risk-risky">{a.riskyCount}</td>
                  <td className="px-4 py-2.5"><StatusBadge status={a.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
