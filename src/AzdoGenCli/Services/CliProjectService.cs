using System;
using System.IO;
using AzdoGenCli.Infrastructure;
using AzdoGenCli.Models;
using AzdoGenCli.Services.Exceptions;
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

        public DeleteProjectResult DeleteProject(DeleteProjectRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation("Starting project deletion: Project={Project} Org={Org}",
                request.ProjectName, request.OrganizationName);

            // Build VstsRestAPI configuration
            var defaultHost = _configuration["LegacyAppSettings:DefaultHost"] ?? "https://dev.azure.com/";
            var apiVersion = _configuration["LegacyAppSettings:ProjectCreationVersion"] ?? "5.0";

            var config = new VstsRestAPI.Configuration
            {
                UriString = $"{defaultHost}{request.OrganizationName}/",
                AccountName = request.OrganizationName,
                PersonalAccessToken = request.AccessToken,
                VersionNumber = apiVersion
            };

            var projectsApi = new VstsRestAPI.ProjectsAndTeams.Projects(config);

            // Get project ID by name
            string projectId;
            try
            {
                projectId = projectsApi.GetProjectIdByName(request.ProjectName);

                // Check for error values returned by GetProjectIdByName
                if (string.IsNullOrEmpty(projectId) ||
                    projectId == Guid.Empty.ToString() ||
                    projectId == "-1")
                {
                    throw new ProjectNotFoundException(request.ProjectName);
                }

                _logger.LogDebug("Found project ID: {ProjectId}", projectId);
            }
            catch (ProjectNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to lookup project {Project}", request.ProjectName);
                throw new ProjectNotFoundException(request.ProjectName);
            }

            // Delete project
            var response = projectsApi.DeleteProject(projectId);

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new AuthenticationFailedException("Authentication failed");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new AuthorizationFailedException("Failed to delete project: Unauthorized / Insufficient permissions.");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new ProjectNotFoundException(request.ProjectName);
            }

            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                var errorContent = response.Content.ReadAsStringAsync().Result;
                _logger.LogError("Delete failed with status {Status}: {Error}",
                    response.StatusCode, errorContent);
                throw new ProjectDeletionFailedException(
                    response.StatusCode,
                    $"Failed with status {response.StatusCode}");
            }

            // Success - 202 Accepted
            _logger.LogInformation("Project {Project} deletion accepted (202)", request.ProjectName);

            return new DeleteProjectResult(
                OrganizationName: request.OrganizationName,
                ProjectName: request.ProjectName,
                ProjectId: projectId,
                DeletionAccepted: true,
                OperationId: null,
                OperationUrl: null);
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
                SelectedTemplate = input.TemplateInfo?.TemplateFolder ?? input.SelectedTemplate,
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

            // Log any status messages from the provisioning engine
            foreach (var kvp in ProjectService.StatusMessages)
            {
                _logger.LogInformation("StatusMessage[{Key}] = {Value}", kvp.Key, kvp.Value);
            }

            var projectId = model.Environment?.ProjectId ?? string.Empty;
            var org = input.OrganizationName;
            var template = input.TemplateInfo?.TemplateFolder ?? input.SelectedTemplate;

            _logger.LogInformation("Provisioning flow completed: ProjectId={ProjectId}, Org={Org}, Template={Template}",
                projectId,
                org,
                template);

            return new[] { projectId, org, template, model.id };
        }
    }
}
