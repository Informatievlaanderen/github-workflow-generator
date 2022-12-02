# github-workflow-generator
Generates build.yml &amp; release.yml files based on command-line parameters

## Features

### Generate build.yml files

Example:
~~~
gwg build --solutionName StreetNameRegistry.sln --sonarKey streetname-registry
~~~
### Generate release.yml files

Example:
~~~
gwg release --repositoryName streetname-registry 
  --repositoryPrefix sr 
  --buildArtifacts buildArtifact1 buildArtifact2
  --nugetPackages My.Package.Name1 My.Package.Name2
  --lambdaSourceFolder /home/runner/work/streetname-registry/streetname-registry/dist/StreetNameRegistry.Api.BackOffice.Handlers.Lambda/linux
  --testS3BucketForLambda s3://my.test.lambda.bucket
  --testServiceMatrix service1 service2 service3
  --stagingS3BucketForLambda  s3://my.stg.lambda.bucket
  --stagingServiceMatrix service1 service2 service3
  --productionS3BucketForLambda  s3://my.prd.lambda.bucket
  --productionServiceMatrix service1 service2
~~~
