# Deploy su Azure App Service Linux (.NET 10)

Questa repository include IaC Bicep e workflow GitHub Actions per build + deploy end-to-end della web app `VstsDemoBuilder` su App Service Linux.

## Prerequisiti

- Subscription Azure con permessi per creare risorse.
- Federated credential OIDC configurata per GitHub Actions (service principal/app registration).
- Repository GitHub con branch `feature/net10-linux-appservice-workshop`.

## File coinvolti

- `infra/main.bicep`
- `infra/parameters/production.parameters.json`
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

### Registrazione app via CLI (az cli)

In alternativa al portale, è possibile scriptare la creazione dell'app registration OAuth:

```bash
# 1. Creare l'app registration OAuth per Azure DevOps
WEBAPP_NAME="azdodemogen"
az ad app create \
  --display-name "AzDo Demo Generator - OAuth" \
  --sign-in-audience "AzureADMultipleOrgs" \
  --web-redirect-uris "https://${WEBAPP_NAME}.azurewebsites.net/Environment/Create" \
  --query "{appId:appId, objectId:id}" -o json

# 2. Aggiungere il permesso Azure DevOps user_impersonation
APP_CLIENT_ID="<appId from step 1>"
AZDO_PERM_ID=$(az ad sp show --id "499b84ac-1321-427f-aa17-267ca6975798" \
  --query "oauth2PermissionScopes[?value=='user_impersonation'].id" -o tsv)
az ad app permission add --id "$APP_CLIENT_ID" \
  --api "499b84ac-1321-427f-aa17-267ca6975798" \
  --api-permissions "${AZDO_PERM_ID}=Scope"

# 3. Creare un client secret
az ad app credential reset --id "$APP_CLIENT_ID" \
  --display-name "gh-actions-deploy" --years 1
```

## Setup OIDC Service Principal per GitHub Actions

Per il deploy OIDC da GitHub Actions, creare un service principal separato con federated credential:

```bash
# 1. Creare app registration per OIDC
az ad app create --display-name "AzDo-DemoGen-GitHub-OIDC" --query "{appId:appId, objectId:id}" -o json
OIDC_APP_ID="<appId>"
OIDC_OBJECT_ID="<objectId>"

# 2. Creare service principal
az ad sp create --id "$OIDC_APP_ID"

# 3. Assegnare ruolo Contributor sulla subscription
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
az role assignment create --assignee "$OIDC_APP_ID" --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION_ID"

# 4. Creare federated credential per GitHub Actions environment 'production'
cat > fedcred.json <<EOF
{
  "name": "github-actions-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>/<repo>:environment:production",
  "audiences": ["api://AzureADTokenExchange"],
  "description": "GitHub Actions OIDC - production environment"
}
EOF
az ad app federated-credential create --id "$OIDC_OBJECT_ID" --parameters fedcred.json
rm fedcred.json
```

## Configurazione variabili e secrets GitHub via CLI

```bash
REPO="<owner>/<repo>"

# Variables
gh variable set AZURE_RESOURCE_GROUP     --body "rg-azdodemogen-prod"   --repo $REPO
gh variable set AZURE_LOCATION           --body "westeurope"            --repo $REPO
gh variable set AZURE_WEBAPP_NAME        --body "azdodemogen"           --repo $REPO
gh variable set AZURE_APP_SERVICE_PLAN   --body "asp-azdodemogen-prod"  --repo $REPO
gh variable set AZURE_APP_INSIGHTS_NAME  --body "ai-azdodemogen-prod"   --repo $REPO

# OIDC Secrets
gh secret set AZURE_CLIENT_ID       --body "<OIDC app client ID>"   --repo $REPO
gh secret set AZURE_TENANT_ID       --body "<tenant ID>"            --repo $REPO
gh secret set AZURE_SUBSCRIPTION_ID --body "<subscription ID>"      --repo $REPO

# App OAuth Secrets
gh secret set LEGACY_CLIENT_ID      --body "<OAuth app client ID>"  --repo $REPO
gh secret set LEGACY_CLIENT_SECRET  --body "<OAuth client secret>"  --repo $REPO
gh secret set LEGACY_TENANT_ID      --body "<tenant ID>"            --repo $REPO
gh secret set LEGACY_REDIRECT_URI   --body "https://azdodemogen.azurewebsites.net/Environment/Create" --repo $REPO
gh secret set LEGACY_APP_SCOPE      --body "499b84ac-1321-427f-aa17-267ca6975798/.default offline_access" --repo $REPO

# Creare il GitHub environment 'production'
echo '{}' | gh api repos/$REPO/environments/production --method PUT --input -
```

## Esecuzione workflow

1. Push su `feature/net10-linux-appservice-workshop` oppure avvio manuale di `Build and deploy (.NET 10 Linux)`.
2. Job `build`: restore, build e publish della soluzione/progetto web.
3. Job `deploy`: login Azure OIDC, deploy Bicep, deploy package zip, smoke test HTTP.

## Deploy manuale (az cli)

```bash
# Naming convention consigliata:
#   Resource group: rg-azdodemogen-prod
#   App Service Plan: asp-azdodemogen-prod
#   Web App: azdodemogen
#   Application Insights: ai-azdodemogen-prod

az group create --name rg-azdodemogen-prod --location westeurope
az deployment group create \
  --resource-group rg-azdodemogen-prod \
  --template-file infra/main.bicep \
  --parameters @infra/parameters/production.parameters.json
```

Dopo il deploy infrastrutturale, pubblicare la web app con:

```bash
dotnet publish src/VstsDemoBuilder/VstsDemoBuilder.csproj -c Release -o publish
cd publish && zip -r ../webapp.zip .
az webapp deploy --resource-group rg-azdodemogen-prod --name azdodemogen --src-path ../webapp.zip --type zip
```
