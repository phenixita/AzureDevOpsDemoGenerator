using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Web;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzdoGenCli.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AzdoGenCli.Auth
{
    /// <summary>
    /// Headless browser-based OAuth 2.0 authentication flow for CLI
    /// Starts local HTTP listener and displays URL for manual browser opening
    /// </summary>
    public static class BrowserAuthenticator
    {
        private const string NativeClientRedirectUri = "https://login.microsoftonline.com/common/oauth2/nativeclient";

        /// <summary>
        /// Authenticate user via OAuth 2.0 browser flow with PKCE
        /// Returns AccessDetails with token on success, or throws on failure
        /// </summary>
        public static async Task<AccessDetails> AuthenticateAsync(IConfiguration config, ILogger logger)
        {
            // Read OAuth configuration
            string tenantId = config["LegacyAppSettings:TenantId"] ?? "common";
            string? clientId = config["LegacyAppSettings:ClientId"];
            string? clientSecret = config["LegacyAppSettings:ClientSecret"];
            string? appScope = config["LegacyAppSettings:appScope"];
            string? configuredRedirectUri = config["LegacyAppSettings:RedirectUri"]?.Trim();
            
            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(appScope))
            {
                throw new InvalidOperationException("ClientId and appScope must be configured in appsettings.json");
            }

            // Generate PKCE challenge
            var (codeVerifier, codeChallenge) = GeneratePkceChallenge();

            bool hasConfiguredRedirectUri = !string.IsNullOrWhiteSpace(configuredRedirectUri);
            bool isNativeClientMode = IsNativeClientRedirectUri(configuredRedirectUri);

            string callbackUrl;
            string listenerPrefix;

            if (hasConfiguredRedirectUri)
            {
                callbackUrl = configuredRedirectUri!;
                listenerPrefix = string.Empty;
            }
            else
            {
                int port = FindFreePort(5001, 5099);
                if (port == -1)
                {
                    throw new InvalidOperationException("No free port available for OAuth callback");
                }

                callbackUrl = $"http://localhost:{port}";
                listenerPrefix = $"http://localhost:{port}/";
            }

            // Build OAuth authorize URL with PKCE challenge
            string authorizeUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?" +
                                  $"client_id={WebUtility.UrlEncode(clientId)}&" +
                                  $"response_type=code&" +
                                  $"redirect_uri={WebUtility.UrlEncode(callbackUrl)}&" +
                                  $"scope={WebUtility.UrlEncode(appScope)}&" +
                                  $"code_challenge={WebUtility.UrlEncode(codeChallenge)}&" +
                                  $"code_challenge_method=S256&" +
                                  $"response_mode=query";

            if (isNativeClientMode)
            {
                return ExchangeTokenFromManualRedirectInput(
                    authorizeUrl,
                    callbackUrl,
                    clientId,
                    clientSecret,
                    appScope,
                    tenantId,
                    codeVerifier,
                    logger);
            }

            if (hasConfiguredRedirectUri && !TryGetHttpListenerPrefix(callbackUrl, out listenerPrefix))
            {
                throw new InvalidOperationException(
                    $"Configured RedirectUri '{callbackUrl}' is not supported for automatic callback capture.");
            }

            // Start HTTP listener
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenerPrefix);
            listener.Start();

            try
            {
                Console.WriteLine();
                Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║  Azure DevOps Authentication Required                                       ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
                Console.WriteLine();
                Console.WriteLine("Opening your default browser for authentication...");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(authorizeUrl);
                Console.ResetColor();
                Console.WriteLine();
                Console.WriteLine("Waiting for authentication to complete...");
                Console.WriteLine();

                OpenBrowser(authorizeUrl);

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var context = await listener.GetContextAsync().WaitAsync(cts.Token);

                string? code = context.Request.QueryString["code"];
                string? error = context.Request.QueryString["error"];

                if (!string.IsNullOrEmpty(error))
                {
                    string errorDescription = context.Request.QueryString["error_description"] ?? error;
                    SendBrowserResponse(context.Response, $"Authentication failed: {errorDescription}", false);
                    throw new InvalidOperationException($"OAuth error: {errorDescription}");
                }

                if (string.IsNullOrEmpty(code))
                {
                    SendBrowserResponse(context.Response, "No authorization code received", false);
                    throw new InvalidOperationException("No authorization code received from OAuth callback");
                }

                string tokenRequestBody = OAuthTokenService.GenerateRequestPostData(
                    clientId, code, callbackUrl, appScope, clientSecret, codeVerifier);
                
                AccessDetails tokenDetails = OAuthTokenService.GetAccessToken(tokenRequestBody, tenantId, logger);

                if (!string.IsNullOrEmpty(tokenDetails.error))
                {
                    SendBrowserResponse(context.Response, $"Token exchange failed: {tokenDetails.error_description}", false);
                    throw new InvalidOperationException($"Token exchange failed: {tokenDetails.error_description}");
                }

                SendBrowserResponse(context.Response, "Authentication successful! You can close this window.", true);
                return tokenDetails;
            }
            finally
            {
                listener.Stop();
                listener.Close();
            }
        }

        private static (string verifier, string challenge) GeneratePkceChallenge()
        {
            byte[] verifierBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(verifierBytes);
            }
            string verifier = Base64UrlEncode(verifierBytes);

            byte[] challengeBytes;
            using (var sha256 = SHA256.Create())
            {
                challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
            }
            string challenge = Base64UrlEncode(challengeBytes);

            return (verifier, challenge);
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            string base64 = Convert.ToBase64String(bytes);
            return base64.Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open browser: {ex.Message}");
            }
        }

        private static AccessDetails ExchangeTokenFromManualRedirectInput(
            string authorizeUrl,
            string callbackUrl,
            string clientId,
            string? clientSecret,
            string appScope,
            string tenantId,
            string codeVerifier,
            ILogger logger)
        {
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Azure DevOps Authentication Required                                       ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine("Open your browser and navigate to:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(authorizeUrl);
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("After sign-in, copy the full redirected URL from the browser address bar and paste it below:");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Redirected URL: ");
            Console.ResetColor();

            string redirectedUrl = SanitizeRedirectedUrlInput(Console.ReadLine());

            if (!Uri.TryCreate(redirectedUrl, UriKind.Absolute, out Uri? redirectedUri))
            {
                throw new InvalidOperationException("The provided redirected URL is not a valid absolute URL.");
            }

            Dictionary<string, string> queryValues = ParseQueryValues(redirectedUri.Query);

            queryValues.TryGetValue("error", out string? error);
            queryValues.TryGetValue("error_description", out string? errorDescription);

            if (!string.IsNullOrWhiteSpace(error))
            {
                string message = string.IsNullOrWhiteSpace(errorDescription) ? error : errorDescription;
                throw new InvalidOperationException($"OAuth error: {message}");
            }

            queryValues.TryGetValue("code", out string? code);
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new InvalidOperationException("No authorization code found in redirected URL query string.");
            }

            string tokenRequestBody = OAuthTokenService.GenerateRequestPostData(
                clientId,
                code,
                callbackUrl,
                appScope,
                clientSecret,
                codeVerifier);

            AccessDetails tokenDetails = OAuthTokenService.GetAccessToken(tokenRequestBody, tenantId, logger);
            if (!string.IsNullOrEmpty(tokenDetails.error))
            {
                throw new InvalidOperationException($"Token exchange failed: {tokenDetails.error_description}");
            }

            return tokenDetails;
        }

        private static string SanitizeRedirectedUrlInput(string? redirectedUrl)
        {
            if (string.IsNullOrWhiteSpace(redirectedUrl))
            {
                throw new InvalidOperationException("No redirected URL was provided.");
            }

            string trimmedRedirectedUrl = redirectedUrl.Trim();
            foreach (char character in trimmedRedirectedUrl)
            {
                if (char.IsControl(character))
                {
                    throw new InvalidOperationException(
                        "The provided redirected URL contains invalid control characters.");
                }
            }

            return trimmedRedirectedUrl;
        }

        private static Dictionary<string, string> ParseQueryValues(string query)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(query))
            {
                return values;
            }

            NameValueCollection parsedQuery = HttpUtility.ParseQueryString(query);
            foreach (string? key in parsedQuery.AllKeys)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    values[key] = parsedQuery[key] ?? string.Empty;
                }
            }

            return values;
        }

        private static bool IsNativeClientRedirectUri(string? redirectUri)
        {
            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                return false;
            }

            return string.Equals(
                NormalizeUriForComparison(redirectUri),
                NormalizeUriForComparison(NativeClientRedirectUri),
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetHttpListenerPrefix(string callbackUrl, out string listenerPrefix)
        {
            listenerPrefix = string.Empty;
            if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out Uri? callbackUri))
            {
                return false;
            }

            if (!string.Equals(callbackUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!callbackUri.IsLoopback)
            {
                return false;
            }

            listenerPrefix = $"{callbackUri.Scheme}://{callbackUri.Host}:{callbackUri.Port}/";
            return true;
        }

        private static string NormalizeUriForComparison(string uri)
        {
            return uri.Trim().TrimEnd('/');
        }

        private static int FindFreePort(int startPort, int endPort)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Loopback, port);
                    listener.Start();
                    listener.Stop();
                    return port;
                }
                catch (SocketException)
                {
                }
            }
            return -1;
        }

        private static void SendBrowserResponse(HttpListenerResponse response, string message, bool success)
        {
            string color = success ? "#28a745" : "#dc3545";
            string icon = success ? "✓" : "✗";
            
            string html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>AzdoGenCli - Authentication</title>
    <style>
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }}
        .container {{
            background: white;
            border-radius: 12px;
            padding: 48px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            text-align: center;
            max-width: 500px;
        }}
        .icon {{
            font-size: 72px;
            color: {color};
            margin-bottom: 24px;
        }}
        h1 {{
            margin: 0 0 16px 0;
            font-size: 28px;
            color: #333;
        }}
        p {{
            margin: 0;
            font-size: 16px;
            color: #666;
            line-height: 1.5;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='icon'>{icon}</div>
        <h1>AzdoGenCli</h1>
        <p>{WebUtility.HtmlEncode(message)}</p>
    </div>
</body>
</html>";

            byte[] buffer = Encoding.UTF8.GetBytes(html);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;
            response.StatusCode = 200;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}
