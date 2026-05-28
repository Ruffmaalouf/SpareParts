param(
    [string]$ResourceGroup = "rg-spareparts-free"
)

$ErrorActionPreference = "Stop"

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

    throw "Azure CLI is not installed."
}

$az = Get-AzPath

Write-Host "This will delete the Azure resource group '$ResourceGroup' and everything inside it."
Write-Host "Use this when you want to stop the Azure deployment completely."
& $az group delete --name $ResourceGroup --yes --no-wait
if ($LASTEXITCODE -ne 0) {
    throw "Failed to request resource group deletion."
}

Write-Host "Deletion requested. Azure will remove the resources in the background."
