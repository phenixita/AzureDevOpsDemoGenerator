using System;
using System.Collections.Generic;

namespace AzdoGenCli.Models
{
    /// <summary>
    /// Template metadata loaded from TemplateSetting.json
    /// Maps to the Template objects in the GroupwiseTemplates array
    /// </summary>
    public class TemplateInfo
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string TemplateFolder { get; set; } = string.Empty;
        public string? ShortName { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public List<string>? Tags { get; set; }
        public string? Author { get; set; }
        public string? Image { get; set; }
        public string? Message { get; set; }
        public bool ForkGitHubRepo { get; set; }
    }

    /// <summary>
    /// Root structure of TemplateSetting.json for deserialization
    /// </summary>
    public class TemplateSettings
    {
        public List<string>? Groups { get; set; }
        public List<string>? PrivateGroups { get; set; }
        public List<GroupTemplates>? GroupwiseTemplates { get; set; }
    }

    /// <summary>
    /// Group with associated templates
    /// </summary>
    public class GroupTemplates
    {
        public string Groups { get; set; } = string.Empty;
        public List<TemplateInfo>? Template { get; set; }
    }
}
