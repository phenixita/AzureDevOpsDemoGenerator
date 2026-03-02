# Piano: CLI per Azure DevOps Demo Generator

## Contesto

Il website attuale è un'app ASP.NET Core MVC che gestisce autenticazione OAuth, selezione template e provisioning di progetti demo in Azure DevOps. L'obiettivo è creare una CLI essenziale che rimpiazzi completamente il sito, riutilizzando la libreria `VstsRestAPI` esistente e adattando la logica di orchestrazione di `ProjectService`.

## Architettura

- **Nuovo progetto**: `src/AzdoGenCli/` (console app net10.0) aggiunto alla solution
- **Riuso diretto**: `VstsRestAPI` (tutte le classi REST API per Azure DevOps) referenziato come project reference
- **Copy-adapt**: `ProjectService.CreateProjectEnvironment()` copiato e adattato come `CliProjectService` (rimozione dipendenze web: StatusMessages, AppPath, Session, SelectListItem)
- **Nessun framework CLI esterno**: args manuali + prompt interattivi, zero dipendenze extra
- **Auth**: Browser-based OAuth (default) + PAT come fallback con `--pat`

## Auth: Browser-Based OAuth Flow

La CLI usa lo stesso flusso OAuth 2.0 Authorization Code dell'app web, adattato per la CLI con il pattern **localhost redirect**:

1. La CLI avvia un `HttpListener` su `http://localhost:{porta}/callback` (porta effimera libera)
2. Apre il browser dell'utente verso `https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize` con `redirect_uri=http://localhost:{porta}/callback`
3. L'utente fa login e acconsente nel browser
4. Microsoft reindirizza a localhost con il `code`
5. La CLI cattura il code, scambia per access_token + refresh_token chiamando il token endpoint
6. Mostra nel browser una pagina HTML di conferma ("Puoi chiudere questa finestra")
7. Ferma l'HttpListener e procede con il token ottenuto

**Dettagli implementativi:**
- Riuso della logica di `AccountService.cs` (GenerateRequestPostData, GetAccessToken) per lo scambio token
- Riuso di `AccessDetails.cs` come model di risposta
- Config OAuth (ClientId, ClientSecret, TenantId, appScope) letta da `appsettings.json` (stessa sezione `LegacyAppSettings`)
- Fallback: `--pat <token>` oppure env var `AZURE_DEVOPS_PAT` per CI/scripting, salta OAuth
- Dopo l'auth, chiama `GetProfile()` e `GetAccounts()` per listare le org disponibili (stessa logica di `AccountService`)

**Nuovo file:** `Auth/BrowserAuthenticator.cs`

## Struttura file

```
src/AzdoGenCli/
  AzdoGenCli.csproj
  Program.cs                         # Entry point: config, auth, input, run
  CliArgs.cs                         # Parsing --pat, --org, --project, --template
  InputCollector.cs                  # Prompt interattivi per valori mancanti
  TemplateDiscovery.cs               # Lista template da TemplateSetting.json
  Auth/
    BrowserAuthenticator.cs          # OAuth flow: listener + browser + token exchange
  Infrastructure/
    LegacyConfigBootstrapper.cs      # Copia da VstsDemoBuilder (bridge config)
  Services/
    CliInput.cs                      # Model minimale (rimpiazza Project.cs)
    CliProjectService.cs             # Orchestrazione 30 step con output console
  PreSetting/
    CreateQueryFolder.json           # Copia da VstsDemoBuilder/PreSetting/
```

## Fasi di implementazione

### Fase 1: Scaffold progetto
- Creare `AzdoGenCli.csproj` con ref a `VstsRestAPI`, stessi NuGet (Newtonsoft.Json, MS.VS.Services.Client, log4net, System.Configuration.ConfigurationManager)
- Aggiungere alla solution
- Copiare `LegacyConfigBootstrapper.cs` con namespace nuovo
- Creare `appsettings.json` con la sezione `LegacyAppSettings` dal web app (include ClientId, ClientSecret, TenantId, appScope)
- `Program.cs` minimale che carica config e chiama `LegacyConfigBootstrapper.Apply()`

