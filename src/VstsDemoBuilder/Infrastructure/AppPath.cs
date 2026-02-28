using System;
using System.IO;

namespace VstsDemoBuilder.Infrastructure
{
    public static class AppPath
    {
        public static string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public static string WebRootPath { get; set; } = AppContext.BaseDirectory;

        public static string MapPath(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(virtualPath) || virtualPath == "~")
            {
                return ContentRootPath;
            }

            var normalized = virtualPath
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);

            if (normalized.StartsWith("~" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                normalized = normalized.Substring(2);
            }

            return Path.Combine(ContentRootPath, normalized.TrimStart(Path.DirectorySeparatorChar));
        }
    }
}
