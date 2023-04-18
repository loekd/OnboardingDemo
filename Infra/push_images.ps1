[CmdletBinding()]
param (
    [Parameter(Mandatory)] 
    [ValidateNotNullOrEmpty()]
    [String]
    $Version,
    [Parameter()]
    [String]
    $Acr = "cronboarding",
    [Parameter()]
    [String]
    $Folder = "D:/Projects/gh/Onboarding",
    [String]
    [ValidateSet('None','ExternalScreeningApi','ExternalScreeningIdp', 'Onboarding', 'All')]
    $PushOption = 'All',
    [switch]    
    $DeployInfra,
    [switch]    
    $ConfigureDevelopmentEnvironmentVariables

)

[Flags()]enum PushOptions
{
  None = 0
  ExternalScreeningApi = 1
  ExternalScreeningIdp = 2
  Onboarding = 4
  All = 7
}

$PushOptions = [PushOptions]::Parse([PushOptions], $PushOption)

$ErrorActionPreference = 'Stop'

function Connect-Acr {
    Write-Host "Login to ACR $script:Acr"

    $context = $(az account show -o json)
    if ($null -eq $context) {
        Write-Host "No Azure context found. Please login to Azure."
        az login
    }

    az acr login -n $script:Acr    
}

function Build-Solution {
    param (
        [String]
        $Configuration = "Release"
    )

    Write-Host "Building Solution $script:Folder/Onboarding.sln with configuration $Configuration and version $script:Version"
    
    dotnet build $script:Folder/Onboarding.sln -c $Configuration -p:Version=$script:Version > $null
}

function Build-Container {
    param (
        [String]
        $Name,
        [String]
        $DockerFile
    )

    Write-Host "Building ${Name} version $script:Version from $DockerFile in $script:Folder"

    docker build -f "${script:Folder}/${DockerFile}" --force-rm -t ${Name}  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=ExternalScreening.Idp" ${script:Folder}

    Write-Host "Tagging latest => ${Name}:latest ${script:Acr}.azurecr.io/${Name}:${script:Version}"
    cmd.exe /c docker tag "${Name}:latest" "${script:Acr}.azurecr.io/${Name}:latest"

    Write-Host "Tagging [${script:Version}] => ${Name}:latest ${script:Acr}.azurecr.io/${Name}:${script:Version}"
    cmd.exe /c docker tag "${Name}:latest" "${script:Acr}.azurecr.io/${Name}:${script:Version}"
}

function Push-Container {
    param (
        [String]
        $Name
    ) 
    
    Write-Host "Pushing $Name to ACR $script:Acr"

    docker push "${script:Acr}.azurecr.io/${Name}:latest" > $null
    docker push "${script:Acr}.azurecr.io/${Name}:${script:Version}" > $null
}

function Deploy-Terraform {
    param (
        [String]
        $InfraFolder = "/Infra"
    )
    Write-Host "Deploying Terraform from $script:Folder/$InfraFolder" 
    Set-Location "$script:Folder/$InfraFolder"
    terraform apply --auto-approve -var onboarding_app_version=$script:Version -var identity_server_app_version=$script:Version -var screening_api_app_version=$script:Version
    Pop-Location
}

#sets environment variables for development
function Set-DevelopmentEnvironmentVariables {
    param (
        
    )
    Write-Host "Setting development environment variables"
    $env:AZURE_TENANT_ID = $(terraform output spn_tenant_id).Trim('"')
    $env:AZURE_CLIENT_ID = $(terraform output spn_client_id).Trim('"')
    $env:AZURE_CLIENT_SECRET = $(terraform output -json spn_client_secret).Trim('"')

    [Environment]::SetEnvironmentVariable("AZURE_TENANT_ID", $env:AZURE_TENANT_ID, "User")
    [Environment]::SetEnvironmentVariable("AZURE_CLIENT_ID", $env:AZURE_CLIENT_ID, "User")
    [Environment]::SetEnvironmentVariable("AZURE_CLIENT_SECRET", $env:AZURE_CLIENT_SECRET, "User")

    Write-Host "Restarting explorer.exe"
    taskkill /f /im explorer.exe
    explorer.exe

    Write-Host "New variables set. Tenant:$env:AZURE_TENANT_ID, ClientId: $env:AZURE_CLIENT_ID"
}

Build-Solution -Configuration "Release" -Version $Version

if ($PushOptions -ne [PushOptions]::None){
    Connect-Acr
}

#idp
if ($PushOptions.HasFlag([PushOptions]::ExternalScreeningIdp)) {
    Build-Container -Name "externalscreeningidp" -DockerFile "/Remote/ExternalScreening.Idp/Dockerfile" 
    Push-Container -Name "externalscreeningidp"
}

#external api
if ($PushOptions.HasFlag([PushOptions]::ExternalScreeningApi)) {
    Build-Container -Name "externalscreeningapi" -DockerFile "/Remote/ExternalScreening.Api/Dockerfile"
    Push-Container -Name "externalscreeningapi"
}

#onboarding app
if ($PushOptions.HasFlag([PushOptions]::Onboarding)) {
    Build-Container -Name "onboardingserver" -DockerFile "/Server/Dockerfile"
    Push-Container -Name "onboardingserver"
}

if ($true -eq $DeployInfra){
    Deploy-Terraform
}

if ($true -eq $ConfigureDevelopmentEnvironmentVariables){
    Set-DevelopmentEnvironmentVariables
}

Write-Host "Done!"





