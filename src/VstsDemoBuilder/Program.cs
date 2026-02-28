using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using VstsDemoBuilder.Infrastructure;
using VstsDemoBuilder.ServiceInterfaces;
using VstsDemoBuilder.Services;

var builder = WebApplication.CreateBuilder(args);

LegacyConfigBootstrapper.Apply(builder.Configuration.GetSection("LegacyAppSettings"));

builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

app.UseHttpsRedirection();

// Serve legacy ASP.NET MVC static folders from the content root
foreach (var folder in new[] { "Content", "Scripts", "assets", "Images", "fonts" })
{
    var folderPath = Path.Combine(app.Environment.ContentRootPath, folder);
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
