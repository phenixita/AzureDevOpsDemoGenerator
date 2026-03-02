using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using System;
using System.IO;

namespace AzdoGenCli.Infrastructure
{
    public static class LoggingSetup
    {
        /// <summary>
        /// Configures Serilog logging with file sink and optional console output.
        /// </summary>
        /// <param name="verbose">If true, sets log level to Debug; otherwise Warning</param>
        /// <param name="consoleOutput">If true, adds console sink</param>
        /// <returns>Configured ILoggerFactory</returns>
        public static ILoggerFactory ConfigureLogging(bool verbose, bool consoleOutput)
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".azdo-gen",
                "logs");

            // Ensure log directory exists
            Directory.CreateDirectory(logDirectory);

            var logFilePath = Path.Combine(logDirectory, "azdo-gen-.log");

            // Set minimum log level based on verbose flag
            var minimumLevel = verbose 
                ? Serilog.Events.LogEventLevel.Debug 
                : Serilog.Events.LogEventLevel.Warning;

            // Configure Serilog
            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: Serilog.RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

            if (consoleOutput)
            {
                loggerConfig.WriteTo.Console(
                    outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}");
            }

            Log.Logger = loggerConfig.CreateLogger();

            // Create LoggerFactory with Serilog provider
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new SerilogLoggerProvider(Log.Logger, dispose: true));

            return loggerFactory;
        }
    }
}
