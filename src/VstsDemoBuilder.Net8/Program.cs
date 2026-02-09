using log4net.Config;
using Microsoft.Extensions.FileProviders;
using VstsDemoBuilder.Infrastructure;
using VstsDemoBuilder.ServiceInterfaces;
using VstsDemoBuilder.Services;

var builder = WebApplication.CreateBuilder(args);

AppSettings.Initialize(builder.Configuration);
System.Web.Hosting.HostingEnvironment.Initialize(builder.Environment.ContentRootPath);

builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        // Preserve PascalCase property names to match legacy JavaScript expectations
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.None; // Allow HTTP in development
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax; // Allow cookies during OAuth redirects
});

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IExtractorService, ExtractorService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var contentRoot = app.Environment.ContentRootPath;
XmlConfigurator.Configure(new FileInfo(Path.Combine(contentRoot, "log4net.config")));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "Content")),
    RequestPath = "/Content"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "Scripts")),
    RequestPath = "/Scripts"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "Images")),
    RequestPath = "/Images"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "assets")),
    RequestPath = "/assets"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "fonts")),
    RequestPath = "/fonts"
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(contentRoot, "Templates")),
    RequestPath = "/Templates"
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
