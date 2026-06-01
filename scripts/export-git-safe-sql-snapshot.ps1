[CmdletBinding()]
param(
    [string]$ConnectionString = "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;",
    [string]$OutputDirectory = (Join-Path $PSScriptRoot "..\database\git-safe-snapshot")
)

$ErrorActionPreference = "Stop"

$tables = @(
    [pscustomobject]@{ Name = "dbo.AppConstants";   File = "001-dbo.AppConstants.sql";   Redact = @() },
    [pscustomobject]@{ Name = "dbo.Brands";         File = "010-dbo.Brands.sql";         Redact = @("CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.CarBrands";      File = "020-dbo.CarBrands.sql";      Redact = @("CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.CarModels";      File = "030-dbo.CarModels.sql";      Redact = @("CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.Categories";     File = "040-dbo.Categories.sql";     Redact = @("CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.CurrencyRates";  File = "050-dbo.CurrencyRates.sql";  Redact = @() },
    [pscustomobject]@{ Name = "dbo.Warehouses";     File = "060-dbo.Warehouses.sql";     Redact = @("Address", "CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.Location";       File = "070-dbo.Location.sql";       Redact = @("CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.Locations";      File = "080-dbo.Locations.sql";      Redact = @("CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.UsedCars";       File = "090-dbo.UsedCars.sql";       Redact = @("SupplierId", "CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.Parts";          File = "100-dbo.Parts.sql";          Redact = @("CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.Stock";          File = "110-dbo.Stock.sql";          Redact = @("CreatedByUserId", "ModifiedByUserId") },
    [pscustomobject]@{ Name = "dbo.StockMovements"; File = "120-dbo.StockMovements.sql"; Redact = @("CreatedByUserId") }
)

$excludedTables = @(
    "dbo.AccountingAccountTypes",
    "dbo.AccountingPostingRoles",
    "dbo.AccountingPostingSettings",
    "dbo.Accounts",
    "dbo.ApplicationExceptionLogs",
    "dbo.AppMenus",
    "dbo.Customers",
    "dbo.JournalEntries",
    "dbo.JournalLines",
    "dbo.OutboundMessages",
    "dbo.PartRequestReservations",
    "dbo.PartRequests",
    "dbo.ReportBuilderBackgroundRuns",
    "dbo.ReportBuilderFavoriteReports",
    "dbo.ReportBuilderSavedReportRoles",
    "dbo.ReportBuilderSavedReports",
    "dbo.ReportBuilderTableLinks",
    "dbo.RoleMenuAccess",
    "dbo.Roles",
    "dbo.Suppliers",
    "dbo.TransactionItems",
    "dbo.Transactions",
    "dbo.TransactionTypes",
    "dbo.usedcar_images",
    "dbo.UsedCarPurchaseLines",
    "dbo.UsedCarPurchases",
    "dbo.UsedCarWholesaleSales",
    "dbo.Users",
    "dbo.WhatsAppCampaignRecipients",
    "dbo.WhatsAppCampaigns"
)

function Quote-Identifier {
    param([Parameter(Mandatory)][string]$Value)

    return "[" + $Value.Replace("]", "]]") + "]"
}

function Quote-TableName {
    param([Parameter(Mandatory)][string]$Value)

    $segments = $Value.Split(".")
    if ($segments.Count -ne 2) {
        throw "Expected a schema-qualified table name, got '$Value'."
    }

    return "$(Quote-Identifier $segments[0]).$(Quote-Identifier $segments[1])"
}

function ConvertTo-SqlLiteral {
    param($Value)

    if ($null -eq $Value -or $Value -is [System.DBNull]) {
        return "NULL"
    }

    if ($Value -is [byte[]]) {
        if ($Value.Length -eq 0) {
            return "0x"
        }

        return "0x" + [Convert]::ToHexString($Value)
    }

    if ($Value -is [bool]) {
        return $(if ($Value) { "1" } else { "0" })
    }

    if ($Value -is [DateTime]) {
        return "'" + $Value.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", [Globalization.CultureInfo]::InvariantCulture) + "'"
    }

    if ($Value -is [DateTimeOffset]) {
        return "'" + $Value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", [Globalization.CultureInfo]::InvariantCulture) + "'"
    }

    if ($Value -is [Guid]) {
        return "'" + $Value.ToString() + "'"
    }

    if ($Value -is [string] -or $Value -is [char]) {
        return "N'" + ([string]$Value).Replace("'", "''") + "'"
    }

    if ($Value -is [IFormattable]) {
        return $Value.ToString($null, [Globalization.CultureInfo]::InvariantCulture)
    }

    throw "Unsupported SQL value type '$($Value.GetType().FullName)'."
}

function Get-TableMetadata {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][string]$TableName
    )

    $command = $Connection.CreateCommand()
    $command.CommandText = @"
SELECT
    c.name AS ColumnName,
    c.is_identity AS IsIdentity,
    CASE WHEN pk.column_id IS NULL THEN 0 ELSE 1 END AS IsPrimaryKey,
    ISNULL(pk.key_ordinal, 0) AS PrimaryKeyOrdinal
FROM sys.columns c
LEFT JOIN (
    SELECT ic.object_id, ic.column_id, ic.key_ordinal
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id
       AND ic.index_id = i.index_id
    WHERE i.is_primary_key = 1
) pk
    ON pk.object_id = c.object_id
   AND pk.column_id = c.column_id
WHERE c.object_id = OBJECT_ID(@TableName)
ORDER BY c.column_id;
"@
    [void]$command.Parameters.Add("@TableName", [System.Data.SqlDbType]::NVarChar, 256)
    $command.Parameters["@TableName"].Value = $TableName

    $columns = @()
    $reader = $command.ExecuteReader()
    try {
        while ($reader.Read()) {
            $columns += [pscustomobject]@{
                Name = $reader.GetString(0)
                IsIdentity = $reader.GetBoolean(1)
                IsPrimaryKey = [Convert]::ToInt32($reader.GetValue(2)) -eq 1
                PrimaryKeyOrdinal = [Convert]::ToInt32($reader.GetValue(3))
            }
        }
    }
    finally {
        $reader.Dispose()
        $command.Dispose()
    }

    if ($columns.Count -eq 0) {
        throw "Table '$TableName' does not exist."
    }

    return $columns
}

function Export-Table {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)]$Definition,
        [Parameter(Mandatory)][string]$DataDirectory
    )

    $metadata = @(Get-TableMetadata -Connection $Connection -TableName $Definition.Name)
    $tableSql = Quote-TableName $Definition.Name
    $columnSql = ($metadata | ForEach-Object { Quote-Identifier $_.Name }) -join ", "
    $orderColumns = @($metadata | Where-Object IsPrimaryKey | Sort-Object PrimaryKeyOrdinal)
    if ($orderColumns.Count -eq 0) {
        $orderColumns = $metadata
    }
    $orderSql = ($orderColumns | ForEach-Object { Quote-Identifier $_.Name }) -join ", "
    $redactedColumns = @($Definition.Redact)
    $hasIdentity = @($metadata | Where-Object IsIdentity).Count -gt 0

    $path = Join-Path $DataDirectory $Definition.File
    $utf8WithoutBom = [Text.UTF8Encoding]::new($false)
    $writer = [IO.StreamWriter]::new($path, $false, $utf8WithoutBom)
    $rowCount = 0

    try {
        $writer.WriteLine("-- Generated by scripts/export-git-safe-sql-snapshot.ps1")
        $writer.WriteLine("-- Source table: $($Definition.Name)")
        if ($redactedColumns.Count -gt 0) {
            $writer.WriteLine("-- Redacted columns: $($redactedColumns -join ', ')")
        }
        $writer.WriteLine("SET NOCOUNT ON;")
        $writer.WriteLine("SET XACT_ABORT ON;")
        $writer.WriteLine("GO")

        if ($hasIdentity) {
            $writer.WriteLine("SET IDENTITY_INSERT $tableSql ON;")
            $writer.WriteLine("GO")
        }

        $command = $Connection.CreateCommand()
        $command.CommandText = "SELECT $columnSql FROM $tableSql ORDER BY $orderSql;"
        $command.CommandTimeout = 300
        $reader = $command.ExecuteReader()
        try {
            while ($reader.Read()) {
                $values = for ($index = 0; $index -lt $metadata.Count; $index++) {
                    $columnName = $metadata[$index].Name
                    if ($redactedColumns -contains $columnName) {
                        "NULL"
                    }
                    else {
                        ConvertTo-SqlLiteral $reader.GetValue($index)
                    }
                }

                $writer.WriteLine("INSERT INTO $tableSql ($columnSql) VALUES ($($values -join ', '));")
                $rowCount++
            }
        }
        finally {
            $reader.Dispose()
            $command.Dispose()
        }

        if ($hasIdentity) {
            $writer.WriteLine("GO")
            $writer.WriteLine("SET IDENTITY_INSERT $tableSql OFF;")
            $writer.WriteLine("GO")
        }
    }
    finally {
        $writer.Dispose()
    }

    return [pscustomobject]@{
        Table = $Definition.Name
        File = "data/$($Definition.File)"
        Rows = $rowCount
        RedactedColumns = $redactedColumns
    }
}

$resolvedOutput = [IO.Path]::GetFullPath($OutputDirectory)
$dataDirectory = Join-Path $resolvedOutput "data"
[IO.Directory]::CreateDirectory($dataDirectory) | Out-Null

Get-ChildItem -LiteralPath $dataDirectory -Filter "*.sql" -File | Remove-Item -Force

$connection = [System.Data.SqlClient.SqlConnection]::new($ConnectionString)
$connection.Open()
try {
    $databaseName = $connection.Database
    $generatedAtUtc = [DateTime]::UtcNow.ToString("o", [Globalization.CultureInfo]::InvariantCulture)
    $manifestTables = @()

    foreach ($table in $tables) {
        Write-Host "Exporting $($table.Name)..."
        $manifestTables += Export-Table -Connection $connection -Definition $table -DataDirectory $dataDirectory
    }

    $applyPath = Join-Path $resolvedOutput "apply.sql"
    $utf8WithoutBom = [Text.UTF8Encoding]::new($false)
    $applyWriter = [IO.StreamWriter]::new($applyPath, $false, $utf8WithoutBom)
    try {
        $applyWriter.WriteLine("-- Generated by scripts/export-git-safe-sql-snapshot.ps1")
        $applyWriter.WriteLine("-- Apply after creating the SparePartsDb schema. Run from this directory with sqlcmd mode enabled.")
        $applyWriter.WriteLine(":on error exit")
        foreach ($table in $tables) {
            $applyWriter.WriteLine(":r .\data\$($table.File)")
        }
    }
    finally {
        $applyWriter.Dispose()
    }

    $manifest = [ordered]@{
        formatVersion = 1
        sourceDatabase = $databaseName
        generatedAtUtc = $generatedAtUtc
        purpose = "Git-safe catalog and inventory snapshot. Personal, identity, messaging, operational and binary vehicle-image data are excluded."
        tables = $manifestTables
        excludedTables = $excludedTables
    }

    $manifestPath = Join-Path $resolvedOutput "snapshot-manifest.json"
    [IO.File]::WriteAllText(
        $manifestPath,
        ($manifest | ConvertTo-Json -Depth 8) + [Environment]::NewLine,
        $utf8WithoutBom
    )
}
finally {
    $connection.Dispose()
}

Write-Host "Snapshot written to $resolvedOutput"
