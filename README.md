# Project X-Ray

Change-impact, dependency, and security blast-radius analysis for enterprise codebases (C#/.NET + React/TypeScript + SQL Server).

> **Code change → dependency graph → deterministic blast radius → security evidence → AI explanation → CRITICAL / RISKY / SAFE / UNKNOWN**

## The core principle

**The dependency graph is authoritative.** Blast radius, distances, and risk states are computed by deterministic graph traversal — zero AI involvement. The AI layer only writes prose explanations, and it is only ever fed facts the deterministic engine already produced (evidence, node results). It can never invent a node, edge, risk state, or finding.

If evidence is incomplete, the result says `UNKNOWN` — never silently `SAFE`.

## Architecture

| Layer | Tech |
| --- | --- |
| Backend | ASP.NET Core (.NET 9) Web API, EF Core **Code-First** against SQL Server |
| Database | SQL Server (LocalDB for local dev) — full normalized schema, see [`Figma/schema.md`](Figma/schema.md) |
| Graph construction | `XRay.Parsers` structural parsers + `IngestionService` snapshot pipeline; deterministic `AnalysisService` computes blast radius and classification |
| Auth | Microsoft Entra ID (Azure AD) via MSAL — with a local dev-only bypass so you can run the app before an App Registration exists |
| Frontend | React 18 + TypeScript + Vite + Tailwind + React Flow, routed with React Router |

This is a full rewrite of the original Python/FastAPI + in-memory-store prototype. See [`documents/technical-guide.md`](documents/technical-guide.md) for the detailed architecture and [`documents/setup-guide.md`](documents/setup-guide.md) for what you need to install/configure.

## Quick start

Requires: .NET 9 SDK, Node 20+, SQL Server LocalDB (ships with Visual Studio / SQL Server Express), git.

```powershell
# 1. Backend — apply migrations (creates the XRay database on LocalDB) and run the API
cd backend
dotnet ef database update --project XRay.Infrastructure --startup-project XRay.Api
dotnet run --project XRay.Api
# API on http://localhost:5006, Swagger at /swagger

# 2. Frontend (separate terminal)
cd frontend
npm install
npm run dev
# App on http://localhost:5173
```

The app runs with a **development authentication bypass** by default (no real Microsoft Entra ID App Registration required) — see [`documents/setup-guide.md`](documents/setup-guide.md) for how to switch to real Azure AD sign-in.

Once signed in: **Projects → Connect Project** (point it at a local repository path, e.g. `demo-app/`) → **Open Project** to ingest and view the dependency graph → **Analyses → Analyze a Change** to run the deterministic engine.

## Repository layout

```
backend/
  XRay.Domain/          Entities for every schema.md table (Identity, Projects, Graph, Analysis, Security, AI, ...)
  XRay.Infrastructure/   EF Core AppDbContext, fluent model configuration, migrations, seed data
  XRay.Application/      (thin — DTOs mostly live next to controllers, see technical-guide.md)
  XRay.Api/              Controllers, services (ingestion, analysis logic, security scan, AI explain, reports, ...), Program.cs, auth
  XRay.Parsers/          Canonical structural parsers used by the app (C#, TypeScript, SQL)
  XRay.Tests/            xUnit tests for the active runtime path

frontend/
  src/api/               Typed API client
  src/auth/              MSAL + dev-bypass auth provider
  src/components/        AppShell, Sidebar, Topbar, RiskBadge, Card, Button, ...
  src/pages/             All 20 screens (Login, Overview, Projects, Architecture, Analyze Change, ...)
  src/state/             ProjectContext (current project selection)

demo-app/                Sample CGOne-style app (.NET API + React + SQL) used as the analysis target — not part of X-Ray itself
Figma/                   Scratch / cross-transfer dump: prompts, notes, screenshots, and schema artifacts used during design handoff; not a canonical source-of-truth for the app itself
documents/               Setup, technical, and user guides
```

> Note: the `Figma/` folder is intentionally a working scratch area for handoff and design transfer, not a guaranteed production asset set. Treat it as reviewable reference material, not something to delete without checking whether a teammate still needs a transferred artifact.

## Classification rules (R1–R12)

Evaluated in priority order in `XRay.Api/Services/AnalysisService.cs`. `MaxDepth` defaults to 6, `ConfidenceThreshold` to 0.70 — matching [`Figma/schema.md`](Figma/schema.md) section 24 exactly.

| Rule | Condition | State |
| --- | --- | --- |
| R1 | Node failed to parse | UNKNOWN |
| R2 | Modified and only partially parsed | UNKNOWN |
| R3 | Node is modified by the change | CRITICAL |
| R4 | HIGH/CRITICAL security finding attributed to the node | CRITICAL |
| R5 | Distance ≤ 1 and min edge confidence ≥ threshold | CRITICAL |
| R6 | Distance ≤ 1 and min edge confidence < threshold | RISKY |
| R8 | Distance 2..MaxDepth | RISKY |
| R9 | Path traverses a runtime-resolved edge | UNKNOWN |
| R10 | Partially parsed | UNKNOWN |
| R11 | Reachable but beyond MaxDepth | RISKY |
| R12 | Fully parsed and provably unreachable | SAFE |

**SAFE is reachable only through R12** — "no impact detected with the available evidence", not "secure". Everything undecidable becomes UNKNOWN.

## Evidence chain

Every non-SAFE node carries the exact graph path that produced its state (source file, line, parser rule, confidence per hop) as `Evidence` rows — viewable on the **Trace Evidence** screen. The AI explanation layer only ever references evidence that already exists; it never authors new evidence, nodes, or edges.

## Known limitations / next steps

- The dashboard intentionally uses a dark theme for dense graph and risk review workflows; users with `prefers-reduced-motion` receive minimal animation.
- The deterministic analysis engine runs synchronously in-request rather than via the `Job` table + background worker (the table exists in the schema; nothing dispatches to it yet).
- No SignalR push for the Analysis Progress screen yet — it's a polling-shaped contract for now.
- The TypeScript parser is regex/heuristic (no ts-morph/real AST); the SQL parser is regex-based even though `Microsoft.SqlServer.TransactSql.ScriptDom` is referenced for a future upgrade.
- Gemini is the LLM provider for all evaluations. Set `Gemini:ApiKey` in `appsettings.json` as instructed locally; the tracked value is only a placeholder. The endpoint, model, temperature, and output-token settings are also under `Gemini` in appsettings.
- Azure DevOps and Microsoft Foundry integrations get a real HTTP connectivity probe on "Test Connection"; Azure AI Search, Microsoft Teams, and Power Automate are modeled with connection state but "Test Connection" is currently simulated.
- C# analysis is class-level (Roslyn syntax tree only, no semantic model), so identically named classes in different namespaces can collapse into one node.
- Dependency detection is structural, not semantic; reflection and fully dynamic dispatch are not resolved.

