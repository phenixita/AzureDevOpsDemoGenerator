using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzdoGenCli.Auth;
using AzdoGenCli.Models;

namespace AzdoGenCli
{
    /// <summary>
    /// Static helper for collecting user input via console prompts
    /// Handles organization selection, project naming, and template selection
    /// </summary>
    public static class InputCollector
    {
        /// <summary>
        /// Prompt for Azure DevOps organization with account list (OAuth) or text input (PAT)
        /// </summary>
        /// <param name="accounts">List of accounts from OAuth (nullable for PAT mode)</param>
        /// <returns>Organization name</returns>
        public static string PromptForOrganization(List<AccountsResponse.Value>? accounts)
        {
            Console.WriteLine();
            Console.WriteLine("Azure DevOps Organization:");
            Console.WriteLine("===========================");

            if (accounts != null && accounts.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Available organizations:");
                Console.WriteLine();

                for (int i = 0; i < accounts.Count; i++)
                {
                    var account = accounts[i];
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"{i + 1}. {account.accountName}");
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   {account.accountUri}");
                    Console.ResetColor();
                }

                Console.WriteLine();

                // Prompt for selection
                while (true)
                {
                    Console.Write($"Select organization (1-{accounts.Count}): ");
                    var input = Console.ReadLine();

                    if (int.TryParse(input, out int selection) && selection >= 1 && selection <= accounts.Count)
                    {
                        var selected = accounts[selection - 1];
                        Console.WriteLine();
                        return selected.accountName ?? string.Empty;
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"✗ Invalid selection. Please enter a number between 1 and {accounts.Count}");
                    Console.ResetColor();
                }
            }
            else
            {
                // PAT mode: manual text entry
                Console.WriteLine();
                Console.WriteLine("Enter your Azure DevOps organization name");
                Console.WriteLine("(e.g., 'myorg' from https://dev.azure.com/myorg)");
                Console.WriteLine();

                while (true)
                {
                    Console.Write("Organization name: ");
                    var org = Console.ReadLine()?.Trim();

                    if (!string.IsNullOrWhiteSpace(org))
                    {
                        Console.WriteLine();
                        return org;
                    }

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Organization name cannot be empty");
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// Prompt for project name with validation
        /// Rules: 1-64 characters, alphanumeric + dash/underscore, no spaces
        /// </summary>
        /// <returns>Valid project name</returns>
        public static string PromptForProjectName()
        {
            Console.WriteLine();
            Console.WriteLine("Project Name:");
            Console.WriteLine("=============");
            Console.WriteLine();
            Console.WriteLine("Enter a name for your new Azure DevOps project");
            Console.WriteLine("Rules: 1-64 characters, alphanumeric, dash (-), underscore (_), no spaces");
            Console.WriteLine();

            while (true)
            {
                Console.Write("Project name: ");
                var projectName = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(projectName))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Project name cannot be empty");
                    Console.ResetColor();
                    continue;
                }

                if (projectName.Length > 64)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Project name cannot exceed 64 characters");
                    Console.ResetColor();
                    continue;
                }

                // Validate characters: alphanumeric, dash, underscore
                if (!Regex.IsMatch(projectName, @"^[a-zA-Z0-9_-]+$"))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("✗ Project name can only contain letters, numbers, dashes (-), and underscores (_)");
                    Console.ResetColor();
                    continue;
                }

