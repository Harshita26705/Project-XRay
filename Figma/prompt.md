# Project X-Ray — Exact Frontend Implementation Prompt

You are an expert product designer and senior frontend engineer building **Project X-Ray**, an enterprise developer/security platform for dependency-graph-based change-impact and blast-radius analysis.

The attached Figma screenshots are the **visual source of truth**. Recreate the UI as closely as possible. Do not redesign, simplify, modernize, recolor, or invent alternative layouts.

The goal is a production-quality React + TypeScript frontend that looks like the supplied Figma screens and behaves like a real application.

---

## 1. Primary Objective

Build the complete Project X-Ray frontend represented by the supplied Figma screens.

The frontend must contain these routes/screens:

1. Login
2. Overview / Dashboard
3. Projects
4. Project Architecture / Dependency Graph
5. Analyze a Change
6. Analysis Progress
7. Analysis Results
8. Trace Evidence
9. Security Center
10. Changes
11. Pull Request Detail
12. Work Item Detail
13. Reports
14. Report Detail
15. Integrations
16. AI & Foundry Configuration
17. Settings
18. Notification Settings
19. Empty States
20. Error States

All screens share one enterprise application shell except Login and dedicated state-showcase pages.

---

# 2. Visual Direction

Use the Figma screenshots exactly as the visual reference.

### Overall style

- dark enterprise SaaS;
- developer-tool / observability / security-console aesthetic;
- restrained;
- technical;
- high information density without feeling cluttered;
- crisp borders;
- subtle panel elevation;
- compact typography;
- blue primary actions;
- semantic red/yellow/green/blue states;
- very dark page background;
- slightly lighter sidebar/topbar;
- muted gray-blue secondary text.

Do not introduce gradients, glassmorphism, large decorative illustrations, excessive rounded cards, oversized typography, or marketing-style hero sections unless visible in the reference.

---

# 3. Application Shell

Create a reusable shell.

## Left sidebar

Fixed vertical navigation containing:

- X-RAY logo/icon;
- `PROJECT ANALYSIS` small subtitle;
- Overview
- Projects
- Analyses
- Changes
- Security
- Reports
- Integrations
- Settings

At the bottom:

- Help & Docs
- separator;
- user avatar;
- user name;
- role such as `Enterprise Admin`.

The active navigation item uses a darker blue selected background and brighter text/icon.

Inactive items are muted.

Sidebar width should visually match the Figma proportion.

## Top bar

The top bar contains:

- breadcrumb;
- project selector;
- environment/branch selector where present;
- dependency graph search field;
- notification icon;
- user avatar.

Use compact controls.

Breadcrumbs must visually distinguish the current page.

---

# 4. Design Tokens

Create centralized design tokens.

Approximate the Figma visual system rather than using browser defaults.

Use tokens for:

```text
page background
sidebar background
topbar background
card background
secondary card background
border
primary blue
muted blue
critical red
risky amber/yellow
safe green
unknown gray/blue
primary text
secondary text
muted text
```

Do not hardcode colors throughout components.

Use CSS variables or a theme system.

---

# 5. Typography

Use a clean modern UI font similar to the Figma design.

Requirements:

- strong page titles;
- compact body copy;
- small uppercase metadata labels;
- monospace font for:
  - file paths;
  - code;
  - IDs;
  - branches;
  - telemetry;
  - technical evidence.

Typography must be dense enough for an enterprise engineering dashboard.

---

# 6. Common Components

Create reusable components:

```text
AppShell
Sidebar
Topbar
Breadcrumbs
ProjectSelector
EnvironmentSelector
GraphSearch
UserMenu
StatCard
RiskBadge
StatusBadge
DataTable
FilterBar
Tabs
Panel
Card
PrimaryButton
SecondaryButton
DangerButton
Toggle
Checkbox
Dropdown
SearchInput
CodeBlock
EvidenceCard
DependencyGraph
ProgressBar
EmptyState
ErrorState
RightDetailPanel
Pagination
Toast
Modal
ConfirmDialog
```

