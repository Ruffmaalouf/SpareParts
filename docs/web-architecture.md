# SpareParts Web (React) — Architecture Reference

This document is a deep-dive reference for the React web front end at
`src/SpareParts.Web.React/`. It is written so a new engineer can onboard from
this file alone: how the app is built and served, how the core client
modules work, the conventions every view follows, and a feature-by-feature
map of the major screens.

---

## 1. High-level shape of the project

`SpareParts.Web.React` is **not** a bundled/compiled React app (no webpack,
no Vite, no Babel, no `npm run build`). It is a set of hand-written **ES
modules** served as plain static files by a minimal ASP.NET Core 8 host. The
browser loads native `<script type="module">` and resolves every
`import ... from "./..."` at runtime directly against the file on disk.

```
src/SpareParts.Web.React/
  Program.cs                     <- ASP.NET Core 8 minimal host (static files only)
  SpareParts.Web.React.csproj
  appsettings.json
  wwwroot/
    index.html                   <- single HTML entry point, loads CDN React + app.js
    app.js                       <- bootstraps the React root
    config.js                    <- runtime-injected config (API base URL, OAuth client IDs)
    config.staging.js            <- staging variant, swapped in at deploy time
    styles.css                   <- base styles
    css/cockpit.css              <- admin/cockpit UI styling
    css/storefront-apex.css      <- customer storefront ("Apex") styling
    brand-icon.svg, assets/      <- static images
    js/
      core/                      <- framework-agnostic building blocks (config, api client, formatters, i18n, stores, screen registry, react-runtime)
      components/                <- shared cross-view UI (auth, layout, shared widgets, smart search, locked-feature modal)
      components/ui/             <- the "design system" component library (AppShell, Sidebar, Topbar, DataTable, PageHeader, etc.)
      services/                  <- small stateless service classes/helpers (ApiClient wrappers, pricing coach, notification/SignalR client, communication payload factory)
      admin/                     <- generic CRUD engine used by the Management screen (crud-config.js, resource-utils.js)
      views/                     <- one file per feature/screen (71 files) — the bulk of the app
```

### Why no bundler

- React and ReactDOM are loaded from `unpkg` as UMD globals in `index.html`
  (`window.React`, `window.ReactDOM`). `core/react-runtime.js` simply
  re-exports `window.React`, `window.ReactDOM`, `React.createElement` (as
  `h`), and the commonly used hooks (`useState`, `useEffect`, `useMemo`,
  `useRef`, `useCallback`, `startTransition`).
- Every component file writes UI with `h(tag, props, ...children)` — this is
  `React.createElement` under a short alias, not JSX. There is no compile
  step, so there cannot be a `.jsx` file; everything is plain `.js` calling
  `h(...)`.
- Third-party libraries needed globally (SignalR client, a QR-code
  generator) are also loaded via `<script>` tags in `index.html` and consumed
  off `window` (e.g. `window.signalR`, `window.qrcode`).
- Because it's just static files, "deploying" the front end means copying
  `wwwroot/` — there is no build artifact to produce beyond what's already
  checked in.

### The ASP.NET host (`Program.cs`)

The host is intentionally tiny:
- `UseDefaultFiles()` + `UseStaticFiles()` serve everything under `wwwroot/`.
  Static file responses are forced `Cache-Control: no-store, no-cache,
  max-age=0` / `Pragma: no-cache` so browsers always fetch the latest JS —
  important because there's no cache-busting hash in filenames (no bundler).
- `/health` returns a small JSON status/uptime payload.
- `MapFallbackToFile("index.html")` makes this a proper SPA host: any
  unmatched route (e.g. a deep link to `/passport/123`) falls back to
  `index.html`, and client-side logic in `app.js`/`app-shell.js` decides what
  to render based on `window.location.pathname`.
- In non-Development environments it adds `UseExceptionHandler("/error")`
  and `UseHsts()`.

This host serves **only the front end**. All data comes from the separate
`SpareParts.Api` project via HTTP calls configured through `config.js`
(`window.SparePartsWebConfig.defaultApiBaseUrl`, normally
`http://localhost:5000` in dev, overridden per environment).

### `index.html` responsibilities

- Loads Google Fonts, preconnects to `unpkg.com`.
- Loads `/config.js` (must load **before** `app.js`, since `app.js`'s module
  graph reads `window.SparePartsWebConfig` synchronously at import time via
  `core/config.js`).
- Loads the Google Identity Services script (`accounts.google.com/gsi/client`)
  for Google sign-in, React/ReactDOM UMD builds, Microsoft SignalR client,
  and a QR code generator library — all as global `<script>` tags, not ES
  modules.
- Loads `styles.css`, `css/cockpit.css`, `css/storefront-apex.css`.
- The only DOM the server renders is `<div id="root"></div>`; everything
  else is client-rendered.

---

## 2. Core client modules (`js/core/`)

These are the framework-agnostic primitives that every view and component
builds on.

