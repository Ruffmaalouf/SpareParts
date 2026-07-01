@echo off
title SpareParts Image Enrichment - Dry Run
cd /d D:\Ralph\SpareParts
if not exist logs mkdir logs

echo This wrapper is intentionally dry-run only.
echo Use run_enrich.bat for the first 50 parts or enrich_all.bat for all active parts.
echo.

dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- ^
  --dry-run true ^
  --limit 50 ^
  --batch-size 50 ^
  --replace-existing true ^
  --min-confidence high

pause
