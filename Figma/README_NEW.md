# Project X-Ray

> **AI-assisted change-impact, dependency, security, and blast-radius analysis for enterprise applications.**

Project X-Ray analyzes a software project from the repository level down through its frontend, backend, APIs, services, repositories, database structures, and external integrations. It builds a dependency graph, maps a proposed change to that graph, deterministically calculates the affected/blast-radius components, attaches security evidence, and uses AI only to explain the findings and recommendations.

The **Figma design is the current product/UI source of truth**. This README describes the product represented by the latest Figma screens and the intended functional behavior behind them.

---

## 1. Product Vision

Before a developer ships a change, X-Ray should answer:

1. **What is changing?**
2. **Which components are directly affected?**
3. **Which downstream components may be affected?**
4. **How far does the change propagate through the architecture?**
5. **Are there security vulnerabilities in the affected path?**
6. **Which evidence proves the conclusion?**
7. **What should the developer test or review?**
8. **What should happen when evidence is incomplete or an integration is unavailable?**

The core trust model is:

```text
Repository / PR / Work Item
          |
          v
     Source Ingestion
          |
          v
     Code + DB Parsers
          |
          v
    Dependency Graph
          |
          +----------------------+
          |                      |
          v                      v
  Deterministic Impact      Security Scanning
      Analysis                   |
          |                      |
          +----------+-----------+
                     |
                     v
              Evidence Chain
                     |
                     v
             Risk Classification
                     |
                     v
          AI Explanation Layer
                     |
                     v
          Reports / Dashboard /
          Notifications / Actions
```

### Trust principle

**The dependency graph and deterministic analysis are authoritative.**

AI does not decide:

- whether a node is affected;
- the blast radius;
- graph distance;
- severity/state;
- evidence paths;
- whether a dependency exists.

AI is an explanation and recommendation layer. Model output must be validated against the graph and discarded when it references entities or facts that do not exist in the analysis result.

If evidence is insufficient, the system must say **UNKNOWN**, not **SAFE**.

---

# 2. Product Navigation

The Figma application contains the following primary navigation:

- Overview
- Projects
- Analyses
- Changes
- Security
- Reports
- Integrations
- Settings
- Help & Docs

The global application shell contains:

- X-Ray product identity/logo;
- left navigation;
- breadcrumb/context header;
- project selector;
- environment/branch selector where applicable;
- dependency-graph search;
- notifications icon;
- user avatar/profile;
- contextual primary actions.

The UI uses a dark enterprise developer-tool aesthetic with compact cards, subtle borders, blue primary actions, and semantic risk colors.

---

# 3. Authentication

## Login

The login screen provides:

- Project X-Ray branding;
- short product tagline: **"See the blast radius before you ship."**
- **Continue with Microsoft** primary authentication action;
- **Continue with SSO** secondary authentication action;
- enterprise security/Azure Active Directory messaging.

Authentication should support enterprise identity rather than local username/password as the primary product flow.

---

# 4. Overview / Dashboard

The Overview screen is the main operational dashboard.

## Header

Show:

- greeting, e.g. `Good morning, Harshita`;
- short explanation of what X-Ray found across projects;
- project selector;
- environment selector;
- dependency graph search;
- notifications;
- user profile.

## KPI cards

Display:

1. **Total Analyses**
2. **Critical Changes**
3. **Components Analyzed**
4. **Security Findings**
5. **Risk Trend Index (24h)**

The risk trend card contains a compact line chart and a percentage trend indicator.

## Recent Analyses

A table showing recent analysis activity with:

- Change
- Type
- Risk
- Affected components
- Created time
- Status

Risk states use:

- Critical
- Risky
- Safe
- Unknown

## Quick Actions

Provide direct shortcuts for:

- Analyze a Change
- Analyze PR
- View Architecture
- Run Security Scan

---

# 5. Projects

The Projects page manages connected repositories/projects.

## Project card

Each project can show:

- project/repository name;
- repository URL/reference;
- connection state;
- technology tags;
- node/component count;
- relationship count;
- security flaw count;
- last indexed time.

Example technology tags represented in the design:

- .NET
- React
- SQL Server
- Azure DevOps

## Project actions

Support:

- Open Project
- Re-index
- Settings

