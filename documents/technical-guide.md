# Technical Guide

This is the technical explanation of Project X-Ray in simple language.

## 1. Goal of the project
Project X-Ray is a change-impact analysis tool.

When a developer makes a change, the project tries to answer:

- Which parts of the system may be affected?
- Which dependencies are connected to the changed component?
- Which parts are high risk?
- How do we explain the impact clearly?

This is useful for code changes, pull requests, service updates, and risk review.

## 2. Main idea: graph first, AI second
The most important design choice is this:

The dependency graph is the source of truth.

The graph tells us which files, classes, services, APIs, databases, and modules are connected.

The AI layer does not decide the graph. It only explains the result in natural language.

This design is important because it keeps the system more reliable and easier to trust.

## 3. High-level architecture
Project X-Ray is built with a layered design.

### Frontend
The frontend is a React + TypeScript app.

It shows:

- the dependency graph
- the changed nodes
- risk status colors
- component details
- evidence and impact explanations

It allows the user to ingest the project and run the analysis from the browser.

### Backend
The backend is built with Python and FastAPI.

It handles:

- project ingestion
- parsing source files
- graph creation
- change detection
- risk classification
- security checks
- AI explanation generation
- report generation

### Graph engine
The graph engine creates a network of connected components.

Example:

- Frontend page calls an API
- API belongs to a controller or service
- Service calls database layer
- database stores data

These connections are used to determine blast radius.

### Security layer
The security layer checks for obvious issues such as secrets, vulnerable dependencies, and risky patterns.

This is added evidence. It does not decide the risk on its own.

### AI adapter
The AI layer is isolated behind an adapter.

That means the project can switch between:

- a mock backend for local demo work
- Microsoft Foundry
- an OpenAI-compatible endpoint

The AI sits on top of the graph and evidence instead of replacing the graph.

## 4. How the tool works
The project follows a simple flow.

### Step 1: ingest the project
The app scans the repository files and extracts useful information.

### Step 2: parse source files
It reads code and extracts relationships such as:

- imports
- service calls
- API routes
- class dependencies
- database references

### Step 3: build the dependency graph
The extracted data is turned into a connected graph of nodes and edges.

### Step 4: detect change impact
The user provides a changed file or change description, and the system finds what is connected to it.

### Step 5: classify impact
Each affected component is assigned a status:

- RED: direct or high-impact area
- YELLOW: mild or medium risk
- GREEN: likely not impacted
- UNKNOWN: not enough evidence

### Step 6: add evidence
Each result is supported by evidence such as a file path, line reference, edge reference, and confidence level.

### Step 7: explain the result
The AI layer reads the graph evidence and produces simple human-readable explanations.

## 5. Why the graph is important
Without the graph, the app would only know that a file changed.

With the graph, the app can answer:

- what else depends on this file?
- what else is connected to this login flow?
- which service is likely reached from the changed UI?

That is what makes the project valuable.

## 6. Status logic
The engine does not simply say "changed file = red".

It checks distance, confidence, path, and known behavior.

Some examples:

- a directly changed component is often RED
- a nearby connected component may be YELLOW
- a fully unreachable component can be GREEN only when proven
- uncertain results stay UNKNOWN

This makes the results more honest and safer than a simple AI guess.

## 7. Security and honest results
This project tries to avoid false certainty.

If the project information is incomplete, or a scanner is unavailable, the result should degrade to UNKNOWN instead of pretending everything is safe.

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
