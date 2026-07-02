# SpareParts Database Architecture Reference

This document is a complete reference for the SpareParts SQL Server schema:
what tables exist, how they relate, how the schema is built and evolved
(both the raw SQL in `database/` and the C# runtime migrations in
`src/SpareParts.Api/Infrastructure/`), and what every script in `scripts/`
does. It is written so a new engineer can onboard on the data layer from
this document alone, without having to read all ~4,500 lines of
`database/schema.sql` or all ~70 C# migration files line by line.

Database engine: **SQL Server**. Data access: **Dapper** (no EF Core; no
LINQ-to-SQL). Repositories live in `src/SpareParts.Infrastructure/`.

---

## 1. The two-source schema model

There is **no single file that fully describes the schema**. It is built
from two independent, additive sources that must both run to get a
production-shaped database:

1. **`database/schema.sql`** — a large, hand-maintained, fully idempotent
   SQL script (every statement is guarded with `IF OBJECT_ID(...) IS
   NULL` / `IF COL_LENGTH(...) IS NULL` checks). It contains the original
   "base" tables plus a large block of migrations that were historically
   run from C# and were later **copied verbatim into this file** under a
   `-- MIGRATIONS (moved from API startup ...)` banner (see below). It is
   meant to be run once, manually, against a fresh database (e.g. for
   staging bootstrap, `sqlcmd`, or SSMS) — see `docs/deployment-plan.md`
   and `scripts/deploy-azure-free.ps1` for where this is invoked.

2. **C# runtime migrations** — `src/SpareParts.Api/Infrastructure/*Migration.cs`
   (~70 files). Each exposes a static `EnsureApplied(ISqlConnectionFactory
   factory)` method containing idempotent raw SQL (via Dapper `Execute`),
   guarded the same way as `schema.sql`. These run **automatically on
   every API process startup**, in a fixed order, from `RunMigrations()`
   in `src/SpareParts.Api/Hosting/SparePartsApiComposition.cs` (around
   line 544). This is the actual source of truth for the live schema —
   it is what runs in every environment (local dev, staging, production)
   every time the API boots.

**Important consequence:** `database/schema.sql` and the C# migrations
overlap heavily but are not byte-identical, and `schema.sql` is missing
some tables entirely (e.g. everything from `PartRequestsMigration.cs`
onward through `PartImageEnrichmentMigration.cs` in the ordering below —
`schema.sql`'s own migrations block does include copies of many of these,
but newer C# migrations added after the last "sync" are not reflected in
`schema.sql`). **The C# migration pipeline is authoritative.** A database
built only from `schema.sql` is not guaranteed to match what the running
API code expects; the API's own migrations will patch it further the
first time the API starts against that database. See the header comment
now present at the top of `database/schema.sql` for a pointer back to
this document.

### Why this matters for TenantId specifically

Multi-tenancy was retrofitted. `database/schema.sql` creates tables like
`Stock` and `StockMovements` with **no `TenantId` column at all**. The
column is added — along with a backfill of existing rows to the default
tenant and a lookup index — exclusively by
`src/SpareParts.Api/Infrastructure/TenantIdMigration.cs`, which runs
after `TenantsMigration.cs` (which creates `dbo.Tenants` and seeds
`Id = 1, Code = 'default'`). `database/schema.sql` now carries an explicit
header comment calling this out. See section 4 below for the full list of
tenant-owned tables.

---

## 2. Table groups and relationships

All tables below live in the `dbo` schema. FK arrows read "child → parent".

### 2.1 Reference / catalog data

| Table | Purpose | Key FKs |
|---|---|---|
| `AppConstants` | Key/value app-wide settings (base currency, counter currency, default transaction type name, etc.) | — |
| `Brands` | Part manufacturer brands | — |
| `Categories` | Part categories, self-referencing tree via `ParentId` | `ParentId → Categories.Id` |
| `CarBrands` | Car manufacturer brands (for used-car catalog), includes logo BLOB | — |
| `CarModels` | Car models under a brand, includes image BLOB, `BodyType` | `CarBrandId → CarBrands.Id` |
| `CurrencyRates` | Latest FX rate snapshot per currency code | — |
| `Warehouses` | Physical warehouses, one flagged `IsMain`, has `Barcode` | — |
| `Location` (singular) | Shipping/used-car origin locations, has `ShippingFees`/currency | `CreatedByUserId`/`ModifiedByUserId → Users.Id` |
| `Locations` (plural) | Warehouse shelf/bin positions | `WarehouseId → Warehouses.Id` |
| `Roles` | User roles (Admin/Manager/Cashier/Web App User + tenant admin roles), badge colors, `IsSystem` flag | — |
| `Users` | Application users, password hash, role | `RoleId → Roles.Id` |
| `Tenants` | Multi-tenant registry, `Code` unique, default tenant `Id=1` | — (created only by `TenantsMigration.cs`, not in `schema.sql`) |

