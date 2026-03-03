using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzdoGenCli.Models;

namespace AzdoGenCli.Auth
{
    /// <summary>
    /// Orchestrates multi-layered authentication logic: PAT -> Env Var -> Cache -> Refresh -> Browser
    /// </summary>
    internal class AuthenticationOrchestrator
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public AuthenticationOrchestrator(IConfiguration config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<(string? accessToken, AccessDetails? oauthToken)> AuthenticateAsync(CliArgs cliArgs)
        {
            // Layer 1: Command Line PAT
            if (!string.IsNullOrEmpty(cliArgs.Pat))
            {
                _logger.LogInformation("Using PAT from --pat argument");
                Console.WriteLine("✓ Using Personal Access Token from command line");
                return (cliArgs.Pat, null);
            }

            // Layer 2: Environment Variable PAT
            string? envPat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
            if (!string.IsNullOrEmpty(envPat))
            {
                _logger.LogInformation("Using PAT from AZURE_DEVOPS_PAT environment variable");
                Console.WriteLine("✓ Using Personal Access Token from environment variable");
                return (envPat, null);
            }

            // Layer 3: Cached OAuth Token
            var oauthToken = TokenCache.GetToken(_logger);
            if (oauthToken != null && !string.IsNullOrEmpty(oauthToken.access_token))
            {
                // Layer 4: Check and Refresh if needed
                if (NeedsRefresh(oauthToken))
                {
                    oauthToken = await TryRefreshAsync(oauthToken);
                }

                if (oauthToken != null && !string.IsNullOrEmpty(oauthToken.access_token))
                {
                    Console.WriteLine("✓ Using cached authentication");
                    return (oauthToken.access_token, oauthToken);
                }
            }

            // Layer 5: Interactive Browser Authentication
            return await InteractiveLoginAsync();
        }

        private bool NeedsRefresh(AccessDetails token)
        {
            if (!token.acquired_at.HasValue) return true;

            int expiresInSeconds = 3600;
            int.TryParse(token.expires_in, out expiresInSeconds);
            
            var expirationTime = token.acquired_at.Value.AddSeconds(expiresInSeconds).AddMinutes(-5);
            return DateTime.UtcNow > expirationTime;
        }

        private async Task<AccessDetails?> TryRefreshAsync(AccessDetails token)
        {
            if (string.IsNullOrEmpty(token.refresh_token)) return null;

            _logger.LogInformation("Attempting silent token refresh");
            Console.WriteLine("🔄 Refreshing authentication...");

            var tenantId = _config["LegacyAppSettings:TenantId"] ?? "common";
            var clientId = _config["LegacyAppSettings:ClientId"] ?? "71a1f726-dc00-4477-a038-5087fd0e71d3";
            var appScope = _config["LegacyAppSettings:appScope"] ?? "499b84ac-1321-427f-aa17-267ca6975798/.default offline_access";
            var redirectUri = _config["LegacyAppSettings:RedirectUri"] ?? "http://localhost:5001";

            var refreshedToken = OAuthTokenService.Refresh_AccessToken(
                token.refresh_token!, 
                tenantId, 
                redirectUri, 
                clientId, 
                appScope, 
                _logger);

            if (refreshedToken != null && !string.IsNullOrEmpty(refreshedToken.access_token))
            {
                TokenCache.SaveToken(refreshedToken, _logger);
                _logger.LogInformation("Silent token refresh successful");
                return refreshedToken;
            }

            _logger.LogWarning("Silent token refresh failed. Will require interactive login.");
            return null;
        }

        private async Task<(string? accessToken, AccessDetails? oauthToken)> InteractiveLoginAsync()
        {
            _logger.LogInformation("Starting OAuth browser authentication flow");
            Console.WriteLine("Starting OAuth authentication...");
            Console.WriteLine();

            var token = await BrowserAuthenticator.AuthenticateAsync(_config, _logger);
            TokenCache.SaveToken(token, _logger);

            Console.WriteLine();
            Console.WriteLine("✓ Authentication successful!");
            _logger.LogInformation("OAuth authentication completed successfully");

            return (token.access_token, token);
        }
    }
}
