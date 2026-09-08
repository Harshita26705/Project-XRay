# Project X-Ray — GitHub Copilot Implementation Prompt

## ROLE

You are the lead software architect and senior full-stack engineer responsible for building **Project X-Ray**, an AI-powered change-impact and blast-radius analysis platform for software projects.

Build this project carefully, incrementally, and production-mindedly, but optimize the first release for a **hackathon MVP** that is reliable, demonstrable, explainable, and easy to run.

The project must use **Microsoft Foundry** as the AI reasoning layer.

The project should integrate with:
- Azure DevOps
- Azure AI Search
- Microsoft Foundry
- A read-only project database connection
- Git repositories / source code
- Frontend
- Backend
- Microsoft Teams
- Security scanning
- Optional sandbox verification

The core user experience is:

> Developer creates a ticket or PR → X-Ray understands the requested change → calculates dependency blast radius → uses project knowledge + Microsoft Foundry to explain the impact → checks security → displays affected components as GREEN/YELLOW/RED → reports the result back to the developer.

---

# 1. NON-NEGOTIABLE IMPLEMENTATION RULES

## Rule 1 — Work incrementally

DO NOT attempt to generate the entire project in one giant implementation.

Implement one phase at a time.

After every phase:

1. Run relevant tests.
2. Verify the implementation.
3. Explain what was created/changed.
4. Tell me exactly what I must do manually.
5. Tell me exactly which Azure/Microsoft resources I must create or configure.
6. Tell me exactly which environment variables/secrets I must provide.
7. Tell me exactly how to verify the phase.
8. Ask me to confirm before moving to the next major phase.

After each phase, include:

### ACTION REQUIRED FROM ME
- [ ] ...
- [ ] ...

If nothing is required:

> ACTION REQUIRED FROM ME: Nothing for this phase.

Never silently assume that an Azure resource, credential, model deployment, webhook, Teams connection, database connection, or API key exists.

## Rule 2 — Never invent credentials or resource values

Never invent:
- API keys
- tenant IDs
- subscription IDs
- client IDs
- client secrets
- endpoints
- connection strings
- Azure resource names
- Azure DevOps URLs

Use `.env.example` placeholders.

Never commit secrets.

Ensure `.gitignore` excludes `.env`, credentials, tokens and local secret files.

## Rule 3 — Prefer Microsoft services

Preferred stack:
- Microsoft Foundry → AI reasoning/agent
- Azure AI Search → vector + hybrid project knowledge
- Azure DevOps → repos, work items, PRs, commits
- Power Automate → workflow/orchestration/notifications
- Microsoft Teams → notifications
- Azure compute → deployment if required
- Read-only SQL connection → database metadata/context
- Real security tooling → security evidence
- React/Next.js → frontend
- Python/FastAPI → backend

Do not introduce unnecessary infrastructure.

## Rule 4 — Do not hallucinate capabilities

If the current Microsoft SDK/API differs from this prompt, use the **current supported approach** and isolate it behind an adapter.

Never fabricate an API, SDK method, connector, model deployment, Azure resource, or configuration.

If something cannot be implemented without a manual action from me, stop and tell me exactly what I need to do.

---

# 2. CORE ARCHITECTURE

Implement this conceptual architecture:

```text
                    AZURE DEVOPS
              ┌─────────────────────┐
              │ Repos               │
              │ Work Items / Tickets│
              │ Pull Requests       │
              │ Commits             │
              └──────────┬──────────┘
                         │
                         ▼
                 ┌───────────────┐
                 │ POWER AUTOMATE│
                 │ Event trigger │
                 │ Workflow      │
                 │ Notifications │
                 └───────┬───────┘
                         │
                         ▼
                 ┌───────────────┐
                 │  X-RAY API    │
                 │ FastAPI       │
                 └───────┬───────┘
                         │
             ┌───────────┼────────────┐
             │           │            │
             ▼           ▼            ▼
       Dependency    Azure AI      Security
          Graph       Search        Analysis
             │           │            │
             └───────────┼────────────┘
                         ▼
                ┌─────────────────┐
                │ Microsoft       │
                │ Foundry         │
                │ X-Ray Agent     │
                └────────┬────────┘
                         │
                         ▼
                  ┌─────────────┐
                  │ Risk Engine │
                  └──────┬──────┘
                         │
                ┌────────┴────────┐
                ▼                 ▼
         X-Ray Dashboard       Reports
         Dependency Graph
         GREEN/YELLOW/RED
                │
                ▼
         Power Automate
          │          │
          ▼          ▼
        Teams       Azure DevOps
                    PR Comment
```

