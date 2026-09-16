import { useEffect, useState } from 'react';
import { api } from '../api/endpoints';
import type { IntegrationConnectionResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { StatusBadge } from '../components/StatusBadge';

const PROVIDERS: { code: string; name: string; purpose: string; placeholder: string }[] = [
  { code: 'AZURE_DEVOPS', name: 'Azure DevOps', purpose: 'Repositories + Pull Requests + Work Items', placeholder: 'https://dev.azure.com/your-organization' },
  { code: 'SQL_SERVER', name: 'SQL Server DB', purpose: 'Database Metadata + Stored Procedures', placeholder: 'https://server.example.com' },
  { code: 'MICROSOFT_FOUNDRY', name: 'Microsoft Foundry', purpose: 'AI Reasoning + Language Logs', placeholder: 'https://resource.openai.azure.com' },
  { code: 'AZURE_AI_SEARCH', name: 'Azure AI Search', purpose: 'Project Knowledge Retrieval + Query Slices', placeholder: 'https://search.example.search.windows.net' },
  { code: 'MICROSOFT_TEAMS', name: 'Microsoft Teams', purpose: 'Notifications + Alert Streams', placeholder: 'https://outlook.office.com/webhook/...' },
  { code: 'POWER_AUTOMATE', name: 'Power Automate', purpose: 'Workflow Automation + Orchestration', placeholder: 'https://prod-00.westeurope.logic.azure.com/...' }
];

export default function IntegrationsPage() {
  const [connections, setConnections] = useState<IntegrationConnectionResponse[]>([]);
  const [baseUrls, setBaseUrls] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [busyProvider, setBusyProvider] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [feedback, setFeedback] = useState<Record<string, string>>({});

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      setConnections(await api.listIntegrations());
    } catch (reason: unknown) {
      setError(reason instanceof Error ? reason.message : 'Unable to load integrations.');
    } finally {
      setLoading(false);
    }
  };
  useEffect(() => {
    void load();
  }, []);

  const test = async (provider: typeof PROVIDERS[number]) => {
    setBusyProvider(provider.code);
    setError(null);
    setFeedback((current) => ({ ...current, [provider.code]: '' }));
    try {
      let connection = connections.find((item) => item.provider === provider.code);
      connection = await api.upsertIntegration({
        provider: provider.code,
        displayName: provider.name,
        externalBaseUrl: baseUrls[provider.code] ?? connection?.externalBaseUrl ?? undefined
      });
      const tested = await api.testIntegration(connection.integrationConnectionId);
      setConnections((current) => [...current.filter((item) => item.provider !== provider.code), tested]);
      setFeedback((current) => ({ ...current, [provider.code]: tested.status === 'CONNECTED' ? 'Connection successful' : 'Connection failed' }));
    } catch (reason: unknown) {
      setFeedback((current) => ({ ...current, [provider.code]: reason instanceof Error ? reason.message : 'Connection test failed.' }));
    } finally {
      setBusyProvider(null);
    }
  };

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Integrations</h1>
        <p className="mt-1 text-sm text-text-secondary">Manage and configure data connectors, code hosts, and enterprise AI engines.</p>
      </div>

      {error && <div className="rounded-md border border-risk-critical/40 bg-risk-critical/10 p-3 text-sm text-risk-critical">{error}</div>}

      {loading ? <div className="text-sm text-text-muted">Loading integrations...</div> : <div className="grid grid-cols-2 gap-5">
        {PROVIDERS.map((p) => {
          const connection = connections.find((c) => c.provider === p.code);
          return (
            <Card key={p.code} className="p-4">
              <div className="flex items-center justify-between">
                <span className="font-semibold">{p.name}</span>
                <StatusBadge status={connection?.status ?? 'NOT_CONNECTED'} />
              </div>
              <p className="mt-1 text-xs text-text-muted">{p.purpose}</p>
              <label className="mt-3 block text-[11px] text-text-muted">
                Endpoint
                <input
                  value={baseUrls[p.code] ?? connection?.externalBaseUrl ?? ''}
                  onChange={(event) => setBaseUrls((current) => ({ ...current, [p.code]: event.target.value }))}
                  placeholder={p.placeholder}
                  className="mt-1 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-xs"
                />
              </label>
              <div className="mt-3">
                <Button onClick={() => void test(p)} disabled={busyProvider !== null}>{busyProvider === p.code ? 'Testing...' : 'Save & Test Connection'}</Button>
                {feedback[p.code] && <p className={`mt-2 text-xs ${feedback[p.code] === 'Connection successful' ? 'text-risk-safe' : 'text-risk-risky'}`}>{feedback[p.code]}</p>}
                {connection?.lastTestedAtUtc && <p className="mt-1 text-[10px] text-text-muted">Last tested {new Date(connection.lastTestedAtUtc).toLocaleString()}</p>}
              </div>
            </Card>
          );
        })}
      </div>}
    </div>
  );
}
