# Azure DevOps Demo Generator - AI Agent Instructions

## Project Overview
ASP.NET Core MVC web application (.NET 10.0) that generates pre-populated Azure DevOps projects from JSON-based templates. Two-tier architecture: **VstsDemoBuilder** (web frontend) and **VstsRestAPI** (Azure DevOps API wrapper library).

Hosted on Kestrel with modern .NET configuration patterns. Solution: `VSTSDemoGeneratorV2.Net8.sln`

## Architecture Patterns

### Service Layer
All business logic lives in service classes implementing interfaces:
- `ProjectService` (IProjectService) - orchestrates project generation
- `TemplateService` (ITemplateService) - manages template loading/validation  
- `AccountService` (IAccountService) - handles authentication
- `ExtractorService` (IExtractorService) - extracts templates from existing projects

Example: [Services/ProjectService.cs](src/VstsDemoBuilder.Net8/Services/ProjectService.cs)

### Azure DevOps API Wrapper Pattern
All API interactions inherit from `ApiServiceBase` which provides:
- HttpClient configuration with OAuth Bearer token
- Base configuration (account, project, PAT) via `IConfiguration`
- Consistent error handling via `LastFailureMessage` property

Example classes: [VstsRestAPI.Net8/WorkItemAndTracking/WorkItem.cs](src/VstsRestAPI.Net8/WorkItemAndTracking/WorkItem.cs), [VstsRestAPI.Net8/Git/Repository.cs](src/VstsRestAPI.Net8/Git/Repository.cs)

```csharp
public class MyAzureDevOpsService : ApiServiceBase
{
    public MyAzureDevOpsService(IConfiguration configuration) : base(configuration) {}
    
    protected HttpClient GetHttpClient() // Inherited, pre-configured
}
```

### Configuration Object
`Configuration` class (VstsRestAPI.Net8/Configuration.cs) carries context between API calls:
- PersonalAccessToken, AccountName, Project, ProjectId
- API version strings, URI parameters
- Pass through constructor for all ApiServiceBase-derived classes

### Progress Tracking for Long Operations
`ProjectService.StatusMessages` static dictionary tracks async project generation:
- Key format: `"{userId}_{timestamp}"` or `"{userId}_{timestamp}_Errors"`
- Polled by client via `EnvironmentController.GetCurrentProgress(string id)`
- Thread-safe via `objLock` object

## Architecture Implementation

### Configuration Pattern
Uses appsettings.json for all configuration. Static `AppSettings` helper (VstsDemoBuilder.Net8/Infrastructure/AppSettings.cs) provides convenient access:
- Initialize in Program.cs: `AppSettings.Initialize(builder.Configuration);`
- Access anywhere: `AppSettings.Get("WorkItemsVersion")`
- All Azure DevOps API versions stored in appsettings.json root

### Controller Patterns
View controllers use `LegacyController` base class for session management:
```csharp
public class EnvironmentController : LegacyController // Inherits from Controller
{
    protected LegacySession Session // Property wrapper around HttpContext.Session
    protected LegacyServerUtility Server // Utilities for encoding/decoding
}
```
API-only controllers inherit directly from `ControllerBase` (e.g., Apis/ProjectController.cs).

**LegacySession** wraps ISession with string indexer syntax: `Session["key"] = "value"`

### Minimal Hosting Model (Program.cs)
Uses top-level statements with WebApplicationBuilder:
- Services registered as Scoped: `builder.Services.AddScoped<IProjectService, ProjectService>()`
- Static file middleware configured for Content/Scripts/Images/Templates directories
- log4net configured from log4net.config file in content root
- Session, Swagger, and ApplicationInsights configured in builder

## Template System

### Template Structure  
Templates live in [src/VstsDemoBuilder.Net8/Templates/{TemplateName}/](src/VstsDemoBuilder.Net8/Templates/)

**ProjectTemplate.json** (entry point) references other JSON files:
```json
{
  "Description": "Template description",
  "Teams": "Teams.json",
  "BoardColumns": "BoardColumns.json",
  "BugfromTemplate": "Bug.json",
  "TemplateVersion": "2.0"
}
```

