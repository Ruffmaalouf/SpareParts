# SpareParts WPF Desktop — Architecture Reference

Audience: a new engineer who has never opened this codebase. This document explains how the
desktop app boots, how MVVM is wired end-to-end, the command/theme infrastructure, and what
every major screen/ViewModel does. It documents **current reality only** — no design changes
are proposed or implied here (theme consolidation is a pending decision for Ralph).

Last updated: Round 2 of the WPF audit (2026-07-02).

---

## 1. Project layout and dependency graph

The desktop app is split across five projects (a sixth, `SpareParts.Desktop.Abstractions`,
holds cross-cutting service interfaces used by both Controls and the Wpf host). This is a
layered MVVM split: models/state at the bottom, UI shell at the top.

```
SpareParts.Desktop.Abstractions   (no project refs — pure interfaces: dialogs, AR bridge, workspace requests)
        ^
SpareParts.Desktop.Interfaces     (-> Domain)                     API client & auth interfaces (I*ApiClient, IApiTokenProvider...)
        ^
SpareParts.Desktop.Helpers        (-> Interfaces, Domain)         RelayCommand, ServiceLocator, ThemeManager, API client
        ^                                                          implementations, RestClientFactory, AppSettings, notifications
SpareParts.Desktop.Controls       (-> Interfaces, Helpers, Domain) Small reusable WPF UserControls (search boxes, pickers,
        ^                                                          dialogs) — compiled as their own WPF class library
SpareParts.Desktop.ViewModels     (-> Application, Abstractions, Interfaces, Domain, Helpers, Controls)
        ^                                                          All ViewModels — the bulk of app logic lives here
SpareParts.Desktop.Wpf            (-> Abstractions, Interfaces, ViewModels, Domain, Helpers, Controls)
                                                                    Windows, feature UserControls (XAML "pages"), themes,
                                                                    App.xaml / composition root, DI registration
```

Solution files at repo root map to slices of this graph for isolated review:
`SpareParts.Controls.sln`, `SpareParts.Helpers.sln`, `SpareParts.Interfaces.sln`,
`SpareParts.ViewModels.sln`. The full app builds via `SpareParts.sln`.

Key point: **ViewModels reference Controls**, not the other way around — a few feature
ViewModels (e.g. Excel import) reuse small display helpers that live in Controls. The WPF host
project (`SpareParts.Desktop.Wpf`) is the only project allowed to know about `Window`/App
composition; everything else is view-model or reusable-control code.

---

## 2. Application startup and composition root

Entry point: `src/SpareParts.Desktop.Wpf/App.xaml.cs` (`App : Application`).

`OnStartup`:
1. Builds a `ServiceCollection` (`ConfigureServices()`), registering:
   - Singletons: `IApiTokenProvider`, `IRestClientFactory`, `IArRenderingService`, `IArDeviceBridge`.
   - Transients: dialog/file-picker/workspace services, every `I*ApiClient` implementation,
     `LoginViewModel`, `UsersViewModel`, `RolesViewModel`, `ManagementViewModel`,
     `InvoiceTabsViewModel`, `MainWindow`, `IMainWindowFactory` → `MainWindowFactory`,
     `LoginWindow`, `ManagementWindow`.
   - **Not every ViewModel is DI-registered.** Most feature ViewModels (dashboard, repair prep,
     barcode mode, WhatsApp, etc.) are constructed directly with `new` inside
     `InvoiceTabsViewModel`'s constructor (see §5) rather than resolved from the container —
     only the small set of "top of tree" ViewModels above go through DI.
2. `Services = services.BuildServiceProvider()`.
3. `ServiceLocator.Provider = Services` — a static bridge (see §3.3) used by code that cannot
   easily take constructor injection (e.g. some XAML-driven code-behind).
4. Resolves and shows `LoginWindow`.

There is no `Startup.cs`/generic host — this is a plain WPF `Application` with a hand-rolled
`Microsoft.Extensions.DependencyInjection` container built once at startup.

### 2.1 Window lifecycle

