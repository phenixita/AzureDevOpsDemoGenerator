using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using AzdoGenCli.Infrastructure;
using AzdoGenCli.Models;

namespace AzdoGenCli
{
    /// <summary>
    /// Static helper for loading and selecting templates from embedded TemplateSetting.json
    /// </summary>
    public static class TemplateDiscovery
    {
        /// <summary>
        /// Load all templates from embedded TemplateSetting.json
        /// </summary>
        /// <returns>Flat list of all templates across all groups</returns>
        public static List<TemplateInfo> LoadTemplates()
        {
            var templateSettingJson = TemplateEmbeddedResourceResolver.ReadTemplateFile("TemplateSetting.json");
            var templateSettings = JsonConvert.DeserializeObject<TemplateSettings>(templateSettingJson);

            if (templateSettings?.GroupwiseTemplates == null)
            {
                return new List<TemplateInfo>();
            }

            var allTemplates = new List<TemplateInfo>();

            foreach (var group in templateSettings.GroupwiseTemplates)
            {
                if (group.Template != null)
                {
                    // Assign category from group name
                    foreach (var template in group.Template)
                    {
                        template.Category = group.Groups;
                        allTemplates.Add(template);
                    }
                }
            }

            return allTemplates;
        }

        /// <summary>
        /// Interactive template selection with numbered menu
        /// </summary>
        /// <param name="templates">List of available templates</param>
        /// <returns>Selected template</returns>
        public static TemplateInfo SelectTemplateInteractive(List<TemplateInfo> templates)
        {
            if (templates == null || templates.Count == 0)
            {
                throw new InvalidOperationException("No templates available");
            }

            Console.WriteLine();
            Console.WriteLine("Available Templates:");
            Console.WriteLine("====================");
            Console.WriteLine();

            for (int i = 0; i < templates.Count; i++)
            {
                var template = templates[i];
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{i + 1}. {template.Name}");
                Console.ResetColor();

                if (!string.IsNullOrEmpty(template.Description))
                {
                    // Strip HTML tags for console display
                    var description = System.Text.RegularExpressions.Regex.Replace(
                        template.Description, 
                        "<.*?>", 
                        string.Empty);
                    
                    // Truncate long descriptions
                    if (description.Length > 100)
                    {
                        description = description.Substring(0, 100) + "...";
                    }
                    
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   {description}");
                    Console.ResetColor();
                }

                if (template.Tags != null && template.Tags.Count > 0)
                {
                    var tagStr = string.Join(", ", template.Tags);
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   Tags: {tagStr}");
                    Console.ResetColor();
                }

                if (!string.IsNullOrEmpty(template.Category))
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   Category: {template.Category}");
                    Console.ResetColor();
                }

                Console.WriteLine();
            }

            // Prompt for selection with validation
            while (true)
            {
                Console.Write($"Select template (1-{templates.Count}): ");
                var input = Console.ReadLine();

                if (int.TryParse(input, out int selection) && selection >= 1 && selection <= templates.Count)
                {
                    Console.WriteLine();
                    return templates[selection - 1];
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ Invalid selection. Please enter a number between 1 and {templates.Count}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Find template by name or short name (case-insensitive)
        /// </summary>
        /// <param name="templates">List of templates to search</param>
        /// <param name="name">Template name, short name, or template folder</param>
        /// <returns>Matching template or null</returns>
        public static TemplateInfo? FindTemplateByName(List<TemplateInfo> templates, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var normalized = name.Trim();

            // Try exact match on ShortName first
            var template = templates.FirstOrDefault(t => 
                string.Equals(t.ShortName, normalized, StringComparison.OrdinalIgnoreCase));

            if (template != null)
            {
                return template;
            }

            // Try Name field
            template = templates.FirstOrDefault(t => 
                string.Equals(t.Name, normalized, StringComparison.OrdinalIgnoreCase));

            if (template != null)
            {
                return template;
            }

            // Try TemplateFolder
            template = templates.FirstOrDefault(t => 
                string.Equals(t.TemplateFolder, normalized, StringComparison.OrdinalIgnoreCase));

            return template;
        }
    }
}
