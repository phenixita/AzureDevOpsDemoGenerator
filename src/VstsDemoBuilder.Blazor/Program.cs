using Microsoft.AspNetCore.Authentication.Cookies;
using VstsDemoBuilder.Blazor.Components;
using VstsDemoBuilder.Blazor.Configuration;
using VstsDemoBuilder.Blazor.Services;
using VstsDemoBuilder.Blazor.Session;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<AzureDevOpsOAuthOptions>(options =>
{
    builder.Configuration.GetSection(AzureDevOpsOAuthOptions.SectionName).Bind(options);

    options.AuthorityUri = GetConfigValue(options.AuthorityUri, builder.Configuration, "AuthorityUri");
    options.ClientId = GetConfigValue(options.ClientId, builder.Configuration, "ClientId", "AzureEntraClientId");
    options.ClientSecret = GetConfigValue(options.ClientSecret, builder.Configuration, "ClientSecret");
    options.RedirectUri = GetConfigValue(options.RedirectUri, builder.Configuration, "RedirectUri");
    options.Scope = GetConfigValue(options.Scope, builder.Configuration, "appScope", "AzureResourceUri");
    options.BaseAddress = GetConfigValue(options.BaseAddress, builder.Configuration, "BaseAddress");
});

builder.Services.AddHttpClient<IAzureDevOpsAuthService, AzureDevOpsAuthService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.AccessDeniedPath = "/";
        options.LogoutPath = "/auth/signout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/project/create", StringComparison.OrdinalIgnoreCase))
    {
        var hasIdentity = context.User?.Identity?.IsAuthenticated == true;
        var organizations = context.Session.GetStringList(SessionKeys.Organizations);
        var hasOrganizations = organizations.Count > 0;
        if (!hasIdentity || !hasOrganizations)
        {
            context.Response.Redirect("/");
            return;
        }
    }

    await next();
});

app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static string GetConfigValue(string currentValue, IConfiguration configuration, params string[] fallbackKeys)
{
    if (!string.IsNullOrWhiteSpace(currentValue))
    {
        return currentValue;
    }

    foreach (var key in fallbackKeys)
    {
        var candidate = configuration[key];
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            return candidate;
        }
    }

    return string.Empty;
}

public partial class Program;
