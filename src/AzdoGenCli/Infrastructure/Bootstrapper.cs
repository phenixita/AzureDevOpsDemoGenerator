using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AzdoGenCli.Infrastructure;
using System;
using System.IO;

namespace AzdoGenCli.Infrastructure
{
    /// <summary>
    /// Handles application initialization, configuration, and logging setup
    /// </summary>
    internal static class Bootstrapper
    {
        public static IConfiguration LoadConfiguration()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            // Bootstrap legacy configuration for VstsRestAPI compatibility
            LegacyConfigBootstrapper.Apply(configuration.GetSection("LegacyAppSettings"));
            
            return configuration;
        }

        public static ILoggerFactory ConfigureLogging(CliArgs args)
        {
            return LoggingSetup.ConfigureLogging(
                verbose: args.Verbose, 
                consoleOutput: args.ConsoleOutput);
        }
    }
}
