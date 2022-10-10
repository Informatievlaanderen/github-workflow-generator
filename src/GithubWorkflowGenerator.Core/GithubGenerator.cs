﻿using System.Threading.Tasks;
using Stubble.Core.Builders;

namespace GithubWorkflowGenerator.Core;

public class GithubGenerator
{
    public async Task<string> GenerateBuildWorkflowAsync(BuildGeneratorOptions options)
    {
        const string template = @"{{=<% %>=}}
name: Build

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
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      shell: bash
      run: |
        ./.sonar/scanner/dotnet-sonarscanner begin /k:""Informatievlaanderen_<% SonarKey %>"" /o:""informatievlaanderen"" /d:sonar.login=""${{ secrets.SONAR_TOKEN }}"" /d:sonar.host.url=""https://sonarcloud.io"" > /dev/null 2>&1

    - name: Build
      shell: bash
      run: |
        dotnet build --nologo --no-restore --configuration Debug <% SolutionName %>

    - name: Sonar end build & analyze
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}  # Needed to get PR information, if any
        SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
      shell: bash
      run: |
        ./.sonar/scanner/dotnet-sonarscanner end /d:sonar.login=""${{ secrets.SONAR_TOKEN }}"" > /dev/null 2>&1

    - name: Test
      shell: bash
      run: dotnet test --nologo --no-build <% SolutionName %>
";

        var stubble = new StubbleBuilder().Build();

        var optionsMap = options.ToKeyValues();
        var result = await stubble.RenderAsync(template, optionsMap);

        return result;
    }

    public async Task<string> GenerateReleaseWorkflowAsync(ReleaseGeneratorOptions options)
    {
        const string template = @"{{=<% %>=}}
name: Release

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
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
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
        <% #BuildArtifacts %>
        docker image save $BUILD_DOCKER_REGISTRY/<% RepositoryName %>/<% . %>:$SEMVER -o ~/<% RepositoryPrefix %>-<% . %>-image.tar
        <% /BuildArtifacts %>
      env:
        BUILD_DOCKER_REGISTRY: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
        SEMVER: ${{ env.RELEASE_VERSION }}

    #
    # Upload NuGet packages
    #

    <% #NuGetPackages %>
    - name: Upload NuGet package <% Package %>
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: nuget-<% Artifact %>
        path: /home/runner/work/<% RepositoryName %>/<% RepositoryName %>/dist/nuget/<% Package %>.*.nupkg
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    <% /NuGetPackages %>
    #
    # Upload build artifacts
    #

    <% #BuildArtifacts %>
    - name: Upload <% . %> artifact
      if: env.RELEASE_VERSION != 'none'
      uses: actions/upload-artifact@v3
      with:
        name: <% . %>
        path: ~/<% RepositoryPrefix %>-<% . %>-image.tar

    <% /BuildArtifacts %>
    - name: Package Lambda functions        
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        echo Zip lambda functions
        pushd <% LambdaSourceFolder %>
        echo zip -r lambda.zip .
        zip -r lambda.zip .
        popd

    - name: Configure AWS credentials (Test)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/configure-aws-credentials@v1
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}

    - name: Login to Amazon ECR (Test)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/amazon-ecr-login@v1.5.1

    - name: Push Lambda functions to S3 Test
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        echo Push Lambda functions to S3 Test
        pushd <% PublishFolderForLambdaTest %>
        echo aws s3 cp lambda.zip <% S3BucketForLambdaTest %>/$SEMVER/lambda.zip
        aws s3 cp lambda.zip <% S3BucketForLambdaTest %>/$SEMVER/lambda.zip
        popd
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

    - name: Configure AWS credentials (Staging)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/configure-aws-credentials@v1
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY }}
        aws-region: ${{ secrets.VBR_AWS_REGION }}

    - name: Login to Amazon ECR (Staging)
      if: env.RELEASE_VERSION != 'none'
      uses: aws-actions/amazon-ecr-login@v1.5.1

    - name: Push Lambda functions to S3 Staging
      if: env.RELEASE_VERSION != 'none'
      shell: bash
      run: |
        echo Push Lambda functions to S3 Staging
        pushd <% PublishFolderForLambdaStaging %>
        echo aws s3 cp lambda.zip <% S3BucketForLambdaStaging %>/$SEMVER/lambda.zip
        aws s3 cp lambda.zip <% S3BucketForLambdaStaging %>/$SEMVER/lambda.zip
        popd
      env:
        SEMVER: ${{ env.RELEASE_VERSION }}

