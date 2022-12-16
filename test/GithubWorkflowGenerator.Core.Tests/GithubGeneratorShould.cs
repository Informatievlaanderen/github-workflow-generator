using System.Threading.Tasks;
using GithubWorkflowGenerator.Core.Options;
using Xunit;

namespace GithubWorkflowGenerator.Core.Tests;

public class GithubGeneratorShould
{
    [Fact]
    public async Task GenerateBuildWorkflowWithPullRequests()
    {
        const string expected = @"name: Build

on:
  push:
    branches: [ main ]
  pull_request:
  workflow_dispatch: 

jobs:
  build:
    if: github.repository_owner == 'Informatievlaanderen'
    name: Build
    runs-on: ubuntu-latest

    steps:
    - name: Checkout Code
      uses: actions/checkout@v3

    - name: Cache Paket
      uses: actions/cache@v3
      env:
        cache-name: cache-paket
      with:
        path: packages
        key: ${{ runner.os }}-build-${{ env.cache-name }}-${{ hashFiles('paket.lock') }}
        restore-keys: |
          ${{ runner.os }}-build-${{ env.cache-name }}-

    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo ""$GITHUB_REPOSITORY"" | awk -F / '{print $2}' | sed -e ""s/:refs//"") >> $GITHUB_ENV
      shell: bash

    - name: Setup .NET Core
      uses: actions/setup-dotnet@v2
      with:
        dotnet-version: ${{ secrets.VBR_DOTNET_VERSION }}

    - name: .NET version
      shell: bash
      run: dotnet --info

    - name: Restore packages
      shell: bash
      run: |
        dotnet tool restore
        dotnet paket install
    
    - name: Cache SonarCloud packages
      uses: actions/cache@v1
      with:
        path: ~/sonar/cache
        key: ${{ runner.os }}-sonar
        restore-keys: ${{ runner.os }}-sonar
        
    - name: Cache SonarCloud scanner
      id: cache-sonar-scanner
      uses: actions/cache@v1
      with:
        path: ./.sonar/scanner
        key: ${{ runner.os }}-sonar-scanner
        restore-keys: ${{ runner.os }}-sonar-scanner

    - name: Install DotCover
      shell: bash
      run: |
        dotnet tool install --global JetBrains.dotCover.GlobalTool
        
    - name: Install SonarCloud scanner
      if: steps.cache-sonar-scanner.outputs.cache-hit != 'true'
      shell: bash
      run: |
        mkdir .sonar
        mkdir .sonar/scanner
        dotnet tool update dotnet-sonarscanner --tool-path ./.sonar/scanner
        
    - name: Sonar begin build & analyze
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}  # Needed to get PR information, if any
        SONAR_TOKEN: ${{ secrets.VBR_SONAR_TOKEN }}
      shell: bash
      run: |
        ./.sonar/scanner/dotnet-sonarscanner begin /k:""Informatievlaanderen_streetname-registry"" /o:""informatievlaanderen"" /d:sonar.login=""${{ secrets.VBR_SONAR_TOKEN }}"" /d:sonar.host.url=""https://sonarcloud.io"" /d:sonar.cs.dotcover.reportsPaths=dotCover.Output.html > /dev/null 2>&1

    - name: Build
      shell: bash
      run: |
        dotnet build --nologo --no-restore --no-incremental --configuration Debug StreetNameRegistry.sln

    - name: Test
      shell: bash
      run: dotnet dotcover test --dcReportType=HTML --nologo --no-build StreetNameRegistry.sln
        
    - name: Sonar end build & analyze
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}  # Needed to get PR information, if any
        SONAR_TOKEN: ${{ secrets.VBR_SONAR_TOKEN }}
      shell: bash
      run: |
        ./.sonar/scanner/dotnet-sonarscanner end /d:sonar.login=""${{ secrets.VBR_SONAR_TOKEN }}"" > /dev/null 2>&1
";

        var options = new BuildGeneratorOptions("StreetNameRegistry.sln", "streetname-registry", true);
        var result = await new GithubGenerator().GenerateBuildWorkflowAsync(options);

        Assert.NotNull(result);
        Assert.Equal(expected.ExceptCharacters(new []{ '#', ' ', '\r', '\n' }), result.ExceptCharacters(new []{ '#', ' ', '\r', '\n' }));
    }

    [Fact]
    public async Task GenerateBuildWorkflowWithoutPullRequests()
    {
        const string expected = @"name: Build

on:
  push:
    branches: [ main ]
  workflow_dispatch: 

jobs:
  build:
    if: github.repository_owner == 'Informatievlaanderen'
    name: Build
    runs-on: ubuntu-latest

    steps:
    - name: Checkout Code
      uses: actions/checkout@v3

    - name: Cache Paket
      uses: actions/cache@v3
      env:
        cache-name: cache-paket
      with:
        path: packages
        key: ${{ runner.os }}-build-${{ env.cache-name }}-${{ hashFiles('paket.lock') }}
        restore-keys: |
          ${{ runner.os }}-build-${{ env.cache-name }}-

    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo ""$GITHUB_REPOSITORY"" | awk -F / '{print $2}' | sed -e ""s/:refs//"") >> $GITHUB_ENV
      shell: bash

    - name: Setup .NET Core
      uses: actions/setup-dotnet@v2
      with:
        dotnet-version: ${{ secrets.VBR_DOTNET_VERSION }}

    - name: .NET version
      shell: bash
      run: dotnet --info

    - name: Restore packages
      shell: bash
      run: |
        dotnet tool restore
        dotnet paket install
    
    - name: Cache SonarCloud packages
      uses: actions/cache@v1
      with:
        path: ~/sonar/cache
        key: ${{ runner.os }}-sonar
        restore-keys: ${{ runner.os }}-sonar
        
    - name: Cache SonarCloud scanner
      id: cache-sonar-scanner
      uses: actions/cache@v1
      with:
        path: ./.sonar/scanner
        key: ${{ runner.os }}-sonar-scanner
        restore-keys: ${{ runner.os }}-sonar-scanner

    - name: Install DotCover
      shell: bash
      run: |
        dotnet tool install --global JetBrains.dotCover.GlobalTool
        
    - name: Install SonarCloud scanner
      if: steps.cache-sonar-scanner.outputs.cache-hit != 'true'
      shell: bash
      run: |
        mkdir .sonar
        mkdir .sonar/scanner
        dotnet tool update dotnet-sonarscanner --tool-path ./.sonar/scanner
        
    - name: Sonar begin build & analyze
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}  # Needed to get PR information, if any
        SONAR_TOKEN: ${{ secrets.VBR_SONAR_TOKEN }}
      shell: bash
      run: |
        ./.sonar/scanner/dotnet-sonarscanner begin /k:""Informatievlaanderen_streetname-registry"" /o:""informatievlaanderen"" /d:sonar.login=""${{ secrets.VBR_SONAR_TOKEN }}"" /d:sonar.host.url=""https://sonarcloud.io"" /d:sonar.cs.dotcover.reportsPaths=dotCover.Output.html > /dev/null 2>&1

    - name: Build
      shell: bash
      run: |
        dotnet build --nologo --no-restore --no-incremental --configuration Debug StreetNameRegistry.sln

    - name: Test
      shell: bash
      run: dotnet dotcover test --dcReportType=HTML --nologo --no-build StreetNameRegistry.sln
        
    - name: Sonar end build & analyze
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}  # Needed to get PR information, if any
        SONAR_TOKEN: ${{ secrets.VBR_SONAR_TOKEN }}
      shell: bash
      run: |
        ./.sonar/scanner/dotnet-sonarscanner end /d:sonar.login=""${{ secrets.VBR_SONAR_TOKEN }}"" > /dev/null 2>&1
