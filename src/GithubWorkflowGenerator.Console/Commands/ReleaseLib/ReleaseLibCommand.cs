using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Threading.Tasks;
using GithubWorkflowGenerator.Core;
using GithubWorkflowGenerator.Core.Options;

namespace GithubWorkflowGenerator.Console.Commands.ReleaseLib;

internal class ReleaseLibCommand : Command
{
    private const string CommandName = "releaselib";
    private const string CommandDescription = "generate a release.yml file for a library";

    public ReleaseLibCommand()
        : this(CommandName, CommandDescription)
    {
        var releaseFileName = new Option<string>(new[] { "--fileName" }, () => "release.yml", "Output file name.");
        var workflowName = new Option<string>(new[] { "--workflowName" }, () => "Release", "Workflow name");
        var nugetPackages = new Option<List<string>>(new[] { "--nugetPackages" }, "Nuget packages (space separated)") { AllowMultipleArgumentsPerToken = true };
        var jiraPrefix = new Option<string>(new[] { "--jiraPrefix" }, "Prefix for JIRA project");
        var jiraProject = new Option<string>(new[] { "--jiraProject" }, () => "GAWR", "JIRA project");

        AddOption(releaseFileName);
        AddOption(workflowName);
        AddOption(nugetPackages);
        AddOption(jiraPrefix);
        AddOption(jiraProject);
        this.SetHandler(async context => await Handle(context!.FileName, context.WorkflowName, context.NuGetPackages, context.JiraPrefix, context.JiraProject),
            new ReleaseLibCommandInputBinder(releaseFileName, workflowName, nugetPackages, jiraPrefix, jiraProject));
    }

    public ReleaseLibCommand(string name, string? description = null)
        : base(name, description)
    { }
        
    private static async Task Handle(string fileName, string workflowName, IEnumerable<string> nugetPackages, string jiraPrefix, string jiraProject)
    {
        var generator = new GithubGenerator();
        var options = new ReleaseLibGeneratorOptions(workflowName, nugetPackages, jiraPrefix, jiraProject);
        string result = await generator.GenerateReleaseLibWorkflowAsync(options);
        await File.WriteAllTextAsync(fileName, result);

        IConsole console = new SystemConsole();
        console.WriteLine($"{fileName} was successfully generated.");
    }
}