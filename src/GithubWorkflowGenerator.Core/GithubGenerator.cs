using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GithubWorkflowGenerator.Core.Options;
using Stubble.Core.Builders;

namespace GithubWorkflowGenerator.Core;

public class GithubGenerator
{
    public async Task<string> GenerateBuildWorkflowAsync(BuildGeneratorOptions options)
    {
        var stubble = new StubbleBuilder().Build();
        var optionsMap = options.ToKeyValues();
        var template = await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Templates", "build.yml"));

        return await stubble.RenderAsync(template, optionsMap);
    }

    public async Task<string> GenerateReleaseWorkflowAsync(ReleaseGeneratorOptions options)
    {
        var stubble = new StubbleBuilder().Build();
        var optionsMap = options.ToKeyValues();
        var template = await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Templates", "release.yml"));

        return await stubble.RenderAsync(template, optionsMap);
    }

    public async Task<string> GenerateReleaseLibWorkflowAsync(ReleaseLibGeneratorOptions options)
    {
        var stubble = new StubbleBuilder().Build();
        var optionsMap = options.ToKeyValues();
        var template = await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Templates", "releaselib.yml"));

        return await stubble.RenderAsync(template, optionsMap);
    }
}