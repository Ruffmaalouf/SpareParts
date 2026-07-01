"""
enrich_part_images.py — SpareParts Part Image Enrichment Pipeline
==================================================================
Finds better product images for every part in the database.

NEVER runs real DB updates unless --dry-run false is explicitly passed.

Usage examples:
    # Dry-run first 50 parts only (safe — no DB writes):
    python scripts/enrich_part_images.py --dry-run --limit 50

    # Generate report only (no image fetching, just audit current state):
    python scripts/enrich_part_images.py --report-only

    # Resume an interrupted run:
    python scripts/enrich_part_images.py --dry-run --resume

    # Full dry-run, all parts:
    python scripts/enrich_part_images.py --dry-run

    # REAL update — only after user reviews the report and explicitly approves:
    python scripts/enrich_part_images.py --dry-run false --min-confidence high

Requirements:
    pip install pyodbc requests

Optional (for Google Custom Search):
    Set environment variables:
        GOOGLE_CSE_API_KEY=...
        GOOGLE_CSE_CX=...
    Or set in appsettings.json under "ImageSearch" section.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import sys
import time
import hashlib
import re
import logging
from datetime import datetime, timezone
from pathlib import Path
from typing import Optional
from urllib.parse import quote_plus

if "--legacy-allow" not in sys.argv:
    print(
        "DEPRECATED: scripts/enrich_part_images.py is blocked by default because "
        "it writes enrichment metadata during dry-run. Use the .NET job at "
        "tools/SpareParts.ImageEnrichment instead. Pass --legacy-allow only if "
        "you intentionally want the old Python behavior."
    )
    sys.exit(2)

sys.argv.remove("--legacy-allow")

# ── dependency checks ──────────────────────────────────────────────────────────
try:
    import pyodbc
except ImportError:
    print("ERROR: pyodbc not installed.  Run: pip install pyodbc")
    sys.exit(1)

try:
    import requests
    from requests.adapters import HTTPAdapter
    from urllib3.util.retry import Retry
except ImportError:
    print("ERROR: requests not installed.  Run: pip install requests")
    sys.exit(1)

# ── paths ──────────────────────────────────────────────────────────────────────
SCRIPT_DIR   = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parent
REPORTS_DIR  = PROJECT_ROOT / "reports"
BACKUPS_DIR  = PROJECT_ROOT / "backups"
PROGRESS_FILE = PROJECT_ROOT / "scripts" / ".enrich_progress.json"

REPORTS_DIR.mkdir(exist_ok=True)
BACKUPS_DIR.mkdir(exist_ok=True)

# ── logging ────────────────────────────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-7s  %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger("enrich")

# ── constants ──────────────────────────────────────────────────────────────────
DEFAULT_CONN = (
    "DRIVER={ODBC Driver 17 for SQL Server};"
    "Server=localhost;"
    "Database=SparePartsDb;"
    "Trusted_Connection=yes;"
    "TrustServerCertificate=yes;"
)

RATE_LIMIT_DELAY   = 0.5   # seconds between external HTTP requests
MIN_IMAGE_BYTES    = 5_000 # skip tiny thumbnails
MAX_RETRIES        = 3
BACKOFF_FACTOR     = 1.5

# Part-name keywords that are AUTOMOTIVE-SPECIFIC — used for sanity-checking
# image titles from Openverse (which is mostly Flickr).
# An image is SUSPICIOUS if its title has zero overlap with these.
AUTOMOTIVE_KEYWORDS = {
    "filter", "pump", "valve", "sensor", "brake", "clutch", "belt",
    "bearing", "gasket", "seal", "injector", "alternator", "starter",
    "radiator", "thermostat", "shock", "strut", "spring", "control arm",
    "ball joint", "tie rod", "caliper", "rotor", "drum", "pad",
    "compressor", "condenser", "evaporator", "blower", "fan",
    "headlight", "tail light", "lamp", "mirror", "bumper", "fender",
    "hood", "door", "window", "glass", "windshield", "wiper",
    "motor", "engine", "exhaust", "manifold", "catalytic", "muffler",
    "transmission", "gearbox", "differential", "axle", "shaft",
    "wheel", "hub", "rim", "tire", "suspension", "steering",
    "rack", "pinion", "hose", "pipe", "clamp", "bracket",
    "switch", "relay", "fuse", "harness", "module", "ecu",
    "intercooler", "turbo", "supercharger", "air intake",
    "battery", "ignition", "coil", "plug", "cable",
    "oil", "coolant", "fluid", "reservoir", "cap",
    "bushing", "mount", "support", "crossmember",
    "regulator", "window regulator", "mirror", "antenna",
    "seat", "carpet", "trim", "panel", "grille", "emblem",
    "part", "spare", "oem", "genuine", "aftermarket",
}

# ── HTTP session ───────────────────────────────────────────────────────────────
def build_http_session() -> requests.Session:
    session = requests.Session()
    retry = Retry(
        total=MAX_RETRIES,
        backoff_factor=BACKOFF_FACTOR,
        status_forcelist=[429, 500, 502, 503, 504],
        allowed_methods=["HEAD", "GET"],
    )
    adapter = HTTPAdapter(max_retries=retry)
    session.mount("http://", adapter)
    session.mount("https://", adapter)
    session.headers.update({
        "User-Agent": "SpareParts-ImageEnricher/1.0 (contact: admin@spareparts.local)"
    })
    return session


# ── database helpers ───────────────────────────────────────────────────────────
def get_connection(conn_str: str) -> pyodbc.Connection:
    drivers = [d for d in pyodbc.drivers() if "SQL Server" in d]
    if not drivers:
        log.error(
            "No SQL Server ODBC driver found. Install from:\n"
            "  https://learn.microsoft.com/en-us/sql/connect/odbc/download-odbc-driver-for-sql-server"
        )
        sys.exit(1)

    # Try the configured driver first, then fall back
    for driver in ["ODBC Driver 17 for SQL Server", "ODBC Driver 18 for SQL Server", drivers[0]]:
        try:
            alt = conn_str.replace("{ODBC Driver 17 for SQL Server}", "{" + driver + "}")
            conn = pyodbc.connect(alt, timeout=10)
            log.info(f"Connected via driver: {driver}")
            return conn
        except pyodbc.Error:
            continue

    log.error("Could not connect to SQL Server. Check that the server is running and the connection string is correct.")
    sys.exit(1)


def ensure_enrichment_table(conn: pyodbc.Connection):
    """Apply PartImageEnrichmentMigration idempotently."""
    cursor = conn.cursor()
    cursor.execute("""
