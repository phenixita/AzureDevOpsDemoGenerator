namespace VstsDemoBuilder.Blazor.Models;

public sealed class TemplateCatalogGroup
{
    public string GroupName { get; set; } = string.Empty;

    public List<TemplateCatalogItem> Templates { get; set; } = [];
}
