using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq;
using GithubWorkflowGenerator.Console.Extensions;

namespace GithubWorkflowGenerator.Console.Commands.Release;

public record ReleaseCommandInput(string FileName, string WorkflowName, string RepositoryName, string RepositoryPrefix, List<string> BuildArtifacts, Dictionary<string, string> NuGetPackages,
    bool SkipLambda,
    string JiraPrefix,
    string JiraProject,
    string LambdaSourceFolder,
    string TestS3BucketForLambda, List<string> TestServiceMatrix,
    string StagingS3BucketForLambda, List<string> StagingServiceMatrix,
    string ProductionS3BucketForLambda, List<string> ProductionServiceMatrix);

public class ReleaseCommandInputBinder : BinderBase<ReleaseCommandInput?>
{
    private readonly Option<string> _fileName;
    private readonly Option<string> _workflowName;
    private readonly Option<string> _repositoryName;
    private readonly Option<string> _repositoryPrefix;
    private readonly Option<List<string>> _buildArtifacts;
    private readonly Option<List<string>> _nugetPackages;
    private readonly Option<bool> _skipLambda;
    private readonly Option<string> _jiraPrefix;
    private readonly Option<string> _jiraProject;
    private readonly Option<string> _lambdaSourceFolder;
    private readonly Option<string> _testS3BucketForLambda;
    private readonly Option<List<string>> _testServiceMatrix;
    private readonly Option<string> _stagingS3BucketForLambda;
    private readonly Option<List<string>> _stagingServiceMatrix;
    private readonly Option<string> _productionS3BucketForLambda;
    private readonly Option<List<string>> _productionServiceMatrix;

    public ReleaseCommandInputBinder(Option<string> fileName, Option<string> workflowName, Option<string> repositoryName, Option<string> repositoryPrefix, Option<List<string>> buildArtifacts, Option<List<string>> nugetPackages,
        Option<bool> skipLambda,
        Option<string> jiraPrefix,
        Option<string> jiraProject,
        Option<string> lambdaSourceFolder,
        Option<string> testS3BucketForLambda, Option<List<string>> testServiceMatrix,
        Option<string> stagingS3BucketForLambda, Option<List<string>> stagingServiceMatrix,
        Option<string> productionS3BucketForLambda, Option<List<string>> productionServiceMatrix)
    {
        _fileName = fileName;
        _workflowName = workflowName;
        _repositoryName = repositoryName;
        _repositoryPrefix = repositoryPrefix;
        _buildArtifacts = buildArtifacts;
        _nugetPackages = nugetPackages;
        _skipLambda = skipLambda;
        _jiraPrefix = jiraPrefix;
        _jiraProject = jiraProject;
        _lambdaSourceFolder = lambdaSourceFolder;
        _testS3BucketForLambda = testS3BucketForLambda;
        _testServiceMatrix = testServiceMatrix;
        _stagingS3BucketForLambda = stagingS3BucketForLambda;
        _stagingServiceMatrix = stagingServiceMatrix;
        _productionS3BucketForLambda = productionS3BucketForLambda;
        _productionServiceMatrix = productionServiceMatrix;
    }

    protected override ReleaseCommandInput? GetBoundValue(BindingContext bindingContext)
    {
        var fileName = bindingContext.ParseResult.GetValueForOption(_fileName);
        var workflowName = bindingContext.ParseResult.GetValueForOption(_workflowName);
        var repositoryName = bindingContext.ParseResult.GetValueForOption(_repositoryName);
        var repositoryPrefix = bindingContext.ParseResult.GetValueForOption(_repositoryPrefix);
        var buildArtifacts = bindingContext.ParseResult.GetValueForOption(_buildArtifacts);
        var nugetPackages = bindingContext.ParseResult.GetValueForOption(_nugetPackages);
        var skipLambda = bindingContext.ParseResult.GetValueForOption(_skipLambda);
        var jiraPrefix = bindingContext.ParseResult.GetValueForOption(_jiraPrefix);
        var jiraProject = bindingContext.ParseResult.GetValueForOption(_jiraProject);
        var lambdaSourceFolder = bindingContext.ParseResult.GetValueForOption(_lambdaSourceFolder);
        var testS3BucketForLambda = bindingContext.ParseResult.GetValueForOption(_testS3BucketForLambda);
        var testServiceMatrix = bindingContext.ParseResult.GetValueForOption(_testServiceMatrix);
        var stagingS3BucketForLambda = bindingContext.ParseResult.GetValueForOption(_stagingS3BucketForLambda);
        var stagingServiceMatrix = bindingContext.ParseResult.GetValueForOption(_stagingServiceMatrix);
        var productionS3BucketForLambda = bindingContext.ParseResult.GetValueForOption(_productionS3BucketForLambda);
        var productionServiceMatrix = bindingContext.ParseResult.GetValueForOption(_productionServiceMatrix);
        if (fileName is null || workflowName is null || repositoryName is null || repositoryPrefix is null || buildArtifacts is null
            || jiraPrefix is null || jiraProject is null
            || testS3BucketForLambda is null || testServiceMatrix is null
            || stagingS3BucketForLambda is null || stagingServiceMatrix is null
            || productionS3BucketForLambda is null || productionServiceMatrix is null)
        {
            return default;
        }

        return new ReleaseCommandInput(fileName, workflowName, repositoryName, repositoryPrefix, buildArtifacts, DictionaryFrom(nugetPackages!),
            skipLambda,
            jiraPrefix,
            jiraProject,
            lambdaSourceFolder!,
            testS3BucketForLambda, testServiceMatrix,
            stagingS3BucketForLambda, stagingServiceMatrix,
            productionS3BucketForLambda, productionServiceMatrix);
    }

    private Dictionary<string, string> DictionaryFrom(List<string> nugetPackages)
    {
        var values = new List<string>(nugetPackages).Where((x, i) => i % 2 != 0).ToList();
        var keys = values.Select(x => x.ToKebabCase()).ToList();
        if (keys.Count != values.Count)
        {
            throw new InvalidOperationException("Uneven number of nuget artifacts and packages");
        }

        return keys
            .Zip(values, (key, value) => new { Key = key, Value = value })
            .ToDictionary(x => x.Key, x => x.Value);
    }
}
