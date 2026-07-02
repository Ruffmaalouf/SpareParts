# Backend API & Infrastructure Architecture Reference

Scope: `src/SpareParts.Api/` and `src/SpareParts.Infrastructure/`. This document is written so a new
engineer can onboard from it alone — it covers the high-level architecture, the request flow from
controller to database, the capability/module system, the migration system, the notification
hosted-service pattern, and a file-by-file map of the major services. For the cross-solution layer
map (Domain / Application / Desktop / Web / Mobile) see `docs/ARCHITECTURE.md`; this document is the
deep dive on the two backend projects only.

---

## 1. High-Level Architecture

`SpareParts.Api` is a single ASP.NET Core 8 Web API project (net8.0) that can be compiled and run as
either:

- **The monolith** (`SpareParts.Api` itself) — every capability enabled, every controller mounted,
  every hosted background "agent" running. This is what `dotnet run` in `src/SpareParts.Api` starts
  today.
- **A capability-scoped microservice** — the same codebase, DI composition, and controllers, but
  booted with a reduced `ServiceCapability[]` list (see §3). Five such profiles are declared in
  `SparePartsApiComposition.ExpectedServiceProfiles` and each has its own thin host project under
  `src/`: `SpareParts.Sales.Api`, `SpareParts.Purchases.Api`, `SpareParts.Inventory.Api`,
  `SpareParts.Identity.Api`, `SpareParts.Catalog.Api`. Each of these host projects is nothing more than
  a `Program.cs` calling `builder.AddSparePartsApiCore()` +
  `builder.Services.AddCapabilities(appName, ServiceCapability.X, ServiceCapability.Health)` +
  `builder.Services.AddCapabilityControllers(ServiceCapability.X, ServiceCapability.Health)` — all of
  the actual controllers/services/middleware/migrations they use are compiled from `SpareParts.Api`
  and `SpareParts.Infrastructure`; the host project only decides which capability subset is turned on.

Two projects make up the backend:

| Project | Responsibility |
|---|---|
| `SpareParts.Api` | HTTP surface: controllers, JWT auth wiring, middleware pipeline, DI composition root, all SQL schema migrations, background "agent" hosted services, SignalR notifications hub. |
| `SpareParts.Infrastructure` | Everything controllers call into: Dapper repositories, business/domain services, accounting engine, payment/subscription engine, cross-cutting exception types, `DbSession`/`RepositoryCatalog` data-access plumbing. |

Both depend on `SpareParts.Domain` (pure DTOs/entities/enums, no framework references) and
`SpareParts.Infrastructure.Interfaces` (the contracts — `ISqlConnectionFactory`, `ITenantContext`,
`IInventoryService`, `ISubscriptionLimitService`, etc. — that let `Infrastructure` and `Api` depend on
abstractions rather than concrete types where cross-capability wiring is needed).

Database access is 100% Dapper over `Microsoft.Data.SqlClient` (SQL Server) — there is no EF Core, no
ORM change-tracking, and no code-first migrations tool. Schema creation/evolution is done by a set of
idempotent, hand-written `EnsureApplied(ISqlConnectionFactory)` static methods (see §5).

---

## 2. Request Flow: Controller → Database

A typical read/write request flows through these layers in order:

1. **Kestrel / ASP.NET Core pipeline** (registered in `SparePartsApiComposition.UseSparePartsApiPipeline`,
   see §7 for the exact middleware order): security headers → global exception handler → CORS → rate
   limiter → JWT authentication → web-app-user route restriction → tenant resolution → ASP.NET
   authorization → SignalR hub mapping → controller routing.
2. **Controller** (`src/SpareParts.Api/Controllers/*.cs`) — every controller inherits
   `SparePartsControllerBase` (itself `ControllerBase`), is annotated `[ApiController]`,
   `[Route("api/...")]`, and (with the sole exception of the anonymous auth/health/web-catalog
   surface) `[Authorize]`. The base class exposes `CurrentUserId`, `CurrentRoleId`, `CurrentTenantId`,
   `CurrentUserIsSuperAdmin` (all read from JWT claims via `ClaimTypes`/`AuthorizationPolicies`/
   `TenantResolutionMiddleware` constants) plus pagination helpers
   (`NormalizePagination`/`NormalizeOptionalPagination`/`ApplyPaginationHeaders`). Controllers are thin:
   they validate the route/model, call exactly one `Infrastructure` service method, and translate the
   result to an `ActionResult`.
3. **Service** (`src/SpareParts.Infrastructure/Services/**/*.cs`) — holds the actual business logic.
   Two data-access shapes are used depending on whether the operation needs a transaction:
   - **Simple read/write, no transaction needed**: `using var conn = _factory.CreateConnection();` then
     Dapper `Query`/`QuerySingleOrDefault`/`Execute` calls directly (see e.g. `DemandMatchingService`,
     `SellerReputationService`).
   - **Multi-step write that must be atomic**: `using var session = new DbSession(_factory, tenantId);`
     — opens a connection *and* begins a transaction together; every statement passes
     `session.Transaction`; the method calls `session.Commit()` only after every step succeeds (see
     e.g. `PartsService.Create`, `PartRequestsService.Reserve`). If a service further needs
     capability-organized repositories instead of raw SQL, it builds a `RepositoryCatalog.For(session)`
     — see §4.
