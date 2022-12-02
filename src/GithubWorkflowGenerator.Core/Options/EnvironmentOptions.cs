using System.Collections.Generic;

namespace GithubWorkflowGenerator.Core.Options;

public record EnvironmentOptions(string S3BucketForLambda, IEnumerable<string> ServiceMatrix);
