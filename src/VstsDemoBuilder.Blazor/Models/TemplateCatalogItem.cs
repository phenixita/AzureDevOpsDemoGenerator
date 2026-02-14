using System.Collections.Generic;

namespace VstsDemoBuilder.Blazor.Models;

public sealed class TemplateCatalogItem
{
    public string Name { get; set; } = string.Empty;

    public string TemplateFolder { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = [];

    public string MessageHtml { get; set; } = string.Empty;

    public List<string> PreviewImages { get; set; } = [];

    public string? ImageUrl { get; set; }
}