";

        var options = new BuildGeneratorOptions("StreetNameRegistry.sln", "streetname-registry", false);
        var result = await new GithubGenerator().GenerateBuildWorkflowAsync(options);

        Assert.NotNull(result);
        Assert.Equal(expected.ExceptCharacters(new []{ '#', ' ', '\r', '\n' }), result.ExceptCharacters(new []{ '#', ' ', '\r', '\n' }));
    }

    [Fact]
    public async Task GenerateReleaseWorkflow()
    {
        const string expected = @"name: Release

on:
  workflow_dispatch:

jobs:
  build:
    if: github.repository_owner == 'Informatievlaanderen'
    name: Release
    runs-on: ubuntu-latest
    outputs:
      version: ${{ steps.set-version.outputs.version }}

    steps:
    - name: Checkout Code
      uses: actions/checkout@v3
      with:
          persist-credentials: false

    - name: Cache NPM
      uses: actions/cache@v3
      env:
        cache-name: cache-npm
      with:
        path: ~/.npm
        key: ${{ runner.os }}-build-${{ env.cache-name }}-${{ hashFiles('**/package-lock.json') }}
        restore-keys: |
          ${{ runner.os }}-build-${{ env.cache-name }}-

    - name: Cache Paket
      uses: actions/cache@v3
      env:
        cache-name: cache-paket
      with:
        path: packages
        key: ${{ runner.os }}-build-${{ env.cache-name }}-${{ hashFiles('paket.lock') }}
        restore-keys: |
          ${{ runner.os }}-build-${{ env.cache-name }}-

    - name: Cache Python
      uses: actions/cache@v3
      env:
        cache-name: cache-pip
      with:
        path: ~/.cache/pip
        key: ${{ runner.os }}-build-${{ env.cache-name }}

    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo ""$GITHUB_REPOSITORY"" | awk -F / '{print $2}' | sed -e ""s/:refs//"") >> $GITHUB_ENV
      shell: bash

    - name: Setup Node.js
      uses: actions/setup-node@v3.5.1

    - name: Setup .NET Core
      uses: actions/setup-dotnet@v2
      with:
        dotnet-version: ${{ secrets.VBR_DOTNET_VERSION }}

    - name: Setup Python
      uses: actions/setup-python@v3
      with:
        python-version: '3.x'

    - name: Node version
      shell: bash
      run: node --version

    - name: .NET version
      shell: bash
      run: dotnet --info

    - name: Python version
      shell: bash
      run: python --version

    - name: Install NPM dependencies
      shell: bash
      run: npm ci

    - name: Install Python dependencies
      shell: bash
      run: |
        python -m pip install --upgrade pip
        pip install requests markdown argparse

    - name: Run Semantic Release
      shell: bash
      run: npx semantic-release
      env:
        BUILD_DOCKER_REGISTRY: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
        GITHUB_TOKEN: ${{ secrets.VBR_GIT_RELEASE_TOKEN }}
        GIT_COMMIT: ${{ github.sha }}
        GIT_USERNAME: ${{ secrets.VBR_GIT_USER }}
        GIT_AUTHOR_NAME: ${{ secrets.VBR_GIT_USER }}
        GIT_COMMITTER_NAME: ${{ secrets.VBR_GIT_USER }}
        GIT_EMAIL: ${{ secrets.VBR_GIT_EMAIL }}
        GIT_AUTHOR_EMAIL: ${{ secrets.VBR_GIT_EMAIL }}
        GIT_COMMITTER_EMAIL: ${{ secrets.VBR_GIT_EMAIL }}

    - name: Set Release Version
      id: set-version
      run: |
        [ ! -f semver ] && echo none > semver
        echo ::set-output name=version::$(cat semver)
        echo RELEASE_VERSION=$(cat semver) >> $GITHUB_ENV
      shell: bash

     #
     # Save artifacts
     #
      
    - name: Save artifacts
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/api-backoffice:$SEMVER -o ~/sr-api-backoffice-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/api-legacy:$SEMVER -o ~/sr-api-legacy-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/api-oslo:$SEMVER -o ~/sr-api-oslo-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/api-crab-import:$SEMVER -o ~/sr-api-crab-import-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/api-extract:$SEMVER -o ~/sr-api-extract-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/projector:$SEMVER -o ~/sr-projector-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/projections-syndication:$SEMVER -o ~/sr-projections-syndication-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/consumer:$SEMVER -o ~/sr-consumer-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/producer:$SEMVER -o ~/sr-producer-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/producer-snapshot-oslo:$SEMVER -o ~/sr-producer-snapshot-oslo-image.tar
        docker image save $BUILD_DOCKER_REGISTRY/streetname-registry/migrator-streetname:$SEMVER -o ~/sr-migrator-streetname-image.tar
      env:
        BUILD_DOCKER_REGISTRY: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
        SEMVER: ${{ env.RELEASE_VERSION }}

    #
    # Upload NuGet packages
    #

    - name: Upload NuGet package api-backoffice
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: nuget-api-backoffice
        path: /home/runner/work/streetname-registry/streetname-registry/dist/nuget/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.BackOffice.*.nupkg
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Upload NuGet package api-backoffice-abstractions
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: nuget-api-backoffice-abstractions
        path: /home/runner/work/streetname-registry/streetname-registry/dist/nuget/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.BackOffice.Abstractions.*.nupkg
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Upload NuGet package api-legacy
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: nuget-api-legacy
        path: /home/runner/work/streetname-registry/streetname-registry/dist/nuget/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Legacy.*.nupkg
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Upload NuGet package api-oslo
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: nuget-api-oslo
        path: /home/runner/work/streetname-registry/streetname-registry/dist/nuget/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Oslo.*.nupkg
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Upload NuGet package api-extract
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: nuget-api-extract
        path: /home/runner/work/streetname-registry/streetname-registry/dist/nuget/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Extract.*.nupkg
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Upload NuGet package api-crab-import
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: nuget-api-crab-import
        path: /home/runner/work/streetname-registry/streetname-registry/dist/nuget/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.CrabImport.*.nupkg
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Upload NuGet package projector
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: nuget-projector
        path: /home/runner/work/streetname-registry/streetname-registry/dist/nuget/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Projector.*.nupkg
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}
    
    #
    # Upload build artifacts
    #
    
    - name: Upload api-backoffice artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: api-backoffice
        path: ~/sr-api-backoffice-image.tar
        
    - name: Upload api-legacy artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: api-legacy
        path: ~/sr-api-legacy-image.tar
        
    - name: Upload api-oslo artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: api-oslo
        path: ~/sr-api-oslo-image.tar
        
    - name: Upload api-crab-import artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: api-crab-import
        path: ~/sr-api-crab-import-image.tar
        
    - name: Upload api-extract artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: api-extract
        path: ~/sr-api-extract-image.tar
        
    - name: Upload projector artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: projector
        path: ~/sr-projector-image.tar
        
    - name: Upload projections-syndication artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: projections-syndication
        path: ~/sr-projections-syndication-image.tar
        
    - name: Upload consumer artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: consumer
        path: ~/sr-consumer-image.tar
        
    - name: Upload producer artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: producer
        path: ~/sr-producer-image.tar

    - name: Upload producer-snapshot-oslo artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: producer-snapshot-oslo
        path: ~/sr-producer-snapshot-oslo-image.tar
        
    - name: Upload migrator-streetname artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: migrator-streetname
        path: ~/sr-migrator-streetname-image.tar

    - name: Package Lambda functions        
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        echo Zip lambda functions
        pushd /home/runner/work/streetname-registry/streetname-registry/dist/StreetNameRegistry.Api.BackOffice.Handlers.Lambda/linux
        echo zip -r lambda.zip .
        zip -r lambda.zip .
        popd

    - name: Configure AWS credentials (Test)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/configure-aws-credentials@v1-node16
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}

    - name: Login to Amazon ECR (Test)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/amazon-ecr-login@v1.5.2

    - name: Push Lambda functions to S3 Test
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        echo Push Lambda functions to S3 Test
        pushd /home/runner/work/streetname-registry/streetname-registry/dist/StreetNameRegistry.Api.BackOffice.Handlers.Lambda/linux
        echo aws s3 cp lambda.zip s3://s3-vbr-test-basisregisters-lam-sr-sqsbackofficehandlerfunction/$SEMVER/lambda.zip
        aws s3 cp lambda.zip s3://s3-vbr-test-basisregisters-lam-sr-sqsbackofficehandlerfunction/$SEMVER/lambda.zip
        popd
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Configure AWS credentials (Staging)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/configure-aws-credentials@v1-node16
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY }}
        aws-region: ${{ secrets.VBR_AWS_REGION }}

    - name: Login to Amazon ECR (Staging)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/amazon-ecr-login@v1.5.2

    - name: Push Lambda functions to S3 Staging
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        echo Push Lambda functions to S3 Staging
        pushd /home/runner/work/streetname-registry/streetname-registry/dist/StreetNameRegistry.Api.BackOffice.Handlers.Lambda/linux
        echo aws s3 cp lambda.zip s3://s3-vbr-stg-basisregisters-lam-sr-sqsbackofficehandlerfunction/$SEMVER/lambda.zip
        aws s3 cp lambda.zip s3://s3-vbr-stg-basisregisters-lam-sr-sqsbackofficehandlerfunction/$SEMVER/lambda.zip
        popd
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Configure AWS credentials (Production)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/configure-aws-credentials@v1-node16
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_PRD }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_PRD }}
        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}

    - name: Login to Amazon ECR (Production)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/amazon-ecr-login@v1.5.2

    - name: Push Lambda functions to S3 Production
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        echo Push Lambda functions to S3 Production
        pushd /home/runner/work/streetname-registry/streetname-registry/dist/StreetNameRegistry.Api.BackOffice.Handlers.Lambda/linux
        echo aws s3 cp lambda.zip s3://s3-vbr-prd-basisregisters-lam-sr-sqsbackofficehandlerfunction/$SEMVER/lambda.zip
        aws s3 cp lambda.zip s3://s3-vbr-prd-basisregisters-lam-sr-sqsbackofficehandlerfunction/$SEMVER/lambda.zip
        popd
      env:
       SEMVER: ${{ env.RELEASE_VERSION }}

  publish_to_atlassian:
    if: needs.build.outputs.version != 'none'
    needs: build
    name: Publish to Atlassian
    runs-on: ubuntu-latest
    steps:
      - name: Checkout Code
        uses: actions/checkout@v3

      - name: Parse repository name
        run: echo REPOSITORY_NAME=$(echo ""$GITHUB_REPOSITORY"" | awk -F / '{print $2}' | sed -e ""s/:refs//"") >> $GITHUB_ENV
        shell: bash

      - name: Cache Paket
        uses: actions/cache@v3
        env:
          cache-name: cache-paket
        with:
          path: packages
          key: ${{ runner.os }}-build-${{ env.cache-name }}-${{ hashFiles('paket.lock') }}
          restore-keys: |
            ${{ runner.os }}-build-${{ env.cache-name }}-

      - name: Cache Python
        uses: actions/cache@v3
        env:
          cache-name: cache-pip
        with:
          path: ~/.cache/pip
          key: ${{ runner.os }}-build-${{ env.cache-name }}

      - name: Setup Python
        uses: actions/setup-python@v3
        with:
          python-version: '3.x'

      - name: Install Python dependencies
        shell: bash
        run: |
          python -m pip install --upgrade pip
          pip install requests markdown argparse

      - name: Publish to Confluence
        if: needs.build.outputs.version != 'none'
        shell: bash
        run: ./packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/ci-confluence.sh
        env:
          CONFLUENCE_TITLE: ${{ env.REPOSITORY_NAME }}
          CONFLUENCE_USERNAME: ${{ secrets.VBR_CONFLUENCE_USER }}
          CONFLUENCE_PASSWORD: ${{ secrets.VBR_CONFLUENCE_PASSWORD }}

      - name: Create Jira Release
        if: env.RELEASE_VERSION != 'none'
        shell: bash
        run: ./packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/ci-jira.sh
        env:
          CONFLUENCE_TITLE: ${{ env.REPOSITORY_NAME }}
          CONFLUENCE_USERNAME: ${{ secrets.VBR_CONFLUENCE_USER }}
          CONFLUENCE_PASSWORD: ${{ secrets.VBR_CONFLUENCE_PASSWORD }}
          JIRA_PREFIX: StreetName
          JIRA_PROJECT: GAWR
          JIRA_VERSION: ${{ needs.build.outputs.version }}

  publish_to_nuget:
    if: needs.build.outputs.version != 'none'
    needs: build
    name: Publish to NuGet
    runs-on: ubuntu-latest
    
    steps:
    - name: Checkout Code
      uses: actions/checkout@v3

    - name: Setup .NET Core
      uses: actions/setup-dotnet@v2
      with:
        dotnet-version: ${{ secrets.VBR_DOTNET_VERSION }}

    - name: .NET version
      shell: bash
      run: dotnet --info

    - name: Download NuGet package api-backoffice
      if: env.RELEASE_VERSION != 'none'
      uses: actions/download-artifact@v3
      with:
        name: nuget-api-backoffice
        path: ~/

    - name: Download NuGet package api-backoffice-abstractions
      if: env.RELEASE_VERSION != 'none'
      uses: actions/download-artifact@v3
      with:
        name: nuget-api-backoffice-abstractions
        path: ~/

    - name: Download NuGet package api-legacy
      if: env.RELEASE_VERSION != 'none'
      uses: actions/download-artifact@v3
      with:
        name: nuget-api-legacy
        path: ~/

    - name: Download NuGet package api-oslo
      if: env.RELEASE_VERSION != 'none'
      uses: actions/download-artifact@v3
      with:
        name: nuget-api-oslo
        path: ~/

    - name: Download NuGet package api-extract
      if: env.RELEASE_VERSION != 'none'
      uses: actions/download-artifact@v3
      with:
        name: nuget-api-extract
        path: ~/

    - name: Download NuGet package api-crab-import
      if: env.RELEASE_VERSION != 'none'
      uses: actions/download-artifact@v3
      with:
        name: nuget-api-crab-import
        path: ~/

    - name: Download NuGet package projector
      if: env.RELEASE_VERSION != 'none'
      uses: actions/download-artifact@v3
      with:
        name: nuget-projector
        path: ~/

    - name: Publish packages to NuGet
      shell: bash
      run: |
        dotnet nuget push ~/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.BackOffice.$SEMVER.nupkg --source nuget.org --api-key $NUGET_API_KEY
        dotnet nuget push ~/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.BackOffice.Abstractions.$SEMVER.nupkg --source nuget.org --api-key $NUGET_API_KEY
        dotnet nuget push ~/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Legacy.$SEMVER.nupkg --source nuget.org --api-key $NUGET_API_KEY
        dotnet nuget push ~/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Oslo.$SEMVER.nupkg --source nuget.org --api-key $NUGET_API_KEY
        dotnet nuget push ~/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Extract.$SEMVER.nupkg --source nuget.org --api-key $NUGET_API_KEY
        dotnet nuget push ~/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.CrabImport.$SEMVER.nupkg --source nuget.org --api-key $NUGET_API_KEY
        dotnet nuget push ~/Be.Vlaanderen.Basisregisters.StreetNameRegistry.Projector.$SEMVER.nupkg --source nuget.org --api-key $NUGET_API_KEY
      env:
        SEMVER: ${{ needs.build.outputs.version }}
        WORKSPACE: ${{ github.workspace }}
        NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}

  push_images_to_test:
    if: needs.build.outputs.version != 'none'
    needs: build
    name: Push images to Test
    runs-on: ubuntu-latest
    steps:
      - name: Configure AWS credentials (Test)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/configure-aws-credentials@v1-node16
        with:
          aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
          aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
          aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}

      - name: Login to Amazon ECR (Test)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/amazon-ecr-login@v1.5.2

      #
      # Download artifacts
      #
      
      - name: Download api-backoffice
        uses: actions/download-artifact@v3
        with:
          name: api-backoffice
          path: ~/
      
      - name: Download api-legacy
        uses: actions/download-artifact@v3
        with:
          name: api-legacy
          path: ~/
      
      - name: Download api-oslo
        uses: actions/download-artifact@v3
        with:
          name: api-oslo
          path: ~/
      
      - name: Download api-crab-import
        uses: actions/download-artifact@v3
        with:
          name: api-crab-import
          path: ~/
      
      - name: Download api-extract
        uses: actions/download-artifact@v3
        with:
          name: api-extract
          path: ~/
      
      - name: Download projector
        uses: actions/download-artifact@v3
        with:
          name: projector
          path: ~/
      
      - name: Download projections-syndication
        uses: actions/download-artifact@v3
        with:
          name: projections-syndication
          path: ~/
      
      - name: Download consumer
        uses: actions/download-artifact@v3
        with:
          name: consumer
          path: ~/
      
      - name: Download producer
        uses: actions/download-artifact@v3
        with:
          name: producer
          path: ~/

      - name: Download producer-snapshot-oslo
        uses: actions/download-artifact@v3
        with:
          name: producer-snapshot-oslo
          path: ~/
      
      - name: Download migrator-streetname
        uses: actions/download-artifact@v3
        with:
          name: migrator-streetname
          path: ~/

      #
      # Load artifacts
      #
      
      - name: Load artifacts
        shell: bash
        run: |
          docker image load -i ~/sr-api-backoffice-image.tar
          docker image load -i ~/sr-api-legacy-image.tar
          docker image load -i ~/sr-api-oslo-image.tar
          docker image load -i ~/sr-api-crab-import-image.tar
          docker image load -i ~/sr-api-extract-image.tar
          docker image load -i ~/sr-projector-image.tar
          docker image load -i ~/sr-projections-syndication-image.tar
          docker image load -i ~/sr-consumer-image.tar
          docker image load -i ~/sr-producer-image.tar
          docker image load -i ~/sr-producer-snapshot-oslo-image.tar
          docker image load -i ~/sr-migrator-streetname-image.tar

      - name: Push artifacts to ECR Test
        if: needs.build.outputs.version != 'none'
        shell: bash
        run: |
          echo $SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-backoffice:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-legacy:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-oslo:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-crab-import:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-extract:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/projector:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/projections-syndication:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/consumer:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/producer:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/producer-snapshot-oslo:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/migrator-streetname:$SEMVER
        env:
          BUILD_DOCKER_REGISTRY: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
          SEMVER: ${{ needs.build.outputs.version }}
          WORKSPACE: ${{ github.workspace }}

  push_images_to_staging:
    if: needs.build.outputs.version != 'none'
    needs: build
    name: Push images to Staging
    runs-on: ubuntu-latest
    steps:
      - name: Configure AWS credentials (Staging)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/configure-aws-credentials@v1-node16
        with:
          aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}

      - name: Login to Amazon ECR (Staging)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/amazon-ecr-login@v1.5.2

      #
      # Download artifacts
      #
      
      - name: Download api-backoffice
        uses: actions/download-artifact@v3
        with:
          name: api-backoffice
          path: ~/
      
      - name: Download api-legacy
        uses: actions/download-artifact@v3
        with:
          name: api-legacy
          path: ~/
      
      - name: Download api-oslo
        uses: actions/download-artifact@v3
        with:
          name: api-oslo
          path: ~/
      
      - name: Download api-crab-import
        uses: actions/download-artifact@v3
        with:
          name: api-crab-import
          path: ~/
      
      - name: Download api-extract
        uses: actions/download-artifact@v3
        with:
          name: api-extract
          path: ~/
      
      - name: Download projector
        uses: actions/download-artifact@v3
        with:
          name: projector
          path: ~/
      
      - name: Download projections-syndication
        uses: actions/download-artifact@v3
        with:
          name: projections-syndication
          path: ~/
      
      - name: Download consumer
        uses: actions/download-artifact@v3
        with:
          name: consumer
          path: ~/
      
      - name: Download producer
        uses: actions/download-artifact@v3
        with:
          name: producer
          path: ~/

      - name: Download producer-snapshot-oslo
        uses: actions/download-artifact@v3
        with:
          name: producer-snapshot-oslo
          path: ~/
      
      - name: Download migrator-streetname
        uses: actions/download-artifact@v3
        with:
          name: migrator-streetname
          path: ~/

      #
      # Load artifacts
      #
      
      - name: Load artifacts
        shell: bash
        run: |
          docker image load -i ~/sr-api-backoffice-image.tar
          docker image load -i ~/sr-api-legacy-image.tar
          docker image load -i ~/sr-api-oslo-image.tar
          docker image load -i ~/sr-api-crab-import-image.tar
          docker image load -i ~/sr-api-extract-image.tar
          docker image load -i ~/sr-projector-image.tar
          docker image load -i ~/sr-projections-syndication-image.tar
          docker image load -i ~/sr-consumer-image.tar
          docker image load -i ~/sr-producer-image.tar
          docker image load -i ~/sr-producer-snapshot-oslo-image.tar
          docker image load -i ~/sr-migrator-streetname-image.tar

      - name: Push artifacts to ECR Staging
        if: needs.build.outputs.version != 'none'
        shell: bash
        run: |
          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-backoffice:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/api-backoffice:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-backoffice:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-legacy:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/api-legacy:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-legacy:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-oslo:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/api-oslo:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-oslo:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-crab-import:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/api-crab-import:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-crab-import:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-extract:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/api-extract:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/api-extract:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/projector:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/projector:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/projector:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/projections-syndication:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/projections-syndication:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/projections-syndication:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/consumer:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/consumer:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/consumer:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/producer:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/producer:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/producer:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/producer-snapshot-oslo:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/producer-snapshot-oslo:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/producer-snapshot-oslo:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/migrator-streetname:$SEMVER $BUILD_DOCKER_REGISTRY/streetname-registry/migrator-streetname:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/streetname-registry/migrator-streetname:$SEMVER
        env:
          BUILD_DOCKER_REGISTRY_TST: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
          BUILD_DOCKER_REGISTRY: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY }}
          SEMVER: ${{ needs.build.outputs.version }}
          WORKSPACE: ${{ github.workspace }}

  push_images_to_production:
    if: needs.build.outputs.version != 'none'
    needs: build
    name: Push images to Production
    runs-on: ubuntu-latest
    steps:
      - name: Configure AWS credentials (Production)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/configure-aws-credentials@v1-node16
        with:
          aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_PRD }}
          aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_PRD }}
          aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}

      - name: Login to Amazon ECR (Production)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/amazon-ecr-login@v1.5.2

      #
      # Download artifacts
      #
      
      - name: Download api-backoffice
        uses: actions/download-artifact@v3
        with:
          name: api-backoffice
          path: ~/
      
      - name: Download api-legacy
        uses: actions/download-artifact@v3
        with:
          name: api-legacy
          path: ~/
      
      - name: Download api-oslo
        uses: actions/download-artifact@v3
        with:
          name: api-oslo
          path: ~/
      
      - name: Download api-crab-import
        uses: actions/download-artifact@v3
        with:
          name: api-crab-import
          path: ~/
      
      - name: Download api-extract
        uses: actions/download-artifact@v3
        with:
          name: api-extract
          path: ~/
      
      - name: Download projector
        uses: actions/download-artifact@v3
        with:
          name: projector
          path: ~/
      
      - name: Download projections-syndication
        uses: actions/download-artifact@v3
        with:
          name: projections-syndication
          path: ~/
      
      - name: Download consumer
        uses: actions/download-artifact@v3
        with:
          name: consumer
          path: ~/
      
      - name: Download producer
        uses: actions/download-artifact@v3
        with:
          name: producer
          path: ~/

      - name: Download producer-snapshot-oslo
        uses: actions/download-artifact@v3
        with:
          name: producer-snapshot-oslo
          path: ~/
      
      - name: Download migrator-streetname
        uses: actions/download-artifact@v3
        with:
          name: migrator-streetname
          path: ~/

      #
      # Load artifacts
      #
      
      - name: Load artifacts
        shell: bash
        run: |
          docker image load -i ~/sr-api-backoffice-image.tar
          docker image load -i ~/sr-api-legacy-image.tar
          docker image load -i ~/sr-api-oslo-image.tar
          docker image load -i ~/sr-api-crab-import-image.tar
          docker image load -i ~/sr-api-extract-image.tar
          docker image load -i ~/sr-projector-image.tar
          docker image load -i ~/sr-projections-syndication-image.tar
          docker image load -i ~/sr-consumer-image.tar
          docker image load -i ~/sr-producer-image.tar
          docker image load -i ~/sr-producer-snapshot-oslo-image.tar
          docker image load -i ~/sr-migrator-streetname-image.tar

      - name: Push artifacts to ECR Production
        if: needs.build.outputs.version != 'none'
        shell: bash
        run: |
          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-backoffice:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-backoffice:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-backoffice:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-legacy:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-legacy:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-legacy:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-oslo:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-oslo:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-oslo:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-crab-import:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-crab-import:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-crab-import:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/api-extract:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-extract:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/api-extract:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/projector:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/projector:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/projector:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/projections-syndication:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/projections-syndication:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/projections-syndication:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/consumer:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/consumer:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/consumer:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/producer:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/producer:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/producer:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/producer-snapshot-oslo:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/producer-snapshot-oslo:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/producer-snapshot-oslo:$SEMVER

          docker tag $BUILD_DOCKER_REGISTRY_TST/streetname-registry/migrator-streetname:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/migrator-streetname:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY_PRD/streetname-registry/migrator-streetname:$SEMVER
        env:
          BUILD_DOCKER_REGISTRY_TST: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
          BUILD_DOCKER_REGISTRY_PRD: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_PRD }}
          SEMVER: ${{ needs.build.outputs.version }}
          WORKSPACE: ${{ github.workspace }}
  
  deploy_to_test_start_slack:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [push_images_to_test, build]
    name: Deploy to test started
    environment: test
    runs-on: ubuntu-latest

    steps:
    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo """"$GITHUB_REPOSITORY"""" | awk -F / '{print $2}' | sed -e """"s/:refs//"""") >> $GITHUB_ENV
      shell: bash

    - name: Notify deployment started
      uses: slackapi/slack-github-action@v1.23.0
      with:
        channel-id: '#team-dinosaur-dev'
        slack-message: Deployment of streetname-registry to test has started
      env:
        SLACK_BOT_TOKEN: ${{ secrets.VBR_SLACK_BOT_TOKEN }}
        SLACK_CHANNEL: ${{ secrets.VBR_NOTIFIER_CHANNEL_NAME }}
        REPOSITORY_NAME: ${{ env.REPOSITORY_NAME }}

  deploy_to_test:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_to_test_start_slack, build]
    name: Deploy to test
    runs-on: ubuntu-latest
    strategy:
      matrix:
        services: ['streetname-registry-api', 'streetname-registry-import-api', 'streetname-registry-projections', 'streetname-registry-producer', 'streetname-registry-producer-snapshot-oslo']
    
    steps:
    - name: Deploy services
      env:
        BUILD_URL: ${{ secrets.VBR_AWS_BUILD_API }}/${{matrix.services}}
        STATUS_URL: ${{ secrets.VBR_AWS_BUILD_STATUS_API }}/${{matrix.services}}
      uses: informatievlaanderen/awscurl-polling-action/polling-action@main
      with:
          environment: test
          version: ${{ needs.build.outputs.version }}
          status-url: $STATUS_URL
          deploy-url: $BUILD_URL
          access-key: ${{ secrets.VBR_AWS_BUILD_USER_ACCESS_KEY_ID }}
          secret-key: ${{ secrets.VBR_AWS_BUILD_USER_SECRET_ACCESS_KEY }}
          region: eu-west-1
          interval: 2

    - name: Deploy services output
      shell: bash
      run: |
        echo build-uuid: ${{ steps.awscurl-polling-action.outputs.build-uuid }}
        echo Status: ${{ steps.awscurl-polling-action.outputs.status }}
        echo ${{ steps.awscurl-polling-action.outputs.final-message }}

  deploy_lambda_to_test:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_to_test, build]
    name: Deploy lambda to test
    runs-on: ubuntu-latest
    
    steps:
    - name: CD Lambda(s) Configure credentials
      uses: aws-actions/configure-aws-credentials@v1-node16
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}
        
    - name: Prepare Lambda(s)
      shell: bash
      run: |
        echo aws s3 cp s3://s3-vbr-test-basisregisters-lam-sr-sqsbackofficehandlerfunction/$VERSION/lambda.zip s3://s3-vbr-test-basisregisters-lam-sr-sqsbackofficehandlerfunction/lambda.zip --copy-props none
        aws s3 cp s3://s3-vbr-test-basisregisters-lam-sr-sqsbackofficehandlerfunction/$VERSION/lambda.zip s3://s3-vbr-test-basisregisters-lam-sr-sqsbackofficehandlerfunction/lambda.zip --copy-props none
      env:
        VERSION: ${{ needs.build.outputs.version }}
        
    - name: Promote Lambda(s)
      shell: bash
      run: |
        echo pulling awscurl docker image
        docker pull ghcr.io/okigan/awscurl:latest
        echo docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""sr-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/test
        docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""sr-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/test
      env:
        ACCESS_KEY_ID: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        SECRET_ACCESS_KEY_ID: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        REGION: ${{ secrets.VBR_AWS_REGION_PRD }}
        PROMOTEURL: ${{ secrets.VBR_AWS_PROMOTE_LAMBDA_BASEURL }}

  deploy_to_test_finish_slack:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_lambda_to_test]
    name: Deploy to test finished
    runs-on: ubuntu-latest

    steps:
    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo """"$GITHUB_REPOSITORY"""" | awk -F / '{print $2}' | sed -e """"s/:refs//"""") >> $GITHUB_ENV
      shell: bash

    - name: Notify deployment finished
      uses: slackapi/slack-github-action@v1.23.0
      with:
        channel-id: '#team-dinosaur-dev'
        slack-message: Deployment of streetname-registry to test has finished
      env:
        SLACK_BOT_TOKEN: ${{ secrets.VBR_SLACK_BOT_TOKEN }}
        SLACK_CHANNEL: ${{ secrets.VBR_NOTIFIER_CHANNEL_NAME }}
        REPOSITORY_NAME: ${{ env.REPOSITORY_NAME }}
  
  deploy_to_staging_start_slack:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [push_images_to_staging, deploy_to_test_finish_slack, build]
    name: Deploy to staging started
    environment: stg
    runs-on: ubuntu-latest

    steps:
    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo """"$GITHUB_REPOSITORY"""" | awk -F / '{print $2}' | sed -e """"s/:refs//"""") >> $GITHUB_ENV
      shell: bash

    - name: Notify deployment started
      uses: slackapi/slack-github-action@v1.23.0
      with:
        channel-id: '#team-dinosaur-dev'
        slack-message: Deployment of streetname-registry to staging has started
      env:
        SLACK_BOT_TOKEN: ${{ secrets.VBR_SLACK_BOT_TOKEN }}
        SLACK_CHANNEL: ${{ secrets.VBR_NOTIFIER_CHANNEL_NAME }}
        REPOSITORY_NAME: ${{ env.REPOSITORY_NAME }}

  deploy_to_staging:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_to_staging_start_slack, build]
    name: Deploy to staging
    runs-on: ubuntu-latest
    strategy:
      matrix:
        services: ['streetname-registry-api', 'streetname-registry-projections', 'streetname-registry-backoffice-api', 'streetname-registry-consumer', 'streetname-registry-producer', 'streetname-registry-migrator-streetname', 'streetname-registry-producer-snapshot-oslo']

    steps:
    - name: CD services
      env:
        BUILD_URL: ${{ secrets.VBR_AWS_BUILD_API }}/${{matrix.services}}
        STATUS_URL: ${{ secrets.VBR_AWS_BUILD_STATUS_API }}/${{matrix.services}}
      uses: informatievlaanderen/awscurl-polling-action/polling-action@main
      with:
          environment: stg
          version: ${{ needs.build.outputs.version }}
          status-url: $STATUS_URL
          deploy-url: $BUILD_URL
          access-key: ${{ secrets.VBR_AWS_BUILD_USER_ACCESS_KEY_ID }}
          secret-key: ${{ secrets.VBR_AWS_BUILD_USER_SECRET_ACCESS_KEY }}
          region: eu-west-1
          interval: 2
          
    - name: output CD services
      shell: bash
      run: |
        echo build-uuid: ${{ steps.awscurl-polling-action.outputs.build-uuid }}
        echo Status: ${{ steps.awscurl-polling-action.outputs.status }}
        echo ${{ steps.awscurl-polling-action.outputs.final-message }}
        
  deploy_lambda_to_staging:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_to_staging, build]
    name: Deploy lambda to staging
    runs-on: ubuntu-latest

    steps:
    - name: CD Lambda(s) Configure credentials
      uses: aws-actions/configure-aws-credentials@v1-node16
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY }}
        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}
        
    - name: Prepare Lambda(s)
      shell: bash
      run: |
        echo aws s3 cp s3://s3-vbr-stg-basisregisters-lam-sr-sqsbackofficehandlerfunction/$VERSION/lambda.zip s3://s3-vbr-stg-basisregisters-lam-sr-sqsbackofficehandlerfunction/lambda.zip --copy-props none
        aws s3 cp s3://s3-vbr-stg-basisregisters-lam-sr-sqsbackofficehandlerfunction/$VERSION/lambda.zip s3://s3-vbr-stg-basisregisters-lam-sr-sqsbackofficehandlerfunction/lambda.zip --copy-props none
      env:
        VERSION: ${{ needs.build.outputs.version }}
        
    - name: Promote Lambda(s)
      shell: bash
      run: |
        echo pulling awscurl docker image
        docker pull ghcr.io/okigan/awscurl:latest
        echo docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""sr-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/stg
        docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""sr-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/stg
      env:
        ACCESS_KEY_ID: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        SECRET_ACCESS_KEY_ID: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        REGION: ${{ secrets.VBR_AWS_REGION_PRD }}
        PROMOTEURL: ${{ secrets.VBR_AWS_PROMOTE_LAMBDA_BASEURL }}

  deploy_to_staging_finish_slack:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_lambda_to_staging]
    name: Deploy to staging finished
    runs-on: ubuntu-latest

    steps:
    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo """"$GITHUB_REPOSITORY"""" | awk -F / '{print $2}' | sed -e """"s/:refs//"""") >> $GITHUB_ENV
      shell: bash

    - name: Notify deployment finished
      uses: slackapi/slack-github-action@v1.23.0
      with:
        channel-id: '#team-dinosaur-dev'
        slack-message: Deployment of streetname-registry to staging has finished
      env:
        SLACK_BOT_TOKEN: ${{ secrets.VBR_SLACK_BOT_TOKEN }}
        SLACK_CHANNEL: ${{ secrets.VBR_NOTIFIER_CHANNEL_NAME }}
        REPOSITORY_NAME: ${{ env.REPOSITORY_NAME }}

  deploy_to_production_start_slack:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [push_images_to_production, deploy_to_staging_finish_slack, build]
    name: Deploy to production started
    environment: prd
    runs-on: ubuntu-latest

    steps:
    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo """"$GITHUB_REPOSITORY"""" | awk -F / '{print $2}' | sed -e """"s/:refs//"""") >> $GITHUB_ENV
      shell: bash

    - name: Notify deployment started
      uses: slackapi/slack-github-action@v1.23.0
      with:
        channel-id: '#team-dinosaur-dev'
        slack-message: Deployment of streetname-registry to production has started
      env:
        SLACK_BOT_TOKEN: ${{ secrets.VBR_SLACK_BOT_TOKEN }}
        SLACK_CHANNEL: ${{ secrets.VBR_NOTIFIER_CHANNEL_NAME }}
        REPOSITORY_NAME: ${{ env.REPOSITORY_NAME }}
        
  deploy_to_production:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_to_production_start_slack, build]
    name: Deploy to Production
    runs-on: ubuntu-latest
    strategy:
      matrix: 
        services: ['streetname-registry-api', 'streetname-registry-import-api', 'streetname-registry-projections']

    steps:
    - name: CD services
      env:
        BUILD_URL: ${{ secrets.VBR_AWS_BUILD_API }}/${{matrix.services}}
        STATUS_URL: ${{ secrets.VBR_AWS_BUILD_STATUS_API }}/${{matrix.services}}
      uses: informatievlaanderen/awscurl-polling-action/polling-action@main
      with:
          environment: prd
          version: ${{ needs.build.outputs.version }}
          status-url: $STATUS_URL
          deploy-url: $BUILD_URL
          access-key: ${{ secrets.VBR_AWS_BUILD_USER_ACCESS_KEY_ID }}
          secret-key: ${{ secrets.VBR_AWS_BUILD_USER_SECRET_ACCESS_KEY }}
          region: eu-west-1
          interval: 2

    - name: output CD services
      shell: bash
      run: |
        echo build-uuid: ${{ steps.awscurl-polling-action.outputs.build-uuid }}
        echo Status: ${{ steps.awscurl-polling-action.outputs.status }}
        echo ${{ steps.awscurl-polling-action.outputs.final-message }}

  deploy_lambda_to_production:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_to_production, build]
    name: Deploy lambda to production
    runs-on: ubuntu-latest

    steps:
    - name: CD Lambda(s) Configure credentials
      uses: aws-actions/configure-aws-credentials@v1-node16
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_PRD }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_PRD }}
        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}
        
    - name: Prepare Lambda(s)
      shell: bash
      run: |
        echo aws s3 cp s3://s3-vbr-prd-basisregisters-lam-sr-sqsbackofficehandlerfunction/$VERSION/lambda.zip s3://s3-vbr-prd-basisregisters-lam-sr-sqsbackofficehandlerfunction/lambda.zip --copy-props none
        aws s3 cp s3://s3-vbr-prd-basisregisters-lam-sr-sqsbackofficehandlerfunction/$VERSION/lambda.zip s3://s3-vbr-prd-basisregisters-lam-sr-sqsbackofficehandlerfunction/lambda.zip --copy-props none
      env:
        VERSION: ${{ needs.build.outputs.version }}
        
    - name: Promote Lambda(s)
      shell: bash
      run: |
        echo pulling awscurl docker image
        docker pull ghcr.io/okigan/awscurl:latest
        echo docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""sr-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/prd
        docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""sr-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/prd
      env:
        ACCESS_KEY_ID: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        SECRET_ACCESS_KEY_ID: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        REGION: ${{ secrets.VBR_AWS_REGION_PRD }}
        PROMOTEURL: ${{ secrets.VBR_AWS_PROMOTE_LAMBDA_BASEURL }}

  deploy_to_production_finish_slack:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [deploy_lambda_to_production]
    name: Deploy to production finished
    runs-on: ubuntu-latest

    steps:
    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo """"$GITHUB_REPOSITORY"""" | awk -F / '{print $2}' | sed -e """"s/:refs//"""") >> $GITHUB_ENV
      shell: bash

    - name: Notify deployment finished
      uses: slackapi/slack-github-action@v1.23.0
      with:
        channel-id: '#team-dinosaur-dev'
        slack-message: Deployment of streetname-registry to production has finished
      env:
        SLACK_BOT_TOKEN: ${{ secrets.VBR_SLACK_BOT_TOKEN }}
        SLACK_CHANNEL: ${{ secrets.VBR_NOTIFIER_CHANNEL_NAME }}
        REPOSITORY_NAME: ${{ env.REPOSITORY_NAME }}

