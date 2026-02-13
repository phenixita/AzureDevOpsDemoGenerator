using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VstsDemoBuilder.Blazor.Models;
using VstsDemoBuilder.Models;
using VstsDemoBuilder.ServiceInterfaces;

namespace VstsDemoBuilder.Blazor.Services;

public sealed class ProvisioningService : IProvisioningService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ProvisioningService> _logger;

    public ProvisioningService(IServiceScopeFactory serviceScopeFactory, ILogger<ProvisioningService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public Task<string> StartProjectProvisioningAsync(ProjectCreateFormModel formModel, string userId, string email, string accessToken)
    {
        var trackingId = Guid.NewGuid().ToString("N")[..8];
        var project = new Project
        {
            id = trackingId,
            SelectedTemplate = formModel.TemplateFolder ?? string.Empty,
            ProjectName = formModel.ProjectName ?? string.Empty,
            accountName = formModel.Organization ?? string.Empty,
            accessToken = accessToken,
            Email = email,
            Parameters = formModel.Parameters
        };

        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
            projectService.AddMessage(trackingId, string.Empty);
        }

        _ = Task.Run(async () =>
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();

            try
            {
                await Task.Run(() => projectService.CreateProjectEnvironment(project));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provisioning failed for {TrackingId}", trackingId);
            }
        });

        return Task.FromResult(trackingId);
    }
}
