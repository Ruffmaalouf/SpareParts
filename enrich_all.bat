@echo off
title SpareParts Image Enrichment - Dry Run All
cd /d D:\Ralph\SpareParts
if not exist logs mkdir logs

echo ============================================================
echo  SpareParts Image Enrichment - DRY RUN all active parts
echo  No database writes will be performed.
echo ============================================================
echo.

dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- ^
  --dry-run true ^
  --batch-size 50 ^
  --replace-existing true ^
  --min-confidence high

echo.
echo Done. Open the newest reports\image_enrichment_report_*.html file.
pause
