import clsx from 'clsx';

export function ProgressBar({ percent, indeterminate, className }: { percent?: number; indeterminate?: boolean; className?: string }) {
  if (indeterminate) {
    return (
      <div className={clsx('h-1.5 w-full overflow-hidden rounded-full bg-cardMuted', className)}>
        <div className="h-full w-1/3 animate-pulse rounded-full bg-primary" />
      </div>
    );
  }

  const clamped = Math.max(0, Math.min(100, percent ?? 0));
  return (
    <div className={clsx('h-1.5 w-full overflow-hidden rounded-full bg-cardMuted', className)}>
      <div className="h-full rounded-full bg-primary transition-all duration-500" style={{ width: `${clamped}%` }} />
    </div>
  );
}
