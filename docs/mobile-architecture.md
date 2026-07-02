# SpareParts Mobile App — Architecture Reference

Scope: `src/SpareParts.Mobile.ReactNative/`. This document is a from-scratch reference for a
new engineer onboarding onto the Expo / React Native mobile app. It covers the app shell and
navigation model, the screen-registry pattern, the role/permission system, the shared component
library, theming, and a feature-by-feature map of every screen. It reflects the code as of the
Round 2 audit (2026-07-02).

Stack: Expo ~54, React 19.1, React Native 0.81.5. No TypeScript, no react-navigation — the app
implements its own lightweight "app shell" navigation over plain component swapping. All modules
use CommonJS (`require`/`module.exports`), not ES `import`, and JSX is not used — UI is built with
`React.createElement` (aliased to `el`) throughout.

---

## 1. High-Level Architecture

```
App.js                                  Expo entry point → requires src/app-shell.js
src/
  app-shell.js                          The entire app shell: boot, auth gate, layout, routing state
  core/                                 Low-level infrastructure (no React, no UI)
    api-client.js                      Fetch wrapper, ApiError, pagination
    app-config.js                      Storage keys, theme defs, feature module catalog, nav groups
    billing-labels.js                  Shared FEATURE_LABELS / LIMIT_LABELS (see §7)
    communication-payload-factory.js   Builders for WhatsApp/SMS payloads
    formatters.js                      Money/date/currency/row-shape helpers
    i18n.js                            en/ar/fr dictionaries + translator factory
    role-policy.js                     Role checks or screen-visibility gating
    screen-registry.js                 ScreenRegistry class (key → component resolution)
    session-store.js                   AsyncStorage-backed session persistence
  admin/
    crud-config.js                     Per-resource field schemas for generic CRUD screens
    resource-utils.js                  Row-id, form-casting, payload-building, search-matching helpers
  services/
    resource-service.js                ResourceService / CrudResourceService (OOP wrapper over ApiClient)
    accounting-service.js              AccountingService (ledger/trial-balance/statement calls)
  components/                          Reusable UI, no screen state, no direct API calls
    app-sidebar.js, bottom-tab-bar.js, phone-header-bar.js, smart-search.js, locked-feature-modal.js
    ui.js                              Original "core" UI kit (Field, Panel, ListRow, buttons, etc.)
    ui/                                Newer UI kit (Card, SectionHeader, StatusPill, BottomSheet, etc.)
    admin/crud-workspace.js            Generic CRUD list/editor panels + workspace launcher
  screens/                             Orchestration only (wire services + state + components)
    screen-registry.js                 The ~70-entry registry (imports every screen/module)
    module-screen.js                   Generic data-driven screen used by ~60 of the ~70 entries
    <feature>-screen.js                Purpose-built screens for the ~10 non-generic features
  theme/
    theme-context.js                   React context + useTheme() hook
    styles.js                          createPalette()/createStyles() — one big StyleSheet factory
assets/                                 Fonts, images (login hero, etc.)
```

### Boot sequence (`App.js` → `src/app-shell.js`)

1. `App.js` just re-exports `AppContent`-wrapping `App()` from `app-shell.js`.
2. `App()` wraps everything in `SafeAreaProvider`.
3. `AppContent()`:
   - Loads Oswald fonts via `expo-font` (`useFonts`). Renders nothing until loaded.
   - On mount, calls `sessionStore.load()` (an instance of `MobileSessionStore`, backed by
     `AsyncStorage`) to restore `apiBaseUrl`, `token`, `user`, `themeKey`, `languageKey`.
   - While restoring, shows a boot screen (`ActivityIndicator` + "Starting Maalouf Auto Parts").
   - If no `token`/`user`, renders `LoginScreen`.
   - Once authenticated, renders the main app frame: optional `AppSidebar`, a `contentPane` with
     `PhoneHeaderBar` (phone width only), `SmartSearch` (admin/staff mode only), the active screen,
     and `BottomTabBar`.
   - A `ThemeContext.Provider` wraps all of the above, supplying `{ palette, styles, t, isRtl,
     textDirection, languageKey }` to every descendant via `useTheme()`.

### Two operating modes: staff app vs. customer storefront

`app-shell.js` branches its entire rendering based on `isCustomerMode = isWebAppUser(user)`
(`core/role-policy.js`, role id `4` = `webAppRoleId` from `app-config.js`):

- **Staff/admin mode** (any other role): renders the full sidebar + ~70-screen registry navigation,
  `SmartSearch`, and whichever screen is resolved via `screenRegistry.resolve(activeScreen.key)`.
