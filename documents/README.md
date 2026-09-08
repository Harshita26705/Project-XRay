# Project X-Ray Documents

This folder contains the project documentation in simple language.

## What this project does
Project X-Ray helps a developer understand the impact of a code change before that change is merged or released.

It reads the project structure, builds a dependency map, checks what components are connected, and marks the likely affected parts as:

- GREEN = no clear impact found
- YELLOW = some risk or uncertain impact
- RED = likely impacted
- UNKNOWN = not enough evidence

The system is designed to be explainable. It does not just say "this is risky". It shows what parts are connected and why.

## Document map

- [technical-guide.md](technical-guide.md) — explains the technical implementation in simple words
- [setup-guide.md](setup-guide.md) — explains what the user must install and what values they need to provide
- [user-guide.md](user-guide.md) — explains how to use the project step by step

## Important project idea
This project uses a graph-first design.

That means the app first builds a connected map of the project, then uses that map to evaluate blast radius. The AI layer is used mainly to explain the result, not to decide the actual impact.

## Quick summary
The system combines:

- a frontend dashboard
- a Python backend
- a dependency graph
- security checks
- optional Microsoft Foundry / Azure AI integration
- optional Azure DevOps, Teams, and AI Search connections

The project can run in a demo/mock mode without live Azure services, but real integration requires credentials and configuration.

## Required input from the user
To use live AI and cloud integrations, the user may need to provide values such as:

- Microsoft Foundry project endpoint or model details
- Azure AI Search endpoint and key
- Azure DevOps organization URL and PAT
- SQL read-only connection details, if used
- Teams webhook or Power Automate endpoint, if used

If those values are not provided, the project can still run in a safe mock/demo mode.
