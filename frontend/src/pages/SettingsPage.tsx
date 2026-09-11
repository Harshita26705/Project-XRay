import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/endpoints';
import type { OrganizationSettingsResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';

const NAV = ['General', 'Projects', 'Repositories', 'Security', 'AI Settings', 'Notifications', 'Access Control', 'API Keys'];

export default function SettingsPage() {
  const [settings, setSettings] = useState<OrganizationSettingsResponse | null>(null);

  useEffect(() => {
    void api.getSettings().then(setSettings);
  }, []);

  const save = async () => {
    if (!settings) return;
    await api.updateSettings(settings);
  };

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Settings</h1>
        <p className="mt-1 text-sm text-text-secondary">Configure organization preferences, analysis standards, and security baselines.</p>
      </div>

      <div className="grid grid-cols-5 gap-6">
        <div className="space-y-1 text-sm">
          {NAV.map((item) => (
            <div key={item}>
              {item === 'AI Settings' ? (
                <Link to="/settings/ai" className="block rounded px-3 py-1.5 text-text-secondary hover:bg-cardMuted">{item}</Link>
              ) : item === 'Notifications' ? (
                <Link to="/settings/notifications" className="block rounded px-3 py-1.5 text-text-secondary hover:bg-cardMuted">{item}</Link>
              ) : (
                <span className={`block rounded px-3 py-1.5 ${item === 'General' ? 'bg-primary-muted text-white' : 'text-text-secondary hover:bg-cardMuted'}`}>{item}</span>
              )}
            </div>
          ))}
        </div>

        <div className="col-span-4 space-y-5">
          <Card className="p-4">
            <h3 className="mb-3 text-sm font-semibold">Organization Profile</h3>
            <label className="block text-xs text-text-secondary">
              Organization Name
              <input
                value={settings?.organizationName ?? ''}
                onChange={(e) => settings && setSettings({ ...settings, organizationName: e.target.value })}
                className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm"
              />
            </label>
          </Card>

          <Card className="p-4">
            <h3 className="mb-3 text-sm font-semibold">Analysis Defaults</h3>
            <ToggleRow label="Include security scan" checked={settings?.includeSecurityScan ?? false} onChange={(v) => settings && setSettings({ ...settings, includeSecurityScan: v })} />
            <ToggleRow label="Include AI explanation" checked={settings?.includeAiExplanation ?? false} onChange={(v) => settings && setSettings({ ...settings, includeAiExplanation: v })} />
            <ToggleRow label="Auto-analyze PRs" checked={settings?.autoAnalyzePullRequests ?? false} onChange={(v) => settings && setSettings({ ...settings, autoAnalyzePullRequests: v })} />
          </Card>

          <Card className="border-risk-critical/40 p-4">
            <h3 className="mb-2 text-sm font-semibold text-risk-critical">Danger Zone</h3>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Delete Organization</p>
                <p className="text-xs text-text-muted">Permanently delete Project X-Ray data, analysis histories, and active tokens. This cannot be undone.</p>
              </div>
              <Button variant="danger">Delete Organization</Button>
            </div>
          </Card>

          <div className="flex gap-2">
            <Button variant="primary" onClick={save}>Save Changes</Button>
            <Button>Cancel</Button>
          </div>
        </div>
      </div>
    </div>
  );
}

function ToggleRow({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex items-center justify-between border-b border-border/60 py-2 text-sm last:border-0">
      <span className="text-text-secondary">{label}</span>
      <button
        onClick={() => onChange(!checked)}
        className={`h-5 w-9 rounded-full transition-colors ${checked ? 'bg-primary' : 'bg-cardMuted'}`}
      >
        <span className={`block h-4 w-4 rounded-full bg-white transition-transform ${checked ? 'translate-x-4' : 'translate-x-0.5'}`} />
      </button>
    </div>
  );
}