4. **Repository** (only for the capabilities that have them —
   `src/SpareParts.Infrastructure/Data/Repositories/**`) — thin Dapper wrappers scoped to one
   `DbSession`, grouped by business area (`SalesRepositories`, `PurchaseRepositories`,
   `InventoryRepositories`, `AccountingRepositories`, `MasterDataRepositories`). Not every service uses
   the repository layer; many services query `session.Connection`/`conn` directly with inline SQL,
   which is the dominant style in this codebase (repositories are used mainly by
   sale/purchase/return/handler workflows that need shared, reusable data access across several
   handlers).
5. **Dapper → SQL Server** — all SQL is hand-written, parameterized (`new { Param = value }` anonymous
   objects bound by Dapper), and tenant-filtered with the `(@TenantId = 0 OR TableAlias.TenantId =
   @TenantId)` pattern (0 = super-admin/cross-tenant bypass, see §8).
6. **Exceptions bubble to `ApiExceptionMiddleware`** — services throw typed exceptions
   (`ValidationException`, `NotFoundException`, `ConflictException`, `PlanLockException`,
   `ExternalServiceException`, or `UnauthorizedAccessException`); the middleware maps each to an HTTP
   status + machine-readable `code`, logs full exception detail (including stack trace) to the
   `ExceptionLogEntry` audit table via `IExceptionLogWriter`, and returns only a generic
   `"An unexpected server error occurred."` message to the client for unmapped/500-class errors — full
   `ex.Message` is only echoed back for the explicitly-typed 4xx exceptions above, never raw stack
   traces.

### Example trace: `POST /api/parts`

```
PartsController.Create(CreatePartRequest req)
  → PartsService.Create(request, userId)
      → new DbSession(_factory, tenantId)             // opens conn + BEGIN TRAN
      → ValidateUsedCar(session, request.UsedCarId)
      → subscriptionLimitService.EnsureWithinLimit(...)  // plan/limit gate — throws PlanLockException if over quota
      → new PartsRepository(session).Insert(part)
      → UsedCarPartPricingAllocator.RepriceUsedCarParts(session, usedCarId, userId)   // if part links to a used car
      → EnsureInitialUsedCarPartStock(session, id, usedCarId, userId)
      → session.Commit()                               // COMMIT TRAN
  → NotifyPartAddedAsync(id, req, cancellationToken)   // controller pushes a SignalR "partAdded" event afterward
  → return Ok(id)
```

---

## 3. The Capability / Module System

Nine `ServiceCapability` enum values (`src/SpareParts.Api/Hosting/ServiceCapability.cs`) partition the
whole feature surface: `Sales`, `Purchases`, `Inventory`, `Accounting`, `Identity`, `Catalog`,
`Reporting`, `Health`, `Billing`. Every controller and most services are mapped to exactly one
capability. Composition is driven by three collaborating pieces in
`src/SpareParts.Api/Hosting/SparePartsApiComposition.cs`:

- **`AddCapabilities(this IServiceCollection, string serviceName, params ServiceCapability[])`** — DI
  registration. Registers a `ServiceProfile` singleton (service name + its capability set, used for
  diagnostics), then always registers tenant context + pricing/subscription/payment services
  (cross-cutting, needed everywhere), then conditionally registers each capability's services and
  hosted background agents behind `if (distinctCapabilities.Contains(ServiceCapability.X))` blocks. A
  few services intentionally straddle two capability blocks and are guarded with `TryAddScoped`
  instead of `AddScoped` so they register exactly once no matter which capability combination brings
  them in first (e.g. `IInventoryService`, `MarketPriceIndexService`, `PartRequestsService`,
  `ScanLookupService`, `SmartSearchService`). Several of the automated "agent" hosted services (see §6)
  are only registered when **multiple** capabilities are present together, because they depend on
  services that live in different capability blocks (e.g. the Intake & Teardown Agent needs
  `UsedCarsService`/`CarCrushService` from Catalog *and* `GarageStockService`/`PartGenealogyService`
  from Inventory).
- **`AddCapabilityControllers(this IServiceCollection, params ServiceCapability[])`** — controller
  discovery filtering. Builds an allow-list of controller type names from the private
  `ControllerMap` dictionary (capability → controller name array) and installs a
  `CapabilityControllerFeatureProvider` (an `IApplicationFeatureProvider<ControllerFeature>`) that
  removes any discovered controller not in that allow-list before MVC finishes composing the
  application. This is how a capability-scoped host (e.g. a hypothetical `SpareParts.Sales.Api`) would
  end up only exposing Sales + Health routes even though every controller class is compiled into the
  same assembly.
