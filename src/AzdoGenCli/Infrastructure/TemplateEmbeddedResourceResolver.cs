using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AzdoGenCli.Infrastructure
{
    public static class TemplateEmbeddedResourceResolver
    {
        private const string ResourcePrefix = "AzdoGenCli.Templates.";
        
        /// <summary>
        /// Reads an embedded template file from the Templates folder.
        /// </summary>
        /// <param name="relativePath">Relative path within Templates folder (e.g., "TemplateSetting.json" or "SmartHotel360/ProjectTemplate.json")</param>
        /// <returns>File content as string</returns>
        public static string ReadTemplateFile(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Template path cannot be null or empty", nameof(relativePath));
            }

            var resourceName = GetResourceName(relativePath);
            var assembly = Assembly.GetExecutingAssembly();
            
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Template not found: {relativePath} (Resource: {resourceName})");
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Checks if a template resource exists.
        /// </summary>
        /// <param name="relativePath">Relative path within Templates folder</param>
        /// <returns>True if template exists, false otherwise</returns>
        public static bool TemplateExists(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            var resourceName = GetResourceName(relativePath);
            var assembly = Assembly.GetExecutingAssembly();
            
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                return stream != null;
            }
        }

        /// <summary>
        /// Extracts all embedded templates to a folder on disk.
        /// This enables legacy file-system based provisioning code paths.
        /// </summary>
        /// <param name="destinationRoot">Destination folder where the Templates tree is created</param>
        /// <returns>Absolute path to the extracted Templates folder</returns>
        public static string ExtractAllTemplates(string destinationRoot)
        {
            if (string.IsNullOrWhiteSpace(destinationRoot))
            {
                throw new ArgumentException("Destination root cannot be null or empty", nameof(destinationRoot));
            }

            var templatesRoot = Path.Combine(destinationRoot, "Templates");
            Directory.CreateDirectory(templatesRoot);

            var assembly = Assembly.GetExecutingAssembly();
            var resources = assembly.GetManifestResourceNames();

            foreach (var resourceName in resources)
            {
                if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var relative = resourceName.Substring(ResourcePrefix.Length);
                var outputPath = MapResourceToPath(templatesRoot, relative);
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    continue;
                }

                using var file = File.Create(outputPath);
                stream.CopyTo(file);
            }

            return templatesRoot;
        }

        /// <summary>
        /// Converts a relative path to a manifest resource name.
        /// Example: "Templates/SmartHotel360/ProjectTemplate.json" -> "AzdoGenCli.Templates.SmartHotel360.ProjectTemplate.json"
        /// </summary>
        private static string GetResourceName(string relativePath)
        {
            // Normalize path separators and convert to dot notation
            var normalized = relativePath
                .Replace('/', '.')
                .Replace('\\', '.');
            
            // Remove "Templates." prefix if it's already there
            if (normalized.StartsWith("Templates.", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring("Templates.".Length);
            }
            
            return ResourcePrefix + normalized;
        }

        private static string MapResourceToPath(string templatesRoot, string relativeResourceName)
        {
            // Best-effort mapping from resource format to disk path:
            // SmartHotel360.ProjectTemplate.json -> SmartHotel360/ProjectTemplate.json
            var parts = new List<string>(relativeResourceName.Split('.'));
            if (parts.Count < 2)
            {
                return Path.Combine(templatesRoot, relativeResourceName);
            }

            var extension = parts[^1];
            var fileStem = parts[^2];
            parts.RemoveRange(parts.Count - 2, 2);
            var directoryPath = parts.Count == 0 ? string.Empty : Path.Combine(parts.ToArray());
            var fileName = $"{fileStem}.{extension}";

            return string.IsNullOrEmpty(directoryPath)
                ? Path.Combine(templatesRoot, fileName)
                : Path.Combine(templatesRoot, directoryPath, fileName);
        }
    }
}