## Connect New Repository

Provide a dedicated empty/add-project area.

Connecting a repository should allow X-Ray to:

1. authenticate/connect to the repository provider;
2. discover repository contents;
3. index source files;
4. identify supported technologies;
5. parse source code and database definitions;
6. build the dependency graph;
7. run baseline security analysis;
8. make the project available for architecture and change-impact analysis.

---

# 6. Architecture / Dependency Graph

The Architecture screen visualizes the application's dependency graph.

## Purpose

Users should be able to understand how the application is connected across:

- frontend;
- APIs;
- controllers;
- services;
- repositories;
- database;
- external systems.

## Node filters

The Figma design provides filters for:

- Frontend
- API
- Controllers
- Services
- Repositories
- Database
- External

## Edge filters

Provide relationship filters for:

- Calls
- Depends on
- Imports
- Reads
- Writes

## Risk legend

Show semantic states:

- Critical
- Risky
- Safe

## Graph interactions

The graph should support:

- pan;
- zoom in;
- zoom out;
- fit/reset view;
- search;
- selecting a node;
- highlighting connected nodes;
- highlighting dependency paths;
- showing node type;
- showing relationship type;
- visually distinguishing impacted nodes;
- showing the graph in a readable multi-tier layout.

The graph must support cross-layer relationships such as:

```text
React Component
   |
   v
Frontend API Client
   |
   v
HTTP API Endpoint
   |
   v
Controller
   |
   v
Service
   |
   v
Repository
   |
   v
SQL / Database
```

---

# 7. Analyze a Change

The Analyze a Change workflow is the primary entry point for impact analysis.

## Change source tabs

Support:

- **Work Item**
- **Pull Request**
- **Manual Change**

## Work Item

Allow the user to enter an Azure DevOps Work Item ID.

Actions:

- Fetch Work Item
- Run X-Ray Analysis

Show:

- target project;
- working branch;
- Azure DevOps connection status.

## Pull Request

Allow analysis of a pull request and its changed files/diff.

## Manual Change

Allow a developer to describe or provide a manual change when a PR/work item is not available.

## Analysis Scope

Provide controls for:

- Scan direct dependencies
- Trace recursive transitive dependencies
- Include external API bindings
- Perform static security analysis

The workflow must clearly distinguish selected scope from unavailable integrations.

---

# 8. Analysis Progress

The analysis progress page makes the analysis pipeline visible rather than showing a generic spinner.

## Foundry Engine Pipeline

The design shows a staged pipeline containing steps such as:

1. Reading Azure DevOps work item and changesets
2. Identifying affected file contexts & core targets
3. Building multi-tiered dependency blast-radius paths
4. Checking static assemblies and credentials security
5. Retrieving downstream project environment context
6. Running dependency rule validation engine
7. Generating cryptographic/blast-radius report

Each step has a state:

- completed;
- currently running;
- pending.

## Engine Trace Telemetry

Display:

- total components traced;
- total components / expected components;
- estimated recursive dependency chains;
- console/engine telemetry output.

## Progress

Show:

- current stage name;
- percentage completed;
- progress bar.

Example:

```text
Analyzing Change
████████████████░░░░░░░░ 43% Completed
```

The progress UI should update from actual backend job state rather than being hard-coded.

---

# 9. Analysis Results

The analysis result is the primary decision screen.

## Header

Display:

- severity/state;
- PR/change identifier;
- change title;
- short description;
- Re-Run Scan;
- View Pull Request.

## Risk summary

Show counts for:

- Critical
- Risky
- Safe
- Unknown

## Blast Radius Mapping

Display an interactive dependency graph containing the affected path.

Each node should expose:

- name;
- component type;
- state;
- direct/transitive relationship;
- connection/path information.

Impacted nodes must be visually distinguishable by risk state.

## X-Ray Findings panel

The right-side findings panel summarizes:

- structural impact;
- security warnings;
- failed unit tests;
- risk counts.

## Expert Explanation

Display AI-generated explanation that is explicitly labelled as an expert/AI explanation.

The explanation should answer:

- why the change matters;
- what downstream component is affected;
- why the risk exists;
- what behavior may break.

Provide:

**View Traceable Evidence**

which navigates to the evidence screen.

---