#    - name: Configure AWS credentials (Production)
#      if: env.RELEASE_VERSION != 'none'
#      uses: aws-actions/configure-aws-credentials@v1
#      with:
#        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_PRD }}
#        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_PRD }}
#        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}
#
#    - name: Login to Amazon ECR (Production)
#      if: env.RELEASE_VERSION != 'none'
#      uses: aws-actions/amazon-ecr-login@v1.5.1
#
#    - name: Push Lambda functions to S3 Production
#      if: env.RELEASE_VERSION != 'none'
#      shell: bash
#      run: |
#        echo Push Lambda functions to S3 Production
#        pushd <% PublishFolderForLambdaProduction %>
#        echo aws s3 cp lambda.zip <% S3BucketForLambdaProduction %>/$SEMVER/lambda.zip
#        aws s3 cp lambda.zip <% S3BucketForLambdaProduction %>/$SEMVER/lambda.zip
#        popd
#      env:
#       SEMVER: ${{ env.RELEASE_VERSION }}

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

    <% #NuGetPackages %>
    - name: Download NuGet package <% Artifact %>
      if: env.RELEASE_VERSION != 'none'
      uses: actions/download-artifact@v3
      with:
        name: nuget-<% Artifact %>
        path: ~/

    <% /NuGetPackages %>

    - name: Publish packages to NuGet
      shell: bash
      run: |
        <% #NuGetPackages %>
        dotnet nuget push ~/<% Package %>.$SEMVER.nupkg --source nuget.org --api-key $NUGET_API_KEY
        <% /NuGetPackages %>
      env:
        SEMVER: ${{ needs.build.outputs.version }}
        WORKSPACE: ${{ github.workspace }}
        NUGET_API_KEY: ${{ secrets.NUGET_API_KEY }}