- **A static constructor guard** in `SparePartsApiComposition` throws `InvalidOperationException` at
  type-init time if any `ServiceCapability` enum value is missing from `ControllerMap` — this fails
  fast (before the host even starts) if a new capability is added without wiring its controllers.

`Program.cs` for the monolith simply calls `AddCapabilities` and `AddCapabilityControllers` with *all
nine* capabilities, so today's running service exposes every controller and every hosted agent.

---

## 4. Data-Access Patterns: `DbSession`, `RepositoryCatalog`, `ISqlConnectionFactory`

- **`ISqlConnectionFactory`** (`SpareParts.Infrastructure.Interfaces`) — single method
  `IDbConnection CreateConnection()`. Implemented by `SqlConnectionFactory`
  (`src/SpareParts.Infrastructure/Data/SqlConnectionFactory.cs`), which wraps
  `Microsoft.Data.SqlClient.SqlConnection`, opens it eagerly, and returns it. Registered as a
  **singleton** in `SparePartsApiComposition.AddSparePartsApiCore` with the connection string resolved
  from `ConnectionStrings:DefaultConnection` (falling back to a local-dev default only when
  `IsDevelopment()`; production throws `InvalidOperationException` if unset — the connection string is
  never printed or logged).
- **`DbSession`** (`src/SpareParts.Infrastructure/Data/DbSession.cs`) — `IDisposable` wrapper that
  opens a connection **and begins a transaction** together, exposing `Connection`, `Transaction`, and
  `TenantId`. Callers must explicitly call `Commit()`; if `Dispose()` runs without a prior `Commit()`,
  it rolls back automatically (defensive default — a thrown exception mid-method safely undoes partial
  writes). This is the standard shape for any service method that performs more than one write that
  must succeed or fail together.
- **`RepositoryCatalog`** (`src/SpareParts.Infrastructure/Data/Repositories/RepositoryCatalog.cs`) — a
  single object built from one `DbSession` (`RepositoryCatalog.For(session)`) that exposes five
  capability-grouped repository bundles as properties: `Sales` (`SalesRepositories`), `Purchases`
  (`PurchaseRepositories`), `Inventory` (`InventoryRepositories`), `Accounting`
  (`AccountingRepositories`), `MasterData` (`MasterDataRepositories`). Each of those bundle classes is
  itself a thin composition root that `new`s up its own individual repositories (e.g.
  `SalesRepositories` wraps `SalesRepository` and `SalesReturnRepository`) against the same session.
  This pattern exists so a multi-step handler (e.g. `CreateSaleHandler`) can reach every repository it
  needs off one object instead of manually constructing five separate repository instances.
- Not every service goes through `RepositoryCatalog` — plenty of simpler services skip repositories
  entirely and just run Dapper directly against `session.Connection`/`conn` with inline SQL (the
  majority pattern in this codebase; repositories are reserved for the handful of workflows —
  primarily sales/purchases/returns — that benefit from reuse across multiple call sites).
- **Tenant scoping**: `DbSession`/`RepositoryCatalog`-based services take a `tenantId` (usually
  `ITenantContext.TenantId`, populated per-request by `TenantResolutionMiddleware` from the caller's
  JWT claims) and apply it in SQL as `(@TenantId = 0 OR SomeTable.TenantId = @TenantId)`. TenantId `0`
  means "no tenant filter" and is only reachable for super-admin callers (see §8) — this is the
  mechanism that prevents one tenant's data from leaking into another tenant's API responses.

---

## 5. The Migration System

There is no migrations *framework* — `RunMigrations` in `SparePartsApiComposition.cs` is a flat,
manually ordered list of ~85 static method calls, one per migration class, executed synchronously on
every application startup (`UseSparePartsApiPipeline` calls `RunMigrations(factory)` before any
middleware is registered). Each migration lives in `src/SpareParts.Api/Infrastructure/*Migration.cs`
and follows the same contract:

```csharp
public static class SomeFeatureMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.SomeTable', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SomeTable ( ... );
END;
-- ALTER TABLE ... ADD COLUMN IF NOT EXISTS-style guards, seed data inserts, etc.
""");
    }
}
```

Key properties of this system:

- **Idempotent by construction** — every migration checks `OBJECT_ID(...) IS NULL` /
  `COL_LENGTH(...) IS NULL` / `IF NOT EXISTS` before creating tables, columns, or seed rows, so running
  `RunMigrations` on an already-current database is a safe no-op. This lets the same code run against a
  freshly-provisioned empty database and a years-old production database identically.
- **Order matters and is manual** — the sequence of calls inside `RunMigrations` (and the parallel
  `MigrationNames` list, see below) encodes real dependencies: `TenantsMigration` must run before
  `TenantIdMigration` (which back-fills a `TenantId` column onto ~40 tenant-owned tables and needs the
  `Tenants` table + its default row to exist first); `AccountingMigration` must run before
  `AccountingCurrencyRateRepairMigration`-style follow-up fixups; and so on. There is no dependency
  graph or topological sort — the ordering is simply the literal call order in the method body, and a
  new migration must be inserted at the correct position by a human.
