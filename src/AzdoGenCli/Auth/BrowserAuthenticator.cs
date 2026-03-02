using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
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
        /// <summary>
        /// Authenticate user via OAuth 2.0 browser flow
        /// Returns AccessDetails with token on success, or throws on failure
        /// </summary>
        public static async Task<AccessDetails> AuthenticateAsync(IConfiguration config, ILogger logger)
        {
            // Find free port in range 5001-5099
            int port = FindFreePort(5001, 5099);
            if (port == -1)
            {
                throw new InvalidOperationException("No free port available in range 5001-5099 for OAuth callback");
            }

            string callbackUrl = $"http://localhost:{port}/callback";
            logger.LogDebug("Using OAuth callback URL: {CallbackUrl}", callbackUrl);

            // Read OAuth configuration
            string tenantId = config["LegacyAppSettings:TenantId"] ?? "common";
            string? clientId = config["LegacyAppSettings:ClientId"];
            string? clientSecret = config["LegacyAppSettings:ClientSecret"];
            string? appScope = config["LegacyAppSettings:appScope"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(appScope))
            {
                throw new InvalidOperationException("ClientId, ClientSecret, and appScope must be configured in appsettings.json");
            }

            // Build OAuth authorize URL
            string authorizeUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/authorize?" +
                                  $"client_id={WebUtility.UrlEncode(clientId)}&" +
                                  $"response_type=code&" +
                                  $"redirect_uri={WebUtility.UrlEncode(callbackUrl)}&" +
                                  $"scope={WebUtility.UrlEncode(appScope)}&" +
                                  $"response_mode=query";

            // Start HTTP listener
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{port}/");
            listener.Start();
            logger.LogDebug("HTTP listener started on port {Port}", port);

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
                    clientId, clientSecret, code, callbackUrl, appScope);
                
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
