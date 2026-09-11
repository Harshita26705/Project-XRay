import clsx from 'clsx';
import type { ButtonHTMLAttributes } from 'react';

type Variant = 'primary' | 'secondary' | 'danger' | 'ghost';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
}

const VARIANT_CLASSES: Record<Variant, string> = {
  primary: 'bg-primary text-white hover:bg-primary-hover disabled:opacity-50',
  secondary: 'bg-cardMuted text-text-primary border border-border hover:border-border-light disabled:opacity-50',
  danger: 'bg-risk-critical/10 text-risk-critical border border-risk-critical/40 hover:bg-risk-critical/20 disabled:opacity-50',
  ghost: 'text-text-secondary hover:text-text-primary hover:bg-cardMuted'
};

export function Button({ variant = 'secondary', className, ...props }: ButtonProps) {
  return (
    <button
      className={clsx(
        'inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors',
        VARIANT_CLASSES[variant],
        className
      )}
      {...props}
    />
  );
}