- **`LoginWindow`** (`Windows/LoginWindow.xaml(.cs)`) — constructed via DI with
  `LoginViewModel` and `IMainWindowFactory` injected. On `LoginViewModel.LoginSucceeded`,
  it calls `_mainWindowFactory.Create()`, sets `Application.Current.MainWindow`, shows it,
  and closes itself. The close (X) button calls `Application.Current.Shutdown()` explicitly
  because the window is borderless (`Close()` alone would not end the process since it isn't
  the app's `MainWindow` at that point).
- **`IMainWindowFactory` / `MainWindowFactory`** (`Factories/IMainWindowFactory.cs`,
  `Factories/MainWindowFactory.cs`) — a thin factory (`Create() => _services.GetRequiredService<MainWindow>()`)
  that exists purely so `LoginWindow` doesn't need to reference `IServiceProvider` directly
  and so `MainWindow` creation is unit-testable/fakeable via the interface.
- **`MainWindow`** (`Windows/MainWindow.xaml(.cs)`) — the single shell window for the whole
  app once logged in. Constructor takes `InvoiceTabsViewModel` (the "god" ViewModel, see §5)
  as `DataContext`. All feature screens are `UserControl`s hosted inside `MainWindow.xaml` and
  shown/hidden via `AppScreen`-driven visibility converters (see §3.4) — **there is only one
  main window; screens are not separate `Window`s**, they are swapped content inside it.
  Code-behind handles only view-only concerns: sidebar collapse/expand animation, closing
  flyout toggle buttons, barcode-scan `Enter` key handling, invoice search double-click,
  drag-move. All business logic stays in `InvoiceTabsViewModel`.
- **`ManagementWindow`** (`Windows/ManagementWindow.xaml(.cs)`) — a secondary, separately
  DI-registered window (`ManagementViewModel` injected) used for the "Management" admin
  screens (customers/suppliers/parts/etc. CRUD). It loads its data on `Loaded` via
  `Dispatcher.BeginInvoke(..., DispatcherPriority.ContextIdle)` so the window paints before
  the (potentially heavy) `LoadAllAsync()` fires. `InvoiceTabsViewModel.IsManagementOpen`
  toggles an in-shell flyout instead in some flows — there are effectively two ways
  Management is surfaced (as a flyout panel bound to `ManagementVm` inside `MainWindow`, and
  as a standalone `ManagementWindow`); both share the same `ManagementViewModel` instance
  registered as DI singleton-per-resolve (transient, but only one is ever constructed since
  only `InvoiceTabsViewModel` and `ManagementWindow` request it and DI is per-resolve here).
- **`PartListingWindow`** (`Windows/Inventory/`), **`UsedCarGalleryWindow`**,
  **`UsedCarListingWindow`**, **`UsedCarPartsWindow`** (`Windows/UsedCars/`) — small modal/
  utility windows constructed ad hoc (not DI-registered in `App.xaml.cs`; they are `new`'d
  where needed, e.g. `PartListingWindow` takes a `ManagementCoordinator` and a
  `PartWorkspaceRequest` directly) to show a generated marketplace listing package, used-car
  photo gallery, etc.

### 2.2 Configuration

`appsettings.wpf.json` is linked into the build output as `appsettings.json`
(`CopyToOutputDirectory=PreserveNewest`). `AppSettings` (in `SpareParts.Desktop.Helpers`)
reads base URLs for each backend API slice (`IdentityApiBaseUrl`, `SalesApiBaseUrl`,
`PurchasesApiBaseUrl`, `InventoryApiBaseUrl`, `CatalogApiBaseUrl`) — the desktop app talks to
the same sliced ASP.NET Core APIs (`SpareParts.Identity.Api`, `SpareParts.Sales.Api`, etc.)
that the web/mobile clients use, not a single monolith endpoint.

---

## 3. Core infrastructure (`SpareParts.Desktop.Helpers`)

### 3.1 Command pattern — `RelayCommand`

`src/SpareParts.Desktop.Helpers/Commands/RelayCommand.cs`:

```csharp
public class RelayCommand : ICommand
{
    public RelayCommand(Action<object?> execute) : this(execute, null) { }
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute) { ... }
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
```

This is the fix from Round 1: `RelayCommand` now supports an optional `canExecute` predicate
(two-arg constructor) in addition to the always-true single-arg constructor. `CanExecuteChanged`
is wired to WPF's `CommandManager.RequerySuggested`, which WPF raises automatically on focus
changes, keybindings, and most user input — so bound `Button`/`MenuItem` controls re-evaluate
`CanExecute` without any ViewModel needing to manually raise `CanExecuteChanged`. This is the
standard "auto-requery" RelayCommand pattern; it means `CanExecute` predicates should be cheap
(they may be invoked frequently by WPF's input pipeline).

Almost every command in the app is constructed as `new RelayCommand(_ => DoThing())` (fire only)
or `new RelayCommand(param => DoThing(param))` (parameterized, e.g. `SelectBrandCommand`,
`CloseTabCommand`). Few call sites currently pass a `canExecute` predicate — most gating is done
imperatively inside the execute delegate (permission checks that publish a notification and
`return` early — see §5.3) rather than by disabling the control via `CanExecute`. This is a
behavioral choice already in the codebase, not something Round 2 changed.

### 3.2 Theme system

Two independent layers exist and are both present in `App.xaml` today:

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="/SpareParts.Desktop.Wpf;component/Themes/ApexTheme.xaml"/>
    <ResourceDictionary Source="/SpareParts.Desktop.Wpf;component/Themes/CockpitTheme.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

- **`ApexTheme.xaml`** (1136 lines) is merged first and defines the **unprefixed** brush/color
  keys actually consumed throughout the app's XAML via `{DynamicResource ...}` —
  `AppBackgroundBrush`, `AccentBrush`, `TextPrimaryBrush`, `TextSecondaryBrush`,
  `CardBackgroundBrush`, `CardBorderBrush`, `BorderBrush`, etc. This is the theme that is
  actually "live" for almost all existing screens.
- **`CockpitTheme.xaml`** (309 lines) is merged second. Its own header comment states its
  intent precisely: *"Mirrors the approved web/mobile cockpit design. All resources are
  prefixed `Ck` and use new keys, so merging this dictionary does NOT retheme existing
  screens; views opt in by referencing these keys."* It defines a parallel palette
  (`CkBgBrush`, `CkPanelBrush`, `CkRedBrush`, `CkAccentBrush` gradient, etc.) that does not
  collide with Apex's keys, so merging both is safe today — nothing currently regresses, but
  also nothing currently opts into Cockpit's palette by default.
- **`ManagementTemplates.xaml`** (`Resources/`) is merged separately, only inside
  `MainWindow.xaml`'s own `Window.Resources`, and supplies `DataTemplate`s keyed by
  ViewModel type (e.g. `DataType="{x:Type ViewModels:ManagementViewModel}"`) plus a few
  shared control styles (`AccountingComboBoxStyle`, `ManagementTabItemStyle`, etc.) that
  several themed styles in `App.xaml` (`AccountingComboOverlayButtonStyle`, `ThemeComboBoxItemStyle`,
  `AccountingComboBoxStyle`, the global `CheckBox` style) are also declared directly.

**Selectable runtime themes** (`ThemeManager` in `SpareParts.Desktop.Helpers/Theming/`) are a
*third*, separate mechanism layered on top of Apex/Cockpit:

```csharp
AppTheme.MPower        -> Themes/BMWMTheme.xaml
AppTheme.NeonGlow       -> Themes/NeonGlowTheme.xaml
AppTheme.AMG            -> Themes/AMGTheme.xaml
AppTheme.PorscheRS      -> Themes/PorscheRSTheme.xaml
AppTheme.LamborghiniSC  -> Themes/LamboTheme.xaml
AppTheme.Default        -> (no dictionary added — Apex/Cockpit base stands as-is)
```

`ThemeManager.ApplyTheme(theme)`:
1. Looks up `Application.Current.Resources.MergedDictionaries`.
2. Removes any dictionary previously tagged with the marker key `"AppThemeOverride"` (removal
   is done **by tag, not by URI**, because WPF resolves relative `Uri`s to absolute `pack://`
   URIs internally, so comparing the original relative `Uri` would never match — this is a
   documented gotcha in the source comment).
3. If the requested theme isn't `Default`, loads the corresponding dictionary, tags it
   (`nd[ThemeTag] = true`), and adds it to `MergedDictionaries` — i.e. **on top of** the
   base Apex + Cockpit dictionaries, so per-theme XAML files only need to override the subset
   of brush keys they want to change (they re-key `AccentBrush` etc. to a per-brand color).
4. Updates `ThemeManager.CurrentTheme`.

Theme selection UI lives in `InvoiceTabsViewModel`: a `Themes` collection of `ThemeOption`
(`Key`, `Name`, `SubTitle`, `AccentHex`→`AccentBrush`, `IsSelected`) is seeded in the
constructor (Classic Dark/Default, M Power, Neon Glow, AMG, Porsche RS, Squadra Corse) and
`SelectThemeCommand` calls `ThemeManager.ApplyTheme(picked.Key)`. `MainWindow.xaml`'s sidebar
renders this collection as swatches.

**Two orphaned theme files exist on disk and are not wired to anything**:
`Themes/DefaultTheme.xaml` (626 lines, defines the same key set as Apex — e.g.
`AppBackgroundBrush`, `AccentBrush`) and `Themes/AuroraTheme.xaml` (985 lines). Neither is
referenced from `App.xaml`, `ThemeManager.ThemeUris`, or any other XAML/code in the repo
(confirmed via full-repo search). They currently have zero effect on the running app. This is
documented as observed fact only — no changes made, per Ralph's standing instruction that
theme/design decisions are pending.

### 3.3 `ServiceLocator`

`SpareParts.Desktop.Helpers/Configuration/ServiceLocator.cs` — a static `IServiceProvider`
holder (`ServiceLocator.Provider`, set once in `App.OnStartup`) with a generic
`Resolve<T>()` helper that throws `InvalidOperationException` if the type isn't registered.
Used sparingly, mostly by code that can't take constructor DI cleanly (some XAML-instantiated
or static-context code). The primary composition pattern is still constructor injection through
the `ServiceCollection` in `App.xaml.cs`; `ServiceLocator` is the escape hatch, not the norm.

### 3.4 Navigation model

There is no `Frame`/`NavigationService`/page-based navigation. Instead:

- `AppScreen` (`SpareParts.Desktop.ViewModels/Navigation/AppScreen.cs`) is a large enum (70+
  members: `HomePage`, `Accounting`, `Pos`, `PartPurchases`, `StockArrivalTheater`,
  `RepairPrepBoard`, `WhatsAppInbox`, `BarcodeMode`, ... down to `ApiPlatform`).
- `InvoiceTabsViewModel.ActiveScreen` (type `AppScreen`) is the single source of truth for
  "what's currently shown" in `MainWindow`.
- `MainWindow.xaml` hosts one `UserControl` per screen, each wrapped so its `Visibility` is
  bound through converters keyed off `ActiveScreen`:
  - **`ScreenEqualsConverter`** (`Converters/ScreenEqualsConverter.cs`) — likely a
    `IValueConverter` comparing bound `AppScreen` to a `ConverterParameter` for simple
    equality-driven visibility/selection.
  - **`ScreenToVisibilityConverter`** (`Converters/ScreenToVisibilityConverter.cs`) — maps
    `ActiveScreen` directly to `Visibility.Visible`/`Collapsed` for a given target screen.
  - **`CountToVisibilityConverter`** (`Converters/CountToVisibilityConverter.cs`) — generic
    "show if collection/count > 0" helper, used for empty-state panels.
  - **`InverseBoolConverter`** (in Helpers) — generic boolean inversion for binding
    `IsEnabled`/`Visibility` against a negated flag.
- "Navigation" is therefore just `InvoiceTabsViewModel.ActiveScreen = AppScreen.X` plus,
  conventionally, kicking off that feature's `LoadAsync()` in the same `RelayCommand` — e.g.
  `GoToStockArrivalCommand` sets `ActiveScreen = AppScreen.StockArrivalTheater` then calls
  `StockArrivalVm.LoadAsync().SafeFireAndForget(...)`. Each `GoTo*Command` also does a
  permission check first (see §5.3) and publishes a rejection notification instead of
  navigating if the current user lacks the permission flag.

### 3.5 HTTP / API client layer

- **`IRestClientFactory` / `RestClientFactory`** (`Http/`) — builds a `RestSharp.RestClient`
  per base URL with a 15s timeout and a custom `ConfigureMessageHandler` wrapping
  `HttpClientHandler` in `RetryHandler` (3 retries, 250ms base delay — presumably exponential
  backoff, see `Http/RetryHandler.cs`). Timeout is applied via reflection
  (`ApplyTimeout`) to tolerate RestSharp API differences between a `TimeSpan?` `Timeout`
  property and an `int` `MaxTimeout` property depending on package version.
- **`ApiClientBase`** (internal static helper in `ApiClients/Base/`) — shared response
  handling: `EnsureSuccessAsync`/`EnsureSuccess` parse a JSON `ApiErrorEnvelope`
  (`{code, message, traceId}`) from failed responses and throw `ApiClientException`; falls back
  to raw body text if the envelope doesn't parse. Also has `BytesToBitmap` (byte[] → frozen
  `BitmapImage`, used for logos/car images/attachments) and `GetMimeType` (extension sniffing
  for uploads).
  Additional base classes `CrudApiClient`, `CrudEntityApiClientBase`, `FeatureApiClientBase`
  layer typed CRUD (`GetAllAsync<T>(path)`, `GetAsync<T>`, etc.) and feature-specific API
  clients (`AccountingApiClient`, `PartsApiClient`, `SalesApiClient`, `GrowthApiClient`, ...)
  on top of this.
