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

    public async Task<List<TemplateCatalogGroup>> GetTemplateGroupsAsync()
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

            return MapTemplateGroups(templateSetting);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load grouped template catalog from {Path}", templateSettingPath);
            return [];
        }
    }

    public async Task<List<TemplateCatalogItem>> GetAllTemplatesAsync()
    {
        var groups = await GetTemplateGroupsAsync().ConfigureAwait(false);
        return groups.SelectMany(group => group.Templates).ToList();
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
            MessageHtml = template.Message ?? string.Empty,
            PreviewImages = NormalizeImagePaths(template.PreviewImages),
            ImageUrl = NormalizeImagePath(imageValue)
        };
    }

    private static List<TemplateCatalogGroup> MapTemplateGroups(TemplateSettingRoot templateSetting)
    {
        var orderedGroupNames = (templateSetting.Groups ?? [])
            .Concat(templateSetting.PrivateGroups ?? [])
            .Where(groupName => !string.IsNullOrWhiteSpace(groupName))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<TemplateCatalogGroup>();
        var existingGroups = templateSetting.GroupwiseTemplates ?? [];
        var processedGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupName in orderedGroupNames)
        {
            var group = existingGroups.FirstOrDefault(candidate => string.Equals(candidate.Groups, groupName, StringComparison.OrdinalIgnoreCase));
            if (group == null)
            {
                continue;
            }

            var templates = (group.Template ?? [])
                .Where(ShouldIncludeTemplate)
                .Select(MapTemplate)
                .ToList();

            if (templates.Count == 0)
            {
                continue;
            }

            result.Add(new TemplateCatalogGroup
            {
                GroupName = groupName,
                Templates = templates
            });
            processedGroupNames.Add(groupName);
        }

        foreach (var group in existingGroups)
        {
            if (string.IsNullOrWhiteSpace(group.Groups) || processedGroupNames.Contains(group.Groups))
            {
                continue;
            }

            var templates = (group.Template ?? [])
                .Where(ShouldIncludeTemplate)
                .Select(MapTemplate)
                .ToList();

            if (templates.Count == 0)
            {
                continue;
            }

            result.Add(new TemplateCatalogGroup
            {
                GroupName = group.Groups,
                Templates = templates
            });
        }

        return result;
    }

    private static bool ShouldIncludeTemplate(TemplateSettingTemplate template)
    {
        return !string.IsNullOrWhiteSpace(template.TemplateFolder) &&
               !template.TemplateFolder.Contains("<iframe", StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> NormalizeImagePaths(List<string>? values)
    {
        return (values ?? [])
            .Select(NormalizeImagePath)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToList();
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
        [JsonPropertyName("Groups")]
        public List<string>? Groups { get; set; }

        [JsonPropertyName("PrivateGroups")]
        public List<string>? PrivateGroups { get; set; }

        [JsonPropertyName("GroupwiseTemplates")]
        public List<TemplateSettingGroup> GroupwiseTemplates { get; set; } = [];
    }

    private sealed class TemplateSettingGroup
    {
        [JsonPropertyName("Groups")]
        public string Groups { get; set; } = string.Empty;

        [JsonPropertyName("Template")]
        public List<TemplateSettingTemplate>? Template { get; set; }
    }

    private sealed class TemplateSettingTemplate
    {
        public string? Name { get; set; }

        public string? TemplateFolder { get; set; }

        public string? Description { get; set; }

        public List<string>? Tags { get; set; }

        public string? Message { get; set; }

        public List<string>? PreviewImages { get; set; }

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