Do not duplicate the same visual component on individual pages.

---

# 7. Risk System

Create one reusable risk system.

Visible labels:

```text
CRITICAL
RISKY
SAFE
UNKNOWN
```

Visual semantics:

- Critical → red;
- Risky → amber/yellow;
- Safe → green;
- Unknown → muted blue/gray.

Risk badges should be compact, uppercase, and visually close to the Figma.

Never use risk color for large page backgrounds. Use it primarily for:

- badges;
- counts;
- node outlines;
- status text;
- alerts;
- small indicators.

---

# 8. Login Screen

Reproduce the Figma login screen.

Composition:

- dark full-page background;
- subtle central blue glow;
- centered authentication card;
- Project X-Ray icon;
- `PROJECT X-RAY`;
- tagline:
  `See the blast radius before you ship.`
- divider;
- blue:
  `Continue with Microsoft`
- secondary:
  `Continue with SSO`
- subtle enterprise security/Azure Active Directory note.

The login card must be vertically and horizontally centered.

Do not add:

- username/password form;
- registration;
- social login;
- unnecessary illustrations.

---

# 9. Overview Screen

Header:

```text
Good morning, Harshita
Here is what X-Ray found across your projects today.
```

Top stat cards:

- Total Analyses
- Critical Changes
- Components Analyzed
- Security Findings
- Risk Trend Index (24h)

Risk trend card contains:

- percentage trend;
- compact line chart.

Main content:

### Recent Analyses

Table columns:

- Change
- Type
- Risk
- Affected
- Created
- Status

### Quick Actions

Cards/actions:

- Analyze a Change
- Analyze PR
- View Architecture
- Run Security Scan

Match the compact two-column layout in the Figma.

---

# 10. Projects Screen

Header:

```text
Projects
Connect and manage dependency tracking directories.
```

Top-right:

`+ Connect Project`

Project card must contain:

- folder/project icon;
- CGOne;
- repository reference;
- CONNECTED badge;
- technology badges:
  - .NET
  - React
  - SQL Server
  - Azure DevOps
- Nodes;
- Relationships;
- Security;
- last indexed timestamp;
- Open Project;
- Re-index;
- Settings.

Next to it, create the dashed empty/add-project card:

```text
+
Connect New Repository
Add an Azure DevOps project path to
automatically construct its dependency blast
radius graph.
```

---

# 11. Architecture Screen

Title:

`CGOne Architecture`

Subtitle:

`Explore dependencies across frontend, backend and database schemas.`

Top-right risk legend:

- Critical
- Risky
- Safe

Node filter pills:

- Frontend
- API
- Controllers
- Services
- Repositories
- Database
- External

Edge filters:

- Calls
- Depends on
- Imports
- Reads
- Writes

Main panel:

- interactive dependency graph;
- dark canvas;
- subtle node cards;
- thin connection lines;
- compact node labels;
- type subtitles.

Example graph:

```text
CheckoutPage
     |
paymentApi.ts
     |
POST /api/payment
     |
PaymentController
     |
PaymentService
     |
PaymentRepository
     |
Payments Table
```

Include graph controls in the lower-right:

- plus;
- minus;
- fit;
- search.

Add a narrow right-side graph drawer/collapse control as represented in the Figma.

Use React Flow or equivalent.

---

# 12. Analyze Change Screen

Title:

`Analyze a Change`

Subtitle:

`Trace potential blast radius and find breaking dependency chains before pushing your code.`

Main form card has tabs:

- Work Item
- Pull Request
- Manual Change

For Work Item:

- Azure DevOps Work Item ID input;
- `Fetch Work Item`;
- connected status;
- `Run X-Ray Analysis`.

Right-side cards:

### Context

Show:

- TARGET PROJECT
- CGOne (Azure DevOps)
- WORKING BRANCH
- branch value.