- **Auth**: `IApiTokenProvider`/`ApiTokenProvider` hold the bearer token in memory;
  `IApiSessionClient`/`ApiSessionClient` exposes `SetToken`/`ClearToken` called from
  `LoginViewModel` on success/failure. `SessionContext.CurrentUser` (static) holds the
  logged-in `SessionUser` (Id, FullName, RoleId, Token, ExpiresAt) referenced throughout the
  UI for display (`CurrentUserDisplayName`, `CurrentUserInitials`, `CurrentUserRoleLabel`) and
  for permission gating.

### 3.6 Notifications

`AppNotificationCenter.Instance` (singleton) exposes an `ObservableCollection<StatusMessage>`
(`Messages`) and a `Publish(text, isSuccess)` method. `InvoiceTabsViewModel.Notifications` binds
directly to `AppNotificationCenter.Instance.Messages`, so the toast/feed panel in `MainWindow`
is really just rendering this shared singleton's collection — any ViewModel anywhere in the app
can call `AppNotificationCenter.Instance.Publish(...)` to surface a message (used heavily for
permission-denied messages and background-task failure reporting, see `HandleBackgroundException`
patterns below).

### 3.7 Fire-and-forget async helper

`TaskExtensions.SafeFireAndForget(this Task, Action<Exception>? onException = null)` — the
standard pattern used everywhere a `RelayCommand`'s execute delegate needs to kick off an
`async Task` method without making the delegate itself `async void`. Almost every `LoadAsync()`
call site in `InvoiceTabsViewModel` and feature ViewModels is wrapped
`SomeVm.LoadAsync().SafeFireAndForget(HandleBackgroundException)`, where
`HandleBackgroundException` publishes a `✗ Background task failed: {message}` notification.