- **Customer mode** (role id 4): renders `CustomerStorefrontScreen` directly (bypassing the
  screen registry entirely) with a fixed 5-tab bottom bar (`customerTabs` in `app-shell.js`:
  `store-home`, `store-parts`, `store-cart`, `store-checkout`, `store-account`). No sidebar, no
  admin navigation, no SmartSearch.

This means the mobile app is really two different navigation trees sharing one shell, one theme
system, and one `ApiClient` instance.

### State owned by `app-shell.js`

`apiBaseUrl`, `token`, `user`, `activeTab` (the routing state — just a string key, no navigation
stack/history), `themeKey`, `languageKey`, `isSidebarOpen`, `isBooting`, `planLock` (subscription
plan-lock modal state, populated by `ApiClient`'s `onPlanLocked` callback on 403 responses with an
`errorCode`).

Layout responsiveness: `isWideLayout = width >= 820`. On wide layouts the sidebar renders inline
and stays open by default; on narrow layouts it renders as an overlay with a backdrop and closes
automatically after selecting a screen.

---

## 2. Navigation Model — No Stack, Just a Key

There is no `react-navigation`, no navigation stack, no deep linking, no back button/gesture
handling. "Navigation" is simply:

```js
const [activeTab, setActiveTab] = useState("dashboard");
// ...
const selectScreen = useCallback((nextTab) => {
  setActiveTab(nextTab);
  if (!isWideLayout) setIsSidebarOpen(false);
}, [isWideLayout]);
```

`selectScreen` (passed down as `onSelect`/`onNavigate`) is the single mechanism every component
uses to change screens — sidebar items, bottom tabs, `SmartSearch` results, dashboard action
buttons, the "billing" screen redirect from the locked-feature modal, etc. There is no history
stack, so there is no programmatic "back" — screens either navigate forward to another `activeTab`
key or rely on modals/bottom sheets for transient detail views (see `BottomSheet` in §6).

`activeScreen` is derived, not stored directly:

```js
const activeScreen = isCustomerMode
  ? visibleBottomTabs.find(item => item.key === activeTab) || visibleBottomTabs[0]
  : visibleAdminScreens.find(item => item.key === activeTab) || bottomTabs[0];
```

