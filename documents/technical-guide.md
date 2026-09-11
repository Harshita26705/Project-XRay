# Technical Guide

This is the technical explanation of Project X-Ray's architecture.

## 1. Goal of the project

Project X-Ray answers, before a change ships:

- Which parts of the system may be affected?
- Which dependencies are connected to the changed component?
- Which parts are high risk?
- Is there security evidence tied to the affected path?
- How do we explain the impact clearly, without inventing facts?

## 2. Main idea: graph first, AI second

**The dependency graph is the source of truth.** It tells us which files, classes, services, APIs, and database tables are connected. The AI layer does not decide the graph or the risk state — it only explains an already-computed result in natural language, and it is only ever fed facts (Evidence rows, AnalysisNodeResult rows) that the deterministic engine already produced.

## 3. Solution architecture

### Backend — ASP.NET Core (.NET 9), EF Core Code-First

```
backend/
  XRay.Domain/           Plain C# entity classes for every schema.md table
  XRay.Infrastructure/    AppDbContext, ModelConfiguration (fluent API), migrations, DbSeeder
  XRay.Application/       Thin — most DTOs live next to controllers for delivery speed (see below)
  XRay.Api/
    Controllers/          One controller per resource (Projects, Changes, Analyses, Security, Reports, Integrations, Ai, Notifications, Settings, Me)
    Services/              ProjectService, IngestionService, ChangeService, SecurityScanService,
                            AnalysisService (the deterministic engine), ReportService, IntegrationService,
                            AiExplanationService, NotificationService, SettingsService, CurrentUserService
    Contracts/             Request/response DTOs
    Auth/                  DevBypassAuthHandler (Development-only fake auth)
    Program.cs             DI wiring, EF Core, auth (real MSAL vs. dev bypass), CORS, Swagger
  XRay.Parsers/           CSharpParser (Roslyn), TypeScriptParser (regex), SqlParser (regex)
  XRay.Tests/             xunit tests
```

**Why services live directly in `XRay.Api` instead of a separate Application/Infrastructure interface split:** schema.md calls for 14 service boundaries across every domain; given the scope, the project favors concrete service classes with direct `AppDbContext` injection over an extra interface-abstraction layer. This is a deliberate, documented tradeoff for delivery speed — see `repo` memory notes if you're extending this codebase.

### Database — SQL Server, EF Core Code-First

Every table from [`Figma/schema.md`](../Figma/schema.md) is modeled as a C# entity (~50 entities across 12 domains: Identity, Projects, Ingestion, Graph, Change Management, Analysis, Security, AI, Reporting, Integrations, Notifications, Operations). Migrations are generated with `dotnet ef migrations add`, and `ModelConfiguration.cs` in `XRay.Infrastructure` configures keys, unique indexes, and lookup-table foreign keys via Fluent API.

All foreign keys use `DeleteBehavior.Restrict` — the schema is dense enough with cross-references that SQL Server's cascade-delete paths would collide; deletes are handled explicitly in application code instead.

### Deterministic analysis engine

`AnalysisService.RunDeterministicEngineAsync`:

1. Loads the project's current `GraphSnapshot` (nodes + edges).
2. Maps the `Change`'s changed file paths to `GraphNode`s (direct impact).
3. Runs a breadth-first search **backwards** along edges (who depends on what changed) up to `MaxDepth` (default 6), tracking distance and minimum edge confidence per path, plus an unbounded pass to detect "reachable but beyond depth" nodes.
4. Classifies every node with the **R1–R12 rule table** (see root README) — `ConfidenceThreshold` defaults to 0.70, matching schema.md section 24 exactly.
5. Writes `AnalysisNodeResult`, `Evidence`, and (if in scope) runs `SecurityScanService` and attaches `SecurityFinding`s, which can upgrade a node's risk to CRITICAL via rule R4.
6. Computes the overall risk state (worst-case across nodes) and marks the analysis complete.

This currently runs **synchronously in the request** rather than via the `Job` table + a background worker (the table exists in the schema for this purpose; nothing dispatches to it yet — a documented follow-up).

### Parsers

- **C# (Roslyn syntax trees, no semantic model):** classes/interfaces classified by naming convention (`*Controller`, `*Service`, `*Repository`, `I*` interfaces, `DbContext` subclasses) → `GraphNode`s; constructor-injected parameters → `DEPENDS_ON` edges; `[HttpGet]`/`[HttpPost]`/etc. + `[Route]` → `API` nodes with `EXPOSES` edges; `DbSet<T>` properties → `DATABASE_TABLE` nodes; member access on context-like fields → best-effort `READS`/`WRITES` edges.
- **TypeScript/TSX (regex/heuristic):** `.tsx` files → `FRONTEND_COMPONENT`, `.ts` → `FRONTEND_MODULE`; relative `import` statements → `IMPORTS` edges; `fetch`/`axios` calls → `CALLS` edges to matching `API` nodes (best-effort path normalization).
- **SQL (regex, `Microsoft.SqlServer.TransactSql.ScriptDom` referenced for a future AST-accurate upgrade):** `CREATE TABLE` → `DATABASE_TABLE` nodes; `FOREIGN KEY ... REFERENCES` → `REFERENCES` edges; `CREATE PROCEDURE` → `STORED_PROCEDURE` nodes.

