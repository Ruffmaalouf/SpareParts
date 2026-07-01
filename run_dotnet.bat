@echo off
title SpareParts Image Enrichment - FULL REPROCESS ALL PARTS
cd /d D:\Ralph\SpareParts
if not exist logs mkdir logs
if not exist reports mkdir reports
if not exist backups mkdir backups

echo ============================================================
echo  SpareParts Image Enrichment - FULL REPROCESS
echo  Sources: AutoZone direct + Bing + catalog page extraction
echo  Trusted autoparts domains auto-apply at High confidence.
echo  Backup created before ANY write.
echo  NOTE: This will take several hours for all 6494 parts.
echo ============================================================
echo.

dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- ^
  --dry-run false ^
  --yes ^
  --batch-size 50 ^
  --replace-existing true ^
  --reprocess-all true ^
  --use-catalog-page-extraction true ^
  --use-bing-text-search true ^
  --use-duckduckgo-text-search false ^
  --allow-unknown-license-auto-update true ^
  --update-oem true ^
  --update-images true ^
  --detect-missing-parts true ^
  --save-candidates true ^
  --delay-ms 400

echo.
echo Done. Check reports\ folder for the HTML report.
echo Rollback SQL is in backups\ if needed.
pause
