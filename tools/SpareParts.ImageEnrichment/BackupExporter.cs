using Microsoft.Data.SqlClient;
using System.Text;

namespace SpareParts.ImageEnrichment;

internal sealed record BackupExport(
    PathInfo PartsCsv,
    PathInfo CurrentPartsReportCsv,
    PathInfo OemPartNumbersCsv,
    PathInfo DonorVehiclesCsv,
    PathInfo? PartCompatibilitiesCsv,
    PathInfo? PassportPhotosCsv,
    PathInfo? EnrichmentCsv,
    PathInfo RollbackSql);

internal static class BackupExporter
{
    public static async Task<BackupExport> ExportAsync(
        SqlConnection conn,
        PathInfo backupsDir,
        PathInfo reportsDir,
        string stamp,
        DatabaseSchema schema,
        RunLogger logger)
    {
        var partsCsv = backupsDir / $"parts_image_urls_{stamp}.csv";
        var currentPartsCsv = reportsDir / $"current_parts_and_images_{stamp}.csv";
        var rollbackSql = backupsDir / $"rollback_parts_image_urls_{stamp}.sql";
        await ExportPartsAsync(conn, partsCsv, currentPartsCsv, rollbackSql);

        var oemPartNumbersCsv = backupsDir / $"parts_oem_part_numbers_{stamp}.csv";
        await ExportQueryAsync(conn, oemPartNumbersCsv, """
SELECT
    p.Id AS PartId,
    p.InternalCode,
    p.Barcode,
    p.Name AS PartName,
    p.OEMNumber,
    p.Notes,
    cat.Name AS Category,
    br.Name AS Brand,
    p.CreatedAt,
    p.ModifiedAt
FROM dbo.Parts p
LEFT JOIN dbo.Categories cat ON cat.Id = p.CategoryId
LEFT JOIN dbo.Brands br ON br.Id = p.BrandId
ORDER BY p.Id;
""");
        logger.Info($"OEM/part-number backup: {oemPartNumbersCsv}");

        var donorVehiclesCsv = backupsDir / $"donor_vehicles_{stamp}.csv";
        await ExportQueryAsync(conn, donorVehiclesCsv, """
SELECT
    uc.Id,
    uc.Barcode AS VehicleCode,
    cb.Name AS Make,
    cm.Name AS Model,
    COALESCE(uc.ModelYear, TRY_CONVERT(INT, cm.Year)) AS [Year],
    cm.EngineType,
    cm.BodyType,
    uc.Location,
    uc.ExpectedSellThroughRate,
    uc.CreatedAt,
    uc.ModifiedAt
FROM dbo.UsedCars uc
LEFT JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
ORDER BY uc.Id;
""");
        logger.Info($"Donor vehicle backup: {donorVehiclesCsv}");

        PathInfo? partCompatibilitiesCsv = null;
        if (schema.HasTable("dbo", "PartCompatibilities"))
        {
            partCompatibilitiesCsv = backupsDir / $"part_compatibilities_{stamp}.csv";
            await ExportQueryAsync(conn, partCompatibilitiesCsv, "SELECT * FROM dbo.PartCompatibilities ORDER BY PartId, Id;");
            logger.Info($"PartCompatibilities backup: {partCompatibilitiesCsv}");
        }

        PathInfo? passportCsv = null;
        if (schema.HasTable("dbo", "PartPassportPhotos"))
        {
            passportCsv = backupsDir / $"part_passport_photos_{stamp}.csv";
            await ExportQueryAsync(conn, passportCsv, """
SELECT
    pp.Id,
    pp.PartId,
    p.InternalCode,
    p.Name AS PartName,
    pp.TransactionId,
    pp.PhotoType,
    pp.PhotoUrl,
    pp.Notes,
    pp.CreatedAt,
    pp.CreatedByUserId
FROM dbo.PartPassportPhotos pp
INNER JOIN dbo.Parts p ON p.Id = pp.PartId
ORDER BY pp.PartId, pp.Id;
""");
            logger.Info($"PartPassportPhotos backup: {passportCsv}");
        }

        PathInfo? enrichmentCsv = null;
        if (schema.HasTable("dbo", "PartImageEnrichment"))
        {
            enrichmentCsv = backupsDir / $"part_image_enrichment_{stamp}.csv";
            await ExportQueryAsync(conn, enrichmentCsv, """
SELECT *
FROM dbo.PartImageEnrichment
ORDER BY PartId, Id;
""");
            logger.Info($"PartImageEnrichment backup: {enrichmentCsv}");
        }

        return new BackupExport(partsCsv, currentPartsCsv, oemPartNumbersCsv, donorVehiclesCsv, partCompatibilitiesCsv, passportCsv, enrichmentCsv, rollbackSql);
    }

