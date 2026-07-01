#Requires -Version 5.1
<#
.SYNOPSIS
    AutoZone Image Enrichment for SpareParts DB
    Fetches product og:image from AutoZone category pages and writes to Parts.ImageUrls.

.USAGE
    pwsh -ExecutionPolicy Bypass -File .\autozone_enrich.ps1
    pwsh -ExecutionPolicy Bypass -File .\autozone_enrich.ps1 -DryRun
    pwsh -ExecutionPolicy Bypass -File .\autozone_enrich.ps1 -Resume
    pwsh -ExecutionPolicy Bypass -File .\autozone_enrich.ps1 -BatchSize 100
#>
param(
    [switch]$DryRun,
    [switch]$AllowLive,
    [switch]$Resume,
    [int]$BatchSize = 50,
    [int]$StartId = 0,
    [string]$ConnStr = "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;"
)

Set-StrictMode -Version 2
$ErrorActionPreference = "Continue"
$ProgressPreference    = "SilentlyContinue"   # suppress Invoke-WebRequest progress bar

if (-not $DryRun -and -not $AllowLive) {
    throw "autozone_enrich.ps1 is deprecated and blocked from live DB writes by default. Use tools\SpareParts.ImageEnrichment for dry-run/reporting, or pass -AllowLive only after explicit approval."
}

$ScriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$LogFile      = Join-Path $ScriptDir "logs\autozone_enrich.log"
$ProgressFile = Join-Path $ScriptDir "scripts\.ps_enrich_progress.json"
$SqlFile      = Join-Path $ScriptDir "autozone_updates.sql"

if (-not (Test-Path (Join-Path $ScriptDir "logs"))) { New-Item -ItemType Directory -Path (Join-Path $ScriptDir "logs") | Out-Null }

# ── Logging ──────────────────────────────────────────────────────────────────
function Log($msg) {
    $ts = Get-Date -Format "HH:mm:ss"
    $line = "$ts  $msg"
    Write-Host $line
    Add-Content -Path $LogFile -Value $line -Encoding UTF8
}

# ── Vehicle model slug mapping ─────────────────────────────────────────────
$MakeSlug = @{
    "BMW"          = "bmw"
    "Audi"         = "audi"
    "Mercedes-Benz"= "mercedes-benz"
    "Nissan"       = "nissan"
    "Honda"        = "honda"
    "Mitsubishi"   = "mitsubishi"
    "GMC"          = "gmc"
}

$ModelSlug = @{
    "A4 2.0T"    = "a4"
    "Q7"         = "q7"
    "325I"       = "325i"
    "328I"       = "328i"
    "335I"       = "335i"
    "525I"       = "525i"
    "535I"       = "535i"
    "650I"       = "650i"
    "750I"       = "750i"
    "X3 2.5"     = "x3"
    "X3 3.0"     = "x3"
    "X5 3.0"     = "x5"
    "X5 4.8"     = "x5"
    "X6 35"      = "x6"
    "Terrain"    = "terrain"
    "Civic DX"   = "civic"
    "Civic LX"   = "civic"
    "C230"       = "c230"
    "C280"       = "c280"
    "C300"       = "c300"
    "E350 Coupe" = "e350"
    "E350"       = "e350"
    "Lancer ES"  = "lancer"
    "Armada"     = "armada"
    "PathFinder" = "pathfinder"
}