IF OBJECT_ID('dbo.PartImageEnrichment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartImageEnrichment
    (
        Id                  INT            IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartImageEnrichment PRIMARY KEY,
        PartId              INT            NOT NULL,
        SelectedImageUrl    NVARCHAR(1000) NULL,
        ThumbUrl            NVARCHAR(1000) NULL,
        SourcePageUrl       NVARCHAR(1000) NULL,
        SourceDomain        NVARCHAR(200)  NULL,
        SearchQueryUsed     NVARCHAR(500)  NULL,
        ConfidenceScore     DECIMAL(5,2)   NOT NULL DEFAULT 0,
        ConfidenceLevel     NVARCHAR(20)   NOT NULL DEFAULT 'low',
        MatchReason         NVARCHAR(500)  NULL,
        ContentType         NVARCHAR(100)  NULL,
        ContentLengthBytes  INT            NULL,
        ImageReachable      BIT            NULL,
        OldImageUrl         NVARCHAR(1000) NULL,
        Status              NVARCHAR(50)   NOT NULL DEFAULT 'pending',
        FetchedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        ApprovedAt          DATETIME2      NULL,
        ApprovedByUserId    INT            NULL,
        AppliedAt           DATETIME2      NULL,
        ErrorMessage        NVARCHAR(1000) NULL,
        CreatedAt           DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        ModifiedAt          DATETIME2      NULL,
        CONSTRAINT FK_PartImageEnrichment_Parts FOREIGN KEY (PartId) REFERENCES dbo.Parts (Id)
    );
    CREATE INDEX IX_PartImageEnrichment_PartId ON dbo.PartImageEnrichment (PartId);
    CREATE INDEX IX_PartImageEnrichment_Status ON dbo.PartImageEnrichment (Status);
END;
""")
    conn.commit()


def fetch_parts(conn: pyodbc.Connection, limit: Optional[int], resume_ids: set[int]) -> list[dict]:
    """Load parts with all context needed for image search."""
    top_clause = f"TOP {limit}" if limit else ""
    cursor = conn.cursor()
    cursor.execute(f"""
SELECT {top_clause}
    p.Id,
    p.InternalCode,
    p.Name                                                     AS PartName,
    p.OEMNumber,
    p.Notes,
    cat.Name                                                   AS Category,
    br.Name                                                    AS Brand,
    cb.Name                                                    AS CarMake,
    cm.Name                                                    AS CarModel,
    cm.Year                                                    AS CarYear,
    cm.EngineType,
    CASE WHEN ISJSON(p.ImageUrls) = 1
         THEN JSON_VALUE(p.ImageUrls, '$[0]')
         ELSE NULL END                                         AS CurrentImageUrl,
    p.ImageUrls,
    pc.CompatibleMakes,
    pc.CompatibleModels,
    pc.YearFrom,
    pc.YearTo,
    pc.CompatibleEngines,
    pc.OemNumbers                                              AS CompatOemNumbers,
    pc.AlternativeNumbers
FROM dbo.Parts p
LEFT JOIN dbo.Categories          cat ON cat.Id = p.CategoryId
LEFT JOIN dbo.Brands              br  ON br.Id  = p.BrandId
LEFT JOIN dbo.UsedCars            uc  ON uc.Id  = p.UsedCarId
LEFT JOIN dbo.CarModels           cm  ON cm.Id  = uc.CarModelId
LEFT JOIN dbo.CarBrands           cb  ON cb.Id  = cm.CarBrandId
LEFT JOIN dbo.PartCompatibilities pc  ON pc.PartId = p.Id
WHERE p.IsActive = 1
ORDER BY p.Id;
""")
    cols = [d[0] for d in cursor.description]
    rows = []
    for row in cursor.fetchall():
        d = dict(zip(cols, row))
        if d["Id"] in resume_ids:
            continue
        rows.append(d)
    return rows


def already_processed_ids(conn: pyodbc.Connection) -> set[int]:
    """Return part IDs that already have an enrichment row (for --resume)."""
    cursor = conn.cursor()
    try:
        cursor.execute("SELECT DISTINCT PartId FROM dbo.PartImageEnrichment;")
        return {row[0] for row in cursor.fetchall()}
    except Exception:
        return set()


# ── image search ───────────────────────────────────────────────────────────────
def build_queries(part: dict) -> list[str]:
    """
    Build ordered list of search queries from most specific to least specific.
    We stop as soon as we find a HIGH-confidence match.
    """
    queries = []
    name        = (part["PartName"]  or "").strip()
    oem         = (part["OEMNumber"] or "").strip()
    brand       = (part["Brand"]     or "").strip()
    make        = (part["CarMake"]   or "").strip()
    model       = (part["CarModel"]  or "").strip()
    year        = str(part["CarYear"] or "").strip()
    engine      = (part["EngineType"]  or "").strip()
    c_makes     = (part["CompatibleMakes"]    or "").strip()
    c_models    = (part["CompatibleModels"]   or "").strip()
    c_oems      = (part["CompatOemNumbers"]   or "").strip()
    c_alt       = (part["AlternativeNumbers"] or "").strip()
    year_from   = str(part["YearFrom"] or "").strip()
    year_to     = str(part["YearTo"]   or "").strip()

    # Tier 1 — OEM number (most specific, highest confidence)
    if oem:
        queries.append(f"{oem} {name}")
        if make:
            queries.append(f"{oem} {make} {model} spare part")
    if c_oems:
        for o in re.split(r"[,;|]+", c_oems):
            o = o.strip()
            if o:
                queries.append(f"{o} {name} spare part")
    if c_alt:
        for o in re.split(r"[,;|]+", c_alt):
            o = o.strip()
            if o:
                queries.append(f"{o} {name}")

    # Tier 2 — specific make/model/year + part name
    if make and model and year:
        queries.append(f"{make} {model} {year} {name}")
    if make and model:
        queries.append(f"{make} {model} {name} spare part")

    # From PartCompatibilities
    if c_makes and c_models:
        mk = c_makes.split(",")[0].strip()
        md = c_models.split(",")[0].strip()
        yr_range = f"{year_from}-{year_to}" if year_from and year_to else year_from or year_to
        queries.append(f"{mk} {md} {yr_range} {name}".strip())

    # Tier 3 — part name + brand + vehicle generation
    if brand and make:
        queries.append(f"{brand} {name} {make}")
    if brand:
        queries.append(f"{brand} {name} automotive")

    # Tier 4 — generic part name only (will yield MEDIUM at best)
    queries.append(f"{name} automotive spare part")
    if make:
        queries.append(f"{make} {name}")

    # Deduplicate while preserving order
    seen = set()
    result = []
    for q in queries:
        q = " ".join(q.split())  # normalise whitespace
        if q and q not in seen:
            seen.add(q)
            result.append(q)
    return result


# ── image sources ──────────────────────────────────────────────────────────────
class ImageCandidate:
    __slots__ = [
        "image_url", "thumb_url", "source_page_url", "source_domain",
        "title", "creator", "license", "raw_score",
    ]

    def __init__(self, image_url, thumb_url, source_page_url, source_domain, title, creator="", license_="", raw_score=0):
        self.image_url      = image_url
        self.thumb_url      = thumb_url or image_url
        self.source_page_url = source_page_url
        self.source_domain  = source_domain
        self.title          = title or ""
        self.creator        = creator or ""
        self.license        = license_ or ""
        self.raw_score      = raw_score  # source-provided relevance score


def search_openverse(query: str, http: requests.Session) -> list[ImageCandidate]:
    """
    Query the Openverse API for CC-licensed images.
    Returns up to 10 candidates sorted by relevance.
    """
    url = "https://api.openverse.org/v1/images/"
    params = {
        "q": query,
        "page_size": 10,
        "format": "json",
        "mature": "false",
    }
    try:
        resp = http.get(url, params=params, timeout=15)
        if resp.status_code != 200:
            return []
        data = resp.json()
        candidates = []
        for item in data.get("results", []):
            img_url  = item.get("url") or item.get("foreign_landing_url")
            thumb    = item.get("thumbnail")
            page_url = item.get("foreign_landing_url", "")
            domain   = _domain(page_url)
            title    = item.get("title", "")
            creator  = item.get("creator", "")
            license_ = item.get("license", "")
            if img_url:
                candidates.append(ImageCandidate(
                    image_url=img_url,
                    thumb_url=thumb,
                    source_page_url=page_url,
                    source_domain=domain,
                    title=title,
                    creator=creator,
                    license_=license_,
                    raw_score=0,
                ))
        return candidates
    except Exception as exc:
        log.debug(f"Openverse search error for '{query}': {exc}")
        return []


def search_wikimedia(query: str, http: requests.Session) -> list[ImageCandidate]:
    """
    Query Wikimedia Commons for freely licensed images.
    """
    url = "https://commons.wikimedia.org/w/api.php"
    params = {
        "action": "query",
        "list": "search",
        "srnamespace": "6",   # File namespace
        "srsearch": query,
        "srlimit": 10,
        "srinfo": "",
        "srprop": "title|snippet",
        "format": "json",
    }
    try:
        resp = http.get(url, params=params, timeout=15)
        if resp.status_code != 200:
            return []
        data = resp.json()
        candidates = []
        for item in data.get("query", {}).get("search", []):
            title = item.get("title", "")  # e.g. "File:BMW_oil_filter.jpg"
            # Derive direct image URL via Wikimedia thumb API
            if not title.startswith("File:"):
                continue
            fname = title[len("File:"):]
            # MD5-based path
            md5 = hashlib.md5(fname.replace(" ", "_").encode()).hexdigest()
            img_url = (
                f"https://upload.wikimedia.org/wikipedia/commons/"
                f"{md5[0]}/{md5[:2]}/{quote_plus(fname.replace(' ', '_'))}"
            )
            page_url = f"https://commons.wikimedia.org/wiki/{quote_plus(title.replace(' ', '_'))}"
            clean_title = re.sub(r'\.(jpg|jpeg|png|webp|gif|svg)$', '', fname, flags=re.IGNORECASE)
            clean_title = clean_title.replace("_", " ")
            candidates.append(ImageCandidate(
                image_url=img_url,
                thumb_url=img_url,
                source_page_url=page_url,
                source_domain="commons.wikimedia.org",
                title=clean_title,
                license_="CC",
            ))
        return candidates
    except Exception as exc:
        log.debug(f"Wikimedia search error for '{query}': {exc}")
        return []


def search_google_cse(query: str, http: requests.Session, api_key: str, cx: str) -> list[ImageCandidate]:
    """
    Google Custom Search API — requires GOOGLE_CSE_API_KEY and GOOGLE_CSE_CX env vars.
    """
    url = "https://www.googleapis.com/customsearch/v1"
    params = {
        "key": api_key,
        "cx": cx,
        "q": query,
        "searchType": "image",
        "num": 10,
        "safe": "active",
        "imgType": "photo",
    }
    try:
        resp = http.get(url, params=params, timeout=15)
        if resp.status_code != 200:
            return []
        data = resp.json()
        candidates = []
        for item in data.get("items", []):
            img_url  = item.get("link", "")
            page_url = item.get("image", {}).get("contextLink", "")
            title    = item.get("title", "")
            domain   = _domain(page_url)
            candidates.append(ImageCandidate(
                image_url=img_url,
                thumb_url=item.get("image", {}).get("thumbnailLink", img_url),
                source_page_url=page_url,
                source_domain=domain,
                title=title,
            ))
        return candidates
    except Exception as exc:
        log.debug(f"Google CSE error for '{query}': {exc}")
        return []


def _domain(url: str) -> str:
    m = re.match(r"https?://([^/]+)", url)
    return m.group(1) if m else ""


# ── image validation ───────────────────────────────────────────────────────────
def validate_image(url: str, http: requests.Session, seen_urls: set[str]) -> dict:
    """
    HEAD request to check reachability, content-type, and content-length.
    Returns a dict with keys: reachable, content_type, content_length, skip_reason.
    """
    if url in seen_urls:
        return {"reachable": False, "content_type": None, "content_length": None, "skip_reason": "duplicate_url"}

    try:
        resp = http.head(url, timeout=10, allow_redirects=True)
        ct  = resp.headers.get("Content-Type", "")
        cl  = resp.headers.get("Content-Length")
        cl_int = int(cl) if cl and cl.isdigit() else None

        if resp.status_code >= 400:
            return {"reachable": False, "content_type": ct, "content_length": cl_int,
                    "skip_reason": f"http_{resp.status_code}"}
        if "image" not in ct.lower():
            return {"reachable": True, "content_type": ct, "content_length": cl_int,
                    "skip_reason": "not_image_content_type"}
        if cl_int is not None and cl_int < MIN_IMAGE_BYTES:
            return {"reachable": True, "content_type": ct, "content_length": cl_int,
                    "skip_reason": "too_small"}
        # Flag known watermark CDNs
        watermark_patterns = ["shutterstock", "gettyimages", "istockphoto", "dreamstime", "depositphotos"]
        if any(p in url.lower() for p in watermark_patterns):
            return {"reachable": True, "content_type": ct, "content_length": cl_int,
                    "skip_reason": "watermarked_source"}
        return {"reachable": True, "content_type": ct, "content_length": cl_int, "skip_reason": None}
    except Exception as exc:
        return {"reachable": False, "content_type": None, "content_length": None,
                "skip_reason": f"request_error: {exc}"}


# ── confidence scoring ─────────────────────────────────────────────────────────
def score_candidate(
    candidate: ImageCandidate,
    part: dict,
    query: str,
    query_tier: int,    # 1=OEM, 2=make/model/year, 3=brand, 4=generic
    validation: dict,
) -> tuple[float, str, str]:
    """
    Returns (score: 0-100, confidence_level: high|medium|low, match_reason: str).
    """
    if not validation["reachable"] or validation["skip_reason"]:
        return 0.0, "low", f"invalid: {validation['skip_reason']}"

    score = 0.0
    reasons = []

    name_lower    = (part["PartName"]  or "").lower()
    oem_lower     = (part["OEMNumber"] or "").lower()
    title_lower   = candidate.title.lower()
    query_lower   = query.lower()

    # ── Title relevance check (most important signal) ──────────────────────
    # Extract the key nouns from the part name
    part_words = set(re.findall(r'\b\w{3,}\b', name_lower)) - {"the", "for", "and", "with", "set"}
    title_words = set(re.findall(r'\b\w{3,}\b', title_lower))
    automotive_in_title = bool(title_words & AUTOMOTIVE_KEYWORDS)

    # How many part-name words appear in the title?
    overlap = part_words & title_words
    overlap_ratio = len(overlap) / max(len(part_words), 1)

    if not automotive_in_title and overlap_ratio < 0.25:
        # Title has no automotive keywords and barely matches part name → reject
        return 2.0, "low", "image title unrelated to part"

    # ── OEM number in title ──────────────────────────────────────────────────
    if oem_lower and oem_lower in title_lower:
        score += 40
        reasons.append("OEM in title")

    # ── Query tier bonus ─────────────────────────────────────────────────────
    tier_bonus = {1: 30, 2: 20, 3: 10, 4: 0}
    score += tier_bonus.get(query_tier, 0)
    reasons.append(f"tier{query_tier}_query")

    # ── Title word overlap ───────────────────────────────────────────────────
    score += overlap_ratio * 20
    if overlap:
        reasons.append(f"title_overlap({','.join(sorted(overlap)[:3])})")

    # ── Automotive keyword in title ──────────────────────────────────────────
    if automotive_in_title:
        score += 10
        reasons.append("automotive_title")

    # ── Source domain bonus ──────────────────────────────────────────────────
    trusted_domains = {
        "commons.wikimedia.org": 10,
        "upload.wikimedia.org": 10,
        "live.staticflickr.com": 3,
        "flickr.com": 3,
    }
    domain_bonus = trusted_domains.get(candidate.source_domain, 0)
    score += domain_bonus

    # ── Content-length bonus (bigger = more likely a real product photo) ────
    cl = validation.get("content_length") or 0
    if cl > 100_000:
        score += 5
        reasons.append("large_image")
    elif cl > 30_000:
        score += 2

    score = min(score, 100.0)

    # ── Map score to confidence level ────────────────────────────────────────
    # HIGH: OEM match OR tier-1 query + strong title overlap
    # MEDIUM: tier-2 query + some automotive relevance
    # LOW: everything else
    if score >= 65:
        level = "high"
    elif score >= 35:
        level = "medium"
    else:
        level = "low"

    reason_str = "; ".join(reasons)
    return round(score, 2), level, reason_str


# ── per-part processing ────────────────────────────────────────────────────────
def process_part(
    part: dict,
    http: requests.Session,
    seen_urls: set[str],
    google_api_key: str,
    google_cx: str,
    min_confidence: str,
    replace_existing: bool,
    report_only: bool,
) -> dict:
    """
    Process a single part: search → validate → score → return result dict.
    """
    result = {
        "PartId":           part["Id"],
        "InternalCode":     part["InternalCode"],
        "PartName":         part["PartName"],
        "CarMake":          part["CarMake"],
        "CarModel":         part["CarModel"],
        "CarYear":          part["CarYear"],
        "Category":         part["Category"],
        "OEMNumber":        part["OEMNumber"],
        "OldImageUrl":      part["CurrentImageUrl"],
        "SelectedImageUrl": None,
        "ThumbUrl":         None,
        "SourcePageUrl":    None,
        "SourceDomain":     None,
        "SearchQueryUsed":  None,
        "ConfidenceScore":  0.0,
        "ConfidenceLevel":  "low",
        "MatchReason":      None,
        "ContentType":      None,
        "ContentLengthBytes": None,
        "ImageReachable":   False,
        "Status":           "skipped",
        "ErrorMessage":     None,
    }

    # If already has an image and we're not replacing, skip
    if part["CurrentImageUrl"] and not replace_existing:
        # Still check if it's from Openverse (suspicious by default)
        if "openverse.org" in (part["CurrentImageUrl"] or ""):
            result["Status"] = "suspicious_existing"
            result["ErrorMessage"] = "Existing image is from Openverse — likely wrong. Use --replace-existing to overwrite."
        else:
            result["Status"] = "skipped"
            result["ErrorMessage"] = "Already has non-Openverse image. Use --replace-existing to overwrite."
        return result

    if report_only:
        result["Status"] = "report_only"
        return result

    queries = build_queries(part)
    best_result = None
    best_score  = -1.0

    for tier_idx, query in enumerate(queries, start=1):
        # Determine query tier for scoring
        oem = (part["OEMNumber"] or "").strip()
        c_oem = (part["CompatOemNumbers"] or "").strip()
        if tier_idx == 1 and (oem or c_oem):
            query_tier = 1
        elif tier_idx <= 3 and (part["CarMake"] or "").strip():
            query_tier = 2
        elif (part["Brand"] or "").strip():
            query_tier = 3
        else:
            query_tier = 4

        # Search sources
        candidates: list[ImageCandidate] = []

        if google_api_key and google_cx:
            candidates += search_google_cse(query, http, google_api_key, google_cx)
            time.sleep(RATE_LIMIT_DELAY)

        candidates += search_openverse(query, http)
        time.sleep(RATE_LIMIT_DELAY)

        candidates += search_wikimedia(query, http)
        time.sleep(RATE_LIMIT_DELAY)

        for cand in candidates:
            validation = validate_image(cand.image_url, http, seen_urls)
            time.sleep(RATE_LIMIT_DELAY / 4)

            score, level, reason = score_candidate(cand, part, query, query_tier, validation)

            if score > best_score:
                best_score  = score
                best_result = {
                    "SelectedImageUrl":   cand.image_url,
                    "ThumbUrl":           cand.thumb_url,
                    "SourcePageUrl":      cand.source_page_url,
                    "SourceDomain":       cand.source_domain,
                    "SearchQueryUsed":    query,
                    "ConfidenceScore":    score,
                    "ConfidenceLevel":    level,
                    "MatchReason":        reason,
                    "ContentType":        validation["content_type"],
                    "ContentLengthBytes": validation["content_length"],
                    "ImageReachable":     validation["reachable"],
                }

        # If we already have a HIGH-confidence result, stop searching
        if best_result and best_result["ConfidenceLevel"] == "high":
            break

    if best_result is None or best_result["ConfidenceLevel"] == "low":
        result["Status"] = "skipped"
        result["ErrorMessage"] = "No image found with sufficient confidence"
        if best_result:
            result.update(best_result)
        return result

    result.update(best_result)

    # Apply status based on confidence and min_confidence filter
    conf_order = {"high": 3, "medium": 2, "low": 1}
    if conf_order[best_result["ConfidenceLevel"]] >= conf_order[min_confidence]:
        if best_result["ConfidenceLevel"] == "high":
            result["Status"] = "auto_approved"
            seen_urls.add(best_result["SelectedImageUrl"])
        else:
            result["Status"] = "pending_review"
    else:
        result["Status"] = "skipped"
        result["ErrorMessage"] = f"Below min-confidence threshold ({min_confidence})"

    return result


# ── DB writes ──────────────────────────────────────────────────────────────────
def upsert_enrichment_row(conn: pyodbc.Connection, r: dict):
    """Insert or update a row in PartImageEnrichment."""
    cursor = conn.cursor()
    cursor.execute("""
MERGE dbo.PartImageEnrichment AS target
USING (SELECT @PartId AS PartId) AS source
ON target.PartId = source.PartId
WHEN MATCHED THEN
    UPDATE SET
        SelectedImageUrl    = @SelectedImageUrl,
        ThumbUrl            = @ThumbUrl,
        SourcePageUrl       = @SourcePageUrl,
        SourceDomain        = @SourceDomain,
        SearchQueryUsed     = @SearchQueryUsed,
        ConfidenceScore     = @ConfidenceScore,
        ConfidenceLevel     = @ConfidenceLevel,
        MatchReason         = @MatchReason,
        ContentType         = @ContentType,
        ContentLengthBytes  = @ContentLengthBytes,
        ImageReachable      = @ImageReachable,
        OldImageUrl         = @OldImageUrl,
        Status              = @Status,
        FetchedAt           = SYSUTCDATETIME(),
        ErrorMessage        = @ErrorMessage,
        ModifiedAt          = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (PartId, SelectedImageUrl, ThumbUrl, SourcePageUrl, SourceDomain,
            SearchQueryUsed, ConfidenceScore, ConfidenceLevel, MatchReason,
            ContentType, ContentLengthBytes, ImageReachable, OldImageUrl,
            Status, FetchedAt, ErrorMessage, CreatedAt)
    VALUES (@PartId, @SelectedImageUrl, @ThumbUrl, @SourcePageUrl, @SourceDomain,
            @SearchQueryUsed, @ConfidenceScore, @ConfidenceLevel, @MatchReason,
            @ContentType, @ContentLengthBytes, @ImageReachable, @OldImageUrl,
            @Status, SYSUTCDATETIME(), @ErrorMessage, SYSUTCDATETIME());
""",
        r["PartId"],
        (r["SelectedImageUrl"] or "")[:1000] if r["SelectedImageUrl"] else None,
        (r["ThumbUrl"] or "")[:1000] if r["ThumbUrl"] else None,
        (r["SourcePageUrl"] or "")[:1000] if r["SourcePageUrl"] else None,
        (r["SourceDomain"] or "")[:200] if r["SourceDomain"] else None,
        (r["SearchQueryUsed"] or "")[:500] if r["SearchQueryUsed"] else None,
        r["ConfidenceScore"],
        r["ConfidenceLevel"],
        (r["MatchReason"] or "")[:500] if r["MatchReason"] else None,
        (r["ContentType"] or "")[:100] if r["ContentType"] else None,
        r["ContentLengthBytes"],
        1 if r["ImageReachable"] else 0,
        (r["OldImageUrl"] or "")[:1000] if r["OldImageUrl"] else None,
        r["Status"],
        (r["ErrorMessage"] or "")[:1000] if r["ErrorMessage"] else None,
    )


def apply_image_to_part(conn: pyodbc.Connection, part_id: int, image_url: str):
    """
    Write the approved image URL to Parts.ImageUrls.
    ONLY called when --dry-run false is set.
    """
    json_array = json.dumps([image_url])
    cursor = conn.cursor()
    cursor.execute(
        "UPDATE dbo.Parts SET ImageUrls = ?, ModifiedAt = SYSUTCDATETIME() WHERE Id = ?;",
        json_array, part_id
    )


# ── progress tracking ──────────────────────────────────────────────────────────
def load_progress() -> dict:
    if PROGRESS_FILE.exists():
        with open(PROGRESS_FILE, "r") as f:
            return json.load(f)
    return {"processed_ids": [], "last_run": None}


def save_progress(processed_ids: list[int]):
    with open(PROGRESS_FILE, "w") as f:
        json.dump({"processed_ids": processed_ids, "last_run": datetime.now().isoformat()}, f)


# ── HTML report ────────────────────────────────────────────────────────────────
def generate_html_report(results: list[dict], stamp: str, dry_run: bool) -> Path:
    report_path = REPORTS_DIR / f"image_enrichment_report_{stamp}.html"

    status_color = {
        "auto_approved":    "#22c55e",
        "pending_review":   "#f59e0b",
        "skipped":          "#6b7280",
        "failed":           "#ef4444",
        "report_only":      "#8b5cf6",
        "suspicious_existing": "#f97316",
    }

    counts = {}
    for r in results:
        counts[r["Status"]] = counts.get(r["Status"], 0) + 1

    summary_rows = "\n".join(
        f'<tr><td style="color:{status_color.get(s,"#666")};font-weight:bold">{s}</td><td>{c}</td></tr>'
        for s, c in sorted(counts.items())
    )

    def img_tag(url, w=120):
        if not url:
            return '<span style="color:#999;font-size:11px">no image</span>'
        return f'<img src="{url}" width="{w}" height="90" style="object-fit:cover;border-radius:4px;border:1px solid #ddd" onerror="this.style.display=\'none\'" loading="lazy">'

    rows_html = []
    for r in results:
        sc = status_color.get(r["Status"], "#888")
        vehicle = " ".join(filter(None, [str(r.get("CarMake") or ""), str(r.get("CarModel") or ""), str(r.get("CarYear") or "")]))
        conf_badge = ""
        if r.get("ConfidenceLevel"):
            badge_color = {"high": "#22c55e", "medium": "#f59e0b", "low": "#ef4444"}.get(r["ConfidenceLevel"], "#888")
            conf_badge = f'<span style="background:{badge_color};color:#fff;padding:2px 8px;border-radius:12px;font-size:11px">{r["ConfidenceLevel"].upper()}</span>'

        rows_html.append(f"""
<tr>
  <td style="font-size:12px;color:#555">{r["PartId"]}</td>
  <td>
    <strong style="font-size:13px">{r["PartName"]}</strong><br>
    <span style="color:#888;font-size:11px">{r["InternalCode"]} {('• ' + vehicle) if vehicle else ''}</span><br>
    <span style="color:#aaa;font-size:10px">{r.get("Category") or ''} {('• OEM: ' + r["OEMNumber"]) if r.get("OEMNumber") else ''}</span>
  </td>
  <td>{img_tag(r["OldImageUrl"])}</td>
  <td>{img_tag(r.get("ThumbUrl") or r.get("SelectedImageUrl"))}</td>
  <td>{conf_badge}<br><small style="color:#999">{r.get("ConfidenceScore") or 0:.1f}/100</small></td>
  <td style="font-size:11px;max-width:200px;word-break:break-all">
    {('<a href="' + r["SourcePageUrl"] + '" target="_blank" style="color:#3b82f6">' + (r.get("SourceDomain") or r["SourcePageUrl"])[:40] + '</a>') if r.get("SourcePageUrl") else '—'}
  </td>
  <td style="font-size:11px;color:#666;max-width:160px">{(r.get("MatchReason") or '—')[:80]}</td>
  <td>
    <span style="color:{sc};font-weight:bold;font-size:12px">{r["Status"]}</span>
    {('<br><small style="color:#999">' + (r.get("ErrorMessage") or "")[:60] + '</small>') if r.get("ErrorMessage") else ''}
  </td>
</tr>""")

    mode_banner = (
        '<div style="background:#fef9c3;border:1px solid #f59e0b;padding:12px 16px;border-radius:6px;margin-bottom:20px">'
        '⚠️ <strong>DRY-RUN MODE</strong> — No database changes were made. Review this report and then run with <code>--dry-run false</code> to apply.'
        '</div>'
    ) if dry_run else (
        '<div style="background:#dcfce7;border:1px solid #22c55e;padding:12px 16px;border-radius:6px;margin-bottom:20px">'
        '✅ <strong>LIVE MODE</strong> — HIGH-confidence images have been written to the database.'
        '</div>'
    )

    html = f"""<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>SpareParts Image Enrichment Report — {stamp}</title>
<style>
  * {{ box-sizing: border-box; margin: 0; padding: 0; }}
  body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background: #f8fafc; color: #1e293b; padding: 24px; }}
  h1 {{ font-size: 22px; margin-bottom: 4px; }}
  .meta {{ color: #64748b; font-size: 13px; margin-bottom: 24px; }}
  .summary {{ display: flex; gap: 16px; flex-wrap: wrap; margin-bottom: 24px; }}
  .stat-card {{ background: #fff; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px 20px; min-width: 140px; }}
  .stat-card .num {{ font-size: 28px; font-weight: 700; }}
  .stat-card .lbl {{ font-size: 12px; color: #64748b; margin-top: 2px; }}
  table {{ width: 100%; border-collapse: collapse; background: #fff; border-radius: 8px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,.1); }}
  th {{ background: #1e293b; color: #f1f5f9; text-align: left; padding: 10px 14px; font-size: 12px; font-weight: 600; text-transform: uppercase; letter-spacing: .5px; }}
  td {{ padding: 10px 14px; border-bottom: 1px solid #f1f5f9; vertical-align: middle; }}
  tr:last-child td {{ border-bottom: none; }}
  tr:hover td {{ background: #f8fafc; }}
  .filter-bar {{ margin-bottom: 16px; display: flex; gap: 10px; flex-wrap: wrap; align-items: center; }}
  .filter-bar button {{ padding: 6px 14px; border-radius: 20px; border: 1px solid #cbd5e1; background: #fff; cursor: pointer; font-size: 12px; }}
  .filter-bar button.active {{ background: #1e293b; color: #fff; border-color: #1e293b; }}
  summary-table {{ margin-bottom: 20px; }}
</style>
</head>
<body>
<h1>SpareParts — Image Enrichment Report</h1>
<p class="meta">Generated: {stamp} &nbsp;|&nbsp; Total parts processed: {len(results)} &nbsp;|&nbsp; Mode: {'DRY-RUN' if dry_run else 'LIVE'}</p>

{mode_banner}

<div class="summary">
  <div class="stat-card"><div class="num">{len(results)}</div><div class="lbl">Parts Processed</div></div>
  <div class="stat-card"><div class="num" style="color:#22c55e">{counts.get('auto_approved', 0)}</div><div class="lbl">Auto-Approved (HIGH)</div></div>
  <div class="stat-card"><div class="num" style="color:#f59e0b">{counts.get('pending_review', 0)}</div><div class="lbl">Pending Review (MEDIUM)</div></div>
  <div class="stat-card"><div class="num" style="color:#6b7280">{counts.get('skipped', 0)}</div><div class="lbl">Skipped / Low conf.</div></div>
  <div class="stat-card"><div class="num" style="color:#ef4444">{counts.get('failed', 0)}</div><div class="lbl">Failed</div></div>
  <div class="stat-card"><div class="num" style="color:#f97316">{counts.get('suspicious_existing', 0)}</div><div class="lbl">Suspicious Existing</div></div>
</div>

<details style="margin-bottom:20px">
<summary style="cursor:pointer;font-weight:600;font-size:13px">Status breakdown</summary>
<table style="max-width:300px;margin-top:10px">
  <tr><th>Status</th><th>Count</th></tr>
  {summary_rows}
</table>
</details>

<div class="filter-bar">
  <strong style="font-size:13px">Filter:</strong>
  <button class="active" onclick="filterTable('all')">All</button>
  <button onclick="filterTable('auto_approved')">✅ Auto-Approved</button>
  <button onclick="filterTable('pending_review')">🔍 Pending Review</button>
  <button onclick="filterTable('skipped')">⏭ Skipped</button>
  <button onclick="filterTable('suspicious_existing')">⚠️ Suspicious</button>
  <button onclick="filterTable('failed')">❌ Failed</button>
</div>

<table id="main-table">
  <thead>
    <tr>
      <th>ID</th>
      <th>Part</th>
      <th>Old Image</th>
      <th>Proposed Image</th>
      <th>Confidence</th>
      <th>Source</th>
      <th>Match Reason</th>
      <th>Status</th>
    </tr>
  </thead>
  <tbody>
    {''.join(rows_html)}
  </tbody>
</table>

<script>
function filterTable(status) {{
  document.querySelectorAll('.filter-bar button').forEach(b => b.classList.remove('active'));
  event.target.classList.add('active');
  document.querySelectorAll('#main-table tbody tr').forEach(tr => {{
    const s = tr.querySelector('td:last-child span') ? tr.querySelector('td:last-child span').textContent.trim() : '';
    tr.style.display = (status === 'all' || s === status) ? '' : 'none';
  }});
}}
</script>

</body>
</html>"""

    with open(report_path, "w", encoding="utf-8") as f:
        f.write(html)

    return report_path


# ── audit of existing images ───────────────────────────────────────────────────
def audit_existing_images(parts: list[dict], http: requests.Session, limit: int = 50) -> list[dict]:
    """
    Check current image URLs for reachability, duplicates, and Openverse contamination.
    Returns audit results list.
    """
    audited = []
    url_counts: dict[str, int] = {}
    for p in parts:
        url = p.get("CurrentImageUrl")
        if url:
            url_counts[url] = url_counts.get(url, 0) + 1

    for part in parts[:limit]:
        url = part.get("CurrentImageUrl")
        status = "missing"
        skip_reason = None
        reachable = None
        ct = None
        cl = None

        if url:
            if "openverse.org" in url:
                status = "suspicious"
                skip_reason = "Openverse URL — likely wrong Flickr photo"
            elif url_counts.get(url, 0) > 1:
                status = "duplicated"
                skip_reason = f"Same URL used by {url_counts[url]} parts"
            else:
                val = validate_image(url, http, set())
                reachable = val["reachable"]
                ct = val["content_type"]
                cl = val["content_length"]
                if not reachable:
                    status = "broken"
                    skip_reason = val["skip_reason"]
                elif val["skip_reason"]:
                    status = "suspicious"
                    skip_reason = val["skip_reason"]
                else:
                    status = "trusted"
            time.sleep(RATE_LIMIT_DELAY / 2)
        else:
            status = "missing"

        audited.append({
            "PartId":       part["Id"],
            "InternalCode": part["InternalCode"],
            "PartName":     part["PartName"],
            "CarMake":      part["CarMake"],
            "CarModel":     part["CarModel"],
            "ImageUrl":     url,
            "AuditStatus":  status,
            "SkipReason":   skip_reason,
            "Reachable":    reachable,
            "ContentType":  ct,
            "ContentLength": cl,
        })

    return audited


def save_audit_csv(audit_results: list[dict], stamp: str):
    path = BACKUPS_DIR / f"image_audit_{stamp}.csv"
    with open(path, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=audit_results[0].keys())
        writer.writeheader()
        writer.writerows(audit_results)
    return path


# ── main ───────────────────────────────────────────────────────────────────────
def main():
    parser = argparse.ArgumentParser(
        description="Enrich SpareParts part images from external sources.",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=__doc__,
    )
    parser.add_argument("--dry-run",          default="true",  help="true (default) = no DB writes. Pass 'false' to apply.")
    parser.add_argument("--yes",              action="store_true",           help="Skip live-run confirmation prompt (for batch/unattended use)")
    parser.add_argument("--limit",            type=int,        default=None, help="Process only first N parts")
    parser.add_argument("--batch-size",       type=int,        default=50,   help="DB transaction batch size (default 50)")
    parser.add_argument("--resume",           action="store_true",           help="Skip already-processed parts")
    parser.add_argument("--replace-existing", action="store_true",           help="Overwrite parts that already have an image")
    parser.add_argument("--min-confidence",   default="high",  choices=["high", "medium", "low"],
                        help="Minimum confidence to include in results (default: high)")
    parser.add_argument("--report-only",      action="store_true",           help="Audit current images, generate report, no fetching")
    parser.add_argument("--conn",             default=DEFAULT_CONN,          help="ODBC connection string")
    parser.add_argument("--skip-audit",       action="store_true",           help="Skip the initial 50-part audit step")
    args = parser.parse_args()

    dry_run = args.dry_run.lower() != "false"

    # Safety check
    if not dry_run and not args.yes:
        print("\n⚠️  WARNING: --dry-run false — this will write image URLs to the database.")
        print("   Are you sure? Type YES to continue: ", end="", flush=True)
        answer = input().strip()
        if answer != "YES":
            print("Aborted.")
            sys.exit(0)

    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")

    log.info("=" * 60)
    log.info(f"SpareParts Image Enrichment  |  {stamp}")
    log.info(f"Mode           : {'DRY-RUN (no DB writes)' if dry_run else '🔴 LIVE (will write to DB)'}")
    log.info(f"Limit          : {args.limit or 'all'}")
    log.info(f"Min confidence : {args.min_confidence}")
    log.info(f"Replace existing: {args.replace_existing}")
    log.info(f"Report only    : {args.report_only}")
    log.info("=" * 60)

    http = build_http_session()

    # ── Google CSE keys (optional) ────────────────────────────────────────────
    google_api_key = os.environ.get("GOOGLE_CSE_API_KEY", "")
    google_cx      = os.environ.get("GOOGLE_CSE_CX", "")
    if google_api_key:
        log.info("Google Custom Search API key detected — will use Google as primary source.")
    else:
        log.info("No Google CSE key — using Openverse + Wikimedia Commons.")

    # ── Connect ───────────────────────────────────────────────────────────────
    log.info("Connecting to SQL Server …")
    conn = get_connection(args.conn)
    log.info("Connected.")

    # ── Apply migration ───────────────────────────────────────────────────────
    ensure_enrichment_table(conn)
    log.info("PartImageEnrichment table ready.")

    # ── Resume support ────────────────────────────────────────────────────────
    resume_ids: set[int] = set()
    if args.resume:
        resume_ids = already_processed_ids(conn)
        log.info(f"Resume mode: skipping {len(resume_ids)} already-processed parts.")

    # ── Load parts ────────────────────────────────────────────────────────────
    log.info("Loading parts …")
    parts = fetch_parts(conn, args.limit, resume_ids)
    log.info(f"Loaded {len(parts)} parts to process.")

    if not parts:
        log.info("Nothing to process. Exiting.")
        conn.close()
        return

    # ── Audit existing images (first 50) ─────────────────────────────────────
    if not args.skip_audit and not args.report_only:
        log.info("\n── STEP 1: Auditing existing images (first 50 parts) ──")
        audit_results = audit_existing_images(parts, http, limit=50)
        audit_path = save_audit_csv(audit_results, stamp)
        log.info(f"Audit CSV: {audit_path}")
        status_summary = {}
        for a in audit_results:
            status_summary[a["AuditStatus"]] = status_summary.get(a["AuditStatus"], 0) + 1
        log.info(f"Audit summary: {status_summary}")

    # ── Process parts in batches ──────────────────────────────────────────────
    log.info(f"\n── STEP 2: Processing {len(parts)} parts in batches of {args.batch_size} ──")
    all_results: list[dict] = []
    processed_ids: list[int] = []
    seen_urls: set[str] = set()

    for batch_start in range(0, len(parts), args.batch_size):
        batch = parts[batch_start : batch_start + args.batch_size]
        log.info(f"\nBatch {batch_start // args.batch_size + 1}: parts {batch[0]['Id']} … {batch[-1]['Id']}")

        batch_results = []
        for part in batch:
            log.info(f"  [{part['Id']}] {part['PartName'][:50]}")
            try:
                result = process_part(
                    part=part,
                    http=http,
                    seen_urls=seen_urls,
                    google_api_key=google_api_key,
                    google_cx=google_cx,
                    min_confidence=args.min_confidence,
                    replace_existing=args.replace_existing,
                    report_only=args.report_only,
                )
            except Exception as exc:
                log.warning(f"    ERROR processing part {part['Id']}: {exc}")
                result = {
                    **{k: part.get(k) for k in ["Id","InternalCode","PartName","CarMake","CarModel","CarYear","Category","OEMNumber"]},
                    "PartId": part["Id"],
                    "OldImageUrl": part.get("CurrentImageUrl"),
                    "SelectedImageUrl": None, "ThumbUrl": None, "SourcePageUrl": None,
                    "SourceDomain": None, "SearchQueryUsed": None,
                    "ConfidenceScore": 0.0, "ConfidenceLevel": "low", "MatchReason": None,
                    "ContentType": None, "ContentLengthBytes": None, "ImageReachable": False,
                    "Status": "failed", "ErrorMessage": str(exc)[:200],
                }

            lvl = result.get("ConfidenceLevel","?")
            sc  = result.get("ConfidenceScore",0)
            st  = result.get("Status","?")
            log.info(f"    → {st} | conf={lvl}({sc:.0f}) | {result.get('SelectedImageUrl','—')[:60] if result.get('SelectedImageUrl') else '—'}")

            batch_results.append(result)
            processed_ids.append(part["Id"])

        # ── Write batch to PartImageEnrichment (always, even in dry-run) ─────
        if not dry_run or True:   # always write enrichment rows (they're metadata, not the image)
            try:
                for r in batch_results:
                    upsert_enrichment_row(conn, r)
                conn.commit()
                log.info(f"  Wrote {len(batch_results)} enrichment rows.")
            except Exception as exc:
                conn.rollback()
                log.error(f"  DB write failed for batch: {exc}")

        # ── Apply to Parts.ImageUrls only if NOT dry-run AND auto_approved ───
        if not dry_run:
            applied = 0
            for r in batch_results:
                if r["Status"] == "auto_approved" and r.get("SelectedImageUrl"):
                    try:
                        apply_image_to_part(conn, r["PartId"], r["SelectedImageUrl"])
                        applied += 1
                    except Exception as exc:
                        log.warning(f"  Failed to apply image for part {r['PartId']}: {exc}")
            if applied:
                conn.commit()
                log.info(f"  Applied {applied} images to Parts.ImageUrls.")

        all_results.extend(batch_results)

        # Save progress after each batch
        save_progress(processed_ids)
        log.info(f"  Progress saved ({len(processed_ids)} parts done).")

    # ── Generate HTML report ──────────────────────────────────────────────────
    log.info("\n── STEP 3: Generating HTML report ──")
    report_path = generate_html_report(all_results, stamp, dry_run)
    log.info(f"Report: {report_path}")

    # ── Print summary ─────────────────────────────────────────────────────────
    status_counts: dict[str, int] = {}
    for r in all_results:
        status_counts[r["Status"]] = status_counts.get(r["Status"], 0) + 1

    log.info("\n" + "=" * 60)
    log.info("SUMMARY")
    log.info("=" * 60)
    log.info(f"Total processed   : {len(all_results)}")
    for status, count in sorted(status_counts.items()):
        log.info(f"  {status:<22}: {count}")
    log.info(f"\nReport saved to   : {report_path}")
    if dry_run:
        log.info("\n⚠️  DRY-RUN complete — no images were written to the database.")
        log.info("   Review the report above, then run with --dry-run false to apply.")
    else:
        applied = status_counts.get("auto_approved", 0)
        log.info(f"\n✅ LIVE run complete — {applied} images applied to Parts.ImageUrls.")

    conn.close()


if __name__ == "__main__":
    main()
