# User Guide

This guide explains how to use Project X-Ray.

## 1. What the app is for

Project X-Ray helps you understand which parts of a system may be affected by a code change — how far it can spread, which services/modules are at risk, and whether the risk is direct, indirect, or genuinely uncertain.

## 2. Starting the app

```powershell
# Backend
cd backend
dotnet run --project XRay.Api

# Frontend (separate terminal)
cd frontend
npm run dev
```

Open `http://localhost:5173`. Click **Continue with Microsoft** on the login screen — by default this uses a local development sign-in with no Azure AD setup required (see the setup guide to switch to real Microsoft Entra ID sign-in).

## 3. Main workflow

1. **Projects → Connect Project** — give it a name and a local repository path (e.g. the bundled `demo-app/` folder), then **Create & Ingest**. This walks the repository, parses C#/TypeScript/SQL files, and builds the dependency graph.
2. **Open Project → Architecture** — explore the dependency graph (frontend → API → controller → service → repository → database).
3. **Analyses → Analyze a Change** — choose **Work Item**, **Pull Request**, or **Manual Change**, list the changed file paths (relative to the repository root), pick your analysis scope, and **Run X-Ray Analysis**.
4. **Analysis Results** — see risk counts (Critical/Risky/Safe/Unknown), the blast-radius graph, X-Ray Findings, and the Expert Explanation.
5. **View Traceable Evidence** — every non-Safe conclusion is backed by an Evidence card: which file, which relationship, what confidence.
6. **Security Center** — all security findings across the project, with severity, component, and remediation guidance.
7. **Reports** — generate and browse historical analysis reports.

## 4. Understanding the risk states

- **CRITICAL** — strong evidence of direct or high-impact change (the component was modified, or a HIGH/CRITICAL security finding is attached to it, or it's one hop away with high-confidence evidence).
- **RISKY** — potential downstream/transitive impact.
- **SAFE** — no impact detected with the evidence currently available. **This is not the same as "secure."**
- **UNKNOWN** — the engine could not establish a trustworthy conclusion (parse failure, dynamic/runtime-resolved dependency, or insufficient metadata). This is a valid, honest result — it is never silently turned into SAFE.

## 5. Reading the dependency graph

Nodes represent frontend components/modules, API endpoints, controllers, services, interfaces, repositories, and database tables. Edges show relationships (`CALLS`, `DEPENDS_ON`, `IMPORTS`, `EXPOSES`, `READS`, `WRITES`, `INHERITS`, `REFERENCES`). If the changed component is connected to many surrounding nodes, the risk expands outward through those edges.

## 6. Using the evidence

Every important conclusion is backed by an `Evidence` record showing the file path, line reference, relationship type, and confidence — visible on the **Trace Evidence** screen. This is what lets you verify a conclusion instead of just trusting a color.

## 7. Using the AI explanation

The **Expert Explanation** panel on the Analysis Results screen is a natural-language summary built strictly from the same evidence you can see on the Trace Evidence screen. If no real Foundry/Azure OpenAI endpoint is configured, it's a deterministic template and is labeled as running in fallback/degraded mode — the underlying risk states and evidence are unaffected either way.

## 8. Common troubleshooting

**The app doesn't start** — check the .NET 9 SDK and Node 20+ are installed, LocalDB is available (`sqllocaldb info` should list `MSSQLLocalDB`), and the backend is running before you load the frontend.

**Ingest fails / no graph appears** — verify the local repository path you gave when connecting the project actually exists and is readable by the process running the backend.

**Analysis creation fails** — make sure you've ingested the project (so there's a current `GraphSnapshot`) before analyzing a change.

**AI explanation says "fallback mode"** — this is expected unless you've configured `AzureOpenAI:Endpoint`/`AzureOpenAI:ApiKey` — see the setup guide.


### Results look uncertain
That may be expected. If the project is incomplete or the graph lacks data, UNKNOWN is safer than a wrong answer.

## 12. Best practice
Use Project X-Ray as a risk review assistant, not as a replacement for engineering judgment.

It helps you spot change impact faster and explain it better, but people still need to review the result.
