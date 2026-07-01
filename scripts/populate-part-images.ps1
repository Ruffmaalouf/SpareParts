param(
    [string]$Server = "localhost",
    [string]$Database = "SparePartsDb",
    [string]$OutputDirectory = "outputs",
    [switch]$Overwrite,
    [switch]$AllowLive
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $AllowLive) {
    throw "populate-part-images.ps1 is deprecated and blocked from live DB writes by default. Use tools\SpareParts.ImageEnrichment for dry-run/reporting. Pass -AllowLive only after explicit approval."
}

function Assert-SqlCmd {
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw "sqlcmd is required but was not found on PATH."
    }
}

function Escape-SqlLiteral {
    # Defense-in-depth: values embedded here can originate from an external,
    # untrusted HTTP API (Openverse search results), not just from our own
    # database. In addition to doubling single quotes (the normal T-SQL
    # string-literal escape), strip control characters (CR/LF/NUL/tab) so an
    # attacker-controlled title/URL cannot inject a standalone line (e.g. a
    # bare "GO" batch separator, which sqlcmd recognizes at the start of a
    # line even though it would otherwise sit inside a quoted string) into
    # the generated .sql file that is later executed via sqlcmd.
    param([AllowNull()][string]$Value)
    if ($null -eq $Value) { return "" }
    $sanitized = $Value -replace "[\r\n\t\x00-\x08\x0B\x0C\x0E-\x1F]", " "
    return $sanitized.Replace("'", "''")
}