---

# 3. CRITICAL DESIGN PRINCIPLE: GRAPH + KNOWLEDGE

Do NOT store the entire project as one giant vector database.

Create a **Project Knowledge Base** with two complementary parts.

## A. Dependency Graph

The graph answers:

> What is connected to what?

Examples:

```text
CheckoutPage → POST /payments
POST /payments → PaymentService
OrderService → PaymentService
PaymentService → PaymentRepository
PaymentRepository → payment_transactions
```

## B. Vector / Knowledge Index

The knowledge index answers:

> What does this component mean?

Examples:

```text
PaymentService:
Handles payment validation and payment processing.

Checkout:
Uses payment confirmation before completing checkout.

Previous incident:
A payment validation change affected checkout.
```

Use Azure AI Search for vector + hybrid retrieval.

## C. Microsoft Foundry

Foundry answers:

> What does this proposed change mean for the system?

Foundry should reason over:
- dependency graph facts
- retrieved project knowledge
- ticket/PR context
- security findings
- historical context
- test results when available

## D. Optional sandbox

The sandbox answers:

> Did the predicted impact actually occur when the change was executed?

Sandbox is a later/stretch feature and must NOT block the MVP.

---

# 4. TECHNOLOGY STACK

Use this stack unless a strong technical reason exists to change it.

## Frontend
- React
- TypeScript
- Vite or Next.js
- React Flow for dependency visualization
- Tailwind CSS or another simple component system

## Backend
- Python
- FastAPI
- Pydantic
- pytest
- httpx
- async processing where useful

## Knowledge
- Azure AI Search
- Microsoft/Azure-supported embeddings
- Hybrid keyword + vector search

## AI
- Microsoft Foundry
- Current supported Foundry SDK/API
- Foundry agent/model appropriate to the current Microsoft platform
- Tool/function calling where useful

Keep Foundry integration in a dedicated adapter/service so SDK changes do not affect the rest of the application.

## Graph
For MVP, use the simplest reliable approach:
1. PostgreSQL tables for nodes/edges, OR
2. Azure Cosmos DB if already justified, OR
3. in-memory graph for local demo with persistence later.

Do NOT add a dedicated graph database unless genuinely necessary.

## Database
The application may connect to an existing project DB in READ-ONLY mode.

Never allow X-Ray to write to the project DB.

## Integrations
- Azure DevOps REST APIs
- Azure DevOps Service Hooks/webhooks
- Power Automate
- Microsoft Teams

---

# 5. REPOSITORY STRUCTURE

Create a clean monorepo:

```text
project-xray/
│
├── backend/
│   ├── app/
│   │   ├── api/
│   │   ├── core/
│   │   ├── models/
│   │   ├── schemas/
│   │   ├── services/
│   │   │   ├── azure_devops.py
│   │   │   ├── ingestion.py
│   │   │   ├── parser.py
│   │   │   ├── dependency_graph.py
│   │   │   ├── azure_search.py
│   │   │   ├── foundry.py
│   │   │   ├── security.py
│   │   │   ├── blast_radius.py
│   │   │   ├── risk_engine.py
│   │   │   ├── teams.py
│   │   │   └── reporting.py
│   │   ├── prompts/
│   │   └── main.py
│   ├── tests/
│   ├── requirements.txt
│   └── .env.example
│
├── frontend/
│   ├── src/
│   │   ├── components/
│   │   ├── pages/
│   │   ├── services/
│   │   ├── types/
│   │   └── App.tsx
│   ├── package.json
│   └── .env.example
│
├── scripts/
│   ├── seed_demo_project.py
│   ├── ingest_repository.py
│   └── run_demo_analysis.py
│
├── docs/
│   ├── architecture.md
│   ├── setup.md
│   ├── api.md
│   └── demo.md
│
├── infra/
│   └── README.md
│
├── .gitignore
├── README.md
└── docker-compose.yml
```

Keep modules small and testable.

---

# 6. PHASE 0 — CREATE A CONTROLLED DEMO APPLICATION