### `core/react-runtime.js`
Thin re-export layer over the globally-loaded React/ReactDOM UMD bundles:
```js
export const React = window.React;
export const ReactDOM = window.ReactDOM;
export const h = React.createElement;
export const { useCallback, useEffect, useMemo, useRef, useState, startTransition } = React;
```
Every other file imports hooks and `h` from here rather than touching
`window.React` directly, which keeps the rest of the codebase decoupled from
"React is a global" as an implementation detail.

### `core/config.js`
Central static configuration:
- `storageKeys` — the `localStorage` key names used for persisted session
  state (`apiBaseUrl`, `token`, `user`, `theme`, `language`).
- `defaultApiBaseUrl`, `googleClientId`, `facebookAppId` — read from
  `window.SparePartsWebConfig` (populated by `config.js`/`config.staging.js`,
  which are swapped per environment at deploy time — this is how the same
  static build points at different API hosts without a rebuild).
- `wpfThemes` — 8 color themes (Apex, Aurora, Carbon, AMG, BMW M, Lambo,
  Neon Glow, Porsche RS) shared in spirit with the WPF desktop app's theme
  system; each theme is a flat map of CSS custom-property values.
- `languageOptions` — `en`/`ar`/`fr`.
- `featureModules` — a large declarative array (~80 entries) that maps every
  WPF-desktop-equivalent capability to a `key`, display `label`/`title`, the
  **source** WPF window/tab it corresponds to (useful for cross-referencing
  desktop parity), its primary API `endpoint`, and a human-readable list of
  `capabilities`. This array is the backbone of navigation (`Sidebar`),
  the generic `module-workspace-view.js` fallback renderer, and the
  Management workspace launcher.
- `managementSections` — the 14 CRUD resource types shown in the Management
  screen (customers, suppliers, brands, parts, part-requests, car-brands,
  car-models, users, warehouses, locations, currencies, roles,
  transaction-types, categories), each pointing at a REST collection
  endpoint.

### `core/formatters.js`
Pure display/value-shaping helpers used throughout the app:
- `normalizeBaseUrl` — trims trailing slash from an API base URL.
- `normalizeThemeKey` / `applyWebTheme` — validates a theme key and writes
  its color palette onto `document.documentElement` as CSS custom
  properties (`--bg`, `--accent`, etc.), which is how theme switching works
  purely through CSS variables with zero re-render cost.
- `pickFirst(row, keysArray)` — returns the first non-empty value from a list
  of camelCase keys on a row (no PascalCase fallback). Used internally by
  `rowTitle`/`rowSubtitle`/`rowAmount` in this same file.
- `readField(row, ...keys)` (module-private, **not exported**) — like
  `pickFirst` but also tries a PascalCase-cased version of each key. Used
  only inside `formatters.js` itself (by `appConstantValue`,
  `resolveRateToBaseCurrency`, etc.) for currency/appConstants lookups where
  API responses can arrive in either casing.
- Multi-currency helpers: `normalizeCurrencyCode`, `appConstantValue`,
  `resolveRateToBaseCurrency`, `displayCurrencyContext`,
  `convertBaseToDisplay`, `displayMoneyFromBase`, `convertCounterToDisplay`,
  `displayMoneyFromCounter` — these implement a base-currency /
  counter-currency / display-currency conversion pipeline driven by
  `appconstants` API data and FX rate rows, used anywhere money needs to be
  shown in a currency different from how it's stored.
- `rowTitle`, `rowSubtitle`, `rowAmount` — generic "describe this API row as
  a title/subtitle/amount" used by the generic Management CRUD table
  columns (`genericColumns` in `management-view.js`).
- `money`, `dateTime`, `shortDate`, `initials`, `asRows`, `escapeHtml` —
  general-purpose formatting/utility functions used everywhere.

**Important distinction from the `read`/`readField`/`pickFirst` family**:
`core/formatters.js` has its own **private** `readField` (unexported) that
is functionally equivalent to `admin/resource-utils.js`'s exported `read`,
but they are separate implementations serving separate call sites. This was
evaluated during the Round 2 consolidation and intentionally left alone
because `readField` here is not imported by any view — see §7 below for the
full consolidation record.

### `core/i18n.js`
A small hand-rolled i18n layer (no external library):
- `dictionaries` — nested translation objects for `en`, `ar`, `fr` covering
  common UI strings, navigation labels, settings, login, management CRUD
  copy, storefront copy, used-car workspace copy, and accounting report
  copy. Not every UI string is translated (many views hard-code English),
  but the shell chrome and several major views are.
- `createTranslator(languageKey)` returns a `t(key, fallback, params)`
  function that does dotted-path lookup (`"accounting.trialBalance"`),
  falls back to the caller-supplied fallback text if the key is missing, and
  interpolates `{param}` placeholders.
- `isRtlLanguage` — Arabic renders right-to-left; `App` sets
  `document.documentElement.dir` accordingly.