### Analysis Scope

Checkboxes:

- Scan direct dependencies
- Trace recursive transitive dependencies
- Include external API bindings
- Perform static security analysis

Checked/unchecked states must match the Figma.

---

# 13. Analysis Progress Screen

Two main cards.

## Foundry Engine Pipeline

List pipeline stages with:

- completed check icons;
- active processing icon;
- disabled/pending icons.

Stages:

1. Reading Azure DevOps work item and changesets
2. Identifying affected file contexts & core targets
3. Building multi-tiered dependency blast radius paths
4. Checking static assemblies and credentials security
5. Retrieving downstream project environment context
6. Running dependency rule validation engine
7. Generating cryptographic blast radius report

## Engine Tracer Telemetry

Display:

- total components traced;
- estimated recursive dependency chains;
- console telemetry output in monospace.

Bottom:

```text
Analyzing Change
43% Completed
```

plus a horizontal progress bar.

Progress must be dynamic.

---

# 14. Analysis Results

This is the most important visual screen.

Header:

- CRITICAL badge;
- PR identifier;
- title:
  `Add payment validation checks`;
- subtitle;
- Re-Run Scan;
- View Pull Request.

Risk cards:

- 4 Critical
- 7 Risky
- 31 Safe
- 3 Unknown

Main panel:

`BLAST RADIUS MAPPING`

Interactive graph with affected nodes.

Right panel:

`X-RAY FINDINGS`

Sections:

- Structural Impact;
- Security Warnings;
- Failed Unit Tests.

Then:

`EXPERT EXPLANATION`

Provide concise evidence-based explanation.

Button:

`View Traceable Evidence`

Do not make the explanation panel visually dominant over the actual evidence/graph.

---

# 15. Trace Evidence Screen

Title:

`Trace Evidence`

Subtitle:

`Review verifiable structural and semantic static analysis proving X-Ray's conclusions.`

Create vertically stacked evidence cards.

### Card 1

Badge:

`DIRECT CHANGE`

File:

`PaymentService.cs`

Show:

- method;
- line range;
- syntax-highlighted code snippet.

### Card 2

Badge:

`STRUCTURAL DEPENDENCY`

File:

`PaymentController.cs`

Show:

- relationship;
- confidence;
- explanation.

### Card 3

Badge:

`DATABASE ACCESS`

File:

`PaymentRepository.cs`

Show:

- SQL/static analysis description.

### Card 4

Badge:

`SECURITY ALERT`

Finding:

`SAST SQL Injection Potential`

Show:

- severity;
- description.

Each card should be collapsible if the implementation needs more detail, while preserving the Figma visual hierarchy.

---

# 16. Security Center

Title:

`Security Center`

Subtitle:

`Security findings connected to your software architecture.`

Summary cards:

- Critical
- Risky
- Safe
- Unknown

Main table:

`Architecture Vulnerabilities`

Columns:

- Finding
- Severity
- Component
- Source
- Status

Rows should visually resemble the Figma.

Right detail panel:

- SQL Injection;
- CRITICAL badge;
- component;
- file;
- line number;
- description;
- code snippet;
- expandable:
  - Risk Impact
  - Recommended Remediation
  - Recommended Tests
  - Evidence;
- `Explain with Foundry`.

---

# 17. Changes Screen

Title:

`Changes`

Subtitle:

`Trace structural evolution and PR blast-radius across active workspaces.`

Tabs:

- Pull Requests
- Work Items
- Commits

Filter row:

- search;
- Risk Level;
- Status;
- Author;
- Date Range.

Table:

- PR;
- Title;
- Author;
- Risk;
- Affected;
- Status.

Rows are clickable.

---

# 18. Pull Request Detail

Header:

- risk badge;
- PR number;
- title;
- author;
- branch;
- creation time;
- files changed;
- lines changed.

Actions:

- Re-Run Scan
- Open in Azure DevOps