---

## 4. MVVM wiring conventions

- **ViewModel base pattern**: most feature ViewModels implement `INotifyPropertyChanged`
  directly with a private `OnPropertyChanged(string)` helper (older/verbose style, seen in
  `InvoiceTabsViewModel`, `LoginViewModel`, `OwnerCockpitDashboardViewModel`,
  `WhatsAppInboxViewModel`, `BarcodeModeViewModel`, etc.). The `Management/*` feature
  ViewModels instead derive from **`ManagementFeatureViewModelBase`**
  (`SpareParts.Desktop.ViewModels/Management/ManagementFeatureViewModelBase.cs`), which
  supplies a generic `SetProperty<T>(ref field, value, [CallerMemberName])` helper — a newer,
  less boilerplate-heavy pattern. Both styles coexist; there is no single shared `ViewModelBase`
  used app-wide.
- **`IsLoading` convention**: nearly every feature ViewModel exposes a `bool IsLoading` (or
  `IsBusy`) property, and `InvoiceTabsViewModel.IsGlobalLoading` is a large `||`-chain over
  every child ViewModel's loading flag, kept in sync via explicit
  `child.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(Child.IsLoading)) OnPropertyChanged(nameof(IsGlobalLoading)); }`
  subscriptions registered one-by-one in the `InvoiceTabsViewModel` constructor (see §5.2) —
  there is no aggregate/observable-composition helper; it's manual wiring, repeated ~60 times.
