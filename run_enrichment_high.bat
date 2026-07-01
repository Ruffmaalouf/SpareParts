@echo off
title SpareParts Image Enrichment - High Confidence Dry Run
cd /d D:\Ralph\SpareParts
if not exist logs mkdir logs

echo ============================================================
echo  SpareParts Image Enrichment - HIGH confidence DRY RUN
echo  No database writes will be performed.
echo ============================================================
echo.

dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- ^
  --dry-run true ^
  --batch-size 50 ^
  --replace-existing true ^
  --min-confidence high

echo.
echo Done. Review the newest report in reports\ before requesting a live apply.
pause
