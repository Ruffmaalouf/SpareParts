using Microsoft.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Text;

namespace SpareParts.ImageEnrichment;

internal sealed record PartRecord
{
    public int Id { get; init; }
    public string InternalCode { get; init; } = "";
    public string? Barcode { get; init; }
    public string Name { get; init; } = "";
    public string? OemNumber { get; init; }
    public string? Notes { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public string? CarMake { get; init; }
    public string? CarModel { get; init; }
    public int? CarYear { get; init; }
    public string? EngineType { get; init; }
    public string? ChassisNumber { get; init; }
    public int? UsedCarId { get; init; }
    public string? CurrentImageUrl { get; init; }
    public string? ImageUrls { get; init; }
    public string? CompatibleMakes { get; init; }
    public string? CompatibleModels { get; init; }
    public int? YearFrom { get; init; }
    public int? YearTo { get; init; }
    public string? CompatibleEngines { get; init; }
    public string? CompatOemNumbers { get; init; }
    public string? AlternativeNumbers { get; init; }

    public IEnumerable<string> ExternalIdentifiers
    {
        get
        {
            foreach (var value in SplitIdentifierList(OemNumber)) yield return value;
            foreach (var value in SplitIdentifierList(CompatOemNumbers)) yield return value;
            foreach (var value in SplitIdentifierList(AlternativeNumbers)) yield return value;
        }
    }

    public string RelationshipKey
    {
        get
        {
            var pieces = new[]
            {
                CarMake,
                CarModel,
                CarYear?.ToString(),
                Category,
                NormalizeNameForKey(Name)
            };

            return string.Join("|", pieces.Where(p => !string.IsNullOrWhiteSpace(p))).ToLowerInvariant();
        }
    }

    private static IEnumerable<string> SplitIdentifierList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield break;
        }

        foreach (var part in value.Split([',', ';', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length >= 3)
            {
                yield return part;
            }
        }
    }

