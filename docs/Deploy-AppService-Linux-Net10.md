# Deploy su Azure App Service Linux (.NET 10)

Questa repository include IaC Bicep e workflow GitHub Actions per build + deploy end-to-end della web app `VstsDemoBuilder` su App Service Linux.

## Prerequisiti

- Subscription Azure con permessi per creare risorse.
- Federated credential OIDC configurata per GitHub Actions (service principal/app registration).
- Repository GitHub con branch `feature/net10-linux-appservice-workshop`.

## File coinvolti

- `infra/main.bicep`
- `infra/parameters/workshop.parameters.json`
- `.github/workflows/build-deploy-linux-net10.yml`

## Variabili repository richieste

Impostare in **Settings > Secrets and variables > Actions > Variables**:

- `AZURE_RESOURCE_GROUP`
- `AZURE_LOCATION` (es. `westeurope`)
- `AZURE_WEBAPP_NAME` (globally unique)
- `AZURE_APP_SERVICE_PLAN`
- `AZURE_APP_INSIGHTS_NAME`

## Secrets repository richiesti

OIDC Azure:

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`

Configurazione applicativa (opzionali ma consigliati per parità funzionale):

- `LEGACY_TENANT_ID` (Entra tenant ID – usare `common` per multi-tenant)
- `LEGACY_CLIENT_ID` (Entra app registration client ID)
- `LEGACY_CLIENT_SECRET` (Entra app registration client secret)
- `LEGACY_REDIRECT_URI`
- `LEGACY_APP_SCOPE` (default: `499b84ac-1321-427f-aa17-267ca6975798/.default offline_access`)
- `LEGACY_PASSWORD`
- `LEGACY_GITHUB_CLIENT_ID`
- `LEGACY_GITHUB_CLIENT_SECRET`
- `LEGACY_GITHUB_REDIRECT_URL`
- `LEGACY_GITHUB_SCOPE`
- `LEGACY_PAT_BASE64`
- `LEGACY_EMAIL_FROM`
- `LEGACY_EMAIL_USERNAME`
- `LEGACY_EMAIL_PASSWORD`

## Registrazione app su Microsoft Entra ID

L'autenticazione OAuth verso Azure DevOps utilizza **Microsoft Entra ID** (ex Azure AD). È necessario:

1. Nel portale Azure → **Microsoft Entra ID → App registrations → New registration**.
2. Impostare come **Redirect URI** il valore `https://<webapp-name>.azurewebsites.net/Environment/Create`.
   - Per Azure DevOps Entra OAuth usare account supportati Microsoft Entra (single/multi-tenant); gli account Microsoft personali (MSA) non sono supportati in modo nativo per questa integrazione.
3. In **API permissions → Add a permission → APIs my organization uses**, cercare `Azure DevOps` (AppId `499b84ac-1321-427f-aa17-267ca6975798`) e aggiungere il permesso `user_impersonation`.
4. In **Certificates & secrets**, creare un client secret e annotarlo.
5. Impostare i secrets nel repository GitHub:
   - `LEGACY_TENANT_ID` = Directory (tenant) ID
   - `LEGACY_CLIENT_ID` = Application (client) ID
   - `LEGACY_CLIENT_SECRET` = valore del secret creato
   - `LEGACY_REDIRECT_URI` = `https://<webapp-name>.azurewebsites.net/Environment/Create`

## Esecuzione workflow

1. Push su `feature/net10-linux-appservice-workshop` oppure avvio manuale di `Build and deploy (.NET 10 Linux)`.
2. Job `build`: restore, build e publish della soluzione/progetto web.
3. Job `deploy`: login Azure OIDC, deploy Bicep, deploy package zip, smoke test HTTP.

## Deploy manuale (az cli)

```bash
az group create --name <rg-name> --location <location>
az deployment group create \
  --resource-group <rg-name> \
  --template-file infra/main.bicep \
  --parameters @infra/parameters/workshop.parameters.json \
  --parameters webAppName=<webapp-name> appServicePlanName=<plan-name> applicationInsightsName=<ai-name>
```

Dopo il deploy infrastrutturale, pubblicare la web app con:

```bash
dotnet publish src/VstsDemoBuilder/VstsDemoBuilder.csproj -c Release -o publish
cd publish && zip -r ../webapp.zip .
az webapp deploy --resource-group <rg-name> --name <webapp-name> --src-path ../webapp.zip --type zip
```