# ── Part type → AutoZone category path mapping ────────────────────────────
# Format: "Part Type" = "top-category/sub-category"
# Blank string = known no AutoZone URL (skip)
$PartSlug = @{
    # Engine / Cooling
    "Water Pump"                    = "engine-cooling/water-pump"
    "Thermostat Housing"            = "engine-cooling/thermostat-housing"
    "Coolant Reservoir"             = "engine-cooling/coolant-reservoir"
    "Radiator"                      = "engine-cooling/radiator"
    "Engine Oil Cooler"             = "engine-cooling/engine-oil-cooler"
    "Transmission Oil Cooler"       = "engine-cooling/transmission-oil-cooler"

    # Filtration
    "Air Filter"                    = "filters/engine-air-filter"
    "Cabin Air Filter"              = "filters/cabin-air-filter"
    "Oil Filter"                    = "filters/engine-oil-filter"

    # Engine
    "Belt Tensioner"                = "engine/belt-tensioner"
    "Throttle Body"                 = "fuel-injection/throttle-body"
    "Turbocharger Assembly"         = "engine/turbocharger"
    "Engine Mount Pair"             = "engine/motor-mount"
    "Engine Oil Pan"                = "engine/oil-pan"
    "Right Valve Cover"             = "engine/valve-cover"
    "Left Valve Cover"              = "engine/valve-cover"
    "Right Cylinder Head"           = "engine/cylinder-head"
    "Left Cylinder Head"            = "engine/cylinder-head"
    "Right Exhaust Manifold"        = "exhaust/exhaust-manifold"
    "Left Exhaust Manifold"         = "exhaust/exhaust-manifold"
    "Engine Assembly"               = ""
    "Engine Bay Fuse Box"           = ""
    "Engine Undertray Belly Pan"    = ""
    "Engine Wiring Harness"         = ""
    "Engine Control Module ECM"     = "engine/engine-management"
    "AFTER MARKET INTERCOOLER"      = "engine/intercooler"

    # Charging/Starting
    "Alternator"                    = "charging-starting/alternator"
    "Starter Motor"                 = "charging-starting/starter"
    "Battery Cable Harness"         = "charging-starting/battery-cable"
    "Battery Tray"                  = "batteries/battery-tray"

    # Brakes
    "ABS Pump Module"               = "brakes/abs-module"
    "Brake Booster"                 = "brakes/brake-booster"
    "Brake Master Cylinder"         = "brakes/master-cylinder"
    "Brake Pedal Assembly"          = "brakes/brake-pedal-assembly"
    "Front Brake Caliper Pair"      = "brakes/brake-caliper"
    "Front Left Brake Caliper"      = "brakes/brake-caliper"
    "Front Right Brake Caliper"     = "brakes/brake-caliper"
    "Rear Left Brake Caliper"       = "brakes/brake-caliper"
    "Rear Right Brake Caliper"      = "brakes/brake-caliper"
    "Front Left Brake Rotor"        = "brakes/brake-rotor"
    "Front Right Brake Rotor"       = "brakes/brake-rotor"
    "Rear Left Brake Rotor"         = "brakes/brake-rotor"
    "Rear Right Brake Rotor"        = "brakes/brake-rotor"

    # Suspension
    "Front Left Strut Assembly"     = "suspension/strut"
    "Front Right Strut Assembly"    = "suspension/strut"
    "Front Strut Assembly Pair"     = "suspension/strut"
    "Rear Left Shock Absorber"      = "suspension/shock-absorber"
    "Rear Right Shock Absorber"     = "suspension/shock-absorber"
    "Front Left Lower Control Arm"  = "suspension/control-arm"
    "Front Right Lower Control Arm" = "suspension/control-arm"
    "Rear Left Control Arm"         = "suspension/control-arm"
    "Rear Right Control Arm"        = "suspension/control-arm"
    "Front Left Upper Control Arm"  = "suspension/control-arm"
    "Front Right Upper Control Arm" = "suspension/control-arm"
    "Front Stabilizer Bar"          = "suspension/sway-bar"
    "Rear Stabilizer Bar"           = "suspension/sway-bar"
    "Front Left Wheel Hub"          = "suspension/wheel-hub"
    "Front Right Wheel Hub"         = "suspension/wheel-hub"
    "Rear Left Wheel Hub"           = "suspension/wheel-hub"
    "Rear Right Wheel Hub"          = "suspension/wheel-hub"

    # Steering
    "Steering Rack Gear"            = "steering/rack-and-pinion"
    "Steering Column"               = "steering/steering-column"
    "Front Left Steering Knuckle"   = "suspension/steering-knuckle"
    "Front Right Steering Knuckle"  = "suspension/steering-knuckle"
    "Rear Left Knuckle"             = "suspension/steering-knuckle"
    "Rear Right Knuckle"            = "suspension/steering-knuckle"
    "Right Tie Rod Assembly"        = "steering/tie-rod"
    "Left Tie Rod Assembly"         = "steering/tie-rod"

    # Drivetrain
    "Front Differential Carrier"    = "drivetrain/differential"
    "Rear Differential Carrier"     = "drivetrain/differential"
    "Front Driveshaft"              = "drivetrain/driveshaft"
    "Rear Driveshaft"               = "drivetrain/driveshaft"
    "Front Left Axle Shaft"         = "drivetrain/axle-shaft"
    "Front Right Axle Shaft"        = "drivetrain/axle-shaft"
    "Rear Left Axle Shaft"          = "drivetrain/axle-shaft"
    "Rear Right Axle Shaft"         = "drivetrain/axle-shaft"
    "Transfer Case Assembly"        = "drivetrain/transfer-case"

    # Transmission
    "Transmission Assembly"         = "transmission/transmission"
    "Transmission Control Module"   = ""
    "Transmission Crossmember"      = ""
    "Torque Converter"              = "transmission/torque-converter"
    "Shifter Assembly"              = "drivetrain/shifter"

    # AC / HVAC
    "AC Compressor"                 = "air-conditioning/ac-compressor"
    "AC Condenser"                  = "air-conditioning/ac-condenser"
    "AC Evaporator"                 = "air-conditioning/ac-evaporator"
    "Blower Motor"                  = "heat-and-air-conditioning/blower-motor"
    "Blower Motor Resistor"         = "heat-and-air-conditioning/blower-motor-resistor"
    "Climate Control Panel"         = ""

    # Fuel / Sensors / Exhaust
    "Upstream Oxygen Sensor"        = "sensors/oxygen-sensor"
    "Downstream Oxygen Sensor"      = "sensors/oxygen-sensor"
    "Exhaust Pipe Section"          = "exhaust/exhaust-pipe"
    "Right Catalytic Converter"     = "exhaust/catalytic-converter"
    "Left Catalytic Converter"      = "exhaust/catalytic-converter"
    "Secondary Cats"                = "exhaust/catalytic-converter"

    # Electrical / Electronics
    "Airbag Control Module"         = ""
    "Audio Amplifier"               = ""
    "Body Control Module"           = ""
    "Cabin Fuse Box"                = ""
    "Tire Pressure Monitoring Module" = ""
    "Power Steering Pump"           = "steering/power-steering-pump"
    "Front Wiper Motor"             = "wiper-accessories/wiper-motor"
    "Wiper Transmission Linkage"    = "wiper-accessories/wiper-transmission"
    "Windshield Washer Reservoir"   = "wiper-accessories/windshield-washer-reservoir"
    "Radio Head Unit"               = "electronics/car-stereo"
    "Speaker Set"                   = ""

    # Lighting
    "Right Headlight Assembly"      = "lighting/headlight"
    "Left Headlight Assembly"       = "lighting/headlight"
    "Right Tail Light Assembly"     = "lighting/tail-light"
    "Left Tail Light Assembly"      = "lighting/tail-light"
    "Tail Light Pair"               = "lighting/tail-light"
    "TailLight Left"                = "lighting/tail-light"
    "TailLight Right"               = "lighting/tail-light"
    "Third Brake Light"             = "lighting/third-brake-light"
    "Right Fog Light"               = "lighting/fog-light"
    "Left Fog Light"                = "lighting/fog-light"
    "Right Turn Signal Lamp"        = "lighting/turn-signal"
    "Left Turn Signal Lamp"         = "lighting/turn-signal"

    # Collision / Body
    "Front Bumper Cover"            = "collision-body-parts-and-hardware/bumper-cover"
    "Rear Bumper Cover"             = "collision-body-parts-and-hardware/bumper-cover"
    "Front Bumper Reinforcement"    = "collision-body-parts-and-hardware/front-bumper"
    "Rear Bumper Reinforcement"     = "collision-body-parts-and-hardware/rear-bumper"
    "Front Grille"                  = "collision-body-parts-and-hardware/grille"
    "Front Left Fender"             = "collision-body-parts-and-hardware/fender"
    "Front Right Fender"            = "collision-body-parts-and-hardware/fender"
    "Front Left Inner Fender Liner" = "collision-body-parts-and-hardware/inner-fender"
    "Front Right Inner Fender Liner"= "collision-body-parts-and-hardware/inner-fender"
    "Front Splash Shield"           = "collision-body-parts-and-hardware/splash-shield"
    "Hood"                          = "collision-body-parts-and-hardware/hood"
    "Trunk Lid Decklid"             = "collision-body-parts-and-hardware/trunk"
    "Rear Back Glass"               = ""
    "Windshield Glass"              = ""
    "Right Quarter Panel"           = ""
    "Right Rocker Panel"            = ""
    "Fuel Filler Door"              = "collision-body-parts-and-hardware/fuel-door"
    "Right Roof Rail"               = ""
    "Right Quarter Glass"           = ""

    # Interior / Seating
    "Center Console"                = ""
    "Dashboard Assembly"            = ""
    "Front Left Seat"               = "interior/seat"
    "Front Right Seat"              = "interior/seat"
    "Rear Seat Assembly"            = "interior/seat"
    "Front Left Seat Belt"          = "safety/seat-belt"
    "Front Right Seat Belt"         = "safety/seat-belt"
    "Rear Left Seat Belt"           = "safety/seat-belt"
    "Rear Right Seat Belt"          = "safety/seat-belt"
    "Sunroof Assembly"              = ""
    "Right Sun Visor"               = ""
    "Left Sun Visor"                = ""

    # Doors / Windows / Mirrors
    "Front Left Door Lock Actuator" = "body-and-hardware/door-lock-actuator"
    "Front Right Door Lock Actuator"= "body-and-hardware/door-lock-actuator"
    "Rear Left Door Lock Actuator"  = "body-and-hardware/door-lock-actuator"
    "Rear Right Door Lock Actuator" = "body-and-hardware/door-lock-actuator"
    "Front Left Window Regulator"   = "body-and-hardware/window-regulator"
    "Front Right Window Regulator"  = "body-and-hardware/window-regulator"
    "Rear Left Window Regulator"    = "body-and-hardware/window-regulator"
    "Rear Right Window Regulator"   = "body-and-hardware/window-regulator"
    "Front Left Door Glass"         = ""
    "Front Right Door Glass"        = ""
    "Rear Left Door Glass"          = ""
    "Rear Right Door Glass"         = ""
    "Front Left Door Shell"         = ""
    "Front Right Door Shell"        = ""
    "Rear Left Door Shell"          = ""
    "Rear Right Door Shell"         = ""
    "Front Left Door Trim Panel"    = ""
    "Front Right Door Trim Panel"   = ""
    "Rear Left Door Trim Panel"     = ""
    "Rear Right Door Trim Panel"    = ""
    "Right Side View Mirror"        = "body-and-hardware/mirror"
    "Left Side View Mirror"         = "body-and-hardware/mirror"

    # Safety
    "Driver Steering Wheel Airbag"  = ""
    "Right Curtain Airbag"          = ""
    "Left Curtain Airbag"           = ""

    # Misc
    "Wheel Set"                     = "wheels-and-accessories/wheel"
    "Wheel Center Cap Set"          = "wheels-and-accessories/center-cap"
    "Spare Wheel"                   = ""
    "Factory Jack Tool Kit"         = ""
    "Air Cleaner Box"               = "filters/air-intake"
}