### `core/api-client.js`
`ApiClient` is the single HTTP abstraction every view uses to talk to
`SpareParts.Api`.
- Constructor takes `(apiBaseUrl, token, onUnauthorized, onPlanLocked)`.
- `requestResponse(path, options)` builds a `fetch` call, always sets
  `Accept: application/json`, auto-sets `Content-Type: application/json`
  when there's a JSON body (skipped for `FormData` uploads), and attaches
  `Authorization: Bearer <token>` when a token is present.
- **401 handling**: if the API returns 401, `onUnauthorized()` fires
  (wired up in `app-shell.js` to clear the session and drop the user back to
  the login/storefront screen) and an `ApiError("Session expired...")` is
  thrown.
- **403 plan-lock handling**: if the API returns 403 with a recognizable
  `errorCode` body (SaaS plan/feature gating), `onPlanLocked(error)` fires
  (wired to show `LockedFeatureModal`) carrying `currentPlan`,
  `requiredPlans`, and an `upgradeUrl`.
- `ApiError` extends `Error` and carries `status`, `errorCode`,
  `currentPlan`, `requiredPlans`, `upgradeUrl` for callers to branch on.
- `get`, `post`, `put`, `delete`, `postForm` — standard verbs;
  `post`/`put` JSON-stringify the body, `postForm` passes a `FormData`
  through untouched (used for image/file uploads).
- **Auto-pagination for `/api/parts`**: `list(path)` special-cases any path
  that resolves to the bare `/api/parts` collection (via
  `isPartsCollectionPath`) and transparently walks all pages using
  `getAllPages`, which keeps requesting `page=n&pageSize=5000` and
  concatenating results until a short page, `X-Total-Count`, or an empty
  batch signals the end. Every other endpoint's `list()` just delegates to a
  single `get()`. This exists because the parts catalog can be large and
  callers throughout the app expect `list("/api/parts")` to return the
  *entire* catalog in one call rather than one page.

### `core/stores.js`
Three tiny wrapper classes around `window.localStorage`, parameterized by a
`Storage`-like object and key names so they're trivially testable:
- `BrowserSessionStore` — persists/loads `apiBaseUrl`, JWT `token`, and the
  raw login response as `user`; `save()` writes all three, `clear()` removes
  token+user (keeps the last-used API base URL).
- `BrowserThemeStore` — persists the selected theme key (validated against
  `themeMap`).
- `BrowserLanguageStore` — persists the selected language key (validated
  against `languageOptions`).

### `core/screen-registry.js`
`ScreenRegistry` is a tiny lookup class: given an ordered list of
`{ key, component }` entries, it resolves a screen key to its React
component, with a hard-coded alias so `"parts"` and `"inventory"` resolve to
each other (`SmartSearch` results reference `"parts"`, the actual sidebar
key is `"inventory"`). Falls back to the first registered screen if the key
is unknown. Actual registration lives in `views/screen-registry.js` (see
§4).

---

## 3. Application bootstrap and shell

### `app.js` (project root of `wwwroot`)
```js
import { h, ReactDOM } from "./js/core/react-runtime.js";
import { App } from "./js/app-shell.js";
ReactDOM.createRoot(document.getElementById("root")).render(h(App));
```
This is the entire entry point — it mounts `<App/>` into `#root`.

### `js/app-shell.js` — the `App` component
This is the root of the component tree and owns all top-level state:
- `apiBaseUrl`, `token`, `user` — session state, seeded from
  `BrowserSessionStore` and persisted back on login/logout.
- `view` — the currently active admin screen key (`"dashboard"` by
  default), changed via `switchView` which wraps `setView` in
  `startTransition` for non-blocking navigation.
- `themeKey`, `languageKey` — seeded from their respective stores; effects
  apply the theme (`applyWebTheme`) and set `document.documentElement.lang`
  / `.dir` whenever they change, and persist back to storage.
- `notifications` — an in-memory toast queue (max 4) fed by:
  - a live SignalR connection (`services/notification-service.js`,
    `createPartNotificationClient`) that listens for `partAdded` and
    `reservationReminder` hub events once a user is signed in, deduping
    part-added notifications per part for 30 seconds via a `Map` ref.
- `planLock` — state for the SaaS plan-lock modal, set by `ApiClient`'s
  `onPlanLocked` callback; `goToBilling()` dismisses the lock and navigates
  to the `billing` screen.
- **Routing logic in `App()`** (not a router library — just conditional
  rendering based on state/URL):
  1. If the URL path starts with `/passport/`, render only
     `PartPassportView` (a public, unauthenticated proof-of-part page meant
     to be linked from WhatsApp/marketplace listings — no shell/login at
     all).
  2. If the user is **not signed in**, or is signed in with the special
     "web app" role (`roleId === 4`, i.e. a customer/storefront account),
     render `CustomerStorefrontView` (the public storefront) plus the
     notification stack. This means storefront customers never see the
     admin cockpit even if they're authenticated.
  3. Otherwise (an authenticated staff/admin user), render the full admin
     shell: `AppShell` (layout wrapper) with a `Sidebar` (navigation),
     `Topbar` (search, notifications, language, "new sale" shortcut), the
     resolved `ActiveScreen` component from `screenRegistry`, the
     notification stack, and `LockedFeatureModal`.