function Test-SafeHttpUrl {
    # Only allow well-formed http/https URLs through to SQL generation.
    # Openverse results are external content; reject anything that isn't a
    # simple absolute http(s) URL rather than trusting it blindly.
    param([AllowNull()][string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return $false }
    $uri = $null
    if (-not [uri]::TryCreate($Value, [UriKind]::Absolute, [ref]$uri)) { return $false }
    return ($uri.Scheme -eq "http" -or $uri.Scheme -eq "https")
}

function Normalize-Cell {
    param([AllowNull()][string]$Value)
    if ($null -eq $Value) { return "" }
    return $Value.Trim()
}

function Get-PartType {
    param([pscustomobject]$Part)

    $name = Normalize-Cell $Part.Name
    if (-not $name) { return "automotive spare part" }

    if ($Part.Make -and $Part.Model -and $Part.ModelYear) {
        $prefix = "$($Part.ModelYear) $($Part.Make) $($Part.Model) "
        if ($name.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $trimmed = $name.Substring($prefix.Length).Trim()
            if ($trimmed) { return $trimmed }
        }
    }

    return $name
}

function Resolve-ImageSearchTerm {
    param([string]$PartType)

    $text = $PartType.ToLowerInvariant()

    if ($text -match "abs pump") { return "ABS brake pump" }
    if ($text -match "ac compressor") { return "automotive AC compressor" }
    if ($text -match "ac condenser") { return "car air conditioning condenser" }
    if ($text -match "ac evaporator") { return "car air conditioning evaporator" }
    if ($text -match "air cleaner|air filter|cabin air filter") { return "automotive air filter box" }
    if ($text -match "alternator") { return "automotive alternator" }
    if ($text -match "starter") { return "automotive starter motor" }
    if ($text -match "battery cable|battery tray") { return "car battery tray" }
    if ($text -match "belt tensioner|timing belt") { return "automotive belt tensioner" }
    if ($text -match "blower motor") { return "automotive blower motor" }
    if ($text -match "brake booster") { return "automotive brake booster" }
    if ($text -match "brake master") { return "automotive brake master cylinder" }
    if ($text -match "brake caliper|brake pads") { return "automotive brake caliper" }
    if ($text -match "brake rotor") { return "automotive brake rotor" }
    if ($text -match "bumper reinforcement") { return "automotive bumper reinforcement" }
    if ($text -match "bumper") { return "automotive bumper cover" }
    if ($text -match "catalytic converter|secondary cats") { return "automotive catalytic converter" }
    if ($text -match "center console") { return "automotive center console" }
    if ($text -match "climate control") { return "automotive climate control panel" }
    if ($text -match "coolant reservoir") { return "automotive coolant reservoir" }
    if ($text -match "control module|ecm|fuse box|tire pressure") { return "automotive electronic control module" }
    if ($text -match "dashboard") { return "automotive dashboard assembly" }
    if ($text -match "door glass|quarter glass|windshield|back glass") { return "automotive car glass" }
    if ($text -match "door lock actuator") { return "car door lock" }
    if ($text -match "door shell|quarter panel|rocker panel|fender") { return "automotive body panel" }
    if ($text -match "door trim") { return "automotive door trim panel" }
    if ($text -match "driveshaft") { return "automotive driveshaft" }
    if ($text -match "engine assembly|long block") { return "automotive engine assembly" }
    if ($text -match "engine mount") { return "automotive engine mount" }
    if ($text -match "engine oil cooler|transmission oil cooler") { return "automotive oil cooler" }
    if ($text -match "engine oil pan") { return "automotive oil pan" }
    if ($text -match "engine undertray|splash shield|inner fender") { return "automotive undercarriage" }
    if ($text -match "wiring harness") { return "automotive wiring harness" }
    if ($text -match "exhaust manifold|exhaust pipe|muffler") { return "automotive exhaust pipe" }
    if ($text -match "fog light") { return "automotive fog light" }
    if ($text -match "fuel filler") { return "automotive fuel filler neck" }
    if ($text -match "fuel injector") { return "automotive fuel injector" }
    if ($text -match "fuel pump") { return "automotive fuel pump" }
    if ($text -match "fuel rail") { return "automotive fuel rail" }
    if ($text -match "fuel tank") { return "automotive fuel tank" }
    if ($text -match "glove box") { return "automotive glove box" }
    if ($text -match "grille") { return "automotive front grille" }
    if ($text -match "harmonic balancer") { return "automotive harmonic balancer" }
    if ($text -match "headlight|xenon") { return "automotive headlight assembly" }
    if ($text -match "headliner") { return "automotive headliner" }
    if ($text -match "heater core") { return "automotive heater core" }
    if ($text -match "hood") { return "automotive hood panel" }
    if ($text -match "ignition coil|spark plug") { return "automotive ignition coil spark plug" }
    if ($text -match "instrument cluster|speedometer") { return "automotive instrument cluster" }
    if ($text -match "intake manifold") { return "automotive intake manifold" }
    if ($text -match "intercooler") { return "automotive intercooler" }
    if ($text -match "interior carpet") { return "automotive interior carpet" }
    if ($text -match "jack tool") { return "car jack" }
    if ($text -match "mirror") { return "automotive side mirror" }
    if ($text -match "oxygen sensor|oxygene sensor|mass air flow") { return "automotive oxygen sensor" }
    if ($text -match "pedal") { return "automotive pedal assembly" }
    if ($text -match "power steering pump") { return "automotive power steering pump" }
    if ($text -match "power steering reservoir") { return "automotive power steering reservoir" }
    if ($text -match "radiator core support") { return "car radiator support" }
    if ($text -match "radiator fan") { return "automotive radiator fan" }
    if ($text -match "radiator") { return "automotive radiator" }
    if ($text -match "radio|navigation display|audio amplifier|speaker") { return "automotive radio head unit" }
    if ($text -match "roof rail") { return "automotive roof rail" }
    if ($text -match "seat belt") { return "automotive seat belt" }
    if ($text -match "seat") { return "automotive car seat" }
    if ($text -match "shifter") { return "car gear shifter" }
    if ($text -match "shock absorber|strut") { return "car shock absorber" }
    if ($text -match "stabilizer bar") { return "automotive stabilizer bar" }
    if ($text -match "steering column") { return "automotive steering column" }
    if ($text -match "steering knuckle|knuckle") { return "automotive steering knuckle" }
    if ($text -match "steering rack") { return "automotive steering rack" }
    if ($text -match "sun visor") { return "automotive sun visor" }
    if ($text -match "sunroof") { return "automotive sunroof assembly" }
    if ($text -match "tail light|taillight|third brake|turn signal") { return "car tail light" }
    if ($text -match "thermostat") { return "automotive thermostat housing" }
    if ($text -match "throttle body") { return "automotive throttle body" }
    if ($text -match "tie rod") { return "automotive tie rod" }
    if ($text -match "torque converter") { return "automotive torque converter" }
    if ($text -match "transmission assembly") { return "automotive transmission assembly" }
    if ($text -match "transmission crossmember") { return "car transmission mount" }
    if ($text -match "transfer case") { return "automotive transfer case" }
    if ($text -match "turbocharger") { return "automotive turbocharger" }
    if ($text -match "valve cover|cylinder head") { return "automotive cylinder head valve cover" }
    if ($text -match "water pump") { return "automotive water pump" }
    if ($text -match "wheel hub") { return "automotive wheel hub" }
    if ($text -match "wheel center cap") { return "automotive wheel center cap" }
    if ($text -match "wheel|tire") { return "automotive wheel tire" }
    if ($text -match "window regulator") { return "car window regulator" }
    if ($text -match "wiper|washer") { return "automotive windshield wiper motor" }
    if ($text -match "airbag") { return "car airbag" }
    if ($text -match "lower control arm|upper control arm|control arm") { return "automotive control arm" }
    if ($text -match "differential") { return "automotive differential carrier" }
    if ($text -match "axle shaft") { return "automotive axle shaft" }

    return "automotive spare part"
}

function Get-SearchTokens {
    param([string]$SearchTerm)

    $stop = @(
        "automotive", "auto", "car", "cars", "spare", "part", "parts", "used",
        "assembly", "module", "pair", "set", "front", "rear", "left", "right"
    )

    return $SearchTerm.ToLowerInvariant().Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries) |
        ForEach-Object { $_ -replace "[^a-z0-9]", "" } |
        Where-Object { $_.Length -ge 3 -and $stop -notcontains $_ } |
        Select-Object -Unique
}