    private static string NormalizeNameForKey(string value)
    {
        var tokens = Program.TokenRegex().Matches(value.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(t => t.Length >= 3)
            .Where(t => !int.TryParse(t, out _))
            .TakeLast(4);
        return string.Join("-", tokens);
    }
}

internal sealed record CurrentImageAudit(
    string Status,
    string Reason,
    bool? Reachable,
    string? ContentType,
    long? ContentLengthBytes);

internal sealed record ImageValidation(
    bool Reachable,
    string? ContentType,
    long? ContentLengthBytes,
    string? SkipReason);

internal sealed record EnrichmentResult
{
    public int PartId { get; init; }
    public string InternalCode { get; init; } = "";
    public string PartName { get; init; } = "";
    public string? OemNumber { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public string? CarMake { get; init; }
    public string? CarModel { get; init; }
    public int? CarYear { get; init; }
    public string? EngineType { get; init; }
    public string? OldImageUrl { get; set; }
    public string CurrentImageStatus { get; set; } = "missing";
    public string? CurrentImageReason { get; set; }
    public bool? CurrentImageReachable { get; set; }
    public string? CurrentImageContentType { get; set; }
    public long? CurrentImageContentLengthBytes { get; set; }
    public string? SelectedImageUrl { get; set; }
    public string? ThumbUrl { get; set; }
    public string? SourcePageUrl { get; set; }
    public string? SourceDomain { get; set; }
    public string? SearchQueryUsed { get; set; }
    public double ConfidenceScore { get; set; }
    public string ConfidenceLevel { get; set; } = "low";
    public string? MatchReason { get; set; }
    public string? ContentType { get; set; }
    public long? ContentLengthBytes { get; set; }
    public bool ImageReachable { get; set; }
    public string Status { get; set; } = "skipped";
    public string? ErrorMessage { get; set; }
    public string? Provider { get; set; }
    public string? License { get; set; }
    public bool SourceCanAutoApply { get; set; }
    public int SearchQueriesExecuted { get; set; }
    public int CandidateImagesFound { get; set; }
    public int CandidateImagesRejected { get; set; }
    public string? RejectionReasons { get; set; }
    public string? TopCandidateUrls { get; set; }

    public static EnrichmentResult FromPart(PartRecord part, CurrentImageAudit audit)
    {
        return new EnrichmentResult
        {
            PartId = part.Id,
            InternalCode = part.InternalCode,
            PartName = part.Name,
            OemNumber = part.OemNumber,
            Category = part.Category,
            Brand = part.Brand,
            CarMake = part.CarMake,
            CarModel = part.CarModel,
            CarYear = part.CarYear,
            EngineType = part.EngineType,
            OldImageUrl = part.CurrentImageUrl,
            CurrentImageStatus = audit.Status,
            CurrentImageReason = audit.Reason,
            CurrentImageReachable = audit.Reachable,
            CurrentImageContentType = audit.ContentType,
            CurrentImageContentLengthBytes = audit.ContentLengthBytes
        };
    }
}

internal static class PartRepository
{
    public static async Task<IReadOnlyList<PartRecord>> FetchPartsAsync(SqlConnection conn)
    {
        const string sql = """
SELECT
    p.Id,
    p.InternalCode,
    p.Barcode,
    p.Name AS PartName,
    p.OEMNumber,
    p.Notes,
    p.UsedCarId,
    cat.Name AS Category,
    br.Name AS Brand,
    cb.Name AS CarMake,
    cm.Name AS CarModel,
    COALESCE(uc.ModelYear, TRY_CONVERT(INT, cm.Year)) AS CarYear,
    cm.EngineType,
    uc.Barcode AS ChassisNumber,
    CASE WHEN ISJSON(p.ImageUrls) = 1 THEN JSON_VALUE(p.ImageUrls, '$[0]') ELSE NULL END AS CurrentImageUrl,
    p.ImageUrls,
    pc.CompatibleMakes,
    pc.CompatibleModels,
    pc.YearFrom,
    pc.YearTo,
    pc.CompatibleEngines,
    pc.OemNumbers AS CompatOemNumbers,
    pc.AlternativeNumbers
FROM dbo.Parts p
LEFT JOIN dbo.Categories cat ON cat.Id = p.CategoryId
LEFT JOIN dbo.Brands br ON br.Id = p.BrandId
LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
LEFT JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
OUTER APPLY
(
    SELECT
        STRING_AGG(CONVERT(NVARCHAR(MAX), x.CompatibleMakes), '; ') AS CompatibleMakes,
        STRING_AGG(CONVERT(NVARCHAR(MAX), x.CompatibleModels), '; ') AS CompatibleModels,
        MIN(x.YearFrom) AS YearFrom,
        MAX(x.YearTo) AS YearTo,
        STRING_AGG(CONVERT(NVARCHAR(MAX), x.CompatibleEngines), '; ') AS CompatibleEngines,
        STRING_AGG(CONVERT(NVARCHAR(MAX), x.OemNumbers), '; ') AS OemNumbers,
        STRING_AGG(CONVERT(NVARCHAR(MAX), x.AlternativeNumbers), '; ') AS AlternativeNumbers
    FROM dbo.PartCompatibilities x
    WHERE x.PartId = p.Id
) pc
WHERE p.IsActive = 1
ORDER BY p.Id;
""";

        var rows = new List<PartRecord>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new PartRecord
            {
                Id = reader.GetInt32("Id"),
                InternalCode = reader.GetStringOrEmpty("InternalCode"),
                Barcode = reader.GetNullableString("Barcode"),
                Name = reader.GetStringOrEmpty("PartName"),
                OemNumber = reader.GetNullableString("OEMNumber"),
                Notes = reader.GetNullableString("Notes"),
                UsedCarId = reader.GetNullableInt("UsedCarId"),
                Category = reader.GetNullableString("Category"),
                Brand = reader.GetNullableString("Brand"),
                CarMake = reader.GetNullableString("CarMake"),
                CarModel = reader.GetNullableString("CarModel"),
                CarYear = reader.GetNullableInt("CarYear"),
                EngineType = reader.GetNullableString("EngineType"),
                ChassisNumber = reader.GetNullableString("ChassisNumber"),
                CurrentImageUrl = reader.GetNullableString("CurrentImageUrl"),
                ImageUrls = reader.GetNullableString("ImageUrls"),
                CompatibleMakes = reader.GetNullableString("CompatibleMakes"),
                CompatibleModels = reader.GetNullableString("CompatibleModels"),
                YearFrom = reader.GetNullableInt("YearFrom"),
                YearTo = reader.GetNullableInt("YearTo"),
                CompatibleEngines = reader.GetNullableString("CompatibleEngines"),
                CompatOemNumbers = reader.GetNullableString("CompatOemNumbers"),
                AlternativeNumbers = reader.GetNullableString("AlternativeNumbers")
            });
        }

        return rows;
    }

    public static async Task<HashSet<int>> FetchProcessedPartIdsAsync(SqlConnection conn, DatabaseSchema schema)
    {
        if (!schema.HasTable("dbo", "PartImageEnrichment"))
        {
            return new HashSet<int>();
        }

        const string sql = "SELECT DISTINCT PartId FROM dbo.PartImageEnrichment WHERE Status IN ('auto_approved','pending_review','admin_approved','skipped');";
        return await FetchIdSetAsync(conn, sql);
    }

    public static async Task<HashSet<int>> FetchFailedPartIdsAsync(SqlConnection conn, DatabaseSchema schema)
    {
        if (!schema.HasTable("dbo", "PartImageEnrichment"))
        {
            return new HashSet<int>();
        }

        const string sql = "SELECT DISTINCT PartId FROM dbo.PartImageEnrichment WHERE Status = 'failed';";
        return await FetchIdSetAsync(conn, sql);
    }

    private static async Task<HashSet<int>> FetchIdSetAsync(SqlConnection conn, string sql)
    {
        var ids = new HashSet<int>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            ids.Add(reader.GetInt32(0));
        }

