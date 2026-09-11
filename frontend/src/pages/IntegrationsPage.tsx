import { useEffect, useState } from 'react';
import { api } from '../api/endpoints';
import type { IntegrationConnectionResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { StatusBadge } from '../components/StatusBadge';

const PROVIDERS: { code: string; name: string; purpose: string; readOnly?: boolean }[] = [
  { code: 'AZURE_DEVOPS', name: 'Azure DevOps', purpose: 'Repositories + Pull Requests + Work Items' },
  { code: 'SQL_SERVER', name: 'SQL Server DB', purpose: 'Database Metadata + Stored Procedures', readOnly: true },
  { code: 'MICROSOFT_FOUNDRY', name: 'Microsoft Foundry', purpose: 'AI Reasoning + Language Logs' },
  { code: 'AZURE_AI_SEARCH', name: 'Azure AI Search', purpose: 'Project Knowledge Retrieval + Query Slices' },
  { code: 'MICROSOFT_TEAMS', name: 'Microsoft Teams', purpose: 'Notifications + Alert Streams' },
  { code: 'POWER_AUTOMATE', name: 'Power Automate', purpose: 'Workflow Automation + Orchestration' }
];

export default function IntegrationsPage() {
  const [connections, setConnections] = useState<IntegrationConnectionResponse[]>([]);
  const [baseUrls, setBaseUrls] = useState<Record<string, string>>({});

  const load = () => void api.listIntegrations().then(setConnections);
  useEffect(load, []);

  const ensure = async (provider: string, name: string) => {
    let connection = connections.find((c) => c.provider === provider);
    if (!connection) {
      connection = await api.upsertIntegration({ provider, displayName: name, externalBaseUrl: baseUrls[provider] || undefined });
    } else if (baseUrls[provider] !== undefined) {
      connection = await api.upsertIntegration({ provider, displayName: name, externalBaseUrl: baseUrls[provider] || undefined });
    }
    await api.testIntegration(connection.integrationConnectionId);
    load();
  };

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Integrations</h1>
        <p className="mt-1 text-sm text-text-secondary">Manage and configure data connectors, code hosts, and enterprise AI engines.</p>
      </div>

      <div className="grid grid-cols-2 gap-5">
        {PROVIDERS.map((p) => {
          const connection = connections.find((c) => c.provider === p.code);
          return (
            <Card key={p.code} className="p-4">
              <div className="flex items-center justify-between">
                <span className="font-semibold">{p.name}</span>
                <StatusBadge status={connection?.status ?? 'NOT_CONNECTED'} />
              </div>
              <p className="mt-1 text-xs text-text-muted">{p.purpose}</p>
              {p.code === 'AZURE_DEVOPS' && (
                <input
                  value={baseUrls[p.code] ?? connection?.externalBaseUrl ?? ''}
                  onChange={(event) => setBaseUrls((current) => ({ ...current, [p.code]: event.target.value }))}
                  placeholder="https://dev.azure.com/your-organization"
                  className="mt-3 w-full rounded border border-border bg-cardMuted px-2 py-1.5 text-xs"
                />
              )}
              {p.readOnly && <span className="mt-2 inline-block rounded border border-border px-1.5 py-0.5 text-[10px] text-text-secondary">READ-ONLY</span>}
              <div className="mt-3">
                <Button onClick={() => ensure(p.code, p.name)}>Test Connection</Button>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
