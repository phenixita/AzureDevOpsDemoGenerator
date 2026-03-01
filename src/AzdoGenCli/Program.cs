using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzdoGenCli.Infrastructure;
using AzdoGenCli.Auth;
using AzdoGenCli.Models;
using AzdoGenCli.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace AzdoGenCli;

class Program
{
    static async System.Threading.Tasks.Task<int> Main(string[] args)
    {
        try
        {
            // Parse command-line arguments
            var cliArgs = CliArgs.Parse(args);

            // Handle --help flag
            if (cliArgs.ShowHelp)
            {
                CliArgs.PrintUsage();
                return 0;
            }

            // Handle --list-templates flag
            if (cliArgs.ListTemplates)
            {
                ListTemplates();
                return 0;
            }

            // Load configuration from appsettings.json and environment variables
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            // Bootstrap legacy configuration for VstsRestAPI compatibility
            LegacyConfigBootstrapper.Apply(configuration.GetSection("LegacyAppSettings"));

            // Configure logging based on CLI flags
            var loggerFactory = LoggingSetup.ConfigureLogging(
                verbose: cliArgs.Verbose, 
                consoleOutput: cliArgs.ConsoleOutput);
            var logger = loggerFactory.CreateLogger<Program>();
            
            logger.LogInformation("AzdoGenCli v1.0 starting");
            logger.LogDebug("Arguments: Verbose={Verbose}, DryRun={DryRun}, ConsoleOutput={ConsoleOutput}", 
                cliArgs.Verbose, cliArgs.DryRun, cliArgs.ConsoleOutput);

            // Print version info
            Console.WriteLine("AzdoGenCli v1.0 - Azure DevOps Demo Project Generator");
            Console.WriteLine();

            // Authentication flow
            string accessToken = null;
            AccessDetails oauthToken = null;

            if (!string.IsNullOrEmpty(cliArgs.Pat))
            {
                // Use PAT from command line
                accessToken = cliArgs.Pat;
                logger.LogInformation("Using PAT from --pat argument");
                Console.WriteLine("✓ Using Personal Access Token from command line");
            }
            else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT")))
            {
                // Use PAT from environment variable
                accessToken = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                logger.LogInformation("Using PAT from AZURE_DEVOPS_PAT environment variable");
                Console.WriteLine("✓ Using Personal Access Token from environment variable");
            }
            else
            {
                // Use OAuth browser authentication
                logger.LogInformation("Starting OAuth browser authentication flow");
                Console.WriteLine("Starting OAuth authentication...");
                Console.WriteLine();
                
                oauthToken = await BrowserAuthenticator.AuthenticateAsync(configuration, logger);
                accessToken = oauthToken.access_token;
                
                Console.WriteLine();
                Console.WriteLine("✓ Authentication successful!");
                logger.LogInformation("OAuth authentication completed successfully");
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Authentication failed: No access token obtained");
                Console.ResetColor();
                logger.LogError("Authentication failed: No access token obtained");
                return 1;
            }

            logger.LogDebug("Access token obtained (length: {TokenLength})", accessToken.Length);
            
            // Phase 4: Input Collection & Template Discovery
            logger.LogInformation("Starting input collection phase");
            
            CliInput cliInput;
            try
            {
                cliInput = InputCollector.CollectInput(
                    cliArgs, 
                    oauthToken, 
                    accessToken, 
                    configuration, 
                    logger);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Input collection failed: {ex.Message}");
                Console.ResetColor();
                logger.LogError(ex, "Input collection failed");
                return 1;
            }

            // Display summary
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
                // Show template-specific message (strip HTML)
                var message = System.Text.RegularExpressions.Regex.Replace(
                    cliInput.TemplateInfo.Message, 
                    "<.*?>", 
                    string.Empty);
                
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("⚠ Template Note:");
                Console.WriteLine(message);
                Console.ResetColor();
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Ready to create project '{cliInput.ProjectName}' in organization '{cliInput.OrganizationName}' with template '{cliInput.SelectedTemplate}'");
            Console.ResetColor();
            
            logger.LogInformation("Input collection complete. Project: {Project}, Org: {Org}, Template: {Template}", 
                cliInput.ProjectName, cliInput.OrganizationName, cliInput.SelectedTemplate);

            if (cliArgs.DryRun)
            {
                Console.WriteLine();
                Console.WriteLine("🔄 Dry-run mode: No changes will be made");
                logger.LogInformation("Dry-run mode - exiting without provisioning");
                return 0;
            }

            Console.WriteLine();
            Console.WriteLine("Starting project provisioning...");

            try
            {
                var extractionRoot = Path.Combine(Path.GetTempPath(), "AzdoGenCli", "templates", Guid.NewGuid().ToString("N"));
                var templatesRoot = TemplateEmbeddedResourceResolver.ExtractAllTemplates(extractionRoot);

                var cliProjectService = new CliProjectService(
                    loggerFactory.CreateLogger<CliProjectService>(),
                    configuration);

                var result = cliProjectService.CreateProjectEnvironment(cliInput, templatesRoot);
                var projectId = result.Length > 0 ? result[0] : string.Empty;
                var org = result.Length > 1 ? result[1] : cliInput.OrganizationName;

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Project created successfully: https://dev.azure.com/{org}/{cliInput.ProjectName}");
                Console.ResetColor();
                logger.LogInformation("Provisioning completed. ProjectId={ProjectId}, Org={Org}, Project={Project}", projectId, org, cliInput.ProjectName);

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Project provisioning failed: {ex.Message}");
                Console.ResetColor();
                logger.LogError(ex, "Project provisioning failed");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Error: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Use --help for usage information");
            return 1;
        }
    }

    /// <summary>
    /// List all available templates from TemplateSetting.json
    /// </summary>
    private static void ListTemplates()
    {
        try
        {
            var templateSettingJson = TemplateEmbeddedResourceResolver.ReadTemplateFile("TemplateSetting.json");
            var templateSettings = JObject.Parse(templateSettingJson);
            var groupwiseTemplates = templateSettings["GroupwiseTemplates"] as JArray;

            Console.WriteLine("Available Templates:");
            Console.WriteLine("====================");
            Console.WriteLine();

            if (groupwiseTemplates != null)
            {
                foreach (var group in groupwiseTemplates)
                {
                    var groupName = group["Groups"]?.ToString();
                    var templates = group["Template"] as JArray;

                    if (templates != null && templates.Count > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"{groupName}:");
                        Console.ResetColor();

                        foreach (var template in templates)
                        {
                            var name = template["Name"]?.ToString();
                            var shortName = template["ShortName"]?.ToString();
                            var description = template["Description"]?.ToString();
                            var tags = template["Tags"] as JArray;

                            Console.WriteLine($"  • {name} ({shortName})");
                            
                            if (tags != null && tags.Count > 0)
                            {
                                var tagStr = string.Join(", ", tags.Select(t => t.ToString()));
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine($"    Tags: {tagStr}");
                                Console.ResetColor();
                            }
                        }
                        Console.WriteLine();
                    }
                }
            }

            Console.WriteLine("Use --template <ShortName> to specify a template for provisioning");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error listing templates: {ex.Message}");
            Console.ResetColor();
        }
    }
}