- **DataTemplate-based view resolution**: for a few areas (e.g. `ManagementViewModel` in
  `Resources/ManagementTemplates.xaml`) a `DataTemplate` keyed by `DataType="{x:Type ...}"`
  lets WPF auto-select the view for a bound ViewModel. Most feature screens, however, are wired
  explicitly — a named `UserControl` in `MainWindow.xaml` with `DataContext` bound to a named
  property on `InvoiceTabsViewModel` (e.g. `<controls:StockArrivalTheaterControl DataContext="{Binding StockArrivalVm}" .../>`
  pattern) rather than implicit `DataType` resolution.
- **Code-behind is view-only**: across all `Controls/*.xaml.cs` and `Windows/*.xaml.cs` files
  inspected, code-behind is limited to `InitializeComponent()`, input event handlers that
  delegate straight to a bound `ICommand`, drag/animation/clipboard/process-start glue, and
  `Loaded` handlers that kick off a load. No business logic or API calls happen in code-behind
  outside of that glue (e.g. `PartListingWindow` calls into `ManagementCoordinator`, not
  directly into an API client).

---

## 5. `InvoiceTabsViewModel` — the application's root ViewModel

`SpareParts.Desktop.ViewModels/ViewModels/InvoiceTabsViewModel.cs` (~2,245 lines) is the
`DataContext` of `MainWindow` and is DI-registered as transient (but only ever constructed
once, at `MainWindow` construction). It is effectively the composition root for *feature*
ViewModels (as opposed to `App.xaml.cs`, which is the composition root for *services*).

### 5.1 Responsibilities

1. **Owns ~70 feature ViewModel instances** as `public X Vm { get; }` properties — dashboard,
   purchasing, stock, growth, marketplace-style features, etc. (full list in §6).
2. **Owns brand/car/part browsing state** for the POS flow: `BrandGroups`,
   `AvailableCars`, `AvailableParts`, `SelectedBrand`, `SelectedCar`, plus the loaders
   (`LoadBrandsAsync`, `LoadCarsAsync`, `LoadPartsAsync`) that call `ICarCatalogApiClient` /
   `IPartsApiClient`.
3. **Owns invoice tab management**: `Tabs` (`ObservableCollection<InvoiceTabViewModel>`),
   `SelectedTab`, `AddTab`/`CloseTab`, and invoice search (`InvoiceSearchText` triggers
   debounced-by-version search via `_invoiceSearchVersion`/`CancellationTokenSource`).
4. **Owns ~20 boolean `CanView*Screen`/`CanCreateInvoice`/`CanViewManagementScreen` permission
   flags** (see §5.3).
5. **Owns navigation** via `ActiveScreen` and ~70 `GoTo*Command`s (see §3.4).
6. **Owns theme selection** (`Themes`, `SelectThemeCommand` — see §3.2).
7. **Owns AR session state** (`IsArSessionActive`, `ArStatusMessage`, `ArOverlayTitle`,
   `ArOverlayDiagnostic`, overlay position, `StartArSessionCommand`/`StopArSessionCommand`)
   backed by `IArRenderingService`/`IArDeviceBridge`.
8. **Aggregates `IsGlobalLoading`** across every owned child ViewModel (see §4).

### 5.2 Construction

The constructor takes ~15 injected API clients/services plus `ManagementViewModel managementVm`
(the only child ViewModel that *is* itself DI-resolved — passed in because `ManagementWindow`
also needs the same instance). Every other child ViewModel listed in §6 is constructed with
`new SomeViewModel(crudApi, ...)` inline in the constructor body — they are **not** individually
DI-registered; `ICrudApiClient` (a generic typed-REST client) is reused by most of them since
most of these features talk to generic CRUD endpoints rather than having bespoke typed clients.

### 5.3 Permission model

Permission flags (`CanViewPosScreen`, `CanViewPurchasesScreen`, `CanViewAccountingScreen`, etc.)
are plain settable properties on `InvoiceTabsViewModel` — the constructor/loading code that
populates them from the current user's role permissions was not fully re-read line-by-line in
this pass but follows the same private-setter/`OnPropertyChanged` pattern as every other flag.
Each `GoTo*Command`'s execute delegate manually checks its corresponding flag first:

```csharp
GoToPosCommand = new RelayCommand(_ =>
{
    if (!CanViewPosScreen)
    {
        AppNotificationCenter.Instance.Publish("✗ You do not have permission to view the POS screen.", false);
        return;
    }
    ActiveScreen = AppScreen.Pos;
});
```

