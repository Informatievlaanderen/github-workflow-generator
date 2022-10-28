using System.Collections.Generic;

namespace GithubWorkflowGenerator.Core.Options;

public record ReleaseLibGeneratorOptions(IEnumerable<string> NuGetPackages);

public static class ReleaseLibGeneratorOptionsExtensions
{
    public static IDictionary<string, object?> ToKeyValues(this ReleaseLibGeneratorOptions options) => new Dictionary<string, object?>
    {
        [nameof(options.NuGetPackages)] = options.NuGetPackages
    };
}