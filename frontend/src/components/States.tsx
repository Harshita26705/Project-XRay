import type { ReactNode } from 'react';

export function EmptyState({ icon, title, message, action }: { icon?: ReactNode; title: string; message: string; action?: ReactNode }) {
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-border bg-card px-6 py-12 text-center">
      {icon && <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-full bg-cardMuted text-text-secondary">{icon}</div>}
      <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
      <p className="mt-1 max-w-sm text-xs text-text-muted">{message}</p>
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}

export function ErrorState({ icon, title, message, action, tone = 'critical' }: { icon?: ReactNode; title: string; message: string; action?: ReactNode; tone?: 'critical' | 'risky' | 'unknown' }) {
  const toneColor = tone === 'critical' ? 'text-risk-critical bg-risk-critical/10' : tone === 'risky' ? 'text-risk-risky bg-risk-risky/10' : 'text-risk-unknown bg-risk-unknown/10';
  return (
    <div className="flex flex-col items-center justify-center rounded-lg border border-border bg-card px-6 py-12 text-center">
      <div className={`mb-3 flex h-12 w-12 items-center justify-center rounded-full ${toneColor}`}>{icon}</div>
      <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
      <p className="mt-1 max-w-sm text-xs text-text-muted">{message}</p>
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