`Location` vs `Locations` is a real, intentional naming collision in this
codebase — not a typo. `Location` (singular, `LocationID` PK) is a
shipping/origin location used by `UsedCars`. `Locations` (plural, `Id` PK)
is a warehouse shelf/bin position used by `Stock`. Keep them distinct when
reading repository code.

### 2.2 Customers / Suppliers

| Table | Purpose | Key FKs |
|---|---|---|
| `Customers` | Customer master data, credit limit, opening balance | `AccountId → Accounts.Id` (1:1, unique filtered index `UX_Customers_AccountId`) |
| `Suppliers` | Supplier master data, opening balance | `AccountId → Accounts.Id` (1:1, unique filtered index `UX_Suppliers_AccountId`) |

`AccountingMigration` auto-creates one ledger `Accounts` row per customer
(code `CUST-000123`) and per supplier (code `SUP-000123`) the first time
it runs, parented under control accounts `1200 Customer Accounts` and
`2100 Supplier Accounts` respectively.

### 2.3 Used cars (donor vehicles / teardown source)

| Table | Purpose | Key FKs |
|---|---|---|
| `UsedCars` | A purchased donor vehicle: price (base + counter currency), shipping/customs/repairs costs, received/shipped flags, `ExpectedSellThroughRate` | `CarModelId → CarModels.Id`, `LocationId → Location.LocationID`, `SupplierId → Suppliers.Id` |
| `usedcar_images` | BLOB images attached to a used car (snake_case name — see section 5) | `UsedCarId → UsedCars.Id` |
| `UsedCarPurchases` | Formal purchase document/posting record for a used car (number, currency pair, payment/posting status) | `UsedCarId → UsedCars.Id`, `SupplierId → Suppliers.Id` |
| `UsedCarPurchaseLines` | Line-item cost breakdown of a purchase (transportation, customs, etc.), each tagged to a GL account | `UsedCarPurchaseId → UsedCarPurchases.Id` (cascade delete), `AccountId → Accounts.Id` |
| `UsedCarWholesaleSales` | Selling a used car whole (not parted out) — buyer info, price in base+counter currency, optional repair cost breakdown as JSON | `UsedCarId → UsedCars.Id` (1:1, unique index), `CustomerId → Customers.Id` |

`Parts.UsedCarId` (see below) links individual parted-out parts back to
their donor vehicle — this replaced an earlier `UsedCarParts` bridge
table, which `PartUsedCarMigration` drops after backfilling `Parts.UsedCarId`
from it.

Many more used-car domain tables exist **only as C# migrations** (not in
`schema.sql`), reflecting an ongoing marketplace-style expansion: `UsedCarStateEventsMigration`,
`UsedCarTeardownMigration`, `PartGenealogyMigration`, `HalfCutsMigration`,
`YardToursMigration`, `NewVsUsedPricingMigration`, and the "innovation
feature" migrations layered on top (negotiations, instant offers,
inspection requests, condition certificates, etc. — see section 3).

### 2.4 Parts / inventory

| Table | Purpose | Key FKs |
|---|---|---|
| `Parts` | The core part catalog row: internal code, barcode, OEM number, condition, multiple price fields (cost/sale/average/estimated market/minimum sell/fast-sale/wholesale/recommended), `PricingStatus`, currency, `MinStock`, `UsedCarId` | `CategoryId → Categories.Id`, `BrandId → Brands.Id`, `UsedCarID → UsedCars.Id` |
| `Stock` | On-hand quantity of a part in a warehouse (+ optional shelf location), reserved quantity | `PartId → Parts.Id`, `WarehouseId → Warehouses.Id`, `LocationId → Locations.Id` |
| `StockMovements` | Append-only ledger of every quantity change (`MovementType`, signed `Quantity`, `UnitCost`, `ReferenceType`/`ReferenceId` pointing back to the source document, `ScanCode`) | `PartId → Parts.Id`, `WarehouseId → Warehouses.Id` |