";

        var options = new ReleaseGeneratorOptions(
            "Release",
            "streetname-registry",
            "sr",
            new[]
            {
                "api-backoffice",
                "api-legacy",
                "api-oslo",
                "api-crab-import",
                "api-extract",
                "projector",
                "projections-syndication",
                "consumer",
                "producer",
                "producer-snapshot-oslo",
                "migrator-streetname"
            },
            new[]
            { 
                new NuGetArtifactAndPackage("api-backoffice", "Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.BackOffice"),
                new NuGetArtifactAndPackage("api-backoffice-abstractions", "Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.BackOffice.Abstractions"),
                new NuGetArtifactAndPackage("api-legacy", "Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Legacy"),
                new NuGetArtifactAndPackage("api-oslo", "Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Oslo"),
                new NuGetArtifactAndPackage("api-extract", "Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.Extract"),
                new NuGetArtifactAndPackage("api-crab-import", "Be.Vlaanderen.Basisregisters.StreetNameRegistry.Api.CrabImport"),
                new NuGetArtifactAndPackage("projector", "Be.Vlaanderen.Basisregisters.StreetNameRegistry.Projector")
            },
            false,
            "StreetName",
            "GAWR",
            "/home/runner/work/streetname-registry/streetname-registry/dist/StreetNameRegistry.Api.BackOffice.Handlers.Lambda/linux",
            new EnvironmentOptions("s3://s3-vbr-test-basisregisters-lam-sr-sqsbackofficehandlerfunction",
                new[] { "streetname-registry-api", "streetname-registry-import-api", "streetname-registry-projections", "streetname-registry-producer", "streetname-registry-producer-snapshot-oslo" }),
            new EnvironmentOptions("s3://s3-vbr-stg-basisregisters-lam-sr-sqsbackofficehandlerfunction",
                new[] { "streetname-registry-api", "streetname-registry-projections", "streetname-registry-backoffice-api", "streetname-registry-consumer", "streetname-registry-producer", "streetname-registry-migrator-streetname", "streetname-registry-producer-snapshot-oslo" }),
            new EnvironmentOptions("s3://s3-vbr-prd-basisregisters-lam-sr-sqsbackofficehandlerfunction",
                new[] { "streetname-registry-api", "streetname-registry-import-api", "streetname-registry-projections" }));
        var result = await new GithubGenerator().GenerateReleaseWorkflowAsync(options);

        Assert.NotNull(result);
        Assert.Equal(expected.ExceptCharacters(new []{ '#', ' ', '\r', '\n' }), result.ExceptCharacters(new []{ '#', ' ', '\r', '\n' }));
    }

    [Fact]
    public async Task GenerateReleaseLibWorkflow()
    {
        const string expected = @"name: Release

on:
  workflow_dispatch:

jobs:
  build:
    if: github.repository_owner == 'Informatievlaanderen'
    name: Release
    runs-on: ubuntu-latest

    steps:
    - name: Checkout Code
      uses: actions/checkout@v3

    - name: Cache NPM
      uses: actions/cache@v3
      env:
        cache-name: cache-npm
      with:
        path: ~/.npm
        key: ${{ runner.os }}-build-${{ env.cache-name }}-${{ hashFiles('**/package-lock.json') }}
        restore-keys: |
          ${{ runner.os }}-build-${{ env.cache-name }}-

    - name: Cache Paket
      uses: actions/cache@v3
      env:
        cache-name: cache-paket
      with:
        path: packages
        key: ${{ runner.os }}-build-${{ env.cache-name }}-${{ hashFiles('paket.lock') }}
        restore-keys: |
          ${{ runner.os }}-build-${{ env.cache-name }}-
          
    - name: Cache Python
      uses: actions/cache@v3
      env:
        cache-name: cache-pip
      with:
        path: ~/.cache/pip
        key: ${{ runner.os }}-build-${{ env.cache-name }}

    - name: Parse repository name
      run: echo REPOSITORY_NAME=$(echo ""$GITHUB_REPOSITORY"" | awk -F / '{print $2}' | sed -e ""s/:refs//"") >> $GITHUB_ENV
      shell: bash

    - name: Setup Node.js
      uses: actions/setup-node@v3

    - name: Setup .NET Core
      uses: actions/setup-dotnet@v2
      with:
        dotnet-version: ${{ secrets.VBR_DOTNET_VERSION }}

    - name: Set up Python
      uses: actions/setup-python@v3
      with:
        python-version: '3.x'

    - name: Node version
      shell: bash
      run: node --version

    - name: .NET version
      shell: bash
      run: dotnet --info

    - name: Python version
      shell: bash
      run: python --version

    - name: Install NPM dependencies
      shell: bash
      run: npm ci

    - name: Install Python dependencies
      shell: bash
      run: |
        python -m pip install --upgrade pip
        pip install requests markdown argparse
        
    - name: Run Semantic Release
      shell: bash
      run: npx semantic-release
      env:
        BUILD_DOCKER_REGISTRY: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
        GITHUB_TOKEN: ${{ secrets.VBR_GIT_RELEASE_TOKEN }}
        GIT_COMMIT: ${{ github.sha }}
        GIT_USERNAME: ${{ secrets.VBR_GIT_USER }}
        GIT_AUTHOR_NAME: ${{ secrets.VBR_GIT_USER }}
        GIT_COMMITTER_NAME: ${{ secrets.VBR_GIT_USER }}
        GIT_EMAIL: ${{ secrets.VBR_GIT_EMAIL }}
        GIT_AUTHOR_EMAIL: ${{ secrets.VBR_GIT_EMAIL }}
        GIT_COMMITTER_EMAIL: ${{ secrets.VBR_GIT_EMAIL }}
        
    - name: Set Release Version
      run: |
        [ ! -f semver ] && echo none > semver
        echo RELEASE_VERSION=$(cat semver) >> $GITHUB_ENV
      shell: bash

    - name: Publish packages to NuGet
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        dotnet nuget push dist/Be.Vlaanderen.Basisregisters.Sqs/Be.Vlaanderen.Basisregisters.Sqs.$SEMVER.nupkg  --source nuget.org --api-key $NUGET_API_KEY
        dotnet nuget push dist/Be.Vlaanderen.Basisregisters.Sqs.Lambda/Be.Vlaanderen.Basisregisters.Sqs.Lambda.$SEMVER.nupkg  --source nuget.org --api-key $NUGET_API_KEY
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}
        WORKSPACE: ${{ github.workspace }}
        NUGET_HOST: ${{ secrets.NUGET_HOST }}
        NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}

    - name: Publish to Confluence
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: ./packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/ci-confluence.sh
      env:
        CONFLUENCE_TITLE: ${{ env.REPOSITORY_NAME }}
        CONFLUENCE_USERNAME: ${{ secrets.VBR_CONFLUENCE_USER }}
        CONFLUENCE_PASSWORD: ${{ secrets.VBR_CONFLUENCE_PASSWORD }}

    - name: Create Jira Release
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: ./packages/Be.Vlaanderen.Basisregisters.Build.Pipeline/Content/ci-jira.sh
      env:
        CONFLUENCE_TITLE: ${{ env.REPOSITORY_NAME }}
        CONFLUENCE_USERNAME: ${{ secrets.VBR_CONFLUENCE_USER }}
        CONFLUENCE_PASSWORD: ${{ secrets.VBR_CONFLUENCE_PASSWORD }}
        JIRA_PREFIX: Address
        JIRA_PROJECT: GAWR
        JIRA_VERSION: ${{ env.RELEASE_VERSION }}
";

        var options = new ReleaseLibGeneratorOptions(
            "Release",
            new[]
            { 
                "Be.Vlaanderen.Basisregisters.Sqs",
                "Be.Vlaanderen.Basisregisters.Sqs.Lambda"
            },
            "Address",
            "GAWR");
        var result = await new GithubGenerator().GenerateReleaseLibWorkflowAsync(options);

        Assert.NotNull(result);
        Assert.Equal(expected.ExceptCharacters(new []{ '#', ' ', '\r', '\n' }), result.ExceptCharacters(new []{ '#', ' ', '\r', '\n' }));
    }
}
