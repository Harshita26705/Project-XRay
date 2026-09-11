import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/endpoints';
import type { OverviewResponse } from '../api/types';
import { Card, StatCard } from '../components/Card';
import { RiskBadge } from '../components/RiskBadge';
import { StatusBadge } from '../components/StatusBadge';
import { useAuth } from '../auth/AuthProvider';
import { EmptyState } from '../components/States';

export default function OverviewPage() {
  const { displayName } = useAuth();
  const [data, setData] = useState<OverviewResponse | null>(null);

  useEffect(() => {
    void api.getOverview().then(setData);
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-bold">Good morning, {displayName.split(' ')[0]}</h1>
        <p className="mt-1 text-sm text-text-secondary">Here is what X-Ray found across your projects today.</p>
      </div>

      <div className="grid grid-cols-5 gap-4">
        <StatCard label="Total Analyses" value={data?.totalAnalyses ?? '—'} />
        <StatCard label="Critical Changes" value={data?.criticalChanges ?? '—'} accent="critical" />
        <StatCard label="Components Analyzed" value={data?.componentsAnalyzed ?? '—'} />
        <StatCard label="Security Findings" value={data?.securityFindings ?? '—'} accent="risky" />
        <StatCard label="Risk Trend Index (24h)" value="—" trend={data && data.criticalChanges > 0 ? '+ trend' : undefined} />
      </div>

      <div className="grid grid-cols-3 gap-6">
        <Card className="col-span-2">
          <div className="border-b border-border px-5 py-4">
            <h3 className="text-sm font-semibold">Recent Analyses</h3>
          </div>
          {!data || data.recentAnalyses.length === 0 ? (
            <div className="p-5">
              <EmptyState
                title="No analyses yet"
                message="Run your first X-Ray dependency sweep to determine high-risk areas of a change."
                action={
                  <Link to="/analyses/new" className="text-xs font-medium text-primary hover:underline">
                    Analyze a Change &rarr;
                  </Link>
                }
              />
            </div>
          ) : (
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-border text-[11px] uppercase text-text-muted">
                  <th className="px-5 py-2 font-medium">Change</th>
                  <th className="px-5 py-2 font-medium">Type</th>
                  <th className="px-5 py-2 font-medium">Risk</th>
                  <th className="px-5 py-2 font-medium">Affected</th>
                  <th className="px-5 py-2 font-medium">Created</th>
                  <th className="px-5 py-2 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.recentAnalyses.map((a) => (
                  <tr key={a.analysisId} className="border-b border-border/60 last:border-0 hover:bg-cardMuted/50">
                    <td className="px-5 py-2.5">
                      <Link to={`/analyses/${a.analysisId}`} className="font-mono text-xs text-primary hover:underline">
                        {a.changeTitle}
                      </Link>
                    </td>
                    <td className="px-5 py-2.5 text-text-secondary">{a.changeType}</td>
                    <td className="px-5 py-2.5">
                      <RiskBadge level={a.riskState} />
                    </td>
                    <td className="px-5 py-2.5 text-text-secondary">{a.affectedComponents} components</td>
                    <td className="px-5 py-2.5 text-text-muted">{new Date(a.createdAtUtc).toLocaleString()}</td>
                    <td className="px-5 py-2.5">
                      <StatusBadge status={a.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Card>

        <Card>
          <div className="border-b border-border px-5 py-4">
            <h3 className="text-sm font-semibold">Quick Actions</h3>
          </div>
          <div className="space-y-1 p-3">
            <QuickAction to="/analyses/new" title="Analyze a Change" subtitle="Submit file, PR or work item" />
            <QuickAction to="/changes" title="Analyze PR" subtitle="Scan open Azure pull request" />
            <QuickAction to="/projects" title="View Architecture" subtitle="Explore dependency graph" />
            <QuickAction to="/security" title="Run Security Scan" subtitle="Verify container & config safety" />
          </div>
        </Card>
      </div>
    </div>
  );
}

function QuickAction({ to, title, subtitle }: { to: string; title: string; subtitle: string }) {
  return (
    <Link to={to} className="block rounded-md px-3 py-2.5 hover:bg-cardMuted">
      <p className="text-sm font-medium text-text-primary">{title}</p>
      <p className="text-xs text-text-muted">{subtitle}</p>
    </Link>
  );
}
