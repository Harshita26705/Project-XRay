import { NavLink, Outlet, useLocation } from 'react-router-dom';
import clsx from 'clsx';
import { useAuth } from '../auth/AuthProvider';

const NAV_ITEMS = [
  { to: '/overview', label: 'Overview', icon: '\u2302' },
  { to: '/projects', label: 'Projects', icon: '\u25A2' },
  { to: '/analyses', label: 'Analyses', icon: '\u269B' },
  { to: '/changes', label: 'Changes', icon: '\u21C4' },
  { to: '/security', label: 'Security', icon: '\u26E8' },
  { to: '/reports', label: 'Reports', icon: '\u2261' },
  { to: '/integrations', label: 'Integrations', icon: '\u2B21' },
  { to: '/settings', label: 'Settings', icon: '\u2699' }
];

function Sidebar() {
  const { displayName, logout } = useAuth();
  return (
    <aside className="flex w-60 shrink-0 flex-col border-r border-border bg-sidebar">
      <div className="flex items-center gap-2 px-5 py-5">
        <div className="flex h-8 w-8 items-center justify-center rounded-md border border-primary/50 bg-primary/10 text-primary">
          <svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor"><path d="M13 2L3 14h7l-1 8 10-12h-7l1-8z" /></svg>
        </div>
        <div>
          <div className="text-sm font-bold leading-none tracking-wide">X-RAY</div>
          <div className="text-[10px] leading-none text-text-muted mt-1">PROJECT ANALYSIS</div>
        </div>
      </div>

      <nav className="flex-1 space-y-0.5 px-3">
        {NAV_ITEMS.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              clsx(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive ? 'bg-primary-muted text-white' : 'text-text-secondary hover:bg-cardMuted hover:text-text-primary'
              )
            }
          >
            <span className="w-4 text-center">{item.icon}</span>
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="border-t border-border px-4 py-4">
        <button className="mb-3 flex items-center gap-2 text-xs text-text-muted hover:text-text-primary">
          <span>?</span> Help &amp; Docs
        </button>
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-full bg-cardMuted text-xs font-semibold">
            {displayName.slice(0, 1)}
          </div>
          <div className="min-w-0 flex-1">
            <div className="truncate text-xs font-semibold text-text-primary">{displayName}</div>
            <div className="text-[10px] text-text-muted">Enterprise Admin</div>
          </div>
          <button onClick={logout} title="Sign out" className="text-text-muted hover:text-text-primary">
            &#8677;
          </button>
        </div>
      </div>
    </aside>
  );
}

function Topbar() {
  const location = useLocation();
  const crumb = NAV_ITEMS.find((n) => location.pathname.startsWith(n.to))?.label ?? 'X-Ray';
  return (
    <header className="flex h-14 shrink-0 items-center justify-between border-b border-border bg-topbar px-6">
      <div className="flex items-center gap-2 text-sm text-text-secondary">
        <span>Project X-Ray</span>
        <span className="text-text-muted">/</span>
        <span className="font-medium text-text-primary">{crumb}</span>
        <span className="ml-3 rounded border border-primary/40 bg-primary/10 px-2 py-0.5 text-xs text-primary">CGOne</span>
        <span className="rounded border border-border px-2 py-0.5 text-xs text-text-secondary">Development</span>
      </div>
      <div className="flex items-center gap-4">
        <input
          placeholder="Search dependency graph..."
          className="w-64 rounded-md border border-border bg-cardMuted px-3 py-1.5 text-xs text-text-primary placeholder:text-text-muted focus:outline-none focus:ring-1 focus:ring-primary"
        />
        <button className="text-text-secondary hover:text-text-primary">&#128276;</button>
      </div>
    </header>
  );
}

export function AppShell() {
  return (
    <div className="flex h-full">
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <Topbar />
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