    private static async Task ExportPartsAsync(SqlConnection conn, PathInfo backupCsv, PathInfo reportCsv, PathInfo rollbackSql)
    {
        const string sql = """
SELECT
    p.Id AS PartId,
    p.InternalCode,
    p.Barcode,
    p.Name AS PartName,
    p.OEMNumber,
    cat.Name AS Category,
    br.Name AS Brand,
    cb.Name AS CarMake,
    cm.Name AS CarModel,
    COALESCE(uc.ModelYear, TRY_CONVERT(INT, cm.Year)) AS CarYear,
    cm.EngineType,
    uc.Barcode AS DonorVehicleCode,
    p.ImageUrls,
    CASE WHEN ISJSON(p.ImageUrls) = 1 THEN JSON_VALUE(p.ImageUrls, '$[0]') ELSE NULL END AS PrimaryImageUrl,
    p.IsActive,
    p.CreatedAt,
    p.ModifiedAt
FROM dbo.Parts p
LEFT JOIN dbo.Categories cat ON cat.Id = p.CategoryId
LEFT JOIN dbo.Brands br ON br.Id = p.BrandId
LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
LEFT JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
ORDER BY p.Id;
""";

        var rows = new List<Dictionary<string, object?>>();
        await using (var cmd = new SqlCommand(sql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var name in names)
                {
                    var value = reader.GetValue(reader.GetOrdinal(name));
                    row[name] = value == DBNull.Value ? null : value;
                }

                rows.Add(row);
            }
        }

        await CsvWriter.WriteObjectsAsync(backupCsv, rows);
        await CsvWriter.WriteObjectsAsync(reportCsv, rows);

        var sb = new StringBuilder();
        sb.AppendLine("-- Rollback Parts.ImageUrls to the values exported before image enrichment.");
        sb.AppendLine("-- Review before executing.");
        sb.AppendLine("BEGIN TRANSACTION;");
        foreach (var row in rows)
        {
            var id = Convert.ToInt32(row["PartId"], System.Globalization.CultureInfo.InvariantCulture);
            var imageUrls = row["ImageUrls"]?.ToString();
            if (string.IsNullOrWhiteSpace(imageUrls))
            {
                sb.AppendLine($"UPDATE dbo.Parts SET ImageUrls = NULL, ModifiedAt = SYSUTCDATETIME() WHERE Id = {id};");
            }
            else
            {
                sb.AppendLine($"UPDATE dbo.Parts SET ImageUrls = N'{EscapeSql(imageUrls)}', ModifiedAt = SYSUTCDATETIME() WHERE Id = {id};");
            }
        }

        sb.AppendLine("COMMIT;");
        await File.WriteAllTextAsync(rollbackSql.FullName, sb.ToString(), Encoding.UTF8);
    }

    private static async Task ExportQueryAsync(SqlConnection conn, PathInfo path, string sql)
    {
        var rows = new List<Dictionary<string, object?>>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();
        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                var value = reader.GetValue(reader.GetOrdinal(name));
                row[name] = value == DBNull.Value ? null : value;
            }

            rows.Add(row);
        }

        await CsvWriter.WriteObjectsAsync(path, rows);
    }

    private static string EscapeSql(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
