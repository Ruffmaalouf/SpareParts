param(
    [string]$ResourceGroup = "spareparts-rg",
    [string]$ApiAppName = "spareparts-api-ralph",
    [string]$WebAppName = "spareparts-web-ralph",
    [string]$ApiUrl = "",
    [string]$WebUrl = "",
    [string]$GoogleClientId = "",
    [string]$GoogleAndroidClientId = "",
    [string]$GoogleIosClientId = "",
    [string]$GoogleWebClientId = "",
    [string]$FacebookAppId = "",
    [string]$FacebookAppSecret = "",
    [switch]$SkipWebDeploy
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$webProject = Join-Path $root "src\SpareParts.Web.React\SpareParts.Web.React.csproj"
$webConfigPath = Join-Path $root "src\SpareParts.Web.React\wwwroot\config.js"
$webStagingConfigPath = Join-Path $root "src\SpareParts.Web.React\wwwroot\config.staging.js"
$apiStagingPath = Join-Path $root "src\SpareParts.Api\appsettings.Staging.json"
$mobileEnvPath = Join-Path $root "src\SpareParts.Mobile.ReactNative\.env"
$mobileStagingEnvPath = Join-Path $root "src\SpareParts.Mobile.ReactNative\.env.staging"
$easJsonPath = Join-Path $root "src\SpareParts.Mobile.ReactNative\eas.json"
$artifacts = Join-Path $root "artifacts\azure-oauth"
$webPublishDir = Join-Path $artifacts "web"
$webZipPath = Join-Path $artifacts "spareparts-web.zip"

function Coalesce-Setting {
    param([string[]]$Values)

    foreach ($value in $Values) {
        if (-not [string]::IsNullOrWhiteSpace($value)) {
            return $value.Trim()
        }
    }

    return ""
}

function Set-JsonProperty {
    param(
        [Parameter(Mandatory)]$Object,
        [Parameter(Mandatory)][string]$Name,
        [AllowEmptyString()][string]$Value
    )

    if ($Object.PSObject.Properties.Name -contains $Name) {
        $Object.$Name = $Value
    } else {
        $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
    }
}

function Get-AzPath {
    $command = Get-Command az -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = @(
        "C:\Program Files\Microsoft SDKs\Azure\CLI2\wbin\az.cmd",
        "C:\Program Files (x86)\Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }

    throw "Azure CLI is not installed. Install it with: winget install --id Microsoft.AzureCLI -e"
}

function Invoke-Native {
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [Parameter(Mandatory)][string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $FilePath $($Arguments -join ' ')"
    }
}

function Invoke-Az {
    param([Parameter(Mandatory)][string[]]$Arguments)
    Invoke-Native -FilePath $script:AzPath -Arguments $Arguments
}

$ApiUrl = Coalesce-Setting @($ApiUrl, "https://$ApiAppName.azurewebsites.net")
$WebUrl = Coalesce-Setting @($WebUrl, "https://$WebAppName.azurewebsites.net")
$GoogleClientId = Coalesce-Setting @($GoogleClientId, $env:GOOGLE_CLIENT_ID, $env:EXPO_PUBLIC_GOOGLE_CLIENT_ID)
$GoogleAndroidClientId = Coalesce-Setting @($GoogleAndroidClientId, $env:GOOGLE_ANDROID_CLIENT_ID, $env:EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID)
$GoogleIosClientId = Coalesce-Setting @($GoogleIosClientId, $env:GOOGLE_IOS_CLIENT_ID, $env:EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID)
$GoogleWebClientId = Coalesce-Setting @($GoogleWebClientId, $env:GOOGLE_WEB_CLIENT_ID, $env:EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID)
$FacebookAppId = Coalesce-Setting @($FacebookAppId, $env:FACEBOOK_APP_ID, $env:EXPO_PUBLIC_FACEBOOK_APP_ID)
$FacebookAppSecret = Coalesce-Setting @($FacebookAppSecret, $env:FACEBOOK_APP_SECRET, $env:EXTERNALAUTH__FACEBOOKAPPSECRET)
$webGoogleClientId = Coalesce-Setting @($GoogleWebClientId, $GoogleClientId)

if ([string]::IsNullOrWhiteSpace($webGoogleClientId) -and [string]::IsNullOrWhiteSpace($FacebookAppId)) {
    throw "Provide at least -GoogleClientId/-GoogleWebClientId or -FacebookAppId."
}

if (-not [string]::IsNullOrWhiteSpace($FacebookAppId) -and [string]::IsNullOrWhiteSpace($FacebookAppSecret)) {
    throw "Facebook login also requires -FacebookAppSecret so the API can validate access tokens."
}

$script:AzPath = Get-AzPath
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Write-Host "Configuring Azure OAuth settings..."
Write-Host "  API: $ApiUrl"
Write-Host "  Web: $WebUrl"

$apiSettings = @("Cors__AllowedOrigins__0=$WebUrl")
if (-not [string]::IsNullOrWhiteSpace($webGoogleClientId)) {
    $apiSettings += "ExternalAuth__GoogleClientId=$webGoogleClientId"
}
if (-not [string]::IsNullOrWhiteSpace($FacebookAppId)) {
    $apiSettings += "ExternalAuth__FacebookAppId=$FacebookAppId"
    $apiSettings += "ExternalAuth__FacebookAppSecret=$FacebookAppSecret"
}

Invoke-Az -Arguments (@(
    "webapp", "config", "appsettings", "set",
    "--resource-group", $ResourceGroup,
    "--name", $ApiAppName,
    "--settings"
) + $apiSettings + @("--output", "none"))

$apiStaging = @"
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "$WebUrl"
    ]
  },
  "ExternalAuth": {
    "GoogleClientId": "$webGoogleClientId",
    "FacebookAppId": "$FacebookAppId"
  }
}
"@
Set-Content -Path $apiStagingPath -Value $apiStaging -Encoding UTF8

