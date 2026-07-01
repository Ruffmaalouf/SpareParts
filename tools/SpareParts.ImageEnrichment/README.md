# SpareParts Image Enrichment Job

This console job audits current part images, searches replacement candidates, looks up OEM candidates, and detects missing expected donor-vehicle parts without writing to the database in dry-run mode.

Safe first pass:

```powershell
dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- --dry-run true --limit 20 --batch-size 20 --update-oem true --update-images true --detect-missing-parts false --save-candidates true --replace-existing true --min-confidence high --candidate-limit-per-part 3 --max-queries-per-part 1
```

One donor vehicle dry run:

```powershell
dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- --dry-run true --vehicle-id 33 --batch-size 50 --update-oem true --update-images true --detect-missing-parts true --save-candidates true --replace-existing true --min-confidence high --candidate-limit-per-part 3 --max-queries-per-part 1
```

All active parts dry run:

```powershell
dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- --dry-run true --batch-size 100 --update-oem true --update-images true --detect-missing-parts true --save-candidates true --replace-existing true --min-confidence high --candidate-limit-per-part 3 --max-queries-per-part 1
```

Live high-confidence apply, only after approval:

```powershell
dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- --dry-run false --yes --batch-size 50 --update-oem true --update-images true --detect-missing-parts true --save-candidates true --replace-existing true --min-confidence high --candidate-limit-per-part 3 --max-queries-per-part 1
```

Apply admin-approved medium-review rows from a review CSV:

```powershell
dotnet run --project tools\SpareParts.ImageEnrichment\SpareParts.ImageEnrichment.csproj -- --dry-run false --yes --apply-review-csv reports\image_enrichment_review_YYYYMMDD_HHMMSS.csv
```

Dry-run writes only files under `backups`, `reports`, and `logs`. Live mode exports backups and rollback SQL before any database write.

Useful flags:

- `--part-id <id>` or `--vehicle-id <id>` to scope a run.
- `--reprocess-failed-only true`, `--reprocess-all true`, and `--resume true` for batch recovery.
- `--use-bing-text-search true` enables review-only Bing/Bing Images fallback candidates.
- `--use-duckduckgo-text-search true` and `--use-catalog-page-extraction true` are optional slower fallbacks.
- `--validate-bing-image-urls true` validates Bing image URLs; by default they are review-only and validation-light.