- Every screen component receives a consistent prop contract:
  `{ api, activeView, languageKey, themeKey, user, onLanguage, onLogout,
  onTheme, onView, t }`.

### `js/components/layout.js`
Sidebar navigation chrome shared by the admin shell:
- `navGroups` — the sidebar's grouping of every screen key into `Core`,
  `Operations`, `Finance`, `Tools`, `Marketplace`, `Intelligence`, and
  `Platform Admin` sections (plus an auto-generated "Other" group for any
  screen key not explicitly grouped).
- `SUPER_ADMIN_ROLE_ID` (5) / `superAdminOnlyKeys` — currently gates the
  `admin-billing` (Pricing & Subscriptions admin) screen to role ID 5 only.
- `NavIcon` — inline SVG icon picker keyed by screen key, with a generic
  fallback path.
- `BrandMark` — the logo mark used in both the sidebar and login screen.
- `Sidebar` — renders grouped nav buttons, the signed-in user panel, and
  sign-out.
- `ThemePicker` / `ThemeRail` — grid vs. compact-rail variants of the same
  theme-swatch picker.
- `LanguageSegment` / `LanguagePicker` — segment vs. grid variants of the
  language switcher.

### `js/components/auth.js`
`LoginScreen` (the admin/staff login page — separate from the storefront's
own inline auth) plus:
- `LoginPanel` — username/password form posting to `/api/auth/login`.
- `ExternalLoginButtons` — renders Google Identity Services and Facebook
  Login buttons when `googleClientId`/`facebookAppId` are configured; both
  post to `/api/auth/external-login` with a `provider` discriminator
  (`google`/`facebook`) and the appropriate token.

### `js/components/smart-search.js`
`SmartSearch` — the global omnibox in the `Topbar`. Debounces (220ms) calls
to `/api/search?q=...&limit=24`, groups results by `section`, remembers the
last 5 search terms in `localStorage` (`sp_recent_searches`), and navigates
via `onNavigate(targetView)` when a result is clicked (normalizing
`"parts"` → `"inventory"`). Has its own **locally-scoped, simpler** `read()`
helper (no PascalCase fallback) — see §7, this one was deliberately left
alone because it is not functionally identical to the rest.

### `js/components/locked-feature-modal.js`
`LockedFeatureModal` — the SaaS upgrade-prompt dialog shown when the API
returns a 403 plan-lock error; offers "Close" or "View Plans" (routes to the
`billing` screen).

### `js/components/shared.js`
Small presentational primitives reused by many older views before the
`components/ui/` library existed: `PageHeader`, `StatusLine`,
`NotificationCenter`, `Badge`, `EmptyState`, `DataTable`. Several newer
views import the richer versions from `components/ui/` instead (there is
some intentional overlap/evolution between `shared.js` and `ui/*.js` — both
are actively used; `shared.js` is the older, simpler generation and `ui/`
is the newer "Apex cockpit" design-system generation).

---

## 4. `components/ui/` — the design-system component library

This directory holds the more polished, "Apex cockpit"-styled building
blocks used by the newer/rebuilt views (inventory, dashboard, storefront,
etc.):