Before connecting a real enterprise application, create a small representative demo project.

Architecture:

```text
frontend
   ↓
API Gateway
   ↓
Order Service
   ↓
Payment Service
   ↓
Payment Database

Order Service → Inventory Service
Frontend → User Service
```

Include:
- frontend
- backend services
- API routes
- service-to-service calls
- repository/data-access layer
- SQL schema
- documentation
- README
- optional demo Azure DevOps work items/PR examples

The demo must include a payment validation function.

Expected conceptual dependency structure:

```text
CheckoutPage
  ↓
Payment API
  ↓
PaymentService
  ↓
PaymentValidator
  ↓
PaymentDB

OrderService
  ↓
PaymentService
```

### ACTION REQUIRED FROM ME
Ask me:
- whether to use the generated demo repository or my real repository
- which programming language I want for the demo
- whether I already have an Azure DevOps project

If I do not have these, create the demo locally so implementation can continue.

---

# 7. PHASE 1 — BACKEND FOUNDATION

Create a FastAPI backend.

Required endpoints:

```text
GET  /health

POST /webhooks/azure-devops

POST /projects

POST /projects/{project_id}/ingest

GET  /projects/{project_id}/graph

POST /analyze/ticket

POST /analyze/pr

GET  /analysis/{analysis_id}

GET  /reports/{analysis_id}
```

Use:
- Pydantic schemas
- centralized configuration
- structured logging
- request IDs
- error handling
- clean service separation

Create tests for all endpoints.

`GET /health` must work without Azure resources.

### ACTION REQUIRED FROM ME
Tell me:
- recommended Python version
- how to create the virtual environment
- how to install dependencies
- how to start FastAPI
- exactly what I should test manually

Do not move to the next phase until this phase is verified.

---

# 8. PHASE 2 — AZURE DEVOPS INTEGRATION

Create a dedicated `azure_devops.py` adapter.

Support:
- repository information
- work items
- pull requests
- changed files
- commits
- project metadata

Conceptual methods:

```python
get_work_item(work_item_id)
get_pull_request(project, repository, pull_request_id)
get_pull_request_changes(...)
get_repository(...)
get_commit(...)
```

Make the adapter mockable.

Use secure, least-privilege authentication appropriate to the current Azure DevOps environment.

Never hard-code credentials.

### ACTION REQUIRED FROM ME
Tell me exactly:
1. Required Azure DevOps permissions.
2. Authentication options.
3. Where I configure the credentials.
4. Which `.env` values are required.
5. How to test connectivity.
6. How to create a demo Azure DevOps project/repository if needed.

---

# 9. PHASE 3 — POWER AUTOMATE

Use Power Automate only as the workflow/glue layer.

Do NOT put dependency analysis, graph traversal or AI reasoning inside Power Automate.

Architecture:

```text
Azure DevOps event
       ↓
Power Automate
       ↓
Call X-Ray API
       ↓
X-Ray analysis
       ↓
Power Automate
       ↓
Teams / Azure DevOps notification
```

Create:

## Flow A — Work Item
```text
Work item created/updated
        ↓
Call X-Ray
        ↓
Analyze ticket
```

## Flow B — Pull Request
```text
PR created/updated
        ↓
Call X-Ray
        ↓
Analyze PR
```

## Flow C — Result
```text
Analysis complete
        ↓
If HIGH risk
        ↓
Teams notification
+
Azure DevOps PR comment where applicable
```

### ACTION REQUIRED FROM ME
Tell me exactly:
- required Power Automate connector(s)
- required connection(s)
- trigger to select
- HTTP URL/body
- authentication method
- how to test
- whether premium licensing is required

If any required feature is premium/paid, tell me BEFORE making it a dependency.

---

# 10. PHASE 4 — REPOSITORY INGESTION

Create an ingestion pipeline that:

1. accesses the repository
2. walks source files
3. ignores generated/binary files
4. ignores `.git`, build output and dependency folders
5. detects source/API/config/schema/docs files
6. extracts metadata
7. identifies services/APIs/database references
8. identifies imports/calls where supported
9. creates graph nodes/edges
10. creates knowledge chunks
11. indexes/persists the results
12. supports incremental updates later

Start with one demo language/framework for reliability.

Do NOT claim universal code understanding.

---

