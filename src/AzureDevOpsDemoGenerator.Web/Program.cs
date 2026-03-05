using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using AzureDevOpsDemoGenerator.Web.Infrastructure;
using AzureDevOpsDemoGenerator.Modules.Core;
using AzureDevOpsDemoGenerator.Modules.Account;
using AzureDevOpsDemoGenerator.Modules.Project;
using AzureDevOpsDemoGenerator.Modules.Template;
using AzureDevOpsDemoGenerator.Modules.Extractor;

var builder = WebApplication.CreateBuilder(args);

LegacyConfigBootstrapper.Apply(builder.Configuration.GetSection("LegacyAppSettings"));

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IExtractorService, ExtractorService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

AppPath.ContentRootPath = app.Environment.ContentRootPath;
AppPath.WebRootPath = app.Environment.WebRootPath;

// App Service terminates TLS at the load balancer — trust forwarded headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Serve legacy ASP.NET MVC static folders from the content root
var contentRoot = app.Environment.ContentRootPath;
foreach (var folder in new[] { "Content", "Scripts", "assets", "Images", "fonts", "Templates" })
{
    var folderPath = Path.Combine(contentRoot, folder);
    if (Directory.Exists(folderPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(folderPath),
            RequestPath = "/" + folder
        });
    }
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Verify}/{id?}");

app.Run();
