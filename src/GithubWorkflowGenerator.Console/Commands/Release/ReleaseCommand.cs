using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GithubWorkflowGenerator.Core;
using GithubWorkflowGenerator.Core.Options;

namespace GithubWorkflowGenerator.Console.Commands.Release;

internal class ReleaseCommand : Command
{
    private const string CommandName = "release";
    private const string CommandDescription = "generate a release.yml file";

    public ReleaseCommand()
        : this(CommandName, CommandDescription)
    {
        var releaseFileName = new Option<string>(new[] { "--fileName" }, () => "release.yml", "Output file name.");
        var repositoryName = new Option<string>(new[] { "--repositoryName" }, "Repository name");
        var repositoryPrefix = new Option<string>(new[] { "--repositoryPrefix" }, "Repository prefix (e.g. sr for streetname-registry)");
        var buildArtifacts = new Option<List<string>>(new[] { "--buildArtifacts" }, "Build artifacts (space separated)") { AllowMultipleArgumentsPerToken = true };
        var nugetPackages = new Option<List<string>>(new[] { "--nugetPackages" }, "Nuget packages (space separated list of --prop:Artifact some-artfact and --prop:Package SomeNuGetPackage)") { AllowMultipleArgumentsPerToken = true };
        var lambdaSourceFolder = new Option<string>(new[] { "--lambdaSourceFolder" }, "Folder where the lambda function is zipped from.");
        var testPublishFolderForLambda = new Option<string>(new[] { "--testPublishFolderForLambda" }, "Test publish folder for lambda (must not end with / or \\)");
        var testS3BucketForLambda = new Option<string>(new[] { "--testS3BucketForLambda" }, "Test S3 bucket for lambda (in format s3://some.bucket.name)");
        var testServiceMatrix = new Option<List<string>>(new[] { "--testServiceMatrix" }, "List of services to deploy on Test (space separated)") { AllowMultipleArgumentsPerToken = true };
        var stagingPublishFolderForLambda = new Option<string>(new[] { "--stagingPublishFolderForLambda" }, "Staging publish folder for lambda (must not end with / or \\)");
        var stagingS3BucketForLambda = new Option<string>(new[] { "--stagingS3BucketForLambda" }, "Staging S3 bucket for lambda (in format s3://some.bucket.name)");
        var stagingServiceMatrix = new Option<List<string>>(new[] { "--stagingServiceMatrix" }, "List of services to deploy on Staging (space separated)") { AllowMultipleArgumentsPerToken = true };
        var productionPublishFolderForLambda = new Option<string>(new[] { "--productionPublishFolderForLambda" }, "Production publish folder for lambda (must not end with / or \\)");
        var productionS3BucketForLambda = new Option<string>(new[] { "--productionS3BucketForLambda" }, "Production S3 bucket for lambda (in format s3://some.bucket.name)");
        var productionServiceMatrix = new Option<List<string>>(new[] { "--productionServiceMatrix" }, "List of services to deploy on Production (space separated)") { AllowMultipleArgumentsPerToken = true };

        AddOption(releaseFileName);
        AddOption(repositoryName);
        AddOption(repositoryPrefix);
        AddOption(buildArtifacts);
        AddOption(nugetPackages);
        AddOption(lambdaSourceFolder);
        AddOption(testPublishFolderForLambda);
        AddOption(testS3BucketForLambda);
        AddOption(testServiceMatrix);
        AddOption(stagingPublishFolderForLambda);
        AddOption(stagingS3BucketForLambda);
        AddOption(stagingServiceMatrix);
        AddOption(productionPublishFolderForLambda);
        AddOption(productionS3BucketForLambda);
        AddOption(productionServiceMatrix);
        this.SetHandler(async context => await Handle(context!.FileName, context.RepositoryName, context.RepositoryPrefix, context.BuildArtifacts, context.NuGetPackages, context.LambdaSourceFolder,
                context.TestPublishFolderForLambda, context.TestS3BucketForLambda, context.TestServiceMatrix,
                context.StagingPublishFolderForLambda, context.StagingS3BucketForLambda, context.StagingServiceMatrix,
                context.ProductionPublishFolderForLambda, context.ProductionS3BucketForLambda, context.ProductionServiceMatrix),
            new ReleaseCommandInputBinder(releaseFileName, repositoryName, repositoryPrefix, buildArtifacts, nugetPackages, lambdaSourceFolder,
                testPublishFolderForLambda, testS3BucketForLambda, testServiceMatrix,
                stagingPublishFolderForLambda, stagingS3BucketForLambda, stagingServiceMatrix,
                productionPublishFolderForLambda, productionS3BucketForLambda, productionServiceMatrix));
    }

    public ReleaseCommand(string name, string? description = null)
        : base(name, description)
    { }
        
    private static async Task Handle(string fileName, string repositoryName, string repositoryPrefix, IEnumerable<string> buildArtifacts, Dictionary<string, string> nugetPackages, string lambdaSourceFolder,
        string testPublishFolderForLambda, string testS3BucketForLambda, IEnumerable<string> testServiceMatrix,
        string stagingPublishFolderForLambda, string stagingS3BucketForLambda, IEnumerable<string> stagingServiceMatrix,
        string productionPublishFolderForLambda, string productionS3BucketForLambda, IEnumerable<string> productionServiceMatrix)
    {
        var generator = new GithubGenerator();
        var options = new ReleaseGeneratorOptions(repositoryName, repositoryPrefix, buildArtifacts, nugetPackages.Select(x => new NuGetArtifactAndPackage(x.Key, x.Value)), lambdaSourceFolder,
            new EnvironmentOptions(testPublishFolderForLambda, testS3BucketForLambda, testServiceMatrix),
            new EnvironmentOptions(stagingPublishFolderForLambda, stagingS3BucketForLambda, stagingServiceMatrix),
            new EnvironmentOptions(productionPublishFolderForLambda, productionS3BucketForLambda, productionServiceMatrix));
        string result = await generator.GenerateReleaseWorkflowAsync(options);
        await File.WriteAllTextAsync(fileName, result);
 
        IConsole console = new SystemConsole();
        console.WriteLine($"{fileName} was successfully generated.");
    }
}