import { useEffect, useState } from 'react';
import { api } from '../api/endpoints';
import type { AiConfigurationResponse } from '../api/types';
import { Card } from '../components/Card';
import { StatusBadge } from '../components/StatusBadge';

export default function AiConfigurationPage() {
  const [config, setConfig] = useState<AiConfigurationResponse | null>(null);

  useEffect(() => {
    void api.getAiConfiguration().then(setConfig);
  }, []);

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-xl font-bold">AI Configuration</h1>
        <p className="mt-1 text-sm text-text-secondary">Orchestrate cognitive models, knowledge bases, and architectural pipelines.</p>
      </div>

      <Card className="p-4">
        <div className="grid grid-cols-4 gap-4 text-xs">
          <div><p className="text-text-muted">COGNITIVE PROVIDER</p><p className="mt-1 text-sm font-semibold">{config?.provider ?? '—'}</p></div>
          <div><p className="text-text-muted">DEPLOYMENT STATUS</p><p className="mt-1"><StatusBadge status={config?.deploymentStatus ?? 'NOT_CONFIGURED'} /></p></div>
          <div><p className="text-text-muted">ACTIVE MODEL</p><p className="mt-1 font-mono text-sm">{config?.activeModel ?? 'not configured'}</p></div>
          <div><p className="text-text-muted">KNOWLEDGE LAYER</p><p className="mt-1 text-sm font-semibold">{config?.knowledgeLayer ?? '—'}</p></div>
        </div>
      </Card>

      <div className="grid grid-cols-2 gap-5">
        <Card className="p-4">
          <h3 className="mb-2 flex items-center gap-1.5 text-sm font-semibold text-risk-risky">Core Integration Philosophy</h3>
          <p className="text-sm text-text-secondary">
            Foundry model reasoning translates complex structural telemetry into native narrative logs. The underlying
            dependency trees and impact matrices remain strictly bounded by X-Ray's deterministic validation engine.{' '}
            <span className="text-risk-risky">The AI pipeline never generates nor predicts structural relationships.</span>
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

      <h3 className="text-sm font-semibold">Active Remediations &amp; Templates</h3>
      <div className="grid grid-cols-3 gap-5">
        <Card className="p-4"><h4 className="text-sm font-semibold">Model Settings</h4><p className="mt-1 text-xs text-text-muted">Configure hyper-parameters, token constraints, and temperature limits.</p></Card>
        <Card className="p-4"><h4 className="text-sm font-semibold">Prompt Templates</h4><p className="mt-1 text-xs text-text-muted">Fine-tune system generation payloads, custom instructions, and schema rules.</p></Card>
        <Card className="p-4"><h4 className="text-sm font-semibold">Usage &amp; Rate Limits</h4><p className="mt-1 text-xs text-text-muted">Monitor quota pools, billing logs, and daily usage statistics.</p></Card>
      </div>
    </div>
  );
}

function PipelineStep({ label }: { label: string }) {
  return <span className="rounded border border-border bg-cardMuted px-2 py-1">{label}</span>;
}
