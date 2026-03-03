using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzdoGenCli.Infrastructure;
using AzdoGenCli.Models;

namespace AzdoGenCli.Services
{
    /// <summary>
    /// Orchestrates the project provisioning process and status monitoring
    /// </summary>
    internal class ProjectProvisioner
    {
        private readonly IConfiguration _config;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<ProjectProvisioner> _logger;

        public ProjectProvisioner(IConfiguration config, ILoggerFactory loggerFactory)
        {
            _config = config;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<ProjectProvisioner>();
        }

        public void Provision(CliInput cliInput)
        {
            Console.WriteLine();
            Console.WriteLine("Starting project provisioning...");

            try
            {
                var extractionRoot = Path.Combine(Path.GetTempPath(), "AzdoGenCli", "templates", Guid.NewGuid().ToString("N"));
                var templatesRoot = TemplateEmbeddedResourceResolver.ExtractAllTemplates(extractionRoot);

                var cliProjectService = new CliProjectService(
                    _loggerFactory.CreateLogger<CliProjectService>(),
                    _config);

                var result = cliProjectService.CreateProjectEnvironment(cliInput, templatesRoot);
                
                string projectId = result.Length > 0 ? result[0] : string.Empty;
                string org = result.Length > 1 ? result[1] : cliInput.OrganizationName;
                string modelId = result.Length > 3 ? result[3] : string.Empty;

                if (CheckForErrors(modelId, org, cliInput.ProjectName))
                {
                    throw new InvalidOperationException("Provisioning failed. Check logs for details.");
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Project created successfully: https://dev.azure.com/{org}/{cliInput.ProjectName}");
                Console.ResetColor();
                
                _logger.LogInformation("Provisioning completed successfully for {Project}", cliInput.ProjectName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Project provisioning failed");
                throw;
            }
        }

        private bool CheckForErrors(string modelId, string org, string projectName)
        {
            var statusMessages = VstsDemoBuilder.Services.ProjectService.StatusMessages;
            
            // Check for general errors
            var errorKey = modelId + "_Errors";
            if (statusMessages.ContainsKey(errorKey) && !string.IsNullOrWhiteSpace(statusMessages[errorKey]))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Project provisioning errors:");
                Console.WriteLine(statusMessages[errorKey]);
                Console.ResetColor();
                _logger.LogError("Provisioning errors: {Errors}", statusMessages[errorKey]);
                return true;
            }

            // Check for OAUTHACCESSDENIED
            if (statusMessages.ContainsKey(modelId) && statusMessages[modelId] == "OAUTHACCESSDENIED")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Access denied. The OAuth token does not have sufficient permissions to create projects.");
                Console.WriteLine("  Try using a PAT with full access: --pat <your-pat>");
                Console.ResetColor();
                return true;
            }

            return false;
        }
    }
}