Each referenced file contains serialized Azure DevOps API payloads for that artifact type.

### Adding New Templates
1. Create folder in Templates/ matching the ShortName
2. Add ProjectTemplate.json + referenced artifact JSON files
3. Update [Templates/TemplateSetting.json](src/VstsDemoBuilder.Net8/Templates/TemplateSetting.json) with metadata
4. Add icon to Templates/TemplateImages/

See [docs/Using-The-Template-Extractor.md](docs/Using-The-Template-Extractor.md) for extracting from existing projects.

## Local Development Workflow

### OAuth Application Registration

**Register your application** using Azure Entra ID (formerly Azure AD):

1. Navigate to [Azure Portal](https://portal.azure.com) → **Azure Entra ID** → **App registrations** → **New registration**
2. Set **Name** (e.g., "Azure DevOps Demo Generator - Local Dev")
3. Set **Supported account types**: "Accounts in any organizational directory (Any Azure AD directory - Multitenant)"
4. Add **Redirect URI**: 
   - Platform: **Web**
   - URI: `https://localhost:5001/Environment/Create` (adjust port if needed)
5. Click **Register**

**After Registration**:
- Note the **Application (client) ID** from the Overview page
- Go to **Certificates & secrets** → **New client secret** → Note the **Value** (this is your ClientSecret)
- Go to **API permissions** → **Add a permission** → **Azure DevOps** → **Delegated permissions**

**Required Azure DevOps Delegated Permissions**:
- `user_impersonation` - Access Azure DevOps on behalf of user (covers all required scopes)

Or configure granular scopes by adding **Expose an API** custom scopes:
- `vso.agentpools` - Agent Pools (read)
- `vso.build_execute` - Build (read and execute)
- `vso.code_write` - Code (read and write)
- `vso.connected_server` - Connected server
- `vso.dashboards_manage` - Dashboards (manage)
- `vso.extension_manage` - Extensions (read and manage)
- `vso.identity` - Identity (read)
- `vso.project_manage` - Project and team (read, write, and manage)
- `vso.release_manage` - Release (read, write, execute, and manage)
- `vso.serviceendpoint_manage` - Service Endpoints (read, query, and manage)
- `vso.test_write` - Test management (read and write)
- `vso.wiki_write` - Wiki (read and write)
- `vso.work_write` - Work items (read and write)
- `vso.workitemsearch` - Work Items (read)

**OAuth Flow**: Application redirects to `https://login.microsoftonline.com/common/oauth2/v2.0/authorize` (or use classic `https://app.vssps.visualstudio.com/oauth2/authorize`) → User authenticates via Microsoft → Azure DevOps redirects to your callback URL with authorization code → Application exchanges code for access token

### Development Setup

**Pre-requisites**: Visual Studio 2022+ or .NET SDK 10.0+

**Setup Steps**:
1. Register OAuth app in Azure Entra ID (see OAuth Application Registration section above) with redirect URI: `https://localhost:5001/Environment/Create`
2. After registration, note the **Application (client) ID** and **Client Secret** from Azure Portal

**Configuration**: Edit [appsettings.Development.json](src/VstsDemoBuilder.Net8/appsettings.Development.json) (gitignored):
```json
{
  "RedirectUri": "https://localhost:5001/Environment/Create",
  "ClientId": "your-application-client-id-from-azure-portal",
  "ClientSecret": "your-client-secret-value-from-azure-portal",
  "appScope": "user_impersonation"
}
```

**Note**: The `appScope` should match the scopes configured in Azure Entra ID. For simplicity, you can use `user_impersonation` which grants all necessary permissions.

**Run Options**:
- **Visual Studio**: Open [VSTSDemoGeneratorV2.Net8.sln](src/VSTSDemoGeneratorV2.Net8.sln), press F5
- **Command Line**: `cd src/VstsDemoBuilder.Net8 && dotnet run`
- **Development URL**: https://localhost:5001 (Kestrel automatically uses this)

**Development Features**:
- Swagger UI available at `/swagger` when `ASPNETCORE_ENVIRONMENT=Development`
- Hot reload enabled for code changes
- No IIS or hosts file modification required

### Common OAuth Issues & Troubleshooting

**"Callback URL mismatch" error**:
- Ensure `RedirectUri` in appsettings.Development.json exactly matches the Authorization callback URL registered in Azure Entra ID
- Include protocol (https://), localhost, port, and path (`/Environment/Create`)
- Example: `https://localhost:5001/Environment/Create`

**"Invalid client" error**:
- Verify `ClientId` matches the Application (client) ID from Azure Portal → Azure Entra ID → App registrations
- Ensure no extra spaces or characters in configuration
- Verify the client secret hasn't expired in Azure Portal

**"Scope mismatch" error**:
- Verify `appScope` includes all required scopes
- Use space-separated scope list in appsettings.json
- Example: `vso.agentpools vso.build_execute vso.code_write`

**SSL certificate errors (Development)**:
- Trust the development HTTPS certificate: `dotnet dev-certs https --trust`
- Restart browser after trusting certificate

**Session/Cookie issues**:
- Check browser privacy settings aren't blocking cookies
- Verify `Session` middleware is configured before `UseRouting` in Program.cs
- Clear browser cache/cookies

**Authorization code not exchanged**:
- Check `EnvironmentController.Create` action handles the callback
- Verify application can reach `https://app.vssps.visualstudio.com/oauth2/token`
- Review log4net logs in `Logs/` directory for detailed error messages

## Code Conventions

### Logging
Use log4net via static logger: `ILog logger = LogManager.GetLogger("ErrorLog");`  
Configured in [log4net.config](src/VstsDemoBuilder.Net8/log4net.config) to write daily rolling files to Logs/

### API Versioning
All Azure DevOps API versions are defined in [appsettings.json](src/VstsDemoBuilder.Net8/appsettings.json) root:
```json
{
  "WorkItemsVersion": "4.1",
  "BuildVersion": "4.1",
  "ReleaseVersion": "4.1"
}
```
Reference via `AppSettings.Get("WorkItemsVersion")` from anywhere in the application

### Dependency Injection
Controllers use constructor injection for services:
```csharp
public EnvironmentController(IProjectService _projectService, 
                             IAccountService _accountService,
                             ITemplateService _templateService)
```

## Critical Files
- [src/VstsDemoBuilder.Net8/Program.cs](src/VstsDemoBuilder.Net8/Program.cs) - application startup and configuration
- [src/VstsDemoBuilder.Net8/Controllers/EnvironmentController.cs](src/VstsDemoBuilder.Net8/Controllers/EnvironmentController.cs) - main project creation endpoint
- [src/VstsDemoBuilder.Net8/Services/ProjectService.cs](src/VstsDemoBuilder.Net8/Services/ProjectService.cs) - orchestrates all Azure DevOps API calls
- [src/VstsRestAPI.Net8/ApiServiceBase.cs](src/VstsRestAPI.Net8/ApiServiceBase.cs) - base class for all API wrappers
- [src/VstsRestAPI.Net8/Configuration.cs](src/VstsRestAPI.Net8/Configuration.cs) - context object pattern
- [src/VstsDemoBuilder.Net8/Templates/TemplateSetting.json](src/VstsDemoBuilder.Net8/Templates/TemplateSetting.json) - template catalog
- [src/VstsDemoBuilder.Net8/Infrastructure/LegacyController.cs](src/VstsDemoBuilder.Net8/Infrastructure/LegacyController.cs) - session management base class
- [src/VstsDemoBuilder.Net8/Infrastructure/AppSettings.cs](src/VstsDemoBuilder.Net8/Infrastructure/AppSettings.cs) - configuration helper
- [src/VstsDemoBuilder.Net8/appsettings.json](src/VstsDemoBuilder.Net8/appsettings.json) - application configuration

## Common Tasks

**Add new Azure DevOps API wrapper**: Create class in VstsRestAPI.Net8/{category}/ inheriting ApiServiceBase, implement methods using `GetHttpClient()`

**Add template support for new artifact type**: Update ProjectTemplate.json schema, add JSON file for artifact, implement parsing/provisioning in ProjectService

**Debug template generation**: Check ProjectService.StatusMessages dictionary, enable log4net verbose logging, verify JSON structure matches Azure DevOps API schema
