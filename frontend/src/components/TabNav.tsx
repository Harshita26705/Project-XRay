export type TabId = 'setup' | 'graph' | 'xray' | 'pr' | 'security';

interface Tab {
  id: TabId;
  label: string;
  hint: string;
  disabled?: boolean;
  badge?: number;
}

interface Props {
  tabs: Tab[];
  active: TabId;
  onChange: (id: TabId) => void;
}

export default function TabNav({ tabs, active, onChange }: Props) {
  return (
    <nav className="border-b border-slate-800 bg-slate-900/80 backdrop-blur">
      <div className="flex gap-1 overflow-x-auto px-2 sm:px-4">
        {tabs.map((tab) => {
          const isActive = tab.id === active;
          return (
            <button
              key={tab.id}
              type="button"
              disabled={tab.disabled}
              onClick={() => onChange(tab.id)}
              title={tab.hint}
              className={`relative shrink-0 whitespace-nowrap border-b-2 px-3 py-2.5 text-sm font-medium transition-colors sm:px-4 ${
                isActive
                  ? 'border-amber-500 text-slate-50'
                  : 'border-transparent text-slate-400 hover:border-slate-600 hover:text-slate-200'
              } ${tab.disabled ? 'cursor-not-allowed opacity-40 hover:border-transparent hover:text-slate-400' : ''}`}
            >
              {tab.label}
              {!!tab.badge && (
                <span className="ml-1.5 rounded-full bg-red-600 px-1.5 py-0.5 text-[10px] font-semibold text-white">
                  {tab.badge}
                </span>
              )}
            </button>
          );
        })}
      </div>
    </nav>
  );
}
