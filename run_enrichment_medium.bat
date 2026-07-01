@echo off
title SpareParts Image Enrichment - Medium Review Dry Run
cd /d D:\Ralph\SpareParts
if not exist logs mkdir logs

echo ============================================================
echo  SpareParts Image Enrichment - MEDIUM review DRY RUN
echo  No database writes will be performed.
echo  This enables broader review-only searching for parts without OEM numbers.
echo ============================================================
echo.

dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- ^
  --dry-run true ^
  --batch-size 50 ^
  --replace-existing true ^
  --min-confidence medium ^
  --search-review-without-identifiers true

echo.
echo Done. Review reports\image_enrichment_review_*.csv before any live apply.
pause