                Console.WriteLine();
                return projectName;
            }
        }

        /// <summary>
        /// Orchestrate full input collection flow
        /// Handles both OAuth (with profile/accounts) and PAT modes
        /// </summary>
        /// <param name="args">Parsed CLI arguments</param>
        /// <param name="oauthToken">OAuth access details (nullable for PAT mode)</param>
        /// <param name="pat">Personal Access Token (nullable for OAuth mode)</param>
        /// <param name="configuration">Configuration for API endpoints</param>
        /// <param name="logger">Logger instance</param>
        /// <returns>Fully populated CliInput</returns>
        public static CliInput CollectInput(
            CliArgs args, 
            AccessDetails? oauthToken, 
            string? pat, 
            IConfiguration configuration,
            ILogger logger)
        {
            var input = new CliInput();

            // Determine authentication mode
            if (oauthToken != null)
            {
                input.IsOAuth = true;
                input.AccessToken = oauthToken.access_token ?? string.Empty;
                input.RefreshToken = oauthToken.refresh_token;
            }
            else if (!string.IsNullOrEmpty(pat))
            {
                input.IsOAuth = false;
                input.AccessToken = pat;
            }
            else
            {
                throw new InvalidOperationException("No authentication token available");
            }

            // Load templates
            logger.LogDebug("Loading templates from embedded resources");
            var templates = TemplateDiscovery.LoadTemplates();
            logger.LogInformation("Loaded {TemplateCount} templates", templates.Count);

            // Check for non-interactive mode (all required args provided)
            bool nonInteractive = !string.IsNullOrEmpty(args.Org) && 
                                  !string.IsNullOrEmpty(args.Project) && 
                                  !string.IsNullOrEmpty(args.Template);

            if (nonInteractive)
            {
                logger.LogInformation("Non-interactive mode: using CLI arguments");

                // Organization
                input.OrganizationName = args.Org!;

                // Project
                input.ProjectName = args.Project!;

                // Template
                var template = TemplateDiscovery.FindTemplateByName(templates, args.Template!);
                if (template == null)
                {
                    throw new InvalidOperationException(
                        $"Template '{args.Template}' not found. Use --list-templates to see available templates.");
                }

                input.SelectedTemplate = template.ShortName ?? template.Name;
                input.TemplateInfo = template;

                logger.LogInformation("Selected template: {TemplateName}", template.Name);
            }
            else
            {
                // Interactive mode
                logger.LogInformation("Interactive mode: prompting for input");

                // Organization selection
                if (!string.IsNullOrEmpty(args.Org))
                {
                    input.OrganizationName = args.Org;
                    logger.LogDebug("Using org from CLI: {Org}", args.Org);
                }
                else
                {
                    List<AccountsResponse.Value>? accounts = null;

                    if (input.IsOAuth)
                    {
                        // Fetch profile and accounts for OAuth flow
                        logger.LogDebug("Fetching user profile and accounts");
                        var baseAddress = configuration["LegacyAppSettings:BaseAddress"] ?? "https://app.vssps.visualstudio.com/";
                        
                        var profile = OAuthTokenService.GetProfile(oauthToken!, baseAddress, logger);
                        if (profile != null && !string.IsNullOrEmpty(profile.id))
                        {
                            logger.LogDebug("Profile ID: {ProfileId}", profile.id);
                            var accountList = OAuthTokenService.GetAccounts(profile.id, oauthToken!, baseAddress, logger);
                            accounts = accountList.value?.ToList();
                            logger.LogInformation("Found {AccountCount} accounts", accounts?.Count ?? 0);
                        }
                    }

                    input.OrganizationName = PromptForOrganization(accounts);
                }

                // Project name
                if (!string.IsNullOrEmpty(args.Project))
                {
                    input.ProjectName = args.Project;
                    logger.LogDebug("Using project from CLI: {Project}", args.Project);
                }
                else
                {
                    input.ProjectName = PromptForProjectName();
                }

                // Template selection
                if (!string.IsNullOrEmpty(args.Template))
                {
                    var template = TemplateDiscovery.FindTemplateByName(templates, args.Template);
                    if (template == null)
                    {
                        throw new InvalidOperationException(
                            $"Template '{args.Template}' not found. Use --list-templates to see available templates.");
                    }

                    input.SelectedTemplate = template.ShortName ?? template.Name;
                    input.TemplateInfo = template;
                    logger.LogDebug("Using template from CLI: {Template}", template.Name);
                }
                else
                {
                    var template = TemplateDiscovery.SelectTemplateInteractive(templates);
                    input.SelectedTemplate = template.ShortName ?? template.Name;
                    input.TemplateInfo = template;
                    logger.LogInformation("Selected template: {Template}", template.Name);
                }
            }

            // Set account name for compatibility (used by some VstsRestAPI methods)
            input.AccountName = input.OrganizationName;

            return input;
        }
    }
}
