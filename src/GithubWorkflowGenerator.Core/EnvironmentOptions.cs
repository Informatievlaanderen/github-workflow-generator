using System.Collections.Generic;

namespace GithubWorkflowGenerator.Core;

public record EnvironmentOptions(string PublishFolderForLambda, string S3BucketForLambda, IEnumerable<string> ServiceMatrix);
