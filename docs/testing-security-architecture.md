# SpareParts — Testing & Security Architecture Reference

This document is a complete reference to SpareParts' automated test suites and API security
posture. It is written so a new engineer can onboard from this file alone: what each test
project verifies, which architecture rules are enforced and why, the full security posture of
the API, and how to run every test project locally.

Last verified: Round 2 of the whole-solution QA/security audit (all 103 ArchitectureTests and
70 ManagementTests passing).

---

## 1. Test project map

```
tests/
  SpareParts.ArchitectureTests/   <- layer rules, critical business-logic paths, security guards,
                                      API composition/contract consistency, quality guardrails
  SpareParts.ManagementTests/     <- WPF desktop layer rules, ManagementCoordinator CRUD wiring,
                                      pricing/valuation engines, WPF surface smoke tests
  SpareParts.IntegrationTests/    <- real SQL Server behavior via Testcontainers (sale creation,
                                      stock/journal consistency, concurrency, rollback)
```

All three are xUnit projects targeting net8.0 (ArchitectureTests and ManagementTests target
`net8.0-windows` because they reference the WPF desktop assemblies).

---

## 2. `SpareParts.ArchitectureTests`

This is the largest and most important suite — 103 tests across 11 files. It is the primary
guard against both architectural drift (layers, controller composition) and security
regressions (auth, headers, tenant isolation).

### 2.1 `LayerDependencyTests.cs` — clean architecture layering

Verifies the dependency direction between the four backend layers never inverts, by reflecting
over each layer's compiled assembly and asserting it does **not** reference assemblies from
layers "outside" it:

| Layer (assembly) | Must not reference |
|---|---|
| `SpareParts.Domain` | Infrastructure, Api, Desktop |
| `SpareParts.Infrastructure.Interfaces` | Infrastructure, Api, Desktop |
| `SpareParts.Application` | Infrastructure, Api, Desktop |
| `SpareParts.Infrastructure` | Api, Desktop |
| `SpareParts.Api` | Desktop |

This enforces the intended dependency direction: `Domain <- Application/Infrastructure.Interfaces
<- Infrastructure <- Api`, with `Desktop` (WPF) consuming the API only over HTTP, never linking
against API/Infrastructure assemblies directly.

### 2.2 `ArchitectureTests.cs` — handler placement

Two small guards specific to the Handler pattern used for multi-step domain operations
(`CreateSaleHandler`, `CreatePurchaseHandler`, etc.):
- `Domain_ShouldNotReferenceInfrastructure` — the domain assembly must not reference Infrastructure.
- `Handlers_ShouldStayInInfrastructure` — handler types must live under the
  `SpareParts.Infrastructure` namespace (not leak into Api or Domain).

### 2.3 `CriticalPathTests.cs` — 24 tests covering core business logic

Exercises the money- and stock-critical code paths with fast, in-memory/fake-repository tests
(no real database except where SQLite in-memory is used for accounting strategy tests):

- **Invoice totals**: `InvoiceTotalsCalculator` subtotal/discount/tax/total math.
- **Stock movements**: `InventoryService.AdjustStock` — writes stock + movement rows, rejects
  negative resulting quantity (`ConflictException`), correctly separates reserved vs. available
  quantity, and does not let a `Sale` movement silently consume an *anonymous* reservation hold.
- **Reservation clock**: default expiry defaults to "tomorrow 6pm local time" converted to UTC;
  `PartReservationExpirationAction.Normalize` maps free-text values to known enum actions.
- **Transfers**: stock transfer between warehouses produces both a `TransferOut` and
  `TransferIn` movement and moves quantity correctly.
- **Accounting/journal posting**: `SaleAccountingStrategy.BuildJournalLines` always balances
  (debit == credit), throws `InvalidOperationException` for negative invoice totals, and treats
  sub-cent rounding differences as balanced (rounds to 4 decimal places before comparing).
- **Owner Cockpit P&L**: daily profit/loss calculator correctly subtracts rent + labor from
  gross profit, and classifies expense accounts into Rent/Labor/Other categories from account
  code/name/description heuristics.
- **Payment status policy**: `DefaultPaymentStatusPolicy` returns `PartiallyPaid` when paid <
  total; used-car purchase payment status correctly prefers the counter-payment amount when
  present over the raw paid amount.
- **Concurrency safety**: 40 parallel workers doing 10 buy/sell cycles each against a single
  stock row via a `ThreadSafeInventoryRepository` — final quantity and movement count must be
  exact (no lost updates). A second test asserts only one of two concurrent "last unit" sales
  wins, the other gets a `ConflictException`.
  - This is a **unit-level concurrency simulation** using an in-memory fake with per-row
    locking. It proves `InventoryService`'s guard logic is correct; real cross-process
    concurrency (SQL Server row locking / optimistic concurrency) is proven separately by
    `SpareParts.IntegrationTests.CreateSaleSqlServerIntegrationTests.Handle_ConcurrentSingleStock_ShouldAllowOnlyOneSale`.
