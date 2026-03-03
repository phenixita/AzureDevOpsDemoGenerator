using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using AzdoGenCli.Infrastructure;
using AzdoGenCli.Models;

namespace AzdoGenCli.Services
{
    /// <summary>
    /// Handles template listing and discovery logic
    /// </summary>
    internal class TemplateService
    {
        public void ListTemplates()
        {
            try
            {
                var templateSettingJson = TemplateEmbeddedResourceResolver.ReadTemplateFile("TemplateSetting.json");
                var templateSettings = JObject.Parse(templateSettingJson);
                var groupwiseTemplates = templateSettings["GroupwiseTemplates"] as JArray;

                Console.WriteLine("Available Templates:");
                Console.WriteLine("====================");
                Console.WriteLine();

                if (groupwiseTemplates != null)
                {
                    foreach (var group in groupwiseTemplates)
                    {
                        var groupName = group["Groups"]?.ToString();
                        var templates = group["Template"] as JArray;

                        if (templates != null && templates.Count > 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"{groupName}:");
                            Console.ResetColor();

                            foreach (var template in templates)
                            {
                                var name = template["Name"]?.ToString();
                                var shortName = template["ShortName"]?.ToString();
                                Console.WriteLine($"  • {name} ({shortName})");
                                
                                var tags = template["Tags"] as JArray;
                                if (tags != null && tags.Count > 0)
                                {
                                    var tagStr = string.Join(", ", tags.Select(t => t.ToString()));
                                    Console.ForegroundColor = ConsoleColor.DarkGray;
                                    Console.WriteLine($"    Tags: {tagStr}");
                                    Console.ResetColor();
                                }
                            }
                            Console.WriteLine();
                        }
                    }
                }

                Console.WriteLine("Use --template <ShortName> to specify a template for provisioning");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error listing templates: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}
