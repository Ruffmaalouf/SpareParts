# SpareParts

SpareParts is an end-to-end automotive spare-parts operations platform combining inventory control, purchasing, point-of-sale workflows, and accounting journal posting.

This repository contains a .NET 8 Web API, a WPF desktop client, shared domain and infrastructure libraries, and an architecture-focused test suite.

---

## Table of contents

1. [Who this document is for](#who-this-document-is-for)
2. [System overview](#system-overview)
3. [Repository structure](#repository-structure)
4. [Architecture and boundaries](#architecture-and-boundaries)
5. [Technology stack](#technology-stack)
6. [Configuration reference](#configuration-reference)
7. [Onboarding quick start](#onboarding-quick-start)
8. [API surface map](#api-surface-map)
9. [Runbook for enterprise IT maintenance](#runbook-for-enterprise-it-maintenance)
10. [Testing and quality gates](#testing-and-quality-gates)
11. [Deployment guidance](#deployment-guidance)
12. [Security and compliance checklist](#security-and-compliance-checklist)
13. [Known limitations and follow-up docs](#known-limitations-and-follow-up-docs)

---

## Who this document is for

This README is intentionally detailed for:

- **New engineers** onboarding to the codebase.
- **Enterprise IT operations** teams responsible for uptime and incident response.
- **Release managers** validating deployment readiness and smoke checks.
- **Security/compliance reviewers** verifying credential/configuration practices.

If you only need to run locally, start with [Onboarding quick start](#onboarding-quick-start).

---

## System overview

SpareParts supports core back-office workflows:

- User authentication and role-based access.
- Master data maintenance (brands, categories, parts, customers, suppliers, warehouses).
- Sales and purchase invoice creation.
- Inventory stock movement updates.
- Accounting journal line generation for commercial transactions.

### Runtime context

1. Desktop user signs in through the API (`/api/auth/login`).
2. API issues JWT, Desktop stores and attaches token for subsequent calls.
3. Controllers delegate to service/handler layer.
4. Services call repositories and domain policies.
5. SQL Server persists business data + movement/journal records.

---

## Repository structure

```text
src/
  SpareParts.Api                      ASP.NET Core API host/composition root
  SpareParts.Domain                   Domain entities, DTOs, and enums
  SpareParts.Infrastructure           Repositories, DB access, services, handlers
  SpareParts.Infrastructure.Interfaces
  SpareParts.Desktop.Wpf              WPF shell application (Windows)
  SpareParts.Desktop.ViewModels       UI workflows/state orchestration
  SpareParts.Desktop.Helpers          API clients, DI helpers, theme/state utilities
  SpareParts.Desktop.Controls         Reusable WPF controls
  SpareParts.Desktop.Interfaces       Cross-assembly desktop contracts

tests/
  SpareParts.ArchitectureTests        Layering + critical-path behavior tests

docs/
  CurrencyRates.sql                   SQL seed/reference artifact
  SENIORITY_EVALUATION_REVIEW.md      Supplemental project note
```

### Solution files

- `SpareParts.sln` is the primary multi-project solution.
- Additional focused solution files exist for controls/interfaces/helpers/view-models.

---

## Architecture and boundaries

### Layered model

#### 1) Domain layer (`SpareParts.Domain`)

- Holds business contracts and entities.
- Contains domain areas such as inventory, sales, purchases, accounting, auth, master data.
- Must not reference infrastructure implementation details.

#### 2) Infrastructure layer (`SpareParts.Infrastructure`)

- Implements repositories and DB integration (`ISqlConnectionFactory`, `DbSession`).
- Contains operational services (`SalesService`, `PurchaseService`, `InventoryService`).
- Contains accounting and policy implementations (`SaleAccountingStrategy`, `DefaultPaymentStatusPolicy`).

#### 3) API layer (`SpareParts.Api`)

- Program startup/DI registrations.
- JWT authentication + authorization configuration.
- CORS policy setup.
- Controller endpoints.
- Exception middleware and startup migrations.

#### 4) Desktop layer (`SpareParts.Desktop.*`)

- WPF views + controls.
- ViewModels and workflow coordination.
- API client wrappers and session/token helpers.

### Startup behavior (API)

On API boot, the host:

1. Resolves configuration (including connection string and JWT settings).
2. Registers services and repositories in DI.
3. Configures JWT bearer auth and authorization.
4. Applies startup migrations:
   - `MenuAccessMigration.EnsureApplied(...)`
   - `TransactionTypesMigration.EnsureApplied(...)`
   - `InvoiceNumberingMigration.EnsureApplied(...)`
5. Enables middleware pipeline (`ApiExceptionMiddleware`, CORS, auth, endpoints).

### Split-service API model ("mother API" decomposition)

The original `SpareParts.Api` host remains available as the full-composition API, and the repo now also includes capability-focused API hosts:

- `SpareParts.Identity.Api` → auth/users/roles + health
- `SpareParts.Catalog.Api` → brands/categories/car catalog/currencies/app constants + health
- `SpareParts.Inventory.Api` → parts/warehouses/transaction types + health
- `SpareParts.Sales.Api` → sales/customers + health
- `SpareParts.Purchases.Api` → purchases/suppliers + health

Each split API uses the same shared composition root (`SpareParts.Api.Hosting`) and only enables a subset of controllers via capability filtering.
`GET /api/health` now also reports the running service name and enabled capabilities so you can verify each API is not "empty".

---

## Technology stack

- **Language/runtime:** C# / .NET 8
- **API framework:** ASP.NET Core
- **Desktop:** WPF (`net8.0-windows`)
- **Data access:** SQL Server + Dapper/repository patterns
- **Authentication:** JWT Bearer tokens
- **Password hashing:** BCrypt
- **Testing:** xUnit (architecture + critical path checks)

---

## Configuration reference

## API configuration (`src/SpareParts.Api/appsettings.json`)

| Key | Required | Purpose | Notes |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | Yes | SQL Server connection string | Required in non-development mode. |
| `Jwt:Secret` | Yes | JWT signing secret | Must be replaced in real environments. |
| `Jwt:Issuer` | Yes | Token issuer | Defaults to `SpareParts.Api`. |
| `Jwt:Audience` | Yes | Token audience | Defaults to `SpareParts.Desktop`. |
| `Jwt:ExpiryHours` | Yes | Token lifetime | Default `12`. |
| `Accounting:*` | Yes | Account ID mapping for journal strategies | Must align with chart of accounts. |
| `Cors:AllowedOrigins` | Recommended | Restrict client origins | If absent: permissive in Development. |

### API secure configuration baseline

- Replace placeholder secret value immediately.
- Use environment-specific secret injection (environment variables, vault, or platform secret store).
- Restrict CORS origins to trusted desktop/web entry points.
- Use environment-specific connection strings (avoid local defaults outside dev).

## Desktop configuration (`src/SpareParts.Desktop.Wpf/appsettings.wpf.json`)

| Key | Required | Purpose | Default |
|---|---|---|---|
| `ApiBaseUrl` | Yes | Base URL for API clients | `http://localhost:5000/` |

---

## Onboarding quick start

## Prerequisites

### API development/runtime

- .NET SDK 8.x
- SQL Server instance reachable from API host

### Desktop development/runtime

- Windows OS (WPF target: `net8.0-windows`)
- .NET SDK 8.x

> API work can be done on non-Windows systems; WPF app execution requires Windows.

## Step 1: restore dependencies

```bash
dotnet restore SpareParts.sln
```

## Step 2: run API

```bash
dotnet run --project src/SpareParts.Api/SpareParts.Api.csproj
```

Expected behavior:

- Host starts listening (default local profile typically on `http://localhost:5000`).
- Startup migrations are executed.

## Step 3: health smoke check

```bash
curl http://localhost:5000/api/health
```

Expected response pattern:

```json
{ "status": "ok", "utc": "..." }
```

## Step 4: run desktop client (Windows)

```bash
dotnet run --project src/SpareParts.Desktop.Wpf/SpareParts.Desktop.Wpf.csproj
```

Before login, confirm `ApiBaseUrl` matches running API address.

---

## API surface map

The following controller groups are available under `src/SpareParts.Api/Controllers`:

| Domain area | Route prefix (representative) | Notes |
|---|---|---|
| Health | `GET /api/health` | Anonymous health signal. |
| Auth | `/api/auth/*` | Login, current user info, dev-only hash utility. |
| Users | `/api/users` | CRUD-like user operations. |
| Roles | `/api/roles` | Role CRUD + menu access management. |
| Customers | `/api/customers` | Master data maintenance. |
| Suppliers | `/api/suppliers` | Master data maintenance. |
| Warehouses | `/api/warehouses` | Warehouse setup/maintenance. |
| Brands / Categories / Parts | `/api/brands`, `/api/categories`, `/api/parts` | Inventory master data. |
| Car brands / models | `/api/carbrands`, `/api/carmodels` | Includes media endpoints. |
| Sales | `/api/sales` | Create/search/update invoice workflows. |
| Purchases | `/api/purchases` | Purchase creation workflow. |
| Transaction types | `/api/transactiontypes` | Lookup and maintenance. |
| Currencies | `/api/currencies` | Currency rate retrieval endpoint. |

For exact request/response contracts, inspect each controller and its domain DTOs.

---

## Runbook for enterprise IT maintenance

## 1) Service startup verification checklist

1. API process starts without fatal config exceptions.
2. DB connectivity succeeds (no connection/login timeouts).
3. `GET /api/health` returns HTTP 200 and status `ok`.
4. Auth login succeeds for a valid active account.
5. A protected endpoint succeeds with bearer token.

## 2) Incident triage matrix

### Incident A: Startup failure due to missing JWT settings

- **Typical symptom:** `InvalidOperationException` referencing `Jwt:Secret`.
- **Likely cause:** secret not supplied in deployment config.
- **Action:** set valid JWT secret and restart host.
- **Post-action validation:** login flow + protected endpoint access.

### Incident B: Startup failure due to missing DB connection string

- **Typical symptom:** missing `ConnectionStrings:DefaultConnection` in non-dev.
- **Likely cause:** environment override missing.
- **Action:** supply valid connection string and restart.
- **Post-action validation:** health check and data endpoint read.

### Incident C: Desktop cannot call API

- Validate `ApiBaseUrl` in desktop config.
- Validate DNS/host/port and firewall rules.
- Confirm API is listening on expected interface/port.
- Confirm token is present and not expired.

### Incident D: Authentication failures for valid users

- Verify account is active in `Users` table.
- Verify stored password hash format is valid BCrypt.
- In development only, use `/api/auth/hashpassword` utility to generate compliant hash and update user record if required.

### Incident E: Inventory/accounting mismatch report

- Validate accounting account IDs in API config.
- Re-run critical path tests for totals/balancing behavior.
- Review recent sales/purchase updates for partial failure patterns.
- Inspect exception logs for movement/journal insert failures.

## 3) Operational diagnostics checklist

- Inspect ASP.NET Core logs (`Logging:LogLevel` config).
- Review API exception middleware output envelope.
- Review SQL exception logs emitted by `SqlExceptionLogWriter`.
- Validate server clock/NTP synchronization (important for JWT expiry behavior).

## 4) Recovery and rollback guidance

- Keep previous stable deployment artifact available.
- Roll back API if startup migration behavior diverges unexpectedly.
- Preserve DB backups prior to schema/data-affecting changes.
- Re-run smoke checks after rollback (health, login, protected read).

---

## Testing and quality gates

## Primary automated suite

```bash
dotnet test tests/SpareParts.ArchitectureTests/SpareParts.ArchitectureTests.csproj
```

### What this suite protects

- Domain layer does not reference infrastructure layer.
- Handler placement remains within infrastructure namespace.
- Invoice totals calculations match expected arithmetic.
- Inventory stock/movement update behavior and rollback paths.
- Accounting journal balancing behavior.
- Concurrency behavior for inventory adjustments.

## Recommended CI pipeline baseline

1. `dotnet restore SpareParts.sln`
2. `dotnet build SpareParts.sln -c Release`
3. `dotnet test tests/SpareParts.ArchitectureTests/SpareParts.ArchitectureTests.csproj -c Release`
4. Deploy to integration environment.
5. Execute smoke checks:
   - `GET /api/health`
   - Auth login
   - One read operation on protected endpoint

---

## Deployment guidance

- Deploy API as an ASP.NET Core service (containerized or VM-based).
- Run behind standard reverse proxy/load balancer per enterprise policy.
- Keep environment configs externalized per environment tier (dev/test/prod).
- Rotate JWT secrets on an established schedule.
- Version desktop and API together; document supported compatibility matrix.

### Suggested environment tiers

- **DEV:** permissive diagnostics, seeded sample data, rapid change.
- **TEST/UAT:** production-like auth/networking, controlled data refreshes.
- **PROD:** locked-down secrets, constrained CORS, monitored SLOs.

---

## Security and compliance checklist

- [ ] No placeholder secrets in deployed config.
- [ ] Principle-of-least-privilege DB credentials.
- [ ] HTTPS termination and secure transport enforced in hosted environment.
- [ ] JWT expiry and issuer/audience validated as expected.
- [ ] Admin-only/development-only utilities are not exposed in production paths.
- [ ] Logging policy reviewed to avoid sensitive-data leakage.

---

## Known limitations and follow-up docs

Current repository documentation still needs dedicated deep-dive pages for:

1. Full database schema and migration/version strategy.
2. Environment-specific deployment manifests (IIS/systemd/container examples).
3. Backup/restore RTO/RPO procedures.
4. SLO/alert thresholds and dashboard references.
5. Version compatibility matrix between desktop releases and API releases.

Until those docs are added, treat this README as the source of truth for onboarding + first-line operations.
