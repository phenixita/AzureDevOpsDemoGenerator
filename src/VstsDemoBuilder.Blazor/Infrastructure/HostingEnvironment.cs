using System;
using System.IO;

namespace System.Web.Hosting
{
    public static class HostingEnvironment
    {
        private static string _contentRootPath;

        public static void Initialize(string contentRootPath)
        {
            if (string.IsNullOrWhiteSpace(contentRootPath))
            {
                throw new ArgumentException("Content root path is required.", nameof(contentRootPath));
            }

            _contentRootPath = contentRootPath;
        }

        public static string MapPath(string virtualPath)
        {
            if (string.IsNullOrWhiteSpace(_contentRootPath))
            {
                throw new InvalidOperationException("HostingEnvironment has not been initialized.");
            }

            if (string.IsNullOrEmpty(virtualPath) || virtualPath == "~")
            {
                return _contentRootPath;
            }

            var trimmed = virtualPath;
            if (trimmed.StartsWith("~", StringComparison.Ordinal))
            {
                trimmed = trimmed.TrimStart('~');
            }

            trimmed = trimmed.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            trimmed = trimmed.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

            return Path.Combine(_contentRootPath, trimmed);
        }
    }
}
