# Project X-Ray — Update Plan

## 1. Objective

Update Project X-Ray so the application can:

1. Analyze projects from either a local project path or a remote/global Git repository.
2. Connect to Azure DevOps Git repositories using the authenticated Microsoft account.
3. Treat Git branches as isolated analysis contexts.
4. Show the active branch throughout the UI and allow branch switching.
5. Maintain a persistent project knowledge layer using:
   - A Vector DB for semantic/code/document context.
   - A Knowledge Graph for structural and dependency relationships.
6. Incrementally update both stores whenever the repository changes.
7. Use the stored context for user-story analysis, dependency explanation, blast-radius analysis, security analysis, and LLM reasoning without sending the entire repository on every request.
8. Complete the remaining frontend features and replace incomplete/mock flows with working backend integration.

> **Source of truth:** Git repository state and commit history. Vector DB and Knowledge Graph are derived, versioned context stores.

---

# 2. Target Architecture

```text
React + Vite
     |
     v
.NET Web API
     |
     +-------------------+-------------------+
     |                   |                   |
Repository Service   Analysis Service   Context Service
     |                   |                   |
     |                   |            +------+------+
     |                   |            |             |
 Local / Azure DevOps   |       Vector DB     Knowledge Graph
     |                   |            |             |
     +---------+---------+------------+-------------+
               |
               v
       Microsoft Foundry / LLM
               |
               v
       Context-aware X-Ray
```

Core abstractions should include:

```text
IRepositoryProvider
IVectorStore
IKnowledgeGraph
ICodeParser
IEmbeddingService
IContextRetriever
IAnalysisEngine
```

---

# 3. Workstream A — Repository Management

## A1. Repository Provider Abstraction

**Owner:** Backend

Introduce an `IRepositoryProvider` abstraction so the analysis engine is independent of the repository source.

Responsibilities:

- Validate repository.
- Retrieve repository metadata.
- List branches.
- Resolve selected branch.
- Retrieve current branch HEAD.
- Retrieve commit history.
- Retrieve files.
- Retrieve changed files between commits.
- Retrieve source for a specific branch/commit.

Implement:

```text
RepositoryProvider
├── LocalRepositoryProvider
└── AzureDevOpsRepositoryProvider
```

### Acceptance Criteria

- Existing local-path workflow continues working.
- Remote repository support uses the same abstraction.
- Analysis services contain no provider-specific logic.
- Provider failures return structured errors.

---

## A2. Local Repository Support

**Owner:** Backend

Formalize the current local-path workflow.

Support:

- Repository root validation.
- Git detection.
- Current branch detection.
- Current commit SHA.
- Branch listing.
- File enumeration.
- Changed-file detection.
- Repository ignore rules.

Do not index irrelevant/generated content such as:

```text
.git/
node_modules/
bin/
obj/
dist/
build/
coverage/
.vscode/
.idea/
generated binaries
large binary files
temporary files
```

---

## A3. Azure DevOps Repository Provider

**Owner:** Backend + Azure DevOps support

Allow the user to enter/select a global Azure DevOps repository.

Support:

1. Repository validation.
2. Microsoft-account authentication.
3. Azure DevOps authorization.
4. Repository metadata.
5. Branch listing.
6. Branch HEAD commit.
7. Source retrieval.
8. Commit history.
9. Changed files.
10. Permission/error handling.

The frontend must never receive repository credentials or secrets.

### Acceptance Criteria

A user can:

```text
Login with Microsoft
→ Connect Azure DevOps Repository
→ See accessible repositories
→ Select repository
→ See branches
→ Select branch
→ Index/analyze branch
```

---

# 4. Workstream B — Microsoft Authentication & Azure DevOps

## B1. Authentication Context

**Owner:** Backend/Auth

Ensure the authenticated Microsoft identity is available consistently to backend services.

Flow:

