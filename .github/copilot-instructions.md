# Azure DevOps Demo Generator - AI Agent Instructions

## Project Overview
ASP.NET MVC web application that generates pre-populated Azure DevOps projects from JSON-based templates. Two-tier architecture: **VstsDemoBuilder** (web frontend) and **VstsRestAPI** (Azure DevOps API wrapper library).

## Architecture Patterns

### Service Layer
All business logic lives in service classes implementing interfaces:
- `ProjectService` (IProjectService) - orchestrates project generation
- `TemplateService` (ITemplateService) - manages template loading/validation  
- `AccountService` (IAccountService) - handles authentication
- `ExtractorService` (IExtractorService) - extracts templates from existing projects

Example: [Services/ProjectService.cs](src/VstsDemoBuilder/Services/ProjectService.cs)

### Azure DevOps API Wrapper Pattern
All API interactions inherit from `ApiServiceBase` which provides:
- HttpClient configuration with OAuth Bearer token
- Base configuration (account, project, PAT) via `IConfiguration`
- Consistent error handling via `LastFailureMessage` property

Example classes: [VstsRestAPI/WorkItemAndTracking/WorkItem.cs](src/VstsRestAPI/WorkItemAndTracking/WorkItem.cs), [VstsRestAPI/Git/Repository.cs](src/VstsRestAPI/Git/Repository.cs)

```csharp
public class MyAzureDevOpsService : ApiServiceBase
{
    public MyAzureDevOpsService(IConfiguration configuration) : base(configuration) {}
    
    protected HttpClient GetHttpClient() // Inherited, pre-configured
}
```

### Configuration Object
`Configuration` class (VstsRestAPI/Configuration.cs) carries context between API calls:
- PersonalAccessToken, AccountName, Project, ProjectId
- API version strings, URI parameters
- Pass through constructor for all ApiServiceBase-derived classes

### Progress Tracking for Long Operations
`ProjectService.StatusMessages` static dictionary tracks async project generation:
- Key format: `"{userId}_{timestamp}"` or `"{userId}_{timestamp}_Errors"`
- Polled by client via `EnvironmentController.GetCurrentProgress(string id)`
- Thread-safe via `objLock` object

## Template System

### Template Structure  
Templates live in [src/VstsDemoBuilder/Templates/{TemplateName}/](src/VstsDemoBuilder/Templates/)

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
3. Update [Templates/TemplateSetting.json](src/VstsDemoBuilder/Templates/TemplateSetting.json) with metadata
4. Add icon to Templates/TemplateImages/

See [docs/Using-The-Template-Extractor.md](docs/Using-The-Template-Extractor.md) for extracting from existing projects.

## Local Development Workflow

**Pre-requisites** ([docs/Local-Development.md](docs/Local-Development.md)):
- Visual Studio 2017+, IIS with HTTPS, SQL Server 2016 Express LocalDB
- Edit `C:\Windows\System32\Drivers\etc\hosts` to map custom domain to 127.0.0.1
- Create self-signed cert in IIS for that domain
- Register OAuth app at https://app.vsaex.visualstudio.com/app/register with callback `https://{yourdomain}/Environment/Create`

**Configuration**: Store OAuth credentials in IIS Configuration Editor, not web.config:
- RedirectUri, ClientId, ClientSecret, AppScope (encoded)

**Debugging**: Set Project URL to match IIS site, not IIS Express

## Code Conventions

### Logging
Use log4net via static logger: `ILog logger = LogManager.GetLogger("ErrorLog");`  
Configured in [Web.config](src/VstsDemoBuilder/Web.config) to write daily rolling files to Logs/

### API Versioning
Define all Azure DevOps API versions in [Web.config](src/VstsDemoBuilder/Web.config) appSettings:
```xml
<add key="WorkItemsVersion" value="4.1"/>
<add key="BuildVersion" value="4.1"/>
```
Reference via `ConfigurationManager.AppSettings["WorkItemsVersion"]`

### Dependency Injection
Controllers use constructor injection for services:
```csharp
public EnvironmentController(IProjectService _projectService, 
                             IAccountService _accountService,
                             ITemplateService _templateService)
```

## Critical Files
- [src/VstsDemoBuilder/Controllers/EnvironmentController.cs](src/VstsDemoBuilder/Controllers/EnvironmentController.cs) - main project creation endpoint
- [src/VstsDemoBuilder/Services/ProjectService.cs](src/VstsDemoBuilder/Services/ProjectService.cs) - orchestrates all Azure DevOps API calls
- [src/VstsRestAPI/ApiServiceBase.cs](src/VstsRestAPI/ApiServiceBase.cs) - base class for all API wrappers
- [src/VstsRestAPI/Configuration.cs](src/VstsRestAPI/Configuration.cs) - context object pattern
- [src/VstsDemoBuilder/Templates/TemplateSetting.json](src/VstsDemoBuilder/Templates/TemplateSetting.json) - template catalog

## Common Tasks

**Add new Azure DevOps API wrapper**: Create class in VstsRestAPI/{category}/ inheriting ApiServiceBase, implement methods using `GetHttpClient()`

**Add template support for new artifact type**: Update ProjectTemplate.json schema, add JSON file for artifact, implement parsing/provisioning in ProjectService

**Debug template generation**: Check ProjectService.StatusMessages dictionary, enable log4net verbose logging, verify JSON structure matches Azure DevOps API schema
