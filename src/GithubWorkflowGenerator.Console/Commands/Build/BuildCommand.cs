using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using GithubWorkflowGenerator.Core;

namespace GithubWorkflowGenerator.Console.Commands.Build;

internal class BuildCommand : Command
{
    private const string CommandName = "build";
    private const string CommandDescription = "generate a build.yml file";

    public BuildCommand()
        : this(CommandName, CommandDescription)
    {
        var buildFileName = new Option<string>(new[] { "--fileName" }, () => "build.yml", "Output file name.");
        var solutionName = new Option<string>(new[] { "--solutionName" }, "Solution name (ends with \"\".sln\"\")\".");
        var sonarKey = new Option<string>(new[] { "--sonarKey" }, "Sonar project key.");
        AddOption(buildFileName);
        AddOption(solutionName);
        AddOption(sonarKey);
        this.SetHandler(Handle, buildFileName, solutionName, sonarKey);
    }
        
    public BuildCommand(string name, string? description = null)
        : base(name, description)
    { }

    private static async Task Handle(string fileName, string solutionName, string sonarKey)
    {
        var generator = new GithubGenerator();
        var options = new BuildGeneratorOptions(solutionName, sonarKey);
        var result = await generator.GenerateBuildWorkflowAsync(options);
        await File.WriteAllTextAsync(fileName, result);
    }
}