```text
Microsoft Account
       ↓
Application Identity
       ↓
Azure DevOps Authorization
       ↓
Accessible Organizations/Projects/Repositories
```

Do not store passwords or unnecessary long-lived credentials.

---

## B2. Azure DevOps Permissions

Create a dedicated authorization layer to determine:

- Accessible Azure DevOps organizations.
- Accessible projects.
- Accessible repositories.
- Repository read permission.
- Branch/commit access.

Repository services should consume an authorization context instead of implementing authentication themselves.

---

# 5. Workstream C — Branch-Aware Architecture

## C1. Branch as a First-Class Entity

**Owner:** Backend + Database

A Project must no longer represent one static codebase.

Use the conceptual hierarchy:

```text
Organization
  └── Project
       └── Repository
            └── Branch
                 └── Commit
                      └── Context Version
```

Every analysis must identify:

```text
Project
Repository
Branch
Commit SHA
Context Version
Analysis
```

---

## C2. Branch Switching

**Owner:** Frontend + Backend

Add a persistent branch selector to the project experience.

Example:

```text
CGOne
Environment: Development
Branch: feature/payment-validation ▼
```

The selector must:

- Show current branch.
- List accessible branches.
- Indicate active branch.
- Show whether the branch is indexed.
- Trigger synchronization/indexing when required.
- Refresh architecture, findings, changes, and analysis context.

### Mandatory Isolation Rule

If the user is viewing:

```text
feature/payment-validation
```

the application must not accidentally show:

```text
main graph
main security findings
another branch's analysis
```

Every relevant backend query must include branch/context scope.

---

# 6. Workstream D — Repository Ingestion

## D1. Ingestion Pipeline

**Owner:** Backend

Implement:

```text
Repository
   ↓
Branch
   ↓
Commit
   ↓
File Discovery
   ↓
Classification
   ↓
Parsing
   ↓
Symbol Extraction
   ↓
Dependency Extraction
   ↓
Security Extraction
   ↓
Chunking
   ↓
Embeddings
   ↓
Vector DB
   ↓
Knowledge Graph
```

Initial language support:

```text
C#
React
TypeScript
JavaScript
SQL / SQL Server
JSON/YAML/configuration
Project/package manifests
```

Architecture should allow more languages later.

---

## D2. File Classification

Classify files as:

```text
SourceCode
Component
Controller
Service
Repository
DatabaseScript
Configuration
Test
Documentation
DependencyManifest
APIContract
Generated
Unknown
```

Generated/irrelevant content should not pollute the main context.

---

## D3. Symbol Extraction

For C#:

```text
Namespace
Class
Interface
Method
Property
Field
Constructor
Controller
Service
Repository
DTO
Entity
```

For React/TypeScript:

```text
Component
Hook
Function
Class
Type
Interface
API Client
Route
State Module
Utility
```

For SQL:

```text
Table
Column
View
Stored Procedure
Function
Trigger
Foreign Key
Index
```

Every extracted entity must be traceable to:

```text
Repository
Branch
Commit
File
Line Range
Symbol
```

---

# 7. Workstream E — Knowledge Graph

## E1. Persistent Project Graph

**Owner:** Backend/Graph

The graph should model structural relationships.

Example:

```text
CheckoutPage
   └── calls → paymentApi.ts
                  └── calls → POST /api/payment
                                └── routes to → PaymentController
                                                   └── calls → PaymentService
                                                                  └── calls → PaymentRepository
                                                                                 └── writes → Payments
```

---

## E2. Graph Node Types

Minimum:

```text
Repository
Branch
Commit
File
Namespace
Class
Interface
Method
Function
ReactComponent
APIEndpoint
Controller
Service
RepositoryClass
Database
Table
Column
StoredProcedure
ExternalService
Package
Test
Configuration
Environment
```

---

## E3. Graph Edge Types

Minimum:

```text
CONTAINS
IMPORTS
CALLS
DEPENDS_ON
IMPLEMENTS
INHERITS
ROUTES_TO
READS
WRITES
QUERIES
USES
REFERENCES
TESTS
CONFIGURES
DEPENDS_ON_PACKAGE
EXTERNAL_CALL
```

Edges should retain provenance where possible:

```text
Source Node
Target Node
Relationship
Branch
Commit
File
Line
Confidence
Detection Method
```

---

# 8. Workstream F — Vector Database Context

## F1. Semantic Project Memory

**Owner:** Backend/AI

Do not embed only entire files. Create meaningful chunks:

```text
Class
Method
React Component
API Endpoint
SQL Object
Documentation
Configuration
Test
```

Each vector record should contain metadata such as:

```text
projectId
repositoryId
branchId
commitSha
fileId
symbolId
filePath
language
nodeType
lineStart
lineEnd
contentHash
```

---

## F2. Context Retrieval

Implement:

```text
User Question
     ↓
Intent Detection
     ↓
Vector Search
     +
Knowledge Graph Traversal
     +
Exact File/Symbol Lookup
     ↓
Context Ranking
     ↓
Focused Context
     ↓
LLM
```

The LLM should receive relevant context rather than the entire repository.

---

# 9. Workstream G — Incremental Updates

## G1. Change Detection

**Owner:** Backend

Track:

```text
Last Indexed Commit
Current Branch HEAD
```

Then:

```text
Previous Commit
     ↓
Current Commit
     ↓
Changed Files
     ↓
Changed Symbols
     ↓
Affected Graph Nodes
     ↓
Affected Vector Chunks
```

Do not rebuild the complete project unnecessarily.

---

## G2. Content Hashing

Track a content hash for every indexed artifact:

```text
filePath
contentHash
commitSha
lastIndexedAt
```

If content is unchanged:

```text
DO NOT re-parse
DO NOT re-embed
DO NOT recreate graph relationships unnecessarily
```

---

## G3. Incremental Graph Update

When a file changes:

1. Re-parse the file.
2. Identify changed symbols.
3. Remove/replace obsolete relationships for the affected version.
4. Add new relationships.
5. Recalculate affected dependencies.
6. Preserve historical versions where required.

---

## G4. Incremental Vector Update

When a chunk changes:

```text
Old Chunk
   ↓
Mark Obsolete / Versioned
   ↓
Generate New Embedding
   ↓
Store New Vector
```

Avoid duplicate active context for the same:

```text
Project + Branch + File/Symbol + Content Version
```

---

# 10. Workstream H — Context Versioning

Every stored context state must be version-aware.

Hierarchy:

```text
Project
  → Repository
    → Branch
      → Commit
        → Context Version
```

An analysis against:

```text
feature/payment-validation @ abc123
```

must never silently use context from:

```text
main @ xyz789
```

---

# 11. Workstream I — X-Ray Analysis Integration

Update existing analysis services to use persistent project context.

Target flow:

```text
PR / Work Item / Manual Change / User Story
              ↓
      Project + Repository
              ↓
          Branch + Commit
              ↓
      Existing Knowledge Graph
              ↓
       Vector Context Search
              ↓
      Dependency Traversal
              ↓
      Security / Test Analysis
              ↓
       Focused LLM Context
              ↓
      Microsoft Foundry / LLM
              ↓
      X-Ray Result + Evidence
```

All results must identify the exact branch and commit analyzed.

---

# 12. Workstream J — User Story / Blast Radius

Input:

```text
User Story
Project
Repository
Branch
Commit
```

Process:

```text
User Story
   ↓
Intent Extraction
   ↓
Potential Components
   ↓
Knowledge Graph Traversal
   ↓
Vector Context Retrieval
   ↓
Affected Components
   ↓
Risk Analysis
   ↓
Blast Radius
```

Output:

```text
Affected Components
Risk
Blast Radius
Security Impact
Recommended Changes
Recommended Tests
Traceable Evidence
```

---

# 13. Workstream K — Frontend Completion