### Fase 2: Auth — Browser OAuth + PAT fallback
- `Auth/BrowserAuthenticator.cs`:
  - `AuthenticateAsync()`: avvia HttpListener, apre browser con `Process.Start(url)`, attende callback, scambia code per token
  - Riusa la logica di `AccountService.GenerateRequestPostData()` e `AccountService.GetAccessToken()` per lo scambio token
  - Timeout di 2 minuti: se l'utente non completa, errore
  - Ritorna `AccessDetails` (access_token, refresh_token)
- `CliArgs.cs`: parsing args (`--pat`, `--org`, `--project`, `--template`, `--templates-path`, `--github-token`)
- Logica in `Program.cs`: se `--pat` fornito → usa PAT direttamente; altrimenti → `BrowserAuthenticator.AuthenticateAsync()`
- Dopo auth OAuth: chiama profile/accounts API per listare org (come fa l'app web in `EnvironmentController`)

### Fase 3: Input e selezione template
- `InputCollector.cs`: se org non specificata e auth OAuth → mostra lista org dall'API; altrimenti prompt
- `TemplateDiscovery.cs`: legge `TemplateSetting.json`, mostra lista numerata, l'utente sceglie

### Fase 4: Orchestrazione core (CliProjectService)
- Copiare `ProjectService.CreateProjectEnvironment()` (~1800 righe) in `CliProjectService.cs`
- Sostituzioni meccaniche:
  - `AddMessage(id, msg)` → `Console.WriteLine($"[{step}] {msg}")`
  - `AppPath.MapPath("~/Templates")` → `input.TemplatesRoot`
  - `model.ReadJsonFile(path)` → `File.ReadAllText(path)`
  - `StatusMessages[id] = "100"` → `return`
  - Rimozione blocco `IssueWI.CreateReportWI` (telemetria)
- `CliInput.cs`: POCO minimale con ProjectName, AccountName, AccessToken, SelectedTemplate, TemplatesRoot, GitHubToken

### Fase 5: Entry point e gestione errori
- `Program.cs` completo: config → auth → input → conferma ("Proceed? [y/N]") → run → stampa URL progetto
- Exit codes: 0=ok, 1=errore provisioning, 2=args invalidi, 3=auth fallita
- Gestione Ctrl+C

### Fase 6: Template e file di supporto
- `PreSetting/CreateQueryFolder.json` copiato nel progetto CLI
- Templates path: env var `AZDO_TEMPLATES_PATH` → fallback a `../VstsDemoBuilder/Templates` (per dev in-repo)

### Fase 7: Polish
- Flag `--dry-run` (mostra cosa farebbe senza chiamare API)
- Flag `--verbose` (dettagli risposte API)
- Validazione input e re-prompt su errori

## File critici da modificare/leggere

| File | Azione |
|------|--------|
| `src/VstsDemoBuilder/Services/ProjectService.cs` | Copiare e adattare orchestrazione |
| `src/VstsDemoBuilder/Services/AccountService.cs` | Riusare logica OAuth (token exchange, profile, accounts) |
| `src/VstsDemoBuilder/Models/AccessDetails.cs` | Riusare model token response |
| `src/VstsDemoBuilder/Controllers/AccountController.cs` | Riferimento per URL authorize |
| `src/VstsDemoBuilder/Controllers/EnvironmentController.cs` | Riferimento per callback flow |
| `src/VstsDemoBuilder/Infrastructure/LegacyConfigBootstrapper.cs` | Copiare (nuovo namespace) |
| `src/VstsDemoBuilder/appsettings.json` | Copiare sezione LegacyAppSettings (OAuth config inclusa) |
| `src/VstsDemoBuilder/Models/Project.cs` | Riferimento per creare CliInput.cs |
| `src/VstsRestAPI/VstsRestAPI.csproj` | Project reference |
| `src/VSTSDemoGeneratorV2.sln` | Aggiungere nuovo progetto |

## Verifica

1. `dotnet build src/AzdoGenCli` compila senza errori
2. `dotnet run --project src/AzdoGenCli -- --help` mostra usage
3. `dotnet run --project src/AzdoGenCli` → apre il browser, utente fa login, CLI riceve token e mostra org + lista template
4. `dotnet run --project src/AzdoGenCli -- --pat XXX --org myorg` → salta OAuth, usa PAT direttamente
5. Test end-to-end con template SmartHotel360 su un org Azure DevOps di test
6. Verifica che il progetto creato abbia tutti gli artefatti (work items, repos, pipelines, board config)
