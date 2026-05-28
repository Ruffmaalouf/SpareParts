param(
    [string]$ResourceGroup = "rg-spareparts-free",
    [string]$Location = "westeurope",
    [string]$AppName = "",
    [string]$PlanName = "spareparts-free-plan",
    [string]$SqlServerName = "",
    [string]$DatabaseName = "SparePartsDb",
    [string]$SqlAdminUser = "spadmin",
    [string]$SqlAdminPassword = "",
    [string]$LocalConnectionString = "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;",
    [switch]$SkipDatabaseImport
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root "artifacts\azure-free"
$publishDir = Join-Path $artifacts "api"
$zipPath = Join-Path $artifacts "spareparts-api.zip"
$bacpacPath = Join-Path $artifacts "SparePartsDb.bacpac"
$apiProject = Join-Path $root "src\SpareParts.Api\SpareParts.Api.csproj"

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

function Get-SqlPackagePath {
    $command = Get-Command SqlPackage -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\Microsoft\SQLDB\DAC\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
        "C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
        "C:\Program Files (x86)\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }

    throw "SqlPackage.exe was not found. Install SQL Server Data-Tier Application Framework or Visual Studio SQL tooling."
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

function Invoke-AzJson {
    param([Parameter(Mandatory)][string[]]$Arguments)

    $output = & $script:AzPath @Arguments --output json
    if ($LASTEXITCODE -ne 0) {
        throw "Azure CLI command failed: az $($Arguments -join ' ')"
    }

    if ([string]::IsNullOrWhiteSpace($output)) {
        return $null
    }

    return $output | ConvertFrom-Json
}

function New-SafeSuffix {
    $letters = "abcdefghijklmnopqrstuvwxyz0123456789"
    -join (1..8 | ForEach-Object { $letters[(Get-Random -Minimum 0 -Maximum $letters.Length)] })
}

function New-StrongSqlPassword {
    "SpareParts!" + (Get-Random -Minimum 100000 -Maximum 999999) + [Guid]::NewGuid().ToString("N").Substring(0, 14)
}

function Ensure-AzureLogin {
    try {
        $null = Invoke-AzJson -Arguments @("account", "show")
    }
    catch {
        Write-Host "Azure login is required. Follow the device-code prompt."
        Invoke-Az -Arguments @("login", "--use-device-code")
    }
}

function Test-AzResource {
    param([Parameter(Mandatory)][string[]]$Arguments)

    & $script:AzPath @Arguments --output none 2>$null
    return $LASTEXITCODE -eq 0
}

$script:AzPath = Get-AzPath
$sqlPackage = Get-SqlPackagePath

Ensure-AzureLogin

$suffix = New-SafeSuffix
if ([string]::IsNullOrWhiteSpace($AppName)) {
    $AppName = "spareparts-api-$suffix"
}
if ([string]::IsNullOrWhiteSpace($SqlServerName)) {
    $SqlServerName = "spareparts-sql-$suffix"
}
if ([string]::IsNullOrWhiteSpace($SqlAdminPassword)) {
    $SqlAdminPassword = New-StrongSqlPassword
    Write-Warning "Generated a temporary SQL admin password for this deployment. If you need SQL admin access later, reset the password in Azure Portal."
}

$apiUrl = "https://$AppName.azurewebsites.net"
$sqlHost = "$SqlServerName.database.windows.net"
$azureConnectionString = "Server=tcp:$sqlHost,1433;Initial Catalog=$DatabaseName;Persist Security Info=False;User ID=$SqlAdminUser;Password=$SqlAdminPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$jwtSecret = [Convert]::ToBase64String([Guid]::NewGuid().ToByteArray()) + [Convert]::ToBase64String([Guid]::NewGuid().ToByteArray())

New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

Write-Host "Using Azure CLI: $script:AzPath"
Write-Host "Using SqlPackage: $sqlPackage"
Write-Host "Resource group: $ResourceGroup"
Write-Host "App Service: $AppName"
Write-Host "SQL server: $SqlServerName"
Write-Host ""

Write-Host "Creating resource group..."
Invoke-Az -Arguments @("group", "create", "--name", $ResourceGroup, "--location", $Location, "--output", "none")

Write-Host "Creating free App Service plan (F1)..."
if (-not (Test-AzResource -Arguments @("appservice", "plan", "show", "--resource-group", $ResourceGroup, "--name", $PlanName))) {
    Invoke-Az -Arguments @("appservice", "plan", "create", "--resource-group", $ResourceGroup, "--name", $PlanName, "--location", $Location, "--sku", "F1", "--output", "none")
}

Write-Host "Creating API web app..."
if (-not (Test-AzResource -Arguments @("webapp", "show", "--resource-group", $ResourceGroup, "--name", $AppName))) {
    Invoke-Az -Arguments @("webapp", "create", "--resource-group", $ResourceGroup, "--plan", $PlanName, "--name", $AppName, "--runtime", "DOTNETCORE:8.0", "--output", "none")
}

Write-Host "Creating Azure SQL logical server..."
if (-not (Test-AzResource -Arguments @("sql", "server", "show", "--resource-group", $ResourceGroup, "--name", $SqlServerName))) {
    Invoke-Az -Arguments @("sql", "server", "create", "--resource-group", $ResourceGroup, "--location", $Location, "--name", $SqlServerName, "--admin-user", $SqlAdminUser, "--admin-password", $SqlAdminPassword, "--output", "none")
}

Write-Host "Allowing Azure App Service to reach Azure SQL..."
Invoke-Az -Arguments @("sql", "server", "firewall-rule", "create", "--resource-group", $ResourceGroup, "--server", $SqlServerName, "--name", "AllowAzureServices", "--start-ip-address", "0.0.0.0", "--end-ip-address", "0.0.0.0", "--output", "none")

Write-Host "Creating Azure SQL Database with free monthly limit and AutoPause exhaustion behavior..."
if (-not (Test-AzResource -Arguments @("sql", "db", "show", "--resource-group", $ResourceGroup, "--server", $SqlServerName, "--name", $DatabaseName))) {
    Invoke-Az -Arguments @(
        "sql", "db", "create",
        "--resource-group", $ResourceGroup,
        "--server", $SqlServerName,
        "--name", $DatabaseName,
        "--edition", "GeneralPurpose",
        "--family", "Gen5",
        "--capacity", "2",
        "--compute-model", "Serverless",
        "--max-size", "32GB",
        "--use-free-limit",
        "--free-limit-exhaustion-behavior", "AutoPause",
        "--backup-storage-redundancy", "Local",
        "--output", "none"
    )
}

if (-not $SkipDatabaseImport) {
    Write-Host "Exporting local SQL Server database to BACPAC..."
    if (Test-Path $bacpacPath) { Remove-Item -LiteralPath $bacpacPath -Force }
    Invoke-Native -FilePath $sqlPackage -Arguments @(
        "/Action:Export",
        "/SourceConnectionString:$LocalConnectionString",
        "/TargetFile:$bacpacPath"
    )

    Write-Host "Importing BACPAC into the empty Azure SQL database..."
    Invoke-Native -FilePath $sqlPackage -Arguments @(
        "/Action:Import",
        "/SourceFile:$bacpacPath",
        "/TargetConnectionString:$azureConnectionString"
    )
}
else {
    Write-Warning "Skipped local database import. The API will point to the Azure SQL database, but it may be empty."
}

Write-Host "Configuring API settings..."
Invoke-Az -Arguments @("webapp", "config", "connection-string", "set", "--resource-group", $ResourceGroup, "--name", $AppName, "--connection-string-type", "SQLAzure", "--settings", "DefaultConnection=$azureConnectionString", "--output", "none")
Invoke-Az -Arguments @(
    "webapp", "config", "appsettings", "set",
    "--resource-group", $ResourceGroup,
    "--name", $AppName,
    "--settings",
    "ASPNETCORE_ENVIRONMENT=Production",
    "ConnectionStrings__DefaultConnection=$azureConnectionString",
    "Jwt__Secret=$jwtSecret",
    "Jwt__Issuer=SpareParts.Api",
    "Jwt__Audience=SpareParts.Desktop",
    "Cors__AllowedOrigins__0=http://localhost:5078",
    "Cors__AllowedOrigins__1=http://127.0.0.1:5078",
    "SCM_DO_BUILD_DURING_DEPLOYMENT=false",
    "--output", "none"
)

Write-Host "Publishing API..."
if (Test-Path $publishDir) { Remove-Item -LiteralPath $publishDir -Recurse -Force }
Invoke-Native -FilePath "dotnet" -Arguments @("publish", $apiProject, "-c", "Release", "-o", $publishDir)

if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "Deploying API zip to Azure App Service..."
Invoke-Az -Arguments @("webapp", "deploy", "--resource-group", $ResourceGroup, "--name", $AppName, "--src-path", $zipPath, "--type", "zip", "--restart", "true", "--async", "false", "--output", "none")

Write-Host "Restarting API..."
Invoke-Az -Arguments @("webapp", "restart", "--resource-group", $ResourceGroup, "--name", $AppName, "--output", "none")

Write-Host "Checking Azure API health..."
$healthUrl = "$apiUrl/api/health"
for ($i = 0; $i -lt 24; $i++) {
    try {
        Start-Sleep -Seconds 5
        $health = Invoke-RestMethod -Uri $healthUrl -TimeoutSec 20
        if ($health.status -eq "ok") {
            $wpfSettings = @{
                ApiBaseUrl = "$apiUrl/"
                SalesApiBaseUrl = "$apiUrl/"
                PurchasesApiBaseUrl = "$apiUrl/"
                InventoryApiBaseUrl = "$apiUrl/"
                IdentityApiBaseUrl = "$apiUrl/"
                CatalogApiBaseUrl = "$apiUrl/"
            } | ConvertTo-Json -Depth 3
            Set-Content -Path (Join-Path $artifacts "wpf-appsettings.azure.json") -Value $wpfSettings -Encoding UTF8

            $webConfig = @"
window.SparePartsWebConfig = {
  defaultApiBaseUrl: "$apiUrl",
  googleClientId: "",
  facebookAppId: ""
};
"@
            Set-Content -Path (Join-Path $artifacts "web-config.azure.js") -Value $webConfig -Encoding UTF8

            Write-Host ""
            Write-Host "Azure deployment is online:"
            Write-Host $apiUrl
            Write-Host ""
            Write-Host "Health check:"
            Write-Host $healthUrl
            Write-Host ""
            Write-Host "Client config files were written to:"
            Write-Host (Join-Path $artifacts "wpf-appsettings.azure.json")
            Write-Host (Join-Path $artifacts "web-config.azure.js")
            return
        }
    }
    catch {
        Write-Host "Waiting for App Service warm-up..."
    }
}

throw "Azure API did not become healthy in time. Check App Service logs in Azure Portal for $AppName."
