using VstsDemoBuilder.Blazor.Models;

namespace VstsDemoBuilder.Blazor.Services;

public interface ITemplateCatalogService
{
    Task<List<TemplateCatalogItem>> GetAllTemplatesAsync();

    Task<List<TemplateParameterDefinition>> GetTemplateParametersAsync(string templateFolder);
}
