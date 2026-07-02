# database/archive/

This folder holds SQL scripts that are **no longer part of the live schema
provisioning or application flow** but are kept (not deleted) for history
and recoverability. Nothing in here runs automatically — the API's C#
migrations (`src/SpareParts.Api/Infrastructure/*Migration.cs`) and
`database/schema.sql` are the only schema sources that matter today.

Files were moved here with `git mv`, so full history (`git log --follow`)
is preserved for each one. They are fully readable and can be restored to
their original location at any time if needed — archiving is a
organizational move, not a deletion.

## Files

### PartImageEnrichmentMigration.sql
Hand-copied, out-of-date duplicate of the real migration
`src/SpareParts.Api/Infrastructure/PartImageEnrichmentMigration.cs`. The
C# version is the one actually wired into `RunMigrations()` in
`SparePartsApiComposition.cs` and runs on every API startup. This `.sql`
copy only creates the `PartImageEnrichment` table and is missing the
later additions the C# version has (`PartImageEnrichmentCandidates`,
`PartOemEnrichment`, `VehicleExpectedPartCandidates` tables, and several
idempotent `ALTER TABLE ... ADD` column backfills). It was never
referenced by any build/deploy/CI step — it was dead weight that could
mislead anyone assuming it was the source of truth. Archived instead of
deleted so the historical hand-copy remains inspectable.

### run_this_in_ssms.sql
One-off manual SSMS script from a June 2026 cowork session used to patch
`Parts.ImageUrls` for the first 10 "high confidence" rows out of a
50-part image audit CSV (`backups\full_image_audit_20260624_233056.csv`,
not part of this repo). References a specific point-in-time backup file
and a specific manual review pass; the data changes it made are already
applied to the working database. Not idempotent against current data,
not referenced by any other script or migration.

### create_enrichment_table.sql
Companion "run this first" script to `run_this_in_ssms.sql` — creates
`dbo.PartImageEnrichment` before the manual update script runs. Superseded
by the real `PartImageEnrichmentMigration.cs`, which creates the same
table (plus more) automatically and idempotently on every API startup.

### minimal_update.sql
A stripped-down variant of the same one-off image patch (no DDL, just
direct `UPDATE dbo.Parts SET ImageUrls = ...` statements for 10 hardcoded
`Parts.Id` values). Intended for `sqlcmd` execution outside SSMS. Same
one-time, already-applied, non-reusable nature as `run_this_in_ssms.sql`.

### apply_enriched_images.sql
A much larger (1,559-row) one-off batch image update generated on
2026-06-29, applying high/medium-confidence enrichment results to
`Parts.ImageUrls` by hardcoded `Parts.Id`. References a companion
rollback script (`backups/rollback_parts_image_urls_20260629_094610.sql`,
not part of this repo) for reversal. This was a single manual data-fix
pass; the values are already applied to the working database and the
script has no future reuse value as written (it targets specific
`Id`s from one point-in-time snapshot).

### oem_parts_insert.sql
One-off seed script that inserts a hardcoded catalog of OEM parts
(seat/engine/etc. components) tied to 33 specific donor `UsedCars` rows
(`UsedCarID` values assumed to already exist, e.g. "Vehicle 1: BMW 335i
2010"). This was a manual bulk-insert used once to backfill a specific
set of already-created used-car teardown records; it is not part of the
schema provisioning path and would corrupt data if re-run against a
database whose `UsedCars`/`Parts` IDs don't match the assumptions baked
into the script.

## Why archive instead of delete

Per `CLAUDE.md`, files are not deleted without explicit user approval.
These six were flagged in the Round 1 audit as dead/already-applied
clutter sitting at the repository root and inside `database/migrations/`
(where their presence could be mistaken for live, wired-up migrations).
Moving them here with `git mv` gets them out of the way of anyone
provisioning a fresh database from `database/schema.sql` +
C# migrations, while keeping full content and git history available.
