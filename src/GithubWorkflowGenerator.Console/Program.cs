using System.CommandLine;
using System.Threading.Tasks;
using GithubWorkflowGenerator.Console.Commands.Build;
using GithubWorkflowGenerator.Console.Extensions;

namespace GithubWorkflowGenerator.Console;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand();
        rootCommand.AddCommands<BuildCommand>();

        return await rootCommand.InvokeAsync(args);
    }
}
