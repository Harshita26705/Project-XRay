import clsx from 'clsx';
import type { ReactNode } from 'react';

export function Card({ children, className }: { children: ReactNode; className?: string }) {
  return <div className={clsx('rounded-lg border border-border bg-card', className)}>{children}</div>;
}

export function CardHeader({ title, subtitle, action }: { title: string; subtitle?: string; action?: ReactNode }) {
  return (
    <div className="flex items-start justify-between border-b border-border px-5 py-4">
      <div>
        <h3 className="text-sm font-semibold text-text-primary">{title}</h3>
        {subtitle && <p className="mt-0.5 text-xs text-text-muted">{subtitle}</p>}
      </div>
      {action}
    </div>
  );
}

export function StatCard({ label, value, trend, accent }: { label: string; value: ReactNode; trend?: string; accent?: 'critical' | 'risky' | 'safe' }) {
  const accentColor = accent === 'critical' ? 'text-risk-critical' : accent === 'risky' ? 'text-risk-risky' : accent === 'safe' ? 'text-risk-safe' : 'text-text-primary';
  return (
    <Card className="p-4">
      <p className="text-[11px] font-medium uppercase tracking-wide text-text-muted">{label}</p>
      <div className="mt-2 flex items-baseline gap-2">
        <span className={clsx('text-2xl font-semibold', accentColor)}>{value}</span>
        {trend && <span className="text-xs font-medium text-risk-critical">{trend}</span>}
      </div>
    </Card>
  );
}
