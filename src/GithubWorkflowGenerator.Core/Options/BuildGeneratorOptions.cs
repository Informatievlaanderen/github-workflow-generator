using System.Collections.Generic;

namespace GithubWorkflowGenerator.Core.Options;

public record BuildGeneratorOptions(string SolutionName, string SonarKey, bool OnPullRequests);

public static class BuildGeneratorExtensions
{
    public static IDictionary<string, object?> ToKeyValues(this BuildGeneratorOptions options) => new Dictionary<string, object?>
    {
        [nameof(options.SolutionName)] = options.SolutionName,
        [nameof(options.SonarKey)] = options.SonarKey,
        [nameof(options.OnPullRequests)] = options.OnPullRequests ? "true" : null
    };
}
