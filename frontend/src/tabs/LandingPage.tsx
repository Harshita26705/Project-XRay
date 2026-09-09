interface Props {
  onGetStarted: () => void;
}

function IconGraph() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-6 w-6">
      <circle cx="6" cy="6" r="2.5" />
      <circle cx="18" cy="6" r="2.5" />
      <circle cx="12" cy="18" r="2.5" />
      <path d="M8.2 7.2 10 16M15.8 7.2 14 16M8.5 6h7" />
    </svg>
  );
}

function IconShield() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-6 w-6">
      <path d="M12 3l7 3v5c0 4.5-3 8-7 10-4-2-7-5.5-7-10V6l7-3z" />
      <path d="M9 12l2 2 4-4" />
    </svg>
  );
}

function IconPulse() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-6 w-6">
      <path d="M3 12h4l2-7 4 14 2-7h6" />
    </svg>
  );
}

function IconGit() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-6 w-6">
      <circle cx="6" cy="6" r="2.2" />
      <circle cx="6" cy="18" r="2.2" />
      <circle cx="18" cy="9" r="2.2" />
      <path d="M6 8.2V15.8M6 9c0 4 3 5.5 6 5.5M18 11.2V13" />
    </svg>
  );
}

function IconDoc() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4">
      <path d="M7 3h7l4 4v14H7z" />
      <path d="M14 3v4h4M9 12h6M9 16h6" />
    </svg>
  );
}

function IconHistory() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4">
      <path d="M3 12a9 9 0 1 0 3-6.7" />
      <path d="M3 4v5h5M12 8v4l3 2" />
    </svg>
  );
}

function IconChart() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" className="h-4 w-4">
      <path d="M4 20V10M12 20V4M20 20v-7" />
    </svg>
  );
}

const FEATURES = [
  {
    icon: IconGit,
    title: 'Set Up',
    body: 'Point X-Ray at any repository and build its authoritative dependency graph in seconds.'
  },
  {
    icon: IconGraph,
    title: 'Dependency Graph',
    body: 'Explore every component, service, and data dependency with an interactive, explorable graph.'
  },
  {
    icon: IconPulse,
    title: 'X-Ray Analysis',
    body: 'Link a DevOps work item or write a requirement and see exactly what it will impact.'
  },
  {
    icon: IconDoc,
    title: 'PR Report',
    body: 'Paste a pull request diff and get a blast-radius report before you merge.'
  },
  {
    icon: IconShield,
    title: 'Security',
    body: 'Surface secrets, vulnerable dependencies, and risky patterns alongside every change.'
  }
];

const STATS = [
  { value: '5', label: 'Analysis stages' },
  { value: '8+', label: 'Relationship types' },
  { value: 'Real-time', label: 'Graph traversal' },
  { value: '100%', label: 'Graph-backed evidence' }
];