This means permission enforcement is **imperative inside the command**, not via
`RelayCommand`'s `canExecute` predicate (which would disable/grey the button). The visible
effect is that nav buttons stay clickable even without permission, but clicking them shows a
red notification instead of navigating. This is existing, intentional-looking behavior — not
something Round 2 changed — and is noted here only so a new engineer isn't surprised buttons
aren't disabled.

---

## 6. Feature-by-feature ViewModel map

All feature ViewModels below live in `SpareParts.Desktop.ViewModels/ViewModels/` (or
`.../Management/`, `.../POS/`, `.../Security/` as noted) and are owned by
`InvoiceTabsViewModel` unless stated otherwise. Each typically follows the same shape: a
`Load(Async)Command`, one or more `ObservableCollection<Row>` for grid/list data, an
`IsLoading`/`IsBusy` flag, and a `Status`/`StatusBrush` pair for inline feedback text.

### Core commerce
- **`OwnerCockpitDashboardViewModel`** (`Controls/Dashboard/OwnerCockpitDashboardControl`) —
  the home dashboard. Loads `KpiTiles`/`Metrics` (`OwnerCockpitMetricCard`), `BottomStats`
  (`OwnerCockpitStatItem`), profit-per-car/profit-per-part tables, a profit heatmap, unpaid
  transactions, and accounting alerts from `IOwnerCockpitApiClient`. Tracks `BusinessDate`,
  `LastRefreshedAt`, `CurrencyCode`.
- **`PosViewModel`** (`POS/PosViewModel.cs`) — a POS-flow-only view-model holding UI-only
  brand/car lists (`GermanBrands`/`JapaneseBrands`/`KoreanBrands`, non-persisted display
  models) and the current invoice's line items; largely superseded in the main flow by
  `InvoiceTabsViewModel`'s own brand/car/part browsing, but still present/used for POS-specific
  screen state (`ActiveScreen`, `WarehouseId`, `CustomerId`).
- **`InvoiceTabViewModel`** / **`InvoiceTabsViewModel`** — one tab per open invoice; each tab
  holds its own `Items` (line items), customer, totals; tab list lives on the root VM (§5).
- **`ManagementViewModel`** (`Management/ManagementViewModel.cs`) — the big admin/CRUD
  aggregator. Owns one feature ViewModel per manageable entity: `CustomersFeature`,
  `SuppliersFeature`, `BrandsFeature`, `PartsFeature`, `PartRequestsFeature`,
  `CarModelsFeature`, `LocationsFeature`, `WarehousesFeature`, `CurrencyRatesFeature`,
  `UsedCarsFeature`, `TransactionTypesFeature`, `AccountingVm`, `ExcelManagerFeature`, plus
  `UsersVm`/`RolesVm`. Delegates most collections/permission flags straight through to the
  relevant feature VM (e.g. `Customers => CustomersFeature.Customers`). Backed by
  `ManagementCoordinator`, which batches ~15 parallel `Task.WhenAll` loads
  (`LoadAllAsync(rolesVm)`) against `ICrudApiClient`/`ICarCatalogApiClient`/`IPartsApiClient`.
- **`AccountingViewModel`** (`Management/AccountingViewModel.cs`) — chart of accounts, posting
  rules, manual journal entries (`ManualJournalLineEditor`), currency rate review.

### Purchasing & stock
- **`PartPurchasesViewModel`**, **`UsedCarPurchasesViewModel`**,
  **`UsedCarWholesaleViewModel`** — purchase order intake for parts vs. whole used cars vs.
  wholesale car lots.
- **`RepairPrepBoardViewModel`** (`Controls/UsedCars/RepairPrepBoardControl`) — a kanban-style
  board (`RepairPrepColumn`s of `RepairPrepCarRow`) tracking used cars through prep/repair
  stages before listing, with `RepairPrepTaskRow` checklists and `RepairPrepLinkedPartRow` for
  parts consumed during prep. Exposes `MetricTiles` (`ObservableCollection<RepairPrepMetricTile>`,
  see §8 — consolidated in this round) showing car count, active count, and task completion
  ratio.
- **`StockArrivalTheaterViewModel`** (`Controls/Inventory/StockArrivalTheaterControl`) — a
  "what just arrived, what should we do about it" board: `Lanes` (photo/waiting-customers/
  campaign/pricing swimlanes of `StockArrivalOpportunity`), `ArrivalFeed`
  (`StockArrivalFeedRow`), `MetricTiles` (arrivals/ready-requests/campaigns/priced-stock).
  Takes an optional `Action<AppScreen> navigate` callback (`NavigateFromStockArrival` in the
  root VM) so opportunity cards can jump straight to another screen.
- **`DeadStockResurrectionViewModel`** (`Controls/Inventory/DeadStockResurrectionControl`) —
  surfaces aged/slow-moving stock (`DeadStockItemRow`) with suggested `DeadStockActionRow`
  actions and `MetricTiles` (candidate count, unit count, stock value, oldest-dormant days).
