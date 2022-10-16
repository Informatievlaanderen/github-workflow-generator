using System.Collections.Generic;

namespace GithubWorkflowGenerator.Core.Options;

public record EnvironmentOptions(string PublishFolderForLambda, string S3BucketForLambda, IEnumerable<string> ServiceMatrix);
