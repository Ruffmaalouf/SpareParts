$proc = Start-Process -FilePath "cmd.exe" -ArgumentList @(
    "/c",
    "cd /d D:\Ralph\SpareParts && dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- --dry-run false --yes --harvest-parts-from-web true --update-images false --update-oem false --detect-missing-parts false --auto-insert-missing-parts false --use-bing-text-search true --use-duckduckgo-text-search false --save-candidates false --delay-ms 500 >> D:\Ralph\SpareParts\logs\harvest_run.log 2>&1"
) -WorkingDirectory "D:\Ralph\SpareParts" -WindowStyle Normal -PassThru
Write-Host "Launched PID $($proc.Id)"
