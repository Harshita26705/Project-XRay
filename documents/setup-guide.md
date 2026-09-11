# Setup Guide

This guide tells you exactly how to set up Project X-Ray and what you need to provide.

## 1. What you need before setup

- .NET 9 SDK
- Node.js 20 or later + npm
- SQL Server LocalDB (ships with Visual Studio, or install "SQL Server Express LocalDB" standalone)
- Git
- The `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

## 2. Clone the project

```bash
git clone https://github.com/Harshita26705/Project-XRay.git
cd Project-XRay
```

## 3. Backend setup

```powershell
cd backend
dotnet ef database update --project XRay.Infrastructure --startup-project XRay.Api
dotnet run --project XRay.Api
```

This creates the `XRay` database on `(localdb)\MSSQLLocalDB`, applies all EF Core Code-First migrations, seeds the reference/lookup tables, and starts the API on `http://localhost:5006` (Swagger UI at `/swagger`).

> If `dotnet ef` says "command not found", the tool's folder (`%USERPROFILE%\.dotnet\tools`) may not be on your `PATH` in that terminal session — add it or open a new terminal after installing the tool.

## 4. Frontend setup

```powershell
cd frontend
npm install
npm run dev
```

The app is available at `http://localhost:5173`.

## 5. Signing in — development bypass vs. real Microsoft Entra ID

**By default, the app runs with a development authentication bypass** on both sides:

- Backend: `appsettings.Development.json` has `"UseDevAuthBypass": true`. Every request is treated as authenticated, as a fixed local user — see `XRay.Api/Auth/DevBypassAuthHandler.cs`. This never activates outside the Development environment.
- Frontend: if no Azure AD App Registration is configured (see below), clicking **Continue with Microsoft** just signs you in locally — see `frontend/src/auth/AuthProvider.tsx`.

This lets you run and demo the whole app immediately, with no Azure tenant required.

### To use real Microsoft Entra ID sign-in

1. In the [Azure portal](https://portal.azure.com), create two **App Registrations** in your tenant: one for the API (expose a scope, e.g. `access_as_user`), one for the frontend SPA (add a platform → Single-page application → redirect URI `http://localhost:5173`), and grant the SPA app permission to call the API's scope.
2. Update `backend/XRay.Api/appsettings.json`:
   ```json
   "AzureAd": {
     "Instance": "https://login.microsoftonline.com/",
     "TenantId": "<your tenant ID>",
     "ClientId": "<the API app registration's client ID>"
   }
   ```
3. Set `"UseDevAuthBypass": false` in `backend/XRay.Api/appsettings.Development.json` (or just don't run in Development).
4. Create `frontend/.env.local`:
   ```env
   VITE_AAD_CLIENT_ID=<the SPA app registration's client ID>
   VITE_AAD_TENANT_ID=<your tenant ID>
   VITE_AAD_API_SCOPE=api://<API app registration client ID>/access_as_user
   ```
5. Restart both the backend and frontend.

## 6. Connecting a project to analyze

The app ingests source code from a **local filesystem path** (it doesn't clone from Azure DevOps yet — see the README's "Known limitations"). On the **Projects** screen, click **Connect Project** and point it at a local repository, for example the bundled sample app:

```text
D:\Hackathon\Project-XRay\demo-app
```

## 7. Optional: real AI explanations (Microsoft Foundry / Azure OpenAI)

Without configuration, the **Expert Explanation** feature falls back to a deterministic, evidence-based template and is labeled "Degraded" — this is intentional honest degradation, not a bug. To enable real calls to an Azure OpenAI / Foundry chat-completions endpoint, add to `backend/XRay.Api/appsettings.json` (or user-secrets/environment variables in production):

```json
"AzureOpenAI": {
  "Endpoint": "https://<your-resource>.openai.azure.com/openai/deployments/<deployment>/chat/completions?api-version=2024-06-01",
  "ApiKey": "<your key>",
  "DeploymentName": "<deployment name>"
}
```

**Never commit real keys.** Use `dotnet user-secrets` locally or App Service/Key Vault configuration in a real deployment.

## 8. Optional: other integrations (Integrations screen)

Azure DevOps and Microsoft Foundry connections get a real HTTP reachability check on "Test Connection" if you provide a base URL via the Integrations screen. Azure AI Search, Microsoft Teams, and Power Automate currently simulate a successful test — wiring these up for real is listed as a follow-up in the README.

## 9. Safe setup rule

Never commit secrets to GitHub. Keep connection strings, API keys, and App Registration secrets in user-secrets, environment variables, or a secret manager (e.g. Azure Key Vault), and keep any local `.env`/`appsettings.Local.json` files out of source control.


## 9. What works without credentials
The core graph engine and demo analysis can work without cloud integrations.

This means the project can still be tested locally in mock mode.

## 10. Recommended first run
For a first pass, use the mock AI mode and test the project locally.

Once that is working, add the live Microsoft and Azure settings one by one.
