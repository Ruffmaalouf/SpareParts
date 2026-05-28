$ErrorActionPreference = "Stop"

$tunnels = Get-Process cloudflared -ErrorAction SilentlyContinue
if (-not $tunnels) {
    Write-Host "No Cloudflare Tunnel process is running."
    return
}

$tunnels | Stop-Process -Force
Write-Host "Stopped Cloudflare Tunnel. Your API is no longer public through trycloudflare.com."
