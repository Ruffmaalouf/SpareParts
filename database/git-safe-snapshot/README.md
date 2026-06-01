# Git-safe SQL snapshot

This directory contains a generated, data-only snapshot of the live `SparePartsDb`
catalog and inventory state. It is designed for Git and intentionally excludes
identity, personal-contact, messaging, accounting, operational-log, transaction,
and binary vehicle-image records.

The exported rows include car models, used cars, detailed parts, OEM numbers,
pricing fields, stock, stock movements, warehouses, and supporting reference
data. Audit user IDs, warehouse addresses, and used-car supplier IDs are written
as `NULL`.

## Refresh the snapshot

Run from the repository root:

```powershell
.\scripts\export-git-safe-sql-snapshot.ps1
```

## Apply the snapshot

Create the database schema first, then run from this directory:

```powershell
sqlcmd -S localhost -d SparePartsDb -E -C -i .\apply.sql
```

The generated SQL files contain inserts. Load them into an empty schema or a
purpose-built development database.

## Full private backups

Use a SQL Server `.bak` file outside Git when a complete disaster-recovery
backup is needed. A raw backup contains password hashes and personal data and
can exceed GitHub's per-file size limit.