#  publish_to_github:
#    if: needs.build.outputs.version != 'none'
#    needs: build
#    name: Publish to Github
#    runs-on: ubuntu-latest
#
#    steps:
#    - name: Checkout Code
#      uses: actions/checkout@v3
#
#    - name: Setup .NET Core
#      uses: actions/setup-dotnet@v2
#      with:
#        dotnet-version: ${{ secrets.VBR_DOTNET_VERSION }}
#
#    - name: .NET version
#      shell: bash
#      run: dotnet --info
#
#    <% #NuGetPackages %>
#    - name: Download NuGet package <% Artifact %>
#      if: env.RELEASE_VERSION != 'none'
#      uses: actions/download-artifact@v3
#      with:
#        name: nuget-<% Artifact %>
#        path: ~/
#
#    <% /NuGetPackages %>
#
#    - name: Prepare push packages to Github
#      shell: bash
#      run: dotnet nuget add source --username InformatieVlaanderen --password ${{ secrets.GITHUB_TOKEN }} --store-password-in-clear-text --name github ""https://nuget.pkg.github.com/informatievlaanderen/index.json"" 
#
#    - name: Publish packages to GitHub
#      shell: bash
#      run: |
#        echo SEMVER $SEMVER
#        echo GITHUB_TOKEN $GITHUB_TOKEN
#        <% #NuGetPackages %>
#        dotnet nuget push ~/<% Package %>.$SEMVER.nupkg --source ""github"" --api-key $GITHUB_TOKEN
#        <% /NuGetPackages %>
#      env:
#        SEMVER: ${{ needs.build.outputs.version }}
#        WORKSPACE: ${{ github.workspace }}
#        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}

  push_images_to_test:
    if: needs.build.outputs.version != 'none'
    needs: build
    name: Push images to Test
    runs-on: ubuntu-latest
    steps:
      - name: Configure AWS credentials (Test)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/configure-aws-credentials@v1
        with:
          aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
          aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
          aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}

      - name: Login to Amazon ECR (Test)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/amazon-ecr-login@v1.5.1

      #
      # Download artifacts
      #

      <% #BuildArtifacts %>
      - name: Download <% . %>
        uses: actions/download-artifact@v3
        with:
          name: <% . %>
          path: ~/

      <% /BuildArtifacts %>
      #
      # Load artifacts
      #

      - name: Load artifacts
        shell: bash
        run: |
          <% #BuildArtifacts %>
          docker image load -i ~/<% RepositoryPrefix %>-<% . %>-image.tar
          <% /BuildArtifacts %>

      - name: Push artifacts to ECR Test
        if: needs.build.outputs.version != 'none'
        shell: bash
        run: |
          echo $SEMVER
          <% #BuildArtifacts %>
          docker push $BUILD_DOCKER_REGISTRY/<% RepositoryName %>/<% . %>:$SEMVER
          <% /BuildArtifacts %>
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
        uses: aws-actions/configure-aws-credentials@v1
        with:
          aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY }}
          aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}

      - name: Login to Amazon ECR (Staging)
        if: needs.build.outputs.version != 'none'
        uses: aws-actions/amazon-ecr-login@v1.5.1

      #
      # Download artifacts
      #

      <% #BuildArtifacts %>
      - name: Download <% . %>
        uses: actions/download-artifact@v3
        with:
          name: <% . %>
          path: ~/

      <% /BuildArtifacts %>
      #
      # Load artifacts
      #

      - name: Load artifacts
        shell: bash
        run: |
          <% #BuildArtifacts %>
          docker image load -i ~/<% RepositoryPrefix %>-<% . %>-image.tar
          <% /BuildArtifacts %>

      - name: Push artifacts to ECR Staging
        if: needs.build.outputs.version != 'none'
        shell: bash
        run: |
          <% #BuildArtifacts %>
          docker tag $BUILD_DOCKER_REGISTRY_TST/<% RepositoryName %>/<% . %>:$SEMVER $BUILD_DOCKER_REGISTRY/<% RepositoryName %>/<% . %>:$SEMVER
          docker push $BUILD_DOCKER_REGISTRY/<% RepositoryName %>/<% . %>:$SEMVER

          <% /BuildArtifacts %>
        env:
          BUILD_DOCKER_REGISTRY_TST: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
          BUILD_DOCKER_REGISTRY: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY }}
          SEMVER: ${{ needs.build.outputs.version }}
          WORKSPACE: ${{ github.workspace }}