# ── HTTP client setup ─────────────────────────────────────────────────────
Add-Type -AssemblyName System.Net.Http
$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AllowAutoRedirect = $true
$handler.MaxAutomaticRedirections = 5
$http = New-Object System.Net.Http.HttpClient($handler)
$http.Timeout = [TimeSpan]::FromSeconds(20)
$http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36")
$http.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml")
$http.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9")

# ── Extract og:image from HTML ────────────────────────────────────────────
function Get-OgImage {
    param([string]$Url)
    try {
        $resp = $http.GetAsync($Url).GetAwaiter().GetResult()
        if (-not $resp.IsSuccessStatusCode) { return $null }
        # Check final URL for redirect to root (no category match)
        $finalUrl = $resp.RequestMessage.RequestUri.ToString()
        if ($finalUrl -match "autozone\.com/?$" -or $finalUrl -match "autozone\.com/\?") { return $null }
        $html = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($html -match '<meta[^>]+property="og:image"[^>]+content="([^"]+)"') {
            $imgUrl = $Matches[1]
            # Only accept AutoZone CDN images (real product photos)
            if ($imgUrl -match "contentassets\.autozone\.com" -or $imgUrl -match "autozone\.com") {
                return $imgUrl
            }
        }
        return $null
    } catch {
        return $null
    }
}

