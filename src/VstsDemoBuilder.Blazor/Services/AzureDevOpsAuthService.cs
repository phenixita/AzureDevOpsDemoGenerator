using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VstsDemoBuilder.Blazor.Configuration;
using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Services;

public sealed class AzureDevOpsAuthService : IAzureDevOpsAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly AzureDevOpsOAuthOptions _options;

    public AzureDevOpsAuthService(HttpClient httpClient, IOptions<AzureDevOpsOAuthOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public string BuildAuthorizationUrl()
    {
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["scope"] = _options.Scope,
            ["redirect_uri"] = _options.RedirectUri
        };

        var encoded = string.Join(
            "&",
            query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value ?? string.Empty)}"));

        return $"{_options.AuthorityUri}?{encoded}";
    }

    public async Task<AuthenticatedSession?> CompleteSignInAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code) ||
            string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret) ||
            string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            return null;
        }

        var tokenRequest = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = _options.ClientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = _options.RedirectUri
        };

        if (!string.IsNullOrWhiteSpace(_options.Scope))
        {
            tokenRequest["scope"] = _options.Scope;
        }

        using var tokenResponse = await _httpClient.PostAsync(_options.TokenEndpoint, new FormUrlEncodedContent(tokenRequest), cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenPayload = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        var token = JsonSerializer.Deserialize<OAuthTokenResponse>(tokenPayload, JsonOptions);
        if (string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            return null;
        }

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        var profile = await FetchProfileAsync(cancellationToken);
        if (profile is null || string.IsNullOrWhiteSpace(profile.Id))
        {
            return null;
        }

        var organizations = await FetchOrganizationsAsync(profile.Id, cancellationToken);
        return new AuthenticatedSession(
            token.AccessToken,
            profile.DisplayName,
            profile.EmailAddress,
            organizations);
    }

    private async Task<ProfileResponse?> FetchProfileAsync(CancellationToken cancellationToken)
    {
        var endpoint = $"{_options.BaseAddress.TrimEnd('/')}/_apis/profile/profiles/me?details=true&api-version=7.1-preview.3";
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<ProfileResponse>(payload, JsonOptions);
    }

    private async Task<IReadOnlyList<string>> FetchOrganizationsAsync(string memberId, CancellationToken cancellationToken)
    {
        var endpoint = $"{_options.BaseAddress.TrimEnd('/')}/_apis/accounts?memberId={Uri.EscapeDataString(memberId)}&api-version=7.1-preview.1";
        using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);
        var accounts = JsonSerializer.Deserialize<AccountListResponse>(payload, JsonOptions);

        return accounts?.Value
            .Select(item => item.AccountName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OrderBy(name => name)
            .ToArray() ?? [];
    }
}
