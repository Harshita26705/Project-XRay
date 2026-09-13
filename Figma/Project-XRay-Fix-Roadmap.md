# Project X-Ray — Remediation & Hardening Roadmap

This turns the architecture/code review into an executable plan. Each phase is
sequenced so it doesn't block the others — you can ship Phase 1 and 2 without
touching the frontend, and Phase 3 without waiting on the test suite.

Rough total: 6 phases, ~7–9 weeks of solo effort, front-loaded on the stuff
that's cheap and de-risks everything after it.

---

## Phase 0 — Hygiene (½ day)

Quick, zero-risk cleanup. Do this first so the rest of the plan starts from a
clean base.

- [ ] Delete `XRay.Tests/UnitTest1.cs` (empty scaffold test).
- [ ] Scrub the real machine name out of `appsettings.json`'s connection
      string (`IN-SAYYED-ALI-C\SQLEXPRESS` → `localdb` or a placeholder);
      move per-developer connection strings to `appsettings.Development.json`
      or user-secrets so they never hit the repo.
- [ ] Add a `.gitignore`/README note clarifying the `Figma/` folder is a
      scratch/cross-transfer dump, not just design assets — future-you (or a
      teammate) will otherwise assume it's safe to delete wholesale.
- [ ] Confirm `bin/`, `obj/`, `dist/` are actually git-ignored — the zip you
      shared included build output (82 MB in `XRay.Api/bin` alone), which
      means either `.gitignore` is missing entries or these got committed.

**Exit criteria:** repo is clean, no build artifacts tracked, no machine-specific secrets.

---

## Phase 1 — Resolve the Engine/Parsers duplication (2–3 days)

This is the highest-leverage fix: right now there are two competing
"discover repo → parse → build graph" implementations, and it's actively
confusing which one is real.

- [ ] **Decide**: `XRay.Parsers` + `IngestionService` is the one wired into
      `Program.cs` and the one your data model / evidence chain is built
      around. Treat it as canonical.