- **Compensating rollback**: if the movement insert fails after the stock row was already
  written, `InventoryService` must compensate (roll the quantity back) rather than leave stock
  inconsistent with the movement ledger.
- **Duplicate invoice-number contention**: `CreateSaleHandler`/`CreatePurchaseHandler` retry
  unique-number generation up to 5 times, then throw `ConflictException` — verified via
  reflection-invoked private methods against a repository stub that always reports the
  generated number as already taken.
- **Error envelope contract round-trip**: `ApiExceptionMiddleware` serializes a thrown
  `ConflictException` into an `ApiErrorEnvelope` JSON body (`code`, `message`, `traceId`), and
  the WPF desktop's `ApiClientBase.EnsureSuccessAsync` (invoked via reflection since it's
  internal) must deserialize that exact envelope back into an `ApiClientException` with matching
  `Code`/`Message`/`TraceId`. This is the one test that proves the API's error contract and the
  desktop client's error parsing haven't drifted apart.
- **Scan payload normalization** and **visual search token extraction** — small parsing/heuristic
  unit tests for the barcode/AI-photo-search features.

### 2.4 `DomainValidationTests.cs` — 13 tests, DataAnnotations + domain constants

Validates that `[Required]`/attribute-based validation on request DTOs
(`CreatePartRequest`, `CreateCustomerRequest`, `CreateSupplierRequest`, `CreateBrandRequest`,
`CreateCategoryRequest`, `CreateUserRequest`) actually fires for missing required fields, that
defaults are correct (`PartDefaults.Currency == "USD"`, default `PricingStatus == Manual`), and
that the `SalesProfitHistoryBuilder` correctly buckets/aggregates profit history by month
(including correctly excluding out-of-range historical data).

### 2.5 `SecurityAndDataIntegrityTests.cs` — 14 tests, the primary security-focused file

This is where most of round 1's security work lives, plus the round-1 "new authorization guard
test":

- **`EveryApiController_ShouldRequireAuthorization_UnlessExplicitlyAllowListed`** (the
  round-1-added guard test). Reflects over every concrete `*Controller` type in the API
  assembly. For each one:
  - If it's on the `AnonymousControllerAllowList`, skip it.
  - Else if it has a class-level `[Authorize]`, it's fine.
  - Else every public action method must individually carry `[Authorize]`.
  - Any controller/action that satisfies none of the above is reported as a violation and fails
    the test with the full list of offending controller/action names.

  The allow-list is intentionally tiny and each entry is commented with *why* it's safe to be
  anonymous:
  ```csharp
  private static readonly HashSet<string> AnonymousControllerAllowList = new(StringComparer.Ordinal)
  {
      nameof(HealthController),   // read-only service/DB status, no sensitive data after the Error-message fix
      nameof(AuthController),     // must be reachable pre-login; sensitive actions are individually [Authorize]d
      nameof(PricingController)   // public plan/pricing catalog, no tenant data
  };
  ```
  This is a durable regression guard: any future controller added to the API without
  `[Authorize]` (accidentally or otherwise) fails the build immediately, unless a maintainer
  deliberately adds it to the allow-list with a justification comment.

- **`ExcelImportController_ShouldRequireAdminPolicy`** / **`ExcelImportService_ShouldRejectSensitiveTablesAndAuditColumns`**
  — bulk Excel import is Admin-only, and the service itself has a hard-coded denylist rejecting
  `dbo.Users` and audit columns like `CreatedByUserId` even if a caller somehow bypassed the
  controller-level policy (defense in depth).

- **`ReportBuilderController_ShouldRequireAdminOrManagerPolicy`** and
  **`ReportBuilderLinkMutationEndpoints_ShouldRequireAdminPolicy`** (`SaveLink`/`DeleteLink`) —
  the ad-hoc report builder is read-accessible to Admin/Manager but structural changes
  (table-relationship links) require full Admin. **`ReportBuilderService_ShouldRejectSensitiveTables`**
  and **`ReportBuilderService_ShouldRejectManagerLinkMutationsBeforeOpeningConnection`** prove
  the service layer independently blocks `dbo.Users` and its own saved-report table from being
  queried/joined, and rejects a Manager-level link mutation attempt *before* even opening a DB
  connection (fail fast, no wasted round-trip).

- **`ApiComposition_ShouldRejectKnownJwtPlaceholderSecrets`** — `SparePartsApiComposition.ResolveJwtSettings`
  throws `InvalidOperationException` at startup if `Jwt:Secret` is still the shipped placeholder
  value (`CHANGE_ME_USE_ENV_OR_USER_SECRETS...` or the old sample secret), preventing an
  accidental deploy with a known/guessable signing key. (`ResolveJwtSettings` also separately
  enforces a 32-character minimum length — see §3.3.)

- **`AuthService_ExternalLogin_ShouldNotReactivateDisabledUser`** — a disabled user
  (`IsActive = 0`) who successfully re-authenticates via an external IdP (Google, in the test)
  must still be rejected with `UnauthorizedAccessException("This account is disabled.")`, and
  the `IsActive` flag must remain `0` afterward — external login cannot be used to silently
  reactivate a disabled account.

