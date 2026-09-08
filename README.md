# Project X-Ray

Change-impact and blast-radius analysis for CGOne-style codebases (C#/.NET + React/TypeScript + SQL Server).

> **CGOne code change → dependency graph → deterministic blast radius → security evidence → explanation → RED / YELLOW / GREEN / UNKNOWN**

## The core principle

**The dependency graph is authoritative.** Blast radius, distances and risk states are computed by deterministic graph traversal with zero AI involvement. The AI layer only writes prose explanations, and anything it invents is discarded before it reaches you.

The deterministic core produces a complete, valid result with **no Azure resource, no AI endpoint, no database and no network access**. Security scanning, knowledge retrieval and AI explanation are strictly additive; when they are missing the result says `UNKNOWN`, never "safe".

## Current status

| Phase | Status |
| --- | --- |
| 0. Scaffold + contracts | Done |
| 1. CGOne-mirroring demo app (.NET + React + SQL) | Done |
| 2. FastAPI backend | Done |
| 3. Ingestion + secret masking | Done |
| 4. C#/.NET parser | Done |
| 5. React/TypeScript parser | Done |
| 6. Cross-layer linker | Done |
| 7. `GraphStore` abstraction (in-memory) | Done |
| 8. Change sources (git diff, manual JSON) | Done |
| 9. Deterministic blast radius | Done |
| 10. Classifier + risk engine + evidence chain | Done |
| 11. Security scanners with honest degradation | Done |
| 12. React dashboard | Done |
| 13. AI adapter (mock / Foundry / OpenAI-compatible) | Done |
| 14. Markdown reports | Done |
| 15–19. SQL Server metadata, Azure DevOps, AI Search, Teams | Not started |
| 20–24. Power Automate, persistence, Roslyn, sandbox, deploy | Not started |

## Quick start

Requires Python 3.11+, Node 20+, .NET 9 SDK (optional, for the demo app), git.

```powershell
# Backend
cd backend
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m pytest              # 35 tests
.\.venv\Scripts\python.exe -m uvicorn app.main:app --port 8000

# Frontend (separate terminal)
cd frontend
npm install
npm run dev                                        # http://localhost:5173
```

Then in the dashboard: **1. Ingest repository** → **2. Run X-Ray analysis**.

### Offline, no server

```powershell
.\backend\.venv\Scripts\python.exe scripts\run_demo_analysis.py
.\backend\.venv\Scripts\python.exe scripts\run_demo_analysis.py --markdown
```

### API smoke test

```powershell
powershell -File .\scripts\smoke-test.ps1
```

## Repository layout

```
backend/app/
  contracts/     Pydantic types shared by every layer
  ingestion/     File walking, secret detection and masking
  parsers/       C#, TypeScript and T-SQL parsers
  graph/         GraphStore abstraction + in-memory implementation + builder
  analysis/      Change mapping, blast radius, classifier, risk engine, orchestration
  security/      Real scanners, normalised into findings
  ai/            Reasoner protocol, mock backend, Foundry / OpenAI-compatible backends
  sources/       Change sources (git diff, manual)
  reporting/     Markdown report
  api/           FastAPI routes
demo-app/        CGOne-mirroring synthetic app (.NET + React + SQL Server DDL)
frontend/        React + TypeScript + React Flow dashboard
scripts/         Demo runner and smoke test
```

## Classification rules

Evaluated in order; **first match wins**. `MAX_DEPTH` defaults to 6, `CONF_THRESHOLD` to 0.70.

| Rule | Condition | State |
| --- | --- | --- |
| R1 | Node failed to parse | UNKNOWN |
| R2 | Modified and only partially parsed | UNKNOWN |
| R3 | Node is modified by the change | RED |
| R4 | HIGH/CRITICAL security finding attributed to the node | RED |
| R5 | Distance ≤ 1 and min edge confidence ≥ threshold | RED |
| R6 | Distance ≤ 1 and min edge confidence < threshold | YELLOW |
| R7 | Critical component within `MAX_DEPTH` | RED |
| R8 | Distance 2..`MAX_DEPTH` | YELLOW |
| R9 | Path traverses a runtime-resolved edge | UNKNOWN |
| R10 | Partially parsed | UNKNOWN |
| R11 | Reachable but beyond `MAX_DEPTH` | YELLOW (`beyond_depth_horizon`) |
| R12 | Fully parsed and provably unreachable | GREEN |

**GREEN is reachable only through R12.** It means "no impact detected with the available evidence", not "safe". Everything undecidable becomes UNKNOWN.

Distance-0 non-modified nodes are interfaces bound to a modified implementation, which is why R5 uses `≤ 1` rather than `== 1`. `BINDS` edges cost zero hops, because an interface and its registered implementation are one logical component — without this, DI indirection would double every distance.

### Analysis-level UNKNOWN

The whole result is UNKNOWN, not GREEN, when the graph is empty, no changed file maps to a component, the change set is empty, or the parse failure ratio exceeds the configured threshold.

## Evidence chain

Every non-GREEN node carries the exact path that produced its state, with the parser rule, source file, line and confidence for each hop:

```
CLASS:PaymentsController --DEPENDS_ON--> INTERFACE:IPaymentService
    csharp.ctor_injection @ Controllers/PaymentsController.cs:13   conf 0.95
API:POST /api/payments   --EXPOSES-->    CLASS:PaymentsController
    csharp.api_exposed_by_controller @ Controllers/PaymentsController.cs:18   conf 0.98
FRONTEND_MODULE:src/api/paymentApi.ts --CALLS--> API:POST /api/payments
    typescript.http_call @ src/api/paymentApi.ts:15   conf 0.90
FRONTEND_COMPONENT:src/pages/CheckoutPage.tsx --IMPORTS--> FRONTEND_MODULE:src/api/paymentApi.ts
    typescript.import @ src/pages/CheckoutPage.tsx:2   conf 0.98
```

Paths come from graph traversal. The AI never authors them.

## What the parsers extract

**C# / .NET** — controllers and route attributes, HTTP verb attributes, DI registrations (`AddScoped<IFoo, Foo>`), constructor injection, field declarations, invocations on injected members, object creation, base types, EF Core `DbSet` declarations and access (read vs write), raw SQL string table references.

**React / TypeScript** — components vs modules, relative imports with extension/index resolution, `fetch` and axios-style client calls with route normalisation.

**T-SQL** — `CREATE TABLE` and `FOREIGN KEY ... REFERENCES`.

Routes are normalised to a shared signature (`/api/Payments/{id:int}` and `` `/api/payments/${id}` `` both become `GET /api/payments/{}`), which is what links a React component to the controller serving it.

## AI backends

Configure with `XRAY_AI_BACKEND`:

- `mock` (default) — deterministic, offline, always labelled as such
- `foundry` — `azure-ai-projects` + `DefaultAzureCredential`
- `openai_compatible` — an Azure OpenAI-compatible endpoint supplied by IT

Guardrails applied to every backend: output is parsed with Pydantic, node ids not present in the graph are dropped, and the model cannot change any state, distance or path. Any failure falls back to the mock backend and marks the explanation subsystem `DEGRADED`.

## Security

Scanners run independently: built-in secret detection (always available), `npm audit`, and `dotnet list package --vulnerable`. Unavailable scanners are listed explicitly and reduce confidence. Secrets are masked during ingestion, before anything is stored, logged or sent to a model.

## Known limitations

- C# types are keyed by simple name, so two same-named classes in different namespaces collapse into one node.
- Analysis is class-level, not method-level.
- Interface implementations discovered without a DI registration get confidence 0.65 and are marked runtime-resolved, so they yield YELLOW/UNKNOWN rather than RED.
- Dependency detection is structural, not semantic; reflection and dynamic dispatch are not resolved.
- The graph is in-memory and rebuilt on each ingest.
- Risk points are declared heuristics, not calibrated probabilities.
- GREEN means "no impact detected with available evidence". Human review is still required.

## To verify against live Microsoft tooling

- The exact `azure-ai-projects` call surface for your Foundry deployment.
- Whether your Power Automate licence permits the HTTP action.
- Whether Azure DevOps service hooks in your org can reach a dev tunnel URL.
- `azure-search-documents` vector field configuration and embedding dimensions.
