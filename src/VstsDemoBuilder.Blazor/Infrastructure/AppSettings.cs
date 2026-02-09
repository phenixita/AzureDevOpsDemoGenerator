using System;
using Microsoft.Extensions.Configuration;

namespace VstsDemoBuilder.Infrastructure
{
    public static class AppSettings
    {
        private static IConfiguration _configuration;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public static string Get(string key)
        {
            if (_configuration == null)
            {
                throw new InvalidOperationException("AppSettings has not been initialized.");
            }

            return _configuration[key];
        }
    }
}
