using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq;

namespace GithubWorkflowGenerator.Console.Commands.Release;

public record ReleaseCommandInput(string FileName, string RepositoryName, string RepositoryPrefix, List<string> BuildArtifacts, Dictionary<string, string> NuGetPackages, string LambdaSourceFolder,
    string TestPublishFolderForLambda, string TestS3BucketForLambda, List<string> TestServiceMatrix,
    string StagingPublishFolderForLambda, string StagingS3BucketForLambda, List<string> StagingServiceMatrix,
    string ProductionPublishFolderForLambda, string ProductionS3BucketForLambda, List<string> ProductionServiceMatrix);

public class ReleaseCommandInputBinder : BinderBase<ReleaseCommandInput?>
{
    private readonly Option<string> _fileName;
    private readonly Option<string> _repositoryName;
    private readonly Option<string> _repositoryPrefix;
    private readonly Option<List<string>> _buildArtifacts;
    private readonly Option<List<string>> _nugetPackages;
    private readonly Option<string> _lambdaSourceFolder;
    private readonly Option<string> _testPublishFolderForLambda;
    private readonly Option<string> _testS3BucketForLambda;
    private readonly Option<List<string>> _testServiceMatrix;
    private readonly Option<string> _stagingPublishFolderForLambda;
    private readonly Option<string> _stagingS3BucketForLambda;
    private readonly Option<List<string>> _stagingServiceMatrix;
    private readonly Option<string> _productionPublishFolderForLambda;
    private readonly Option<string> _productionS3BucketForLambda;
    private readonly Option<List<string>> _productionServiceMatrix;

    public ReleaseCommandInputBinder(Option<string> fileName, Option<string> repositoryName, Option<string> repositoryPrefix, Option<List<string>> buildArtifacts, Option<List<string>> nugetPackages, Option<string> lambdaSourceFolder,
        Option<string> testPublishFolderForLambda, Option<string> testS3BucketForLambda, Option<List<string>> testServiceMatrix,
        Option<string> stagingPublishFolderForLambda, Option<string> stagingS3BucketForLambda, Option<List<string>> stagingServiceMatrix,
        Option<string> productionPublishFolderForLambda, Option<string> productionS3BucketForLambda, Option<List<string>> productionServiceMatrix)
    {
        _fileName = fileName;
        _repositoryName = repositoryName;
        _repositoryPrefix = repositoryPrefix;
        _buildArtifacts = buildArtifacts;
        _nugetPackages = nugetPackages;
        _lambdaSourceFolder = lambdaSourceFolder;
        _testPublishFolderForLambda = testPublishFolderForLambda;
        _testS3BucketForLambda = testS3BucketForLambda;
        _testServiceMatrix = testServiceMatrix;
        _stagingPublishFolderForLambda = stagingPublishFolderForLambda;
        _stagingS3BucketForLambda = stagingS3BucketForLambda;
        _stagingServiceMatrix = stagingServiceMatrix;
        _productionPublishFolderForLambda = productionPublishFolderForLambda;
        _productionS3BucketForLambda = productionS3BucketForLambda;
        _productionServiceMatrix = productionServiceMatrix;
    }

    protected override ReleaseCommandInput? GetBoundValue(BindingContext bindingContext)
    {
        var fileName = bindingContext.ParseResult.GetValueForOption(_fileName);
        var repositoryName = bindingContext.ParseResult.GetValueForOption(_repositoryName);
        var repositoryPrefix = bindingContext.ParseResult.GetValueForOption(_repositoryPrefix);
        var buildArtifacts = bindingContext.ParseResult.GetValueForOption(_buildArtifacts);
        var nugetPackages = bindingContext.ParseResult.GetValueForOption(_nugetPackages);
        var lambdaSourceFolder = bindingContext.ParseResult.GetValueForOption(_lambdaSourceFolder);
        var testPublishFolderForLambda = bindingContext.ParseResult.GetValueForOption(_testPublishFolderForLambda);
        var testS3BucketForLambda = bindingContext.ParseResult.GetValueForOption(_testS3BucketForLambda);
        var testServiceMatrix = bindingContext.ParseResult.GetValueForOption(_testServiceMatrix);
        var stagingPublishFolderForLambda = bindingContext.ParseResult.GetValueForOption(_stagingPublishFolderForLambda);
        var stagingS3BucketForLambda = bindingContext.ParseResult.GetValueForOption(_stagingS3BucketForLambda);
        var stagingServiceMatrix = bindingContext.ParseResult.GetValueForOption(_stagingServiceMatrix);
        var productionPublishFolderForLambda = bindingContext.ParseResult.GetValueForOption(_productionPublishFolderForLambda);
        var productionS3BucketForLambda = bindingContext.ParseResult.GetValueForOption(_productionS3BucketForLambda);
        var productionServiceMatrix = bindingContext.ParseResult.GetValueForOption(_productionServiceMatrix);
        if (fileName is null || repositoryName is null || repositoryPrefix is null || buildArtifacts is null /*|| nugetPackages is null*/
            || testPublishFolderForLambda is null || testS3BucketForLambda is null || testServiceMatrix is null
            || stagingPublishFolderForLambda is null || stagingS3BucketForLambda is null || stagingServiceMatrix is null
            || productionPublishFolderForLambda is null || productionS3BucketForLambda is null || productionServiceMatrix is null)
        {
            return default;
        }

        return new ReleaseCommandInput(fileName, repositoryName, repositoryPrefix, buildArtifacts, DictionaryFrom(nugetPackages!), lambdaSourceFolder!,
            testPublishFolderForLambda, testS3BucketForLambda, testServiceMatrix,
            stagingPublishFolderForLambda, stagingS3BucketForLambda, stagingServiceMatrix,
            productionPublishFolderForLambda, productionS3BucketForLambda, productionServiceMatrix);
    }

    private Dictionary<string, string> DictionaryFrom(List<string> nugetPackages)
    {
        var keys = new List<string>(nugetPackages).Where((x, i) => i % 2 == 0).ToList();
        var values = new List<string>(nugetPackages).Where((x, i) => i % 2 != 0).ToList();
        if (keys.Count != values.Count)
        {
            throw new InvalidOperationException("Uneven number of nuget artifacts and packages");
        }

        return keys
            .Zip(values, (key, value) => new { Key = key, Value = value })
            .ToDictionary(x => x.Key, x => x.Value);
    }
}
