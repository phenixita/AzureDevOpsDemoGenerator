# Local Testing Guide - Azure DevOps Demo Generator

## Prerequisites
- .NET 10 SDK installed
- Visual Studio Code or Visual Studio 2022
- Azure subscription with access to the Entra app registration

## Step 1: Add localhost redirect URI to Entra app

Run this command to add a localhost redirect URI:

```powershell
az ad app update --id b6a1499b-7e6d-49fa-a8b9-8316c2a455de `
  --web-redirect-uris `
    "https://adodemowk260228165811.azurewebsites.net/Environment/Create" `
    "https://localhost:5001/Environment/Create"
```

Or manually in Azure Portal:
1. Go to **Azure Portal → Microsoft Entra ID → App registrations**
2. Find **ADO Demo Generator Workshop** (App ID: `b6a1499b-7e6d-49fa-a8b9-8316c2a455de`)
3. Click **Authentication**
4. Under **Web → Redirect URIs**, click **Add URI**
5. Add: `https://localhost:5001/Environment/Create`
6. Click **Save**

## Step 2: Configure local secrets

**Option A: Using appsettings.Development.json** (recommended, simplest approach)

Create `src/VstsDemoBuilder/appsettings.Development.json`:

```json
{
  "LegacyAppSettings": {
    "TenantId": "xxxxxxxxxxxxxxxxexxxxxxxxxx",
    "ClientId": "xxxxxxxxxxxxxxxxxxxxxxx",
    "ClientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "RedirectUri": "https://localhost:5001/Environment/Create",
    "appScope": "499b84ac-1321-427f-aa17-267ca6975798/.default offline_access"
  }
}
```

**Note:** This file is in `.gitignore` and won't be committed to the repository.
 

## Step 3: Run the application

```powershell
cd src\VstsDemoBuilder
dotnet run
```

The app will start on `https://localhost:5001` (or the port shown in console output).

## Step 4: Test the auth flow

1. Open browser to `https://localhost:5001/Account/Verify`
2. Click **Sign In** button
3. You should be redirected to Microsoft Entra login
4. Sign in with a **Microsoft Entra work or school account** (not personal MSA)
5. After consent, you should land on `https://localhost:5001/Environment/CreateProject`

## Troubleshooting

### "Your sign-in session expired"
- You landed on `/Account/Verify` before clicking Sign In. This is expected—just click Sign In.

### "Authentication code was not returned"
- Check that the redirect URI in Entra matches exactly: `https://localhost:5001/Environment/Create`
- Verify port number matches your running app

### "Azure DevOps requires a Microsoft Entra work or school account"
- You tried signing in with a personal Microsoft account (MSA)
- Use a work/school account from your Azure AD tenant instead

### "Authentication is not configured correctly"
- Check that all secrets are set (TenantId, ClientId, ClientSecret, RedirectUri)
- Run: `dotnet user-secrets list` to verify (if using user-secrets)

### Check logs
Look at console output for detailed error messages:
- `OAuth callback error:` - Entra returned an error during authorize
- `OAuth token exchange failed:` - Token endpoint returned an error
- `OAuth configuration missing` - Local config is incomplete

## Verify configuration

```powershell
# Check Entra app redirect URIs
az ad app show --id b6a1499b-7e6d-49fa-a8b9-8316c2a455de --query "web.redirectUris"

# List user secrets (if using user-secrets)
cd src\VstsDemoBuilder
dotnet user-secrets list
```

## Clean up (optional)

After testing, if you want to remove localhost redirect URI:

```powershell
az ad app update --id b6a1499b-7e6d-49fa-a8b9-8316c2a455de `
  --web-redirect-uris `
    "https://adodemowk260228165811.azurewebsites.net/Environment/Create"
```