# 10. Evidence / Trace Evidence

Every important conclusion must be explainable through traceable evidence.

## Evidence page

Show a title such as:

**Trace Evidence**

Subtitle:

Review verifiable structural and semantic static analysis proving X-Ray's conclusions.

## Evidence types

The Figma design shows evidence cards such as:

### Direct Change

Example:

- file: `PaymentService.cs`
- method;
- line range;
- source code excerpt.

### Structural Dependency

Example:

- file: `PaymentController.cs`
- class relationship;
- dependency confidence.

### Database Access

Example:

- file: `PaymentRepository.cs`
- SQL/database operation;
- analysis profile.

### Security Alert

Example:

- SAST SQL Injection Potential;
- severity;
- affected source.

Evidence must be tied to actual parser/scanner output.

## Evidence requirements

Each evidence item should include, where applicable:

- file path;
- line number/range;
- parser/scanner rule;
- relationship;
- confidence;
- source snippet;
- security rule;
- severity.

The AI must never create an evidence path that was not produced by the deterministic analysis layer.

---

# 11. Security Center

The Security Center connects vulnerabilities to the software architecture.

## Summary cards

Show:

- Critical
- Risky
- Safe
- Unknown

## Architecture Vulnerabilities table

Columns:

- Finding
- Severity
- Component
- Source
- Status

Example findings represented in the design include:

- SQL Injection
- Hardcoded secret
- Vulnerable dependency
- Insecure deserialization
- Weak encryption

Statuses include:

- Open
- Resolved

## Finding detail panel

Selecting a finding opens contextual details containing:

- finding name;
- severity;
- component;
- file;
- line number;
- description;
- source snippet;
- risk impact;
- recommended remediation;
- recommended tests;
- evidence.

Expandable sections should allow detailed investigation without leaving the Security Center.

## AI explanation

Provide:

**Explain with Foundry**

This generates an explanation using the available evidence without changing the underlying finding.

---

# 12. Changes

The Changes screen provides change-history and blast-radius tracking.

## Tabs

- Pull Requests
- Work Items
- Commits

## Filters

Support:

- search by PR title/number;
- Risk Level;
- Status;
- Author;
- Date Range.

## Change table

Display:

- PR/change identifier;
- title;
- author;
- risk;
- affected components;
- status.

Statuses represented include:

- Analyzed
- Pending

Selecting a change opens the corresponding detail page.

---

# 13. Pull Request Detail

A PR detail page provides a focused review experience.

## Header

Display:

- risk;
- PR number;
- title;
- author;
- branch;
- creation time;
- changed-file count;
- line-change information.

Actions:

- Re-Run Scan
- Open in Azure DevOps

## Tabs

Provide:

- Overview
- Blast Radius
- Security
- Tests
- Evidence

## Overview

Include:

### Change Summary

Explain what the pull request changes and which runtime/schema assumptions it introduces.

### Expert Risk Analysis

Provide an evidence-backed analysis explaining why the change is Critical/Risky/Safe/Unknown.

## Affected Components

Show every affected component with:

- component name;
- component type;
- risk state.

---

# 14. Work Item Detail

The Work Item Detail page predicts impact before implementation.

## Header

Display:

- risk;
- Work Item ID;
- title;
- description;
- View in Azure DevOps.

## Work item metadata

Show:

- Priority;
- Assigned To;
- State;
- Description.

## X-Ray Predicted Impact

List predicted affected components and risk.

## Predicted Blast Radius

Provide a compact graph showing the predicted dependency spread.

## Recommended Implementation Areas

List likely files/components that will need changes.

## Recommended Tests

Suggest tests based on impacted components.

## Security Considerations

Highlight security-sensitive implementation checks.

## Foundry Predictive Report

Use AI to explain the predicted impact from the graph/context. Predictions must remain clearly labelled as predictions and must not be presented as verified evidence.

---

# 15. Reports

The Reports page provides historical analysis results.

## Filters

Support:

- Project;
- Risk Level;
- Date Range;
- Type.

## Report table

Show:

- Date;
- Change;
- Project;
- Risk;
- Affected Components;
- Security Findings;
- Status.

## Pagination

Support:

- Previous;
- Next;
- current result range / total result count.

---

