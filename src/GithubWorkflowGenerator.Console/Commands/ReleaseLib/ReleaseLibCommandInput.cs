using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;

namespace GithubWorkflowGenerator.Console.Commands.ReleaseLib;

public record ReleaseLibCommandInput(string FileName, List<string> NuGetPackages, string JiraPrefix, string JiraProject);

public class ReleaseLibCommandInputBinder : BinderBase<ReleaseLibCommandInput?>
{
    private readonly Option<string> _fileName;
    private readonly Option<List<string>> _nugetPackages;
    private readonly Option<string> _jiraPrefix;
    private readonly Option<string> _jiraProject;

    public ReleaseLibCommandInputBinder(Option<string> fileName, Option<List<string>> nugetPackages, Option<string> jiraPrefix, Option<string> jiraProject)
    {
        _fileName = fileName;
        _nugetPackages = nugetPackages;
        _jiraPrefix = jiraPrefix;
        _jiraProject = jiraProject;
    }

    protected override ReleaseLibCommandInput? GetBoundValue(BindingContext bindingContext)
    {
        var fileName = bindingContext.ParseResult.GetValueForOption(_fileName);
        var nugetPackages = bindingContext.ParseResult.GetValueForOption(_nugetPackages);
        var jiraPrefix = bindingContext.ParseResult.GetValueForOption(_jiraPrefix);
        var jiraProject = bindingContext.ParseResult.GetValueForOption(_jiraProject);
        if (fileName is null || nugetPackages is null || jiraPrefix is null || jiraProject is null)
        {
            return default;
        }

        return new ReleaseLibCommandInput(fileName, nugetPackages, jiraPrefix, jiraProject);
    }
}