#  push_images_to_production:
#    if: needs.build.outputs.version != 'none'
#    needs: build
#    name: Push images to Production
#    runs-on: ubuntu-latest
#    steps:
#      - name: Configure AWS credentials (Production)
#        if: needs.build.outputs.version != 'none'
#        uses: aws-actions/configure-aws-credentials@v1
#        with:
#          aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_PRD }}
#          aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_PRD }}
#          aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}
#
#      - name: Login to Amazon ECR (Production)
#        if: needs.build.outputs.version != 'none'
#        uses: aws-actions/amazon-ecr-login@v1.5.1
#
#      #
#      # Download artifacts
#      #
#
#      <% #BuildArtifacts %>
#      - name: Download <% . %>
#        uses: actions/download-artifact@v3
#        with:
#          name: <% . %>
#          path: ~/
#
#      <% /BuildArtifacts %>
#      #
#      # Load artifacts
#      #
#
#      - name: Load artifacts
#        shell: bash
#        run: |
#          <% #BuildArtifacts %>
#          docker image load -i ~/<% RepositoryPrefix %>-<% . %>-image.tar
#          <% /BuildArtifacts %>
#
#      - name: Push artifacts to ECR Production
#        if: needs.build.outputs.version != 'none'
#        shell: bash
#        run: |
#          <% #BuildArtifacts %>
#          docker tag $BUILD_DOCKER_REGISTRY_TST/<% RepositoryName %>/<% . %>:$SEMVER $BUILD_DOCKER_REGISTRY_PRD/<% RepositoryName %>/<% . %>:$SEMVER
#          docker push $BUILD_DOCKER_REGISTRY_PRD/<% RepositoryName %>/<% . %>:$SEMVER
#
#          <% /BuildArtifacts %>
#        env:
#          BUILD_DOCKER_REGISTRY_TST: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_TST }}
#          BUILD_DOCKER_REGISTRY_PRD: ${{ secrets.VBR_BUILD_DOCKER_REGISTRY_PRD }}
#          SEMVER: ${{ needs.build.outputs.version }}
#          WORKSPACE: ${{ github.workspace }}

  deploy_to_test:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [push_images_to_test, build]
    name: Deploy to test
    environment: test
    runs-on: ubuntu-latest
    strategy:
      matrix:
        services: [<% &ServiceMatrixTest %>]

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
    environment: test
    runs-on: ubuntu-latest
    
    steps:
    - name: CD Lambda(s) Configure credentials
      uses: aws-actions/configure-aws-credentials@v1
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}
        
    - name: Prepare Lambda(s)
      shell: bash
      run: |
        echo aws s3 cp <% S3BucketForLambdaTest %>/$VERSION/lambda.zip <% S3BucketForLambdaTest %>/lambda.zip --copy-props none
        aws s3 cp <% S3BucketForLambdaTest %>/$VERSION/lambda.zip <% S3BucketForLambdaTest %>/lambda.zip --copy-props none
      env:
        VERSION: ${{ needs.build.outputs.version }}
        
    - name: Promote Lambda(s)
      shell: bash
      run: |
        echo pulling awscurl docker image
        docker pull ghcr.io/okigan/awscurl:latest
        echo docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""<% RepositoryPrefix %>-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/test
        docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""<% RepositoryPrefix %>-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/test
      env:
        ACCESS_KEY_ID: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        SECRET_ACCESS_KEY_ID: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        REGION: ${{ secrets.VBR_AWS_REGION_PRD }}
        PROMOTEURL: ${{ secrets.VBR_AWS_PROMOTE_LAMBDA_BASEURL }}

  deploy_to_staging:
    if: github.repository_owner == 'Informatievlaanderen'
    needs: [push_images_to_staging, deploy_to_test, build]
    name: Deploy to staging
    environment: stg
    runs-on: ubuntu-latest
    strategy:
      matrix:
        services: [<% &ServiceMatrixStaging %>]

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
    environment: stg
    runs-on: ubuntu-latest

    steps:
    - name: CD Lambda(s) Configure credentials
      uses: aws-actions/configure-aws-credentials@v1
      with:
        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID }}
        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY }}
        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}
        
    - name: Prepare Lambda(s)
      shell: bash
      run: |
        echo aws s3 cp <% S3BucketForLambdaStaging %>/$VERSION/lambda.zip <% S3BucketForLambdaStaging %>/lambda.zip --copy-props none
        aws s3 cp <% S3BucketForLambdaStaging %>/$VERSION/lambda.zip <% S3BucketForLambdaStaging %>/lambda.zip --copy-props none
      env:
        VERSION: ${{ needs.build.outputs.version }}
        
    - name: Promote Lambda(s)
      shell: bash
      run: |
        echo pulling awscurl docker image
        docker pull ghcr.io/okigan/awscurl:latest
        echo docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""<% RepositoryPrefix %>-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/stg
        docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""<% RepositoryPrefix %>-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/stg
      env:
        ACCESS_KEY_ID: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
        SECRET_ACCESS_KEY_ID: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
        REGION: ${{ secrets.VBR_AWS_REGION_PRD }}
        PROMOTEURL: ${{ secrets.VBR_AWS_PROMOTE_LAMBDA_BASEURL }}