`activeBottomKey` reconciles which bottom-tab icon should highlight — if the active screen isn't
one of the 5 fixed bottom tabs (e.g. you're on "Accounting", reached via the sidebar), the bottom
bar falls back to highlighting "management" as the closest umbrella tab.

---

## 3. The Screen-Registry Pattern

### `src/core/screen-registry.js` — the resolution engine

```js
class ScreenRegistry {
  constructor(items) {
    this.items = items;                                   // ordered list, drives sidebar grouping
    this.componentMap = new Map(items.map(i => [i.key, i.component]));
    this.aliases = new Map([["inventory", "parts"], ["parts", "inventory"]]);
  }
  resolve(key) {
    const normalizedKey = this.componentMap.has(key) ? key : this.aliases.get(key);
    return this.componentMap.get(normalizedKey) || this.componentMap.get(this.items[0].key);
  }
}
```

Falls back to the **first registered item** (`dashboard`) if a key is unknown — there is no
"screen not found" error screen.

### `src/screens/screen-registry.js` — the actual ~70-entry table

This file constructs the single `screenRegistry` instance consumed by `app-shell.js`. Each entry
is `{ key, label, component }`. There are two families of entries:

1. **Purpose-built screens** (~10): imported directly, e.g.
   `{ key: "dashboard", label: "Dashboard", component: DashboardScreen }`.
2. **Generic module screens** (~60): built via `createModuleScreen(moduleByKey(key))`, where
   `moduleByKey` looks up a declarative descriptor from `featureModules` in `core/app-config.js`
   (title, API endpoint(s), `capabilities` bullet list, optional `commandOnly` flag) and
   `createModuleScreen` wraps `ModuleScreen` (see §3.2) with that descriptor bound in.

Every module in `featureModules` (in `app-config.js`) does **not** need a matching registry entry
— `featureModules` is the superset of "things the WPF/web platform can do"; `screenRegistry` is
the subset actually wired into a mobile route. As of this audit every `featureModules` entry has a
matching registry key (checked by cross-referencing both lists).

`navigationGroups` (also in `app-config.js`) is a separate, purely cosmetic table that buckets
screen keys into sidebar sections: Core, Operations, Finance, Tools, Marketplace, Intelligence,
Platform Admin. `AppSidebar` builds its grouped, collapsible list by joining `navigationGroups`
against the *visible* subset of `screenRegistry.items` (any screen key not found in any group
falls into an ad-hoc "Other" bucket).

### 3.1 Registering a new screen — the pattern to follow

To add a genuinely new, custom screen:
1. Create `src/screens/my-feature-screen.js` exporting a named component.
2. Import it in `src/screens/screen-registry.js` and add `{ key, label, component: MyFeatureScreen }`
   to the array.
3. Add the same `key` to a `navigationGroups` bucket in `core/app-config.js` (otherwise it lands in
   "Other").
4. If it should be in the bottom tab bar, add its key to `bottomTabKeys` in `app-shell.js`.
5. If it's staff-only or needs a specific role, extend `core/role-policy.js`.

To add a screen backed only by an existing generic API shape (list + optional detail sheet), it's
usually enough to add an entry to `featureModules` in `app-config.js` and reference it via
`createModuleScreen(moduleByKey("my-key"))` in the registry — no new screen file needed at all.
This is how the bulk of the ~70 screens (Escrow, Loyalty, Warranty, QR Tag System, etc.) are
implemented.

### 3.2 `module-screen.js` — the generic-screen workhorse

`ModuleScreen({ api, module, onNavigate })` is a big switch:

- `module.key === "business-assistant"` → renders `BusinessAssistantModuleScreen` (chat-style Q&A
  against `/api/business-assistant/ask`, plus a starter-action grid and insights panel).
- `module.key === "report-builder"` → `ReportBuilderModuleScreen` (schema explorer: pick table,
  pick columns, run, save/load saved reports and background runs).
- `module.key === "ar"` → `ScanLookupModuleScreen` (barcode/text scan resolution against
  `/api/scans/resolve`, plus camera/gallery-driven visual search against
  `/api/scans/visual-search`, with tappable pins over the captured photo).
- `module.key === "part-requests"` → `PartRequestsModuleScreen` (reservation clock UI: reserve /
  remind-staff / release / mark-fulfilled against `/api/partrequests/*`).
- `module.key === "used-car-wholesale"` → `UsedCarWholesaleModuleScreen` (bespoke as-is sale form
  with repair-item line items and projected P/L).
- **Default path** (everything else, i.e. the majority): a single generic list+detail screen.
  - `canPreview` is `false` when `module.commandOnly` is set, or the endpoint ends in `/ask` or
    `/resolve` — those modules render as "mapped to a command endpoint" with no preview list.
  - Otherwise it calls `loadModuleRows(api, module.previewEndpoint || module.endpoint)` (which
    auto-paginates if the endpoint is `/api/parts`) and renders each row as a `PartListCard`
    (image, title via `rowTitle()`, subtitle via `rowSubtitle()`, price/amount via `rowAmount()` —
    all generic row-shape guessers from `core/formatters.js`).
  - Tapping a row opens a `BottomSheet` showing every non-object, non-image field on the row as a
    `ListRow`, plus the row's image if `rowImageUri()` finds one.
  - A `StickyActionBar` with a single "Close" action accompanies the open sheet.

`createModuleScreen(module)` just returns a small wrapper component so each registry entry gets
its own component identity while sharing the same underlying logic and `module` descriptor.

### 3.3 `management-screen.js` — the generic CRUD workhorse

Separate from `module-screen.js`. Two tabs: "Workspaces" (renders `WorkspaceLauncher`, a tile grid
linking into every visible screen — effectively a searchable app launcher) and "Records" (a
classic list+form CRUD screen driven by `managementSections` in `app-config.js`, matched against
`crudConfigs` in `admin/crud-config.js` by section key).

- If a section has a `crudConfigs` entry, `CrudResourceService` handles list/save/remove against
  `config.basePath`, with per-field `type`/`required`/`readOnly`/`create`/`update` rules used by
  `admin/resource-utils.js` (`buildPayload`, `formFromRow`, `emptyForm`, `matchesRow`, `rowId`) to
  cast form values into API payloads.
- If a section has no `crudConfigs` entry (e.g. `part-requests`, most reference tables), it falls
  back to a bare `ResourceService` (list only) — `crudConfig` stays `null` and the editor panel
  renders a read-only/browse-only message instead of a save form.

---

## 4. Role / Permission System (`core/role-policy.js`)

Deliberately minimal — there is no granular per-feature permission matrix on the mobile client
(that logic lives server-side; the mobile app only gates *navigation visibility*, not API access):

```js
const SUPER_ADMIN_ROLE_ID = 5;
const superAdminOnlyScreenKeys = new Set(["admin-billing"]);

function isWebAppUser(user)     // roleId === webAppRoleId (4)  → customer storefront mode
function isSuperAdmin(user)     // roleId === 5                 → can see admin-billing
function isScreenVisible(key, user)
  // true unless key is superAdminOnlyScreenKeys and user is NOT super admin
```

`roleId` is read defensively across four possible casings: `user.roleId ?? user.RoleId ??
user.roleID ?? user.role_id` — a symptom of the API sometimes returning PascalCase and the client
needing to tolerate both.

Consumers:
- `app-shell.js`: filters `screenRegistry.items` down to `visibleAdminScreens` via
  `isScreenVisible`, and switches the entire render tree via `isWebAppUser`.
- `app-sidebar.js`: re-filters the screens it's given (defense in depth) before grouping.
- `admin-billing-screen.js`: does its own explicit `roleId !== SUPER_ADMIN_ROLE_ID` check and
  renders a "forbidden" message body instead of the real content — this is a **UI-only** gate; the
  actual authorization boundary is the API's own `[Authorize]`/role checks server-side.

There is exactly one role-gated screen today: `admin-billing` (Pricing & Subscriptions), visible
only to `SUPER_ADMIN_ROLE_ID` (5). Everything else is visible to any authenticated non-customer
user; feature-level entitlement (subscription plan limits/features) is handled separately via the
`LockedFeatureModal` + `ApiClient.onPlanLocked` flow (triggered by 403 responses carrying an
`errorCode`, `currentPlan`, `requiredPlans`, `upgradeUrl`), not by `role-policy.js`.

---

## 5. Core Services Layer

### `core/api-client.js`

`ApiClient(apiBaseUrl, token, onUnauthorized, onPlanLocked)` — a thin `fetch` wrapper.

- Always sends `Accept: application/json` and `X-Client-Platform: mobile`; adds
  `Authorization: Bearer <token>` when present; sets `Content-Type: application/json` unless the
  body is `FormData` (file/photo uploads use `postForm`).
- `401` responses call `onUnauthorized()` (wired to `clearSession` in `app-shell.js`, which drops
  the app back to `LoginScreen`) and throw a generic "Session expired" `ApiError`.
- `403` responses with an `errorCode` in the JSON body call `onPlanLocked(error)` (wired to
  `handlePlanLocked` in `app-shell.js`, which opens `LockedFeatureModal`).
- `list(path)` auto-detects `/api/parts` collection requests and transparently paginates via
  `getAllPages` (walks `page`/`pageSize` query params, reads `X-Total-Count`/`X-Page-Size`
  response headers, stops when a page is short or the running total meets the header count) —
  every other endpoint just does a single `get`. This exists because the parts catalog can exceed
  a single page and several screens (`MechanicModeScreen`, `PartCompatibilityScreen`, dead code
  `PartsScreen`) need the *full* set client-side for local filtering.
- `ApiError` carries `status`, `errorCode`, `currentPlan`, `requiredPlans`, `upgradeUrl` for the
  plan-lock flow to consume.

### `core/formatters.js`

Grab-bag of pure functions used everywhere: `money()`, `shortDate()`/`shortDateTime()`,
`initials()`, `normalizeBaseUrl/ThemeKey/LanguageKey()`, and a family of generic "guess the shape
of an unknown API row" helpers (`rowTitle`, `rowSubtitle`, `rowAmount`, `pickFirst`, `readField`)
that power `module-screen.js`'s generic list rendering — they try a prioritized list of common
field names (`name`, `invoiceNumber`, `customerName`, etc.) so one component can render wildly
different resource shapes. Also owns multi-currency display-conversion helpers
(`displayCurrencyContext`, `convertBaseToDisplay`, `convertCounterToDisplay`,
`displayMoneyFromBase/Counter`) used by dashboard, accounting, and repair-prep screens.

### `core/session-store.js`

`MobileSessionStore` wraps `AsyncStorage` under fixed keys (`core/app-config.js`
`storageKeys`). Persists API base URL, JWT token, the full login response as `user`, theme key,
language key. Has a "packaged API base URL" reset behavior: if the app was built with
`EXPO_PUBLIC_API_BASE_URL` baked in and the stored URL still points at a local dev host
(`localhost`/`127.0.0.1`/`10.0.2.2`), it silently overwrites the stored URL with the packaged one
on next boot — a safety net so production builds don't get stuck pointed at a developer's machine.

### `core/role-policy.js`, `core/i18n.js`

Covered in §4 and below (theming/i18n section is folded into §7 since language and theme travel
together through the same context).

### `core/communication-payload-factory.js`

Static factory building WhatsApp/SMS send payloads (`salesInvoice`, `paymentReminder`,
`partAvailability`, `freeText`) against the `CommunicationChannel`/`CommunicationRecipientKind`/
`CommunicationTemplateKey` enums in `app-config.js`. Used by `InvoicesScreen`,
`MechanicModeScreen`, `WhatsAppScreen`, `parts-screen.js` (dead code).

### `services/resource-service.js`, `services/accounting-service.js`

Thin OOP wrappers over `ApiClient` for two specific call patterns: generic CRUD resources
(`ResourceService`/`CrudResourceService`, used exclusively by `management-screen.js`) and
accounting report queries (`AccountingService`, used by `accounting-screen.js`). Per
`ARCHITECTURE.md`, new services should only be added once a call pattern repeats in ≥2 places.

### `admin/crud-config.js`, `admin/resource-utils.js`

`crud-config.js` declares field schemas (label, type, required/optional, keyboard type, create/
update visibility) per management section (`customers`, `suppliers`, `parts`, `users`,
`warehouses`, etc.). `resource-utils.js` turns those schemas into form defaults (`emptyForm`),
row→form hydration (`formFromRow`), form→API payload casting (`buildPayload`, respects
`field.type`, `optional`, `readOnly`, per-create/update key overrides), and a generic
substring search matcher (`matchesRow`) tried against a fixed list of common field names.

---

## 6. Shared Component Library

Two parallel UI kits coexist (an older flat one and a newer folder-based one — both still in
active use, not a deprecated/replacement pair):

### `components/ui.js` — the original kit

Exports: `Field`, `PrimaryButton`, `SecondaryButton`, `ScreenScroll`, `ScreenHeader`, `StatusText`,
`Panel`, `ListRow`, `EmptyState`. This is the workhorse used by nearly every purpose-built screen
(`billing-screen.js`, `dashboard-screen.js`, `invoices-screen.js`, `mechanic-mode-screen.js`,
`management-screen.js`, `module-screen.js`, etc.) for form fields, section panels, and simple list
rows. All of them call `useTheme()` internally to pull `styles`/`palette` — callers never pass
style props directly, they just supply content/behavior props.

### `components/ui/` — the newer kit (`index.js` re-exports all of these)

`Card` (elevated tappable/static surface), `SectionHeader` (title+subtitle+optional action),
`StatusPill` (colored status/tone badge), `EmptyState` (a second, slightly different empty-state
component — note: **there are two `EmptyState` implementations**, one in `ui.js` and one in
`ui/EmptyState.js`; screens import whichever one they need explicitly by path), `SearchBar`,
`FilterChips`, `LoadingSkeleton`/`ShimmerBlock`, `StickyActionBar`/`StickyActionBarSpacer`
(bottom-pinned action row, safe-area aware, used for sheet/detail confirm flows),
`BottomSheet` (Animated + `PanResponder`-driven slide-up modal — no `reanimated`/
`gesture-handler` dependency; drag-to-dismiss with velocity/threshold detection),
`ImageGallery`, `PartListCard` (image + title + subtitle + price + status badges — the row
renderer used by `ModuleScreen`'s generic preview list), `VehicleListCard`.

### `components/admin/crud-workspace.js`

`CrudField` (renders a `Switch` for boolean fields, `TextInput` otherwise, respecting
`readOnly`/`required`), `CrudRow`, `ResourceListPanel`, `ResourceEditorPanel`, `WorkspaceLauncher`
(the tile grid used by Management → Workspaces tab, driven by `navigationGroups` + `launchMeta()`
from `resource-utils.js` for short badge text per screen key).

### App-shell-level components (not generic — one call site each, live in `components/` root)

- `app-sidebar.js` — `AppSidebar`: searchable, collapsible-by-group nav list + inline theme
  switcher + user panel with sign-out. Re-filters visible screens via `isScreenVisible` (defense
  in depth against the parent already having filtered).
- `bottom-tab-bar.js` — `BottomTabBar`: hand-drawn SVG icon set (`TabIcon`, using
  `react-native-svg` primitives, no icon font/library dependency) plus an animated active-tab
  underline indicator (`TabIndicator`, `Animated.spring`-driven scaleX).
- `phone-header-bar.js` — `PhoneHeaderBar`: minimal breadcrumb/hamburger shown only on narrow
  layouts (sidebar hidden).
- `smart-search.js` — `SmartSearch`: debounced (240ms) global search hitting `/api/search`,
  grouped by `section`, tapping a result calls `onNavigate(result.targetView)`. Detects
  401/403-shaped errors heuristically (status code or message regex) and proactively calls
  `api.onUnauthorized()` even though `ApiClient` would normally handle 401 itself — a
  belt-and-braces guard specific to this component.
- `locked-feature-modal.js` — `LockedFeatureModal`: renders the plan-lock/upgrade prompt
  triggered by `ApiClient.onPlanLocked`; "View Plans" navigates to the `billing` screen key.

---

## 7. Theming (`useTheme()` pattern)

### `theme/theme-context.js`

```js
const ThemeContext = React.createContext(defaultThemeBundle);
function useTheme() { return React.useContext(ThemeContext); }
```

The context value (the "theme bundle") is `{ palette, styles, languageKey, isRtl, textDirection,
t }`, computed once per render in `app-shell.js`:

```js
const themeBundle = useMemo(() => {
  const nextPalette = createPalette(themeKey);
  const isRtl = isRtlLanguage(languageKey);
  return {
    palette: nextPalette,
    styles: createStyles(nextPalette),
    languageKey, isRtl,
    textDirection: isRtl ? "rtl" : "ltr",
    t: createTranslator(languageKey)
  };
}, [languageKey, themeKey]);
```

Every component in the tree calls `const { styles, palette, t } = useTheme();` at the top and
pulls named style keys off the single generated `StyleSheet` — there is no per-component
`StyleSheet.create` scattered around the codebase; `theme/styles.js` is one large factory
(~6,400+ lines) producing every style key used anywhere in the app, keyed by feature/component
prefix (e.g. `partsMetricGrid`, `billingPlanCard`, `mechanicHero`, `usedCarListingOverlay`,
`smartSearchPanel`, `bottomTabBar`, `sideNavItem`, `loginPanel`, `adminCrudField`, `uiCard`,
`uiSheetBackdrop`, `uiStickyBar...`). Adding a new visual treatment means adding new keys to this
factory, not writing a local stylesheet in the screen file.

### `core/app-config.js` — `wpfThemes`

Nine themes shared conceptually with the WPF desktop app's naming (`apex`, `aurora`, `carbon`,
`amg`, `bmw-m`, `lambo`, `neon-glow`, `porsche-rs` — 8 listed keys, default is `apex`). Each theme
is a flat color token object (`bg`, `surface`, `surface2`, `sidebar`, `input`, `line`, `text`,
`muted`, `soft`, `accent`, `accentViolet`?, `accent2`?, `whatsapp`, `danger`).
`createPalette(themeKey)` in `theme/styles.js` just looks up `themeMap.get(normalizeThemeKey
(themeKey)).colors`; `normalizeThemeKey` falls back to `defaultThemeKey` ("apex") for any unknown
key. Theme selection is persisted via `sessionStore.saveTheme()` and surfaced in both
`AppSidebar` (inline grid) and `SettingsScreen`.

### i18n (`core/i18n.js`)

Three languages: `en` (default, no dictionary needed — `t(key, fallback)` just returns `fallback`
when no translation exists), `ar` (RTL), `fr`. `createTranslator(languageKey)` returns a
`t(key, fallback, params?)` function doing dotted-path lookup into a nested dictionary object with
`{param}`-style interpolation. `isRtlLanguage()` drives `textDirection`/`isRtl` in the theme
bundle, though RTL layout mirroring is largely left to React Native's platform-level RTL support
rather than manual per-component flipping. Every screen's user-facing strings go through `t()`
with an English fallback as the second argument — so even untranslated keys degrade gracefully.

---

## 8. Feature-by-Feature Screen Map

### 8.1 Purpose-built screens (custom logic, not generic `ModuleScreen`)

| Screen key | File | Summary |
|---|---|---|
| (pre-auth) | `login-screen.js` | Username/password login, Google/Facebook OAuth (`expo-auth-session`), API base URL field, animated hero background. Calls `/api/auth/login` or `/api/auth/external-login`. |
| `dashboard` | `dashboard-screen.js` | Owner cockpit: KPI tiles, action queue (built from dashboard alerts + failed messages + currency margin rows via `buildActionQueue`), profit/loss panel, SVG-charted heatmap tiles, recent communications. Largest single screen file (~1000+ lines). |
| `invoices` | `invoices-screen.js` | POS/Sales invoice list, send-invoice / payment-reminder via WhatsApp (`CommunicationPayloadFactory`). |
| `parts` | `mechanic-mode-screen.js` (`MechanicModeScreen`) | The **live** parts workflow: scan/resolve a code, search parts, view live stock by warehouse, reserve stock against a part request (`AutoRelease`/`StaffReminder` expiration actions), take & send a part photo to a customer, create a quick part request. This is what the `"parts"` registry key actually renders — see §9.2 for the dead-code sibling. |
| `compatibility` | `part-compatibility-screen.js` | SVG-rendered part↔vehicle fitment graph; matches parts against used-car records by OEM/brand/model heuristics; supports selecting a part and visually exploring compatible donor cars. |
| `contacts` | `contacts-screen.js` | Read-only customer + supplier list with balances. |
| `management` | `management-screen.js` | Workspace launcher + generic CRUD browser (see §3.3). |
| `billing` | `billing-screen.js` | Tenant's own subscription: current plan, usage/limits, features, available plans with upgrade/downgrade/trial/cancel actions, payment history, invoices. Calls `/api/subscription/*`, `/api/pricing/packages`, `/api/payments/history`, `/api/invoices`. |
| `admin-billing` | `admin-billing-screen.js` | Super-admin-only (role 5) platform billing console: package editor (features/limits toggles), tenant subscription activation, payments (mark-paid), invoices, webhook event log. Tabs: Packages / Subscriptions / Payments / Invoices / Webhook Events. |
| `settings` | `settings-screen.js` | Theme picker, language picker, sign-out confirmation. |
| `used-cars` | `used-cars-screen.js` | Used-car inventory with photo gallery (pinch-zoom via `PanResponder`), linked-parts assignment, create/edit/delete. Largest secondary screen file. |
| `repair-prep` | `repair-prep-screen.js` | Kanban-style repair/prep board (persisted locally in `AsyncStorage` under a versioned key) layered over `/api/usedcars` data, with per-car task checklists and cost tracking. |
| `dead-stock` | `dead-stock-screen.js` | Dormant-stock report with configurable dormant-days/row-count thresholds against `/api/parts/dead-stock`. |
| `accounting` | `accounting-screen.js` | Trial balance / ledger / statement-of-account viewer with PDF export (`expo-print` + `expo-sharing`), multi-currency display conversion. |
| `whatsapp` | `whatsapp-screen.js` | Conversation list + thread view + free-text compose, plus WhatsApp campaign preview/send against `/api/communications/campaigns/*`. |

Plus `customer-storefront-screen.js` (`CustomerStorefrontScreen`) — rendered directly by
`app-shell.js` for customer-role users, **not** through the screen registry at all. Implements the
5-tab customer shopping flow: browse/search catalog (`store-parts`), cart with quantity stepper
(`store-cart`), checkout with shipping address + payment gateway reference fields
(`store-checkout`, posts to `/api/web-catalog/checkout`), and a home/account tab. See §9.3 for the
cart-quantity clamp finding.

### 8.2 Generic module screens (via `createModuleScreen` + `featureModules` descriptor)

The remaining ~55 registry keys are generic `ModuleScreen` instances, each described purely by its
`featureModules` entry (title, endpoint(s), capability bullets, optional `commandOnly`). Grouped by
`navigationGroups` bucket:

- **Operations**: `sales-returns`, `part-passport`, `part-requests`* , `purchase-parts`,
  `used-car-purchases`, `used-car-wholesale`*, `stock-arrival`, `car-twin`, `stock`, `reorder`,
  `expiry-alerts`, `loyalty`, `warranty`, `shipments`, `mechanic-desk`, `garage-stock`,
  `part-reserve`, `part-reel`, `halfcut`, `escrow`, `listing-boost`, `live-inspection`,
  `part-genealogy`, `yard-tour`, `instant-offer`, `part-insurance` (`*` = has a bespoke
  sub-component inside `module-screen.js`, see §3.2).
- **Finance**: `manual-journal`, `report-builder`*, `quotes`, `customer-aging`, `supplier-aging`,
  `market-price`, `price-genius`, `price-report`, `new-vs-used`.
- **Tools**: `whatsapp-selling`, `business-assistant`*, `ar`* (scan/visual search), `activity-log`,
  `voice-search`, `symptom-search`, `does-it-fit`, `condition-scanner`, `qr-tag`, `ar-finder`,
  `api-platform`, `kareem`.
- **Marketplace**: `my-garage`, `needboard`, `watchlist`, `seller-reputation`,
  `seller-verification`, `community-guard`, `referral`, `regional-demand`, `mechanic-trust`.
- **Intelligence**: `car-crush`, `dismantler-forecast`, `negotiation`.

Modules flagged `commandOnly: true` in `app-config.js` (e.g. `symptom-search`, `voice-search`,
`does-it-fit`, `price-genius`, `condition-scanner`, `qr-tag`, `part-genealogy`,
`dismantler-forecast`, `new-vs-used`, `kareem`, `ar-finder`, `price-report`,
`car-crush`, `whatsapp-selling`) render with no preview list — `ModuleScreen` shows the module's
capability summary and a "mapped to a command endpoint" status message instead, because these
correspond to POST/action-style API endpoints rather than browsable collections.

---

## 9. Known Issues / Findings (carried from Round 1 and re-verified in Round 2)

### 9.1 Fixed in Round 2: cross-screen import of billing labels

`admin-billing-screen.js` used to import `FEATURE_LABELS`/`LIMIT_LABELS` from `billing-screen.js`
— a screen importing from another screen, violating `ARCHITECTURE.md`'s "screens: orchestration
only" layering rule. Both maps now live in `core/billing-labels.js` and both screens import from
there. See the Round 2 report for the file diff.

### 9.2 Still open: `screens/parts-screen.js` is dead code

`parts-screen.js` exports a fully-built `PartsScreen` component (metrics grid, demand-matching
against active part requests, listing-package generation modal, WhatsApp share, filters by status/
condition/price). **It is not imported by `screens/screen-registry.js` or anywhere else in the
codebase** — the `"parts"` registry key resolves to `MechanicModeScreen` instead (confirmed via
repo-wide search: the only references to `PartsScreen` are its own function declaration and
`module.exports` line in the file itself). This duplicates functionality already covered (in a
different, scan/reserve-oriented UX) by `MechanicModeScreen`. Left in place per Ralph's standing
instruction that file deletion requires explicit approval — flagged here as dead code awaiting a
delete decision, not touched in Round 2.

### 9.3 Still open (product decision, not a bug): cart quantity clamp in customer storefront

In `customer-storefront-screen.js`, both the "add to cart" and "increment quantity" cart mutators
clamp the max quantity to `part.availableQuantity || 99`:

```js
// addToCart
{ ...item, quantity: Math.min(item.quantity + 1, part.availableQuantity || 99), part }
// updateQuantity
const available = item.part?.availableQuantity || 99;
```

Because `||` treats `0` as falsy, a part with `availableQuantity === 0` (out of stock) falls back
to a cap of `99` instead of `0` — a customer could add up to 99 units of a part with zero recorded
stock. This was flagged in Round 1 as a business-rule question (is 0-stock supposed to be
purchasable/backorderable, or should the cap be 0?) rather than a clear-cut bug, and is
re-confirmed unchanged in Round 2. No code change made — left for Ralph's product decision.

---

## 10. Quick Reference — "Where do I…"

- **…add a brand-new custom screen?** New file in `screens/`, register it in
  `screens/screen-registry.js`, add its key to a `navigationGroups` bucket in `core/app-config.js`.
- **…add a screen backed by an existing REST-ish endpoint with no special UX?** Add an entry to
  `featureModules` in `core/app-config.js`, then `createModuleScreen(moduleByKey("key"))` in the
  registry — no new file needed.
- **…add a field to a Management CRUD resource?** Edit the resource's `fields` array in
  `admin/crud-config.js`.
- **…change a color/theme token?** `core/app-config.js` → `wpfThemes`.
- **…add a new reusable style?** `theme/styles.js` (`createStyles`) — do not create local
  `StyleSheet.create` calls inside screens/components.
- **…add a translated string?** `core/i18n.js` dictionaries (`en` fallback lives inline at each
  `t(key, fallback)` call site, so `en` dictionary entries are optional but `ar`/`fr` should be
  added for parity).
- **…gate a screen by role?** `core/role-policy.js` — extend `superAdminOnlyScreenKeys` or add a
  new predicate, then consult it from `isScreenVisible` / `app-shell.js`'s `visibleAdminScreens`.
- **…call the API?** Through the `api` prop threaded into every screen (an `ApiClient` instance
  created once in `app-shell.js`). Use `api.list()` for anything that might paginate (currently
  auto-detected only for `/api/parts`), `api.get/post/put/postForm/delete` otherwise.
