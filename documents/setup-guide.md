# Setup Guide

This guide tells you exactly how to set up Project X-Ray and what you need to provide.

## 1. What you need before setup
You need the following on your machine:

- Python 3.11 or later
- Node.js 20 or later
- npm
- Git
- Optional: .NET 9 SDK for the demo application

## 2. Clone the project
From your terminal:

```bash
git clone https://github.com/Harshita26705/Project-XRay.git
cd Project-XRay
```

If you are working from a local folder already, open that folder in VS Code instead.

## 3. Backend setup
Open a terminal and run:

```bash
cd backend
python -m venv .venv
.
```

On Windows PowerShell:

```powershell
cd backend
python -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
pip install -r requirements.txt
```

Then run tests:

```powershell
python -m pytest
```

If you want to run the backend server:

```powershell
python -m uvicorn app.main:app --reload --port 8000
```

## 4. Frontend setup
Open a second terminal:

```bash
cd frontend
npm install
npm run dev
```

The app should be available on:

```text
http://localhost:5173
```

## 5. Optional demo app build
If you want to build or test the demo .NET app, make sure .NET 9 SDK is installed.

Then run:

```powershell
cd demo-app
dotnet build
```

## 6. What the user must provide
The project can run in demo/mock mode without cloud credentials. But if you want real AI or Azure integration, you need to provide values.

### Required for real Foundry or AI reasoning
You may need:

- Microsoft Foundry project endpoint
- model deployment name
- API key or token
- Azure tenant information
- Azure subscription information

### Required for Azure AI Search
You may need:

- Azure AI Search endpoint
- search key
- index name
- embedding configuration

### Required for Azure DevOps integration
You may need:

- Azure DevOps organization URL
- personal access token (PAT)
- project name

### Required for read-only database context
You may need:

- database server name
- database name
- username and password or managed identity setup
- read-only permissions only

### Required for Teams or Power Automate
You may need:

- Teams webhook URL
- flow endpoint or connector details

## 7. Environment variables
Create a local `.env` file from the example file if present.

Common variables may include:

```env
XRAY_AI_BACKEND=mock
AZURE_OPENAI_ENDPOINT=
AZURE_OPENAI_API_KEY=
AZURE_OPENAI_API_VERSION=
AZURE_SEARCH_ENDPOINT=
AZURE_SEARCH_KEY=
AZURE_DEVOPS_ORG_URL=
AZURE_DEVOPS_PAT=
SQL_CONNECTION_STRING=
TEAMS_WEBHOOK_URL=
POWER_AUTOMATE_WEBHOOK_URL=
```

Important:

- do not commit real secrets
- keep `.env` local only
- use `.env.example` as a safe template

## 8. Safe setup rule
Never commit secrets to GitHub.

Use local environment variables and keep `.env` in `.gitignore`.

## 9. What works without credentials
The core graph engine and demo analysis can work without cloud integrations.

This means the project can still be tested locally in mock mode.

## 10. Recommended first run
For a first pass, use the mock AI mode and test the project locally.

Once that is working, add the live Microsoft and Azure settings one by one.
