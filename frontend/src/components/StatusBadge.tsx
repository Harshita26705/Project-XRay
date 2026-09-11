import clsx from 'clsx';

export function StatusBadge({ status, className }: { status: string; className?: string }) {
  const map: Record<string, string> = {
    CONNECTED: 'bg-risk-safe/15 text-risk-safe border-risk-safe/40',
    NOT_CONNECTED: 'bg-risk-unknown/15 text-risk-unknown border-risk-unknown/40',
    OPEN: 'bg-risk-critical/15 text-risk-critical border-risk-critical/40',
    RESOLVED: 'bg-risk-safe/15 text-risk-safe border-risk-safe/40',
    ANALYZED: 'bg-risk-safe/15 text-risk-safe border-risk-safe/40',
    PENDING: 'bg-risk-risky/15 text-risk-risky border-risk-risky/40',
    COMPLETED: 'bg-risk-safe/15 text-risk-safe border-risk-safe/40',
    FAILED: 'bg-risk-critical/15 text-risk-critical border-risk-critical/40',
    RUNNING: 'bg-primary/15 text-primary border-primary/40',
    CONFIGURED: 'bg-risk-unknown/15 text-risk-unknown border-risk-unknown/40'
  };
  return (
    <span
      className={clsx(
        'inline-flex items-center rounded border px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide',
        map[status] ?? 'bg-card border-border text-text-secondary',
        className
      )}
    >
      {status.replace(/_/g, ' ')}
    </span>
  );
}
