using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
        /// Authenticate user via OAuth 2.0 browser flow
        /// Returns AccessDetails with token on success, or throws on failure
        /// </summary>
        public static async Task<AccessDetails> AuthenticateAsync(IConfiguration config, ILogger logger)
        {
            // Read OAuth configuration
            string tenantId = config["LegacyAppSettings:TenantId"] ?? "common";
            string? clientId = config["LegacyAppSettings:ClientId"];
            string? appScope = config["LegacyAppSettings:appScope"];
            string? configuredRedirectUri = config["LegacyAppSettings:RedirectUri"]?.Trim();
            
            logger.LogDebug("Using public client (non-confidential) OAuth flow - no client secret");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(appScope))
            {
                throw new InvalidOperationException("ClientId and appScope must be configured in appsettings.json");
            }

            bool hasConfiguredRedirectUri = !string.IsNullOrWhiteSpace(configuredRedirectUri);
            bool isNativeClientMode = IsNativeClientRedirectUri(configuredRedirectUri);

            string callbackUrl;
            string listenerPrefix;

            if (hasConfiguredRedirectUri)
            {
                callbackUrl = configuredRedirectUri!;
                listenerPrefix = string.Empty;
                logger.LogDebug("Using configured OAuth redirect URI: {RedirectUri}", callbackUrl);
            }
            else
            {
                int port = FindFreePort(5001, 5099);
                if (port == -1)
                {
                    throw new InvalidOperationException("No free port available in range 5001-5099 for OAuth callback");
                }

                callbackUrl = $"http://localhost:{port}";
                listenerPrefix = $"http://localhost:{port}/";
                logger.LogDebug("Using generated OAuth callback URL: {CallbackUrl}", callbackUrl);
            }

            // Build OAuth authorize URL
            string authorizeUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?" +
                                  $"client_id={WebUtility.UrlEncode(clientId)}&" +
                                  $"response_type=code&" +
                                  $"redirect_uri={WebUtility.UrlEncode(callbackUrl)}&" +
                                  $"scope={WebUtility.UrlEncode(appScope)}&" +
                                  $"response_mode=query";

            if (isNativeClientMode)
            {
                return ExchangeTokenFromManualRedirectInput(
                    authorizeUrl,
                    callbackUrl,
                    clientId,
                    appScope,
                    tenantId,
                    logger);
            }

            if (hasConfiguredRedirectUri)
            {
                if (!TryGetHttpListenerPrefix(callbackUrl, out listenerPrefix))
                {
                    throw new InvalidOperationException(
                        $"Configured RedirectUri '{callbackUrl}' is not supported for automatic callback capture. " +
                        $"Use '{NativeClientRedirectUri}' for manual copy/paste mode, or leave RedirectUri empty for localhost listener mode.");
                }
            }

            // Start HTTP listener
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(listenerPrefix);
            listener.Start();
            logger.LogDebug("HTTP listener started with prefix {ListenerPrefix}", listenerPrefix);

            try
            {
                // Display URL to user (headless - no auto-launch)
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
                Console.WriteLine("Waiting for authentication to complete...");
                Console.WriteLine();

                // Wait for callback with 2-minute timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var context = await listener.GetContextAsync().WaitAsync(cts.Token);

                // Extract authorization code from query string
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

                logger.LogDebug("Authorization code received, exchanging for token...");

                // Exchange authorization code for access token
                string tokenRequestBody = OAuthTokenService.GenerateRequestPostData(
                    clientId, code, callbackUrl, appScope);
                
                AccessDetails tokenDetails = OAuthTokenService.GetAccessToken(tokenRequestBody, tenantId, logger);

                if (!string.IsNullOrEmpty(tokenDetails.error))
                {
                    SendBrowserResponse(context.Response, $"Token exchange failed: {tokenDetails.error_description}", false);
                    throw new InvalidOperationException($"Token exchange failed: {tokenDetails.error_description}");
                }

                // Success!
                SendBrowserResponse(context.Response, "Authentication successful! You can close this window.", true);
                logger.LogInformation("OAuth authentication completed successfully");

                return tokenDetails;
            }
            catch (OperationCanceledException)
            {
                logger.LogError("Authentication timed out after 2 minutes");
                throw new TimeoutException("Authentication timed out. Please try again.");
            }
            finally
            {
                listener.Stop();
                listener.Close();
                logger.LogDebug("HTTP listener stopped");
            }
        }

        private static AccessDetails ExchangeTokenFromManualRedirectInput(
            string authorizeUrl,
            string callbackUrl,
            string clientId,
            string appScope,
            string tenantId,
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

            logger.LogDebug("Authorization code received from manual redirected URL, exchanging for token...");

            string tokenRequestBody = OAuthTokenService.GenerateRequestPostData(
                clientId,
                code,
                callbackUrl,
                appScope);

            AccessDetails tokenDetails = OAuthTokenService.GetAccessToken(tokenRequestBody, tenantId, logger);
            if (!string.IsNullOrEmpty(tokenDetails.error))
            {
                throw new InvalidOperationException($"Token exchange failed: {tokenDetails.error_description}");
            }

            logger.LogInformation("OAuth authentication completed successfully using manual redirect URI mode");
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
                        "The provided redirected URL contains invalid control characters. Paste the full URL from the browser address bar.");
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

        /// <summary>
        /// Find a free TCP port in the specified range
        /// Returns -1 if no port is available
        /// </summary>
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
                    // Port is in use, try next one
                }
            }
            return -1;
        }

        /// <summary>
        /// Send simple HTML response to browser
        /// </summary>
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
