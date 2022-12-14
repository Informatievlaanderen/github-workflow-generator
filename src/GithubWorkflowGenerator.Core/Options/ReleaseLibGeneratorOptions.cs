using System.Collections.Generic;

namespace GithubWorkflowGenerator.Core.Options;

public record ReleaseLibGeneratorOptions(IEnumerable<string> NuGetPackages, string JiraPrefix, string JiraProject);

public static class ReleaseLibGeneratorOptionsExtensions
{
    public static IDictionary<string, object?> ToKeyValues(this ReleaseLibGeneratorOptions options) => new Dictionary<string, object?>
    {
        [nameof(options.NuGetPackages)] = options.NuGetPackages,
        [nameof(options.JiraPrefix)] = options.JiraPrefix,
        [nameof(options.JiraProject)] = options.JiraProject
    };
}