Tabs:

- Overview
- Blast Radius
- Security
- Tests
- Evidence

Overview:

### Change Summary

Large card.

### Expert Risk Analysis

Blue-accented analysis card.

Right:

`Affected Components (5)`

Each component is a compact card with:

- component name;
- type;
- risk badge.

---

# 19. Work Item Detail

Header:

- RISKY;
- WORK ITEM #9214;
- title:
  `Update checkout API`;
- subtitle;
- View in Azure DevOps.

Metadata card:

- Priority;
- Assigned To;
- State;
- Description.

Left content:

`X-RAY Predicted Impact`

Component cards:

- PaymentService
- PaymentController
- CheckoutPage

Then:

`FOUNDRY PREDICTIVE REPORT`

Right:

`Predicted Blast Radius`

Include a compact dependency graph.

Then:

- Recommended Implementation Areas;
- Recommended Tests;
- Security Considerations.

Use this page to clearly distinguish **predicted** impact from verified PR evidence.

---

# 20. Reports Screen

Title:

`Reports`

Subtitle:

`Review historical static analysis profiles, risk scorecards, and blast radius changes.`

Filters:

- Project
- Risk Level
- Date Range
- Type

Search:

`Search change ID or description...`

Table:

- Date;
- Change;
- Project;
- Risk;
- Affected Components;
- Security Findings;
- Status.

Bottom:

- result count;
- Previous;
- Next.

---

# 21. Report Detail

Header card:

- risk;
- PR/change ID;
- title;
- project;
- author;
- date;
- Export PDF;
- Markdown;
- Share Report.

Grid:

### Executive Summary

Text summary.

### Blast Radius

Counts:

- Critical Components;
- Risky Transitive Impacts.

`View Graph →`

### Security & Vulnerabilities

Finding summary.

Then two large cards:

### Microsoft Foundry Analysis & Explanation

AI explanation.

### Recommendations

Bulleted actionable recommendations with green indicators.

---

# 22. Integrations Screen

Title:

`Integrations`

Subtitle:

`Manage and configure data connectors, code hosts, and enterprise AI engines.`

Use a two-column card grid.

Cards:

### Azure DevOps

Purpose:

Repositories + Pull Requests + Work Items

### SQL Server DB

Purpose:

Database Metadata + Stored Procedures

Mark it:

`READ-ONLY`

### Microsoft Foundry

Purpose:

AI Reasoning + Language Logs

### Azure AI Search

Purpose:

Project Knowledge Retrieval + Query Slices

### Microsoft Teams

Purpose:

Notifications + Alert Streams

### Power Automate

Purpose:

Workflow Automation + Orchestration

Each card has:

- icon;
- title;
- Connected/Configured badge;
- description;
- capability footer;
- settings icon;
- Test Connection button.

---

# 23. AI Configuration Screen

Title:

`AI Configuration`

Subtitle:

`Orchestrate cognitive models, knowledge bases, and architectural pipelines.`

Top status card:

- Cognitive Provider;
- Deployment Status;
- Active Model;
- Knowledge Layer;
- Reconfigure Pipeline.

Main cards:

### Core Integration Philosophy

Explain that Foundry reasons over deterministic structural telemetry and cannot override dependency/risk truth.

### Cognitive Inference Pipeline

Visual flow:

```text
1. Dependency Graph
   >
2. Trace Collection
   >
3. AI Knowledge
   >
4. Cognitive Output
```

Bottom:

`Active Remediations & Templates`

Three cards:

- Model Settings
- Prompt Templates
- Usage & Rate Limits

---

# 24. Settings Screen

Title:

`Settings`

Subtitle:

`Configure organization preferences, analysis standards, and security baselines.`

Left internal settings navigation:

- General
- Projects
- Repositories
- Security
- AI Settings
- Notifications
- Access Control
- API Keys

General page:

### Organization Profile

Inputs:

- Organization Name
- Default Target Project