# 11. PHASE 5 — DEPENDENCY GRAPH

Node types:

```text
PROJECT
FRONTEND_COMPONENT
API
SERVICE
CLASS
FUNCTION
DATABASE
TABLE
EXTERNAL_SERVICE
DOCUMENT
```

Relationship types:

```text
CALLS
IMPORTS
USES
READS
WRITES
EXPOSES
DEPENDS_ON
DOCUMENTS
```

Edges should contain evidence where possible:

```json
{
  "from": "OrderService",
  "to": "PaymentService",
  "relationship": "CALLS",
  "source_file": "order/payment_client.py",
  "line_start": 42,
  "confidence": 0.98
}
```

Implement:

```python
find_direct_dependencies(node_id)
find_dependencies(node_id, max_depth=3)
find_dependents(node_id, max_depth=3)
find_path(source, target)
```

Distinguish:
- direct dependency
- indirect dependency
- no dependency detected

### ACTION REQUIRED FROM ME
Tell me:
- how to run the parser
- which files/nodes/edges were detected
- how to inspect the graph
- how to adapt parser rules for my real framework/language

---

# 12. PHASE 6 — READ-ONLY DATABASE CONTEXT

Support an optional read-only database connection.

Use it for metadata:
- tables
- columns
- foreign keys
- views/stored procedures if appropriate

Do NOT copy arbitrary production data into the AI.

Enforce:
- SELECT/metadata only
- no INSERT
- no UPDATE
- no DELETE
- no DDL
- no migrations

Make the database integration optional.

### ACTION REQUIRED FROM ME
Tell me:
- database engine supported first
- exact read-only role/permissions
- how to create the user
- how to configure credentials securely
- required environment variables
- how to verify X-Ray cannot write

Never ask me to paste a password into chat.

---

# 13. PHASE 7 — AZURE AI SEARCH PROJECT KNOWLEDGE INDEX

Use Azure AI Search as the Project Knowledge Index.

Index:
- code chunks
- API documentation
- architecture documentation
- README
- tickets
- PR descriptions
- relevant database metadata
- historical incident summaries where available

Suggested fields:

```text
id
project_id
document_type
path
service
symbol
content
content_vector
commit_id
ticket_id
pr_id
timestamp
```

Use meaningful chunks.

Do NOT index the entire repository as one document.

Implement:

```text
index_project()
index_code_chunk()
index_ticket()
index_pull_request()
search_project_knowledge()
hybrid_search()
```

Use:
- exact/keyword search
- vector search
- hybrid retrieval

This matters because identifiers like `PaymentService`, `POST /payments`, `ADO-1234` and `payment_transactions` need exact matching.

### ACTION REQUIRED FROM ME
Tell me exactly:
1. Azure resources required.
2. Appropriate MVP tier.
3. How to create the search service.
4. How to create the index.
5. Required embedding/model resource.
6. Required environment variables.
7. How to obtain them.
8. How to run indexing.
9. How to verify search.

Use current supported Microsoft documentation/SDKs rather than assuming old APIs.

---

# 14. PHASE 8 — MICROSOFT FOUNDRY

Microsoft Foundry is the AI reasoning layer.

Create an X-Ray analysis agent.

The agent receives:

```text
CHANGE REQUEST
TICKET OR PR DETAILS
CHANGED FILES
DEPENDENCY GRAPH FACTS
RETRIEVED PROJECT KNOWLEDGE
SECURITY FINDINGS
HISTORICAL CONTEXT
TEST RESULTS IF AVAILABLE
```

Responsibilities:
1. understand the change
2. interpret graph facts
3. connect change to project context
4. explain direct vs indirect impact
5. interpret security findings
6. identify missing evidence
7. recommend tests/reviewers
8. return structured output

The agent must never:
- invent dependencies
- invent security vulnerabilities
- claim a test passed when it did not
- claim certainty without evidence
- modify production code
- deploy anything
- access production directly

---

# 15. FOUNDRY OUTPUT SCHEMA

Return structured JSON similar to:

```json
{
  "risk": "HIGH",
  "confidence": 0.91,
  "summary": "The payment validation change directly affects PaymentService and may affect OrderService and Checkout.",
  "affected_nodes": [
    {
      "id": "PaymentService",
      "risk": "RED",
      "impact_type": "DIRECT",
      "reason": "The component is directly modified.",
      "evidence": ["payment_service.py"]
    },
    {
      "id": "OrderService",
      "risk": "YELLOW",
      "impact_type": "INDIRECT",
      "reason": "OrderService depends on PaymentService."
    },
    {
      "id": "UserService",
      "risk": "GREEN",
      "impact_type": "NONE",
      "reason": "No dependency path was detected."
    }
  ],
  "security_findings": [],
  "recommendations": [],
  "missing_information": []
}
```

Validate output with Pydantic.

If parsing fails:
1. safely retry where appropriate
2. log the failure without secrets
3. never convert malformed output into a false "safe" result

---

# 16. FOUNDRY SYSTEM INSTRUCTION

Store the agent prompt in version control.

Base it on:

```text
You are Project X-Ray, a software change-impact analysis agent.

Analyze proposed software changes using:
1. Dependency graph facts.
2. Retrieved project knowledge.
3. Azure DevOps ticket/PR information.
4. Security scan findings.
5. Historical project context.
6. Test results when available.

Rules:
- Never invent a dependency.
- Treat dependency graph facts as authoritative for connectivity.
- Distinguish direct, indirect and unsupported impact.
- Every RED/YELLOW node must have an explanation.
- Use evidence whenever possible.
- Never invent a security finding.
- Do not claim tests passed without evidence.
- If information is missing, explicitly say so.
- Recommend specific tests for risky changes.
- Return structured JSON matching the X-Ray schema.
```

---

# 17. PHASE 9 — TICKET ANALYSIS

Implement:

```text
POST /analyze/ticket
```

Input:

```json
{
  "project_id": "...",
  "work_item_id": 1234
}
```

Flow:

```text
Get ticket
   ↓
Extract requested change
   ↓
Identify likely components
   ↓
Search project knowledge
   ↓
Find graph nodes
   ↓
Calculate dependency blast radius
   ↓
Run relevant security/context checks
   ↓
Call Foundry
   ↓
Calculate final risk
   ↓
Persist analysis
   ↓
Return result
```

If the ticket is vague, say:

> Insufficient information to confidently identify the affected component.

Never hallucinate.

---

# 18. PHASE 10 — PR ANALYSIS

Implement:

```text
POST /analyze/pr
```

Fetch:
- PR title
- description
- author
- source branch
- target branch
- changed files
- commits

Map changed files to graph nodes.

Flow:

```text
PR
 ↓
Changed files
 ↓
Changed graph nodes
 ↓
Dependency traversal
 ↓
Knowledge retrieval
 ↓
Security scan
 ↓
Foundry analysis
 ↓
Risk
```

Example:

```text
RISK: HIGH

Files changed: 2
Services affected: 3
APIs affected: 2
Database objects affected: 1

RED:
PaymentService
Payment API

YELLOW:
OrderService
Checkout

GREEN:
UserService
InventoryService
```

---

# 19. PHASE 11 — BLAST-RADIUS ALGORITHM

Do not rely entirely on AI.

Use deterministic graph traversal.

Example:

```python
changed_nodes = identify_changed_nodes(change)

direct_impact = find_dependents(
    changed_nodes,
    max_depth=1
)

indirect_impact = find_dependents(
    changed_nodes,
    max_depth=3
)
```

Initial classification:

```text
Changed component        → RED
Direct dependency        → RED/YELLOW depending on criticality
Indirect dependency      → YELLOW
No dependency path       → GREEN
Confirmed security issue → increase severity
Critical service         → increase severity
```

Foundry explains/refines the result but cannot invent graph relationships.

---

# 20. PHASE 12 — RISK ENGINE

Create a transparent risk engine.

Starting factors:

```text
Direct dependency impact       25
Indirect dependency impact     15
Critical component             20
Change size/complexity         15
Security severity              25
```

Score:

```text
0–29    GREEN
30–59   YELLOW
60–100  RED
```

These are heuristics, NOT statistically validated probabilities.

Return:

```json
{
  "score": 74,
  "level": "RED",
  "factors": [
    {
      "factor": "Direct payment service modification",
      "points": 25
    }
  ]
}
```

The UI must explain why the score exists.

---

# 21. PHASE 13 — SECURITY ANALYSIS

Use actual security tooling wherever possible.

Do NOT make the LLM the security scanner.