**Owner:** Frontend**

The existing Figma design should be treated as the target product experience. Complete incomplete screens and replace static/mock interactions with real APIs.

## K1. Authentication

Complete:

- Microsoft login.
- SSO where applicable.
- Authentication state.
- Session persistence.
- Logout.
- Unauthorized state.
- Authentication errors.

## K2. Overview

Connect real data for:

- Total Analyses.
- Critical Changes.
- Components Analyzed.
- Security Findings.
- Risk trend.
- Recent Analyses.
- Quick Actions.

## K3. Projects

Complete:

- Connect Project.
- Local repository selection.
- Azure DevOps repository URL/selection.
- Repository validation.
- Repository connection.
- Branch discovery.
- Branch selection.
- Re-index.
- Project settings.
- Connection errors.
- Empty states.

## K4. Branch Selector

Add branch selection to the project header/navigation.

It must display:

```text
Repository
Environment
Current Branch
Current Commit / sync status
```

Switching branch must refresh all branch-scoped screens.

## K5. Architecture

Complete:

- Real graph data.
- Branch-aware graph.
- Node filters.
- Edge filters.
- Search.
- Zoom/pan.
- Node details.
- Relationship details.
- Risk visualization.
- Loading/empty/error states.

## K6. Analyze Change

### Work Item

- Work Item ID.
- Fetch Work Item.
- Work Item details.
- Branch selection.
- Scope configuration.
- Run analysis.

### Pull Request

- PR selection/input.
- Fetch PR.
- Source/target branch context.
- Changed files.
- Scope configuration.
- Run analysis.

### Manual Change

- Manual input.
- Branch selection.
- File/component selection where applicable.
- Scope configuration.
- Run analysis.

## K7. Analysis Progress

Connect to real background-job state.

Show:

```text
Current stage
Completed stages
Components traced
Dependency chains
Security scan
Context retrieval
AI analysis
Overall percentage
Telemetry/logs
```

Do not fake progress once backend jobs are available.

## K8. Analysis Results

Complete:

- Risk summary.
- Blast radius graph.
- Findings.
- Structural impact.
- Security warnings.
- Failed tests.
- Unknown impact.
- Expert explanation.
- Traceable evidence.
- Re-run analysis.

Every result must contain:

```text
Project
Repository
Branch
Commit
Analysis ID
```

## K9. Trace Evidence

Complete:

- Direct change evidence.
- Structural dependency evidence.
- Database evidence.
- Security evidence.
- Code excerpts.
- File/line navigation.
- Confidence.
- Provenance.

## K10. Security Center

Complete:

- Findings list.
- Severity filters.
- Component details.
- File/line.
- Code snippets.
- Risk impact.
- Recommended remediation.
- Recommended tests.
- Evidence.
- Foundry explanation.

## K11. Changes

Complete:

- Pull Requests.
- Work Items.
- Commits.
- Search.
- Risk filters.
- Status filters.
- Author filters.
- Date filters.
- Branch filters.

## K12. PR Detail

Complete:

- Overview.
- Blast Radius.
- Security.
- Tests.
- Evidence.
- Change summary.
- Expert risk analysis.
- Affected components.
- Azure DevOps navigation.
- Re-run scan.

## K13. Work Item Detail

Complete:

- Work Item metadata.
- Predicted impact.
- Predicted blast radius.
- Foundry predictive report.
- Recommended implementation areas.
- Recommended tests.
- Security considerations.
- Azure DevOps navigation.

## K14. Reports

Complete:

- Project filter.
- Branch filter.
- Risk filter.
- Date range.
- Report type.
- Pagination.
- Report detail.
- PDF export.
- Markdown export.
- Share report.

## K15. Integrations

Complete real connection status/configuration for:

```text
Azure DevOps
SQL Server
Microsoft Foundry
Azure AI Search
Microsoft Teams
Power Automate
```

Each integration needs:

- Connection status.
- Test connection.
- Configuration.
- Error state.
- Last successful connection.
- Permission/capability information.

## K16. AI / Foundry Configuration

Complete:

- Provider status.
- Active model.
- Knowledge layer.
- Pipeline configuration.
- Model settings.
- Prompt templates.
- Usage/rate limits.
- Reconfigure pipeline.
- Connection testing.

## K17. Settings

Complete:

```text
General
Projects
Repositories
Security
AI Settings
Notifications
Access Control
API Keys
```

Persist settings through backend APIs.

## K18. Notification Settings

Channels:

```text
Microsoft Teams
Azure DevOps
Email
```

Events:

```text
High-risk change detected
Critical security finding
Analysis completed
Analysis failed
New PR detected
```

Persist channel/event rules through the backend.

## K19. Empty States

Make these functional:

- No projects connected.
- No analyses yet.
- No security findings.
- No reports generated.

## K20. Error States

Implement:

- Analysis incomplete.
- Azure DevOps unavailable.
- Unknown impact.
- Analysis unavailable.
- Repository unavailable.
- Branch unavailable.
- Context indexing failure.
- Vector DB failure.
- Knowledge Graph failure.
- AI/Foundry failure.
- Authentication failure.
- Authorization failure.

Never silently fall back to another branch/project's data.

---

# 14. Workstream L — API Layer

Create/complete APIs around these domains.

## Repository

```text
POST /api/projects
POST /api/projects/{projectId}/repositories
GET  /api/repositories/{repositoryId}
POST /api/repositories/{repositoryId}/validate
GET  /api/repositories/{repositoryId}/branches
POST /api/repositories/{repositoryId}/sync
```

## Branch

```text
GET  /api/projects/{projectId}/branches
POST /api/projects/{projectId}/branches/{branchId}/activate
GET  /api/branches/{branchId}/status
GET  /api/branches/{branchId}/commits
```

## Indexing

```text
POST /api/branches/{branchId}/index
GET  /api/branches/{branchId}/index/status
POST /api/branches/{branchId}/index/incremental
```

## Graph

```text
GET /api/branches/{branchId}/graph
GET /api/branches/{branchId}/graph/nodes/{nodeId}
GET /api/branches/{branchId}/graph/nodes/{nodeId}/dependencies
GET /api/branches/{branchId}/graph/nodes/{nodeId}/dependents
```

## Context

```text
POST /api/context/search
POST /api/context/retrieve
GET  /api/branches/{branchId}/context/status
```

## Analysis

```text
POST /api/analyses
GET  /api/analyses/{analysisId}
GET  /api/analyses/{analysisId}/progress
GET  /api/analyses/{analysisId}/results
GET  /api/analyses/{analysisId}/evidence
POST /api/analyses/{analysisId}/rerun
```

Routes can be adjusted to match existing project conventions.

---

# 15. Workstream M — MSSQL Schema

Extend the existing MSSQL schema rather than creating a second unrelated model.

Minimum conceptual entities:

```text
Project
Repository
RepositoryProvider
Branch
Commit
File
FileVersion
Symbol
SymbolVersion
Dependency
DependencyVersion
ContextVersion
VectorDocument
GraphNode
GraphEdge
IndexJob
IndexJobItem
Analysis
AnalysisScope
AnalysisFinding
Evidence
```

Important uniqueness boundaries must include branch/version where applicable.

The schema must support:

```text
Same repository
→ multiple branches
→ multiple commits
→ multiple context versions
→ multiple analyses
```

---

# 16. Workstream N — Background Jobs

Repository indexing and analysis must not block HTTP requests.

Use background jobs for:

```text
Repository Sync
Branch Indexing
Incremental Indexing
Graph Construction
Embedding Generation
Security Scanning
Analysis
Report Generation
```

Job states:

```text
Queued
Running
Completed
Failed
Retrying
```

Track:

```text
Job ID
Project
Repository
Branch
Commit
Started At
Completed At
Progress
Current Stage
Error
Retry Count
```

