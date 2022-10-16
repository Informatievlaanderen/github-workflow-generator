using System.Collections.Generic;
using System.CommandLine;
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
        var nugetPackages = new Option<List<string>>(new[] { "--nugetPackages" }, "Nuget packages (space separated)") { AllowMultipleArgumentsPerToken = true };

        AddOption(releaseFileName);
        AddOption(nugetPackages);
        this.SetHandler(async context => await Handle(context!.FileName, context.NuGetPackages), new ReleaseLibCommandInputBinder(releaseFileName, nugetPackages));
    }

    public ReleaseLibCommand(string name, string? description = null)
        : base(name, description)
    { }
        
    private static async Task Handle(string fileName, IEnumerable<string> nugetPackages)
    {
        var generator = new GithubGenerator();
        var options = new ReleaseLibGeneratorOptions(nugetPackages);
        string result = await generator.GenerateReleaseLibWorkflowAsync(options);
        await File.WriteAllTextAsync(fileName, result);
    }
}