# 16. Report Detail

The report detail page is an executive/technical summary of an analysis.

## Header

Show:

- risk;
- analysis/change ID;
- title;
- project;
- author;
- date.

Actions:

- Export PDF
- Markdown
- Share Report

## Executive Summary

Summarize:

- change;
- structural impact;
- security findings;
- highest-risk affected components;
- recommended intervention.

## Blast Radius

Show:

- critical components;
- risky/transitive impacts;
- View Graph action.

## Security & Vulnerabilities

Show:

- primary vulnerability;
- affected component;
- severity;
- count of additional findings.

## Microsoft Foundry Analysis & Explanation

Explain the analysis in natural language based on verified evidence.

## Recommendations

Provide actionable recommendations such as:

- parameterize SQL strings;
- validate input payloads;
- add unit tests around validation boundaries;
- review affected dependency contracts.

Recommendations should be traceable to the analysis whenever possible.

---

# 17. Integrations

The Integrations page manages enterprise data and reasoning connectors.

The Figma design includes:

1. **Azure DevOps**
   - repositories;
   - pull requests;
   - work items;
   - workflow tasks.

2. **SQL Server DB**
   - database metadata;
   - stored procedures;
   - schema profiling;
   - read-only safety boundary.

3. **Microsoft Foundry**
   - AI reasoning;
   - natural-language explanations;
   - code insight/log capabilities.

4. **Azure AI Search**
   - project knowledge retrieval;
   - cross-project knowledge indexing;
   - query slices.

5. **Microsoft Teams**
   - notifications;
   - alert streams.

6. **Power Automate**
   - workflow automation;
   - remediation orchestration.

Each integration card supports:

- connection state;
- short purpose description;
- Test Connection;
- configuration/settings action.

---

# 18. AI & Foundry Configuration

The AI Configuration page manages the cognitive layer.

## Provider status

Display:

- Cognitive Provider;
- Deployment Status;
- Active Model;
- Knowledge Layer.

Example design values:

- Microsoft Foundry;
- connected;
- `foundry-gpt4-turbo`;
- Azure AI Search.

## Core Integration Philosophy

The UI must communicate that Foundry reasons over structural telemetry rather than becoming the source of truth.

Important rule:

> The AI pipeline may generate explanations and recommendations, but deterministic validation remains authoritative over dependency relationships and impact states.

## Cognitive Inference Pipeline

Visualize:

```text
1. Dependency Graph
        >
2. Trace Collection
        >
3. AI Knowledge
        >
4. Cognitive Output
```

## Active Remediations & Templates

Provide sections for:

### Model Settings

Configure:

- model/hyperparameters;
- token constraints;
- temperature;
- other safe model limits.

### Prompt Templates

Configure:

- system prompts;
- analysis prompts;
- explanation templates;
- custom instructions.

### Usage & Rate Limits

Monitor:

- quota pools;
- billing/log information;
- daily usage;
- token usage.

---

# 19. Settings

Settings contain product-wide configuration.

## Sections

- General
- Projects
- Repositories
- Security
- AI Settings
- Notifications
- Access Control
- API Keys

## General

### Organization Profile

Fields:

- Organization Name
- Default Target Project

### Analysis Defaults

Toggles:

- Include security scan
- Include AI explanation
- Auto-analyze PRs

### Danger Zone

Provide:

- Delete Organization

Deletion must clearly communicate that it is permanent and cannot be undone.

## Save behavior

Provide:

- Save Changes
- Cancel

---

# 20. Notification Settings

Notification configuration contains channels and granular event rules.

## Notification Channels

Support:

- Microsoft Teams;
- Azure DevOps;
- Email.

Each channel displays connection/configuration state and configuration controls.

## Granular Event Rules

Support per-channel toggles for:

- High-risk change detected
- Critical security finding
- Analysis completed
- Analysis failed
- New PR detected

Each event can independently enable/disable:

- Teams;
- DevOps;
- Email.

Changes are saved explicitly.

---

# 21. Empty States

The application must have intentional empty states instead of blank pages.

The Figma design contains four primary empty states:

## No projects connected

Message:

Connect your Azure DevOps project workspace to start creating codebase structures.

Action:

**Connect Project**

## No analyses yet

Message:

Run your first X-Ray dependency sweep to determine high-risk areas of a change.

Action:

**Analyze a Change**

## No security findings

Message:

Compliance details and secret disclosures will populate here after running a scan.

Action:

**Run Security Scan**

## No reports generated

Message:

Reports summarize structural trends and are created automatically on analysis cycles.

Action:

**View Analyses**

---

# 22. Error States

Errors are explicit and actionable.

## Analysis incomplete

Explain:

Some evidence could not be collected and the dependency graph may be incomplete.

Actions:

- Partial results available
- Retry Analysis

This must never silently become GREEN.

## Azure DevOps unavailable

Explain that the repository/service cannot be reached and enterprise credentials/connection should be checked.

Action:

**Retry Connection**

## Unknown Impact

Use when X-Ray cannot determine the dependency relationship because of missing evidence such as:

- parser limitation;
- repository unavailable;
- dynamic dependency;
- insufficient metadata.

Action:

**View Evidence**

## AI Analysis unavailable

Explain that Microsoft Foundry is not responding and dependency analysis remains available, but the AI explanation layer is temporarily unavailable.

Action:

**Check AI Status**

---

# 23. Risk Model

The product uses four visible states:

| State | Meaning |
|---|---|
| Critical | Strong evidence of direct/high-confidence/high-impact change or critical security issue |
| Risky | Potential downstream/transitive impact or meaningful security concern |
| Safe | No impact detected with the currently available evidence |
| Unknown | The system cannot establish a sufficiently trustworthy conclusion |

**Safe is not equivalent to secure.**

A result must become Unknown when critical evidence is unavailable.

---

# 24. Deterministic Analysis Rules

The original deterministic analysis model remains an important backend contract.

Example ordering:

| Rule | Condition | State |
|---|---|---|
| R1 | Node failed to parse | UNKNOWN |
| R2 | Modified and only partially parsed | UNKNOWN |
| R3 | Node is modified by the change | RED / Critical |
| R4 | HIGH/CRITICAL security finding attributed to node | RED / Critical |
| R5 | Distance ≤ 1 and minimum edge confidence ≥ threshold | RED / Critical |
| R6 | Distance ≤ 1 and minimum edge confidence < threshold | YELLOW / Risky |
| R7 | Critical component within configured depth | RED / Critical |
| R8 | Distance 2..MAX_DEPTH | YELLOW / Risky |
| R9 | Path traverses runtime-resolved edge | UNKNOWN |
| R10 | Partially parsed | UNKNOWN |
| R11 | Reachable but beyond depth horizon | YELLOW / Risky |
| R12 | Fully parsed and provably unreachable | GREEN / Safe |

Default configuration from the existing implementation:

- `MAX_DEPTH = 6`
- `CONF_THRESHOLD = 0.70`

The exact backend state labels may be `RED/YELLOW/GREEN/UNKNOWN`, while the UI maps them to human-readable labels such as Critical/Risky/Safe/Unknown.

---

# 25. Evidence Chain

A non-Safe result should contain a traceable path similar to:

```text
CLASS:PaymentsController
  --DEPENDS_ON-->
INTERFACE:IPaymentService

API:POST /api/payments
  --EXPOSES-->
CLASS:PaymentsController

FRONTEND_MODULE:src/api/paymentApi.ts
  --CALLS-->
API:POST /api/payments

FRONTEND_COMPONENT:src/pages/CheckoutPage.tsx
  --IMPORTS-->
FRONTEND_MODULE:src/api/paymentApi.ts
```

Every hop should preserve:

- source file;
- line;
- rule;
- confidence;
- relationship;
- source/target nodes.

The AI must not author this path.

---

# 26. Supported Analysis Sources

The architecture should support:

- Git diff;
- Azure DevOps Pull Requests;
- Azure DevOps Work Items;
- commits;
- manual changes;
- repository source;
- database metadata;
- security scanner output;
- project knowledge retrieved through Azure AI Search.

---

# 27. Parsing / Dependency Extraction

The existing parser architecture supports the following concepts.

## C# / .NET

Extract:

- controllers;
- route attributes;
- HTTP verb attributes;
- DI registrations;
- constructor injection;
- injected member usage;
- object creation;
- base types;
- EF Core `DbSet`;
- read/write database access;
- raw SQL table references.