---

# 17. Workstream O — Consistency & Isolation

These are mandatory.

## O1. Branch Isolation

Every context query must be scoped to the active branch and appropriate commit/context version.

## O2. Project Isolation

A project must never retrieve another project's context.

## O3. Repository Isolation

Repositories must not share code context unless an explicit cross-repository dependency is modeled.

## O4. Tenant Isolation

Organization/tenant data must remain isolated.

## O5. Analysis Reproducibility

Store:

```text
Branch
Commit SHA
Context Version
Analysis Configuration
```

An old analysis must remain understandable even after the branch changes.

---

# 18. Workstream P — Security

Cover:

- Microsoft authentication.
- Azure DevOps authorization.
- Repository permissions.
- API authorization.
- Tenant isolation.
- Secret management.
- SQL access.
- Vector DB access.
- Knowledge Graph access.
- Prompt/context injection resistance.
- Sensitive source-code handling.
- Audit logging.

Never expose repository credentials to the frontend.

Do not send the complete repository to the LLM by default. Retrieve the minimum relevant context.

---

# 19. Workstream Q — Observability

Track:

```text
Repository sync duration
Files discovered
Files changed
Files skipped
Files parsed
Symbols extracted
Graph nodes created/updated
Graph edges created/updated
Embeddings created/updated
Embedding failures
Index duration
Context retrieval duration
LLM request duration
Analysis duration
```

Expose useful progress to the UI without exposing secrets.

---

# 20. Recommended Implementation Sequence

## Phase 1 — Current-State Audit

1. Inventory .NET APIs/services.
2. Inventory React/Vite screens.
3. Identify mock/static data.
4. Identify incomplete frontend features.
5. Verify current MSSQL schema.
6. Verify current Microsoft authentication.

**Deliverable:** current-state implementation map.

---

## Phase 2 — Repository Abstraction

1. `IRepositoryProvider`.
2. Local provider.
3. Git metadata.
4. Branch discovery.
5. Commit discovery.
6. Changed-file detection.

**Deliverable:** local repository works through the provider abstraction.

---

## Phase 3 — Azure DevOps

1. Microsoft authentication.
2. Azure DevOps authorization.
3. Repository connection.
4. Repository validation.
5. Branch listing.
6. Commit retrieval.
7. File retrieval.
8. Permission/error handling.

**Deliverable:** user can connect an Azure DevOps repository and select a branch.

---

## Phase 4 — Branch-Aware Backend

1. Branch model.
2. Commit model.
3. Active branch.
4. Branch-aware APIs.
5. Branch-aware analysis.
6. Branch-aware DB queries.

**Deliverable:** backend operates correctly against the selected branch.

---

## Phase 5 — Knowledge Ingestion

1. File classification.
2. C# parsing.
3. React/TypeScript parsing.
4. SQL parsing.
5. Symbol extraction.
6. Dependency extraction.
7. Graph construction.

**Deliverable:** complete project knowledge graph for a branch/commit.

---

## Phase 6 — Vector Context

1. Chunking.
2. Metadata.
3. Embeddings.
4. Vector storage.
5. Semantic retrieval.
6. Hybrid graph + vector retrieval.

**Deliverable:** LLM retrieves relevant project context without receiving the entire repository.

---

## Phase 7 — Incremental Synchronization

1. HEAD detection.
2. Commit comparison.
3. Changed-file detection.
4. Content hashing.
5. Incremental parsing.
6. Incremental graph update.
7. Incremental embedding update.
8. Context version creation.

**Deliverable:** repository updates update context efficiently.

---

## Phase 8 — X-Ray Integration

Connect:

```text
PR
Work Item
Manual Change
User Story
```

to:

```text
Branch
Commit
Knowledge Graph
Vector Context
Security Engine
Foundry
```

**Deliverable:** X-Ray analysis uses persistent project context.

---

