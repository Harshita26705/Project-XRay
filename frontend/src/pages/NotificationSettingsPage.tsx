import { useEffect, useState } from 'react';
import { api } from '../api/endpoints';
import type { NotificationChannelResponse, NotificationRuleResponse } from '../api/types';
import { Card } from '../components/Card';
import { Button } from '../components/Button';
import { StatusBadge } from '../components/StatusBadge';

export default function NotificationSettingsPage() {
  const [channels, setChannels] = useState<NotificationChannelResponse[]>([]);
  const [rules, setRules] = useState<NotificationRuleResponse[]>([]);

  const load = () => {
    void api.listNotificationChannels().then(setChannels);
    void api.listNotificationRules().then(setRules);
  };
  useEffect(load, []);

  const toggle = async (ruleId: string, current: boolean) => {
    await api.updateNotificationRule(ruleId, !current);
    load();
  };

  const eventTypes = [...new Set(rules.map((r) => r.eventType))];

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">Notification Settings</h1>
        <p className="mt-1 text-sm text-text-secondary">Manage alert streams, webhooks, and granular event mapping across connected channels.</p>
      </div>

      <Card className="p-4">
        <h3 className="mb-3 text-sm font-semibold">Notification Channels</h3>
        <div className="grid grid-cols-3 gap-3">
          {channels.map((c) => (
            <div key={c.notificationChannelId} className="rounded border border-border p-3">
              <p className="text-sm font-medium">{c.displayName}</p>
              <StatusBadge status={c.isEnabled ? 'CONNECTED' : 'CONFIGURED'} className="mt-1" />
            </div>
          ))}
        </div>
      </Card>

      <Card className="p-4">
        <h3 className="mb-3 text-sm font-semibold">Granular Event Rules</h3>
        <table className="w-full text-left text-sm">
          <thead>
            <tr className="border-b border-border text-[11px] uppercase text-text-muted">
              <th className="py-2 font-medium">Trigger Event</th>
              {channels.map((c) => (
                <th key={c.notificationChannelId} className="py-2 text-center font-medium">{c.channelType}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {eventTypes.map((eventType) => (
              <tr key={eventType} className="border-b border-border/60 last:border-0">
                <td className="py-2.5">{eventType.replace(/_/g, ' ')}</td>
                {channels.map((c) => {
                  const rule = rules.find((r) => r.eventType === eventType && r.channelId === c.notificationChannelId);
                  return (
                    <td key={c.notificationChannelId} className="py-2.5 text-center">
                      {rule && (
                        <button
                          onClick={() => toggle(rule.notificationRuleId, rule.isEnabled)}
                          className={`h-5 w-9 rounded-full transition-colors ${rule.isEnabled ? 'bg-primary' : 'bg-cardMuted'}`}
                        >
                          <span className={`block h-4 w-4 rounded-full bg-white transition-transform ${rule.isEnabled ? 'translate-x-4' : 'translate-x-0.5'}`} />
                        </button>
                      )}
                    </td>
                  );
                })}
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      <div className="flex gap-2">
        <Button variant="primary">Save Changes</Button>
        <Button>Cancel</Button>
      </div>
    </div>
  );
}