## React / TypeScript

Extract:

- components;
- modules;
- relative imports;
- extension/index resolution;
- `fetch`;
- axios-style calls;
- normalized HTTP routes.

## T-SQL

Extract:

- `CREATE TABLE`;
- foreign-key references.

## Cross-layer route matching

Normalize routes so frontend calls can be linked to backend endpoints.

Example concept:

```text
GET /api/orders/{id:int}
GET /api/orders/${id}
        |
        v
GET /api/orders/{}
```

---

# 28. Security Scanning

Security analysis is independent of AI.

Potential scanners include:

- secret detection;
- `npm audit`;
- `.NET` vulnerable-package scanning;
- SAST/static analysis;
- SQL injection detection;
- hardcoded-secret detection;
- insecure deserialization detection;
- weak-cryptography checks;
- dependency vulnerability checks.

Secrets must be masked before they are:

- stored;
- logged;
- sent to an AI model.

Unavailable scanners must be explicitly reported rather than silently treated as passing.

---

# 29. AI Guardrails

Every AI backend must be constrained.

AI output should be:

1. parsed against a strict schema;
2. checked against known graph node IDs;
3. stripped of unknown entities;
4. prevented from modifying risk states;
5. prevented from modifying distances;
6. prevented from creating evidence paths;
7. marked as degraded when the AI subsystem fails.

If AI is unavailable, X-Ray should still return the deterministic result.

---

# 30. Integrations Architecture

Recommended integration boundaries:

```text
Azure DevOps
    |
    +--> PRs
    +--> Work Items
    +--> Repositories
    |
    v
 X-Ray Ingestion
    |
    +--> Code Parser
    +--> DB Metadata Parser
    +--> Security Scanner
    |
    v
 Dependency Graph
    |
    +--> Deterministic Analysis
    +--> Evidence
    |
    +--> Microsoft Foundry
    |       |
    |       +--> Explanation
    |       +--> Recommendations
    |
    +--> Azure AI Search
    +--> Microsoft Teams
    +--> Power Automate
```

---

# 31. Backend Architecture

Recommended structure:

```text
backend/
└── app/
    ├── contracts/
    ├── ingestion/
    ├── parsers/
    │   ├── csharp/
    │   ├── typescript/
    │   └── sql/
    ├── graph/
    ├── analysis/
    ├── security/
    ├── ai/
    ├── integrations/
    │   ├── azure_devops/
    │   ├── sql_server/
    │   ├── foundry/
    │   ├── ai_search/
    │   ├── teams/
    │   └── power_automate/
    ├── reporting/
    ├── notifications/
    ├── api/
    └── main.py
```

---

# 32. Frontend Architecture

Recommended frontend:

```text
frontend/
└── src/
    ├── components/
    │   ├── layout/
    │   ├── navigation/
    │   ├── cards/
    │   ├── tables/
    │   ├── graph/
    │   ├── risk/
    │   ├── evidence/
    │   └── forms/
    ├── pages/
    │   ├── Login/
    │   ├── Overview/
    │   ├── Projects/
    │   ├── Architecture/
    │   ├── AnalyzeChange/
    │   ├── AnalysisProgress/
    │   ├── AnalysisResults/
    │   ├── Evidence/
    │   ├── Security/
    │   ├── Changes/
    │   ├── PullRequestDetail/
    │   ├── WorkItemDetail/
    │   ├── Reports/
    │   ├── ReportDetail/
    │   ├── Integrations/
    │   ├── AIConfiguration/
    │   ├── Settings/
    │   ├── Notifications/
    │   ├── EmptyStates/
    │   └── ErrorStates/
    ├── services/
    ├── hooks/
    ├── state/
    ├── types/
    └── routes/
```

React Flow or an equivalent graph library should be used for the architecture/blast-radius visualizations.

---

# 33. API Capabilities

The backend should expose APIs for at least:

```text
Authentication
Projects
Repositories
Graph
Analyses
Analysis Progress
Analysis Results
Evidence
Security Findings
Pull Requests
Work Items
Reports
Integrations
AI Configuration
Settings
Notifications
```

Representative operations:

```text
POST /projects
GET  /projects
POST /projects/{id}/reindex

POST /analyses
GET  /analyses/{id}
GET  /analyses/{id}/progress
GET  /analyses/{id}/graph
GET  /analyses/{id}/evidence

GET  /security/findings
GET  /security/findings/{id}

GET  /changes
GET  /pull-requests/{id}
GET  /work-items/{id}

GET  /reports
GET  /reports/{id}

GET  /integrations
POST /integrations/{id}/test

GET  /settings
PUT  /settings

GET  /notifications/settings
PUT  /notifications/settings
```

Exact endpoint names may change with implementation, but the frontend must be backed by real API state.

---

# 34. Data Model

Core entities should include:

```text
Organization
User
Project
Repository
Environment
Branch

Analysis
AnalysisTarget
AnalysisScope
AnalysisProgress

GraphNode
GraphEdge
Evidence
RiskAssessment

Change
PullRequest
WorkItem
Commit

SecurityFinding
SecurityScanner
SecurityEvidence

Report
Recommendation

Integration
AIConfiguration
PromptTemplate

NotificationChannel
NotificationRule
```

---

# 35. Non-Functional Requirements

## Trust

- Deterministic graph traversal.
- Explainable evidence.
- No AI-generated graph facts.
- Unknown instead of false confidence.

## Security

- Secret masking.
- Read-only database integration where appropriate.
- Enterprise authentication.
- API key protection.
- Least-privilege integration credentials.

## Resilience

- Individual integration failures must degrade only the affected subsystem.
- Core graph analysis should continue where possible.
- Errors must be explicit.
- Partial results must be labelled.

## Performance

- Incremental repository indexing where possible.
- Background analysis jobs.
- Streaming/progress updates.
- Graph traversal bounded by configurable depth.
- Pagination for historical data.

---

# 36. Current Technology Direction

The existing implementation uses:

- Python 3.11+
- FastAPI
- React + TypeScript
- React Flow
- .NET 9 SDK for the demo ecosystem
- SQL Server metadata analysis
- Azure DevOps integration
- Microsoft Foundry / OpenAI-compatible AI abstraction
- Azure AI Search
- Microsoft Teams
- Power Automate

The deterministic core should remain runnable without an AI service.

---

# 37. Quick Start

The existing development workflow is:

```powershell
# Backend
cd backend
python -m venv .venv
.\.venv\Scripts\python.exe -m pip install -r requirements.txt
.\.venv\Scripts\python.exe -m pytest
.\.venv\Scripts\python.exe -m uvicorn app.main:app --port 8000

# Frontend
cd frontend
npm install
npm run dev
```

Then:

```text
Login
  -> Connect Project
  -> Index Repository
  -> View Architecture
  -> Analyze a Change / PR / Work Item
  -> Review Progress
  -> Review Blast Radius
  -> Trace Evidence
  -> Review Security
  -> Export/Share Report
```

---

# 38. Figma-to-Implementation Acceptance Checklist

Every screen in the latest Figma should have a corresponding implemented route/state:

- [ ] Login
- [ ] Overview
- [ ] Projects
- [ ] Architecture
- [ ] Analyze Change
- [ ] Analysis Progress
- [ ] Analysis Results
- [ ] Trace Evidence
- [ ] Security Center
- [ ] Changes
- [ ] Pull Request Detail
- [ ] Work Item Detail
- [ ] Reports
- [ ] Report Detail
- [ ] Integrations
- [ ] AI Configuration
- [ ] Settings
- [ ] Notification Settings
- [ ] Empty States
- [ ] Error States

Each screen must also implement:

- [ ] loading state;
- [ ] empty state where applicable;
- [ ] error state where applicable;
- [ ] disabled state;
- [ ] success feedback;
- [ ] responsive behavior;
- [ ] keyboard-accessible controls;
- [ ] real API-backed data where functionality exists.

---

# 39. Product Rule

> **X-Ray should never claim certainty it does not have.**

The system's value is not simply producing an attractive dependency graph. It is producing a trustworthy chain:

```text
Change
  -> Changed Target
  -> Dependency Path
  -> Blast Radius
  -> Security Evidence
  -> Risk
  -> Explanation
  -> Recommended Action
```

That chain must be inspectable by the developer before code is shipped.
