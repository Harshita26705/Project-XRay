# Project X-Ray Documents

This folder contains the project documentation in simple language.

## What this project does
Project X-Ray helps a developer understand the impact of a code change before that change is merged or released.

It reads the project structure, builds a dependency map, checks what components are connected, and marks the likely affected parts as:

- CRITICAL = strong evidence of direct or high-impact change
- RISKY = potential downstream/transitive impact
- SAFE = no impact detected with the available evidence (not the same as "secure")
- UNKNOWN = not enough evidence for a trustworthy conclusion

The system is designed to be explainable. It does not just say "this is risky". It shows what parts are connected and why.

## Document map

- [technical-guide.md](technical-guide.md) — explains the technical implementation (ASP.NET Core + EF Core Code-First + React)
- [setup-guide.md](setup-guide.md) — explains what to install and how to configure auth/AI/integrations
- [user-guide.md](user-guide.md) — explains how to use the project step by step

## Important project idea
This project uses a graph-first design.

That means the app first builds a connected map of the project (in SQL Server, via EF Core Code-First), then uses that map to evaluate blast radius deterministically. The AI layer is used only to explain the result — it is fed nothing but facts the deterministic engine already produced, and it can never invent a node, edge, risk state, or finding.

## Quick summary
The system combines:

- a React + TypeScript frontend (20 screens, matching the Figma design in `Figma/prompt.md`)
- an ASP.NET Core (.NET 9) backend with EF Core Code-First against SQL Server
- a dependency graph built by real parsers (Roslyn for C#, regex/heuristic for TypeScript and SQL)
- a deterministic blast-radius + classifier engine (the R1–R12 rules)
- regex-based security scanning (secrets, SQL injection, weak crypto, insecure deserialization)
- an AI explanation layer that calls Microsoft Foundry/Azure OpenAI when configured, and otherwise degrades honestly to a deterministic template
- Microsoft Entra ID (Azure AD) authentication, with a local development bypass so you can run everything without an Azure tenant

## Required input from the user

To run the app at all: nothing beyond the .NET 9 SDK, Node 20+, and SQL Server LocalDB — see [setup-guide.md](setup-guide.md).

To use real cloud integrations, you may additionally provide:

- a Microsoft Entra ID App Registration (Tenant ID, Client IDs) for real sign-in
- a Microsoft Foundry / Azure OpenAI endpoint + API key for real AI explanations
- Azure DevOps organization details for a real "Test Connection" check
- Azure AI Search, Microsoft Teams, and Power Automate connection details (currently these three are simulated in "Test Connection" — real wiring is a documented follow-up)

If those values are not provided, the project still runs completely, with honest fallback/degraded states shown in the UI rather than fake success.

