"""
backup_part_images.py — SpareParts Part Image Backup Utility
=============================================================
Exports the current state of part images (Parts.ImageUrls + PartPassportPhotos)
to CSV files in the backups/ folder before any enrichment work.

Usage:
    python scripts/backup_part_images.py
    python scripts/backup_part_images.py --conn "Server=localhost;Database=SparePartsDb;Trusted_Connection=yes;TrustServerCertificate=yes;"

Requirements:
    pip install pyodbc
"""
import argparse
import csv
import json
import os
import sys
from datetime import datetime

try:
    import pyodbc
except ImportError:
    print("ERROR: pyodbc is not installed. Run: pip install pyodbc")
    sys.exit(1)

DEFAULT_CONN = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "Server=localhost;"
    "Database=SparePartsDb;"
    "Trusted_Connection=yes;"
    "TrustServerCertificate=yes;"
)


def get_connection(conn_str: str):
    try:
        return pyodbc.connect(conn_str, timeout=10)
    except pyodbc.Error as e:
        print(f"ERROR: Cannot connect to database.\n  {e}")
        print("\nTry installing the ODBC driver:")
        print("  https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server")
        sys.exit(1)


def backup_parts_image_urls(conn, backups_dir: str, stamp: str):
    """Export Parts.ImageUrls for every part."""
    out_path = os.path.join(backups_dir, f"parts_image_urls_{stamp}.csv")

    query = """
SELECT
    p.Id                                                            AS PartId,
    p.InternalCode,
    p.Name                                                          AS PartName,
    p.OEMNumber,
    cb.Name                                                         AS CarMake,
    cm.Name                                                         AS CarModel,
    cm.Year                                                         AS CarYear,
    cm.EngineType,
    cat.Name                                                        AS Category,
    br.Name                                                         AS Brand,
    p.ImageUrls,
    CASE WHEN ISJSON(p.ImageUrls) = 1
         THEN JSON_VALUE(p.ImageUrls, '$[0]')
         ELSE NULL
    END                                                             AS PrimaryImageUrl,
    p.IsActive,
    p.CreatedAt,
    p.ModifiedAt
FROM dbo.Parts p
LEFT JOIN dbo.Categories    cat ON cat.Id = p.CategoryId
LEFT JOIN dbo.Brands        br  ON br.Id  = p.BrandId
LEFT JOIN dbo.UsedCars      uc  ON uc.Id  = p.UsedCarId
LEFT JOIN dbo.CarModels     cm  ON cm.Id  = uc.CarModelId
LEFT JOIN dbo.CarBrands     cb  ON cb.Id  = cm.CarBrandId
ORDER BY p.Id;
"""

    cursor = conn.cursor()
    cursor.execute(query)
    columns = [desc[0] for desc in cursor.description]
    rows = cursor.fetchall()

    with open(out_path, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)
        writer.writerow(columns)
        for row in rows:
            writer.writerow(list(row))

    print(f"  [OK] Parts ImageUrls backup: {out_path}  ({len(rows)} rows)")
    return out_path, len(rows)


def backup_passport_photos(conn, backups_dir: str, stamp: str):
    """Export PartPassportPhotos table."""
    out_path = os.path.join(backups_dir, f"part_passport_photos_{stamp}.csv")

    query = """
SELECT
    pp.Id,
    pp.PartId,
    p.InternalCode,
    p.Name                                                          AS PartName,
    pp.TransactionId,
    pp.PhotoType,
    CASE pp.PhotoType
        WHEN 1 THEN 'SellerListing'
        WHEN 2 THEN 'BuyerReceipt'
        WHEN 3 THEN 'DisputeEvidence'
        ELSE 'Unknown'
    END                                                             AS PhotoTypeLabel,
    pp.PhotoUrl,
    pp.Notes,
    pp.CreatedAt,
    pp.CreatedByUserId
FROM dbo.PartPassportPhotos pp
INNER JOIN dbo.Parts p ON p.Id = pp.PartId
ORDER BY pp.PartId, pp.Id;
"""
    cursor = conn.cursor()
    try:
        cursor.execute(query)
    except pyodbc.ProgrammingError:
        print("  [SKIP] PartPassportPhotos table does not exist yet.")
        return None, 0

    columns = [desc[0] for desc in cursor.description]
    rows = cursor.fetchall()

    with open(out_path, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)
        writer.writerow(columns)
        for row in rows:
            writer.writerow(list(row))

    print(f"  [OK] PartPassportPhotos backup: {out_path}  ({len(rows)} rows)")
    return out_path, len(rows)


def backup_compatibilities(conn, backups_dir: str, stamp: str):
    """Export PartCompatibilities if the table exists."""
    out_path = os.path.join(backups_dir, f"part_compatibilities_{stamp}.csv")

    query = """
IF OBJECT_ID('dbo.PartCompatibilities', 'U') IS NOT NULL
BEGIN
    SELECT
        pc.Id,
        pc.PartId,
        p.InternalCode,
        p.Name                  AS PartName,
        pc.CompatibleMakes,
        pc.CompatibleModels,
        pc.YearFrom,
        pc.YearTo,
        pc.CompatibleEngines,
        pc.OemNumbers,
        pc.AlternativeNumbers,
        pc.Notes
    FROM dbo.PartCompatibilities pc
    INNER JOIN dbo.Parts p ON p.Id = pc.PartId
    ORDER BY pc.PartId;
END
"""
    cursor = conn.cursor()
    cursor.execute(query)
    try:
        columns = [desc[0] for desc in cursor.description]
        rows = cursor.fetchall()
    except Exception:
        print("  [SKIP] PartCompatibilities table does not exist yet.")
        return None, 0

    with open(out_path, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)
        writer.writerow(columns)
        for row in rows:
            writer.writerow(list(row))

    print(f"  [OK] PartCompatibilities backup: {out_path}  ({len(rows)} rows)")
    return out_path, len(rows)


def main():
    parser = argparse.ArgumentParser(description="Backup SpareParts part images to CSV.")
    parser.add_argument(
        "--conn",
        default=DEFAULT_CONN,
        help="ODBC connection string (default uses Windows Auth to localhost)",
    )
    parser.add_argument(
        "--backups-dir",
        default=os.path.join(os.path.dirname(__file__), "..", "backups"),
        help="Directory to write backup CSVs (default: ../backups)",
    )
    args = parser.parse_args()

    backups_dir = os.path.abspath(args.backups_dir)
    os.makedirs(backups_dir, exist_ok=True)

    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    print(f"\nSpareParts Image Backup — {stamp}")
    print(f"Backup dir : {backups_dir}")
    print(f"Connecting …")

    conn = get_connection(args.conn)
    print("  [OK] Connected to database.\n")

    print("Exporting tables …")
    path1, n1 = backup_parts_image_urls(conn, backups_dir, stamp)
    path2, n2 = backup_passport_photos(conn, backups_dir, stamp)
    path3, n3 = backup_compatibilities(conn, backups_dir, stamp)

    conn.close()

    print(f"\nDone. {n1} parts backed up.")
    print(f"\nFiles written:")
    for p in [path1, path2, path3]:
        if p:
            print(f"  {p}")


if __name__ == "__main__":
    main()
