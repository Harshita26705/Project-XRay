import clsx from 'clsx';

export type RiskLevel = 'CRITICAL' | 'RISKY' | 'SAFE' | 'UNKNOWN';

const STYLES: Record<RiskLevel, string> = {
  CRITICAL: 'bg-risk-critical/15 text-risk-critical border-risk-critical/40',
  RISKY: 'bg-risk-risky/15 text-risk-risky border-risk-risky/40',
  SAFE: 'bg-risk-safe/15 text-risk-safe border-risk-safe/40',
  UNKNOWN: 'bg-risk-unknown/15 text-risk-unknown border-risk-unknown/40'
};

const LABELS: Record<RiskLevel, string> = {
  CRITICAL: 'Critical',
  RISKY: 'Risky',
  SAFE: 'Safe',
  UNKNOWN: 'Unknown'
};

export function RiskBadge({ level, className }: { level: string | null | undefined; className?: string }) {
  const normalized = (level?.toUpperCase() as RiskLevel) ?? 'UNKNOWN';
  const key = STYLES[normalized] ? normalized : 'UNKNOWN';
  return (
    <span
      className={clsx(
        'inline-flex items-center rounded border px-2 py-0.5 text-[11px] font-semibold uppercase tracking-wide',
        STYLES[key],
        className
      )}
    >
      {LABELS[key]}
    </span>
  );
}
