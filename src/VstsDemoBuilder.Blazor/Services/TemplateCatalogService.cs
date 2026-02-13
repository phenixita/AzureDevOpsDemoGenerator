using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Services;

public sealed class TemplateCatalogService : ITemplateCatalogService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<TemplateCatalogService> _logger;

    public TemplateCatalogService(IWebHostEnvironment environment, ILogger<TemplateCatalogService> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<List<TemplateCatalogItem>> GetAllTemplatesAsync()
    {
        var templateSettingPath = Path.Combine(_environment.ContentRootPath, "Templates", "TemplateSetting.json");
        if (!File.Exists(templateSettingPath))
        {
            _logger.LogWarning("TemplateSetting.json not found at {Path}", templateSettingPath);
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(templateSettingPath).ConfigureAwait(false);
            var templateSetting = JsonSerializer.Deserialize<TemplateSettingRoot>(json, JsonOptions);
            if (templateSetting?.GroupwiseTemplates == null)
            {
                return [];
            }

            return templateSetting.GroupwiseTemplates
                .SelectMany(group => group.Template ?? [])
                .Select(MapTemplate)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load template catalog from {Path}", templateSettingPath);
            return [];
        }
    }

    public async Task<List<TemplateParameterDefinition>> GetTemplateParametersAsync(string templateFolder)
    {
        if (string.IsNullOrWhiteSpace(templateFolder))
        {
            return [];
        }

        var projectTemplatePath = Path.Combine(_environment.ContentRootPath, "Templates", templateFolder, "ProjectTemplate.json");
        if (!File.Exists(projectTemplatePath))
        {
            _logger.LogWarning("ProjectTemplate.json not found for template {TemplateFolder} at {Path}", templateFolder, projectTemplatePath);
            return [];
        }

        try
        {
            var json = await File.ReadAllTextAsync(projectTemplatePath).ConfigureAwait(false);
            var projectTemplate = JsonSerializer.Deserialize<ProjectTemplateRoot>(json, JsonOptions);
            if (projectTemplate?.Parameters == null || projectTemplate.Parameters.Count == 0)
            {
                return [];
            }

            return projectTemplate.Parameters
                .Where(parameter => parameter != null)
                .Select(parameter => new TemplateParameterDefinition
                {
                    Label = parameter.Label ?? string.Empty,
                    FieldName = parameter.FieldName ?? string.Empty
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load template parameters for {TemplateFolder}", templateFolder);
            return [];
        }
    }

    private static TemplateCatalogItem MapTemplate(TemplateSettingTemplate template)
    {
        var imageValue = string.IsNullOrWhiteSpace(template.Icon) ? template.Image : template.Icon;

        return new TemplateCatalogItem
        {
            Name = template.Name ?? string.Empty,
            TemplateFolder = template.TemplateFolder ?? string.Empty,
            Description = template.Description ?? string.Empty,
            Tags = template.Tags ?? [],
            ImageUrl = NormalizeImagePath(imageValue)
        };
    }

    private static string? NormalizeImagePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (value.StartsWith("/", StringComparison.OrdinalIgnoreCase) || value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return $"/Templates/TemplateImages/{value}";
    }

    private sealed class TemplateSettingRoot
    {
        [JsonPropertyName("GroupwiseTemplates")]
        public List<TemplateSettingGroup> GroupwiseTemplates { get; set; } = [];
    }

    private sealed class TemplateSettingGroup
    {
        [JsonPropertyName("Template")]
        public List<TemplateSettingTemplate>? Template { get; set; }
    }

    private sealed class TemplateSettingTemplate
    {
        public string? Name { get; set; }

        public string? TemplateFolder { get; set; }

        public string? Description { get; set; }

        public List<string>? Tags { get; set; }

        public string? Icon { get; set; }

        public string? Image { get; set; }
    }

    private sealed class ProjectTemplateRoot
    {
        public List<ProjectTemplateParameter>? Parameters { get; set; }
    }

    private sealed class ProjectTemplateParameter
    {
        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("fieldName")]
        public string? FieldName { get; set; }
    }
}