$webConfig = @"
window.SparePartsWebConfig = {
  defaultApiBaseUrl: "$ApiUrl",
  googleClientId: "$webGoogleClientId",
  facebookAppId: "$FacebookAppId"
};
"@
Set-Content -Path $webStagingConfigPath -Value $webConfig -Encoding UTF8

$mobileEnv = @"
EXPO_PUBLIC_API_BASE_URL=$ApiUrl
EXPO_PUBLIC_GOOGLE_CLIENT_ID=$GoogleClientId
EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID=$GoogleAndroidClientId
EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID=$GoogleIosClientId
EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID=$GoogleWebClientId
EXPO_PUBLIC_FACEBOOK_APP_ID=$FacebookAppId
"@
Set-Content -Path $mobileEnvPath -Value $mobileEnv -Encoding UTF8
Set-Content -Path $mobileStagingEnvPath -Value $mobileEnv -Encoding UTF8

$easJson = Get-Content $easJsonPath -Raw | ConvertFrom-Json
if ($null -eq $easJson.build.staging.env) {
    $easJson.build.staging | Add-Member -NotePropertyName env -NotePropertyValue ([pscustomobject]@{})
}
Set-JsonProperty $easJson.build.staging.env "EXPO_PUBLIC_API_BASE_URL" $ApiUrl
Set-JsonProperty $easJson.build.staging.env "EXPO_PUBLIC_GOOGLE_CLIENT_ID" $GoogleClientId
Set-JsonProperty $easJson.build.staging.env "EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID" $GoogleAndroidClientId
Set-JsonProperty $easJson.build.staging.env "EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID" $GoogleIosClientId
Set-JsonProperty $easJson.build.staging.env "EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID" $GoogleWebClientId
Set-JsonProperty $easJson.build.staging.env "EXPO_PUBLIC_FACEBOOK_APP_ID" $FacebookAppId
$easJson | ConvertTo-Json -Depth 10 | Set-Content -Path $easJsonPath -Encoding UTF8

if (-not $SkipWebDeploy) {
    $originalWebConfig = Get-Content $webConfigPath -Raw
    try {
        Set-Content -Path $webConfigPath -Value $webConfig -Encoding UTF8

        if (Test-Path $webPublishDir) { Remove-Item -LiteralPath $webPublishDir -Recurse -Force }
        Invoke-Native -FilePath "dotnet" -Arguments @("publish", $webProject, "-c", "Release", "-o", $webPublishDir)

        if (Test-Path $webZipPath) { Remove-Item -LiteralPath $webZipPath -Force }
        Compress-Archive -Path (Join-Path $webPublishDir "*") -DestinationPath $webZipPath -Force

        Invoke-Az -Arguments @(
            "webapp", "deploy",
            "--resource-group", $ResourceGroup,
            "--name", $WebAppName,
            "--src-path", $webZipPath,
            "--type", "zip",
            "--restart", "true",
            "--async", "false",
            "--output", "none"
        )
    }
    finally {
        Set-Content -Path $webConfigPath -Value $originalWebConfig -Encoding UTF8
    }
}

Invoke-Az -Arguments @("webapp", "restart", "--resource-group", $ResourceGroup, "--name", $ApiAppName, "--output", "none")

Write-Host "[OK] OAuth settings were applied."
Write-Host "[OK] API App Settings updated for external auth."
if (-not $SkipWebDeploy) {
    Write-Host "[OK] Web app redeployed with OAuth client IDs."
}
Write-Host "[OK] Local staging/mobile config updated."