`Stock` and `StockMovements` are the two hottest tables in the system —
`HotPathIndexMigration` (the last block in `schema.sql`) adds
non-clustered indexes on `(PartId, WarehouseId)`, `WarehouseId`,
`(PartId, CreatedAt DESC)`, and `(ReferenceType, ReferenceId)` for these
specifically, based on known repository query shapes.

Other parts-adjacent tables (all C#-migration-only, not in `schema.sql`):
`ReorderRules`, `PartSubstitutes`, `PartExpiryMigration` fields on `Parts`,
`SupplierPriceHistory`, `PartReservationsMigration`, `PartCompatibilityMigration`,
`PartMarkdownMigration` fields, `PartMarketingNotificationMigration`,
`PartReelsMigration`, `PartPassportPhotosMigration`.

### 2.5 Sales / invoicing / transactions

The system uses a **unified transaction model**, not separate
"SalesInvoices"/"PurchaseInvoices" tables (those names exist only as
**legacy, defensively-guarded** statements — see below).

| Table | Purpose | Key FKs |
|---|---|---|
| `TransactionTypes` | The set of transaction kinds (`sale`, `sale_return`, `purchase`, `used_car_purchase`, ...), each with its own currency, counter rate, and auto-incrementing serial-number format/counter | — |
| `Transactions` | One row per invoice/purchase/return/used-car-purchase document. Header-level totals (subtotal, discount, tax, total, paid, in both base and counter currency), payment status/method, posting status (draft/posted), links to customer/supplier/warehouse/used car, `IsReturn` + `ParentReferenceId` for returns | `TransactionTypeId → TransactionTypes.Id`, optionally `CustomerId → Customers.Id`, `SupplierId → Suppliers.Id`, `WarehouseId → Warehouses.Id`, `UsedCarId → UsedCars.Id` |
| `TransactionItems` | Line items on a transaction — either a `PartId` line or an `AccountId` (fee/discount/GL) line, quantity, unit price/cost, discount, tax rate, currency + base/counter amounts | `TransactionId → Transactions.Id`, optionally `PartId → Parts.Id`, `AccountId → Accounts.Id` |

Key constraints: `UX_Transactions_Type_ReferenceId` (unique per type +
reference id) and `UX_Transactions_Type_Number` (unique per type +
transaction number) — both created by `TransactionsMigration`.

**Legacy `SalesInvoices` / `PurchaseInvoices` note:** `InvoiceNumberingMigration`
and parts of `TransactionsMigration` contain statements like
`IF OBJECT_ID('dbo.SalesInvoices', 'U') IS NOT NULL ...`. These are
**defensive/backward-compatible** statements only — neither `schema.sql`
nor any current C# migration *creates* a `SalesInvoices` or
`PurchaseInvoices` table. They exist to safely patch such tables *if*
they happen to exist from an older schema generation (e.g. a database
migrated forward from a pre-unification version of the app), migrating
their data/serials into the unified `Transactions`/`TransactionItems`
model. On a fresh database these blocks are no-ops.

`InvoiceNumberingMigration` also creates two SQL Server `SEQUENCE`
objects, `SalesInvoiceNumberSequence` and `PurchaseInvoiceNumberSequence`,
used to generate gap-tolerant, high-concurrency-safe invoice numbers.

Sales-adjacent tables that exist only via C# migrations: `Quotes` /
`QuoteItems` (also present in `schema.sql`'s migrations block),
`RepairOrdersMigration`, `Shipments` / `ShipmentEvents`, `WarrantyClaims`,
`CustomerLoyaltyMigration` → `LoyaltyTransactions`, `CustomerCreditLimitMigration`,
`CustomerPriceTierMigration`, `TransactionsPaymentReminderMigration`,
`SalesReturnTypeMigration`.

### 2.6 Accounting / general ledger

| Table | Purpose | Key FKs |
|---|---|---|
| `AccountingAccountTypes` | Lookup: `asset`, `liability`, `equity`, `income`, `expense` | — |
| `Accounts` | Chart of accounts, hierarchical via `ParentId`, tagged with `AccountTypeKey` | `ParentId → Accounts.Id` (self), `AccountTypeKey → AccountingAccountTypes.TypeKey` |
| `AccountingPostingRoles` | Lookup of "roles" a posting can target (`sales_cash`, `sales_revenue`, `cogs`, `inventory`, `purchase_offset`, `used_car_price`, `used_car_transportation`, `used_car_partout`, `used_car_shipping`, `used_car_customs`, `used_car_repairs`) | — |
| `AccountingPostingSettings` | Maps each posting role to a concrete `Accounts.Id` (the "which GL account does 'cogs' post to" config) | `SettingKey → AccountingPostingRoles.RoleKey`, `AccountId → Accounts.Id` |
| `JournalEntries` | One row per journal posting event (`EntryDate`, `ReferenceType`/`ReferenceId` back to the source document, e.g. `Sale`, `UsedCarPurchase`, `Manual`) | — |
| `JournalLines` | Double-entry lines: `Debit`/`Credit` (exactly one non-zero per row, enforced by `CK_JournalLines_SingleSide`), plus a full multi-currency shadow (`CurrencyCode`, `OriginalAmount`, `RateToBase`, `CounterAmount`, `BaseCurrencyCode`, `CounterCurrencyCode`) | `JournalEntryId → JournalEntries.Id`, `AccountId → Accounts.Id` |

`AccountingMigration` seeds a starter chart of accounts (Cash `1000`,
Inventory `1100`, Used Car Cost `1150`, Accounts Payable `2000`, Supplier
Accounts `2100`, Owner Equity `3000`, Sales Revenue `4000`, COGS `5000`,
Operating Expenses `6000` + sub-accounts for used-car transportation/
part-out/shipping/customs/repairs `5210`–`5250`), then wires up
`AccountingPostingSettings` to point at them by default.

`AccountingCurrencyRateRepairMigration` (schema.sql only / also mirrored
as a C# migration name pattern in spirit — see `AccountingCurrencyRateRepairMigration.cs`)
is a one-time data-repair pass: it recomputes `JournalLines`/`Transactions`/
`TransactionItems` currency fields for rows that were written before the
counter-currency columns existed, using the current `AppConstants`
currency configuration as the source of truth. It only touches rows that
look like they were never properly backfilled (rate = 1 exactly, matching
symptomatic amount patterns) — it's conservative by design.

### 2.7 Messaging / communications / reporting

| Table | Purpose |
|---|---|
| `OutboundMessages` | Generic outbound/inbound message log (WhatsApp, email, SMS), tracks provider status | 
| `WhatsAppCampaigns` / `WhatsAppCampaignRecipients` | Bulk WhatsApp campaign definitions and per-recipient send status |
| `ReportBuilderTableLinks` | Metadata describing how tables relate for the ad-hoc report builder feature |
| `ReportBuilderSavedReports` / `ReportBuilderSavedReportRoles` | User-saved custom reports and role-based sharing |
| `ReportBuilderFavoriteReports` | Per-user favorited reports |
| `ReportBuilderBackgroundRuns` | Background/async execution state for long-running report queries |
| `ActivityLogs` | Generic audit trail: action, entity type/id, before/after JSON snapshots, user, IP |
| `ExcelImportMetadata` | Records column-mapping configuration used by the Excel bulk-import feature |

### 2.8 Access control / navigation

| Table | Purpose | Key FKs |
|---|---|---|
| `AppMenus` | Registry of app menu/screen keys (`pos_screen`, `invoice_create`, `stock_management_screen`, etc.) | — |
| `RoleMenuAccess` | Per-role, per-menu CRUD permission flags (`CanView`/`CanEdit`/`CanModify`/`CanDelete`) | `RoleId → Roles.Id` (cascade), `MenuId → AppMenus.Id` (cascade) |

### 2.9 Image / data enrichment pipeline

| Table | Purpose |
|---|---|
| `PartImageEnrichment` | One row per part's proposed replacement image, with confidence scoring and admin-approval workflow fields |
| `PartImageEnrichmentCandidates` | Multiple ranked image candidates considered per part before one is selected |
| `PartOemEnrichment` | Proposed OEM/manufacturer part number enrichment per part, with confidence scoring |
| `VehicleExpectedPartCandidates` | Candidate parts expected to exist on a given donor vehicle, sourced from vehicle-spec research |

These four tables are created exclusively by
`src/SpareParts.Api/Infrastructure/PartImageEnrichmentMigration.cs` (the
last migration in the `RunMigrations()` order). See section 5 for the
now-archived stale hand-copy of an earlier, incomplete version of this
migration.

### 2.10 Marketplace / "innovation features" tables (C#-migration-only)

A large number of tables exist purely via C# migrations and are **not**
represented in `database/schema.sql` at all (or only partially). These
back the customer-facing marketplace/web-app features layered on top of
the core inventory/accounting system: `PricingPackagesMigration`,
`UserVehiclesMigration`, `NeedBoardMigration`, `WatchlistMigration`,
`SellerVerificationMigration`, `MarketplaceFeaturesMigration`,
`GarageStockMigration`, `PartReservationsMigration`, `PartReelsMigration`,
`HalfCutsMigration`, `EscrowTransactionsMigration`, `ListingBoostsMigration`,
`ReferralsMigration`, `PartCompatibilityMigration`, `Phase2FeatureCodesMigration`,
`ConditionCertificatesMigration`, `PartPassportPhotosMigration`,
`ContentReportsMigration`, `InspectionRequestsMigration`,
`PartGenealogyMigration`, `MechanicProfilesMigration`,
`NewVsUsedPricingMigration`, `YardToursMigration`, `NegotiationsMigration`,
`InstantOffersMigration`, `InsuranceAddonsMigration`, `ApiKeysMigration`,
`Phase3FeatureCodesMigration`. Each file name is self-descriptive of the
feature area; consult the individual `.cs` file for exact table/column
shapes when working in that area — that level of table-by-table detail
is out of scope for this document (this round's task is a database-layer
audit, not a feature audit).

---

## 3. Migration systems in detail

### 3.1 `database/schema.sql` structure

The file has two halves:

1. **Lines ~1–695**: base `CREATE TABLE` statements for the original
   core tables (AppConstants through OutboundMessages — see section 2.1–2.5),
   each guarded by `IF OBJECT_ID(...) IS NULL`, plus seed data (default
   Admin role/user, default warehouse, default `AppConstants`, default
   `TransactionTypes`, default `AccountingAccountTypes`).

2. **Lines ~697–4500**, under the banner `-- MIGRATIONS (moved from API
   startup — all idempotent, safe to re-run)`: a sequence of blocks, each
   headed by a comment naming the migration it mirrors (e.g.
   `-- ── AccountingMigration ───`), copied from the corresponding C#
   migration's SQL body. These run in the same relative order as the C#
   `RunMigrations()` list, though **this file is not kept in perfect
   lockstep with new C# migrations** — several later migrations (from
   `PartRequestsMigration` onward through the marketplace/innovation
   features and `PartImageEnrichmentMigration`) are only partially
   represented or absent. The file ends with a `HotPathIndexMigration`
   block (index-only, no schema changes) explicitly marked
   `PENDING: apply to any live database only after explicit approval`.

Every statement in this file is safe to re-run — that's the whole design
principle. It uses `OBJECT_ID(...) IS NULL`, `COL_LENGTH(...) IS NULL`,
`NOT EXISTS (SELECT 1 FROM sys.indexes/sys.foreign_keys/...)` guards
throughout, and wraps risky `ALTER COLUMN` statements in `BEGIN TRY /
BEGIN CATCH` where a failure should not abort the whole script (e.g.
widening a column that might already be correct).

### 3.2 C# runtime migrations (`src/SpareParts.Api/Infrastructure/*Migration.cs`)

**This is the authoritative, always-current migration path.** Every file
is a `static class` with one `public static void EnsureApplied(ISqlConnectionFactory
factory)` method that opens a connection and executes one large
idempotent SQL string via Dapper. There is no migration-version table —
idempotency is achieved entirely through `IF OBJECT_ID/COL_LENGTH/EXISTS`
guards inside the SQL itself (same pattern as `schema.sql`).

They are invoked in a **fixed, explicit order** from `RunMigrations()` in
`src/SpareParts.Api/Hosting/SparePartsApiComposition.cs` (~line 544),
which itself is called once during application startup composition
(~line 530). The order matters — later migrations assume earlier ones
already ran (e.g. everything assumes `TenantsMigration` and
`TenantIdMigration` ran first; `AccountingMigration` assumes `Customers`/
`Suppliers` already exist).

Full list, in the exact order they run (70 files as of this audit):

```
TenantsMigration                       WhatsAppCampaignsMigration
TenantIdMigration                      ReportBuilderLinksMigration
InvoiceNumberingMigration              ReportBuilderAdvancedMigration
AccountingMigration                    ReorderRulesMigration
WebAppUserRoleMigration                PartSubstitutesMigration
UserRoleIdMigration                    PartExpiryMigration
MenuAccessMigration                    CustomerLoyaltyMigration
TransactionTypesMigration              CustomerPriceTierMigration
PartAveragePriceMigration              WarrantyClaimsMigration
PartUsedCarMigration                   SupplierPriceHistoryMigration
CurrencyRatesMigration                 ShipmentsMigration
AppConstantsMigration                  ActivityLogMigration
CarModelsMigration                     QuotesMigration
LocationsMigration                     CustomerCreditLimitMigration
UsedCarsMigration                      PricingPackagesMigration
UsedCarStateEventsMigration            UserVehiclesMigration
UsedCarPartPricingMigration            NeedBoardMigration
UsedCarTeardownMigration               WatchlistMigration
PartMarketingNotificationMigration     SellerVerificationMigration
PartMarkdownMigration                  MarketplaceFeaturesMigration
TransactionsPaymentReminderMigration   RepairOrdersMigration
UsedCarPurchasesMigration              GarageStockMigration
UsedCarWholesaleSalesMigration         PartReservationsMigration
TransactionsMigration                  PartReelsMigration
BarcodeScanningMigration               HalfCutsMigration
PartRequestsMigration                  EscrowTransactionsMigration
PartUsedCarStockMigration              ListingBoostsMigration
UsedCarImagesMigration                 ReferralsMigration
CommunicationsMigration                PartCompatibilityMigration
                                        Phase2FeatureCodesMigration
                                        ConditionCertificatesMigration
                                        PartPassportPhotosMigration
                                        ContentReportsMigration
                                        InspectionRequestsMigration
                                        PartGenealogyMigration
                                        MechanicProfilesMigration
                                        NewVsUsedPricingMigration
                                        YardToursMigration
                                        NegotiationsMigration
                                        InstantOffersMigration
                                        InsuranceAddonsMigration
                                        ApiKeysMigration
                                        Phase3FeatureCodesMigration
                                        SalesReturnTypeMigration
                                        PartImageEnrichmentMigration
```

(Reformatted into two columns purely for readability here; the real
order is the single sequential list in `SparePartsApiComposition.cs`
lines 546–619 — consult that file directly for the authoritative order.)

Editing these files is **out of scope for the database-developer role
this round** per the task boundaries (dev-backend-api owns C# migration
authorship/edits); they are documented here for completeness only.

### 3.3 How the two systems relate operationally

- Fresh **staging/manual bootstrap** (e.g. via `sqlcmd`/SSMS against an
  empty database, or `scripts/deploy-azure-free.ps1`): run
  `database/schema.sql` first to get the base tables + most historical
  migrations in one shot, then start the API — its `RunMigrations()`
  pass will apply `TenantsMigration`/`TenantIdMigration` (not present at
  all in `schema.sql`) and any newer migrations `schema.sql` doesn't yet
  mirror.
- **Local dev / every normal run**: the API's `RunMigrations()` always
  runs on startup regardless of how the database was created, so the
  C# migrations are the real safety net — even a database that skipped
  `schema.sql` entirely could, in principle, be bootstrapped purely by
  starting the API against an empty database, since nearly every
  C# migration also guards its own base `CREATE TABLE`. (`schema.sql` is
  still the faster/preferred path for seeding a large base schema in one
  batch.)
- **Never treat `schema.sql` alone as ground truth for "what the schema
  looks like."** Always cross-check against the C# migration that owns
  a given table if in doubt — grep `src/SpareParts.Api/Infrastructure/`
  for the table name.

---

## 4. Tenant / dbName isolation

Multi-tenancy is column-based (a `TenantId INT` column, not
schema-per-tenant or database-per-tenant). `TenantsMigration.cs` creates
`dbo.Tenants` and seeds tenant `Id = 1` (`Code = 'default'`).
`TenantIdMigration.cs` then adds `TenantId INT NULL` to each table in its
hardcoded list, backfills existing rows to `DefaultTenantId = 1`, and
(for tables marked `CreateIndex = true`) adds a non-clustered index
`IX_<Table>_TenantId`.

Tables that get a `TenantId` column **and** an index (`CreateIndex =
true`): `Users`, `Parts`, `Stock`, `StockMovements`, `Customers`,
`Suppliers`, `Warehouses`, `Locations`, `Brands`, `Categories`,
`UsedCars`, `Transactions`, `TransactionTypes`, `Accounts`,
`JournalEntries`, `UsedCarPurchases`, `UsedCarWholesaleSales`.

Tables that get a `TenantId` column but **no** dedicated index
(`CreateIndex = false`, typically child/detail tables expected to be
queried via their parent's already-indexed FK): `Location`, `CarBrands`,
`CarModels`, `usedcar_images`, `TransactionItems`, `AccountingPostingSettings`,
`JournalLines`, `UsedCarPurchaseLines`, `Quotes`, `QuoteItems`,
`PartRequests`, `PartRequestReservations`, `PartSubstitutes`,
`ReorderRules`, `OutboundMessages`, `WhatsAppCampaigns`,
`WhatsAppCampaignRecipients`, `ReportBuilderSavedReports`,
`ReportBuilderFavoriteReports`, `ReportBuilderTableLinks`, `Shipments`,
`LoyaltyTransactions`, `WarrantyClaims`, `SupplierPriceHistory`,
`ActivityLogs`, `CurrencyRates`, `AppConstants`, `ExcelImportMetadata`.

This column is **not present anywhere in `database/schema.sql`** — see
the header comment now at the top of that file. Any query written
against a fresh `schema.sql`-only database that assumes `TenantId`
exists will fail until the API's migrations run at least once. Dapper
repository code that filters by `TenantId` is dev-backend-api's/
dev-database's shared concern going forward — this round's audit did
not modify repository code (out of scope, C# repository files excluded
per task instructions).

---

## 5. Known naming inconsistencies (documentation only)

- **`usedcar_images`** is `snake_case` while every sibling table is
  `PascalCase` (`UsedCars`, `StockMovements`, etc.). A comment was added
  directly above its `CREATE TABLE` statement in `database/schema.sql`
  in this round explaining this is a deliberate deferral, not an
  oversight: renaming it would be a breaking change requiring
  coordinated updates to the Dapper repository code in
  `src/SpareParts.Infrastructure/` and to
  `src/SpareParts.Api/Infrastructure/UsedCarImagesMigration.cs`, which
  also references `dbo.usedcar_images` by exact name. No rename was
  performed this round.
- **`Location` vs `Locations`** (see section 2.1) — not flagged as an
  error, but worth calling out for onboarding: these are two genuinely
  different tables for two different concepts that happen to differ only
  by a plural "s". Read the FK target carefully when touching either.

---

## 6. `scripts/` folder inventory

| Script | Language | Purpose |
|---|---|---|
| `backup_part_images.py` | Python (pyodbc) | Exports current `Parts.ImageUrls` + `PartPassportPhotos` state to CSV files under `backups/`, intended to run before any image-enrichment work so changes are reversible. Read-only against the DB. |
| `check-app-spec-parity.js` | Node.js | Static analysis script — compares the app's declared feature/screen spec against implementation to produce `outputs/app-spec-parity-report.json`. Not database-related at runtime (no DB connection), included here because it lives in `scripts/`. |
| `configure-azure-oauth.ps1` | PowerShell | Deployment helper — configures Google/Facebook OAuth app settings for an already-deployed Azure API/Web app pair (writes `wwwroot/config.js` and Azure App Service settings). No direct SQL. |
| `delete-azure-free.ps1` | PowerShell | Deployment teardown — deletes the Azure resource group used by the "free tier" hosting setup (`rg-spareparts-free` by default). Destructive infra operation; not a DB script per se but can delete a hosted SQL Server if one was provisioned in that resource group. |
| `deploy-azure-free.ps1` | PowerShell | Main Azure free-tier deployment script. Provisions/updates App Service + SQL Server/Database, and (unless `-SkipDatabaseImport`) imports the schema/data into the target Azure SQL database. This is the primary script that would run `database/schema.sql` (or an export of it) against a real (non-local) database — a production/staging database operation requiring the same approval discipline as any other schema change. |
| `enrich_part_images.py` | Python | The current, sanctioned part-image enrichment pipeline. Explicitly documents "NEVER runs real DB updates unless `--dry-run false` is explicitly passed" — defaults to safe dry-run/report modes. This is the modern replacement for the archived one-off SQL scripts described in section 7. |
| `export-git-safe-sql-snapshot.ps1` | PowerShell | Generates the contents of `database/git-safe-snapshot/` — a redacted, insert-only SQL export of catalog/inventory tables (car models, used cars, parts, stock, stock movements, warehouses, reference data) safe to commit to git. Explicitly excludes identity/personal-contact/messaging/accounting/log/transaction data and redacts audit user-id columns and addresses to `NULL`. See `database/git-safe-snapshot/README.md` for the apply procedure (`sqlcmd ... -i apply.sql` against an already-schema'd empty/dev database). |
| `populate-part-images.ps1` | PowerShell | **Deprecated and self-blocking**: throws immediately unless called with `-AllowLive`, directing users to the newer `tools/SpareParts.ImageEnrichment` tool instead. Kept for reference/emergency use only. |
| `setup-staging-urls.ps1` | PowerShell | Post-deployment step that patches staging config files (API/Web URLs, OAuth client IDs) after `deploy-azure-free.ps1` has produced real Azure hostnames. No direct SQL. |
| `start-free-host.ps1` | PowerShell | Local/self-hosted convenience script — starts the API process and a `cloudflared` tunnel for exposing it publicly during free-tier hosting, with health-check polling and logging to `logs/`. No DB schema changes. |
| `stop-free-host.ps1` | PowerShell | Stops the `cloudflared` tunnel process started by `start-free-host.ps1`. No DB involvement. |

None of the scripts above perform destructive raw SQL against a live
database as their default/only mode — the two that touch data
(`deploy-azure-free.ps1`'s import step and `populate-part-images.ps1`)
either require an explicit approval flag or are themselves the
deployment path that already requires sign-off under CLAUDE.md's
"no production database commands without explicit approval" rule.

---

## 7. `database/archive/` — retired one-off SQL scripts

As of this round, six files that were dead/already-applied one-off SQL
scripts have been moved (via `git mv`, preserving history) from the
repository root and from `database/migrations/` into `database/archive/`:
`PartImageEnrichmentMigration.sql`, `run_this_in_ssms.sql`,
`create_enrichment_table.sql`, `minimal_update.sql`,
`apply_enriched_images.sql`, `oem_parts_insert.sql`. None of these are
referenced by any build, CI, deploy, or migration-runner code path. See
`database/archive/README.md` for a per-file explanation of what each one
did and why it's archived rather than deleted (nothing was deleted — all
six remain fully readable and restorable).

---

## 8. Practical guidance for new engineers

- **To find where a table is created or altered**: `grep` for the table
  name across both `database/schema.sql` and
  `src/SpareParts.Api/Infrastructure/*.cs`. Check the C# result first —
  it's more likely to be current.
- **To find where a table is queried/written from application code**:
  look in `src/SpareParts.Infrastructure/` (Dapper repositories) — this
  document does not catalog repository code, only schema.
- **Never assume a table has a `TenantId` column just because it's a
  "normal" table** — check section 4's list, or grep
  `TenantIdMigration.cs` directly.
- **All schema changes in both `schema.sql` and C# migrations must stay
  idempotent** (`IF OBJECT_ID/COL_LENGTH/EXISTS` guards) — this is a hard
  convention in this codebase, not a suggestion, since both run
  unconditionally on every relevant startup/bootstrap.
- **Money columns** are consistently `DECIMAL(19,4)` (or `DECIMAL(18,2)`
  in a few older C# migrations) with parallel base/counter-currency
  shadow columns (`BaseCurrencyCode`, `CounterCurrencyCode`, `RateToBase`,
  `CounterAmount`, etc.) on any table that can be posted in a
  non-default currency — this pattern repeats across `JournalLines`,
  `Transactions`, `TransactionItems`, `UsedCarPurchases`,
  `UsedCarPurchaseLines`, `UsedCarWholesaleSales`, and `UsedCars` itself.
- **Every table uses `SYSUTCDATETIME()`** for timestamp defaults — the
  application is UTC-only at the data layer.
