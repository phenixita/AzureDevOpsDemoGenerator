using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VstsDemoBuilder.Blazor.Services;
using VstsDemoBuilder.Blazor.Session;

namespace VstsDemoBuilder.Blazor.Controllers;

[Route("auth")]
public sealed class AuthController : Controller
{
    private readonly IAzureDevOpsAuthService _authService;

    public AuthController(IAzureDevOpsAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet("login")]
    [AllowAnonymous]
    public IActionResult Login()
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        if (IsUnsupportedBrowser(userAgent))
        {
            return Redirect("/unsupported-browser");
        }

        return Redirect(_authService.BuildAuthorizationUrl());
    }

    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Redirect("/");
        }

        var session = await _authService.CompleteSignInAsync(code, cancellationToken);
        if (session is null)
        {
            return Redirect("/");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, session.DisplayName ?? string.Empty),
            new(ClaimTypes.Email, session.Email ?? string.Empty)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        HttpContext.Session.SetString(SessionKeys.AccessToken, session.AccessToken ?? string.Empty);
        HttpContext.Session.SetString(SessionKeys.DisplayName, session.DisplayName ?? string.Empty);
        HttpContext.Session.SetString(SessionKeys.Email, session.Email ?? string.Empty);
        HttpContext.Session.SetStringList(SessionKeys.Organizations, session.Organizations);

        return Redirect("/project/create");
    }

    [HttpGet("signout")]
    [AllowAnonymous]
    public async Task<IActionResult> SignOutUser()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    private static bool IsUnsupportedBrowser(string userAgent)
    {
        return userAgent.Contains("MSIE", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("Trident", StringComparison.OrdinalIgnoreCase);
    }
}
