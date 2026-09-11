import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useProjects } from '../state/ProjectContext';
import { api } from '../api/endpoints';
import type { ChangeResponse, ChangeType } from '../api/types';
import { Card } from '../components/Card';
import { RiskBadge } from '../components/RiskBadge';
import { StatusBadge } from '../components/StatusBadge';
import { FilterSelect } from '../components/FilterSelect';
import { EmptyState } from '../components/States';

const TABS: { key: ChangeType; label: string }[] = [
  { key: 'PULL_REQUEST', label: 'Pull Requests' },
  { key: 'WORK_ITEM', label: 'Work Items' },
  { key: 'MANUAL', label: 'Commits' }
];

export default function ChangesPage() {
  const { currentProject } = useProjects();
  const [changes, setChanges] = useState<ChangeResponse[]>([]);
  const [tab, setTab] = useState<ChangeType>('PULL_REQUEST');
  const [search, setSearch] = useState('');
  const [riskFilter, setRiskFilter] = useState('ALL');
  const [statusFilter, setStatusFilter] = useState('ALL');
  const [authorFilter, setAuthorFilter] = useState('ALL');
  const [dateRange, setDateRange] = useState('ALL');

  useEffect(() => {
    if (!currentProject) return;
    void api.listChanges(currentProject.projectId).then(setChanges);
  }, [currentProject]);

  const byTab = changes.filter((c) => c.changeType === tab);
  const authors = [...new Set(byTab.map((c) => c.author).filter((a): a is string => Boolean(a)))];

  const filtered = byTab.filter((c) => {
    if (search && !c.title.toLowerCase().includes(search.toLowerCase()) && !c.changeId.startsWith(search)) return false;
    if (riskFilter !== 'ALL' && (c.riskState ?? 'UNKNOWN') !== riskFilter) return false;
    if (statusFilter !== 'ALL' && c.status !== statusFilter) return false;
    if (authorFilter !== 'ALL' && c.author !== authorFilter) return false;
    if (dateRange !== 'ALL') {
      const days = dateRange === '7' ? 7 : 30;
      const cutoff = Date.now() - days * 24 * 60 * 60 * 1000;
      if (new Date(c.createdAtUtc).getTime() < cutoff) return false;
    }
    return true;
  });
  const detailPath = (c: ChangeResponse) => (c.changeType === 'WORK_ITEM' ? `/changes/work-item/${c.changeId}` : `/changes/pr/${c.changeId}`);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Changes</h1>
        <p className="mt-1 text-sm text-text-secondary">Trace structural evolution and PR blast-radius across active workspaces.</p>
      </div>

      <div className="flex gap-1 border-b border-border">
        {TABS.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`border-b-2 px-3 py-2 text-sm font-medium ${tab === t.key ? 'border-primary text-text-primary' : 'border-transparent text-text-muted'}`}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search PR title, number..."
          className="w-56 rounded border border-border bg-cardMuted px-2 py-1.5 text-xs placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-primary"
        />
        <FilterSelect value={riskFilter} onChange={setRiskFilter} label="Risk Level" options={['ALL', 'CRITICAL', 'RISKY', 'SAFE', 'UNKNOWN']} />
        <FilterSelect value={statusFilter} onChange={setStatusFilter} label="Status" options={['ALL', 'ANALYZED', 'PENDING']} />
        <FilterSelect value={authorFilter} onChange={setAuthorFilter} label="Author" options={['ALL', ...authors]} />
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
            <EmptyState title="Nothing here yet" message="Changes analyzed through X-Ray will show up in this list." />
          </div>
        ) : (
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="border-b border-border text-[11px] uppercase text-text-muted">
                <th className="px-4 py-2 font-medium">Title</th>
                <th className="px-4 py-2 font-medium">Author</th>
                <th className="px-4 py-2 font-medium">Risk</th>
                <th className="px-4 py-2 font-medium">Affected</th>
                <th className="px-4 py-2 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((c) => (
                <tr key={c.changeId} className="border-b border-border/60 last:border-0 hover:bg-cardMuted/50">
                  <td className="px-4 py-2.5">
                    <Link to={detailPath(c)} className="font-medium text-primary hover:underline">{c.title}</Link>
                  </td>
                  <td className="px-4 py-2.5 text-text-secondary">{c.author ?? '—'}</td>
                  <td className="px-4 py-2.5"><RiskBadge level={c.riskState} /></td>
                  <td className="px-4 py-2.5 text-text-secondary">{c.affectedComponents} components</td>
                  <td className="px-4 py-2.5"><StatusBadge status={c.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
