$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$logs = Join-Path $root "logs"
$apiLog = Join-Path $logs "spareparts-api.log"
$apiErr = Join-Path $logs "spareparts-api.err.log"
$tunnelLog = Join-Path $logs "cloudflared-api.err.log"
$tunnelOut = Join-Path $logs "cloudflared-api.out.log"
$apiHealth = "http://localhost:5000/api/health"

New-Item -ItemType Directory -Force -Path $logs | Out-Null

function Get-CloudflaredPath {
    $command = Get-Command cloudflared -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = @(
        "C:\Program Files\cloudflared\cloudflared.exe",
        "C:\Program Files (x86)\cloudflared\cloudflared.exe",
        (Join-Path $root "tools\cloudflared.exe")
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) { return $candidate }
    }

    throw "cloudflared is not installed. Install it with: winget install --id Cloudflare.cloudflared -e"
}

function Test-Api {
    try {
        $health = Invoke-RestMethod -Uri $apiHealth -TimeoutSec 5
        return $health.status -eq "ok"
    }
    catch {
        return $false
    }
}

if (-not (Test-Api)) {
    Write-Host "Starting SpareParts API on http://localhost:5000 ..."
    $apiProject = Join-Path $root "src\SpareParts.Api\SpareParts.Api.csproj"
    Start-Process -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $apiProject, "--urls", "http://localhost:5000") `
        -WorkingDirectory $root `
        -RedirectStandardOutput $apiLog `
        -RedirectStandardError $apiErr `
        -WindowStyle Hidden | Out-Null

    $ready = $false
    for ($i = 0; $i -lt 30; $i++) {
        Start-Sleep -Seconds 2
        if (Test-Api) {
            $ready = $true
            break
        }
    }

    if (-not $ready) {
        throw "SpareParts API did not become healthy. Check $apiErr"
    }
}

$existingUrl = $null
if (Test-Path $tunnelLog) {
    $existingUrl = Select-String -Path $tunnelLog -Pattern "https://[-a-z0-9]+\.trycloudflare\.com" -AllMatches |
        Select-Object -Last 1 |
        ForEach-Object { $_.Matches.Value }
}

$cloudflaredPath = Get-CloudflaredPath
$runningTunnel = Get-Process cloudflared -ErrorAction SilentlyContinue | Select-Object -First 1

if (-not $runningTunnel) {
    if (Test-Path $tunnelLog) { Remove-Item -LiteralPath $tunnelLog -Force }
    if (Test-Path $tunnelOut) { Remove-Item -LiteralPath $tunnelOut -Force }

    Write-Host "Starting Cloudflare Tunnel to http://localhost:5000 ..."
    Start-Process -FilePath $cloudflaredPath `
        -ArgumentList @("tunnel", "--url", "http://localhost:5000", "--no-autoupdate") `
        -RedirectStandardOutput $tunnelOut `
        -RedirectStandardError $tunnelLog `
        -WindowStyle Hidden | Out-Null
}

$publicUrl = $existingUrl
for ($i = 0; $i -lt 30 -and [string]::IsNullOrWhiteSpace($publicUrl); $i++) {
    Start-Sleep -Seconds 1
    if (Test-Path $tunnelLog) {
        $publicUrl = Select-String -Path $tunnelLog -Pattern "https://[-a-z0-9]+\.trycloudflare\.com" -AllMatches |
            Select-Object -Last 1 |
            ForEach-Object { $_.Matches.Value }
    }
}

if ([string]::IsNullOrWhiteSpace($publicUrl)) {
    throw "Tunnel started, but no public URL was found yet. Check $tunnelLog"
}

Write-Host ""
Write-Host "SpareParts API is online for free from this PC:"
Write-Host $publicUrl
Write-Host ""
Write-Host "Health check:"
Write-Host "$publicUrl/api/health"
Write-Host ""
Write-Host "SQL Server stays private on this PC. Keep this PC awake and online."
