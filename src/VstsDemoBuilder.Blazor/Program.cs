using log4net.Config;
using Microsoft.Extensions.FileProviders;
using VstsDemoBuilder.Blazor.Components;
using VstsDemoBuilder.Infrastructure;
using VstsDemoBuilder.ServiceInterfaces;
using VstsDemoBuilder.Services;

var builder = WebApplication.CreateBuilder(args);

AppSettings.Initialize(builder.Configuration);
System.Web.Hosting.HostingEnvironment.Initialize(builder.Environment.ContentRootPath);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IExtractorService, ExtractorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

var contentRoot = app.Environment.ContentRootPath;
XmlConfigurator.Configure(new FileInfo(Path.Combine(contentRoot, "log4net.config")));

// Configure static files
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "Templates")),
    RequestPath = "/Templates"
});

app.UseRouting();
app.UseSession();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
