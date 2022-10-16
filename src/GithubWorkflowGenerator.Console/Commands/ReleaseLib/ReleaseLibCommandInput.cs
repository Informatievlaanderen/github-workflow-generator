using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;

namespace GithubWorkflowGenerator.Console.Commands.ReleaseLib;

public record ReleaseLibCommandInput(string FileName, List<string> NuGetPackages);

public class ReleaseLibCommandInputBinder : BinderBase<ReleaseLibCommandInput?>
{
    private readonly Option<string> _fileName;
    private readonly Option<List<string>> _nugetPackages;

    public ReleaseLibCommandInputBinder(Option<string> fileName, Option<List<string>> nugetPackages)
    {
        _fileName = fileName;
        _nugetPackages = nugetPackages;
    }

    protected override ReleaseLibCommandInput? GetBoundValue(BindingContext bindingContext)
    {
        var fileName = bindingContext.ParseResult.GetValueForOption(_fileName);
        var nugetPackages = bindingContext.ParseResult.GetValueForOption(_nugetPackages);
        if (fileName is null || nugetPackages is null)
        {
            return default;
        }

        return new ReleaseLibCommandInput(fileName, nugetPackages);
    }
}
