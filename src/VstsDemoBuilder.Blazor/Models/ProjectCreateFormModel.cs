using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VstsDemoBuilder.Blazor.Validation;

namespace VstsDemoBuilder.Blazor.Models;

public class ProjectCreateFormModel
{
    [Required(ErrorMessage = "Please select a template")]
    public string? TemplateFolder { get; set; }

    public string? TemplateName { get; set; }

    public string? TemplateDescription { get; set; }

    public string? TemplateImageUrl { get; set; }

    public string? PendingTemplateFolder { get; set; }

    public string? PendingTemplateName { get; set; }

    public string? PendingTemplateDescription { get; set; }

    public string? PendingTemplateImageUrl { get; set; }
    
    [Required(ErrorMessage = "Project name is required")]
    [ProjectName]
    public string? ProjectName { get; set; }
    
    [Required(ErrorMessage = "Please select an organization")]
    public string? Organization { get; set; }
    
    // Dynamic parameters entered by user
    public Dictionary<string, string> Parameters { get; set; } = new();
}
