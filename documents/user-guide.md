# User Guide

This guide explains how to use Project X-Ray in simple language.

## 1. What the app is for
Project X-Ray helps you understand which parts of a system may be affected by a code change.

It is useful when you want to know:

- how far a change can spread
- which services or modules are at risk
- what parts are connected to a changed area
- whether the issue is likely direct, indirect, or uncertain

## 2. How to start the project
Use these steps:

### Start the backend
```powershell
cd backend
.\.venv\Scripts\Activate.ps1
python -m uvicorn app.main:app --reload --port 8000
```

### Start the frontend
Open another terminal:

```powershell
cd frontend
npm install
npm run dev
```

Then open the local dashboard URL in the browser.

## 3. Main actions in the app
After the app loads, the main user flow is usually:

1. Ingest the project or repo
2. Load the dependency map
3. Select a change or changed file
4. Run the analysis
5. Review the results
6. Open evidence and impacted nodes
7. Read the AI explanation if enabled

## 4. Understanding the colors
Each component is marked as a status:

- GREEN: no clear impact found
- YELLOW: possible or indirect impact
- RED: likely impacted area
- UNKNOWN: unclear or missing evidence

Use these colors as a first indicator, but always read the evidence.

## 5. Reading the graph
The graph is the main visual view.

Nodes represent things like:

- services
- APIs
- controllers
- repositories
- modules
- database objects

Edges show how they connect.

If the changed part is connected to many surrounding nodes, the risk expands outward.

## 6. How to interpret an impact result
A result usually includes:

- changed component
- impacted nodes
- why they are connected
- file and code references
- confidence or risk level
- explanation text

If a node is RED, it is likely in the blast radius.

If a node is YELLOW, it might be affected but is not clearly direct.

If a node is GREEN, it is not currently shown as impacted based on available evidence.

If a node is UNKNOWN, the project lacks enough reliable information.

## 7. Using the evidence
The project should always show evidence behind the decision.

Good evidence includes:

- file name
- class or module name
- dependency edge
- line references
- relationship type

This matters because it helps the developer trust the result rather than just accepting a color.

## 8. Using the AI explanation
If AI is enabled, the app generates a natural-language explanation of the impact.

This explanation should be read as a summary, not as the final decision source.

The real logic still comes from the graph and risk engine.

## 9. Recommended workflow for a developer
Use this workflow:

1. Open the project in the dashboard
2. Ingest repository data
3. Choose a target change or file
4. Run analysis
5. Review impacted components
6. Open the evidence chain
7. Use the summary to decide if the change needs extra review

## 10. What happens in demo mode
In demo mode, the system can run without external cloud services.

This is useful for:

- testing the logic
- validating the graph
- verifying the dashboard
- showing the idea to a team in a hackathon or demo

## 11. Common troubleshooting
### The app does not start
Check:

- Python version
- Node version
- installed dependencies
- whether the backend is running before the frontend

### No results appear
Check:

- the repo was ingested correctly
- the project folder is valid
- the analysis step was actually run

### AI explanation is missing
Check whether the AI backend is configured and whether mock mode is enabled.

### Results look uncertain
That may be expected. If the project is incomplete or the graph lacks data, UNKNOWN is safer than a wrong answer.

## 12. Best practice
Use Project X-Ray as a risk review assistant, not as a replacement for engineering judgment.

It helps you spot change impact faster and explain it better, but people still need to review the result.