# ── DB helpers ────────────────────────────────────────────────────────────
function Open-Conn {
    $c = New-Object System.Data.SqlClient.SqlConnection($ConnStr)
    $c.Open()
    return $c
}

function Exec-Query {
    param($conn, [string]$sql, [hashtable]$params = @{})
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 60
    foreach ($kv in $params.GetEnumerator()) {
        $cmd.Parameters.AddWithValue($kv.Key, $kv.Value) | Out-Null
    }
    return $cmd.ExecuteReader()
}

function Exec-NonQuery {
    param($conn, [string]$sql, [hashtable]$params = @{})
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 60
    foreach ($kv in $params.GetEnumerator()) {
        $cmd.Parameters.AddWithValue($kv.Key, $kv.Value) | Out-Null
    }
    return $cmd.ExecuteNonQuery()
}

# ── Progress ──────────────────────────────────────────────────────────────
$processedIds = @{}
if ($Resume -and (Test-Path $ProgressFile)) {
    $data = Get-Content $ProgressFile | ConvertFrom-Json
    foreach ($id in $data.processedIds) { $processedIds[$id] = $true }
    Log "Resuming — already processed $($processedIds.Count) parts"
}

function Save-Progress {
    @{ processedIds = @($processedIds.Keys) } | ConvertTo-Json | Set-Content -Path $ProgressFile -Encoding UTF8
}