function Score-ImageResult {
    param(
        [pscustomobject]$Result,
        [string[]]$Tokens
    )

    $haystack = "$($Result.title) $($Result.url) $($Result.creator)".ToLowerInvariant()
    $score = 0
    foreach ($token in $Tokens) {
        if ($haystack.Contains($token)) { $score += 6 }
    }
    if ($haystack -match "auto|automotive|car|vehicle") { $score += 2 }
    if ([string]$Result.url -match "\.(jpg|jpeg|png|webp)(\?|$)") { $score += 1 }
    if (-not $Result.thumbnail -and -not $Result.url) { $score -= 100 }
    return $score
}

function Invoke-OpenverseImageSearch {
    param(
        [string]$SearchTerm,
        [int]$RetryCount = 3
    )

    $query = [uri]::EscapeDataString($SearchTerm)
    $url = "https://api.openverse.org/v1/images/?q=$query&page_size=10&license_type=all"
    $headers = @{ "User-Agent" = "SparePartsImageImport/1.0 (local database enrichment)" }

    for ($attempt = 1; $attempt -le $RetryCount; $attempt++) {
        try {
            return Invoke-RestMethod -Uri $url -Headers $headers -TimeoutSec 30
        }
        catch {
            if ($attempt -eq $RetryCount) { throw }
            Start-Sleep -Seconds (10 * $attempt)
        }
    }
}

function Resolve-ImageForSearchTerm {
    param([string]$SearchTerm)

    $response = Invoke-OpenverseImageSearch -SearchTerm $SearchTerm
    $results = @($response.results)
    if ($results.Count -eq 0) {
        return [pscustomobject]@{
            SearchTerm = $SearchTerm
            ImageUrl = "/assets/part-headlight.png"
            SourceUrl = ""
            LandingUrl = ""
            Title = "Local part placeholder"
            Creator = "SpareParts"
            License = "local"
            Score = 0
        }
    }

    $tokens = @(Get-SearchTokens $SearchTerm)
    $best = $results |
        Sort-Object @{ Expression = { Score-ImageResult -Result $_ -Tokens $tokens }; Descending = $true } |
        Select-Object -First 1

    return [pscustomobject]@{
        SearchTerm = $SearchTerm
        ImageUrl = if ($best.thumbnail) { [string]$best.thumbnail } else { [string]$best.url }
        SourceUrl = [string]$best.url
        LandingUrl = [string]$best.foreign_landing_url
        Title = [string]$best.title
        Creator = [string]$best.creator
        License = [string]$best.license
        Score = Score-ImageResult -Result $best -Tokens $tokens
    }
}

function Save-ImageCache {
    param(
        [hashtable]$Cache,
        [string]$Path
    )

    $cacheObject = [ordered]@{}
    foreach ($entry in ($Cache.GetEnumerator() | Sort-Object Key)) {
        $cacheObject[$entry.Key] = $entry.Value
    }

    $json = $cacheObject | ConvertTo-Json -Depth 6
    $tempPath = "$Path.$PID.tmp"

    for ($attempt = 1; $attempt -le 5; $attempt++) {
        try {
            $json | Set-Content -Path $tempPath -Encoding UTF8
            Move-Item -Path $tempPath -Destination $Path -Force
            return
        }
        catch {
            Start-Sleep -Milliseconds (250 * $attempt)
        }
    }

    Write-Warning "Could not save image cache to $Path. Continuing with in-memory cache."
}