        return ids;
    }

    public static async Task<IReadOnlyList<VehicleRecord>> FetchVehiclesAsync(SqlConnection conn)
    {
        const string sql = """
SELECT
    uc.Id,
    uc.Barcode AS VehicleCode,
    cb.Name AS Make,
    cm.Name AS Model,
    COALESCE(uc.ModelYear, TRY_CONVERT(INT, cm.Year)) AS [Year],
    cm.EngineType,
    cm.BodyType,
    uc.Location,
    COUNT(p.Id) AS ActivePartCount
FROM dbo.UsedCars uc
LEFT JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
LEFT JOIN dbo.Parts p ON p.UsedCarId = uc.Id AND p.IsActive = 1
GROUP BY uc.Id, uc.Barcode, cb.Name, cm.Name, COALESCE(uc.ModelYear, TRY_CONVERT(INT, cm.Year)), cm.EngineType, cm.BodyType, uc.Location
ORDER BY uc.Id;
""";

        var rows = new List<VehicleRecord>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new VehicleRecord
            {
                Id = reader.GetInt32("Id"),
                VehicleCode = reader.GetNullableString("VehicleCode"),
                Make = reader.GetNullableString("Make"),
                Model = reader.GetNullableString("Model"),
                Year = reader.GetNullableInt("Year"),
                EngineType = reader.GetNullableString("EngineType"),
                BodyType = reader.GetNullableString("BodyType"),
                Location = reader.GetNullableString("Location"),
                ActivePartCount = reader.GetInt32("ActivePartCount")
            });
        }

        return rows;
    }
}

internal sealed record VehicleRecord
{
    public int Id { get; init; }
    public string? VehicleCode { get; init; }
    public string? Make { get; init; }
    public string? Model { get; init; }
    public int? Year { get; init; }
    public string? EngineType { get; init; }
    public string? BodyType { get; init; }
    public string? Location { get; init; }
    public int ActivePartCount { get; init; }

    public string DisplayName => string.Join(" ", new[] { Year?.ToString(CultureInfo.InvariantCulture), Make, Model, EngineType }
        .Where(s => !string.IsNullOrWhiteSpace(s)));
}

internal sealed class DatabaseSchema
{
    private readonly HashSet<string> _tables;
    private readonly List<SchemaColumn> _columns;

    private DatabaseSchema(HashSet<string> tables, List<SchemaColumn> columns)
    {
        _tables = tables;
        _columns = columns;
    }

    public static async Task<DatabaseSchema> LoadAsync(SqlConnection conn)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var columns = new List<SchemaColumn>();

        const string tableSql = """
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_SCHEMA, TABLE_NAME;
""";
        await using (var cmd = new SqlCommand(tableSql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
            }
        }

        const string columnSql = """
SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION;
""";
        await using (var cmd = new SqlCommand(columnSql, conn))
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add(new SchemaColumn(
                    reader.GetString(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    reader.GetString(5)));
            }
        }

        return new DatabaseSchema(tables, columns);
    }

    public bool HasTable(string schema, string table) => _tables.Contains($"{schema}.{table}");

    public string ToReportText()
    {
        var focus = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Parts",
            "PartImageEnrichment",
            "PartImageEnrichmentCandidates",
            "PartOemEnrichment",
            "VehicleExpectedPartCandidates",
            "PartPassportPhotos",
            "Categories",
            "Brands",
            "CarBrands",
            "CarModels",
            "UsedCars",
            "PartCompatibilities",
            "usedcar_images"
        };

        var sb = new StringBuilder();
        sb.AppendLine("SpareParts image enrichment schema inspection");
        sb.AppendLine();
        sb.AppendLine("Related tables found:");
        foreach (var table in _tables.Where(t => focus.Contains(t.Split('.').Last())).OrderBy(t => t))
        {
            sb.AppendLine($"- {table}");
        }

        sb.AppendLine();
        foreach (var group in _columns.Where(c => focus.Contains(c.TableName)).GroupBy(c => $"{c.SchemaName}.{c.TableName}").OrderBy(g => g.Key))
        {
            sb.AppendLine(group.Key);
            foreach (var col in group)
            {
                var len = col.MaxLength is null ? "" : $"({col.MaxLength})";
                sb.AppendLine($"  {col.ColumnName}: {col.DataType}{len}, nullable={col.IsNullable}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}

internal sealed record SchemaColumn(
    string SchemaName,
    string TableName,
    string ColumnName,
    string DataType,
    int? MaxLength,
    string IsNullable);

internal static class SqlReaderExtensions
{
    public static string GetStringOrEmpty(this SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? "" : reader.GetString(index);
    }

    public static string? GetNullableString(this SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        var value = reader.GetValue(index);
        return value?.ToString();
    }

    public static int? GetNullableInt(this SqlDataReader reader, string name)
    {
        var index = reader.GetOrdinal(name);
        if (reader.IsDBNull(index))
        {
            return null;
        }

        return Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);
    }

    public static int GetInt32(this SqlDataReader reader, string name)
    {
        return reader.GetInt32(reader.GetOrdinal(name));
    }
}
