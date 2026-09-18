import { useEffect, useMemo, useState } from 'react';
import { api } from '../api/endpoints';
import type { AiConfigurationResponse, OrganizationSettingsResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { Loader } from '../components/Loader';
import { StatusBadge } from '../components/StatusBadge';
import { useProjects } from '../state/ProjectContext';

const NAV = ['General', 'AI Settings', 'Notifications'] as const;
type Tab = (typeof NAV)[number];

export default function SettingsPage() {
  const [settings, setSettings] = useState<OrganizationSettingsResponse | null>(null);
  const [savedSettings, setSavedSettings] = useState<OrganizationSettingsResponse | null>(null);
  const [aiConfig, setAiConfig] = useState<AiConfigurationResponse | null>(null);
  const [activeTab, setActiveTab] = useState<Tab>('General');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [savingAi, setSavingAi] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { projects } = useProjects();

  const [aiForm, setAiForm] = useState({
    provider: 'Google Gemini',
    activeModel: 'gemini-2.5-flash',
    temperature: 0.2,
    maxTokens: 200,
    systemPromptOverride: 'You are a precise, evidence-bound software analysis assistant.'
  });

  useEffect(() => {
    void (async () => {
      try {
        const [orgSettings, aiSettings] = await Promise.all([
          api.getSettings(),
          api.getAiConfiguration()
        ]);
        setSettings(orgSettings);
        setSavedSettings(orgSettings);
        setAiConfig(aiSettings);
        setAiForm({
          provider: aiSettings.provider ?? 'Google Gemini',
          activeModel: aiSettings.activeModel ?? 'gemini-2.5-flash',
          temperature: 0.2,
          maxTokens: 200,
          systemPromptOverride: 'You are a precise, evidence-bound software analysis assistant.'
        });
      } catch (reason: unknown) {
        setError(reason instanceof Error ? reason.message : 'Unable to load settings.');
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const save = async () => {
    if (!settings) return;
    setSaving(true);
    setMessage(null);
    setError(null);
    try {
      const updated = await api.updateSettings(settings);
      setSettings(updated);
      setSavedSettings(updated);
      setMessage('Settings saved.');
    } catch (reason: unknown) {
      setError(reason instanceof Error ? reason.message : 'Unable to save settings.');
    } finally {
      setSaving(false);
    }
  };

  const saveAi = async () => {
    setSavingAi(true);
    setMessage(null);
    setError(null);
    try {
      const updated = await api.updateAiConfiguration({
        provider: aiForm.provider,
        activeModel: aiForm.activeModel,
        temperature: aiForm.temperature,
        maxTokens: aiForm.maxTokens,
        systemPromptOverride: aiForm.systemPromptOverride
      });
      setAiConfig(updated);
      setMessage('AI settings saved.');
    } catch (reason: unknown) {
      setError(reason instanceof Error ? reason.message : 'Unable to save AI settings.');
    } finally {
      setSavingAi(false);
    }
  };

  const cancel = () => {
    if (savedSettings) setSettings(savedSettings);
    setMessage(null);
    setError(null);
  };

  const notificationRules = useMemo(() => [
    { eventType: 'Analysis Completed', channel: 'Email Digest' },
    { eventType: 'Critical Security Finding', channel: 'Teams Alert' },
    { eventType: 'Project Re-indexed', channel: 'Slack' }
  ], []);

  const renderContent = () => {
    if (activeTab === 'AI Settings') {
      return (
        <div className="space-y-5">
          <Card className="p-4">
            <div className="mb-4 grid grid-cols-4 gap-4 text-xs">
              <div>
                <p className="text-text-muted">COGNITIVE PROVIDER</p>
                <p className="mt-1 text-sm font-semibold">{aiConfig?.provider ?? 'Google Gemini'}</p>
              </div>
              <div>
                <p className="text-text-muted">DEPLOYMENT STATUS</p>
                <p className="mt-1"><StatusBadge status={aiConfig?.deploymentStatus ?? 'NOT_CONFIGURED'} /></p>
              </div>
              <div>
                <p className="text-text-muted">ACTIVE MODEL</p>
                <p className="mt-1 font-mono text-sm">{aiConfig?.activeModel ?? aiForm.activeModel}</p>
              </div>
              <div>
                <p className="text-text-muted">KNOWLEDGE LAYER</p>
                <p className="mt-1 text-sm font-semibold">{aiConfig?.knowledgeLayer ?? 'Azure AI Search'}</p>
              </div>
            </div>
          </Card>

          <div className="grid grid-cols-2 gap-5">
            <Card className="p-4">
              <h3 className="mb-2 flex items-center gap-1.5 text-sm font-semibold text-risk-risky">Core Integration Philosophy</h3>
              <p className="text-sm text-text-secondary">
                Foundry model reasoning translates structural telemetry into narrative context. The underlying dependency graph remains authoritative and is never inferred from AI output.
                <span className="mt-2 block text-risk-risky">The AI toggle below is per-run; the model/provider configuration is global.</span>
              </p>
            </Card>
            <Card className="p-4">
              <h3 className="mb-3 text-sm font-semibold">Cognitive Inference Pipeline</h3>
              <div className="flex items-center gap-2 text-xs text-text-secondary">
                <PipelineStep label="1. Dependency Graph" />
                <span>&rarr;</span>
                <PipelineStep label="2. Trace Collection" />
                <span>&rarr;</span>
                <PipelineStep label="3. AI Knowledge" />
                <span>&rarr;</span>
                <PipelineStep label="4. Cognitive Output" />
              </div>
            </Card>
          </div>

          <Card className="p-4">
            <h3 className="mb-4 text-sm font-semibold">Model Configuration</h3>
            <div className="grid grid-cols-2 gap-4">
              <label className="block text-xs text-text-secondary">
                Provider
                <select
                  value={aiForm.provider}
                  onChange={(event) => setAiForm({ ...aiForm, provider: event.target.value })}
                  className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm"
                >
                  <option value="Google Gemini">Google Gemini</option>
                  <option value="Mock">Deterministic Fallback</option>
                </select>
              </label>

              <label className="block text-xs text-text-secondary">
                Active model
                <select
                  value={aiForm.activeModel}
                  onChange={(event) => setAiForm({ ...aiForm, activeModel: event.target.value })}
                  className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm"
                >
                  <option value="gemini-2.5-flash">gemini-2.5-flash</option>
                  <option value="gemini-2.5-pro">gemini-2.5-pro</option>
                  <option value="gemini-2.0-flash-lite">gemini-2.0-flash-lite</option>
                </select>
              </label>

              <label className="block text-xs text-text-secondary">
                Temperature
                <input
                  type="number"
                  min={0}
                  max={1}
                  step={0.1}
                  value={aiForm.temperature}
                  onChange={(event) => setAiForm({ ...aiForm, temperature: Number(event.target.value) })}
                  className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm"
                />
              </label>

              <label className="block text-xs text-text-secondary">
                Max output tokens
                <input
                  type="number"
                  min={64}
                  max={4096}
                  step={16}
                  value={aiForm.maxTokens}
                  onChange={(event) => setAiForm({ ...aiForm, maxTokens: Number(event.target.value) })}
                  className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm"
                />
              </label>
            </div>

            <label className="mt-4 block text-xs text-text-secondary">
              System prompt override
              <textarea
                value={aiForm.systemPromptOverride}
                onChange={(event) => setAiForm({ ...aiForm, systemPromptOverride: event.target.value })}
                rows={5}
                className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm"
              />
            </label>

            <div className="mt-4 flex items-center justify-between rounded border border-border bg-cardMuted px-3 py-2 text-sm">
              <div>
                <div className="font-medium text-text-primary">Analysis defaults</div>
                <div className="text-xs text-text-muted">This toggle is per-run; AI provider and model settings are global.</div>
              </div>
              <ToggleRow
                label="Include AI explanation"
                checked={settings?.includeAiExplanation ?? false}
                onChange={(v) => settings && setSettings({ ...settings, includeAiExplanation: v })}
              />
            </div>

            <div className="mt-4 flex gap-2">
              <Button variant="primary" onClick={() => void saveAi()} disabled={savingAi}>{savingAi ? 'Saving...' : 'Save AI Settings'}</Button>
            </div>
          </Card>
        </div>
      );
    }

    if (activeTab === 'Notifications') {
      return (
        <div className="space-y-5">
          <Card className="p-4">
            <h3 className="mb-3 text-sm font-semibold">Notification Channels</h3>
            <div className="space-y-2">
              {notificationRules.map((rule) => (
                <div key={rule.eventType} className="flex items-center justify-between rounded border border-border bg-cardMuted px-3 py-2 text-sm">
                  <span className="text-text-secondary">{rule.eventType}</span>
                  <span className="font-medium text-text-primary">{rule.channel}</span>
                </div>
              ))}
            </div>
          </Card>
        </div>
      );
    }

    return (
      <div className="space-y-5">
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
          <h3 className="mb-3 text-sm font-semibold">Project Defaults</h3>
          <label className="block text-xs text-text-secondary">
            Default target project
            <select
              value={settings?.defaultTargetProjectId ?? ''}
              onChange={(event) => settings && setSettings({ ...settings, defaultTargetProjectId: event.target.value || null })}
              className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-sm"
            >
              <option value="">No default project</option>
              {projects.map((project) => <option key={project.projectId} value={project.projectId}>{project.name}</option>)}
            </select>
          </label>
        </Card>

        <Card className="p-4">
          <h3 className="mb-3 text-sm font-semibold">Analysis Defaults</h3>
          <ToggleRow label="Include security scan" checked={settings?.includeSecurityScan ?? false} onChange={(v) => settings && setSettings({ ...settings, includeSecurityScan: v })} />
          <ToggleRow label="Include AI explanation" checked={settings?.includeAiExplanation ?? false} onChange={(v) => settings && setSettings({ ...settings, includeAiExplanation: v })} />
          <ToggleRow label="Auto-analyze PRs" checked={settings?.autoAnalyzePullRequests ?? false} onChange={(v) => settings && setSettings({ ...settings, autoAnalyzePullRequests: v })} />
        </Card>

        <div className="flex gap-2">
          <Button variant="primary" onClick={() => void save()} disabled={saving || !settings}>{saving ? 'Saving...' : 'Save Changes'}</Button>
          <Button onClick={cancel} disabled={saving || !settings}>Cancel</Button>
        </div>
      </div>
    );
  };

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Settings</h1>
        <p className="mt-1 text-sm text-text-secondary">Configure organization preferences, analysis standards, and AI defaults.</p>
      </div>

      {error && <div className="rounded-md border border-risk-critical/40 bg-risk-critical/10 p-3 text-sm text-risk-critical">{error}</div>}
      {message && <div className="rounded-md border border-risk-safe/40 bg-risk-safe/10 p-3 text-sm text-risk-safe">{message}</div>}

      {loading ? <Loader label="Loading settings..." /> : (
        <div className="grid grid-cols-5 gap-6">
          <div className="space-y-1 text-sm">
            {NAV.map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setActiveTab(item)}
                className={`block w-full rounded px-3 py-1.5 text-left ${activeTab === item ? 'bg-primary-muted text-white' : 'text-text-secondary hover:bg-cardMuted'}`}
              >
                {item}
              </button>
            ))}
          </div>

          <div className="col-span-4">{renderContent()}</div>
        </div>
      )}
    </div>
  );
}

function ToggleRow({ label, checked, onChange }: { label: string; checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <div className="flex items-center justify-between border-b border-border/60 py-2 text-sm last:border-0">
      <span className="text-text-secondary">{label}</span>
      <button
        type="button"
        onClick={() => onChange(!checked)}
        className={`h-5 w-9 rounded-full transition-colors ${checked ? 'bg-primary' : 'bg-cardMuted'}`}
      >
        <span className={`block h-4 w-4 rounded-full bg-white transition-transform ${checked ? 'translate-x-4' : 'translate-x-0.5'}`} />
      </button>
    </div>
  );
}

function PipelineStep({ label }: { label: string }) {
  return <span className="rounded border border-border bg-cardMuted px-2 py-1">{label}</span>;
}