Assert-SqlCmd
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$auditPath = Join-Path $OutputDirectory "part-image-import-$timestamp.csv"
$sqlPath = Join-Path $OutputDirectory "part-image-import-$timestamp.sql"
$cachePath = Join-Path $OutputDirectory "part-image-source-cache.json"

$partsQuery = @"
SET NOCOUNT ON;
SELECT
    CAST(p.Id AS NVARCHAR(20)) AS Id,
    REPLACE(REPLACE(REPLACE(ISNULL(p.Name, N''), CHAR(13), N' '), CHAR(10), N' '), N'|', N'/') AS Name,
    REPLACE(REPLACE(REPLACE(ISNULL(p.InternalCode, N''), CHAR(13), N' '), CHAR(10), N' '), N'|', N'/') AS InternalCode,
    REPLACE(REPLACE(REPLACE(ISNULL(cb.Name, N''), CHAR(13), N' '), CHAR(10), N' '), N'|', N'/') AS Make,
    REPLACE(REPLACE(REPLACE(ISNULL(cm.Name, N''), CHAR(13), N' '), CHAR(10), N' '), N'|', N'/') AS Model,
    ISNULL(CONVERT(NVARCHAR(20), uc.ModelYear), N'') AS ModelYear,
    REPLACE(REPLACE(REPLACE(ISNULL(p.ImageUrls, N''), CHAR(13), N' '), CHAR(10), N' '), N'|', N'/') AS ExistingImageUrls
FROM dbo.Parts p
LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
LEFT JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
WHERE p.IsActive = 1
ORDER BY p.Id;
"@

Write-Host "Reading active parts from $Database..."
$rawRows = & sqlcmd -S $Server -d $Database -E -C -h -1 -W -w 65535 -s "|" -Q $partsQuery
if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed while reading parts." }

$parts = foreach ($line in $rawRows) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $columns = $line -split "\|", 7
    if ($columns.Count -lt 7) { continue }
    $id = 0
    if (-not [int]::TryParse((Normalize-Cell $columns[0]), [ref]$id)) { continue }
    [pscustomobject]@{
        Id = $id
        Name = Normalize-Cell $columns[1]
        InternalCode = Normalize-Cell $columns[2]
        Make = Normalize-Cell $columns[3]
        Model = Normalize-Cell $columns[4]
        ModelYear = Normalize-Cell $columns[5]
        ExistingImageUrls = Normalize-Cell $columns[6]
    }
}

if ($parts.Count -eq 0) { throw "No active parts were found." }

foreach ($part in $parts) {
    $part | Add-Member -NotePropertyName PartType -NotePropertyValue (Get-PartType $part)
    $part | Add-Member -NotePropertyName SearchTerm -NotePropertyValue (Resolve-ImageSearchTerm $part.PartType)
}

$searchTerms = $parts.SearchTerm | Sort-Object -Unique
Write-Host "Found $($parts.Count) active parts across $($searchTerms.Count) image search families."

$imageCache = @{}
if (Test-Path $cachePath) {
    $cacheJson = Get-Content $cachePath -Raw
    if (-not [string]::IsNullOrWhiteSpace($cacheJson)) {
        $loaded = $cacheJson | ConvertFrom-Json
        if ($loaded -is [array]) {
            foreach ($entry in $loaded) {
                if ($entry.Key) { $imageCache[$entry.Key] = $entry.Value }
            }
        }
        else {
            foreach ($property in $loaded.PSObject.Properties) {
                $imageCache[$property.Name] = $property.Value
            }
        }
    }
}

$termIndex = 0
foreach ($term in $searchTerms) {
    $termIndex++
    if ($imageCache.ContainsKey($term)) {
        Write-Host "[$termIndex/$($searchTerms.Count)] Cache: $term"
        continue
    }

    Write-Host "[$termIndex/$($searchTerms.Count)] Openverse: $term"
    $imageCache[$term] = Resolve-ImageForSearchTerm -SearchTerm $term
    Save-ImageCache -Cache $imageCache -Path $cachePath

    if ($termIndex -lt $searchTerms.Count) {
        Start-Sleep -Milliseconds 3200
    }
}

$auditRows = foreach ($part in $parts) {
    $image = $imageCache[$part.SearchTerm]
    [pscustomobject]@{
        PartId = $part.Id
        InternalCode = $part.InternalCode
        PartName = $part.Name
        Make = $part.Make
        Model = $part.Model
        ModelYear = $part.ModelYear
        PartType = $part.PartType
        SearchTerm = $part.SearchTerm
        ImageUrl = $image.ImageUrl
        SourceUrl = $image.SourceUrl
        LandingUrl = $image.LandingUrl
        ImageTitle = $image.Title
        Creator = $image.Creator
        License = $image.License
        Score = $image.Score
        UpdatedImageUrls = ($Overwrite -or [string]::IsNullOrWhiteSpace($part.ExistingImageUrls))
    }
}

