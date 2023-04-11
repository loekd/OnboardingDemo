[CmdletBinding()]
param (
    [Parameter()]
    [String]
    $Version = "0.14",
    [Parameter()]
    [String]
    $Acr = "cronboarding",
    [Parameter()]
    [String]
    $Folder = "D:/Projects/gh/Onboarding",
    [String]
    [ValidateSet('None','ExternalScreeningApi','ExternalScreeningIdp', 'Onboarding', 'All')]
    $PushOption = 'All'
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

Build-Solution -Configuration "Release" -Version $Version

if ($PushOptions -ne [PushOptions]::None){
    Connect-Acr
}

#idp
if ($PushOptions.HasFlag([PushOptions]::ExternalScreeningIdp)) {
    Build-Container -Name "externalscreeningidp" -DockerFile "/Remote/ExternalScreening.Idp/Dockerfile" -Path "$script:Folder"
    Push-Container -Name "externalscreeningidp"
}

#external api
if ($PushOptions.HasFlag([PushOptions]::ExternalScreeningApi)) {
    Build-Container -Name "externalscreeningapi" -DockerFile "/Remote/ExternalScreening.Api/Dockerfile" -Path "$script:Folder"
    Push-Container -Name "externalscreeningapi"
}

#onboarding app
if ($PushOptions.HasFlag([PushOptions]::Onboarding)) {
    Build-Container -Name "onboardingserver" -DockerFile "/Server/Dockerfile" -Path "$script:Folder"
    Push-Container -Name "onboardingserver"
}

Write-Host "Done!"