| Component | Purpose |
|---|---|
| `AppShell.js` | Top-level responsive layout: fixed sidebar + topbar + scrollable content area, with a mobile nav-toggle. |
| `Sidebar.js` | Cockpit-styled sidebar (distinct from `components/layout.js`'s `Sidebar` — this is the design-system variant used inside `AppShell`). |
| `Topbar.js` | Header bar: menu toggle, search slot, notifications bell, language switch, "new sale" button, user avatar. |
| `PageHeader.js` | Title/kicker/subtitle/action header block for screen content. |
| `DataTable.js` | Sortable/empty-state-aware table renderer. |
| `SearchBar.js` | Standalone search input used inside view-local filter bars. |
| `FilterPanel.js` | Collapsible filter chip/option panel. |
| `StatsCard.js` | KPI tile (used on dashboard-style summaries). |
| `PartCard.js` | Product-card rendering for a part row (used in inventory grid view and storefront). |
| `SellerCard.js` | Card for seller/mechanic/reputation-style entities. |
| `VehicleCard.js` | Card for used-car / vehicle entities. |
| `StatusPill.js` / `Badge.js` | Small status/label chips. |
| `EmptyState.js` | "No data" placeholder block. |
| `Modal.js` / `Drawer.js` | Overlay dialog / slide-in panel primitives. |
| `FormSection.js` | Grouped form-field layout helper (used by login and various admin forms). |
| `ActionBar.js` | Sticky footer bar for form actions (Save/Cancel), used by login and CRUD forms. |
| `ImageGallery.js` | Lightbox-style image gallery (used cars, part photos). |
| `LoadingSkeleton.js` | Skeleton loading placeholder. |
| `CockpitIcons.js` | Shared inline SVG icon set for the cockpit/dashboard UI (`Icon` component). |

---

## 5. Services (`js/services/`)

Stateless helper classes/functions that sit between views and the
`ApiClient`, encapsulating cross-view business logic that doesn't belong in
a single view file:

- **`resource-service.js`** — `ResourceService` (generic `list(endpoint)`
  wrapper returning normalized rows via `asRows`) and
  `CrudResourceService extends ResourceService`, which layers create/update/
  delete on top using a per-resource `config` object (from
  `admin/crud-config.js`) and the shared `buildPayload`/`rowId` helpers from
  `admin/resource-utils.js`. This is what powers the generic Management CRUD
  screen for all 14 `managementSections` resource types without one-off
  create/edit code per resource.
- **`accounting-service.js`** — `AccountingService` wraps the four
  `/api/accounting/...` report endpoints (accounts, statement-parties,
  trial-balance, ledger, statement-of-account/-party), building query
  strings via a small `buildQuery` helper that drops empty params.
- **`communication-payload-factory.js`** — `CommunicationPayloadFactory`
  builds the numeric-enum payload shapes the API's communications endpoint
  expects for common send actions (sales-invoice send, payment reminder,
  account-balance reminder, part-availability message, free-text message) —
  centralizes the channel/templateKey/recipientKind enum values so views
  don't hard-code magic numbers individually.
- **`notification-service.js`** — `createPartNotificationClient` wraps a
  SignalR `HubConnectionBuilder` against `/hubs/notifications`, wiring
  `partAdded` and `reservationReminder` hub events to caller-supplied
  callbacks; returns `null` if no token or no SignalR script loaded so
  callers can no-op safely.
- **`pricing-coach.js`** — `smartPricingCoach(part, waitingCustomers)`
  is a rules engine that inspects a part's price/cost/market-price/stock
  signals plus how many customers are waiting on it, and returns a
  `{ tone, badge, message, suggestedPrice }` recommendation (e.g. "price is
  below usual market range", "stock is rare and N customers are waiting").
  `waitingCustomersByPart(requests)` aggregates open/contacted part-request
  rows into a `Map<partId, waitingCount>`. `PricingCoachCard` /
  `PricingCoachSignal` are small render helpers consumed by
  `inventory-view.js`, `stock-arrival-theater-view.js`, and
  `module-workspace-view.js`'s stock workspace. This file now imports the
  shared `read` helper from `admin/resource-utils.js` (see §7).

---

## 6. Admin CRUD engine (`js/admin/`)

- **`resource-utils.js`** — the generic-resource toolkit used by the
  Management screen and now the single canonical home of the `read(row,
  ...keys)` field-lookup helper (see §7):
  - `read(row, ...keys)` — returns the first defined/non-null/non-empty
    value among the given keys, trying both the exact key and its
    PascalCase form (handles APIs that sometimes serialize
    `camelCase`/`PascalCase` inconsistently).
  - `rowId(row, config)` — resolves a row's identifier using
    `config.idKeys` (defaults to `["id", "userId", "locationId"]`).
  - `emptyForm(config)` / `formFromRow(row, config)` — build a form-state
    object from a CRUD config's `fields` array, either blank (using each
    field's `defaultValue`) or pre-filled from an existing row.
  - `buildPayload(config, form, isUpdate)` — turns form state back into an
    API payload, respecting per-field `create`/`update`/`readOnly`/
    `optionalUpdate`/`createKey`/`updateKey` flags and casting
    `bool`/`number`/text types.
  - `matchesRow(row, term)` — free-text search predicate checking a fixed
    list of common field names.
  - `rowTitle(row)` / `rowSubtitle(row)` — generic title/subtitle
    derivation for the Management resource list panel (a parallel,
    independent implementation to `core/formatters.js`'s `rowTitle`/
    `rowSubtitle` — both exist because they serve different row shapes/UI
    contexts; not part of the Round 2 consolidation scope).

- **`crud-config.js`** — declarative field definitions (`crudConfigs`) per
  resource type (`customers`, `suppliers`, `brands`, `parts`,
  `part-requests`, `car-brands`, `car-models`, `users`, `warehouses`,
  `locations`, `roles`, `transaction-types`, `categories`), each with a
  `basePath`, optional `idKeys`/`canUpdate`/`canDelete`, and a `fields`
  array describing label, type (`bool`/`number`/`email`/text), required/
  optional/readOnly/create-or-update-only behavior, and default values.
  This single config file plus `resource-utils.js` + `resource-service.js`
  is what lets `management-view.js` support full CRUD for 13 different
  resource types without per-resource form/table code.

---

## 7. View-per-feature structure (`js/views/`)

There are 71 files under `js/views/`, one (sometimes two, for closely
related screens) per feature. Every view is a self-contained module
exporting one or more React components consumed by
`js/views/screen-registry.js`.

### Common view conventions
Nearly every view follows the same shape:
1. Imports `h`/hooks from `core/react-runtime.js`, formatting helpers from
   `core/formatters.js`, and presentational primitives from
   `components/shared.js` and/or `components/ui/*`.
2. Declares local constants (status option lists, column definitions, empty
   form shapes).
3. Declares a functional component named `XyzView({ api, ... })` (or
   `XyzWorkspaceView`) that:
   - Holds local `useState` for rows, filters, the active form, loading/
     saving flags, and a `status` message string shown via `StatusLine`.
   - Defines a `load` callback (wrapped in `useCallback`) that calls
     `api.get`/`api.list`/`Promise.all([...])` and sets state, with
     try/catch/finally updating the `status` string and `isLoading`.
   - Runs `load()` in a `useEffect` on mount (and sometimes on filter
     change).
   - Defines `save`/`remove`/action callbacks that call `api.post`/
     `api.put`/`api.delete`, then re-run `load()` on success.
   - Renders `PageHeader` + filter/search controls + a `DataTable` or card
     grid + a detail/edit panel, using `money`/`dateTime`/`shortDate`/
     `initials` from `formatters.js` for display.
4. Exports the component (and sometimes a helper like `selectPartPassport`
   or `passportHref` for cross-view navigation, e.g. from inventory into
   the Part Passport workspace).

### The generic "module workspace" pattern
Many WPF-desktop-equivalent screens that don't yet have a bespoke web UI are
rendered through **`module-workspace-view.js`**'s `createModuleView(module)`
factory, driven entirely by a `featureModules` entry from `core/config.js`.
`ModuleWorkspaceView` special-cases a handful of modules with dedicated
sub-workspaces defined in the same file (`BusinessAssistantWorkspace`,
`ReportBuilderWorkspace`, `ScanLookupWorkspace`, `PartPurchasesWorkspace`,
`UsedCarPurchasesWorkspace`, `UsedCarWholesaleWorkspace`, `StockWorkspace`,
`ManualJournalWorkspace`), and otherwise falls back to a generic
preview-table renderer (`genericColumns` from `management-view.js`, fed by
`loadModuleRows(api, module.endpoint)`) — this is why some `featureModules`
entries with `commandOnly: true` (like `symptom-search`, `whatsapp-selling`,
`car-crush`, `voice-search`, `does-it-fit`, `price-genius`,
`condition-scanner`, `qr-tag`, `part-genealogy`, `dismantler-forecast`,
`new-vs-used`, `kareem`, `ar-finder`, `price-report`) either render a status
message only (no browsable list, since the underlying endpoint is a POST
"command" rather than a GET collection) or have their own bespoke view file
instead when a richer UI was built (most of the AI/marketplace feature
views listed below **do** have dedicated `views/*.js` files rather than
going through the generic fallback — check `views/screen-registry.js` for
the authoritative mapping of key → component).

### `views/screen-registry.js`
This file is the single source of truth mapping every screen `key` (matching
a `featureModules` entry) to its React component, and constructs the
`screenRegistry` (`ScreenRegistry` instance from `core/screen-registry.js`)
consumed by `app-shell.js`. **This is the first place to look** when adding
a new screen or figuring out which file implements a given sidebar item.

> Note: there is also a `js/admin/crud-config.js`-adjacent, differently
> named `screen-registry.js` under `js/core/` (the generic `ScreenRegistry`
> **class**) versus `js/views/screen-registry.js` (the **instance** wiring
> every concrete view). Don't confuse the two when searching the codebase.

### Feature-by-feature map of major views

**Core commerce**
- `dashboard-view.js` — Owner Cockpit: sales/profit/cash/debt/stock-value
  KPIs, signal lights (`isRedSignal`), margin-watch detection.
- `invoices-view.js` — POS / Sales: create/search invoices, send invoice or
  payment reminder via WhatsApp (`CommunicationPayloadFactory`).
- `sales-returns-view.js` — browse sales return records, credit/refund
  review.
- `inventory-view.js` — Parts catalog: browse/filter by code/name/OEM,
  condition and availability filters, donor-car linkage, smart pricing
  coach integration, part-request demand matching (`waitingCustomersByPart`),
  send part-availability WhatsApp messages, links into Part Passport.
  (Uses `read` aliased as `readValue`, imported from `admin/resource-utils.js`
  — see §Round 2 consolidation below.)
- `part-passport-view.js` / `part-passport-workspace-view.js` — a
  public-facing "proof card" for a part (used-car provenance, OEM evidence)
  plus the internal workspace to pick a part and prepare its shareable link;
  `part-passport-view.js` exposes `passportHref` reused by other views.
- `part-compatibility-view.js` — visual part-to-vehicle fitment graph, OEM
  and donor-car fitment evidence.
- `part-requests-view.js` — unavailable-part demand board / follow-up list.
- `quotes-view.js` — draft/sent quotes, line items against live parts
  catalog, quote-to-sale conversion, expiry tracking.
- `contacts-view.js` — customers/suppliers/opening balances.
- `management-view.js` — the generic CRUD engine UI (workspace launcher +
  resource list/detail panel) driving all 14 `managementSections` resource
  types via `admin/crud-config.js` + `admin/resource-utils.js` +
  `services/resource-service.js`; also exports `genericColumns` reused by
  `module-workspace-view.js`.
- `settings-view.js` — theme/language pickers + account/sign-out panel.

**Purchasing / used cars / stock**
- `stock-arrival-theater-view.js` — "arrival" kanban-style lanes (photo
  queue, waiting customers, campaigns, pricing) for freshly purchased stock.
- `used-cars-view.js` — used-car records, image galleries, linked parts,
  break-even/teardown profit tracking.
- `car-twin-workspace-view.js` — Vehicle Digital Twin: 3D-ish timeline of
  condition/state events per vehicle (lazy-loads `three.js` from `unpkg` for
  the visual, `loadThree()`), event types (Inspection, MileageUpdate,
  ConditionChange, LocationMove, Note).
- `repair-prep-board-view.js` — kanban board (Bought → Inspected → Parts
  Needed → Repairing → Photo-ready → Listed → Sold) with a per-car task
  checklist and prep-cost tracking, persisted to `localStorage`.
- `dead-stock-view.js` — dormant stock candidates + shelf-value recovery
  summary.
- `growth-lab-view.js` — "Money Finder": tonight's-money queue, donor-car
  treasure map, auction simulator, teardown queue, duplicate detection,
  buying radar, WhatsApp voice-to-quote.
- `barcode-mode-view.js` — barcode/AR scan lookup workspace; contains its
  own Code128 pattern table for label rendering/printing.

**Finance / accounting**
- `accounting-view.js` — Trial Balance / Ledger / Statement-of-Account
  report tabs via `AccountingService`, with base/counter/display currency
  conversion (`displayCurrencyContext`) and PDF export.
- `customer-aging-view.js` / `supplier-aging-view.js` — 0/30/60/90+ day
  aging buckets for receivables/payables.
- `billing-view.js` — the tenant-facing subscription screen: current
  subscription, package comparison, payment history, invoice list; exports
  `FEATURE_LABELS`/`LIMIT_LABELS` reused by `admin-billing-view.js`.
- `admin-billing-view.js` — super-admin-only (role ID 5) SaaS package/
  subscription/payment/invoice/webhook-event administration (tabs: Packages,
  Subscriptions, Payments, Invoices, Webhook Events).

**Communication / AI**
- `whatsapp-view.js` — conversation list/thread history/free-text send, plus
  a "voice-to-quote" demo dataset (transcribed customer voice notes →
  suggested parts) and campaign composer (segment/language selectors).
- `whatsapp-selling-view.js` — seller WhatsApp number configuration and a
  shareable listing-message generator (`buildWhatsAppMessage` →
  `wa.me/<phone>?text=...` deep link) plus marketplace catalog export.
- `kareem-view.js` — "AutoChat Kareem" multilingual AI chat workspace.
- `module-workspace-view.js`'s `BusinessAssistantWorkspace` — natural-language
  business assistant that turns answers into actions (reports, reminders,
  purchase-order drafts, campaigns).

**Marketplace / seller tools** (each a dedicated view file):
`needboard-view.js`, `watchlist-view.js`, `seller-reputation-view.js`,
`seller-verification-view.js`, `my-garage-view.js`, `referral-view.js`,
`regional-demand-view.js`, `mechanic-trust-view.js`,
`community-guard-view.js`, `escrow-view.js`, `listing-boost-view.js`,
`halfcut-view.js`, `part-reel-view.js`, `part-reserve-view.js`,
`garage-stock-view.js`, `mechanic-desk-view.js`, `live-inspection-view.js`,
`yard-tour-view.js`, `negotiation-view.js`, `instant-offer-view.js`,
`part-insurance-view.js`, `market-price-view.js`, `new-vs-used-view.js`,
`price-report-view.js`, `symptom-search-view.js`, `voice-search-view.js`,
`does-it-fit-view.js`, `price-genius-view.js`, `condition-scanner-view.js`,
`qr-tag-view.js`, `part-genealogy-view.js`, `dismantler-forecast-view.js`,
`car-crush-view.js`, `ar-finder-view.js`. These mirror the long tail of
`featureModules` entries; most are self-contained single-file screens with
their own local state and direct `api` calls (no shared service layer
beyond `ApiClient`).

**Ops / back-office**
- `reorder-view.js`, `expiry-alerts-view.js`, `loyalty-view.js`,
  `warranty-view.js`, `shipments-view.js`, `activity-log-view.js`,
  `api-platform-view.js` (API key issuance/revocation, super-admin-adjacent
  but not role-gated in the sidebar the way `admin-billing` is).

### Storefront

- `storefront-view.js` — `CustomerStorefrontView`, the full public-facing
  "Apex Motorsport"-styled e-commerce storefront rendered instead of the
  admin shell whenever there is no authenticated staff session (see
  `app-shell.js` routing logic in §3). Contains: marque/brand filter chips,
  a scrolling trust-signal ticker, cart state, checkout flow with
  Areeba/Whish payment gateway selection, login/sign-in panel reuse from
  `components/auth.js`'s `LoginPanel`, and part card rendering (bench-
  checked/OEM badges, availability labels, image resolution from
  `imageUrl`/`imageUrls` in several possible serialized shapes). Styled by
  `css/storefront-apex.css`.

---

## 8. Round 2 duplication consolidation (`read(row, ...keys)`)

Round 1 of the audit flagged a `read(row, ...keys)` field-lookup fallback
helper (returns the first defined/non-empty value among candidate keys,
trying both the literal key and its PascalCase form) duplicated verbatim
across 12+ files. Round 2 closed this out:

- **Canonical implementation**: `admin/resource-utils.js`'s exported
  `read(row, ...keys)` — chosen because it was already the shared,
  exported, actively-imported implementation used by
  `services/resource-service.js` and `views/management-view.js`, unlike the
  private `readField` in `core/formatters.js` (not exported) or
  `pickFirst` in the same file (different signature: takes an array, not
  rest args, and has no PascalCase fallback).
- **11 files** had a byte-identical local `function read(row, ...keys) {...}`
  removed and replaced with `import { read } from "../admin/resource-utils.js";`:
  `views/accounting-view.js`, `views/car-twin-workspace-view.js`,
  `views/module-workspace-view.js`, `views/used-cars-view.js`,
  `views/part-passport-workspace-view.js`,
  `views/stock-arrival-theater-view.js`, `views/part-compatibility-view.js`,
  `views/repair-prep-board-view.js`, `views/barcode-mode-view.js`,
  `views/dead-stock-view.js`, `services/pricing-coach.js`.
- **1 additional file** had the same logic under a different local name,
  `readValue(row, ...keys)`: `views/inventory-view.js`. Rather than rewrite
  its dozen call sites, the fix imports the shared helper under an alias —
  `import { read as readValue } from "../admin/resource-utils.js";` — and
  removes the local definition, so all call sites keep working unmodified.
- **Left alone (not functionally identical)**: `components/smart-search.js`
  has its own local `read(row, ...keys)` that omits the PascalCase fallback
  entirely — a genuinely different (simpler) implementation, not a copy of
  the canonical one. Consolidating it would change behavior for any API
  response using PascalCase keys, so it was intentionally left as-is.
- **Left alone (different helpers, different purpose)**:
  `core/formatters.js`'s private `readField`/`pickFirst` and
  `admin/resource-utils.js`'s own `rowTitle`/`rowSubtitle` were reviewed but
  not merged into this consolidation — they serve different call sites
  (`formatters.js` internals, generic Management CRUD row rendering
  respectively) and merging them was out of scope for a same-signature
  duplicate cleanup.

After the change, all 113 `wwwroot/js/**/*.js` files were verified with
`node --check` (syntax), a custom import-resolution + named-export scan (360
relative imports across 789 named bindings, all resolving), and a
`dotnet build` of `SpareParts.Web.React.csproj`, all passing.

---

## 9. Styling

- `styles.css` — base/reset and shared layout styling.
- `css/cockpit.css` — the admin "cockpit" design system (sidebar, topbar,
  cards, tables, forms, kanban lanes, badges, etc.) used by every
  authenticated staff screen.
- `css/storefront-apex.css` — the customer-facing "Apex Motorsport"
  storefront theme, separate from the cockpit styling and from the
  `wpfThemes` CSS-variable theme system (storefront branding is fixed, not
  user-selectable).
- Runtime theme switching (`wpfThemes` in `core/config.js`) works by writing
  CSS custom properties onto `:root` (`applyWebTheme` in
  `core/formatters.js`); `cockpit.css` consumes those variables, so no CSS
  file swap is needed to change themes.

---

## 10. Where to look for common tasks

| Task | Start here |
|---|---|
| Add a new admin screen | `core/config.js` (`featureModules` entry) → new file in `views/` → register in `views/screen-registry.js` → add to a group in `components/layout.js`'s `navGroups` |
| Add a new CRUD resource to Management | `admin/crud-config.js` (new `crudConfigs` entry) → `core/config.js` (`managementSections` entry) |
| Change API base URL behavior | `core/config.js`, `wwwroot/config.js` / `config.staging.js` |
| Add a translation string | `core/i18n.js` `dictionaries.en/ar/fr` |
| Add a new theme | `core/config.js` `wpfThemes` array |
| Debug a failed API call | `core/api-client.js` (`ApiError`, 401/403 handling) |
| Change storefront UI | `views/storefront-view.js`, `css/storefront-apex.css` |
| Change sidebar grouping | `components/layout.js` `navGroups` |
| Add a currency-aware money display | `core/formatters.js` `displayCurrencyContext`/`displayMoneyFromBase` |