$auditRows | Export-Csv -Path $auditPath -NoTypeInformation -Encoding UTF8

$sqlLines = New-Object System.Collections.Generic.List[string]
$sqlLines.Add("SET ANSI_NULLS ON;")
$sqlLines.Add("SET QUOTED_IDENTIFIER ON;")
$sqlLines.Add("SET ANSI_PADDING ON;")
$sqlLines.Add("SET ANSI_WARNINGS ON;")
$sqlLines.Add("SET CONCAT_NULL_YIELDS_NULL ON;")
$sqlLines.Add("SET ARITHABORT ON;")
$sqlLines.Add("SET NUMERIC_ROUNDABORT OFF;")
$sqlLines.Add("SET NOCOUNT ON;")
$sqlLines.Add("SET XACT_ABORT ON;")
$sqlLines.Add("BEGIN TRANSACTION;")
$sqlLines.Add("IF OBJECT_ID('dbo.PartPassportPhotos', 'U') IS NULL THROW 51000, 'dbo.PartPassportPhotos does not exist. Run API migrations first.', 1;")

foreach ($row in $auditRows) {
    if (-not (Test-SafeHttpUrl $row.ImageUrl)) {
        Write-Warning "Skipping PartId $($row.PartId): image URL is not a well-formed http(s) URL ('$($row.ImageUrl)')."
        continue
    }

    $imageJson = "[" + (($row.ImageUrl | ConvertTo-Json -Compress)) + "]"
    $imageJsonSql = Escape-SqlLiteral $imageJson
    $imageUrlSql = Escape-SqlLiteral $row.ImageUrl
    $title = if ($row.ImageTitle.Length -gt 160) { $row.ImageTitle.Substring(0, 160) } else { $row.ImageTitle }
    $notesRaw = "Imported from Openverse. Title: $title; License: $($row.License); Source: $($row.LandingUrl)"
    if ($notesRaw.Length -gt 500) { $notesRaw = $notesRaw.Substring(0, 500) }
    $notesSql = Escape-SqlLiteral $notesRaw
    $id = [int]$row.PartId

    if ($Overwrite) {
        $sqlLines.Add("DELETE FROM dbo.PartPassportPhotos WHERE PartId = $id AND Notes LIKE N'Imported from Openverse.%';")
        $sqlLines.Add("UPDATE dbo.Parts SET ImageUrls = N'$imageJsonSql', ModifiedAt = SYSUTCDATETIME() WHERE Id = $id;")
    }
    else {
        $sqlLines.Add("UPDATE dbo.Parts SET ImageUrls = N'$imageJsonSql', ModifiedAt = SYSUTCDATETIME() WHERE Id = $id AND (ImageUrls IS NULL OR LTRIM(RTRIM(ImageUrls)) = N'');")
    }

    $sqlLines.Add("IF NOT EXISTS (SELECT 1 FROM dbo.PartPassportPhotos WHERE PartId = $id AND PhotoUrl = N'$imageUrlSql')")
    $sqlLines.Add("    INSERT INTO dbo.PartPassportPhotos (PartId, TransactionId, PhotoType, PhotoUrl, Notes, CreatedAt, CreatedByUserId) VALUES ($id, NULL, 1, N'$imageUrlSql', N'$notesSql', SYSUTCDATETIME(), NULL);")
}

$sqlLines.Add("COMMIT;")
$sqlLines | Set-Content -Path $sqlPath -Encoding UTF8

Write-Host "Wrote audit CSV: $auditPath"
Write-Host "Applying SQL updates..."
& sqlcmd -S $Server -d $Database -E -C -b -i $sqlPath
if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed while applying updates. SQL file kept at $sqlPath" }

$verifyQuery = @"
SET NOCOUNT ON;
SELECT COUNT(*) AS ActiveParts FROM dbo.Parts WHERE IsActive = 1;
SELECT COUNT(*) AS ActivePartsWithImages FROM dbo.Parts WHERE IsActive = 1 AND ImageUrls IS NOT NULL AND LTRIM(RTRIM(ImageUrls)) <> N'';
SELECT COUNT(*) AS PassportPhotos FROM dbo.PartPassportPhotos;
"@
& sqlcmd -S $Server -d $Database -E -C -Q $verifyQuery

Write-Host "Done."
