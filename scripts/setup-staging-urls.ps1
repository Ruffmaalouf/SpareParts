#!/usr/bin/env pwsh
# setup-staging-urls.ps1
# Run this AFTER deploy-azure-free.ps1 to patch all staging configs with the real Azure URLs.
# Usage:
#   .\scripts\setup-staging-urls.ps1 -ApiUrl https://spareparts-api-XXXXX.azurewebsites.net -WebUrl https://spareparts-web-XXXXX.azurewebsites.net

param(
    [Parameter(Mandatory = $true)]
    [string]$ApiUrl,

    [string]$WebUrl = "",

    [string]$GoogleClientId = "",
    [string]$GoogleAndroidClientId = "",
    [string]$GoogleIosClientId = "",
    [string]$GoogleWebClientId = "",
    [string]$FacebookAppId = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

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

$ApiUrl = $ApiUrl.TrimEnd("/")
if ([string]::IsNullOrWhiteSpace($WebUrl)) {
    $WebUrl = $ApiUrl -replace "spareparts-api-", "spareparts-web-"
}
$WebUrl = $WebUrl.TrimEnd("/")
$webGoogleClientId = if ([string]::IsNullOrWhiteSpace($GoogleWebClientId)) { $GoogleClientId } else { $GoogleWebClientId }

Write-Host "Patching staging configs..."
Write-Host "  API URL: $ApiUrl"
Write-Host "  Web URL: $WebUrl"

# ── 1. appsettings.Staging.json ──────────────────────────────────────────────
$apiStagingPath = Join-Path $root "src\SpareParts.Api\appsettings.Staging.json"
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
Write-Host "[OK] $apiStagingPath"

# ── 2. Web config.staging.js ─────────────────────────────────────────────────
$webStagingConfigPath = Join-Path $root "src\SpareParts.Web.React\wwwroot\config.staging.js"
$webStagingConfig = @"
window.SparePartsWebConfig = {
  defaultApiBaseUrl: "$ApiUrl",
  googleClientId: "$webGoogleClientId",
  facebookAppId: "$FacebookAppId"
};
"@
Set-Content -Path $webStagingConfigPath -Value $webStagingConfig -Encoding UTF8
Write-Host "[OK] $webStagingConfigPath"

# ── 3. Mobile .env (for EAS build / npm run) ──────────────────────────────────
$mobileEnvPath = Join-Path $root "src\SpareParts.Mobile.ReactNative\.env"
$mobileEnv = @"
EXPO_PUBLIC_API_BASE_URL=$ApiUrl
EXPO_PUBLIC_GOOGLE_CLIENT_ID=$GoogleClientId
EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID=$GoogleAndroidClientId
EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID=$GoogleIosClientId
EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID=$GoogleWebClientId
EXPO_PUBLIC_FACEBOOK_APP_ID=$FacebookAppId
"@
Set-Content -Path $mobileEnvPath -Value $mobileEnv -Encoding UTF8
Write-Host "[OK] $mobileEnvPath"

# ── 4. eas.json staging profile env ──────────────────────────────────────────
$easJsonPath = Join-Path $root "src\SpareParts.Mobile.ReactNative\eas.json"
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
Write-Host "[OK] $easJsonPath (staging profile)"

# ── Verify no localhost remains ───────────────────────────────────────────────
$mobileEnvContent = Get-Content $mobileEnvPath -Raw
if ($mobileEnvContent -match "localhost|127\.0\.0\.1|10\.0\.2\.2") {
    Write-Warning "localhost still found in mobile .env — check the file"
} else {
    Write-Host "[OK] No localhost in mobile .env"
}

Write-Host ""
Write-Host "============================================================"
Write-Host "All staging configs patched."
Write-Host ""
Write-Host "NEXT STEPS:"
Write-Host ""
Write-Host "1. Deploy web app (copy staging config.js over config.js, publish, restore):"
Write-Host "   Copy-Item src\SpareParts.Web.React\wwwroot\config.staging.js src\SpareParts.Web.React\wwwroot\config.js"
Write-Host "   dotnet publish src\SpareParts.Web.React\SpareParts.Web.React.csproj -c Release -o artifacts\azure-free\web"
Write-Host "   Compress-Archive -Path artifacts\azure-free\web\* -DestinationPath artifacts\azure-free\spareparts-web.zip -Force"
Write-Host "   az webapp deploy --resource-group rg-spareparts-staging --name spareparts-web-XXXXX --src-path artifacts\azure-free\spareparts-web.zip --type zip --restart true"
Write-Host "   Copy-Item artifacts\azure-free\web-config.azure.js src\SpareParts.Web.React\wwwroot\config.js  # restore dev config"
Write-Host ""
Write-Host "2. Build Android APK:"
Write-Host "   cd src\SpareParts.Mobile.ReactNative"
Write-Host "   npm install"
Write-Host "   eas login"
Write-Host "   eas build --platform android --profile staging --non-interactive"
Write-Host "   # EAS emails you a download link when done (~10 min)"
Write-Host "============================================================"