- **`AccountingMutationEndpoints_ShouldRequireAdminOrManagerRoleIdPolicy`** (11 inline cases) —
  every accounting-mutating endpoint (account CRUD, account-type CRUD, posting-role CRUD,
  posting-settings update, manual journal creation) requires the `AdminOrManager` policy.

- **`UsersService_Update_ShouldThrowNotFound_WhenUserDoesNotExist`** /
  **`UsersService_Deactivate_ShouldThrowNotFound_WhenUserDoesNotExist`** — operating on a
  non-existent user ID must 404, not silently no-op or leak existence via a different error
  shape.

- **`InventoryService_AtomicDecrement_ShouldAllowOnlyOneWinner_WhenStockHitsZero`** — two
  concurrent sales for the last unit of stock: exactly one succeeds, one gets `ConflictException`,
  final quantity is exactly 0 (no oversell).

- **`SaleAccountingStrategy_ShouldTreatRoundedTotalsAsBalanced`** — journal lines built from
  invoice totals with more precision than the ledger rounds to must still balance after rounding.

### 2.6 `SecurityHeadersMiddlewareTests.cs` — 4 tests, response security headers

Verifies `SecurityHeadersMiddleware.ApplyHeaders` (round 1's header hardening):

| Header | Value | Condition |
|---|---|---|
| `X-Content-Type-Options` | `nosniff` | always |
| `X-Frame-Options` | `DENY` | always |
| `Referrer-Policy` | `no-referrer` | always |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` | always (appropriate for a pure JSON API — no inline scripts/styles/frames are ever served by the API itself) |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | **only** when `context.Request.IsHttps == true` |

The middleware registers header-setting via `context.Response.OnStarting` (so headers are still
applied even if a later middleware/controller writes the response directly), and a fourth test
confirms `InvokeAsync` always calls `next` (never short-circuits the pipeline).

Registered early in the pipeline in `SparePartsApiComposition.UseSparePartsApiPipeline`, before
`ApiExceptionMiddleware`, so headers are present even on error responses.

### 2.7 `TenantIsolationTests.cs` — 7 tests, multi-tenant data isolation

- **`TenantsController_ShouldRequireSuperAdminPolicy`** — tenant management is
  `SuperAdmin`-only (role ID 5), the highest privilege tier, above ordinary `Admin`.
- **`TenantContext_Legacy_ShouldHaveTenantIdZero`** — the "legacy"/pre-multi-tenant fallback
  context has `TenantId == 0`, `IsResolved == true`, `IsSuperAdmin == false` — used by handlers
  in tests and by any single-tenant deployment path.
- **`UsersService_GetAll_ShouldFilterByTenantId`** / **`UsersService_Deactivate_ShouldNotAffectOtherTenants`**
  — seed two tenants' users into the same physical table, prove tenant 1's service instance only
  ever sees/returns tenant 1's rows, and that tenant 1 attempting to deactivate tenant 2's user
  ID gets `NotFoundException` (not silently a no-op, and not a cross-tenant success) — this is
  the concrete regression test for row-level tenant isolation working end-to-end through the
  service layer (not just "the column exists").
- **`TenantsService_ExistsAndActive_*`** — the tenant-existence/active check used by
  `TenantResolutionMiddleware` (see §3.2) correctly returns false for an unknown tenant ID and
  true for the seeded default tenant.

### 2.8 `ApiCompositionTests.cs` — 6 theory/fact tests (5 theory cases), controller-to-capability wiring

SpareParts is split into deployable "capabilities" (`Sales`, `Purchases`, `Inventory`,
`Accounting`, `Identity`, `Catalog`, `Reporting`, `Health`, `Billing`) so the same codebase can
run either as one monolith (`SpareParts.Api`, all capabilities) or as smaller per-capability
services (`SpareParts.Sales.Api`, `SpareParts.Inventory.Api`, etc. — see
`SparePartsApiComposition.ExpectedServiceProfiles`). `ApiCompositionTests` proves the
capability -> controller wiring in `SparePartsApiComposition.ControllerMap` is exactly right:

- **`AddCapabilityControllers_ShouldExposeOnlyExpectedControllers`** (theory, one case per
  capability) — spins up a real `ApplicationPartManager`/`ControllerFeatureProvider` pipeline
  with only that capability (+`Health`) enabled, and asserts the exact set of controller names
  the MVC framework would expose matches the expected list for that capability. This is what
  guarantees, e.g., that a `SpareParts.Purchases.Api` deployment cannot accidentally expose
  `UsersController` or `AccountsController` — only the controllers wired to `Purchases` and
  `Health` are reachable.
  - **Round 2 fix**: these expected lists had drifted badly stale (see §5) — they were last
    updated when the API had ~40 controllers; the codebase now has 91. The lists are now kept in
    exact sync with `SparePartsApiComposition.ControllerMap` (the production source of truth).
- **`AddCapabilityControllers_WithAllCapabilities_ShouldCoverEveryConcreteApiController`** — with
  *every* capability enabled, the resulting controller set must equal literally every
  `*Controller` class in the `SpareParts.Api` assembly. This is the completeness check: it
  proves `ControllerMap` has no orphaned/forgotten controller that isn't reachable under any
  capability combination. (This test's passing is what confirms `ControllerMap` itself, not just
  the test's copy of it, is internally correct — see the Round 2 investigation notes in §5.)
- **`AddCapabilities_ShouldRegisterSharedInvoiceServicesOnceWhenSalesAndPurchasesAreEnabled`** —
  when both `Sales` and `Purchases` are enabled (as in the monolith), shared invoice
  infrastructure (`IInvoiceNumberGenerator`, `IPaymentStatusPolicy`, `IInvoiceTotalsCalculator`,
  `IInventoryService`) is registered exactly once each (not once per capability), using
  `TryAddScoped`/`TryAddSingleton` internally.
- **`AddCapabilities_WhenInventoryEnabled_ShouldRegisterAiBackedPartsServices`** /
  **`AddCapabilities_WhenReportingEnabled_ShouldRegisterCommunicationsServices`** — spot-check
  that capability-gated DI registrations (AI part-notes/visual-search services under
  `Inventory`; communications/report-builder/growth-intelligence services under `Reporting`)
  are actually wired up.

### 2.9 `QualityGuardrailTests.cs` — 5 tests, cross-platform feature-parity guardrails

These tests statically parse the web (`screen-registry.js`, `config.js`), mobile
(`screen-registry.js`, `app-config.js`), and desktop (`AppScreen.cs` enum +
ViewModel/Window marker files) source files with regex, and cross-check them against each
other and against the real backend controller routes. They exist to catch feature-parity drift
between the three client platforms and stale/incorrect API-contract documentation:

- **`Feature_lists_should_not_drift_between_web_mobile_and_desktop`** — every screen key present
  on web must be present on mobile and on desktop (as a marker in `AppScreen.cs` + optional
  file-existence markers for desktop screens that aren't simple 1:1 enum-to-key mappings, e.g.
  `contacts`, `management`, `settings`), and vice versa. Also cross-checks the `featureModules`
  contract arrays (web `config.js` vs mobile `app-config.js`) for endpoint/label/title parity and
  that every mobile module has a non-empty `capabilities` list.
  - **Round 2 fix**: this test's static `CanonicalScreens` list and `DesktopScreenMap`
    dictionary were badly stale (34 screens tracked vs. 74 real screens); see §5. A second,
    separate parsing bug (`ParseFeatureModules` using a naive non-greedy `{...}` regex) was also
    found and fixed — see §5.3.
- **`Shared_mobile_and_web_screen_labels_titles_and_themes_should_match`** — where web and
  mobile share a screen/module key, the human-readable label/title text must be character-for-
  character identical (no "Parts" on one platform and "Inventory" on the other for the same
  key), and the WPF theme catalog (`wpfThemes` in `config.js`/`app-config.js`) must match
  key-for-key between web and mobile.
- **`Api_contract_smoke_tests_should_cover_every_screen_endpoint`** — every `endpoint:` value
  declared in web/mobile `featureModules` must resolve to a real controller route (route
  templates are read via reflection from every controller's `[Route]`/`[Http*]` attributes,
  parameter placeholders like `{id:int}` are treated as wildcards). This is a compile-time-ish
  smoke test that a frontend "this screen calls this API" contract string isn't simply wrong —
  it does **not** call the endpoint over HTTP, it only proves the string matches a route that
  exists.
  - **Round 2 fix**: found and fixed a genuine regex bug in `RouteMatchesEndpoint` (route
    templates with a type constraint like `{id:int}` were never being wildcarded, causing false
    "missing route" reports), plus 6 real frontend-config bugs where the declared `endpoint`
    string didn't match any real backend route (see §5.4 — two of these, `mechanic-desk` and
    `halfcut` on mobile, were live production bugs, not just documentation drift).
- **`Lightweight_visual_navigation_smoke_tests_should_cover_web_and_mobile_navigation`** — the
  web/mobile navigation-group definitions (`layout.js` groups / `app-config.js` `keys` groups)
  must have no duplicate items, no group pointing at a screen key that doesn't exist, and no
  screen that exists but isn't reachable from any navigation group (an "orphan screen").
- **`Health_dashboard_should_cover_split_apis_database_migrations_clients_and_notifications`** —
  builds a real `HealthController` against an in-memory SQLite factory and asserts the returned
  `HealthDashboardDto` correctly reports service/capability info, lists every split-API profile
  from `SparePartsApiComposition.ExpectedServiceProfiles`, database connectivity, the exact
  migration count (`SparePartsApiComposition.MigrationNames.Count`), client config (web default
  API base URL, CORS allowed origins), and notifications config (SignalR hub path + registration,
  webhook-configured flag).

---

## 3. Security posture of the API

### 3.1 Authentication (JWT)

- Scheme: `JwtBearerDefaults.AuthenticationScheme`, configured in
  `SparePartsApiComposition.AddSparePartsApiCore`.
- Validation: issuer, audience, lifetime, and signing key are **all** validated
  (`ValidateIssuerSigningKey/Issuer/Audience/Lifetime = true`), `ClockSkew = TimeSpan.Zero` (no
  grace period on expiry).
- Signing key resolution (`ResolveJwtSettings`, `SparePartsApiComposition.cs`):
  - Throws at startup if `Jwt:Secret` is missing.
  - Throws if the secret is a known placeholder (`CHANGE_ME...`, the old sample secret, or
    contains `USE_ENV_OR_USER_SECRETS`) — see `IsPlaceholderJwtSecret`.
  - Throws if the secret is shorter than 32 characters.
  - Covered by `SecurityAndDataIntegrityTests.ApiComposition_ShouldRejectKnownJwtPlaceholderSecrets`.
- SignalR token passthrough: the notifications hub (`/hubs/notifications`) accepts the JWT via
  an `access_token` query-string parameter (standard SignalR pattern, since browsers can't set
  `Authorization` headers on WebSocket upgrade requests) — this is scoped specifically to that
  path only (`OnMessageReceived` checks `path.StartsWithSegments("/hubs/notifications")`), so a
  token in a query string elsewhere in the API is not accepted as a bearer token.

### 3.2 Authorization

- **Global default**: `app.UseAuthorization()` is registered in the pipeline; there is no global
  `[AllowAnonymous]` fallback — controllers/actions are locked down unless explicitly opened up.
- **Enforcement guard**: `SecurityAndDataIntegrityTests.EveryApiController_ShouldRequireAuthorization_UnlessExplicitlyAllowListed`
  (§2.5) is the durable regression test ensuring every controller either has `[Authorize]` or is
  on the tiny, justified `AnonymousControllerAllowList` (`HealthController`, `AuthController`,
  `PricingController`).
- **Role-ID policies** (`AuthorizationPolicies.cs`), backed by a `roleId` claim on the JWT:
  - `Admin` (role ID from `UserRole.Admin`)
  - `AdminOrManager` (Admin or Manager)
  - `WebAppUser` (the restricted storefront-only role, ID from `WebAppUserRoleMigration.RoleId`)
  - `SuperAdmin` (role ID 5 — highest tier, used for tenant management)
  - Each policy is a `RequireAssertion` reading the `roleId` claim and checking membership.
- **Web-app user restriction** (`WebAppUserRestrictionMiddleware.cs`): a public storefront
  customer (role `WebAppUser`) is confined to a small allow-list of path prefixes
  (`/api/auth/me`, `/api/auth/external-login`, `/api/web-catalog`, `/hubs/notifications`) —
  any other API call from a web-app-user-role token gets `403 Forbidden` with a clear
  `ApiErrorEnvelope` message, even if the underlying controller's `[Authorize]` policy would
  otherwise let the request through. OPTIONS preflight requests are always passed through
  (`HttpMethods.IsOptions` short-circuit) so CORS preflight isn't blocked.
- **Tenant resolution** (`TenantResolutionMiddleware.cs`, runs after `UseAuthentication`, before
  `UseAuthorization`):
  - Anonymous requests skip tenant resolution entirely (login/health).
  - A `SuperAdmin` token with no `tenant_id` claim gets `TenantContext.IsSuperAdmin = true`,
    `TenantId = 0` (cross-tenant access, by design, only reachable via the `SuperAdmin` policy).
  - Any other authenticated request must carry a valid positive integer `tenant_id` claim, or
    the request is rejected `401 Unauthorized` with `"Tenant claim is missing or invalid."`.
  - The claimed tenant must exist and be active (`TenantsService.ExistsAndActive`), or the
    request is rejected `403 Forbidden` with `"Tenant is not active or not found."` — this stops
    a JWT for a deactivated/deleted tenant from being usable even if it hasn't expired yet.
  - Once resolved, `TenantContext.TenantId`/`TenantCode` are populated for the rest of the
    request pipeline; every tenant-scoped repository/service filters by this value. Verified
    end-to-end by `TenantIsolationTests` (§2.7).

### 3.3 Response security headers

See §2.6 (`SecurityHeadersMiddleware`) for the exact header values. Applied globally, first in
the pipeline (before exception handling), via `context.Response.OnStarting` so they land on
error responses too.

### 3.4 Error handling / no stack-trace leakage (round 1 fix)

`ApiExceptionMiddleware` (`src/SpareParts.Api/Errors/ApiExceptionMiddleware.cs`) is the single
catch-all for unhandled exceptions:
- Maps known exception types to specific HTTP status codes and stable string error codes
  (`ValidationException` -> 400/`validation_error`, `NotFoundException` -> 404/`not_found`,
  `ConflictException` -> 409/`conflict`, `PlanLockException` -> 403/`plan_lock`,
  `ExternalServiceException` -> 502/`upstream_error`, `UnauthorizedAccessException` -> 401/`unauthorized`,
  everything else -> 500/`internal_error`).
- **Client-facing message**: for `internal_error` specifically, the message returned to the
  client is always the generic `"An unexpected server error occurred."` — the real
  `ex.Message`/stack trace is **never** put in the HTTP response body for unmapped/unexpected
  exceptions. For the mapped exception types (validation, not-found, conflict, etc.) the
  exception's own message is returned, because those messages are already written to be
  user-safe domain messages (e.g. `"Cannot reduce stock below zero for part 10 in warehouse 3."`).
  This is the round-1 stack-trace-leak fix.
- Every response carries a `traceId` (`context.TraceIdentifier`) so support/logs can correlate a
  user-reported error to server-side logs without exposing internal detail to the client.
- Full exception detail (type, message, stack trace, source class/method/line extracted via
  `StackTrace`) is still captured server-side via `IExceptionLogWriter` (persisted to the
  database) — nothing is lost, it's just not sent to the client.
- Contract-verified end-to-end by `CriticalPathTests.ApiToClientErrorEnvelope_Contract_ShouldRoundTripCodeMessageAndTraceId`
  (§2.3), which also proves the WPF desktop client's error parsing understands this exact
  envelope shape.

### 3.5 CORS

Configured in `SparePartsApiComposition.AddSparePartsApiCore`:
- If `Cors:AllowedOrigins` config is set (non-empty array) -> `WithOrigins(...)` restricted to
  that explicit allow-list, any method/header.
- Else if running in Development -> `AllowAnyOrigin()` (convenience for local frontend dev).
- Else (production, no explicit allow-list configured) -> falls back to
  `WithOrigins("http://localhost:5000")` only — i.e. production defaults to a same-origin-only
  posture rather than silently allowing any origin if the config is missing.
- Verified indirectly via `QualityGuardrailTests.Health_dashboard_should_cover_split_apis_database_migrations_clients_and_notifications`,
  which asserts a configured `Cors:AllowedOrigins` entry shows up in the health dashboard's
  `ClientConfig.CorsAllowedOrigins`.

### 3.6 OPTIONS / TRACE handling

- OPTIONS (CORS preflight): explicitly passed through unauthenticated/unrestricted in
  `WebAppUserRestrictionMiddleware` (`HttpMethods.IsOptions` check) so preflight requests are
  never blocked by the web-app-user path restriction; the ASP.NET Core CORS middleware
  (`app.UseCors()`) otherwise handles the OPTIONS preflight response per the configured policy
  in §3.5.
- TRACE: ASP.NET Core's Kestrel/routing pipeline does not route the `TRACE` HTTP method to MVC
  controllers by default (no controller action accepts it), so TRACE requests fall through to a
  404/405 rather than being handled as a valid request — there is no explicit TRACE-echo
  endpoint anywhere in the codebase.

### 3.7 Rate limiting

`AddRateLimiter` registers a fixed-window limiter keyed by remote IP
(`AuthRateLimitPolicy = "auth-login"`, 10 requests/minute, no queueing) applied to the
login endpoint, mitigating credential-stuffing/brute-force attempts against `/api/auth/login`.
Rejected requests get `429 Too Many Requests` with a plain-text explanation.

### 3.8 File upload / import authorization

`ExcelImportController` requires the `Admin` policy specifically (not just `AdminOrManager`),
and `ExcelImportService` independently denylists sensitive tables (`dbo.Users`) and audit
columns (e.g. `CreatedByUserId`) at the service layer — so even if a caller somehow bypassed the
controller policy, the service itself refuses to import into/read audit-sensitive schema.
Covered by `SecurityAndDataIntegrityTests.ExcelImportController_ShouldRequireAdminPolicy` and
`ExcelImportService_ShouldRejectSensitiveTablesAndAuditColumns` (§2.5).

### 3.9 Database name / tenant identifier leakage

Round 1 addressed direct database/tenant-identifier leakage in error responses (folded into the
`internal_error` generic-message fix in §3.4 — no exception message containing a connection
string, schema name, or internal tenant DB identifier is ever surfaced to a client for an
unmapped exception). Tenant *code* (not a DB name) is only ever echoed back via the resolved
`TenantContext` to the same authenticated tenant it belongs to (never cross-tenant — see §3.2).

---

## 4. `SpareParts.ManagementTests`

70 tests. Covers the WPF desktop application's `ManagementCoordinator`/`ManagementViewModel`
layer plus WPF-specific layering rules and a few standalone pricing/valuation engines used by
the desktop app.

- **`DesktopLayerDependencyTests.cs`** (9 tests) — mirrors `LayerDependencyTests` but for the
  desktop-specific assembly chain: `Desktop.Interfaces` and `Desktop.Abstractions` must not
  reference `Desktop.Helpers`/`ViewModels`/`Controls`/`Wpf` (the shell); `Desktop.Helpers` and
  `Desktop.Controls` must not reference `ViewModels`/`Wpf`; `Desktop.ViewModels` must not
  reference the `Wpf` shell assembly, but **must** reference `Desktop.Abstractions` and
  `SpareParts.Application`; and the `Wpf` shell **must** reference `Desktop.ViewModels`. This
  keeps the desktop MVVM layering one-directional (Shell -> ViewModels -> Abstractions/Helpers/
  Controls), so ViewModels stay unit-testable without pulling in WPF windowing types.
- **`ManagementCoordinatorCrudTests.cs`** — theory-driven tests proving every "Save" (create),
  "Save" (update), and "Delete" action in the desktop Management workspace calls the expected
  HTTP verb + URL + payload type against a `RecordingCrudApiClient` test double — i.e. the UI
  layer is wired to the correct REST endpoint for every entity type it manages (customers,
  suppliers, brands, categories, users, roles, warehouses, car brands/models, etc.).
- **`SmartPricingCoachTests.cs`**, **`UsedCarEntryProfitMapTests.cs`**,
  **`UsedVehiclePartPricingEngineTests.cs`**, **`UsedCarWholesaleViewModelTests.cs`** — unit
  tests for the desktop's pricing-recommendation and used-car-teardown profit calculators.
- **`WpfSurfaceSmokeTests.cs`** — instantiates real WPF `UserControl`/`Window` types
  (parameterless and via XAML) to catch XAML-load-time crashes (missing resource keys, binding
  setup exceptions at construction time) without needing a full interactive UI test — this is
  the desktop equivalent of a "does it even render" smoke test.

---

## 5. `SpareParts.IntegrationTests`

Two files; the real-database counterpart to the fake-repository `CriticalPathTests` concurrency
tests. Uses **Testcontainers** (`Testcontainers.MsSql`) to spin up a real, disposable SQL Server
2022 container per test run.

- **`SqlServerSaleTestDatabase.cs`** — test fixture that starts the container
  (`IAsyncLifetime.InitializeAsync`), exposes `CanRunIntegrationTests()` (returns `false`
  gracefully if Docker/the container couldn't start — e.g. no Docker available in the CI/dev
  environment — rather than failing hard), and `ResetSchemaAsync`/`SeedBaselineAsync` helpers
  that drop and recreate a minimal sales/stock/accounting schema per test.
- **`CreateSaleSqlServerIntegrationTests.cs`** (3 tests, all skip gracefully if no container is
  available):
  - `Handle_ShouldCreateInvoice_DecrementStock_AndWriteJournal` — a real `CreateSaleHandler` run
    against real SQL Server correctly creates the invoice row, invoice-item row(s), decrements
    stock by the sold quantity, and writes a balanced journal entry (4 journal lines for a single
    line-item sale: debit/credit pairs for revenue and COGS).
  - `Handle_ConcurrentSingleStock_ShouldAllowOnlyOneSale` — the real cross-process/cross-
    connection concurrency proof: two real `CreateSaleHandler` instances on separate SQL
    connections race to sell the last unit of stock; exactly one succeeds (`ConflictException` on
    the other), final stock is exactly 0, and exactly one invoice was created. This is the
    genuine end-to-end proof that complements `CriticalPathTests`' in-memory simulation (§2.3).
  - `Handle_WhenJournalInsertFails_ShouldRollbackInvoiceAndStock` — seeds an *invalid* posting
    configuration (`seedValidPostingAccounts: false`) so journal insertion fails partway through,
    and proves the whole operation rolls back atomically: no invoice row, no stock movement, no
    journal entry, and stock quantity unchanged — i.e. the sale creation is a single all-or-
    nothing database transaction, not a sequence of independently-committed steps.

---

## 6. How to run each test project

All commands assume a working directory of the repository root (`D:\Ralph\SpareParts`) and the
.NET 8 SDK installed. `ArchitectureTests` and `ManagementTests` require the Windows-only
`net8.0-windows` TFM (they reference WPF assemblies for the layer-dependency checks), so they
must be run on Windows; `IntegrationTests` targets plain `net8.0` and additionally requires a
working Docker daemon (used by Testcontainers to launch the disposable SQL Server container).

```powershell
# Architecture, security, and quality-guardrail tests (fast, no external dependencies)
dotnet test tests/SpareParts.ArchitectureTests/SpareParts.ArchitectureTests.csproj

# WPF desktop layering + ManagementCoordinator + pricing-engine tests (fast, no external dependencies)
dotnet test tests/SpareParts.ManagementTests/SpareParts.ManagementTests.csproj

# Real SQL Server integration tests (requires Docker running; tests self-skip if the container
# cannot start, so this is always safe to run even without Docker — it just won't exercise
# anything in that case)
dotnet test tests/SpareParts.IntegrationTests/SpareParts.IntegrationTests.csproj
```

To run a single test or a filtered subset (any project), use the standard xUnit/VSTest filter
syntax, e.g.:

```powershell
dotnet test tests/SpareParts.ArchitectureTests/SpareParts.ArchitectureTests.csproj `
  --filter "FullyQualifiedName~SecurityAndDataIntegrityTests"
```

To run the whole solution's test surface in one pass (still excludes anything that needs Docker
if it's unavailable, since IntegrationTests self-skips):

```powershell
dotnet test SpareParts.sln
```

As of this document's last verification: **103/103** ArchitectureTests pass, **70/70**
ManagementTests pass.

---

## 7. Round 2 investigation notes (for historical/audit context)

Round 1 left 7 pre-existing `ArchitectureTests` failures unresolved, flagged as unrelated to
security and deferred to other agents' domains (backend/database/desktop/mobile/web) to settle
first. Round 2 re-investigated all 7 once every platform's work had landed:

### 7.1 `ApiCompositionTests.AddCapabilityControllers_ShouldExposeOnlyExpectedControllers` (5 cases)

**Root cause**: test drift, not a code bug. `SparePartsApiComposition.ControllerMap` (production
code) had been correctly updated many times as new controllers were added (confirmed by the
separately-passing `AddCapabilityControllers_WithAllCapabilities_ShouldCoverEveryConcreteApiController`
completeness check), but the test file's own hand-duplicated `CapabilityControllerCases` expected
lists were never updated to match. **Fix**: updated the test's expected lists to mirror
`ControllerMap` exactly, capability by capability.

### 7.2 `QualityGuardrailTests.Feature_lists_should_not_drift_between_web_mobile_and_desktop`

**Root cause**: test drift. The `CanonicalScreens` array (34 entries) and `DesktopScreenMap`
dictionary were stale relative to the ~74 screens actually implemented across web, mobile, and
desktop (confirmed present and genuinely implemented on all three platforms — real ViewModels
exist on desktop for every new screen, real components exist on web/mobile). Also found that
`admin-billing` was declared `WebOnlyScreens` even though mobile and desktop had both since
grown a real `admin-billing` screen too. **Fix**: expanded `CanonicalScreens` and
`DesktopScreenMap` to the full current set, and emptied `WebOnlyScreens` since no screen is
web-exclusive anymore.

A second, independent bug was found and fixed inside the *same* test method: `ParseFeatureModules`
used a naive non-greedy regex (`\{(?<body>.*?)\}`) to carve each `featureModules` object out of
the JS array text. The `car-twin` module's `endpoint` field contains a literal
`{id}` route placeholder (`/api/usedcars/{id}/twin`), and the non-greedy regex terminated the
object body at that embedded `}` instead of the object's real closing brace — truncating the
capture before the `capabilities` field, so it parsed as empty and failed
`Assert.NotEmpty(mobileModule.Capabilities)`. **Fix**: replaced the regex-based split with a
brace-depth/quote-aware scanner (`ExtractTopLevelObjectBodies`) that correctly ignores braces
inside string literals.

### 7.3 `QualityGuardrailTests.Api_contract_smoke_tests_should_cover_every_screen_endpoint`

Two distinct root causes, both fixed:

1. **Genuine test-code bug** in `RouteMatchesEndpoint`: `Regex.Escape` only escapes `{` (not `}`,
   which is not a .NET regex metacharacter on its own), so the placeholder-substitution regex
   `@"\\\{[^}]+\\\}"` (which required an *escaped* closing brace) never matched any route
   template with a type constraint like `{id:int}`, silently leaving the literal `{id:int}` in
   the final match pattern and causing every such route to never match any endpoint. This made
   `api/usedcars/{id:int}/twin` and `api/usedcars/{id:int}/state-events` — both real, correctly
   implemented routes — falsely reported as "missing". **Fix**: corrected the regex to
   `@"\\\{[^}]+\}"` (matching an escaped opening brace through to an *unescaped* closing brace).

2. **Genuine frontend-config bugs** (6 endpoint strings that didn't match any real backend
   route), found only after fix #1 above stopped masking them:
   - `mechanic-desk` endpoint was `/api/mechanic-desk` (no such controller); the real backend for
     this screen is `RepairOrdersController` at `/api/repair-orders`. This module is **not**
     `commandOnly`, so on mobile (which drives its generic module-table screen off
     `module.endpoint`) this was a live bug — the Mechanic Desk screen's data fetch would 404.
     Fixed on both web and mobile config.
   - `halfcut` endpoint was `/api/half-cut` (singular); the real route is `/api/half-cuts`
     (plural, matching `HalfCutController`'s `[Route("api/half-cuts")]`). Same live-bug
     situation on mobile as `mechanic-desk`. Fixed on both web and mobile config.
   - `car-crush`, `qr-tag`, `part-genealogy`, `new-vs-used` endpoint strings pointed at
     non-existent bare/incorrect paths. These four are all `commandOnly: true` modules (backed by
     dedicated screen components, not the generic endpoint-driven module table), so this was
     contract-documentation drift rather than a live runtime bug — but was still corrected for
     accuracy: `car-crush` -> `/api/car-crush/generate`, `qr-tag` -> `/api/qr-tag/{id}`,
     `part-genealogy` -> `/api/part-genealogy/{id}`, `new-vs-used` -> `/api/new-vs-used/{id}`,
     each now pointing at a real controller route.

Files touched for the frontend-config fixes: `src/SpareParts.Web.React/wwwroot/js/core/config.js`,
`src/SpareParts.Mobile.ReactNative/src/core/app-config.js`.

All 7 originally-failing cases, plus the two additional bugs uncovered while fixing them (the
`ParseFeatureModules` brace bug and the `RouteMatchesEndpoint` escaping bug), are now resolved.
Full suite: 103/103 `ArchitectureTests` passing, 70/70 `ManagementTests` passing.
