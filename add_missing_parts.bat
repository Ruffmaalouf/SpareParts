@echo off
title SpareParts - Web Harvest: Add All Missing Parts
cd /d D:\Ralph\SpareParts
if not exist logs mkdir logs
if not exist reports mkdir reports
if not exist backups mkdir backups

echo ============================================================
echo  SpareParts - WEB CATALOG HARVEST (v2 - Image-Search Primary)
echo.
echo  For EVERY donor vehicle, uses Bing IMAGE search (proven to work)
echo  against trusted OEM sites (partsouq, realoem, autodoc, fcpeuro).
echo  OEM numbers extracted from image titles + source page URLs.
echo  21 part categories x 32 vehicles = ~672 Bing image queries.
echo  Every discovered part not already in dbo.Parts is inserted.
echo.
echo  To test ONE vehicle first, add: --vehicle-id 33 --dry-run true
echo  For a dry run (no inserts), add: --dry-run true
echo ============================================================
echo.

dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- ^
  --dry-run false ^
  --yes ^
  --update-images false ^
  --update-oem false ^
  --detect-missing-parts false ^
  --auto-insert-missing-parts false ^
  --harvest-parts-from-web true ^
  --use-bing-text-search true ^
  --use-duckduckgo-text-search false ^
  --save-candidates false ^
  --delay-ms 300

echo.
echo Done. New parts are in dbo.Parts with IsActive=1.
echo Run run_dotnet.bat next to enrich images for all new parts.
pause