- **`PartCompatibilityViewModel`** (`Controls/Inventory/PartCompatibilityControl`) — fitment
  graph/compatibility explorer: `CompatibilityMatch`, `PartCompatibilityFitmentGroup`,
  graph nodes/edges (`PartCompatibilityGraphNode`/`Edge`) for a visual compatibility map, plus
  `MetricTiles` (models/donor cars/also-fits/available-stock).
  Also computes `BuildMetrics(matches, fitmentGroups)`, the method touched by the Round 2
  consolidation.
- **`PartPassportViewModel`** (`Controls/Inventory/PartPassportControl`) — a single part's
  full history/provenance ("passport") view.
- **`ReorderCenterViewModel`**, **`ExpiryAlertsViewModel`**, **`GarageStockViewModel`**,
  **`PartReserveViewModel`**, **`PartReelViewModel`** — stock-health and reservation utilities.
- **`StockSnapshotViewModel`** — row model for the plain stock-management grid
  (`LoadStockSnapshotsAsync` in the root VM), annotated with waiting-customer counts computed
  via `SmartPricingCoach.WaitingCustomersByPart`.

### Growth / marketplace-style feature set
`GrowthLabViewModel` (`Controls/Growth/GrowthLabControl`) is the flagship "Money Finder" —
loads a nightly briefing (`UnlockableValue`, `MoneyActions`, `DonorCars`, `TeardownQueue`,
`BuyingRadar`) and renders it as `MetricTiles` (unlockable value/money actions/donor cars/
teardown queue/buying signals) plus supporting lists. Around it, `InvoiceTabsViewModel` owns a
long tail of smaller, single-purpose marketplace/community feature ViewModels — all following
the same `Load(Async)Command` + `ObservableCollection` + `IsLoading` shape, all backed by
`ICrudApiClient` against generic REST endpoints:
`NeedboardViewModel`, `WatchlistViewModel`, `SellerReputationViewModel`,
`SellerVerificationViewModel`, `SymptomSearchViewModel`, `MechanicDeskViewModel`,
`WhatsAppSellingViewModel`, `HalfcutViewModel`, `CarCrushViewModel`, `EscrowViewModel`,
`MarketPriceViewModel`, `ListingBoostViewModel`, `ReferralViewModel`, `VoiceSearchViewModel`,
`MyGarageViewModel`, `DoesItFitViewModel`, `PriceGeniusViewModel`, `ConditionScannerViewModel`,
`CommunityGuardViewModel`, `LiveInspectionViewModel`, `QrTagViewModel`,
`PartGenealogyViewModel`, `DismantlerForecastViewModel`, `RegionalDemandViewModel`,
`MechanicTrustViewModel`, `NewVsUsedViewModel`, `NegotiationViewModel`, `YardTourViewModel`,
`InstantOfferViewModel`, `PartInsuranceViewModel`, `KareemViewModel` (chat-style assistant,
`KareemChatMessage`), `CarTwinViewModel`, `ArFinderViewModel`, `PriceReportViewModel`,
`ApiPlatformViewModel`.

### Communication / operations
- **`WhatsAppInboxViewModel`** (`Controls/Communications/WhatsAppInboxControl`) — two modes in
  one ViewModel: (1) a conversation inbox (`Conversations`, `Messages`,
  `StartManualConversationCommand`, `SendMessageCommand`) and (2) a bulk campaign builder
  (`CampaignSegments`/`CampaignLanguages` option lists, `CampaignAssets`, `CampaignRecipients`,
  `RecentCampaigns`, `LoadCampaignBuilderCommand`/`PreviewCampaignCommand`/`SendCampaignCommand`).
  Both share `ICrudApiClient`.
- **`BusinessAssistantViewModel`** (`Controls/BusinessAssistant/BusinessAssistantControl`) —
  a chat-style assistant driven by `IBusinessAssistantApiClient`
  (`BusinessAssistantMessageViewModel` per turn).
- **`BarcodeModeViewModel`** (largest single-purpose VM after `InvoiceTabsViewModel`) —
  covers barcode/QR scanning, label generation/printing, and quick stock actions in one
  screen: scan resolution (`ResolveScanCommand`, `ScanLookupResultDto`), visual/photo search
  (`SearchByPictureCommand`, `VisualPartMatchDto`), label preview/print
  (`GenerateSelectedLabelCommand`, `GenerateVisibleLabelsCommand`, `PrintLabelsCommand`,
  backed by `Code128BarcodeEncoder` + `QrImageFactory`), and inline stock operations
  (`CheckStockCommand`, `SellPartCommand`, `TransferPartCommand`,
  `AttachUsedCarCommand`/`DetachUsedCarCommand`). Depends on four API clients
  (`IPartsApiClient`, `ISalesApiClient`, `ICrudApiClient`, `IWarehouseApiClient`) — the widest
  dependency set of any feature VM, reflecting that it's a cross-cutting operational tool
  rather than a single-domain screen. `MainWindow`'s barcode scan textbox forwards `Enter`
  keypresses straight to `BarcodeModeVm.ResolveScanCommand`.