Target:
- SAST/code vulnerabilities
- secret detection
- dependency vulnerabilities
- IaC vulnerabilities where relevant
- selected API authentication/authorization checks

Normalize findings:

```json
{
  "severity": "HIGH",
  "type": "SECRET",
  "file": "...",
  "line": 42,
  "description": "...",
  "source": "security_scanner"
}
```

Foundry interprets findings; it does not invent them.

Never display real secrets.

Do not execute dangerous exploit instructions.

If security tooling is unavailable:

```text
Security status: UNKNOWN
```

Never turn "not checked" into "safe."

---

# 22. PHASE 14 — VISUAL DEPENDENCY GRAPH

Build the main X-Ray dashboard.

Use React Flow or equivalent.

The graph is the hero feature.

Example:

```text
Frontend
   ↓
API Gateway
   ↓
Order Service
   ↓
Payment Service
   ↓
Payment Database
```

Risk colors:

```text
GREEN  = no impact detected with available evidence
YELLOW = possible/indirect risk
RED    = direct/high/confirmed risk
```

Support:
- zoom
- pan
- click node
- inspect node
- inspect relationship
- inspect evidence
- show why node is red/yellow
- filter node types
- show dependency paths

Clicking a red node:

```text
PaymentService

Risk:
HIGH

Impact:
DIRECT

Reason:
This component is directly modified.

Dependents:
OrderService
Checkout API

Evidence:
payment_service.py
PR #932
```

Do not make a flashy 3D graph.

---

# 23. PHASE 15 — DASHBOARD

Dashboard sections:

## Header

```text
PROJECT X-RAY
Change Impact Analysis
```

## Summary

```text
Risk: HIGH
Confidence: 91%

Services affected: 3
APIs affected: 2
Database objects affected: 1
Security findings: 1
```

## Dependency graph
Largest section.

## Risk explanation
Foundry explanation.

## Security
Findings and severity.

## Recommendations
- required tests
- suggested reviewers
- rollout precautions
- manual review areas

## Evidence
- files
- tickets
- PRs
- graph paths
- security findings
- tests

---

# 24. PHASE 16 — PR COMMENT

Post a concise PR comment.

Example:

```text
PROJECT X-RAY — CHANGE IMPACT ANALYSIS

Risk: HIGH
Confidence: 91%

Affected:
RED  PaymentService — directly modified
RED  POST /payments — uses modified validation
YELLOW  OrderService — depends on PaymentService
YELLOW  Checkout — depends on affected API
GREEN  UserService — no dependency path detected

Security:
YELLOW  SQL injection candidate

Recommended:
- Run payment integration tests
- Run checkout integration tests
- Review authorization logic

View full analysis:
<dashboard link>
```

Do not post huge AI responses.

---

# 25. PHASE 17 — MICROSOFT TEAMS

Use Power Automate to notify Teams.

Example:

```text
X-Ray: High-risk change detected

PR #932
Update payment validation

Risk: HIGH

Services affected: 3
APIs affected: 2
Security findings: 1

View X-Ray Analysis
```

### ACTION REQUIRED FROM ME
Tell me:
- which Teams channel to use
- which connection is required
- which Power Automate action to select
- whether licensing is required
- how to test the notification

---

# 26. PHASE 18 — REPORTING

Generate readable Markdown/HTML first.

Sections:

```text
1. Change Summary
2. Risk Score
3. Blast Radius
4. Dependency Paths
5. Security Findings
6. Recommendations
7. Evidence
8. Test Results
9. Confidence
10. Missing Information
```

Example:

```text
PROJECT X-RAY REPORT

Change:
Modify payment validation

Overall Risk:
HIGH

Confidence:
91%

Blast Radius:
3 services
2 APIs
1 database

Direct Impact:
PaymentService
Payment API

Indirect Impact:
OrderService
Checkout

Security:
1 medium finding

Recommended:
Payment integration tests
Checkout integration tests
Authorization review
```

---

# 27. PHASE 19 — OPTIONAL SANDBOX VERIFICATION

Only implement after the MVP works.

Flow:

```text
PR
 ↓
Static X-Ray analysis
 ↓
Risk detected
 ↓
Create isolated sandbox
 ↓
Apply PR
 ↓
Build
 ↓
Run automated tests
 ↓
Run approved security tests
 ↓
Collect results
 ↓
Foundry interprets evidence
 ↓
Update confidence
```

