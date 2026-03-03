using System.Threading.Tasks;

namespace AzdoGenCli;

/// <summary>
/// CLI Entry point
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        var cliArgs = CliArgs.Parse(args);
        var runner = new CliRunner(cliArgs);
        return await runner.RunAsync();
    }
}