export default function LandingPage({ onGetStarted }: Props) {
  return (
    <div className="h-full overflow-y-auto bg-xray-bg text-slate-100">
      <nav className="sticky top-0 z-20 border-b border-xray-main/10 bg-xray-bg/95 px-4 py-4 backdrop-blur sm:px-8">
        <div className="mx-auto flex max-w-6xl items-center justify-between">
          <div className="flex items-center gap-2 text-sm font-bold tracking-tight">
            <span className="flex h-7 w-7 items-center justify-center rounded-md bg-xray-main text-xray-bg shadow-[0_0_0.8rem_rgba(0,240,255,0.5)]">
              X
            </span>
            PROJECT X-RAY
          </div>
          <div className="hidden items-center gap-6 text-sm text-slate-300 sm:flex">
            <span className="border-b-2 border-xray-main pb-1 text-slate-100">Home</span>
            <span className="cursor-default transition-colors hover:text-cyan-400">Features</span>
            <span className="cursor-default transition-colors hover:text-cyan-400">Security</span>
          </div>
          <button
            type="button"
            onClick={onGetStarted}
            className="rounded-full bg-xray-main px-4 py-1.5 text-xs font-semibold text-xray-bg shadow-[0_0_1rem_rgba(0,240,255,0.4)] transition-transform hover:scale-105"
          >
            Launch app
          </button>
        </div>
      </nav>

      <header className="relative flex min-h-[calc(100vh-4rem)] items-center overflow-hidden px-4 py-16 sm:px-8">
        <div className="pointer-events-none absolute right-[-4rem] top-1/2 h-72 w-72 -translate-y-1/2 animate-float rounded-full bg-xray-main opacity-10 blur-3xl sm:h-96 sm:w-96" />
        <div className="mx-auto grid max-w-6xl grid-cols-1 items-center gap-10 lg:grid-cols-2">
          <div className="animate-fadeIn">
            <p className="text-sm font-medium text-cyan-400">Welcome to</p>
            <h1 className="mt-2 text-4xl font-extrabold tracking-tight sm:text-5xl">
              PROJECT <span className="text-cyan-400">X-RAY</span>
            </h1>
            <p className="mt-3 text-xl font-semibold text-slate-200">
              Change Impact Analysis, <span className="text-cyan-400">Reinvented</span>
            </p>
            <p className="mt-4 max-w-lg text-sm leading-relaxed text-slate-400">
              Ingest a repository, build its dependency graph, and know exactly what a
              requirement, pull request, or code change will break — before it ships. The
              dependency graph is authoritative; AI output is explanatory only.
            </p>

            <div className="mt-6 flex items-center gap-3">
              <span className="flex h-9 w-9 items-center justify-center rounded-full border-2 border-xray-main text-cyan-400 transition-all hover:bg-xray-main hover:text-xray-bg hover:shadow-[0_0_1rem_rgba(0,240,255,0.6)]">
                <IconDoc />
              </span>
              <span className="flex h-9 w-9 items-center justify-center rounded-full border-2 border-xray-main text-cyan-400 transition-all hover:bg-xray-main hover:text-xray-bg hover:shadow-[0_0_1rem_rgba(0,240,255,0.6)]">
                <IconHistory />
              </span>
              <span className="flex h-9 w-9 items-center justify-center rounded-full border-2 border-xray-main text-cyan-400 transition-all hover:bg-xray-main hover:text-xray-bg hover:shadow-[0_0_1rem_rgba(0,240,255,0.6)]">
                <IconChart />
              </span>
            </div>

            <button
              type="button"
              onClick={onGetStarted}
              className="mt-8 rounded-full bg-xray-main px-6 py-3 text-sm font-semibold text-xray-bg shadow-[0_0_1rem_rgba(0,240,255,0.6)] transition-transform hover:scale-105"
            >
              Get Started
            </button>
          </div>

          <div className="relative hidden h-80 lg:block">
            <div className="absolute left-6 top-2 w-52 animate-floatCard rounded-2xl border-2 border-xray-main bg-xray-panel p-4 text-center shadow-xl">
              <span className="mx-auto mb-2 flex h-9 w-9 items-center justify-center text-cyan-400">
                <IconGraph />
              </span>
              <div className="text-sm font-semibold text-cyan-400">Dependency Graph</div>
              <div className="text-xs text-slate-400">40+ components mapped</div>
            </div>
            <div className="absolute right-2 top-32 w-52 animate-floatCard rounded-2xl border-2 border-xray-main bg-xray-panel p-4 text-center shadow-xl [animation-delay:0.5s]">
              <span className="mx-auto mb-2 flex h-9 w-9 items-center justify-center text-cyan-400">
                <IconShield />
              </span>
              <div className="text-sm font-semibold text-cyan-400">Safety Check</div>
              <div className="text-xs text-slate-400">Secrets & vuln scanning</div>
            </div>
            <div className="absolute bottom-2 left-24 w-52 animate-floatCard rounded-2xl border-2 border-xray-main bg-xray-panel p-4 text-center shadow-xl [animation-delay:1s]">
              <span className="mx-auto mb-2 flex h-9 w-9 items-center justify-center text-cyan-400">
                <IconPulse />
              </span>
              <div className="text-sm font-semibold text-cyan-400">X-Ray Analysis</div>
              <div className="text-xs text-slate-400">Blast radius in one click</div>
            </div>
          </div>
        </div>
      </header>

      <section className="flex min-h-screen flex-col justify-center bg-xray-panel px-4 py-16 sm:px-8">
        <div className="mx-auto max-w-6xl animate-fadeIn">
          <h2 className="text-center text-3xl font-bold sm:text-4xl">
            Everything you need for a <span className="text-cyan-400">safe change</span>
          </h2>
          <p className="mx-auto mt-2 max-w-xl text-center text-sm text-slate-400">
            Five focused steps take you from a raw repository to a confident, reviewed change.
          </p>
          <div className="mt-10 grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-5">
            {FEATURES.map(({ icon: Icon, title, body }) => (
              <div
                key={title}
                className="rounded-2xl border-2 border-transparent bg-xray-bg p-6 text-center transition-all hover:-translate-y-2 hover:border-xray-main hover:shadow-[0_1rem_3rem_rgba(0,240,255,0.2)]"
              >
                <span className="mx-auto flex h-14 w-14 items-center justify-center text-cyan-400">
                  <Icon />
                </span>
                <h3 className="mt-3 text-base font-semibold text-slate-100">{title}</h3>
                <p className="mt-1 text-xs leading-relaxed text-slate-400">{body}</p>
              </div>
            ))}
          </div>
          <div className="mt-10 flex justify-center">
            <button
              type="button"
              onClick={onGetStarted}
              className="rounded-full bg-xray-main px-6 py-3 text-sm font-semibold text-xray-bg shadow-[0_0_1rem_rgba(0,240,255,0.4)] transition-transform hover:scale-105"
            >
              Get Started
            </button>
          </div>
        </div>
      </section>

      <section className="flex min-h-screen flex-col justify-center bg-xray-bg px-4 py-16 sm:px-8">
        <div className="mx-auto max-w-6xl animate-fadeIn">
          <h2 className="text-center text-3xl font-bold sm:text-4xl">
            About Project <span className="text-cyan-400">X-Ray</span>
          </h2>
          <p className="mx-auto mt-2 max-w-xl text-center text-sm text-slate-400">
            Built for teams that want a repeatable, evidence-based way to review risky changes.
          </p>
          <div className="mt-8 grid grid-cols-2 gap-4 lg:grid-cols-4">
            {STATS.map((stat) => (
              <div
                key={stat.label}
                className="rounded-xl border-l-4 border-xray-main bg-xray-panel py-8 text-center"
              >
                <div className="text-3xl font-bold text-cyan-400">{stat.value}</div>
                <div className="mt-1 text-xs text-slate-400">{stat.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="flex min-h-screen flex-col items-center justify-center border-t-2 border-xray-main bg-gradient-to-br from-xray-panel to-transparent px-4 py-16 sm:px-8">
        <div className="mx-auto max-w-3xl animate-fadeIn text-center">
          <h2 className="text-3xl font-bold sm:text-4xl">Ready to X-Ray your next change?</h2>
          <p className="mx-auto mt-3 max-w-xl text-sm text-slate-300">
            Ingest a repository and get a graph-backed impact report before you ship.
          </p>
          <div className="mt-8 flex flex-wrap justify-center gap-4">
            <button
              type="button"
              onClick={onGetStarted}
              className="rounded-full bg-xray-main px-6 py-3 text-sm font-semibold text-xray-bg shadow-[0_0_1rem_rgba(0,240,255,0.6)] transition-transform hover:scale-105"
            >
              Open the app
            </button>
            <button
              type="button"
              onClick={onGetStarted}
              className="rounded-full border-2 border-xray-main px-6 py-3 text-sm font-semibold text-cyan-400 transition-all hover:bg-xray-main hover:text-xray-bg"
            >
              View security scanning
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}
