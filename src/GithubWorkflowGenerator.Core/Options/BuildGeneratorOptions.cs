using System.Collections.Generic;

namespace GithubWorkflowGenerator.Core.Options;

public record BuildGeneratorOptions(string SolutionName, string SonarKey);

public static class BuildGeneratorExtensions
{
    public static IDictionary<string, string> ToKeyValues(this BuildGeneratorOptions options) => new Dictionary<string, string>
    {
        [nameof(options.SolutionName)] = options.SolutionName,
        [nameof(options.SonarKey)] = options.SonarKey
    };
}
