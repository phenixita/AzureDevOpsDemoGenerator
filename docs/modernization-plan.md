## Plan: Modernize AzureDevOps Demo Generator - Critical Issues First

**TL;DR**: Fix critical socket exhaustion and thread pool deadlock risks in HTTP layer, then systematically modernize dependency injection, eliminate async-over-sync patterns, and replace legacy frontend libraries. This phased approach prioritizes production stability while establishing modern .NET 10 patterns.

**Scope**: Critical fixes first -> Core patterns -> Frontend modernization. Breaking changes acceptable for cleaner architecture.

---

## **Phase 1: CRITICAL STABILITY FIXES** (2-3 weeks)

Fix socket exhaustion and thread pool starvation issues that cause production instability under load.

### Steps

1. **Replace all `new HttpClient()` instantiations with IHttpClientFactory** (*must complete first*)
   - Register typed clients for VstsRestAPI services (Projects, WorkItem, Queries, etc.)
   - Configure default timeout (30s), retry policies with Polly
   - Fix: [src/VstsRestAPI/Services/HttpServices.cs](src/VstsRestAPI/Services/HttpServices.cs#L24), [src/VstsDemoBuilder/Services/AccountService.cs](src/VstsDemoBuilder/Services/AccountService.cs#L90), all ApiServiceBase derived classes

2. **Eliminate ALL `.Result` and `.GetAwaiter().GetResult()` calls** (*parallel with step 1*)
   - Convert synchronous methods to async Task<T>
   - Add CancellationToken parameters to all async methods
   - Update controller action methods to async Task<IActionResult>
   - Critical files: [WorkItem.cs](src/VstsRestAPI/WorkItemAndTracking/WorkItem.cs#L58), [Queries.cs](src/VstsRestAPI/QueriesAndWidgets/Queries.cs#L285), [ClassificationNodes.cs](src/VstsRestAPI/WorkItemAndTracking/ClassificationNodes.cs), [TemplateService.cs](src/VstsDemoBuilder/Services/TemplateService.cs#L191)

3. **Structured exception handling for HTTP operations** (*depends on step 1*)
   - Replace generic `catch (Exception)` with specific types: HttpRequestException, JsonException, OperationCanceledException
   - Add custom ApiException with StatusCode, ErrorCode properties
   - Use ILogger<T> instead of static logger instances
   - Primary: all VstsRestAPI classes, HttpServices.cs

4. **Remove Thread.Sleep() calls** (*parallel*)
   - Replace `Thread.Sleep(2000)` with async `await Task.Delay(2000, ct)`
   - Found in: [Queries.cs](src/VstsRestAPI/QueriesAndWidgets/Queries.cs#L78)

**Verification after Phase 1**:
1. Run load test with Apache Bench: `ab -n 1000 -c 50 http://localhost:5000/` - verify no socket exhaustion errors
2. Check thread pool starvation: `dotnet-counters monitor --counters System.Runtime[threadpool-*]` - ThreadPool.Queue.Length should stay low
3. Validate all API calls under cancellation: test timeout scenarios with CancellationTokenSource(100ms)
4. Confirm no unhandled exceptions in logs during load test

---

## **Phase 2: CORE MODERNIZATION** (3-4 weeks)

Establish modern dependency injection, configuration, and service patterns.

### Steps

5. **Migrate from ConfigurationManager.AppSettings to IConfiguration** (*depends on Phase 1 completion*)
   - Remove LegacyConfigBootstrapper bridge
   - Inject IConfiguration via constructor in all services/controllers
   - Move remaining AppSettings from Web.config to appsettings.json/appsettings.{env}.json
   - Critical: [AccountController.cs](src/VstsDemoBuilder/Controllers/AccountController.cs#L60), [ProjectService.cs](src/VstsDemoBuilder/Services/ProjectService.cs#L129), [EnvironmentController.cs](src/VstsDemoBuilder/Controllers/EnvironmentController.cs)

6. **Eliminate static state in ProjectService** (*depends on step 5*)
   - Replace static `statusMessages` Dictionary with scoped IStatusMessenger service
   - Use SignalR Hub for real-time status updates (already have infrastructure)
   - Replace static `logger` with injected ILogger<ProjectService>
   - Convert static methods to instance methods with DI
   - File: [ProjectService.cs](src/VstsDemoBuilder/Services/ProjectService.cs#L45-L80)

7. **Introduce Options pattern for strongly-typed configuration** (*parallel with step 6*)
   - Create AzureDevOpsOptions, GitHubOptions, ApplicationOptions classes
   - Register via `services.Configure<T>(configuration.GetSection("..."))`
   - Replace direct IConfiguration["key"] with IOptions<T> injection
   - Enables validation: `services.AddOptions<AzureDevOpsOptions>().Validate(...)`

8. **Dependency injection for VstsRestAPI services** (*depends on step 6*)
   - Register Projects, WorkItem, Queries, ClassificationNodes as scoped services
   - Create interfaces: IProjectsService, IWorkItemService, etc.
   - Remove `new Projects(config)` instantiation from controllers
   - Update: [ExtractorController.cs](src/VstsDemoBuilder/Controllers/ExtractorController.cs#L120), ProjectService.cs

9. **Abstract file I/O with IFileProvider** (*parallel with step 8*)
   - Create ITemplateRepository, ILogRepository interfaces
   - Use PhysicalFileProvider for Templates, PrivateTemplates, Log directories
   - Replace direct File.ReadAllText, Directory.Exists calls
   - Enable unit testing with mock file providers
   - Files: [EnvironmentController.cs](src/VstsDemoBuilder/Controllers/EnvironmentController.cs#L89-116), [ProjectService.cs](src/VstsDemoBuilder/Services/ProjectService.cs)

**Verification after Phase 2**:
1. Confirm no ConfigurationManager.AppSettings calls: `grep -r "ConfigurationManager.AppSettings" src/` returns 0 results
2. Verify DI container registration: app startup logs show all scoped services registered
3. Test configuration validation: intentionally misconfigure appsettings.json and verify startup fails with clear error
4. Check no static state: run parallel requests and verify no race conditions in status updates

---

## **Phase 3: API & URL CONSTRUCTION** (1-2 weeks)

Standardize HTTP request patterns and URL building.

### Steps

10. **Migrate Newtonsoft.Json to System.Text.Json** (*depends on Phase 2*)
    - Replace JsonConvert.SerializeObject/DeserializeObject
    - Use JsonSerializer with configured options (camelCase, nulls)
    - Remove package references to Newtonsoft.Json where possible
    - Use GetFromJsonAsync/PostAsJsonAsync extension methods
    - Test: ensure backward compatibility with existing JSON payloads

11. **Create strongly-typed API endpoint builders** (*parallel with step 10*)
    - Define IAzureDevOpsEndpoints interface with methods like GetWorkItemUri(), GetQueryUri()
    - Use UriBuilder instead of string.Format for URL construction
    - Encode query parameters properly
    - Files: [ClassificationNodes.cs](src/VstsRestAPI/WorkItemAndTracking/ClassificationNodes.cs#L30), [Queries.cs](src/VstsRestAPI/QueriesAndWidgets/Queries.cs#L138), all VstsRestAPI classes

12. **Add Polly resilience policies** (*depends on Phase 1, step 1*)
    - Transient error retry: 3 attempts with exponential backoff (500ms, 1s, 2s)
    - Circuit breaker: open after 5 consecutive failures, half-open after 30s
    - Timeout policy: 30s per request
    - Register via AddHttpClient().AddPolicyHandler()

**Verification after Phase 3**:
1. Compare JSON serialization: serialize/deserialize test objects with both libraries, verify identical output
2. Validate URL encoding: test special characters in project names, verify correct encoding
3. Simulate transient failures: use Chaos Monkey or mock server, verify Polly retries correctly
4. Check resilience: simulate API downtime, verify circuit breaker opens and logs appropriately

---

## **Phase 4: FRONTEND MODERNIZATION** (2-3 weeks)

Remove jQuery, Knockout.js; use vanilla ES2022+ JavaScript or lightweight library.

### Steps

13. **Replace jQuery with vanilla JavaScript** (*parallel with Phase 3*)
    - Migrate `$.ajax()` to Fetch API with async/await
    - Replace `$('#selector')` with document.getElementById/querySelector
    - Remove $(document).ready, use DOMContentLoaded
    - Update: [Scripts/AppScripts/*.js](src/VstsDemoBuilder/Scripts/AppScripts/)
    - Bundle with Vite or esbuild for modern ES modules

14. **Remove Knockout.js, use vanilla JS or Alpine.js** (*depends on step 13*)
    - Identify Knockout bindings in views: `data-bind="..."`
    - Replace observables with native form state or Alpine.js x-model directives
    - For complex scenarios: consider lightweight reactive library (Alpine.js, Petite-Vue)
    - Critical views: [CreateProject.cshtml](src/VstsDemoBuilder/Views/Environment/CreateProject.cshtml), other Razor views with data-bind

15. **Modernize JavaScript tooling** (*parallel with step 14*)
    - Add package.json with npm/pnpm scripts for bundling, minification
    - Use Vite for dev server with HMR, esbuild for production builds
    - Configure TypeScript for type safety (even if gradual adoption)
    - Setup ESLint with modern rules (no-var, prefer-const, async best practices)

16. **Update inline scripts to external modules** (*depends on step 15*)
    - Extract inline `<script>` blocks from Razor views to separate .js files
    - Use `<script type="module" src="~/js/createProject.js"></script>`
    - Leverage ES6 imports for shared utilities
    - Reduce global scope pollution

**Verification after Phase 4**:
1. Browser compatibility: test in Chrome, Firefox, Edge, Safari - verify no jQuery/Knockout dependency errors
2. Bundle size: compare before/after with `npx source-map-explorer` - target 50%+ reduction
3. Lighthouse audit: verify Performance, Accessibility, Best Practices scores improve (target 90+)
4. Manual testing: validate all form submissions, AJAX requests, dynamic UI updates work correctly

---

## **Relevant Files**

### Critical (Phase 1 - Stability)
- [src/VstsRestAPI/Services/HttpServices.cs](src/VstsRestAPI/Services/HttpServices.cs) - HttpClient instantiation pattern
- [src/VstsRestAPI/ApiServiceBase.cs](src/VstsRestAPI/ApiServiceBase.cs) - Base class for HTTP services
- [src/VstsRestAPI/WorkItemAndTracking/WorkItem.cs](src/VstsRestAPI/WorkItemAndTracking/WorkItem.cs) - Extensive `.Result` usage
- [src/VstsRestAPI/QueriesAndWidgets/Queries.cs](src/VstsRestAPI/QueriesAndWidgets/Queries.cs) - Thread.Sleep removal
- [src/VstsDemoBuilder/Services/AccountService.cs](src/VstsDemoBuilder/Services/AccountService.cs) - Direct HttpClient usage

### Core Modernization (Phase 2)
- [src/VstsDemoBuilder/Services/ProjectService.cs](src/VstsDemoBuilder/Services/ProjectService.cs) - Static state elimination, DI
- [src/VstsDemoBuilder/Infrastructure/LegacyConfigBootstrapper.cs](src/VstsDemoBuilder/Infrastructure/LegacyConfigBootstrapper.cs) - Remove bridge
- [src/VstsDemoBuilder/Controllers/AccountController.cs](src/VstsDemoBuilder/Controllers/AccountController.cs) - ConfigurationManager migration
- [src/VstsDemoBuilder/Controllers/EnvironmentController.cs](src/VstsDemoBuilder/Controllers/EnvironmentController.cs) - File I/O abstraction
- [src/VstsDemoBuilder/Controllers/ExtractorController.cs](src/VstsDemoBuilder/Controllers/ExtractorController.cs) - DI injection
- [src/VstsDemoBuilder/Program.cs](src/VstsDemoBuilder/Program.cs) - DI registration, Options pattern

### API Layer (Phase 3)
- All classes in [src/VstsRestAPI/](src/VstsRestAPI/) folder - System.Text.Json migration, URL builders
- [src/VstsRestAPI/WorkItemAndTracking/ClassificationNodes.cs](src/VstsRestAPI/WorkItemAndTracking/ClassificationNodes.cs) - URL construction patterns

### Frontend (Phase 4)
- [src/VstsDemoBuilder/Scripts/AppScripts/](src/VstsDemoBuilder/Scripts/AppScripts/) - jQuery/Knockout removal
- [src/VstsDemoBuilder/Views/Environment/CreateProject.cshtml](src/VstsDemoBuilder/Views/Environment/CreateProject.cshtml) - Knockout bindings
- All Razor views in [src/VstsDemoBuilder/Views/](src/VstsDemoBuilder/Views/) with data-bind attributes

### New Files to Create
- `src/VstsRestAPI/Infrastructure/IAzureDevOpsEndpoints.cs` - Typed endpoint builders
- `src/VstsDemoBuilder/Services/IStatusMessenger.cs` - SignalR status broadcaster
- `src/VstsDemoBuilder/Services/ITemplateRepository.cs` - File I/O abstraction
- `src/VstsDemoBuilder/Configuration/AzureDevOpsOptions.cs` - Options pattern models
- `package.json`, `vite.config.js` - Frontend tooling

---

## **Verification Summary**

### Phase 1 (Critical)
1. Load test with `ab -n 1000 -c 50` - no socket exhaustion
2. Monitor thread pool with `dotnet-counters` - no starvation
3. Test cancellation scenarios with short timeouts
4. Confirm structured exception handling in logs

### Phase 2 (Core)
1. Grep for ConfigurationManager.AppSettings - zero results
2. Verify DI container registration in startup logs
3. Test configuration validation with invalid appsettings
4. Parallel request testing for race conditions

### Phase 3 (API)
1. JSON compatibility tests (Newtonsoft vs System.Text.Json)
2. URL encoding validation with special characters
3. Polly retry simulation (Chaos Monkey/mock failures)
4. Circuit breaker behavior under API downtime

### Phase 4 (Frontend)
1. Cross-browser testing (Chrome, Firefox, Edge, Safari)
2. Bundle size analysis with source-map-explorer
3. Lighthouse audit (target: 90+ scores)
4. Manual E2E testing of all interactive features

---

## **Decisions**

### Approach
- **Phased rollout** starting with critical stability fixes prevents production risk
- **Breaking changes allowed** enables cleaner architecture without legacy baggage
- **Testing deferred** focuses resources on modernization; recommend adding tests in future iteration

### Technology Choices
- **IHttpClientFactory** (native .NET) over RestSharp for HTTP client management
- **System.Text.Json** over Newtonsoft.Json for AOT-compatibility and performance
- **Vanilla JavaScript** over framework for MVC views keeps bundle size minimal
- **Polly** for resilience policies (industry standard for .NET)

### Scope Boundaries
- **Included**: HTTP layer, async patterns, DI, configuration, frontend JavaScript
- **Excluded**: Database layer (not found), test coverage (deferred), Blazor migration (keeping MVC)
- **Future consideration**: Add comprehensive test suite after modernization stabilizes

### Trade-offs
- **Breaking API changes**: cleaner code vs. consumer updates -> chosen cleaner architecture
- **Full async migration**: all-or-nothing required to prevent deadlocks -> phased per-service
- **Frontend strategy**: MVC+modern JS cheaper than full Blazor rewrite -> incremental improvement

---

## **Further Considerations**

1. **Secret Management**: Currently passing tokens as parameters. Should we integrate Azure Key Vault for production, User Secrets for dev, or defer to future security sprint? *Recommendation: Phase 2.5 - add between Phase 2 and 3*

2. **API Versioning**: Azure DevOps API versions hardcoded in URL strings. Create ApiVersion configuration option with fallback? *Recommendation: Part of Phase 3, step 11 endpoint builders*

3. **SignalR for Real-time Updates**: Existing infrastructure found. Should Phase 2 step 6 fully implement SignalR broadcaster, or keep simpler polling mechanism initially? *Recommendation: Full SignalR implementation - infrastructure already exists*

4. **Frontend Build Pipeline**: CI/CD workflow needs update to run npm install/build before publish. Add now or after Phase 4 completes? *Recommendation: Add in Phase 4, step 15 as part of tooling setup*