## Phase 9 — Frontend Completion

Priority:

```text
Authentication
→ Projects
→ Repository Connection
→ Branch Selector
→ Architecture
→ Analysis
→ Analysis Progress
→ Results
→ Evidence
→ Security
→ Changes
→ Reports
→ Integrations
→ Settings
→ Notifications
→ Empty/Error States
```

---

## Phase 10 — Hardening

1. Security review.
2. Branch-isolation tests.
3. Tenant-isolation tests.
4. Incremental-indexing tests.
5. Large-repository tests.
6. Failure/retry tests.
7. Performance tests.
8. Frontend integration tests.
9. API contract tests.
10. End-to-end tests.

---

# 21. Definition of Done — End-to-End Scenario

The feature set is complete when this workflow works:

```text
User logs in with Microsoft
        ↓
Connect Project
        ↓
Connect/select Azure DevOps repository
        ↓
Validate access
        ↓
List branches
        ↓
Select feature/payment-validation
        ↓
Index branch
        ↓
Create Vector Context + Knowledge Graph
        ↓
Open Architecture
        ↓
View branch-specific graph
        ↓
Enter user story:
"Add payment validation to checkout."
        ↓
Retrieve relevant code + graph + semantic context
        ↓
LLM / Foundry reasoning
        ↓
Affected Components
Risk
Blast Radius
Security Impact
Recommendations
Tests
Evidence
        ↓
Developer pushes a new commit
        ↓
Detect new HEAD
        ↓
Index only changed/affected context
        ↓
Update branch graph + vector context
        ↓
Switch to main
        ↓
Use main's graph/vector context
```

No branch may leak context into another branch.

---

# 22. Priority Matrix

| Priority | Area | Importance |
|---|---|---|
| P0 | Branch-aware repository model | Critical |
| P0 | Azure DevOps integration | Critical |
| P0 | Microsoft authentication/authorization | Critical |
| P0 | Knowledge Graph | Critical |
| P0 | Vector project context | Critical |
| P0 | Incremental context updates | Critical |
| P0 | Branch isolation | Critical |
| P0 | X-Ray analysis integration | Critical |
| P1 | Repository ingestion/parsing | High |
| P1 | Architecture graph UI | High |
| P1 | Branch switcher UI | High |
| P1 | Project connection UI | High |
| P1 | Analysis/results integration | High |
| P1 | Security Center | High |
| P1 | Trace Evidence | High |
| P1 | PR/Work Item integration | High |
| P2 | Reports/export/share | Medium |
| P2 | Notifications | Medium |
| P2 | Integration settings | Medium |
| P2 | Advanced AI configuration | Medium |
| P2 | Additional repository providers | Future |

---

# 23. Engineering Principles

### Single Responsibility

Keep repository access, parsing, graph construction, vector indexing, retrieval, analysis, and presentation separate.

### Open/Closed

Adding GitHub/GitLab later should require another repository provider, not rewriting the analysis engine.

### Dependency Inversion

Core services should depend on interfaces rather than concrete database/vector/graph implementations.

### Explicit Versioning

Never assume the latest repository state is the state used by an earlier analysis.

### Fail Closed

If branch/context cannot be resolved, do not silently analyze another branch.

### Traceability

Important findings and LLM explanations should be traceable to repository evidence whenever possible.

---

# 24. Final Product Goal

Project X-Ray should evolve from:

```text
"Provide a project → analyze it"
```

into:

```text
"Connect a living repository
→ maintain a continuously updated, branch-aware project knowledge system
→ understand any part of the project
→ analyze changes and user stories against the correct code state
→ explain the blast radius with traceable evidence."
```

The four core layers are:

```text
Git Repository
    = Source of Truth + Version History

Knowledge Graph
    = Structural Memory

Vector DB
    = Semantic Memory

Microsoft Foundry / LLM
    = Reasoning Layer
```

Together they form the persistent intelligence layer of Project X-Ray.
