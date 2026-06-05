# SpareParts — Full Application Feature Audit Report

**Date:** 2026-06-05  
**Branch:** `app-feature-audit-report`  
**Auditor:** Claude Code (claude-sonnet-4-6)  
**Scope:** Full repository — backend API, web frontend, mobile app, WPF desktop, database schema, CI/CD  

---

## Table of Contents

1. [Technical Architecture](#1-technical-architecture)
2. [Database Entity Table](#2-database-entity-table)
3. [API Endpoint Table](#3-api-endpoint-table)
4. [Feature List by Module](#4-feature-list-by-module)
5. [Web Screen Registry](#5-web-screen-registry)
6. [Mobile Screen Registry](#6-mobile-screen-registry)
7. [WPF Desktop Features](#7-wpf-desktop-features)
8. [Route-to-Feature Map](#8-route-to-feature-map)
9. [Completeness Matrix](#9-completeness-matrix)
10. [User Flow Report](#10-user-flow-report)
11. [Risk and Bug Table](#11-risk-and-bug-table)
12. [Missing Feature Recommendations](#12-missing-feature-recommendations)

---

## 1. Technical Architecture

### 1.1 Technology Stack

| Layer | Technology | Version / Notes |
|---|---|---|
| Backend API | ASP.NET Core | .NET 8.0 |
| Data access | Dapper | Direct SQL, no EF Core ORM |
| Database | SQL Server | 2022; Testcontainers in CI |
| Real-time | SignalR | Hub path `/hubs/notifications` |
| AI integration | OpenAI API | GPT-4 for notes, visual search, business assistant |
| Auth | JWT Bearer | 12-hour expiry; Google + Facebook OAuth |
| Web frontend | React 18 | Vanilla JS (no bundler), `h()` hyperscript |
| Mobile | React Native / Expo | 16 screens |
| Desktop | WPF (.NET 8) | MVVM, Windows only |
| CI/CD | GitHub Actions | 4 workflows (see §1.7) |

### 1.2 Project Structure

```
SpareParts/
├── src/
│   ├── SpareParts.Api/                  ← ASP.NET Core 8 Web API
│   │   ├── Controllers/                 ← 42 controllers
│   │   ├── Hosting/                     ← DI composition, JWT settings
│   │   ├── Infrastructure/              ← 35 database migrations
│   │   ├── Middleware/                  ← Error handling, web-user restriction
│   │   └── Services/                    ← Auth service
│   ├── SpareParts.Domain/               ← Pure domain entities & DTOs
│   ├── SpareParts.Infrastructure/       ← Dapper repos, services (90+)
│   ├── SpareParts.Web.React/            ← React web app (static + ASP.NET host)
│   │   └── wwwroot/js/views/            ← 32 screen JS files
│   ├── SpareParts.Mobile.ReactNative/   ← Expo React Native (16 screens)
│   └── SpareParts.Desktop.Wpf/          ← WPF desktop (Windows only)
├── tests/
│   ├── SpareParts.ArchitectureTests/    ← Layer rules, security tests
│   ├── SpareParts.ManagementTests/      ← WPF ViewModel tests (Windows)
│   └── SpareParts.IntegrationTests/     ← SQL Server integration tests
├── database/
│   └── schema.sql                       ← Idempotent DDL for all tables
├── solutions/
│   ├── SpareParts.Backend.sln           ← Linux-safe backend solution
│   ├── SpareParts.Tests.sln             ← Test projects
│   └── SpareParts.Desktop.sln           ← WPF (Windows only)
└── .github/workflows/                   ← CI/CD pipelines
```

### 1.3 Backend Architecture

- **Layering:** Domain → Infrastructure → API (enforced by ArchitectureTests)
- **DI/Composition:** All services registered by `ServiceCapability` enum in `SparePartsApiComposition.cs`
- **Capabilities:** Sales, Purchases, Inventory, Accounting, Identity, Catalog, Reporting, Health
- **Base controller:** `SparePartsControllerBase` provides `CurrentUserId`, `CurrentRoleId`, pagination helpers
- **Migrations:** 35 idempotent static migrations (`IF OBJECT_ID IS NULL`), not auto-applied — require explicit `EnsureApplied()` calls or running DDL from `database/schema.sql`

### 1.4 Frontend Architecture

- React loaded via CDN (no build step / bundler)
- `screen-registry.js` registers all 32 web screens
- `h()` hyperscript replaces JSX
- `featureModules` config array drives module-workspace screens
- Auth via JWT stored in memory/localStorage; `auth.js` handles token refresh

### 1.5 Authentication & Authorization

| Aspect | Details |
|---|---|
| Scheme | JWT Bearer (default) + Google/Facebook OAuth |
| Token expiry | 12 hours |
| Roles | System (implicit), Admin (1), Manager (2), Cashier (3), WebAppUser (migrated ID) |
| Policies | `role-id:admin`, `role-id:admin-or-manager`, `role-id:web-app-user` |
| Rate limiting | Auth endpoint: 10 req/min per IP |
| Web user restriction | `WebAppUserRestrictionMiddleware` gates capabilities |
| Global auth | All controllers require auth except `AuthController` login endpoints |

**Evidence:** `src/SpareParts.Api/Hosting/SparePartsApiComposition.cs`, `src/SpareParts.Api/Services/AuthService.cs`, `src/SpareParts.Api/Middleware/WebAppUserRestrictionMiddleware.cs`

### 1.6 Error Handling

- `ApiExceptionMiddleware` catches typed exceptions and maps them to HTTP status codes:
  - `NotFoundException` → 404
  - `ValidationException` → 400
  - Unhandled → 500 (no stack trace exposed to client)

**Evidence:** `src/SpareParts.Api/Errors/ApiExceptionMiddleware.cs`

### 1.7 CI/CD Workflows

| Workflow | Trigger | What it does |
|---|---|---|
| `ci.yml` | PR + push to main/master | Build (warnings-as-errors), format check, ArchitectureTests, SQL Server integration tests |
| `backend-ci.yml` | Optimized backend CI | Build, ArchitectureTests, publish artifact on main/master |
| `wpf-build.yml` | Windows runner | WPF compile + ManagementTests |
| `deploy-staging.yml` | Manual / push | Full staging deploy with migration orchestration |

### 1.8 Configuration Structure

```json
{
  "ConnectionStrings": { "DefaultConnection": "..." },
  "Jwt": { "Secret", "Issuer", "Audience", "ExpiryHours": 12 },
  "Cors": { "AllowedOrigins": ["localhost:5078", "5076", "5173", "5081"] },
  "Accounting": { "CashAccountId", "SalesAccountId", "CogsAccountId", "InventoryAccountId" },
  "OpenAI": { "ApiKey", "Model", "BaseUrl", "TimeoutSeconds" },
  "Communications": { "Provider", "WebhookUrl", "WebhookSecret", "TimeoutSeconds" },
  "ExternalAuth": { "GoogleClientId", "FacebookAppId", "FacebookAppSecret" }
}
```

---

## 2. Database Entity Table

All tables are in the `dbo` schema on SQL Server 2022.

| Table | Purpose | Key Columns | Related Migration |
|---|---|---|---|
| `AppConstants` | Key-value configuration store | Key, Value | AppConstantsMigration |
| `Brands` | Part manufacturer/brand master | Id, Name, LogoUrl | — (schema.sql) |
| `Categories` | Part category hierarchy | Id, Name, ParentId | — (schema.sql) |
| `CarBrands` | Vehicle manufacturer master | Id, Name, LogoData | CarModelsMigration |
| `CarModels` | Vehicle model definitions | Id, CarBrandId, Name, Year | CarModelsMigration |
| `CurrencyRates` | Exchange rates snapshot | Id, FromCurrency, ToCurrency, Rate, Date | CurrencyRatesMigration |
| `Warehouses` | Physical warehouse master | Id, Name, IsMain | — (schema.sql) |
| `Locations` | Warehouse storage bins/aisles | Id, WarehouseId, Name, Code | LocationsMigration |
| `Roles` | User role definitions | Id, Name | — (schema.sql) |
| `Users` | User accounts | Id, Username, PasswordHash, RoleId | — (schema.sql) |
| `Customers` | Customer/buyer master | Id, Name, Phone, Email, AccountId, CreditLimit | — (schema.sql) |
| `Suppliers` | Supplier/vendor master | Id, Name, Phone, ContactPerson, AccountId | — (schema.sql) |
| `UsedCars` | Vehicle inventory for part-out | Id, Make, Model, Year, VIN, Status, WarehouseId | UsedCarsService migration |
| `usedcar_images` | Vehicle photos (binary) | Id, UsedCarId, ImageData, ContentType | UsedCarImagesMigration |
| `Parts` | Part master with multi-tier pricing | Id, Name, InternalCode, Barcode, CategoryId, BrandId, CostPrice, SalePrice, WholesalePrice, MinStockLevel | — (schema.sql) |
| `Stock` | Inventory by part/warehouse/location | Id, PartId, WarehouseId, LocationId, Quantity, ReservedQuantity | — (schema.sql) |
| `StockMovements` | Inventory audit trail | Id, PartId, WarehouseId, Quantity, MovementType, TransactionId, Timestamp | — (schema.sql) |
| `Accounts` | Chart of accounts (GL) | Id, Code, Name, AccountTypeId, Currency | AccountingMigration |
| `AccountingAccountTypes` | Account classification | TypeKey, Name, NormalBalance | AccountingMigration |
| `AccountingPostingSettings` | Transaction-to-GL account mappings | Id, TransactionTypeId, RoleKey, AccountId | AccountingMigration |
| `AccountingPostingRoles` | Role-based posting rules | RoleKey, Description | AccountingMigration |
| `JournalEntries` | Accounting journal header | Id, Date, Reference, TransactionId, CreatedByUserId | AccountingMigration |
| `JournalLines` | Debit/credit lines | Id, JournalEntryId, AccountId, Debit, Credit, Currency, ExchangeRate | AccountingMigration |
| `TransactionTypes` | Invoice/receipt type definitions | Id, Code, Name, Direction | TransactionTypesMigration |
| `Transactions` | Invoice/PO header | Id, TypeId, Number, Date, CustomerId, SupplierId, WarehouseId, TotalAmount, PaidAmount, Status | TransactionsMigration |
| `TransactionItems` | Invoice line items | Id, TransactionId, PartId, Quantity, UnitPrice, DiscountAmount, TaxRate, CostPrice | TransactionsMigration |
| `ExcelImportMetadata` | Column mapping for bulk imports | Id, TargetTable, ColumnMappings | — |
| `OutboundMessages` | WhatsApp/SMS queue & log | Id, RecipientPhone, Body, Provider, Status, SentAt | CommunicationsMigration |
| `Quotes` | Quote/estimate header | Id, QuoteNumber, QuoteDate, ExpiryDate, CustomerId, CustomerName, WarehouseId, Status, Notes | QuotesMigration |
| `QuoteItems` | Quote line items | Id, QuoteId, PartId, Description, Quantity, UnitPrice, DiscountAmount, SortOrder | QuotesMigration |

**Additional tables added by migrations (not in base schema.sql):**

| Table | Migration | Purpose |
|---|---|---|
| `PartRequests` | PartRequestsMigration | Customer part demand tracking |
| `PartSubstitutes` | PartSubstitutesMigration | Compatible part cross-references |
| `PartExpiry` | PartExpiryMigration | Expiration date tracking per part |
| `ReorderRules` | ReorderRulesMigration | Min stock / reorder point per part |
| `CustomerLoyalty` | CustomerLoyaltyMigration | Loyalty points ledger |
| `CustomerPriceTiers` | CustomerPriceTierMigration | Custom pricing per customer |
| `WarrantyClaims` | WarrantyClaimsMigration | Warranty & return tracking |
| `SupplierPriceHistory` | SupplierPriceHistoryMigration | Historical supplier quotes |
| `Shipments` | ShipmentsMigration | Inbound/outbound shipment tracking |
| `ActivityLog` | ActivityLogMigration | Full audit trail |
| `WhatsAppCampaigns` | WhatsAppCampaignsMigration | Bulk messaging campaigns |
| `ReportBuilderSavedReports` | ReportBuilderLinksMigration | Saved custom reports |
| `ReportBuilderLinks` | ReportBuilderLinksMigration | Custom join definitions |

---

## 3. API Endpoint Table

**Base URL:** `/api`  
**Auth:** All endpoints require JWT Bearer unless noted. Admin/Manager restrictions noted inline.

### Authentication (`/api/auth`)

| Method | Path | Auth Required | Description |
|---|---|---|---|
| POST | `/api/auth/login` | No | Username/password login, returns JWT |
| POST | `/api/auth/external-login` | No | Google/Facebook OAuth login |
| GET | `/api/auth/me` | Yes | Current user info |
| GET | `/api/auth/hashpassword` | Dev only | Password hash helper |

### Users (`/api/users`)

| Method | Path | Auth Required | Description |
|---|---|---|---|
| GET | `/api/users` | Admin | List all users |
| POST | `/api/users` | Admin | Create user |
| PUT | `/api/users/{id}` | Admin | Update user |
| DELETE | `/api/users/{id}` | Admin | Delete user |

### Roles (`/api/roles`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/roles` | List all roles |
| GET | `/api/roles/{id}` | Get role by ID |
| POST | `/api/roles` | Create role |
| PUT | `/api/roles/{id}` | Update role |
| DELETE | `/api/roles/{id}` | Delete role |
| GET | `/api/roles/{id}/menu-access` | Get role menu permissions |
| PUT | `/api/roles/{id}/menu-access` | Update role menu permissions |

### Parts / Inventory (`/api/parts`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/parts` | List parts (paginated, search/filter) |
| GET | `/api/parts/dead-stock` | Dead stock analysis |
| GET | `/api/parts/{id}/stock` | Stock levels by warehouse |
| POST | `/api/parts` | Create part |
| PUT | `/api/parts/{id}` | Update part |
| PUT | `/api/parts/{id}/usedcar` | Link part to used car |
| POST | `/api/parts/{id}/transfer` | Transfer stock between warehouses |
| DELETE | `/api/parts/{id}` | Delete part |
| POST | `/api/parts/ai/notes` | Generate AI description for part |

### Part Substitutes (`/api/parts/{partId}/substitutes`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/parts/{partId}/substitutes` | List compatible substitutes |
| POST | `/api/parts/{partId}/substitutes` | Add substitute |
| DELETE | `/api/parts/{partId}/substitutes/{substituteId}` | Remove substitute |

### Part Expiry (`/api/parts/expiry`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/parts/expiry/alerts` | Parts nearing expiry |
| PUT | `/api/parts/expiry/{partId}` | Set/update expiry date |

### Part Requests (`/api/partrequests`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/partrequests` | List part requests (filter by status) |
| POST | `/api/partrequests` | Create part request |
| POST | `/api/partrequests/{id}/reserve` | Reserve stock for request |
| POST | `/api/partrequests/{id}/release-reservation` | Release reservation |
| PUT | `/api/partrequests/{id}/status` | Update request status |
| DELETE | `/api/partrequests/{id}` | Delete request |

### Warehouses (`/api/warehouses`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/warehouses` | List all warehouses |
| POST | `/api/warehouses` | Create warehouse |
| PUT | `/api/warehouses/{id}` | Update warehouse |
| DELETE | `/api/warehouses/{id}` | Delete warehouse |

### Locations (`/api/locations`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/locations` | List locations (filter by warehouse) |
| POST | `/api/locations` | Create location |
| PUT | `/api/locations/{id}` | Update location |
| DELETE | `/api/locations/{id}` | Delete location |

### Reorder (`/api/reorder`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/reorder/rules` | List reorder rules |
| PUT | `/api/reorder/rules` | Create/update reorder rule |
| DELETE | `/api/reorder/rules/{partId}` | Remove reorder rule |
| GET | `/api/reorder/suggestions` | AI-driven reorder suggestions |

### Barcode / Visual Scan (`/api/scans`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/scans/resolve` | Resolve barcode to part |
| POST | `/api/scans/visual-search` | Image-based part lookup (OpenAI) |

### Sales (`/api/sales`)

| Method | Path | Description |
|---|---|---|
| POST | `/api/sales` | Create sales invoice |
| GET | `/api/sales` | Search/list invoices |
| GET | `/api/sales/{invoiceId}` | Get invoice detail (incl. CostPrice per line) |
| PUT | `/api/sales/{invoiceId}` | Update invoice |
| POST | `/api/sales/{invoiceId}/payments` | Record payment |

### Customers (`/api/customers`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/customers` | List customers (paginated) |
| POST | `/api/customers` | Create customer |
| PUT | `/api/customers/{id}` | Update customer |
| DELETE | `/api/customers/{id}` | Delete customer |
| GET | `/api/customers/aging` | Aging report (0/30/60/90+ day buckets) |

### Customer Pricing (`/api/customer-pricing`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/customer-pricing` | List customer price tiers |
| PUT | `/api/customer-pricing/{customerId}/tier` | Set customer price tier |
| GET | `/api/customer-pricing/resolve` | Resolve effective price for customer+part |

### Quotes / Estimates (`/api/quotes`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/quotes` | List quotes (filter by status/search) |
| GET | `/api/quotes/{id}` | Get quote detail with line items |
| POST | `/api/quotes` | Create quote |
| PUT | `/api/quotes/{id}/status` | Update quote status (Draft→Sent→Accepted→Declined) |
| POST | `/api/quotes/{id}/convert` | Convert accepted quote to sales invoice |
| DELETE | `/api/quotes/{id}` | Delete quote (Admin/Manager only) |

### Purchases (`/api/purchases`)

| Method | Path | Description |
|---|---|---|
| POST | `/api/purchases` | Create purchase order/invoice |
| GET | `/api/purchases` | Search/list purchase invoices |
| GET | `/api/purchases/{purchaseId}` | Get purchase detail |
| PUT | `/api/purchases/{purchaseId}` | Update purchase |
| GET | `/api/purchases/used-cars` | List used-car purchase records |
| GET | `/api/purchases/used-cars/{id}` | Get used-car purchase detail |
| POST | `/api/purchases/used-cars` | Create used-car purchase |
| POST | `/api/purchases/used-cars/{id}/post` | Post used-car purchase to accounting |
| DELETE | `/api/purchases/used-cars/{id}` | Delete used-car purchase |

### Suppliers (`/api/suppliers`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/suppliers` | List suppliers (paginated) |
| POST | `/api/suppliers` | Create supplier |
| PUT | `/api/suppliers/{id}` | Update supplier |
| DELETE | `/api/suppliers/{id}` | Delete supplier |

### Supplier Price History (`/api/supplier-price-history`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/supplier-price-history/parts/{partId}` | Price history for a part |
| GET | `/api/supplier-price-history/parts/{partId}/comparison` | Supplier price comparison |
| POST | `/api/supplier-price-history` | Record new supplier quote |

### Used Cars (`/api/usedcars`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/usedcars` | List used cars |
| GET | `/api/usedcars/wholesale-sales` | List wholesale sales |
| GET | `/api/usedcars/{id}/images` | Get vehicle images |
| POST | `/api/usedcars/{id}/wholesale-sales` | Record wholesale sale |
| POST | `/api/usedcars/{id}/images` | Upload vehicle image |
| DELETE | `/api/usedcars/images/{imageId}` | Delete image |
| POST | `/api/usedcars` | Create used car record |
| PUT | `/api/usedcars/{id}` | Update used car |
| DELETE | `/api/usedcars/{id}` | Delete used car |

### Loyalty (`/api/loyalty`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/loyalty/customers/{customerId}` | Customer loyalty summary |
| GET | `/api/loyalty/customers/top` | Top loyalty customers |
| GET | `/api/loyalty/customers/{customerId}/transactions` | Loyalty point history |
| POST | `/api/loyalty/points` | Award loyalty points |
| POST | `/api/loyalty/redeem` | Redeem loyalty points |

### Warranty (`/api/warranty`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/warranty` | List warranty claims |
| GET | `/api/warranty/{id}` | Get claim detail |
| POST | `/api/warranty` | File warranty claim |
| PUT | `/api/warranty/{id}/resolve` | Resolve/close claim |

### Shipments (`/api/shipments`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/shipments` | List shipments |
| GET | `/api/shipments/{id}` | Get shipment detail |
| POST | `/api/shipments` | Create shipment |
| PUT | `/api/shipments/{id}/status` | Update shipment status |

### Accounting (`/api/accounting`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/accounting/accounts` | List GL accounts |
| GET | `/api/accounting/account-types` | List account types |
| POST | `/api/accounting/account-types` | Create account type |
| PUT | `/api/accounting/account-types/{typeKey}` | Update account type |
| DELETE | `/api/accounting/account-types/{typeKey}` | Delete account type |
| GET | `/api/accounting/posting-roles` | List posting roles |
| POST | `/api/accounting/posting-roles` | Create posting role |
| PUT | `/api/accounting/posting-roles/{roleKey}` | Update posting role |
| DELETE | `/api/accounting/posting-roles/{roleKey}` | Delete posting role |
| GET | `/api/accounting/posting-settings` | Get GL posting settings |
| PUT | `/api/accounting/posting-settings` | Update posting settings |
| GET | `/api/accounting/journal-entries` | List journal entries |
| GET | `/api/accounting/journal-entries/{id}` | Get journal entry detail |
| POST | `/api/accounting/journal-entries/manual` | Create manual journal entry |
| GET | `/api/accounting/ledger` | General ledger |
| GET | `/api/accounting/trial-balance` | Trial balance |
| GET | `/api/accounting/statement-parties` | List parties for SOA |
| GET | `/api/accounting/statement-of-account` | Statement of account |
| GET | `/api/accounting/statement-of-account/party` | SOA for specific party |

### Accounts / Chart of Accounts (`/api/accounts`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/accounts` | List all accounts |
| POST | `/api/accounts` | Create account |
| PUT | `/api/accounts/{id}` | Update account |
| DELETE | `/api/accounts/{id}` | Delete account |

### Brands (`/api/brands`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/brands` | List brands (paginated) |
| POST | `/api/brands` | Create brand |
| PUT | `/api/brands/{id}` | Update brand |
| DELETE | `/api/brands/{id}` | Delete brand |

### Categories (`/api/categories`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/categories` | List categories |
| POST | `/api/categories` | Create category |

### Car Brands (`/api/carbrands`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/carbrands` | List car brands |
| GET | `/api/carbrands/{id}/logo` | Get brand logo image |
| POST | `/api/carbrands/{id}/logo` | Upload brand logo |
| POST | `/api/carbrands` | Create car brand |
| PUT | `/api/carbrands/{id}` | Update car brand |
| DELETE | `/api/carbrands/{id}` | Delete car brand |

### Car Models (`/api/carmodels`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/carmodels` | List car models |
| GET | `/api/carmodels/{id}/image` | Get model image |
| POST | `/api/carmodels/{id}/image` | Upload model image |
| POST | `/api/carmodels` | Create car model |
| PUT | `/api/carmodels/{id}` | Update car model |
| DELETE | `/api/carmodels/{id}` | Delete car model |

### Currencies (`/api/currencies`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/currencies` | List currency rates |

### App Constants (`/api/appconstants`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/appconstants` | List all constants |
| PUT | `/api/appconstants/{key}` | Update constant value |

### Transaction Types (`/api/transactiontypes`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/transactiontypes` | List transaction types |
| POST | `/api/transactiontypes` | Create type |
| PUT | `/api/transactiontypes/{id}` | Update type |
| DELETE | `/api/transactiontypes/{id}` | Delete type |

### Excel Import (`/api/excelimport`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/excelimport/targets` | List importable entity targets |
| POST | `/api/excelimport/rows` | Import rows from Excel |

### Communications / WhatsApp (`/api/communications`)

| Method | Path | Description |
|---|---|---|
| POST | `/api/communications/send` | Send WhatsApp/SMS message |
| GET | `/api/communications/recent` | Recent outbound messages |
| GET | `/api/communications/conversations` | Conversation list |
| GET | `/api/communications/messages` | Messages in a conversation |
| GET | `/api/communications/campaign-assets` | Campaign template assets |
| POST | `/api/communications/campaign-preview` | Preview campaign message |
| POST | `/api/communications/campaigns/send` | Send bulk campaign |
| GET | `/api/communications/campaigns/recent` | Recent campaigns |
| POST | `/api/communications/inbound` | Incoming webhook (WhatsApp provider) |

### Owner Cockpit (`/api/owner-cockpit`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/owner-cockpit` | Executive dashboard (P&L, trends, alerts) |

### Business Assistant (`/api/business-assistant`)

| Method | Path | Description |
|---|---|---|
| POST | `/api/business-assistant/ask` | Natural language business query (OpenAI) |
| POST | `/api/business-assistant/actions/run` | Execute AI-suggested action |

### Growth Intelligence (`/api/growth`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/growth/briefing` | Daily growth briefing |
| POST | `/api/growth/auction-simulator` | Simulate pricing scenarios |
| POST | `/api/growth/voice-quote` | Voice-input quote generation |

### Report Builder (`/api/reportbuilder`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/reportbuilder/tables` | Available tables for reports |
| GET | `/api/reportbuilder/columns` | Columns for a table |
| GET | `/api/reportbuilder/links` | Custom join definitions |
| GET | `/api/reportbuilder/join-graph` | Full schema join graph |
| GET | `/api/reportbuilder/schema` | Full DB schema metadata |
| GET | `/api/reportbuilder/security-options` | Security/role filtering options |
| GET | `/api/reportbuilder/saved-reports` | List saved reports |
| GET | `/api/reportbuilder/saved-reports/{id}` | Get saved report |
| POST | `/api/reportbuilder/saved-reports` | Save report |
| DELETE | `/api/reportbuilder/saved-reports/{id}` | Delete saved report |
| POST | `/api/reportbuilder/saved-reports/{id}/favorite` | Toggle favorite |
| GET | `/api/reportbuilder/background-runs` | List background runs |
| POST | `/api/reportbuilder/background-runs` | Start background report run |
| GET | `/api/reportbuilder/background-runs/{id}/result` | Get run result |
| POST | `/api/reportbuilder/links` | Create custom join link |
| DELETE | `/api/reportbuilder/links/{id}` | Delete custom join |
| POST | `/api/reportbuilder/run` | Execute report query |

### Search (`/api/search`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/search` | Universal smart search (parts, customers, etc.) |

### Activity Log (`/api/activity-log`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/activity-log` | List audit log entries |
| POST | `/api/activity-log` | Write audit log entry |

### Web Catalog (`/api/web-catalog`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/web-catalog/parts` | Public-facing part catalog |
| POST | `/api/web-catalog/checkout` | Web storefront checkout |
| POST | `/api/web-catalog/part-requests` | Submit part request from storefront |
| POST | `/api/web-catalog/visual-search` | Image-based part search (public) |

### Health (`/api/health`)

| Method | Path | Description |
|---|---|---|
| GET | `/api/health` | Service health & migration status |

---

## 4. Feature List by Module

### Module 1: Authentication & User Management

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Username/Password Login | Validates credentials, returns JWT | All users | ✅ Complete |
| Google/Facebook OAuth | External OAuth login flow | Web users | ✅ Complete |
| JWT Auth | Stateless bearer token auth on all endpoints | System | ✅ Complete |
| User CRUD | Admin creates/manages user accounts | Admin | ✅ Complete |
| Role Management | Define roles & assign menu permissions | Admin | ✅ Complete |
| Menu Access Control | Per-role screen visibility | Admin | ✅ Complete |
| Password Hashing | Bcrypt hashing utility (dev endpoint) | Dev | ✅ Complete |
| Web User Restriction | Limits WebAppUser to catalog-only access | System | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/AuthController.cs`, `UsersController.cs`, `RolesController.cs`, `src/SpareParts.Api/Services/AuthService.cs`

---

### Module 2: Dashboard & Executive Cockpit

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Owner Cockpit | Daily P&L, profit heatmap, unpaid transactions, inventory snapshot | Owner/Manager | ✅ Complete |
| Dashboard Quick Cards | Total parts, available, reserved, low stock, active requests, quote-ready, source gaps, margin watch | All | ✅ Complete |
| Growth Intelligence ("Money Finder") | Revenue/profit analysis, growth briefing, pricing scenarios, voice-quote | Owner | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/OwnerCockpitController.cs`, `GrowthController.cs`, `src/SpareParts.Web.React/wwwroot/js/views/dashboard-view.js`, `growth-lab-view.js`

---

### Module 3: Inventory / Parts

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Part Master | Create/edit/delete parts with multi-tier pricing (cost, sale, wholesale, fast-sale, min, recommended) | Staff | ✅ Complete |
| Stock Levels | View stock by warehouse/location | Staff | ✅ Complete |
| Stock Transfer | Move stock between warehouses | Staff | ✅ Complete |
| Dead Stock Detection | Identify slow-moving / zero-movement inventory | Manager | ✅ Complete |
| Part Expiry Alerts | Track expiration dates; alert on approaching expiry | Staff | ✅ Complete |
| Part Substitutes / Compatibility | Cross-reference compatible substitute parts | Staff | ✅ Complete |
| Reorder Center | Set min-stock rules; view AI reorder suggestions | Manager | ✅ Complete |
| Barcode Scanning | Resolve barcode to part record | Staff | ✅ Complete |
| Visual Part Search | Upload photo to identify part (OpenAI) | Staff | ✅ Complete |
| AI Part Notes | Auto-generate part description via OpenAI | Staff | ✅ Complete |
| Part Passport | Full part lifecycle view (stock, transactions, substitutes, images) | Staff | ✅ Complete |
| Excel Bulk Import | Import parts/inventory data from Excel file | Admin | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/PartsController.cs`, `PartSubstitutesController.cs`, `PartExpiryController.cs`, `ReorderController.cs`, `ScansController.cs`, `ExcelImportController.cs`, `src/SpareParts.Web.React/wwwroot/js/views/inventory-view.js`, `dead-stock-view.js`, `expiry-alerts-view.js`, `reorder-view.js`

---

### Module 4: Sales / POS

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Create Sales Invoice | Multi-line invoice with customer, warehouse, payment method, paid amount | Cashier/Staff | ✅ Complete |
| Search / List Invoices | Paginated invoice list with search | Staff | ✅ Complete |
| Invoice Detail | View full invoice with line items + cost price | Staff | ✅ Complete |
| Profit Per Line | Each invoice line shows cost price, line profit, margin % | Manager/Owner | ✅ Complete |
| Invoice Profit Summary | Total revenue, total cost, overall margin per invoice | Manager/Owner | ✅ Complete |
| Payment Recording | Record payment against invoice (partial/full) | Cashier | ✅ Complete |
| Invoice Update | Edit invoice details | Staff | ✅ Complete |
| WhatsApp Invoice Send | Send invoice to customer via WhatsApp | Staff | ✅ Complete |
| WhatsApp Payment Reminder | Send payment reminder via WhatsApp | Staff | ✅ Complete |
| Multi-warehouse Sales | Select source warehouse per invoice | Staff | ✅ Complete |
| Walk-in Customer | Invoice without requiring registered customer | Cashier | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/SalesController.cs`, `src/SpareParts.Infrastructure/Services/SalesService.cs`, `src/SpareParts.Web.React/wwwroot/js/views/invoices-view.js`

---

### Module 5: Quotes / Estimates

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Create Quote | Quote with customer, warehouse, expiry, line items | Staff | ✅ Complete |
| Quote Lifecycle | Status workflow: Draft → Sent → Accepted → Declined → Converted | Staff | ✅ Complete |
| Quote Detail | View quote with all lines and total | Staff | ✅ Complete |
| Convert to Invoice | One-click conversion of Accepted quote to sales invoice | Staff | ✅ Complete |
| Quote Search | Filter quotes by status and search term | Staff | ✅ Complete |
| Delete Quote | Remove quote (Admin/Manager only) | Manager | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/QuotesController.cs`, `src/SpareParts.Infrastructure/Services/QuotesService.cs`, `src/SpareParts.Domain/Sales/Quote.cs`, `QuoteDto.cs`, `src/SpareParts.Web.React/wwwroot/js/views/quotes-view.js`

---

### Module 6: Purchases

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Create Purchase Invoice | Multi-line purchase from supplier | Purchasing | ✅ Complete |
| Search / List Purchases | Paginated purchase list with search | Staff | ✅ Complete |
| Purchase Detail | View purchase with line items | Staff | ✅ Complete |
| Purchase Update | Edit purchase record | Staff | ✅ Complete |
| Used Car Purchases | Record vehicle acquisition for part-out | Purchasing | ✅ Complete |
| Post Used Car Purchase | Post to accounting (GL journal entries) | Manager | ✅ Complete |
| Supplier Price History | Record and compare supplier quotes per part | Purchasing | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/PurchasesController.cs`, `SupplierPriceHistoryController.cs`, `src/SpareParts.Infrastructure/Services/PurchaseService.cs`

---

### Module 7: Customers

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Customer Master | CRUD for customer records (name, phone, email, credit limit) | Staff | ✅ Complete |
| Customer Aging Report | Receivables bucketed 0/30/60/90+ days overdue | Manager/Owner | ✅ Complete |
| Customer Price Tiers | Assign custom pricing tier per customer | Manager | ✅ Complete |
| Effective Price Resolution | Resolve actual price for customer+part combination | System | ✅ Complete |
| Loyalty Program | Points accrual, redemption, top-customer leaderboard | Manager/Staff | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/CustomersController.cs`, `CustomerPricingController.cs`, `LoyaltyController.cs`, `src/SpareParts.Infrastructure/Services/CustomersService.cs`, `src/SpareParts.Web.React/wwwroot/js/views/customer-aging-view.js`

---

### Module 8: Suppliers

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Supplier Master | CRUD for supplier records | Purchasing | ✅ Complete |
| Supplier Price History | Track and compare prices quoted per part per supplier | Purchasing | ✅ Complete |
| Supplier Price Comparison | Side-by-side price comparison across suppliers for a part | Purchasing | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/SuppliersController.cs`, `SupplierPriceHistoryController.cs`

---

### Module 9: Used Cars / Donor Vehicles

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Used Car Inventory | Add/edit/remove used cars with make, model, year, VIN, status | Staff | ✅ Complete |
| Vehicle Image Upload | Upload and display multiple photos per vehicle | Staff | ✅ Complete |
| Part-Out Workflow | Link parts to dismantled vehicle; auto-pricing allocation | Staff | ✅ Complete |
| Wholesale Sales | Record used car sold wholesale | Sales | ✅ Complete |
| Repair Prep Board | Track repair readiness of vehicle-sourced parts | Mechanics | ✅ Complete |
| Used Car Purchases | Record vehicle acquisition transactions | Purchasing | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/UsedCarsController.cs`, `PurchasesController.cs` (used-cars routes), `src/SpareParts.Web.React/wwwroot/js/views/used-cars-view.js`, `repair-prep-board-view.js`

---

### Module 10: Accounting / Finance

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Chart of Accounts | GL account hierarchy with type classification | Accountant | ✅ Complete |
| Double-Entry Journal | Auto-generated & manual journal entries | Accountant | ✅ Complete |
| Multi-Currency Ledger | Journal lines with currency + exchange rate | Accountant | ✅ Complete |
| Posting Settings | Map transaction types to GL accounts | Admin | ✅ Complete |
| Posting Roles | Role-based GL posting rules | Admin | ✅ Complete |
| General Ledger | Full ledger view with date filtering | Accountant | ✅ Complete |
| Trial Balance | Debit/credit balance summary | Accountant | ✅ Complete |
| Statement of Account | Per-party A/R or A/P statement | Accountant | ✅ Complete |
| Manual Journal Entry | Post manual adjustments | Accountant | ✅ Complete |
| Transaction Types | Define custom invoice/receipt types | Admin | ✅ Complete |
| Sale Auto-Posting | Automatic GL entries on invoice creation | System | ✅ Complete |
| Purchase Auto-Posting | Automatic GL entries on purchase creation | System | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/AccountingController.cs`, `AccountsController.cs`, `TransactionTypesController.cs`, `src/SpareParts.Infrastructure/Services/AccountingService.cs`, `src/SpareParts.Web.React/wwwroot/js/views/accounting-view.js`

---

### Module 11: WhatsApp / Communications

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Send WhatsApp Message | Send message to customer phone number | Staff | ✅ Complete |
| Conversation Inbox | View conversation threads | Staff | ✅ Complete |
| Inbound Webhook | Receive incoming WhatsApp messages | System | ✅ Complete |
| Campaign Messaging | Bulk WhatsApp campaigns with templates | Manager | ✅ Complete |
| Campaign Preview | Preview message before sending campaign | Manager | ✅ Complete |
| Invoice/Reminder Send | WhatsApp invoice delivery + payment reminder | Cashier | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/CommunicationsController.cs`, `src/SpareParts.Infrastructure/Services/CommunicationsService.cs`, `src/SpareParts.Web.React/wwwroot/js/views/whatsapp-view.js`

---

### Module 12: AI / Business Intelligence

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Business Assistant | Natural-language queries about business data (OpenAI) | Owner/Manager | ✅ Complete |
| AI Part Notes | Auto-generate part description from name/code | Staff | ✅ Complete |
| Visual Part Search | Upload part image → identify part via OpenAI | Staff | ✅ Complete |
| Growth Briefing | Daily AI briefing on revenue, profit, opportunities | Owner | ✅ Complete |
| Auction Simulator | Simulate pricing scenarios for parts | Owner | ✅ Complete |
| Voice Quote | Voice-input quote generation | Staff | ✅ Complete |
| Reorder Suggestions | AI-driven reorder recommendations | Manager | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/BusinessAssistantController.cs`, `GrowthController.cs`, `PartsController.cs` (`/ai/notes`), `ScansController.cs` (`/visual-search`), `ReorderController.cs`

---

### Module 13: Reporting

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Custom Report Builder | Build SQL reports from schema with drag-and-drop joins | Manager/Admin | ✅ Complete |
| Saved Reports | Save, favorite, and re-run custom reports | Manager | ✅ Complete |
| Background Report Runs | Async report execution with result polling | Manager | ✅ Complete |
| Schema Explorer | Browse DB tables, columns, joins for report building | Admin | ✅ Complete |
| Dead Stock Report | Identify slow-moving inventory | Manager | ✅ Complete |
| Customer Aging Report | Receivables aging by 30/60/90+ day buckets | Manager | ✅ Complete |
| Profit Per Invoice | Per-line profit + total margin on invoices | Manager/Owner | ✅ Complete |
| Owner Cockpit Dashboard | Executive KPIs and daily P&L | Owner | ✅ Complete |
| Trial Balance | Accounting debit/credit summary | Accountant | ✅ Complete |
| Statement of Account | Per-customer or per-supplier balance statement | Accountant | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/ReportBuilderController.cs`, `OwnerCockpitController.cs`, `src/SpareParts.Infrastructure/Services/ReportBuilderService.cs`

---

### Module 14: Contacts / Contacts View

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Contacts Screen | Unified view of customers and suppliers | Staff | ✅ Complete |
| Smart Search | Cross-entity search across parts, customers, invoices | Staff | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/SearchController.cs`, `src/SpareParts.Web.React/wwwroot/js/views/contacts-view.js`

---

### Module 15: Warehouse Management

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Warehouse CRUD | Create/manage multiple warehouses | Admin | ✅ Complete |
| Location/Bin CRUD | Create storage locations/aisles within warehouses | Admin | ✅ Complete |
| Stock by Location | View stock at specific bin/location | Staff | ✅ Complete |
| Stock Arrival Theater | Guided stock receiving workflow | Staff | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/WarehousesController.cs`, `LocationsController.cs`, `src/SpareParts.Web.React/wwwroot/js/views/stock-arrival-theater-view.js`

---

### Module 16: Part Requests

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Create Part Request | Customer or staff submits request for unavailable part | Staff | ✅ Complete |
| Request Status Workflow | Open → Contacted → Reserved → Fulfilled → Cancelled | Staff | ✅ Complete |
| Reserve Stock | Lock stock for a pending request | Staff | ✅ Complete |
| Release Reservation | Cancel stock reservation | Staff | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/PartRequestsController.cs`, `src/SpareParts.Web.React/wwwroot/js/views/part-requests-view.js`

---

### Module 17: Warranty & Returns

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| File Warranty Claim | Log warranty claim against a sold part | Staff | ✅ Complete |
| Claim Detail | View claim status and notes | Staff | ✅ Complete |
| Resolve Claim | Close warranty claim with resolution | Manager | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/WarrantyController.cs`, `src/SpareParts.Web.React/wwwroot/js/views/warranty-view.js`

---

### Module 18: Shipments

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Create Shipment | Log inbound/outbound shipment | Staff | ✅ Complete |
| Shipment Detail | View shipment status and items | Staff | ✅ Complete |
| Update Status | Move shipment through status stages | Staff | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/ShipmentsController.cs`, `src/SpareParts.Web.React/wwwroot/js/views/shipments-view.js`

---

### Module 19: Settings & Configuration

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| App Constants | Runtime config key-value store | Admin | ✅ Complete |
| Currency Rates | View/update FX rates | Admin | ✅ Complete |
| Transaction Types | Define invoice/receipt types | Admin | ✅ Complete |
| Settings Screen | In-app settings UI | Admin | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/AppConstantsController.cs`, `CurrenciesController.cs`, `TransactionTypesController.cs`

---

### Module 20: Master Data / Catalog

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Brands | Part manufacturer master | Admin | ✅ Complete |
| Categories | Part category hierarchy | Admin | ✅ Complete |
| Car Brands | Vehicle manufacturer master with logo | Admin | ✅ Complete |
| Car Models | Vehicle model definitions with image | Admin | ✅ Complete |
| Management Workspace | Admin hub for master data management | Admin/Manager | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/BrandsController.cs`, `CarBrandsController.cs`, `CarModelsController.cs`, `CategoriesController.cs`

---

### Module 21: Activity Log / Audit

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Activity Log | Append-only audit trail for user actions | Admin | ✅ Complete |
| Log Viewer | Browse audit entries with filters | Admin | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/ActivityLogController.cs`, `src/SpareParts.Web.React/wwwroot/js/views/activity-log-view.js`

---

### Module 22: Web Catalog / Storefront

| Feature | What It Does | Who Uses It | Status |
|---|---|---|---|
| Public Part Catalog | Web-accessible part search (no login) | Customers | ✅ Complete |
| Web Checkout | Customer places order from storefront | Customers | ✅ Complete |
| Web Part Requests | Submit request for unlisted part from storefront | Customers | ✅ Complete |
| Visual Search (public) | Image-based part search from storefront | Customers | ✅ Complete |

**Evidence:** `src/SpareParts.Api/Controllers/WebCatalogController.cs`, `src/SpareParts.Infrastructure/Services/WebCatalogService.cs`

---

## 5. Web Screen Registry

All 32 registered screens in `src/SpareParts.Web.React/wwwroot/js/views/screen-registry.js`:

| Key | Label | Component File | Module |
|---|---|---|---|
| `dashboard` | Dashboard | `dashboard-view.js` | Dashboard |
| `invoices` | POS / Sales | `invoices-view.js` | Sales |
| `inventory` | Parts | `inventory-view.js` | Inventory |
| `part-passport` | Part Passport | `part-passport-workspace-view.js` | Inventory |
| `compatibility` | Compatibility | `part-compatibility-view.js` | Inventory |
| `part-requests` | Part Requests | `part-requests-view.js` | Part Requests |
| `contacts` | Contacts | `contacts-view.js` | Contacts |
| `management` | Management | `management-view.js` | Admin |
| `settings` | Settings | `settings-view.js` | Settings |
| `purchase-parts` | Part Purchases | Module workspace | Purchases |
| `used-car-purchases` | Used Car Purchases | Module workspace | Used Cars |
| `used-car-wholesale` | Used Car Wholesale | Module workspace | Used Cars |
| `stock-arrival` | Stock Arrival | `stock-arrival-theater-view.js` | Warehouse |
| `used-cars` | Used Cars | `used-cars-view.js` | Used Cars |
| `repair-prep` | Repair / Prep | `repair-prep-board-view.js` | Used Cars |
| `stock` | Stock | Module workspace | Inventory |
| `dead-stock` | Dead Stock | `dead-stock-view.js` | Reporting |
| `growth-lab` | Money Finder | `growth-lab-view.js` | AI / BI |
| `accounting` | Accounting | `accounting-view.js` | Accounting |
| `manual-journal` | Manual Journal | Module workspace | Accounting |
| `report-builder` | Report Builder | Module workspace | Reporting |
| `whatsapp` | WhatsApp | `whatsapp-view.js` | Communications |
| `business-assistant` | AI Assistant | Module workspace | AI / BI |
| `ar` | AR Search | `barcode-mode-view.js` | Inventory |
| `reorder` | Reorder Center | `reorder-view.js` | Inventory |
| `expiry-alerts` | Expiry Alerts | `expiry-alerts-view.js` | Inventory |
| `loyalty` | Loyalty | `loyalty-view.js` | Customers |
| `warranty` | Warranty & Returns | `warranty-view.js` | Warranty |
| `shipments` | Shipments | `shipments-view.js` | Shipments |
| `activity-log` | Activity Log | `activity-log-view.js` | Audit |
| `quotes` | Quotes / Estimates | `quotes-view.js` | Sales |
| `customer-aging` | Customer Aging | `customer-aging-view.js` | Customers |

---

## 6. Mobile Screen Registry

16 screens in `src/SpareParts.Mobile.ReactNative/src/screens/`:

| Screen File | Feature Covered |
|---|---|
| `login-screen.js` | Authentication |
| `dashboard-screen.js` | Dashboard / KPIs |
| `invoices-screen.js` | POS / Sales |
| `parts-screen.js` | Inventory / Parts |
| `contacts-screen.js` | Customers & Suppliers |
| `accounting-screen.js` | Accounting |
| `whatsapp-screen.js` | WhatsApp communications |
| `used-cars-screen.js` | Used car inventory |
| `dead-stock-screen.js` | Dead stock analysis |
| `repair-prep-screen.js` | Repair prep board |
| `part-compatibility-screen.js` | Part substitutes |
| `management-screen.js` | Admin management |
| `settings-screen.js` | App settings |
| `mechanic-mode-screen.js` | Mechanic-specific workflow |
| `customer-storefront-screen.js` | Customer-facing catalog |
| `module-screen.js` | Generic module wrapper |

**Note:** Mobile app appears to cover core workflows; specific feature parity vs. web not verified by code review of each screen's internals.

---

## 7. WPF Desktop Features

**Windows** in `src/SpareParts.Desktop.Wpf/Windows/`:

| Window | Purpose |
|---|---|
| `LoginWindow.xaml` | Authentication UI |
| `MainWindow.xaml` | Primary application shell |
| `ManagementWindow.xaml` | Admin management panel |

**Services** (API clients for WPF):
- `WarehousesApiClient`, `PartsApiClient`, `CustomersApiClient`, and others
- MVVM with ViewModels; data fetched via API (same backend)
- Custom theming system with `Themes/` and `Converters/`

**Note:** WPF is Windows-only. The ManagementTests suite validates WPF ViewModel logic and runs in `wpf-build.yml` on a Windows runner.

---

## 8. Route-to-Feature Map

| Route Prefix | Feature Module | Key Screen |
|---|---|---|
| `/api/auth` | Authentication | Login |
| `/api/users` | User Management | Management |
| `/api/roles` | Role Management | Management |
| `/api/parts` | Inventory | Parts, Part Passport |
| `/api/parts/{id}/substitutes` | Part Compatibility | Compatibility |
| `/api/parts/expiry` | Expiry Alerts | Expiry Alerts |
| `/api/partrequests` | Part Requests | Part Requests |
| `/api/warehouses` | Warehouse Management | Management |
| `/api/locations` | Bin/Location Management | Management |
| `/api/reorder` | Reorder Center | Reorder Center |
| `/api/scans` | Barcode / Visual Search | AR Search |
| `/api/sales` | Sales / POS | POS / Sales |
| `/api/customers` | Customers | Contacts |
| `/api/customer-pricing` | Customer Pricing | Management |
| `/api/quotes` | Quotes / Estimates | Quotes / Estimates |
| `/api/purchases` | Purchases | Part Purchases |
| `/api/suppliers` | Suppliers | Contacts |
| `/api/supplier-price-history` | Supplier Pricing | Management |
| `/api/usedcars` | Used Cars | Used Cars |
| `/api/loyalty` | Loyalty Program | Loyalty |
| `/api/warranty` | Warranty & Returns | Warranty & Returns |
| `/api/shipments` | Shipments | Shipments |
| `/api/accounting` | Accounting / GL | Accounting |
| `/api/accounts` | Chart of Accounts | Accounting |
| `/api/transactiontypes` | Transaction Types | Settings |
| `/api/communications` | WhatsApp | WhatsApp |
| `/api/owner-cockpit` | Executive Dashboard | Dashboard |
| `/api/business-assistant` | AI Assistant | AI Assistant |
| `/api/growth` | Growth Intelligence | Money Finder |
| `/api/reportbuilder` | Report Builder | Report Builder |
| `/api/search` | Smart Search | (global) |
| `/api/activity-log` | Audit Trail | Activity Log |
| `/api/web-catalog` | Web Storefront | Customer Storefront |
| `/api/brands` | Brands Catalog | Management |
| `/api/categories` | Categories Catalog | Management |
| `/api/carbrands` | Car Brands | Management |
| `/api/carmodels` | Car Models | Management |
| `/api/currencies` | Currency Rates | Settings |
| `/api/appconstants` | App Configuration | Settings |
| `/api/excelimport` | Excel Import | Management |
| `/api/health` | Health Check | — |

---

## 9. Completeness Matrix

| Feature | Backend API | Database | Web UI | Mobile | WPF | Overall Status |
|---|---|---|---|---|---|---|
| Login / Auth | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| User Management | ✅ | ✅ | ✅ (Management) | ⚠️ Partial | ✅ | ✅ Complete |
| Role & Menu Access | ✅ | ✅ | ✅ | ❌ | ⚠️ | ✅ Complete |
| Part Master (CRUD) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Stock Levels | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Stock Transfer | ✅ | ✅ | ✅ | ⚠️ | ⚠️ | ✅ Complete |
| Dead Stock | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ Complete |
| Part Expiry Alerts | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Part Substitutes | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ Complete |
| Reorder Center | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Barcode Scan | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ Complete (web) |
| Visual Part Search | ✅ | — | ✅ | ❌ | ❌ | ✅ Complete (web) |
| AI Part Notes | ✅ | — | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Excel Import | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Sales Invoice | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Profit Per Line | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Payment Recording | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Quotes / Estimates | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Purchases | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Customer Master | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Customer Aging | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Customer Pricing Tiers | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Loyalty Program | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Supplier Master | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Supplier Price History | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Used Cars | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Repair Prep Board | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ Complete |
| Warranty Claims | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Shipments | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Accounting / GL | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ Complete |
| Trial Balance / SOA | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| WhatsApp Messaging | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ Complete |
| WhatsApp Campaigns | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Business Assistant (AI) | ✅ | — | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Growth Intelligence | ✅ | — | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Report Builder | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Owner Cockpit | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ Complete |
| Part Requests | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Smart Search | ✅ | — | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Web Catalog / Storefront | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ Complete |
| Activity Log | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |
| Warehouse / Location Mgmt | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete |
| Stock Arrival Theater | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ Complete (web) |

**Legend:** ✅ Available | ⚠️ Partial / unclear | ❌ Not present

---

## 10. User Flow Report

### Flow 1: Login

**Steps:**
1. User opens the app (web/mobile/WPF)
2. Enters username + password → POST `/api/auth/login`
3. JWT returned → stored client-side
4. All subsequent requests include `Authorization: Bearer <token>`
5. Token expires after 12 hours → user re-authenticates

**Screens:** Login screen (all platforms)  
**APIs:** `POST /api/auth/login`  
**Data saved:** JWT (client-side only)  
**Weak points:** No refresh token mechanism found — users are logged out hard after 12 hours.

---

### Flow 2: Create Sales Invoice

**Steps:**
1. Staff opens **POS / Sales** screen
2. Selects warehouse (defaults to main warehouse)
3. Optionally selects a registered customer (or walk-in)
4. Picks parts from dropdown → unit price auto-fills from part's sale price
5. Sets quantity → clicks **Add Line** (line added to draft)
6. Repeats for each item
7. Sets payment method and paid amount
8. Clicks **Create Invoice** → POST `/api/sales`
9. Backend: creates `Transactions` record, `TransactionItems`, `StockMovements`, auto-posts GL journal entries
10. Invoice number displayed; invoice appears in list

**Screens:** `invoices-view.js`  
**APIs:** `POST /api/sales`, `GET /api/customers`, `GET /api/warehouses`, `GET /api/parts`  
**Tables written:** `Transactions`, `TransactionItems`, `StockMovements`, `JournalEntries`, `JournalLines`  
**Weak points:** No quantity-available check before adding to draft (could oversell if stock check is deferred to backend).

---

### Flow 3: Create and Convert Quote

**Steps:**
1. Staff opens **Quotes / Estimates**
2. Fills customer name, phone, warehouse, expiry date
3. Adds parts from catalog with quantity and price
4. Clicks **Create Quote** → POST `/api/quotes` → status = Draft
5. Quote appears in list
6. Staff clicks **View** → detail panel expands
7. Clicks **Mark Sent** → PUT `/api/quotes/{id}/status` → status = Sent
8. Customer accepts → **Mark Accepted** → status = Accepted
9. Clicks **Convert to Invoice** → POST `/api/quotes/{id}/convert`
10. Backend: validates catalog parts exist, creates sale invoice, updates quote status to Converted
11. New invoice created → staff navigates to Invoices

**Screens:** `quotes-view.js` → `invoices-view.js`  
**APIs:** `POST /api/quotes`, `PUT /api/quotes/{id}/status`, `POST /api/quotes/{id}/convert`, `GET /api/sales`  
**Tables written:** `Quotes`, `QuoteItems`, `Transactions`, `TransactionItems`, `StockMovements`  
**Weak points:** Quote lines without a `PartId` are silently dropped during conversion. No WhatsApp quote-sharing yet.

---

### Flow 4: Receive New Stock (Purchase)

**Steps:**
1. Purchasing opens **Part Purchases** screen
2. Selects supplier and warehouse
3. Adds parts with quantities and unit costs
4. Creates purchase invoice → POST `/api/purchases`
5. Backend: creates `Transactions` (purchase type), `TransactionItems`, increases `Stock.Quantity`, creates `StockMovements`, posts GL journal entries

**Screens:** `purchase-parts` module workspace  
**APIs:** `POST /api/purchases`, `GET /api/suppliers`, `GET /api/parts`, `GET /api/warehouses`  
**Tables written:** `Transactions`, `TransactionItems`, `Stock`, `StockMovements`, `JournalEntries`

---

### Flow 5: Customer Aging Review

**Steps:**
1. Manager opens **Customer Aging** screen
2. App calls GET `/api/customers/aging`
3. SQL query groups unpaid invoices by customer, calculates days overdue via `DATEDIFF`
4. Customers shown in table with buckets: Current / 1-30 / 31-60 / 61-90 / 90+
5. Manager filters by customer name/phone
6. Overdue amounts highlighted in red
7. Manager initiates follow-up (manual phone call or WhatsApp message)

**Screens:** `customer-aging-view.js`  
**APIs:** `GET /api/customers/aging`  
**Tables read:** `Transactions`, `Customers`  
**Weak points:** No direct "send WhatsApp reminder from aging screen" button yet.

---

### Flow 6: Part Request Lifecycle

**Steps:**
1. Customer requests a part not in stock
2. Staff creates request → POST `/api/partrequests`
3. Staff contacts supplier → updates status to **Contacted**
4. Part arrives → staff reserves stock → POST `/api/partrequests/{id}/reserve` → status **Reserved**
5. Customer collects → status **Fulfilled**
6. If customer cancels → release reservation → POST `/api/partrequests/{id}/release-reservation` → status **Cancelled**

**Screens:** `part-requests-view.js`  
**APIs:** `POST /api/partrequests`, `PUT /api/partrequests/{id}/status`, `POST /api/partrequests/{id}/reserve`  
**Tables written:** `PartRequests`, `Stock` (reserved quantity)

---

### Flow 7: Used Car Part-Out

**Steps:**
1. Staff adds used car → POST `/api/usedcars` (make, model, year, VIN)
2. Uploads photos → POST `/api/usedcars/{id}/images`
3. Records purchase transaction → POST `/api/purchases/used-cars`
4. Parts stripped from car → created as new Parts linked to vehicle
5. Stock allocated from vehicle → `Stock` records created with pricing from `UsedCarPartPricingAllocator`
6. Parts become available for sale in inventory
7. Vehicle can be sold wholesale → POST `/api/usedcars/{id}/wholesale-sales`

**Screens:** `used-cars-view.js`, `repair-prep-board-view.js`  
**APIs:** `POST /api/usedcars`, `POST /api/usedcars/{id}/images`, `POST /api/purchases/used-cars`  
**Tables written:** `UsedCars`, `usedcar_images`, `Transactions`, `Parts`, `Stock`

---

### Flow 8: WhatsApp Customer Communication

**Steps:**
1. Staff opens **WhatsApp** screen
2. Views conversation list → GET `/api/communications/conversations`
3. Selects conversation → GET `/api/communications/messages`
4. Types message → POST `/api/communications/send`
5. Backend: routes via webhook to WhatsApp provider; logs to `OutboundMessages`
6. Inbound messages arrive via webhook → POST `/api/communications/inbound`

**Screens:** `whatsapp-view.js`  
**APIs:** `POST /api/communications/send`, `GET /api/communications/conversations`  
**Tables written:** `OutboundMessages`  
**Weak points:** WhatsApp provider is abstracted via webhook — provider configuration must be set correctly in `appsettings.json`.

---

## 11. Risk and Bug Table

| # | Area | Risk / Bug | Severity | Evidence | Recommendation |
|---|---|---|---|---|---|
| 1 | Quote conversion | Quote lines without `PartId` are silently dropped during `ConvertToInvoice`. If ALL lines lack a PartId, conversion throws `ValidationException` — but if some do, non-catalog lines disappear without warning | Medium | `QuotesService.cs:164-166` | Warn user in UI which lines will be dropped before converting |
| 2 | Sales invoice | No stock availability check in the draft builder before adding lines. Oversell risk deferred to backend | Medium | `invoices-view.js:129-166` | Add real-time available-stock display on part selection |
| 3 | JWT auth | No refresh token mechanism. Hard logout after 12 hours with potential loss of unsaved form data | Medium | `JwtSettings.cs`, `AuthService.cs` | Implement refresh token flow or silent re-authentication |
| 4 | WhatsApp | Provider webhook URL and secret must be manually configured. No validation or fallback if misconfigured | Medium | `appsettings.json` `Communications` section | Add health check for communications provider connectivity |
| 5 | Migrations | 35 migrations are NOT auto-applied at startup — they require explicit `EnsureApplied()` calls or running SQL DDL manually. Missing `QuotesMigration` call at startup | High | `SparePartsApiComposition.cs` (no `EnsureApplied` call for QuotesMigration) | Add migration runner at startup or document manual steps clearly |
| 6 | OpenAI dependency | AI features (business assistant, visual search, part notes, growth) will silently fail or error if `OpenAI:ApiKey` is not configured | Medium | `SparePartsApiComposition.cs` HttpClient registrations | Add graceful degradation and clear error messages when AI is unconfigured |
| 7 | Customer aging | Aging query reads `PaidAmount < TotalAmount` — does not account for credit notes or adjustments outside the `Transactions` table | Low | `CustomersService.cs GetAging()` | Review if credit notes affect balances and adjust SQL |
| 8 | File uploads | `POST /api/usedcars/{id}/images` stores binary image data directly in SQL Server `usedcar_images` table. This will impact DB size and performance at scale | Medium | `UsedCarImagesMigration.cs` | Consider moving to blob storage (S3, Azure Blob) |
| 9 | `hashpassword` endpoint | `GET /api/auth/hashpassword` appears to be a dev-only utility. If not properly restricted in production it could be misused | Low | `AuthController.cs:48` | Confirm it is gated by environment (dev-only) or remove |
| 10 | Accounting auto-post | Sale and purchase auto-posting depends on `AccountingPostingSettings` being correctly configured. Missing configuration silently skips journal entries | Medium | `SaleAccountingStrategy.cs`, `PurchaseAccountingStrategy.cs` | Add validation/warning if posting settings are incomplete |
| 11 | Mobile feature gap | Mobile app has 16 screens but does not expose quotes, aging, warranty, shipments, expiry alerts, reorder, or report builder | Low | `screen-registry.js` (mobile) | Assess and plan mobile feature parity for critical screens |
| 12 | Schema.sql vs migrations | `database/schema.sql` contains base table DDL; 13+ additional tables are created only by code migrations. Running `schema.sql` alone will produce an incomplete database | Medium | `database/schema.sql`, migration files | Create a single combined idempotent DDL script or document the full setup order |

---

## 12. Missing Feature Recommendations

These features do NOT currently exist in the codebase. They are recommended based on the workflows and business context of an auto parts shop.

| # | Feature | Business Value | Effort | Notes |
|---|---|---|---|---|
| 1 | **WhatsApp Quote Sharing** | High — share quotes directly to customer WhatsApp | Low | Button in `quotes-view.js` calling `/api/communications/send` with quote PDF or text |
| 2 | **Quote PDF / Print Export** | High — professional printable quote for customers | Medium | Generate PDF server-side or browser print-to-PDF |
| 3 | **Refresh Token / Session Keep-Alive** | High — prevents data loss on 12-hour hard logout | Medium | Implement refresh token endpoint in `AuthController` |
| 4 | **Automated Migration Runner** | High — prevents empty DB from missing migrations at startup | Medium | Run `EnsureApplied()` for all migrations in `Program.cs` |
| 5 | **Supplier Aging (Payables)** | High — mirror of customer aging for outstanding payables | Medium | Mirror `GetAging()` pattern on `SuppliersService` |
| 6 | **Return Merchandise Authorization (RMA)** | High — formal return workflow with credit notes | High | New entity; credit note transaction type; GL reversal |
| 7 | **Customer Credit Limit Enforcement** | Medium — block invoices when customer balance exceeds limit | Medium | Check `Customers.CreditLimit` vs. aging balance in `SalesService` |
| 8 | **Invoice Profit History Chart** | Medium — margin trend over time per part / category | Medium | New API endpoint; chart in invoices-view.js |
| 9 | **Quote Expiry Notifications** | Medium — auto-notify customer before quote expires | Medium | Background job polling `Quotes.ExpiryDate`; send via WhatsApp |
| 10 | **Customer Aging Alert on Dashboard** | Medium — surface overdue receivables in Owner Cockpit | Low | Add "Overdue Receivables" card linking to `customer-aging` screen |
| 11 | **Payment Gateway Integration** | High — online payment from web storefront | High | Stripe/PayPal integration for `/api/web-catalog/checkout` |
| 12 | **Mobile Parity for Quotes, Warranty, Aging** | Medium — staff on mobile cannot access new screens | Medium | Add mobile screens mirroring web views |
| 13 | **Blob Storage for Images** | Medium — vehicle images in SQL will degrade performance at scale | Medium | Replace binary column with URL; upload to S3/Azure Blob |
| 14 | **Two-Factor Authentication (2FA)** | Medium — security hardening for admin accounts | Medium | TOTP or SMS OTP on login for Admin/Manager roles |
| 15 | **Stock Reservation Expiry** | Low — auto-release reservations after N days | Low | Background job; configurable timeout via AppConstants |
| 16 | **WhatsApp Part Request Auto-Response** | Low — auto-reply to inbound part request messages | Medium | Parse inbound webhook; query parts; send availability reply |
| 17 | **Barcode Label Printing** | Low — print barcodes directly from Parts screen | Low | Browser print or label printer integration |
| 18 | **Multi-Tenant / Multi-Branch** | Future — separate data per branch | High | Significant schema and auth changes required |

---

*Report generated from direct code inspection of the SpareParts repository. Every feature documented above is backed by at least one referenced source file. Features marked as unclear or partial reflect genuine code-level uncertainty, not assumptions.*
