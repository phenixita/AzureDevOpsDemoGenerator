using System;
using System.Collections.Generic;
using System.Linq;

namespace AzdoGenCli
{
    /// <summary>
    /// Parsed command-line arguments for AzdoGenCli
    /// </summary>
    public class CliArgs
    {
        public string? Pat { get; set; }
        public string? Org { get; set; }
        public string? Project { get; set; }
        public string? Template { get; set; }
        public bool DeleteProject { get; set; }
        public bool Force { get; set; }
        public bool Verbose { get; set; }
        public bool DryRun { get; set; }
        public bool ConsoleOutput { get; set; }
        public bool ListTemplates { get; set; }
        public bool ShowHelp { get; set; }

        /// <summary>
        /// Parse command-line arguments into CliArgs instance
        /// Supports both --flag and -flag formats
        /// </summary>
        public static CliArgs Parse(string[] args)
        {
            var result = new CliArgs();

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();

                // Handle flags with values
                if ((arg == "--pat" || arg == "-pat") && i + 1 < args.Length)
                {
                    result.Pat = args[++i];
                }
                else if ((arg == "--org" || arg == "-org") && i + 1 < args.Length)
                {
                    result.Org = args[++i];
                }
                else if ((arg == "--project" || arg == "-project") && i + 1 < args.Length)
                {
                    result.Project = args[++i];
                }
                else if ((arg == "--template" || arg == "-template") && i + 1 < args.Length)
                {
                    result.Template = args[++i];
                }
                else if (arg == "--delete-project" || arg == "-delete-project")
                {
                    result.DeleteProject = true;
                }
                else if (arg == "--force" || arg == "-force" || arg == "-f")
                {
                    result.Force = true;
                }
                // Handle boolean flags
                else if (arg == "--verbose" || arg == "-verbose")
                {
                    result.Verbose = true;
                }
                else if (arg == "--dry-run" || arg == "-dry-run")
                {
                    result.DryRun = true;
                }
                else if (arg == "--console" || arg == "-console")
                {
                    result.ConsoleOutput = true;
                }
                else if (arg == "--list-templates" || arg == "-list-templates")
                {
                    result.ListTemplates = true;
                }
                else if (arg == "--help" || arg == "-help" || arg == "-h")
                {
                    result.ShowHelp = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Print usage information
        /// </summary>
        public static void PrintUsage()
        {
            Console.WriteLine(@"AzdoGenCli - Azure DevOps Demo Project Generator (CLI Edition)

USAGE:
    AzdoGenCli [OPTIONS]

OPTIONS:
    --pat <token>           Personal Access Token for Azure DevOps authentication
    --org <organization>    Azure DevOps organization name (e.g., 'myorg')
    --project <name>        Target project name to create
    --template <name>       Template name to apply (use --list-templates to see available)
    --delete-project        Delete the specified project (requires --org and --project)
    --force, -f             Bypass confirmation prompt when deleting
    
    --list-templates        List all available embedded templates and exit
    --verbose               Enable detailed debug logging
    --console               Output logs to console (in addition to file)
    --dry-run               Show what would be done without making changes
    --help, -h              Show this help message

EXAMPLES:
    AzdoGenCli --org myorg --project ""My New Project"" --template ""PartsUnlimited""
    AzdoGenCli --delete-project --org myorg --project ""Old Project"" --force

AUTHENTICATION:
    AzdoGenCli uses OAuth browser authentication by default. Alternatively:
    - Use --pat flag to provide a Personal Access Token directly
    - Set AZURE_DEVOPS_PAT environment variable

EXAMPLES:
    # Interactive OAuth flow
    AzdoGenCli

    # Use PAT authentication
    AzdoGenCli --pat xxxxxxxxxxxxxxxxxxxxxxxx

    # List available templates
    AzdoGenCli --list-templates

    # Verbose output with console logging
    AzdoGenCli --verbose --console

    # Full provisioning with all flags
    AzdoGenCli --org myorg --project MyDemo --template SmartHotel360 --verbose
");
        }
    }
}