- [ ] Diff `XRay.Engine`'s `AnalysisPipeline`/`GraphAggregator` against
      `XRay.Parsers` for anything genuinely better (e.g. if the engine's
      SQL-table cross-referencing or namespace handling is more complete,
      port *that logic* into `XRay.Parsers`, don't port the class).
- [ ] Delete `XRay.Engine` and `SemanticGraphEngineTests.cs`, or if you want
      to keep it as a research sandbox, move it out of the solution (e.g.
      `/experiments/`) so it stops appearing as a live project in
      `XRay.sln` and stops implying it's in the runtime path.
- [ ] Update `README.md`'s repository layout table to remove the now-false
      "Deterministic engine" pointer to a project that no longer ships.

**Exit criteria:** one code path builds the graph, `XRay.sln` has no
orphaned projects, README matches reality.

---

## Phase 2 — Correctness fixes in the classifier (3–5 days)

These are the two issues that undercut the "deterministic" claim, which is
the whole value proposition of the product.

- [ ] **Fix the BFS re-enqueue bug** in `AnalysisService.RunDeterministicEngineAsync`:
      track `bestDistance` optimistically *at enqueue time*, not only at
      dequeue time, so a node can't be pushed onto the frontier multiple
      times at the same depth. This turns worst-case near-combinatorial
      blowup on fan-in-heavy graphs into proper O(V+E).
- [ ] **Make path selection deterministic**: when multiple equal-distance
      paths reach a node, pick a stable tiebreaker (e.g. lowest confidence
      first for conservatism, then lexicographic edge ID) instead of
      "whichever the dictionary/group enumeration happened to dequeue
      first." Add a unit test that runs the same graph twice and asserts
      identical `RuleCode`/`Evidence` output both times.
- [ ] **Eliminate the N+1** on `GraphEdgeTypes` lookup inside the per-node
      evidence loop — preload `Dictionary<Guid, string> edgeTypeCodeById`
      once before the loop, same pattern already used elsewhere in the file.
- [ ] Add a namespace-aware node key to `CSharpParser` (`CLASS:{ns}.{typeName}`
      instead of `CLASS:{typeName}`) so same-named classes in different
      namespaces stop collapsing into one graph node. This is a schema-
      compatible change (it only affects `ExternalKey` generation), but
      re-run ingestion on the demo app afterward to confirm node counts
      change as expected.

**Exit criteria:** BFS is linear-time and deterministic given the same input
graph; the namespace-collision known-limitation in the README can be removed
because it's fixed.

---

## Phase 3 — Make the Analysis Progress screen honest (3–4 days)

Right now `AnalysisService.CreateAndRunAsync` runs synchronously, so the
`/progress` endpoint is faking a multi-stage pipeline that's already
finished by the time anyone polls it. Two ways to fix this — pick one based
on how much time you have:

**Option A (fast, ships this week): make the UI match reality.**
- [ ] Replace the 6-stage fake pipeline + fabricated console log with a
      single indeterminate spinner ("Running deterministic analysis…") since
      the backend genuinely can't report partial progress today.
- [ ] Remove the hardcoded `"GENERATING_REPORT"` stage fabrication in
      `AnalysesController.Progress` — just return `COMPLETED`/`RUNNING` and
      let the frontend redirect on `COMPLETED`, no fake percentage.

**Option B (proper fix, matches your own README's "next steps"): make it real.**
- [ ] Move `RunDeterministicEngineAsync` off the request thread and onto the
      `Job` table + a background worker (`IHostedService` or a simple
      `BackgroundService` queue is enough for current scale — no need for
      Hangfire/Azure Functions yet).
- [ ] Have the worker update `Job`/`Analysis` status at each real stage
      boundary (graph load → BFS → security scan → AI explanation) so
      `/progress` reports actual state.
- [ ] Once this exists, revisit SignalR for push updates instead of 1s
      polling — but only after the job table is real, otherwise you're
      building push infrastructure for fake data.

Recommendation: do **Option A now** (half a day, stops the product from
visibly lying to anyone who watches closely), and schedule **Option B** as
its own project once Phase 5's scale work makes synchronous analysis
actually too slow to run in-request anyway.

**Exit criteria:** what the UI displays is either real, or honestly
indeterminate — no more fabricated stage/log output.

---

## Phase 4 — Build a real test suite (1.5–2 weeks)

Current coverage is effectively zero on the parts that matter most.

- [ ] **Classifier tests first** (`AnalysisService.Classify` / R1–R12): one
      test per rule, built from small synthetic graphs — this is the
      highest-value target since it's pure logic, no I/O, and it's the
      product's core claim.
- [ ] **Parser tests** for `CSharpParser`/`TypeScriptParser`/`SqlParser`:
      feed known snippets, assert exact nodes/edges produced. Cheap and
      catches regressions the moment someone touches parsing.
- [ ] **Ingestion integration test**: run `IngestionService` against a small
      fixture repo (a trimmed slice of `demo-app/` is perfect for this) with
      an in-memory or LocalDB test database, assert final node/edge counts.
- [ ] **BFS determinism test** from Phase 2 — run twice, assert identical
      output.
- [ ] Wire `dotnet test` into a CI step (see Phase 6) so this doesn't rot.

**Exit criteria:** the classifier and parsers have meaningful coverage;
tests run in CI, not just locally.

---

## Phase 5 — Scale & robustness (2–3 weeks)

Needed before pointing this at genuinely large enterprise repos, per its own
pitch.

- [ ] **Incremental ingestion**: instead of rebuilding the entire snapshot
      on every run, diff changed files (via commit SHA / file hash) and only
      re-parse + patch the affected nodes/edges. This is the single biggest
      scalability unlock.
- [ ] **Batch the `CodeFiles` lookup** in `IngestionService` — currently one
      `FirstOrDefaultAsync` per new file path; load existing `CodeFiles` for
      the repository into a dictionary once up front instead.
- [ ] **Frontend graph layout**: bring in `dagre` or `elkjs` for
      `ArchitecturePage` instead of the fixed `x: i*240, y: layerIdx*150`
      grid — this is what will actually make the graph readable once a real
      repo produces hundreds of nodes.
- [ ] **Frontend data layer**: introduce TanStack Query (React Query) to
      replace the hand-rolled `useEffect`/`useState`/try-catch fetch pattern
      repeated across ~24 pages — gets you caching, retry, and de-dup for
      free and will matter more as pages multiply.

**Exit criteria:** re-ingesting a repo with only a handful of changed files
is fast (not a full rebuild); the graph view stays legible past ~200 nodes.

---

## Phase 6 — Strengthening pass ("make it more strong")

These go beyond fixing what's broken — they're what would make this feel
like a production-grade tool rather than a strong prototype.

**Engine quality**
- [ ] Upgrade the C# parser from Roslyn syntax-tree-only to a semantic model
      (`CSharpCompilation` with real symbol resolution) — this is what
      actually fixes cross-project/cross-assembly resolution, not just the
      namespace-key patch from Phase 2.
- [ ] Replace the regex-based TypeScript parser with `ts-morph` for real AST
      parsing — regex-based structural parsing will silently mis-parse
      anything moderately complex (generics, decorators, JSX edge cases).
- [ ] Since `Microsoft.SqlServer.TransactSql.ScriptDom` is already a
      dependency, actually use it for the SQL parser instead of regex — it's
      referenced but unused today.

**Operational maturity**
- [ ] Add structured logging (Serilog or `ILogger` with structured
      properties) around ingestion and analysis runs — right now failures
      inside `IngestionService`'s per-file loop are swallowed into an
      `errors` list with no correlation to a request/trace ID.
- [ ] Add a CI pipeline (GitHub Actions is fine): restore → build → `dotnet
      test` → `npm run build` on every PR. This is what makes Phase 4's
      tests actually protect you.
- [ ] Add basic rate limiting / request size limits on the ingestion and
      analysis endpoints — both can trigger expensive, unbounded work
      (arbitrary repo size, arbitrary file content) from an authenticated
      but not necessarily trusted caller.

**Security & config**
- [ ] Move the Azure DevOps PAT and Azure OpenAI key out of
      `appsettings`/config-file territory entirely into a secrets manager
      (Azure Key Vault, or user-secrets for local dev) — today they're read
      via `_configuration[...]`, which is fine structurally, but confirm no
      environment ever falls back to committing a real value into a
      tracked `appsettings.*.json`.
- [ ] Add authorization checks (not just authentication) on
      project-scoped endpoints — confirm a user can't fetch another
      organization's `ProjectId` by guessing/enumerating GUIDs.

**Frontend polish**
- [ ] Respect `prefers-reduced-motion` for the `pulseGlow` animation.
- [ ] Add a light theme or at least confirm dark-mode-only is an intentional
      product decision, not an oversight — worth a one-line note in the
      README either way so it doesn't look unfinished.

---

## Suggested sequencing if working solo

| Week | Focus |
|---|---|
| 1 | Phase 0 + Phase 1 (cleanup, kill the duplicate engine) |
| 2 | Phase 2 (BFS correctness, determinism, N+1, namespace keys) |
| 3 | Phase 3 Option A (honest progress UI) + start Phase 4 (classifier tests) |
| 4–5 | Finish Phase 4 (parser + ingestion tests, CI) |
| 6–7 | Phase 5 (incremental ingestion, graph layout, React Query) |
| 8–9+ | Phase 6, picked off opportunistically (semantic parser upgrade is the biggest single item) |

Everything in Phase 6 is worth doing eventually but none of it blocks a
demo or a portfolio submission — Phases 0–4 are what take this from "cool
prototype with a load-bearing lie in the progress screen" to "correct and
trustworthy for its stated scope."