- **`MigrationNames`** (`SparePartsApiComposition.ExpectedServiceProfiles`'s sibling field) is a
  separate `IReadOnlyList<string>` built from `nameof(EachMigrationClass)`, listing every migration
  class name in the same order as `RunMigrations`. It exists as a single source of truth other code
  (tests, tooling) can use to assert "every migration that runs is accounted for" without having to
  parse the `RunMigrations` method body — but note it is a *parallel, hand-maintained* list, not
  derived from `RunMigrations` automatically, so the two must be kept in sync by hand when adding a
  migration.
- **No down-migrations / rollback** — this is a forward-only, additive schema evolution system. Nothing
  in the codebase drops columns or tables as part of a migration.
- **Representative examples**:
  - `TenantsMigration` — creates `dbo.Tenants`, seeds the default tenant (Id 1) and a Super Admin role
    (Id 5) if missing.
  - `TenantIdMigration` — iterates a hard-coded `TenantTable[]` list (table name + whether to create a
    supporting index), adds a nullable `TenantId INT` column to each if missing, back-fills existing
    rows to `TenantsMigration.DefaultTenantId`, and creates `IX_{Table}_TenantId` indexes for the
    high-traffic tables.
  - `AccountingCurrencyRateRepairMigration` — a one-time data-repair migration (not a schema migration)
    that recalculates historical `JournalLines`/`Transactions`/`TransactionItems` rows whose currency
    conversion was computed with a stale/incorrect rate, using `AppConstants` + `CurrencyRates` to
    resolve the correct base/counter rate before rewriting the affected rows.
  - Most other migrations follow the simple "create table if missing (+ maybe seed rows)" shape:
    `PartRequestsMigration`, `WhatsAppCampaignsMigration`, `EscrowTransactionsMigration`,
    `ListingBoostsMigration`, etc.

---

## 6. The Notification / Hosted-Service ("Agent") Pattern

`src/SpareParts.Api/Notifications/` contains two kinds of classes:

1. **SignalR real-time push** — `NotificationsHub` (`[Authorize] : Hub`, mapped at
   `/hubs/notifications` in `UseSparePartsApiPipeline`) plus typed notification payload records
   (`PartAddedNotification`, `PartReservationReminderNotification`) and a small
   `NotificationEvents` string-constant class (`PartAdded`, `ReservationReminder`) naming the SignalR
   event names controllers/hosted services broadcast under. Controllers inject
   `IHubContext<NotificationsHub>` directly and call
   `_notifications.Clients.All.SendAsync(NotificationEvents.PartAdded, payload, ct)` after a successful
   write (see `PartsController.Create` → `NotifyPartAddedAsync`). JWT auth for the hub itself is wired
   specially in `AddSparePartsApiCore`: the `OnMessageReceived` JWT bearer event reads the token from
   the `access_token` query string (not the `Authorization` header) specifically for requests to
   `/hubs/notifications`, since browser `EventSource`/WebSocket clients can't set custom headers on the
   handshake.
2. **`BackgroundService` "agents"** — long-running hosted services that poll the database on a fixed
   interval and perform autonomous shop-automation work. Every agent follows the same three-part shape:

   ```csharp
   public sealed class SomeAgentHostedService : BackgroundService
   {
       private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30); // or minutes/hours

       protected override async Task ExecuteAsync(CancellationToken stoppingToken)
       {
           await RunOnceAsync(stoppingToken);          // run immediately on startup, don't wait a full interval
           using var timer = new PeriodicTimer(PollInterval);
           try
           {
               while (await timer.WaitForNextTickAsync(stoppingToken))
                   await RunOnceAsync(stoppingToken);
           }
           catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
       }

       private async Task RunOnceAsync(CancellationToken cancellationToken)
       {
           try
           {
               using var scope = _scopeFactory.CreateScope();       // hosted services are singletons —
               var service = scope.ServiceProvider.GetRequiredService<SomeScopedService>();  // must create a DI scope per tick to resolve scoped services
               // ... do the work, log outcomes with _logger.LogInformation/LogWarning ...
           }
           catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
           catch (Exception ex) { _logger.LogWarning(ex, "... tick failed."); }  // one tick's failure never crashes the host or blocks the next tick
       }
   }
   ```

   The eleven agents currently registered (conditionally, per §3) are:

   | Hosted service | Poll interval | Capability gate | Purpose |
   |---|---|---|---|
   | `PartReservationClockHostedService` | (see file) | Inventory | Expires/reminds on part-request stock reservations past their deadline. |
   | `ReservationExpiryHostedService` | (see file) | Inventory | Releases reserved stock back to available inventory once a reservation lapses. |
   | `QuoteExpiryHostedService` | (see file) | Sales | Expires stale customer quotes. |
   | `ReportBuilderBackgroundRunHostedService` | (see file) | Reporting | Executes scheduled/background report-builder runs. |
   | `SubscriptionMaintenanceHostedService` | 1 hour | always | Expires due subscriptions, flags past-due ones, lets monthly usage counters roll over. |
   | `IntakeTeardownAgentHostedService` | (see file) | Catalog + Inventory | Agent #1: turns received donor cars into draft torn-down parts automatically. |
   | `PricingAgentHostedService` | 30 s | Catalog + Inventory + Reporting | Agent #2: prices and activates draft parts (`PricingStatus = "Manual"` → priced + active) in per-used-car batches via `PartAutoPricingService`. |
   | `MarketingAgentHostedService` | 30 s | Catalog + Inventory + Reporting | Agent #3: for every part the Pricing Agent just activated, matches it against open part requests / need-board ads via `DemandMatchingService` and sends WhatsApp "available now" messages to every match. See §9 for the Round-2 N+1 fix applied here. |
   | `DeadStockMarkdownAgentHostedService` | (see file) | Catalog + Inventory + Reporting | Automatically discounts parts that have gone dormant past a threshold. |
   | `BuyingAdvisorAgentHostedService` | (see file) | Catalog + Inventory + Reporting | Surfaces auto-generated buy/restock recommendations. |
   | `ArCollectionsAgentHostedService` | (see file) | Catalog + Inventory + Reporting | Automates accounts-receivable collection follow-ups. |
   | `OwnerCockpitDigestAgentHostedService` | (see file) | Catalog + Inventory + Reporting | Builds the owner's periodic business digest. |

   All eleven are true `BackgroundService`s registered via `services.AddHostedService<T>()`, run inside
   the same process as the web host, and share its lifetime — there is no separate worker process or
   job scheduler.

---

## 7. Middleware Pipeline (exact order)

`SparePartsApiComposition.UseSparePartsApiPipeline(WebApplication app)`:

1. `RunMigrations(factory)` — runs synchronously *before* the middleware pipeline is even built, so a
   fresh/updated schema is guaranteed before the first request is served.
2. `SecurityHeadersMiddleware` — sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
   `Referrer-Policy: no-referrer`, `Content-Security-Policy: default-src 'none'; frame-ancestors
   'none'` on every response, plus `Strict-Transport-Security` (1 year, includeSubDomains) when the
   request was HTTPS. Registered first (outermost) so headers are attached via `Response.OnStarting`
   even if a later middleware throws.
3. `ApiExceptionMiddleware` — global try/catch; see §2 step 6.
4. `UseCors()` — applies the default CORS policy built in `AddSparePartsApiCore` (see §8).
5. `UseRateLimiter()` — enforces the `auth-login` fixed-window policy (10 requests/minute per client IP)
   used by the login endpoint; on rejection returns 429 with a plain-text body.
6. `UseAuthentication()` — JWT bearer validation (issuer/audience/lifetime/signing-key all validated,
   zero clock skew).
7. `WebAppUserRestrictionMiddleware` — for any authenticated caller whose JWT `roleId` claim equals the
   Web App User role, restricts them to an allow-list of path prefixes (`/api/auth/me`,
   `/api/auth/external-login`, `/api/web-catalog`, `/hubs/notifications`) and returns 403 (JSON
   `ApiErrorEnvelope`) for anything else — this is what keeps the public storefront/web-app login role
   from reaching internal shop-management endpoints even though it holds a valid JWT. OPTIONS requests
   are always passed through unconditionally (needed for CORS preflight).
8. `TenantResolutionMiddleware` — for authenticated, non-super-admin callers, validates the `tenant_id`
   JWT claim is present/well-formed and the tenant is active (`TenantsService.ExistsAndActive`),
   populating the request-scoped `TenantContext` (`TenantId`, `TenantCode`, `IsSuperAdmin`,
   `IsResolved`) that every tenant-scoped service reads via `ITenantContext`. Anonymous requests skip
   tenant resolution entirely (login/health). Super-admins with no tenant claim get `TenantId = 0`
   (the "no filter" sentinel, see §8).
9. `UseAuthorization()` — evaluates `[Authorize]`/policy attributes against the now-fully-populated
   claims principal.
10. `MapHub<NotificationsHub>(NotificationsHubPath)` — mounts the SignalR hub at
    `/hubs/notifications`.
11. `MapControllers()` — standard MVC controller routing, filtered per-host by
    `CapabilityControllerFeatureProvider` (see §3).

---

## 8. Auth, Tenancy, and Security Building Blocks

- **JWT issuance**: `AuthService.Login`/`ExternalLoginAsync` (`src/SpareParts.Api/Services/AuthService.cs`)
  verify credentials (BCrypt password hash, or Google/Facebook token verification for external login),
  then mint a `JwtSecurityToken` with claims: `sub`/`NameIdentifier` (user id), `name`, `roleId`
  (`AuthorizationPolicies.RoleIdClaimType`), `username`, `tenant_id`/`tenant_code`
  (`TenantResolutionMiddleware` claim types), and a unique `jti`. Signing uses HMAC-SHA256 with a
  secret resolved from `Jwt:Secret` — `ResolveJwtSettings` refuses to start the app if that secret is
  missing, is one of several known placeholder values, or is under 32 characters, so a weak/default JWT
  secret cannot silently ship to production.
- **Role-based policies** (`AuthorizationPolicies.cs`): four claim-based policies —
  `Admin`, `AdminOrManager`, `WebAppUser`, `SuperAdmin` — each implemented as
  `RequireAssertion(ctx => HasAnyRoleId(ctx.User, ...))` against the `roleId` claim, not ASP.NET's
  built-in `[Authorize(Roles=...)]`. `HasAnyRoleId`/`GetRoleId` are also called directly by middleware
  (`WebAppUserRestrictionMiddleware`) and `SparePartsControllerBase` for non-attribute-based checks.
- **Tenancy**: every tenant-owned table has a `TenantId` column (added by `TenantIdMigration`). The
  convention `(@TenantId = 0 OR Table.TenantId = @TenantId)` means tenant id `0` is a deliberate
  "bypass filter" sentinel reachable only by an authenticated Super Admin whose JWT carries no
  `tenant_id` claim (see `TenantResolutionMiddleware`) — regular tenant users always have a concrete,
  validated, active `TenantId` and can never see another tenant's rows.
- **Global `[Authorize]` enforcement**: every controller carries `[Authorize]` except the small,
  intentionally-anonymous surface (login, health checks, and the public web-catalog/storefront
  browsing endpoints used by the unauthenticated marketing site) — this is one of the things
  `tests/SpareParts.ArchitectureTests` verifies automatically (out of scope for this document, owned by
  dev-qa-security, but worth knowing the enforcement is test-guarded, not just convention).
- **CORS**: `AddSparePartsApiCore` builds a single default policy — an explicit origin allow-list from
  `Cors:AllowedOrigins` config when present; otherwise `AllowAnyOrigin` only in `Development`; otherwise
  (production, no configured list) a hard-coded `http://localhost:5000` fallback. There is no wildcard
  CORS in production once `Cors:AllowedOrigins` is configured.
- **Production-safe error handling**: see §2 step 6 — `ApiExceptionMiddleware` never returns
  `ex.ToString()`/stack traces to the client; the full exception (including stack trace) is only
  persisted server-side via `IExceptionLogWriter` → `SqlExceptionLogWriter`.

---

## 9. File-by-File Map of Major Services

This section is not exhaustive (there are 200+ files across both projects) — it covers the pieces a new
engineer is most likely to need to find quickly. Every class below lives in its own file, one type per
file (enforced repo-wide as of this round's cleanup — no nested classes remain in either project).

### `src/SpareParts.Api/Hosting/`
- `SparePartsApiComposition.cs` — the composition root; see §3, §5, §7.
- `ServiceCapability.cs`, `ServiceProfile.cs` — the capability enum and the profile record used for
  diagnostics/expected-profile assertions.
- `CapabilityControllerFeatureProvider.cs` — MVC `IApplicationFeatureProvider` that filters discovered
  controllers down to a capability's allow-list.
- `JwtSettings.cs` — POCO for `Jwt:*` config (`Secret`, `Issuer`, `Audience`, `ExpiryHours`).

### `src/SpareParts.Api/Middleware/`
- `SecurityHeadersMiddleware.cs`, `TenantResolutionMiddleware.cs`, `WebAppUserRestrictionMiddleware.cs`
  — see §7.

### `src/SpareParts.Api/Errors/`
- `ApiExceptionMiddleware.cs` — global exception → HTTP response translation (§2, §8).
- `ApiErrorEnvelope.cs` — the JSON error shape returned to clients (`code`, `message`, `traceId`, plus
  plan-lock-specific fields when applicable).

### `src/SpareParts.Api/Infrastructure/`
- `AuthorizationPolicies.cs` — role-id claim policies (§8).
- `*Migration.cs` (~85 files) — see §5.

### `src/SpareParts.Api/Notifications/`
- `NotificationsHub.cs`, `NotificationEvents.cs`, `PartAddedNotification.cs`,
  `PartReservationReminderNotification.cs` — SignalR surface (§6).
- `*AgentHostedService.cs`, `*HostedService.cs` — the eleven background agents (§6).

### `src/SpareParts.Api/Services/`
- `AuthService.cs` — login, external login (Google/Facebook), JWT minting (§8).
- `ExternalAuthSettings.cs` — Google/Facebook client id/secret config POCO.
- Supporting external-login DTOs now live in their own files: `ExternalProfile.cs`,
  `GoogleTokenInfo.cs`, `FacebookDebugTokenResponse.cs`, `FacebookDebugTokenData.cs`,
  `FacebookProfileResponse.cs`.

### `src/SpareParts.Api/Controllers/`
- One controller per resource/feature (`PartsController`, `SalesController`, `PurchasesController`,
  `AccountingController`, `UsedCarsController`, `SubscriptionController`, `PaymentsController`,
  `PricingController`, `AdminPricingController`/`AdminSubscriptionsController`/
  `AdminPaymentsController`/`AdminInvoicesController` (super-admin billing views), plus ~65 more
  feature-specific controllers covering the "AI agent" surfaces (`PriceGeniusController`,
  `KareemController`, `DemandMatching`-backed `NeedBoardController`, etc). `SparePartsControllerBase.cs`
  is the shared base (§2). `UserRow.cs` is a small internal DTO used by `AuthController`/`UsersController`.

### `src/SpareParts.Infrastructure/Data/`
- `SqlConnectionFactory.cs`, `DbSession.cs` — connection/transaction plumbing (§4).
- `AccountingDapperBootstrap.cs` — registers custom Dapper type handlers at startup (see
  `AccountTypeTypeHandler.cs` for the enum ↔ SQL mapping it wires up).
- `AccountingCurrencyContext.cs` / `AccountingCurrencyContextResolver.cs` — resolves
  base/counter-currency + exchange rate context shared by accounting-adjacent services (used by
  `UsedCarPartPricingAllocator`, sales/purchase currency handling, etc.).
- `AccountingSchemaInspector.cs`, `AccountingSql.cs` — schema introspection + shared accounting SQL
  fragments.
- `SqlExceptionLogWriter.cs` — writes `ExceptionLogEntry` rows for `ApiExceptionMiddleware` (§2, §8).
- `TransactionTimelineReader.cs` / `TransactionTimelineSnapshot.cs` — builds a unified activity timeline
  across transaction types for a given entity.
- `Repositories/` — see §4 (`RepositoryCatalog.cs` + the five `*Repositories.cs` bundles + their
  per-entity repository classes under `Accounting/`, `Communications/`, `Inventory/`, `MasterData/`,
  `Purchases/`, `Sales/`).

### `src/SpareParts.Infrastructure/Services/` (selected)
- **Sales/Purchases pipeline**: `CreateSaleHandler.cs`, `CreateSalesReturnHandler.cs`,
  `CreatePurchaseHandler.cs`, `CreateUsedCarPurchaseHandler.cs` — orchestrate a full
  sale/return/purchase transaction (inventory adjustment + accounting journal entry + invoice
  numbering) via `RepositoryCatalog`. `SalesService.cs`, `SalesReturnsService.cs`, `PurchaseService.cs`
  wrap these handlers for controller consumption.
- **Accounting engine**: `AccountingService.cs`, `AccountingSettingsProvider.cs`,
  `AccountingJournalLineFactory.cs`, `AccountingJournalDescriptionFormatter.cs`,
  `SaleAccountingStrategy.cs` / `ReturnAccountingStrategy.cs` / `PurchaseAccountingStrategy.cs`
  (implement a shared `IAccountingStrategy<T>` per transaction type, registered per-capability in
  `SparePartsApiComposition`), `CustomerAccountResolver.cs` / `SupplierAccountResolver.cs` (resolve the
  GL account a given customer/supplier posts to).
- **Inventory**: `InventoryService.cs`, `PartsService.cs` (catalog CRUD, dead-stock report, listing
  package generation, used-car part stock bootstrap), `PartReservationService.cs`,
  `PartRequestsService.cs` (customer part requests + stock reservation clock — see the reservation
  lookup/target/stock/line row types extracted to their own `PartRequest*.cs` files this round),
  `ReorderAnalysisService.cs`, `UsedCarPartPricingAllocator.cs` (proportional cost allocation across a
  used car's linked parts, using `UsedVehiclePartPricingEngine` from `SpareParts.Domain`).
- **Pricing/Billing** (`Services/Pricing/`): `PricingPackageService.cs`, `SubscriptionService.cs` +
  `SubscriptionLimitService.cs` (plan feature/limit enforcement — throws `PlanLockException`, mapped to
  HTTP 403 `plan_lock` by `ApiExceptionMiddleware`), `PaymentService.cs` + `PaymentProviderFactory.cs` +
  `IPaymentProvider` implementations (`ManualPaymentProvider.cs`, `TestPaymentProvider.cs`,
  `StripePaymentProvider.cs`), `InvoiceService.cs`, `PaymentSettings.cs` (+ its now-separate
  `PaymentProvidersSettings.cs`/`TestProviderSettings.cs`/`ManualProviderSettings.cs`/
  `StripeProviderSettings.cs` files), `SubscriptionStatusCalculator.cs`.
- **The four automated "agents'" services** (driven by the hosted services in §6):
  `PartAutoPricingService.cs` (Pricing Agent), `DemandMatchingService.cs` (Marketing Agent — see §10 for
  this round's fixes), `DeadStockMarkdownService.cs` (Dead Stock Markdown Agent),
  `BuyingAdvisorService.cs` (Buying Advisor Agent), `ArCollectionsService.cs` (AR Collections Agent),
  `OwnerCockpitDigestService.cs` (Owner Cockpit Digest Agent).
- **Catalog/Used cars**: `UsedCarsService.cs` (the largest service in the codebase — used-car lifecycle,
  wholesale sales, teardown; its five nested row/context types were extracted to their own files this
  round: `UsedCarPartInventoryValueRow.cs`, `UsedCarWholesaleSaleCurrencyContext.cs`,
  `UsedCarWholesaleSaleRecord.cs`, `UsedCarWholesaleLookup.cs`, `UsedCarWholesaleCustomerLookup.cs`),
  `UsedCarImagesService.cs`, `UsedCarTwinService.cs`, `CarCrushService.cs`, `PartCompatibilityService.cs`.
- **AI/assistant surfaces**: `VisualPartSearchService.cs` (photo-to-part search via OpenAI vision,
  its `VisualRecognitionResult.cs`/`VisualRecognitionPayload.cs`/`VisualPartCandidate.cs` types now
  extracted), `PartNotesAiService.cs`, `BusinessAssistantService.cs`, `KareemConciergeService.cs`,
  `SymptomSearchService.cs` (static rulebook diagnosis, its `SymptomRule.cs` record extracted),
  `SmartSearchService.cs`.
- **Reporting**: `ReportBuilderService.cs` (+ `ReportBuilderService.Advanced.cs` and
  `ReportBuilderService.Currency.cs` partial-class split for advanced formulas / currency projection),
  `ReportBuilderFormulaSqlCompiler.cs`, `OwnerCockpitService.cs`, `GrowthIntelligenceService.cs` (donor
  car treasure/teardown-queue/buying-radar/voice-quote intelligence — its six SQL row DTOs extracted to
  their own files this round: `DonorCarRow.cs`, `DonorPartRow.cs`, `DuplicateCandidateRow.cs`,
  `BuyingRadarRow.cs`, `AuctionHistoryRow.cs`, `QuoteCandidateRow.cs`).
- **Communications** (`Services/Communications/`): `CommunicationsService.cs` (WhatsApp/message send
  orchestration), `WhatsAppCampaignService.cs`, `ICommunicationDeliveryClient.cs` +
  `WebhookCommunicationDeliveryClient.cs` / `DisabledCommunicationDeliveryClient.cs` (delivery is a
  swappable strategy — disabled by default unless `Communications:*` webhook config is present).
- **Cross-cutting exception types** (each now its own file, thrown by services and mapped by
  `ApiExceptionMiddleware`): `ValidationException.cs`, `NotFoundException.cs`, `ConflictException.cs`,
  `PlanLockException.cs`, `ExternalServiceException.cs`, `DomainException.cs` (base type for the
  domain-specific ones).
- **Tenant context**: `TenantContext.cs` (the `ITenantContext` implementation, request-scoped, populated
  by `TenantResolutionMiddleware`; exposes a `TenantContext.Legacy` static instance for any code path
  that predates tenancy and needs an unfiltered/tenant-0 context).

---

## 10. Round 2 Fixes Applied to This Codebase (context for future readers)

Two behavioral fixes were made to `DemandMatchingService`/`MarketingAgentHostedService` alongside this
documentation pass, worth knowing about when reading either file:

1. **Batched demand matching** — `MarketingAgentHostedService.RunOnceAsync` used to call
   `DemandMatchingService.FindMatches(part.Name)` once per pending part (2 extra SQL round-trips per
   part per 30-second tick). `DemandMatchingService` now exposes
   `FindMatchesForParts(IReadOnlyList<string> partNames)`, which issues exactly one query per source
   (`PartRequests`, `PartWantedAds`) covering every pending part name in that tick, keyed back into a
   per-part-name dictionary in application code. `FindMatches(string)` is kept as a single-name
   convenience wrapper over the batched method for any other caller.
2. **LIKE wildcard escaping** — part names matched against `PartRequests.RequestedPartName` /
   `PartWantedAds.PartName` are now escaped (`%`, `_`, `[`, and the escape character itself) before
   being substituted into a `LIKE '%' + @param + '%' ESCAPE '\'` pattern, so a part name containing a
   literal `%` or `_` can no longer widen the match pattern unexpectedly.

All 16 pre-existing "one type per file" violations flagged in Round 1 (plus one additional file found
during this round's audit, `VisualPartSearchService.cs`) were fixed by extracting every nested
class/record to its own file, matching the convention already used for `PartAutoPricingService.cs`'s
`CarCostRow` in Round 1. Where two different files had nested types with the same name but different
shapes (e.g. two different `PaymentRow`/`PackageRow` projections in `Services/Pricing/`), the extracted
types were given distinct, more specific names to avoid ambiguity now that they're visible
file-/namespace-wide instead of being invisibly scoped to their old containing class.
