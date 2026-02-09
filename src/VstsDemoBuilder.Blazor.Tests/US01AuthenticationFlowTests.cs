using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VstsDemoBuilder.Blazor.Models;
using VstsDemoBuilder.Blazor.Services;

namespace VstsDemoBuilder.Blazor.Tests;

public class US01AuthenticationFlowTests
{
    [Fact]
    public async Task EntryPage_ShowsSignInCallToAction()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Sign In", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/auth/login", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithUnsupportedBrowser_RedirectsToUnsupportedPage()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1)");

        var response = await client.GetAsync("/auth/login");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/unsupported-browser", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WithSupportedBrowser_StartsOAuthRedirect()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        var response = await client.GetAsync("/auth/login");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("https://login.microsoftonline.com/", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OperationalPage_WithoutValidSession_RedirectsToEntry()
    {
        using var factory = new TestApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/project/create");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CallbackSuccess_RedirectsToProjectPage_AndDisplaysOrganizations()
    {
        var sessionData = new AuthenticatedSession(
            AccessToken: "token-123",
            DisplayName: "Test User",
            Email: "test@example.com",
            Organizations: ["org-alpha", "org-zeta"]);

        using var factory = new TestApplicationFactory(options =>
        {
            options.ResultForCode = code => code == "ok" ? sessionData : null;
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var callback = await client.GetAsync("/auth/callback?code=ok");
        Assert.Equal(HttpStatusCode.Redirect, callback.StatusCode);
        Assert.EndsWith("/project/create", callback.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        var projectPage = await client.GetAsync("/project/create");
        var html = await projectPage.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, projectPage.StatusCode);
        Assert.Contains("org-alpha", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("org-zeta", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SignOut_ClearsSession_AndBlocksOperationalPage()
    {
        var sessionData = new AuthenticatedSession(
            AccessToken: "token-123",
            DisplayName: "Test User",
            Email: "test@example.com",
            Organizations: ["org-alpha"]);

        using var factory = new TestApplicationFactory(options =>
        {
            options.ResultForCode = _ => sessionData;
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        await client.GetAsync("/auth/callback?code=ok");
        var signOut = await client.GetAsync("/auth/signout");

        Assert.Equal(HttpStatusCode.Redirect, signOut.StatusCode);
        Assert.Equal("/", signOut.Headers.Location?.ToString());

        var projectPage = await client.GetAsync("/project/create");
        Assert.Equal(HttpStatusCode.Redirect, projectPage.StatusCode);
        Assert.Contains("/", projectPage.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<FakeAzureDevOpsAuthService>? _configureFake;

    public TestApplicationFactory(Action<FakeAzureDevOpsAuthService>? configureFake = null)
    {
        _configureFake = configureFake;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAzureDevOpsAuthService>();
            var fake = new FakeAzureDevOpsAuthService();
            _configureFake?.Invoke(fake);
            services.AddSingleton<IAzureDevOpsAuthService>(fake);
        });
    }
}

internal sealed class FakeAzureDevOpsAuthService : IAzureDevOpsAuthService
{
    public Func<string, AuthenticatedSession?> ResultForCode { get; set; } = _ => null;

    public string BuildAuthorizationUrl()
    {
        return "https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id=test-client&response_type=code&scope=vso.profile&redirect_uri=https%3A%2F%2Flocalhost%2Fauth%2Fcallback";
    }

    public Task<AuthenticatedSession?> CompleteSignInAsync(string code, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ResultForCode(code));
    }
}