### Analysis Defaults

Toggles:

- Include security scan
- Include AI explanation
- Auto-analyze PRs

### Danger Zone

Red-accented panel:

`Delete Organization`

Explain that deletion permanently removes the organization and cannot be undone.

Buttons:

- Save Changes
- Cancel

---

# 25. Notification Settings

Title:

`Notification Settings`

Subtitle:

`Manage alert streams, webhooks, and granular event mapping across connected channels.`

### Notification Channels

Cards:

- Microsoft Teams
- Azure DevOps
- Email

Show connection/configured state.

### Granular Event Rules

Table-like matrix.

Rows:

- High-risk change detected
- Critical security finding
- Analysis completed
- Analysis failed
- New PR detected

Columns:

- Teams
- DevOps
- Email

Each cell has a toggle.

Bottom:

- Save Changes
- Cancel

---

# 26. Empty States

Create a dedicated page demonstrating empty states.

Four equal cards:

### No projects connected

Icon.

Text:

`Connect your Azure DevOps project workspace to start creating codebase structures.`

Button:

`Connect Project`

### No analyses yet

Text:

`Run your first X-Ray dependency sweep to determine high-risk areas of a change.`

Button:

`Analyze a Change`

### No security findings

Text:

`Compliance details and secret disclosures will populate here after running a scan.`

Button:

`Run Security Scan`

### No reports generated

Text:

`Reports summarize structural trends and are created automatically on analysis cycles.`

Button:

`View Analyses`

---

# 27. Error States

Create four equal cards.

### Analysis incomplete

Red warning icon.

Text explaining incomplete evidence.

Buttons/actions:

- Partial results available
- Retry Analysis

### Azure DevOps unavailable

Amber connection/offline icon.

Action:

`Retry Connection`

### Unknown Impact

Neutral/unknown icon.

Explain that X-Ray cannot determine the dependency relationship due to insufficient evidence.

Action:

`View Evidence`

### Analysis unavailable

Red icon.

Explain that Microsoft Foundry is not responding.

Action:

`Check AI Status`

---

# 28. Interaction Requirements

Buttons must work.

Examples:

- Connect Project opens project connection flow.
- Re-index triggers a visible loading/progress state.
- Analyze starts an analysis job.
- Progress screen updates.
- View Pull Request opens PR detail.
- View Traceable Evidence opens Evidence.
- View Graph opens Architecture.
- Explain with Foundry shows AI generation/loading/result.
- Export buttons initiate export behavior.
- Save Changes persists state.
- Cancel restores previous state.
- Retry actions retry the appropriate failed operation.
- Search filters the displayed data.
- Tabs change content.
- Filters affect tables.
- Graph controls work.

Do not create dead buttons unless the screen explicitly represents a static state.

---

# 29. Data and State

Use realistic mock data initially, but structure the frontend so API integration is straightforward.

Create typed models for:

```text
Project
Repository
GraphNode
GraphEdge
Analysis
AnalysisProgress
RiskAssessment
Evidence
SecurityFinding
PullRequest
WorkItem
Report
Integration
AIConfiguration
NotificationRule
```

Use a service layer instead of putting API calls directly inside visual components.

Support:

- loading;
- success;
- error;
- empty;
- partial-result;
- retry.

---

# 30. Graph Requirements

Use React Flow or an equivalent graph library.

Graph nodes must visually reflect:

- component type;
- risk;
- selected state;
- affected state.

Graph interactions:

- pan;
- zoom;
- fit;
- select;
- search;
- highlight dependencies;
- highlight blast radius;
- show relationship types.

The graph should support cross-layer relationships.

Example:

```text
CheckoutPage
  -> paymentApi.ts
  -> POST /api/payment
  -> PaymentController
  -> PaymentService
  -> PaymentRepository
  -> Payments Table
```

---

# 31. Accessibility

All interactive controls must support:

- keyboard navigation;
- visible focus;
- semantic labels;
- accessible buttons;
- accessible form controls;
- appropriate contrast;
- tooltip/aria labels for icon-only actions.

Do not rely on color alone to communicate risk.

---

# 32. Responsive Behavior

The supplied Figma is desktop-first.

Preserve the desktop composition at large sizes.

For smaller screens:

- collapse sidebar;
- preserve topbar actions;
- stack cards;
- convert multi-column panels to vertical sections;
- keep graph usable;
- avoid horizontal overflow where possible.

Do not change the visual identity.

---

# 33. Animation

Use subtle, functional animation only.

Allowed:

- page transitions;
- progress updates;
- graph selection;
- button loading;
- panel expansion;
- notification feedback.

Avoid:

- excessive motion;
- decorative animations;
- bouncing elements.

---

# 34. Important Product Rules

### Rule 1 — Deterministic truth

The graph and deterministic analysis engine are authoritative.

### Rule 2 — AI explains

AI can produce:

- explanations;
- summaries;
- recommendations;
- predictive text.

AI cannot invent:

- nodes;
- relationships;
- risk;
- distances;
- evidence;
- test results.

### Rule 3 — Unknown is valid

If evidence is insufficient, show:

`UNKNOWN`

Never automatically convert missing evidence into:

`SAFE`.

### Rule 4 — Degraded integrations

If Azure DevOps, SQL Server, Foundry, AI Search, Teams, or Power Automate is unavailable, show a clear integration/error/degraded state.

### Rule 5 — Evidence is first-class

Every important risk result must be traceable to evidence.

---

# 35. Suggested Frontend Structure

```text
frontend/
└── src/
    ├── components/
    │   ├── layout/
    │   ├── navigation/
    │   ├── common/
    │   ├── risk/
    │   ├── tables/
    │   ├── graph/
    │   ├── evidence/
    │   ├── security/
    │   └── forms/
    │
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
    │
    ├── services/
    ├── hooks/
    ├── state/
    ├── types/
    ├── styles/
    └── routes/
```

---

# 36. Implementation Sequence

Implement in this order:

### Phase 1 — Foundation

1. theme/tokens;
2. fonts;
3. AppShell;
4. sidebar;
5. topbar;
6. breadcrumbs;
7. buttons;
8. cards;
9. badges;
10. tables;
11. inputs.

### Phase 2 — Core Product

12. Overview;
13. Projects;
14. Architecture;
15. graph;
16. Analyze Change;
17. Analysis Progress;
18. Analysis Results;
19. Evidence.

### Phase 3 — Security & Change Management

20. Security Center;
21. Changes;
22. PR Detail;
23. Work Item Detail.

### Phase 4 — Reporting

24. Reports;
25. Report Detail.

### Phase 5 — Enterprise Integrations

26. Integrations;
27. AI Configuration;
28. Settings;
29. Notification Settings.

### Phase 6 — Robustness

30. Empty States;
31. Error States;
32. loading states;
33. API integration;
34. accessibility;
35. responsive behavior.

---

# 37. Final Fidelity Checklist

Before declaring the frontend complete, compare every screen against the supplied Figma screenshots.

Verify:

- [ ] same overall layout;
- [ ] same sidebar hierarchy;
- [ ] same navigation labels;
- [ ] same page hierarchy;
- [ ] same card proportions;
- [ ] same spacing rhythm;
- [ ] same typography hierarchy;
- [ ] same button hierarchy;
- [ ] same badge styles;
- [ ] same risk semantics;
- [ ] same table density;
- [ ] same graph placement;
- [ ] same right-side panels;
- [ ] same filters;
- [ ] same tabs;
- [ ] same empty/error states;
- [ ] same topbar controls;
- [ ] same dark visual language;
- [ ] same information hierarchy.

**Do not add features that are not represented by the Figma unless required to make an existing feature functional.**

The result should feel like one coherent enterprise product, not twenty unrelated pages.
