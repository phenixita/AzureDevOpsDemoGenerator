using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Services;

public interface ITemplateCatalogService
{
    Task<List<TemplateCatalogGroup>> GetTemplateGroupsAsync();

    Task<List<TemplateCatalogItem>> GetAllTemplatesAsync();

    Task<List<TemplateParameterDefinition>> GetTemplateParametersAsync(string templateFolder);
}