#  deploy_to_production:
#    if: github.repository_owner == 'Informatievlaanderen'
#    needs: [deploy_to_staging, build]
#    name: Deploy to Production
#    environment: prd
#    runs-on: ubuntu-latest
#    strategy:
#      matrix: 
#        services: [<% &ServiceMatrixProduction %>]
#
#    steps:
#    - name: CD services
#      env:
#        BUILD_URL: ${{ secrets.VBR_AWS_BUILD_API }}/${{matrix.services}}
#        STATUS_URL: ${{ secrets.VBR_AWS_BUILD_STATUS_API }}/${{matrix.services}}
#      uses: informatievlaanderen/awscurl-polling-action/polling-action@main
#      with:
#          environment: prd
#          version: ${{ needs.build.outputs.version }}
#          status-url: $STATUS_URL
#          deploy-url: $BUILD_URL
#          access-key: ${{ secrets.VBR_AWS_BUILD_USER_ACCESS_KEY_ID }}
#          secret-key: ${{ secrets.VBR_AWS_BUILD_USER_SECRET_ACCESS_KEY }}
#          region: eu-west-1
#          interval: 2
#
#    - name: output CD services
#      shell: bash
#      run: |
#        echo build-uuid: ${{ steps.awscurl-polling-action.outputs.build-uuid }}
#        echo Status: ${{ steps.awscurl-polling-action.outputs.status }}
#        echo ${{ steps.awscurl-polling-action.outputs.final-message }}
#
#  deploy_lambda_to_production:
#    if: github.repository_owner == 'Informatievlaanderen'
#    needs: [deploy_to_production, build]
#    name: Deploy lambda to production
#    environment: prd
#    runs-on: ubuntu-latest
#
#    steps:
#    - name: CD Lambda(s) Configure credentials
#      uses: aws-actions/configure-aws-credentials@v1
#      with:
#        aws-access-key-id: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_PRD }}
#        aws-secret-access-key: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_PRD }}
#        aws-region: ${{ secrets.VBR_AWS_REGION_PRD }}
#        
#    - name: Prepare Lambda(s)
#      shell: bash
#      run: |
#        echo aws s3 cp <% S3BucketForLambdaProduction %>/$VERSION/lambda.zip <% S3BucketForLambdaProduction %>/lambda.zip --copy-props none
#        aws s3 cp <% S3BucketForLambdaProduction %>/$VERSION/lambda.zip <% S3BucketForLambdaProduction %>/lambda.zip --copy-props none
#      env:
#        VERSION: ${{ needs.build.outputs.version }}
#        
#    - name: Promote Lambda(s)
#      shell: bash
#      run: |
#        echo pulling awscurl docker image
#        docker pull ghcr.io/okigan/awscurl:latest
#        echo docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""<% RepositoryPrefix %>-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/prd
#        docker run --rm okigan/awscurl --access_key $ACCESS_KEY_ID --secret_key $SECRET_ACCESS_KEY_ID --region $REGION -X POST -d '{ ""functionName"": ""<% RepositoryPrefix %>-sqsbackofficehandlerfunction"", ""project"": ""basisregisters"", ""domain"": ""basisregisters"" }' $PROMOTEURL/prd
#      env:
#        ACCESS_KEY_ID: ${{ secrets.VBR_AWS_ACCESS_KEY_ID_TST }}
#        SECRET_ACCESS_KEY_ID: ${{ secrets.VBR_AWS_SECRET_ACCESS_KEY_TST }}
#        REGION: ${{ secrets.VBR_AWS_REGION_PRD }}
#        PROMOTEURL: ${{ secrets.VBR_AWS_PROMOTE_LAMBDA_BASEURL }}
#  
";

        var stubble = new StubbleBuilder().Build();

        var optionsMap = options.ToKeyValues();
        var result = await stubble.RenderAsync(template, optionsMap);
        
        return result;
    }
}