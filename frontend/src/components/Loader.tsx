interface LoaderProps {
  label?: string;
  size?: 'sm' | 'md' | 'lg';
  /** Fills the parent's height and centers the loader — use for full-page/panel loading states. */
  fullHeight?: boolean;
  className?: string;
}

const SIZE_PX: Record<NonNullable<LoaderProps['size']>, number> = { sm: 18, md: 26, lg: 36 };

/** Shared "infinity" loading indicator used for every loading state across the app. */
export function Loader({ label, size = 'md', fullHeight = false, className = '' }: LoaderProps) {
  const height = SIZE_PX[size];
  return (
    <div
      role="status"
      aria-live="polite"
      className={`flex flex-col items-center justify-center gap-3 ${fullHeight ? 'h-full' : 'py-10'} ${className}`}
    >
      <svg width={height * 2} height={height} viewBox="0 0 80 40" fill="none" xmlns="http://www.w3.org/2000/svg" aria-hidden="true">
        <path
          d="M10 20C10 11.2 17.2 4 26 4C39 4 41 36 54 36C62.8 36 70 28.8 70 20C70 11.2 62.8 4 54 4C41 4 39 36 26 36C17.2 36 10 28.8 10 20Z"
          stroke="#22273d"
          strokeWidth="5"
        />
        <path
          d="M10 20C10 11.2 17.2 4 26 4C39 4 41 36 54 36C62.8 36 70 28.8 70 20C70 11.2 62.8 4 54 4C41 4 39 36 26 36C17.2 36 10 28.8 10 20Z"
          stroke="#3b82f6"
          strokeWidth="5"
          strokeLinecap="round"
          strokeDasharray="40 130"
          className="xray-loader-path"
        />
      </svg>
      {label && <p className="text-xs text-text-muted">{label}</p>}
      <span className="sr-only">Loading</span>
    </div>
  );
}
