using System.CommandLine;
using System.CommandLine.Binding;

namespace GithubWorkflowGenerator.Console.Commands.Build;

public record BuildCommandInput(string FileName, string SolutionName, string SonarKey, bool OnPullRequests);

public class BuildCommandInputBinder : BinderBase<BuildCommandInput?>
{
    private readonly Option<string> _fileName;
    private readonly Option<string> _solutionName;
    private readonly Option<string> _sonarKey;
    private readonly Option<bool> _onPullRequests;

    public BuildCommandInputBinder(Option<string> fileName, Option<string> solutionName, Option<string> sonarKey, Option<bool> onPullRequests)
    {
        _fileName = fileName;
        _solutionName = solutionName;
        _sonarKey = sonarKey;
        _onPullRequests = onPullRequests;
    }

    protected override BuildCommandInput? GetBoundValue(BindingContext bindingContext)
    {
        var fileName = bindingContext.ParseResult.GetValueForOption(_fileName);
        var solutionName = bindingContext.ParseResult.GetValueForOption(_solutionName);
        var sonarKey = bindingContext.ParseResult.GetValueForOption(_sonarKey);
        var onPullRequests = bindingContext.ParseResult.GetValueForOption(_onPullRequests);
        if (fileName is null || solutionName is null || sonarKey is null)
        {
            return default;
        }

        return new BuildCommandInput(fileName, solutionName, sonarKey, onPullRequests);
    }
}
