using System;
using System.Collections.Generic;
using System.Linq;

namespace GithubWorkflowGenerator.Core.Options;

public record ReleaseGeneratorOptions(string WorkflowName, string RepositoryName, string RepositoryPrefix, IEnumerable<string> BuildArtifacts, IEnumerable<NuGetArtifactAndPackage> NuGetPackages,
    bool SkipLambda,
    string JiraPrefix,
    string JiraProject,
    string LambdaSourceFolder,
    EnvironmentOptions Test, EnvironmentOptions Staging, EnvironmentOptions Production);

public static class ReleaseGeneratorOptionsExtensions
{
    private static string SingleQuoted(this string s)
    {
        if (s[Index.Start] != '\'')
        {
            s = s.Insert(0, "'");
        }

        if (s[^1] != '\'')
        {
            s += '\'';
        }

        return s;
    }

    private static IEnumerable<string> Transform(this IEnumerable<string?>? values, Func<string, string> transform)
    {
        var valueList = values?.ToList();
        if (valueList is null || !valueList.Any())
        {
            return Enumerable.Empty<string>();
        }

        var result = valueList
            .Where(x => x is not null)
            .Select(x => transform(x!));

        return result;
    }

    public static IDictionary<string, object?> ToKeyValues(this ReleaseGeneratorOptions options) => new Dictionary<string, object?>
    {
        [nameof(options.WorkflowName)] = options.WorkflowName,
        [nameof(options.RepositoryName)] = options.RepositoryName,
        [nameof(options.RepositoryPrefix)] = options.RepositoryPrefix,
        [nameof(options.BuildArtifacts)] = options.BuildArtifacts,
        [nameof(options.NuGetPackages)] = options.NuGetPackages,
        [nameof(options.SkipLambda)] = options.SkipLambda,
        [nameof(options.JiraPrefix)] = options.JiraPrefix,
        [nameof(options.JiraProject)] = options.JiraProject,
        [nameof(options.LambdaSourceFolder)] = options.LambdaSourceFolder,
        [nameof(options.Test.S3BucketForLambda) + nameof(options.Test)] = options.Test.S3BucketForLambda,
        [nameof(options.Staging.S3BucketForLambda) + nameof(options.Staging)] = options.Staging.S3BucketForLambda,
        [nameof(options.Production.S3BucketForLambda) + nameof(options.Production)] = options.Production.S3BucketForLambda,
        [nameof(options.Test.ServiceMatrix) + nameof(options.Test)] = string.Join(", ", options.Test.ServiceMatrix.Transform(SingleQuoted)),
        [nameof(options.Staging.ServiceMatrix) + nameof(options.Staging)] = string.Join(", ", options.Staging.ServiceMatrix.Transform(SingleQuoted)),
        [nameof(options.Production.ServiceMatrix) + nameof(options.Production)] = string.Join(", ", options.Production.ServiceMatrix.Transform(SingleQuoted))
    };
}