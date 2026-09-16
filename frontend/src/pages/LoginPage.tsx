import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthProvider';

export default function LoginPage() {
  const { login, isAuthenticated, isDevBypass } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (isAuthenticated) navigate('/overview', { replace: true });
  }, [isAuthenticated, navigate]);

  if (isAuthenticated) return null;

  return (
    <div className="relative flex h-full items-center justify-center bg-page">
      <div className="pointer-events-none absolute h-96 w-96 rounded-full bg-primary/20 blur-[120px]" />
      <div className="relative z-10 w-full max-w-sm rounded-xl border border-border bg-card p-8 text-center shadow-2xl">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-lg border border-primary/50 bg-primary/10 text-primary">
          <svg viewBox="0 0 24 24" width="22" height="22" fill="currentColor"><path d="M13 2L3 14h7l-1 8 10-12h-7l1-8z" /></svg>
        </div>
        <h1 className="text-lg font-bold tracking-wide">PROJECT X-RAY</h1>
        <p className="mt-1 text-xs text-text-muted">See the blast radius before you ship.</p>

        <div className="my-6 border-t border-border" />

        <button
          onClick={login}
          className="flex w-full items-center justify-center gap-2 rounded-md bg-primary py-2.5 text-sm font-medium text-white hover:bg-primary-hover"
        >
          <span>&#8862;</span> Continue with Microsoft
        </button>
        <p className="mt-6 text-[11px] text-text-muted">
          {isDevBypass
            ? 'Dev mode: no Azure AD App Registration configured yet — signing in uses a local bypass user.'
            : 'Secured by Azure Active Directory. Authorized enterprise use only.'}
        </p>
      </div>
    </div>
  );
}