# ── Load parts from DB ────────────────────────────────────────────────────
Log "=== AutoZone Image Enrichment $(if ($DryRun) {'[DRY-RUN]'} else {'[LIVE]'}) ==="
Log "Connecting to SQL Server..."

try {
    $conn = Open-Conn
} catch {
    Log "ERROR: Cannot connect to SQL Server: $_"
    exit 1
}

$query = @"
SELECT
    p.Id,
    p.Name,
    p.PartType,
    v.Make,
    v.Model,
    v.Year,
    CASE WHEN ISJSON(p.ImageUrls)=1 THEN JSON_VALUE(p.ImageUrls,N'`$[0]') ELSE NULL END AS CurrentImage
FROM dbo.Parts p
LEFT JOIN dbo.Vehicles v ON p.VehicleId = v.Id
WHERE v.Id IS NOT NULL
  AND p.Id > @StartId
ORDER BY p.Id
"@

$parts = @()
$reader = Exec-Query $conn $query @{ "@StartId" = $StartId }
while ($reader.Read()) {
    $parts += @{
        Id           = $reader["Id"]
        Name         = $reader["Name"]
        PartType     = if ($reader["PartType"] -is [DBNull]) { "" } else { $reader["PartType"] }
        Make         = if ($reader["Make"] -is [DBNull]) { "" } else { $reader["Make"] }
        Model        = if ($reader["Model"] -is [DBNull]) { "" } else { $reader["Model"] }
        Year         = if ($reader["Year"] -is [DBNull]) { "" } else { $reader["Year"] }
        CurrentImage = if ($reader["CurrentImage"] -is [DBNull]) { $null } else { $reader["CurrentImage"] }
    }
}
$reader.Close()

Log "Loaded $($parts.Count) parts with vehicle associations"

# ── Build SQL output file header ──────────────────────────────────────────
Set-Content -Path $SqlFile -Value "-- AutoZone Image Updates (generated $(Get-Date))`nUSE SparePartsDb;`nGO`nBEGIN TRANSACTION;`nGO" -Encoding UTF8

# ── Main enrichment loop ──────────────────────────────────────────────────
$found = 0; $skipped = 0; $failed = 0; $noUrl = 0; $alreadyDone = 0
$batchUpdates = @()
$total = $parts.Count
$i = 0

