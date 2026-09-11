import clsx from 'clsx';

export function ProgressBar({ percent, className }: { percent: number; className?: string }) {
  const clamped = Math.max(0, Math.min(100, percent));
  return (
    <div className={clsx('h-1.5 w-full overflow-hidden rounded-full bg-cardMuted', className)}>
      <div className="h-full rounded-full bg-primary transition-all duration-500" style={{ width: `${clamped}%` }} />
    </div>
  );
}
