using System;
using System.CommandLine;
using System.Linq;

namespace GithubWorkflowGenerator.Console.Extensions;

internal static class RootCommandExtensions
{
    public static void AddCommands<T>(this RootCommand rootCommand)
        where T : class
    {
        var assembly = typeof(T).Assembly;
        var commands = assembly.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Command)));

        foreach (var item in commands)
        {
            if (Activator.CreateInstance(item) is Command command)
            {
                rootCommand.AddCommand(command);
            }
        }
    }
}