- **`ActivityLogViewModel`**, **`QuotesViewModel`**, **`CustomerAgingViewModel`**,
  **`SupplierAgingViewModel`**, **`ShipmentsViewModel`**, **`WarrantyClaimsViewModel`**,
  **`LoyaltyViewModel`** — operational reporting/utility screens, same shape as above.
- **`BillingSubscriptionViewModel`**, **`AdminBillingViewModel`**,
  **`BillingPackageViewModel`** — subscription/billing management for the SpareParts product
  itself (not customer invoicing).
- **`SalesReturnsViewModel`** — uses `IsBusy` instead of `IsLoading` (one of the few
  inconsistencies in the otherwise-uniform `IsLoading` convention, along with `CarCrushViewModel`
  and `KareemViewModel` which also use `IsBusy`).

### Reporting
- **`ReportBuilderViewModel`** (partial class split across
  `ReportBuilderViewModel.cs`/`.Advanced.cs`/`.Pro.cs`, `Controls/Reports/ReportBuilderControl`) —
  a full ad hoc report designer: table/column metadata (`ReportBuilderColumnOption`), filters
  (`ReportBuilderFilterRow`, `ReportBuilderOperatorOption`), group-by/aggregate rows,
  calculated fields, KPI cards, chart points, role-based access rows, and export
  (`ReportExportWriter`). Drag-and-drop column reordering is modeled by
  `Controls/Reports/ReportBuilderColumnDragPayload.cs`.

### Auth / security
- **`LoginViewModel`** (`Auth/LoginViewModel.cs`) — see §2.1. Also pings all 5 backend API
  base URLs on construction (`CheckApiAsync`/`PingAllEndpointsAsync`) to show an
  online/offline health summary on the login screen before the user even submits credentials.
- **`UsersViewModel`**, **`RolesViewModel`** (`Security/`) — user/role administration, each
  DI-registered directly (unlike most feature VMs) since they're also used by `ManagementViewModel`.

---

## 7. Reusable Controls (`SpareParts.Desktop.Controls`)

Smaller, dependency-light `UserControl`s meant to be reused across feature screens (as opposed
to the feature-screen `UserControl`s in `SpareParts.Desktop.Wpf/Controls/*`, which are one-per-
feature and live in the host project):

- `Dialogs/CustomMessageBox.xaml(.cs)` — the app's custom-styled replacement for
  `System.Windows.MessageBox`, used via `CustomMessageBox.Show(message, title, kind)` (seen
  called from `MainWindow.xaml.cs` on invoice-open failure).
- `Pickers/CurrencyCodeSelectorControl`, `Pickers/SaleDatePickerControl`.
- `Search/CustomerSearchControl`, `Search/PartSearchControl`, `Search/RoleSearchControl`,
  `Search/WarehouseSearchControl` — typeahead search boxes over the corresponding entity,
  sharing `Search/SearchTreeHelper.cs`.
- `Tabs/RolesTab.xaml`, `Tabs/UsersTab.xaml` — the two tabs shown inside the security/roles
  management area, bound to `RolesViewModel`/`UsersViewModel`.
- `Models/RoleItem.cs` — a display-only row model for the roles tab.

---

## 8. Round 2 refactor: shared `MetricTile`

**Before**: five structurally identical DTOs existed, one per feature area —
`DeadStockMetricTile`, `GrowthMetricTile`, `PartCompatibilityMetricTile`,
`RepairPrepMetricTile` (all `sealed class` with a 4-arg constructor), and
`StockArrivalMetricTile` (a `sealed record` with positional properties) — all shaped
`{ string Label, string Value, string Detail, Brush AccentBrush }` and all bound from XAML via
plain, non-typed `DataTemplate`s referencing only those four property names (confirmed by
inspecting every `MetricTiles` binding site in `Controls/Growth/GrowthLabControl.xaml`,
`Controls/UsedCars/RepairPrepBoardControl.xaml`, `Controls/Inventory/PartCompatibilityControl.xaml`,
`Controls/Inventory/StockArrivalTheaterControl.xaml`, `Controls/Inventory/DeadStockResurrectionControl.xaml`
— none use `DataType`-based implicit template selection, so no XAML needed to change).

**After**: a single shared `MetricTile` type replaces all five call sites (see the Round 2
report for the exact file list and status of the old duplicate files, which remain on disk
pending Ralph's approval to delete since Ralph's standing rule requires explicit approval
before deleting files).

---

## 9. Things a new engineer should know but that were **not** changed in this audit

- Theme layering (Apex + Cockpit + selectable per-brand overlays, plus two fully orphaned
  theme files) is documented above exactly as found. Any visual/UX change to this system
  requires the Design Change Workflow (mockup first, screenshots after) and is explicitly out
  of scope for this round.
- Permission gating is imperative-inside-command rather than `CanExecute`-driven, so nav
  buttons remain visually enabled even when a user lacks permission; clicking shows a
  notification instead. Documented, not changed.
- `InvoiceTabsViewModel` is very large (~2,245 lines, ~70 owned ViewModels, ~70 commands) and
  is the single biggest complexity hotspot in the codebase. Splitting it was not requested this
  round and was not attempted.