Example:

```text
Prediction:
OrderService → YELLOW

Sandbox:
Checkout integration test FAILED

Updated result:
OrderService → RED

Reason:
Predicted dependency impact was confirmed by test failure.
```

Sandbox must never:
- access production
- use production credentials
- modify production
- deploy automatically
- run arbitrary dangerous code without isolation

If sandbox threatens the hackathon timeline, leave it as a documented stretch feature.

---

# 28. PHASE 20 — ERROR HANDLING

Every external integration must fail safely.

## Azure DevOps unavailable

```text
Azure DevOps integration unavailable.
Analysis cannot retrieve PR context.
```

Do not pretend analysis succeeded.

## Azure AI Search unavailable

```text
Project knowledge retrieval unavailable.
Dependency-only analysis can continue with reduced confidence.
```

## Foundry unavailable

```text
AI reasoning unavailable.
Deterministic dependency analysis is available.
```

## Security scanner unavailable

```text
Security scan unavailable.
Security status: UNKNOWN.
```

Never convert "not checked" into "safe."

---

# 29. CONFIDENCE

Distinguish:

```text
RISK
```

from:

```text
CONFIDENCE
```

Example:

```text
Risk: HIGH
Confidence: 91%
```

High risk + low confidence means:

> potentially dangerous, but insufficient evidence.

Low risk + low confidence means:

> more investigation is needed.

Do not represent confidence as a validated probability unless it actually is.

---

# 30. OBSERVABILITY

Every analysis should have structured logging containing:

```text
analysis_id
project_id
event_type
ticket_id
pr_id
timestamp
duration
graph_nodes_examined
knowledge_chunks_retrieved
security_findings
foundry_status
risk
confidence
```

Never log:
- passwords
- tokens
- API keys
- secrets
- sensitive production data

---

# 31. TESTING STRATEGY

Create tests at every layer.

## Unit tests
- parser
- graph construction
- graph traversal
- risk engine
- schemas
- security finding normalization

## Integration tests
- Azure DevOps adapter with mocks
- Azure AI Search adapter with mocks
- Foundry adapter with mocked responses
- webhook flow
- PR analysis

## Frontend tests
- graph rendering
- risk colors
- node details
- report display
- error states

## End-to-end demo test

```text
Create ticket
 ↓
Trigger X-Ray
 ↓
Find PaymentService
 ↓
Traverse dependencies
 ↓
Retrieve project context
 ↓
Run security analysis
 ↓
Foundry explanation
 ↓
Risk result
 ↓
Graph changes colors
 ↓
Report generated
```

---

# 32. DEMO DATA

Create a repeatable demo dataset.

Ticket:

```text
ADO-1234

Title:
Change payment validation

Description:
Add additional validation to the payment processing flow.
```

Expected conceptual impact:

```text
PaymentService       RED
Payment API          RED
Payment DB           RED/YELLOW
OrderService         YELLOW
Checkout             YELLOW
UserService          GREEN
InventoryService     GREEN
```

The actual final status must come from the implemented risk policy.

---

# 33. README

Create a strong README containing:

1. What Project X-Ray is.
2. Why it exists.
3. Architecture.
4. Tech stack.
5. Local setup.
6. Azure resources.
7. Environment variables.
8. Azure DevOps setup.
9. Power Automate setup.
10. Azure AI Search setup.
11. Microsoft Foundry setup.
12. Security setup.
13. Running locally.
14. Running the demo.
15. API documentation.
16. Troubleshooting.
17. Known limitations.
18. Future roadmap.

Never put secrets in README.

---

# 34. ENVIRONMENT VARIABLES

Create `.env.example`.

Use placeholders such as:

```text
AZURE_DEVOPS_ORG_URL=
AZURE_DEVOPS_PROJECT=
AZURE_DEVOPS_REPOSITORY=

AZURE_SEARCH_ENDPOINT=
AZURE_SEARCH_INDEX_NAME=
AZURE_SEARCH_API_KEY=

FOUNDRY_ENDPOINT=
FOUNDRY_PROJECT_NAME=
FOUNDRY_MODEL_DEPLOYMENT=

DATABASE_HOST=
DATABASE_PORT=
DATABASE_NAME=
DATABASE_USER=
DATABASE_PASSWORD=

TEAMS_FLOW_URL=
```