foreach ($part in $parts) {
    $i++
    $id = $part.Id

    # Skip if already processed (resume mode)
    if ($processedIds.ContainsKey($id)) { $alreadyDone++; continue }

    # Skip if already has a good image (non-Openverse)
    if ($part.CurrentImage -and $part.CurrentImage -notmatch "openverse\.org" -and $part.CurrentImage -notmatch "contentassets\.autozone") {
        # Has a non-Openverse image — only overwrite Openverse/Wikimedia ones
        Log "[$i/$total] ID $id SKIP (already has image)"
        $processedIds[$id] = $true; $skipped++; continue
    }

    # Get part type slug
    $partType = $part.PartType
    if (-not $partType) { $partType = $part.Name -replace "^[0-9]+ .+ .+ ", "" }

    $slug = ""
    if ($PartSlug.ContainsKey($partType)) {
        $slug = $PartSlug[$partType]
    } else {
        # Auto-generate: strip Left/Right/Front/Rear prefixes, lowercase, hyphenate
        $cleanType = $partType -replace "^(Front|Rear|Left|Right|Driver|Passenger) (Left|Right) ", "" `
                               -replace "^(Front|Rear|Left|Right) ", "" `
                               -replace " (Left|Right|Pair|Assembly)$", "" `
                               -replace " ", "-" `
                               -replace "[^a-zA-Z0-9-]", "" `
                               -replace "--+", "-"
        $cleanType = $cleanType.ToLower()
        $slug = $cleanType
    }

    if (-not $slug) {
        Log "[$i/$total] ID $id SKIP-NO-URL ($partType)"
        $processedIds[$id] = $true; $noUrl++; continue
    }

    # Get vehicle slugs
    $makeSlug  = if ($MakeSlug.ContainsKey($part.Make)) { $MakeSlug[$part.Make] } else { $part.Make.ToLower() -replace " ", "-" }
    $modelSlug = if ($ModelSlug.ContainsKey($part.Model)) { $ModelSlug[$part.Model] } else { $part.Model.ToLower() -replace "[^a-zA-Z0-9-]", "" -replace " ", "-" }
    $year      = $part.Year

    $azUrl = "https://www.autozone.com/$slug/$makeSlug/$modelSlug/$year"

    # Fetch og:image
    $imgUrl = Get-OgImage $azUrl
    Start-Sleep -Milliseconds 800   # rate limit

    if ($imgUrl) {
        $jsonArr = '["' + $imgUrl + '"]'
        Log "[$i/$total] ID $id FOUND | $partType | $($part.Make) $($part.Model) $year | $($imgUrl.Substring([Math]::Max(0,$imgUrl.Length-60)))"
        $found++

        # Collect SQL update
        $safeJson = $jsonArr.Replace("'", "''")
        $batchUpdates += "UPDATE dbo.Parts SET ImageUrls = N'$safeJson' WHERE Id = $id;"

        # Apply to DB immediately if not dry-run
        if (-not $DryRun) {
            Exec-NonQuery $conn "UPDATE dbo.Parts SET ImageUrls = @img WHERE Id = @id" @{ "@img" = $jsonArr; "@id" = $id } | Out-Null
        }
    } else {
        Log "[$i/$total] ID $id MISS  | $partType | $($part.Make) $($part.Model) $year | $azUrl"
        $failed++
    }

    $processedIds[$id] = $true

    # Save progress and flush SQL every batch
    if ($i % $BatchSize -eq 0) {
        Save-Progress
        if ($batchUpdates.Count -gt 0) {
            Add-Content -Path $SqlFile -Value ($batchUpdates -join "`n") -Encoding UTF8
            $batchUpdates = @()
        }
        Log "--- Batch checkpoint: $i/$total processed | found=$found miss=$failed skip=$skipped noUrl=$noUrl ---"
    }
}

# Final flush
Save-Progress
if ($batchUpdates.Count -gt 0) {
    Add-Content -Path $SqlFile -Value ($batchUpdates -join "`n") -Encoding UTF8
}
Add-Content -Path $SqlFile -Value "COMMIT TRANSACTION;`nGO" -Encoding UTF8

$conn.Close()

Log ""
Log "=== COMPLETE ==="
Log "  Total parts : $total"
Log "  Found image : $found"
Log "  No URL      : $noUrl"
Log "  No image    : $failed"
Log "  Skipped     : $skipped"
Log "  Already done: $alreadyDone"
Log "  SQL updates : $SqlFile"
Log "  Log file    : $LogFile"
if ($DryRun) {
    Log ""
    Log "DRY-RUN — no DB writes. Review $SqlFile then run without -DryRun to apply."
}
