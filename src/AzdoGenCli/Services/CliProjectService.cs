using System;
using System.IO;
using AzdoGenCli.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VstsDemoBuilder.Models;
using VstsDemoBuilder.Services;

namespace AzdoGenCli.Services
{
    public class CliProjectService
    {
        private readonly ILogger<CliProjectService> _logger;
        private readonly IConfiguration _configuration;

        public CliProjectService(ILogger<CliProjectService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public string[] CreateProjectEnvironment(CliInput input, string templatesRoot)
        {
            ArgumentNullException.ThrowIfNull(input);

            if (string.IsNullOrWhiteSpace(templatesRoot))
            {
                throw new ArgumentException("templatesRoot cannot be null or empty", nameof(templatesRoot));
            }

            if (!Directory.Exists(templatesRoot))
            {
                throw new DirectoryNotFoundException($"Templates folder not found: {templatesRoot}");
            }

            // Keep MapPath("~/Templates") behavior working in the reused orchestration.
            var contentRoot = Directory.GetParent(templatesRoot)?.FullName;
            if (!string.IsNullOrWhiteSpace(contentRoot))
            {
                VstsDemoBuilder.Infrastructure.AppPath.ContentRootPath = contentRoot;
                AzdoGenCli.Infrastructure.AppPath.ContentRootPath = contentRoot;
            }

            // Map CLI DTO to existing orchestration model.
            var model = new Project
            {
                id = Guid.NewGuid().ToString("N"),
                ProjectName = input.ProjectName,
                accountName = input.OrganizationName,
                accessToken = input.AccessToken,
                SelectedTemplate = input.SelectedTemplate,
                selectedTemplateFolder = input.TemplateInfo?.TemplateFolder,
                IsPrivatePath = false,
                PrivateTemplatePath = string.Empty,
                websiteUrl = _configuration["LegacyAppSettings:URL"] ?? string.Empty,
                Region = string.Empty,
                IsApi = true,
                isExtensionNeeded = false,
                isAgreeTerms = false,
                UserMethod = "Random",
                selectedUsers = string.Empty,
                accountUsersForWi = new System.Collections.Generic.List<string>(),
                Email = string.Empty,
                Name = string.Empty,
                GitHubFork = false,
                GitHubToken = string.Empty,
                Parameters = new System.Collections.Generic.Dictionary<string, string>()
            };

            _logger.LogInformation("Starting provisioning: Project={Project} Org={Org} Template={Template}",
                input.ProjectName,
                input.OrganizationName,
                input.SelectedTemplate);

            var service = new ProjectService();
            var result = service.CreateProjectEnvironment(model);

            var projectId = model.Environment?.ProjectId ?? string.Empty;
            var org = input.OrganizationName;
            var template = input.TemplateInfo?.TemplateFolder ?? input.SelectedTemplate;

            _logger.LogInformation("Provisioning flow completed: ProjectId={ProjectId}, Org={Org}, Template={Template}",
                projectId,
                org,
                template);

            return new[] { projectId, org, template };
        }
    }
}
