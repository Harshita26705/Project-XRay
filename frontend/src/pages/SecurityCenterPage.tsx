import { useEffect, useState } from 'react';
import { api } from '../api/endpoints';
import { useProjects } from '../state/ProjectContext';
import type { SecurityFindingResponse } from '../api/types';
import { Card } from '../components/Card';
import { StatusBadge } from '../components/StatusBadge';
import { FilterSelect } from '../components/FilterSelect';
import { EmptyState } from '../components/States';

const SEVERITY_COLOR: Record<string, string> = {
  CRITICAL: 'text-risk-critical',
  HIGH: 'text-risk-critical',
  MEDIUM: 'text-risk-risky',
  LOW: 'text-risk-safe',
  INFO: 'text-risk-unknown'
};

export default function SecurityCenterPage() {
  const { currentProject } = useProjects();
  const [findings, setFindings] = useState<SecurityFindingResponse[]>([]);
  const [selected, setSelected] = useState<SecurityFindingResponse | null>(null);
  const [severityFilter, setSeverityFilter] = useState('ALL');
  const [statusFilter, setStatusFilter] = useState('ALL');

  useEffect(() => {
    if (!currentProject) return;
    void api.getSecurityFindings(currentProject.projectId).then((data) => {
      setFindings(data);
      setSelected(data[0] ?? null);
    });
  }, [currentProject]);

  const counts = {
    critical: findings.filter((f) => f.severity === 'CRITICAL').length,
    risky: findings.filter((f) => f.severity === 'HIGH' || f.severity === 'MEDIUM').length,
    safe: findings.filter((f) => f.severity === 'LOW').length,
    unknown: findings.filter((f) => f.severity === 'INFO').length
  };

  const filtered = findings.filter((f) => {
    if (severityFilter !== 'ALL' && f.severity !== severityFilter) return false;
    if (statusFilter !== 'ALL' && f.status !== statusFilter) return false;
    return true;
  });

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Security Center</h1>
        <p className="mt-1 text-sm text-text-secondary">Security findings connected to your software architecture.</p>
      </div>

      <div className="grid grid-cols-4 gap-4">
        <Card className="p-4 text-center"><p className="text-2xl font-bold text-risk-critical">{counts.critical}</p><p className="text-xs text-text-muted">Critical</p></Card>
        <Card className="p-4 text-center"><p className="text-2xl font-bold text-risk-risky">{counts.risky}</p><p className="text-xs text-text-muted">Risky</p></Card>
        <Card className="p-4 text-center"><p className="text-2xl font-bold text-risk-safe">{counts.safe}</p><p className="text-xs text-text-muted">Safe</p></Card>
        <Card className="p-4 text-center"><p className="text-2xl font-bold text-risk-unknown">{counts.unknown}</p><p className="text-xs text-text-muted">Unknown</p></Card>
      </div>

      <div className="grid grid-cols-3 gap-5">
        <Card className="col-span-2">
          <div className="flex items-center justify-between border-b border-border px-4 py-3">
            <span className="text-sm font-semibold">Architecture Vulnerabilities</span>
            <div className="flex gap-2">
              <FilterSelect value={severityFilter} onChange={setSeverityFilter} label="Severity" options={['ALL', 'CRITICAL', 'HIGH', 'MEDIUM', 'LOW', 'INFO']} />
              <FilterSelect value={statusFilter} onChange={setStatusFilter} label="Status" options={['ALL', 'OPEN', 'RESOLVED']} />
            </div>
          </div>
          {filtered.length === 0 ? (
            <div className="p-5"><EmptyState title="No security findings" message="Compliance details and secret disclosures will populate here after running a scan." /></div>
          ) : (
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-border text-[11px] uppercase text-text-muted">
                  <th className="px-4 py-2 font-medium">Finding</th>
                  <th className="px-4 py-2 font-medium">Severity</th>
                  <th className="px-4 py-2 font-medium">Component</th>
                  <th className="px-4 py-2 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((f) => (
                  <tr key={f.securityFindingId} onClick={() => setSelected(f)} className="cursor-pointer border-b border-border/60 last:border-0 hover:bg-cardMuted/50">
                    <td className="px-4 py-2.5">{f.title}</td>
                    <td className={`px-4 py-2.5 font-semibold ${SEVERITY_COLOR[f.severity]}`}>{f.severity}</td>
                    <td className="px-4 py-2.5 font-mono text-xs text-text-secondary">{f.component ?? '—'}</td>
                    <td className="px-4 py-2.5"><StatusBadge status={f.status} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Card>

        {selected && (
          <Card className="p-4">
            <div className="flex items-center justify-between">
              <h3 className="font-semibold">{selected.title}</h3>
              <span className={`text-xs font-bold ${SEVERITY_COLOR[selected.severity]}`}>{selected.severity}</span>
            </div>
            <div className="mt-3 space-y-1 text-xs">
              <p><span className="text-text-muted">Component: </span>{selected.component ?? '—'}</p>
              <p><span className="text-text-muted">File: </span><span className="font-mono">{selected.filePath ?? '—'}</span></p>
              <p><span className="text-text-muted">Line: </span>{selected.lineStart ?? '—'}</p>
            </div>
            <p className="mt-3 text-sm text-text-secondary">{selected.description}</p>
            {selected.remediation && (
              <div className="mt-3 rounded border border-border bg-page p-2 text-xs text-text-secondary">
                <span className="font-semibold text-text-primary">Recommended Remediation: </span>
                {selected.remediation}
              </div>
            )}
          </Card>
        )}
      </div>
    </div>
  );
}