These are examples only. Verify current Microsoft SDK/API configuration and adjust names as needed.

Never fabricate values.

---

# 35. LOCAL DEVELOPMENT

Create `docker-compose.yml` where useful.

The project should support:

```text
XRAY_MODE=demo
```

Demo mode may use:
- seeded graph
- seeded knowledge
- mocked external events
- mocked Foundry responses

But clearly label mocked/demo analysis.

Never present mocked analysis as live AI analysis.

---

# 36. SECURITY REQUIREMENTS

Project X-Ray itself must be secure.

Implement:
- input validation
- API authentication/authorization where appropriate
- secret management
- HTTPS in deployed environments
- read-only DB access
- least-privilege Azure DevOps access
- webhook validation
- rate limiting where appropriate
- safe errors
- no secrets in logs
- no secrets in vector index
- sandbox isolation

Before indexing code/configuration, detect obvious secrets and exclude/mask them.

---

# 37. LIMITATIONS TO DOCUMENT

Document clearly:

- dependency detection is not perfect
- language/framework support is limited in MVP
- AI output is advisory
- security coverage depends on configured scanners
- GREEN means no impact detected with available evidence, not guaranteed safety
- sandbox verification is optional
- human approval remains required

---

# 38. FINAL HACKATHON DEMO

The final demo should take approximately 2–3 minutes.

1. Show the normal architecture.
2. Create "Change payment validation."
3. Trigger X-Ray.
4. Show the graph changing colors.
5. Click PaymentService and show evidence.
6. Show Foundry explanation.
7. Show security findings.
8. Create/refresh a PR.
9. Show automatic PR analysis.
10. Show Teams notification.
11. Optionally show sandbox verification.

The strongest story is:

```text
BEFORE:
CHANGE → MERGE → PRODUCTION DOWN

X-RAY:
CHANGE → UNDERSTAND → BLAST RADIUS → SECURITY → TEST → SAFE MERGE
```

Core message:

> "Project X-Ray helps developers understand what a change could break before it reaches production."

---

# 39. IMPLEMENTATION ORDER

Build in exactly this order unless a real technical dependency requires a change:

```text
1. Demo application
2. FastAPI backend
3. Azure DevOps adapter
4. Repository ingestion
5. Dependency graph
6. Azure AI Search knowledge layer
7. Microsoft Foundry integration
8. Deterministic blast-radius engine
9. Risk engine
10. Security scanning
11. React dependency graph UI
12. Ticket analysis
13. PR analysis
14. Power Automate
15. Teams notifications
16. PR comments
17. Reports
18. Sandbox verification
19. Deployment
20. Final testing/demo
```

Do not start with:
- sandbox
- Teams
- AI agent

The dependency graph is the foundation.

---

# 40. COPILOT WORKING METHOD

For each phase:

### A. Explain
Briefly explain what is being built and why.

### B. Inspect
Inspect the existing repository before changing files.

Do not overwrite existing work unnecessarily.

### C. Plan
List files to create/modify.

### D. Implement
Make the smallest complete implementation.

### E. Test
Run tests, lint, type checks and build where applicable.

### F. Report
Always output:

```text
IMPLEMENTED
- ...

TESTED
- ...

FILES CHANGED
- ...

ACTION REQUIRED FROM ME
- ...

AZURE/EXTERNAL SETUP REQUIRED
- ...

ENVIRONMENT VARIABLES REQUIRED
- ...

HOW TO VERIFY
- ...

KNOWN ISSUES
- ...
```

### G. Wait
After each major phase, STOP and wait for my confirmation.

Do not proceed automatically.

---

# 41. START NOW

Before writing implementation code:

1. Inspect the current repository.
2. Determine whether a project already exists.
3. List relevant existing files.
4. Identify the current stack.
5. Compare it with the architecture above.
6. Identify reusable components.
7. Do not delete existing work.
8. Propose Phase 0.
9. Tell me exactly what I need to provide/configure.
10. Wait for my confirmation before proceeding.

If the repository is empty, start by creating the project structure and demo application.

The goal is not merely to generate code.

The goal is to build a **working, explainable, secure, demo-ready Project X-Ray system** with Microsoft Foundry at its AI reasoning core.