### Security scanning

`SecurityScanService` runs regex-based SAST rules against changed files' source text (re-read from the local repository path): hardcoded secrets, SQL injection via string concatenation, weak crypto (`MD5`/`DES`/`SHA1`), and insecure deserialization (`BinaryFormatter`/`JavaScriptSerializer`). Findings are normalized into `SecurityFinding` rows tied to the responsible `GraphNode`.

### AI explanation layer

`AiExplanationService` builds an explanation using only `AnalysisNodeResult`/`Evidence` facts. If `AzureOpenAI:Endpoint`/`AzureOpenAI:ApiKey` are configured, it calls a real chat-completions endpoint with a prompt built strictly from those facts; otherwise it falls back to a deterministic template and marks the generation `Degraded = true`. Every `AIGeneration` is linked to the `Evidence` it referenced via `AIOutputReference`, so a generation can never claim a fact the deterministic engine didn't produce.

### Authentication

Real Microsoft Entra ID (Azure AD) via `Microsoft.Identity.Web` (backend JWT bearer validation) and MSAL.js/`@azure/msal-react` (frontend). A **development-only bypass** (`DevBypassAuthHandler` on the backend, a local-only auth provider on the frontend) lets you run the whole app without an App Registration — see the setup guide for how to switch to real sign-in.

### Frontend — React 18 + TypeScript + Vite + Tailwind + React Flow

```
frontend/src/
  api/          Typed fetch client + per-domain endpoint functions
  auth/          MSAL config + AuthProvider (real MSAL or dev bypass)
  state/         ProjectContext (current selected project)
  components/    AppShell (Sidebar/Topbar), RiskBadge, StatusBadge, Card/StatCard, Button, ProgressBar, EmptyState/ErrorState
  pages/         All 20 screens from the Figma spec, each fetching real data from the API
```

Design tokens (dark enterprise palette, risk colors) live in `tailwind.config.js`. Routing is React Router; `ProjectContext` tracks which project is "current" (persisted in `localStorage`) since most screens are scoped to a single project.

## 4. Data flow, end to end

```
Connect Project (local path)
  -> IngestionService walks the repo, invokes CSharpParser/TypeScriptParser/SqlParser
  -> materializes a GraphSnapshot (GraphNode + GraphEdge), unresolved edge targets become EXTERNAL nodes
  -> Architecture screen renders it via React Flow

Analyze a Change (Work Item / Pull Request / Manual)
  -> ChangeService creates a Change + ChangeFile rows
  -> AnalysisService.CreateAndRunAsync maps changed files to GraphNodes, runs BFS blast radius,
     applies R1-R12, runs SecurityScanService, writes AnalysisNodeResult + Evidence + SecurityFinding
  -> Analysis Results / Trace Evidence / Security Center screens render the real results
  -> AiExplanationService produces the "Expert Explanation" from those same facts
  -> ReportService assembles a Report + ReportSections for the Reports / Report Detail screens
```

## 5. Known limitations

See the root [`README.md`](../README.md) "Known limitations / next steps" section — it's kept in one place to avoid drift between documents.


This is an important engineering decision.

## 8. Azure and Microsoft integrations
These integrations are optional and should be treated as extensions, not the core product.

### Microsoft Foundry
Used for explanation and higher-level reasoning.

### Azure AI Search
Used for project knowledge lookup and search support.

### Azure DevOps
Used for pull requests, work item context, commit history, and repo metadata.

### Teams and Power Automate
Used for alerts and workflow notifications when the project is integrated into a team environment.

### SQL read-only connection
Used for project metadata or context, but never written to.

## 9. Why the project is split into modules
The backend is structured into clear sections so each part can be tested independently.

Examples:

- ingestion: read files and gather metadata
- parsers: understand C#, TypeScript, and SQL structure
- graph: represent dependencies
- analysis: determine change impact
- security: scan and report findings
- AI: explain findings using Foundry or mock backend
- reporting: output the result in Markdown or UI

This makes the project easier to maintain and simpler to test.

## 10. Demo and MVP approach
The project was designed first for a reliable hackathon/demo MVP.

Rather than building every external integration at once, the team focused on the real logic first:

- dependency graph
- deterministic classification
- evidence trails
- UI output
- explainability

Once that works, cloud and Azure integrations can be added later.

## 11. Key engineering lesson
The project does not depend on AI to be correct.

The graph and rules decide the impact.

AI is a companion that helps explain the result.

That is safer and more practical for technical teams.
