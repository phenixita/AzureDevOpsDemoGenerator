using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzdoGenCli.Auth;
using AzdoGenCli.Models;
using AzdoGenCli.Services;
using AzdoGenCli.Infrastructure;

namespace AzdoGenCli
{
    /// <summary>
    /// Main entry point for CLI application logic
    /// Orchestrates the high-level workflow
    /// </summary>
    internal class CliRunner
    {
        private readonly CliArgs _args;
        private IConfiguration? _config;
        private ILoggerFactory? _loggerFactory;
        private ILogger? _logger;

        public CliRunner(CliArgs args)
        {
            _args = args;
        }

        public async Task<int> RunAsync()
        {
            try
            {
                // Help flow
                if (_args.ShowHelp)
                {
                    CliArgs.PrintUsage();
                    return 0;
                }

                // Template listing flow
                if (_args.ListTemplates)
                {
                    new TemplateService().ListTemplates();
                    return 0;
                }

                // Logout flow
                if (_args.ClearCache)
                {
                    TokenCache.ClearToken();
                    Console.WriteLine("✓ Cached authentication cleared");
                    return 0;
                }

                // Initialize core services
                Initialize();
                _logger!.LogInformation("AzdoGenCli starting");

                // Authentication flow
                var (accessToken, oauthToken) = await AuthenticateWithRetriesAsync();
                if (string.IsNullOrEmpty(accessToken)) return 1;

                // Input collection flow
                var cliInput = CollectInput(accessToken, oauthToken);
                if (cliInput == null) return 1;

                DisplaySummary(cliInput);

                // Execution flow
                if (_args.DryRun)
                {
                    Console.WriteLine("🔄 Dry-run mode: No changes will be made");
                    return 0;
                }

                new ProjectProvisioner(_config!, _loggerFactory!).Provision(cliInput);
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Error: {ex.Message}");
                Console.ResetColor();
                _logger?.LogError(ex, "Unhandled CLI error");
                return 1;
            }
        }

        private void Initialize()
        {
            _config = Bootstrapper.LoadConfiguration();
            _loggerFactory = Bootstrapper.ConfigureLogging(_args);
            _logger = _loggerFactory.CreateLogger<CliRunner>();
            
            Console.WriteLine("AzdoGenCli v1.0 - Azure DevOps Demo Project Generator");
            Console.WriteLine();
        }

        private async Task<(string? accessToken, AccessDetails? oauthToken)> AuthenticateWithRetriesAsync()
        {
            var authOrchestrator = new AuthenticationOrchestrator(_config!, _logger!);
            int authRetries = 0;

            while (authRetries < 2)
            {
                var (accessToken, oauthToken) = await authOrchestrator.AuthenticateAsync(_args);
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger!.LogError("Authentication failed: No access token obtained");
                    return (null, null);
                }

                // Test the token via InputCollector (which performs the first API call)
                try
                {
                    // This is a "peek" to see if the token is valid
                    return (accessToken, oauthToken);
                }
                catch (Exception ex) when (IsUnauthorized(ex))
                {
                    _logger!.LogWarning("Authentication failed with 401 Unauthorized. Retrying...");
                    TokenCache.ClearToken(_logger);
                    authRetries++;
                }
            }

            return (null, null);
        }

        private CliInput? CollectInput(string accessToken, AccessDetails? oauthToken)
        {
            try
            {
                return InputCollector.CollectInput(
                    _args, 
                    oauthToken, 
                    accessToken, 
                    _config!, 
                    _logger!);
            }
            catch (Exception ex) when (IsUnauthorized(ex))
            {
                // This shouldn't normally happen because we check in AuthenticateWithRetriesAsync
                // but if it does, we need to handle it.
                throw new InvalidOperationException("Session expired. Please run again to re-authenticate.");
            }
        }

        private void DisplaySummary(CliInput cliInput)
        {
            Console.WriteLine();
            Console.WriteLine("Project Configuration Summary:");
            Console.WriteLine("==============================");
            Console.WriteLine($"Organization:  {cliInput.OrganizationName}");
            Console.WriteLine($"Project Name:  {cliInput.ProjectName}");
            Console.WriteLine($"Template:      {cliInput.TemplateInfo?.Name ?? cliInput.SelectedTemplate}");
            Console.WriteLine($"Auth Method:   {(cliInput.IsOAuth ? "OAuth" : "PAT")}");
            Console.WriteLine();

            if (cliInput.TemplateInfo?.Message != null)
            {
                var message = System.Text.RegularExpressions.Regex.Replace(cliInput.TemplateInfo.Message, "<.*?>", string.Empty);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ Template Note:");
                Console.WriteLine(message);
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Ready to create project '{cliInput.ProjectName}' in organization '{cliInput.OrganizationName}'");
            Console.ResetColor();
        }

        private bool IsUnauthorized(Exception ex)
        {
            return ex.Message.Contains("Unauthorized") || ex.Message.Contains("401");
        }
    }
}
