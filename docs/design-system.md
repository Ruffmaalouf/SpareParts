# SpareParts — Design System Reference (Round 2 Audit)

Status: **documentation only**. This document catalogs the design tokens that
actually exist in the codebase today, across all three frontends. No styling
files were modified to produce this document (per CLAUDE.md's Design Change
Workflow — mockups must be shown and approved before any code changes).

Sources read in full:

- `src/SpareParts.Web.React/wwwroot/css/cockpit.css` (workshop/back-office
  shell — "Cockpit Control Center")
- `src/SpareParts.Web.React/wwwroot/css/storefront-apex.css` (customer-facing
  storefront — "Apex" motorsport-kinetic design)
- `src/SpareParts.Desktop.Wpf/Themes/ApexTheme.xaml`
- `src/SpareParts.Desktop.Wpf/Themes/DefaultTheme.xaml` (internally labeled
  "Petrol Head")
- `src/SpareParts.Desktop.Wpf/Themes/CockpitTheme.xaml`
- `src/SpareParts.Desktop.Wpf/Themes/AuroraTheme.xaml` (internally labeled
  "APEX WORKSHOP THEME", a **light-mode** theme — file name and content
  identity do not match)
- `src/SpareParts.Desktop.Wpf/Themes/AMGTheme.xaml`
- `src/SpareParts.Desktop.Wpf/Themes/BMWMTheme.xaml`
- `src/SpareParts.Desktop.Wpf/Themes/LamboTheme.xaml` (internally labeled
  "Squadra Corse")
- `src/SpareParts.Desktop.Wpf/Themes/NeonGlowTheme.xaml`
- `src/SpareParts.Desktop.Wpf/Themes/PorscheRSTheme.xaml`
- `src/SpareParts.Desktop.Wpf/App.xaml` (theme merge order)
- `src/SpareParts.Mobile.ReactNative/src/core/app-config.js` (`wpfThemes`
  palette array — despite the name, this is the canonical mobile+WPF-runtime
  palette source)
- `src/SpareParts.Mobile.ReactNative/src/theme/styles.js` (6,464 lines —
  the single mobile stylesheet, built from `createPalette()` /
  `createStyles()`)
- `src/SpareParts.Mobile.ReactNative/src/theme/theme-context.js` (theme
  bundle provider/consumer)

Not read as styling sources (out of scope per the task's file list, noted for
completeness): `src/SpareParts.Web.React/wwwroot/styles.css` is a legacy
364 KB stylesheet that exceeded the read tool's size limit; it predates the
Cockpit/Apex systems described below and was excluded from this pass.

---

## 1. Platform inventory — how many design systems actually exist

| Platform | Design system(s) present | Notes |
|---|---|---|
| Web (React) | 2 active: **Cockpit** (`cockpit.css`, `.ck-*` classes, back-office/dashboard) and **Apex** (`storefront-apex.css`, `.apx-*` classes, customer storefront) | Two fully independent token sets, scoped by wrapper class (`.ck-shell`, `.apex-store`) so they never collide, but they also never share a single source of truth. |
| WPF Desktop | 9 theme files, switchable at runtime via a theme picker: Apex, Default ("Petrol Head"), Cockpit, Aurora ("Apex Workshop" — light mode), AMG, BMW M, Lambo ("Squadra Corse"), Neon Glow, Porsche RS | `App.xaml` merges `ApexTheme.xaml` then `CockpitTheme.xaml` at startup — Cockpit's key overrides win by default before any user theme selection runs. |
| Mobile (React Native) | 8 themes driven by one shared palette table (`wpfThemes` in `app-config.js`) and one shared `createStyles()` function: apex, aurora, carbon, amg, bmw-m, lambo, neon-glow, porsche-rs | Despite the `wpfThemes` variable name, this palette table is **not** identical to the WPF theme files (see Section 5). Mobile has a "carbon" theme with no WPF equivalent; WPF has "Cockpit"/"Default" themes with no mobile equivalent. |

Total distinct theme identities across the solution: **web has 2, WPF has 9, mobile has 8** — 19 theme definitions in total, none of which are generated from a shared source file.

---

## 2. Web — Color Tokens

### 2.1 Cockpit (`cockpit.css`, scope `.ck-shell`)

| Token | Hex / value | Role |
|---|---|---|
| `--ck-bg` | `#070708` | Page background |
| `--ck-panel` | `#0f0f12` | Primary panel/card surface |
| `--ck-panel2` | `#131316` | Secondary/nested surface |
| `--ck-border` | `#1c1c22` | Default border |
| `--ck-border-soft` | `#26262e` | Softer border (inputs, pills) |
| `--ck-red` | `#ff3b3b` | Accent (primary — "Cockpit red") |
| `--ck-orange` | `#ff8a3d` | Accent (secondary/gradient partner) |
| `--ck-grad` | `linear-gradient(135deg,#ff3b3b 0%,#ff8a3d 100%)` | Accent gradient (buttons, headings, KPI icons) |
| `--ck-text` | `#f4f4f7` | Primary text |
| `--ck-muted` | `#8c8c9c` | Secondary text |
| `--ck-muted2` | `#5d5d6d` | Tertiary/label text |
| `--ck-green` | `#2bdb8f` | Signal: success / positive delta / live indicator |
| `--ck-blue` | `#4d8dff` | Signal: informational (legend/category color only) |
| `--ck-purple` | `#b768ff` | Signal: category color only |
| `--ck-cyan` | `#34d9d4` | Signal: category color only |
| `--ck-gold` | `#ffc257` | Signal: category color only |
| Danger (inline, not a variable) | `#ff3b3b` reused for `.ck-down`, low-stock badges | Signal: negative/danger — same hex as the primary accent, no distinct danger token |

Background is layered further with radial gradients using `rgba(255,59,59,.09)` and `rgba(255,138,61,.05)` on top of `#050506`.

### 2.2 Apex Storefront (`storefront-apex.css`, scope `.apex-store`)

| Token | Hex / value | Role |
|---|---|---|
| `--apx-bg` | `#08090b` | Page background |
| `--apx-ink` | `#f7f4ed` | Primary text (warm off-white, not pure white) |
| `--apx-muted` | `#b6b0a5` | Secondary text |
| `--apx-soft` | `#7d7a72` | Tertiary text |
| `--apx-panel` | `#111316` | Primary panel/card surface |
| `--apx-panel2` | `#191c20` | Secondary/nested surface |
| `--apx-line` | `rgba(247, 244, 237, .13)` | Border (alpha-based, not solid hex) |
| `--apx-accent` | `#ff4b3a` | Accent (primary — "Brick Red", per the file's own comment) |
| `--apx-accent2` | `#ff8a3d` | Accent (secondary/gradient partner — identical hex to Cockpit's `--ck-orange`) |
| `--apx-on-accent` | `#ffffff` | Text/icon color placed on top of accent fills |
| `--apx-glow` | `rgba(255, 75, 58, .45)` | Accent glow/shadow tint |
| `--apx-green` | `#34e08a` | Signal: success / in-stock / live badge |

No dedicated danger/warning token exists in this file; `.apx-btn-danger` reuses `var(--apx-accent)` in a gradient with a hardcoded `#ff6a5a`.

### 2.3 Web typography

| Role | Font stack | Notes |
|---|---|---|
| Display / headings | `"Oswald", system-ui, sans-serif` (Apex: `--apx-display`) | Used for all `h1`/`h3`/brand/section titles, uppercase, tight `line-height: .9`–`.95`, negative letter-spacing on large headings |
| Body | `"Inter", system-ui, sans-serif` | Base font on both `.ck-shell` and `.apex-store` |
| Monospace / code-like values | `"IBM Plex Mono", monospace` | Apex only — used for part codes, prices in some contexts, placeholder art, cart totals |
| Cockpit body base size | `14px` implicit (no explicit root rule found beyond component-level sizing) | Component sizes range ~9px (`ck-kpi-label`) to 30px (`ck-metric-big`) |
| Apex base size | `font-size: 14px; line-height: 1.5` (set on `.apex-store` root) | Hero `h1` scales via `clamp(46px, 7.2vw, 96px)`; section titles `clamp(30px, 4vw, 52px)` |

### 2.4 Web spacing / corner-radius conventions

| System | Corner radius pattern | Spacing pattern |
|---|---|---|
| Cockpit | Large, soft radii: `22px` (outer device frame), `18px`/`17px` (cards/panels), `13–16px` (buttons, pills, nav items), `8–12px` (small icon buttons, chips) | Grid gaps mostly `12–22px`; content padding `22px 26px 30px` |
| Apex Storefront | Small, sharp/motorsport radii: `12px` (modal), `10px` (passport visual, trust cards), `8px` (cards, panels), `6px` (buttons, inputs, footer pay boxes) | Grid gaps mostly `10–16px`; page padding uses `clamp()` responsive values, e.g. `clamp(18px, 5vw, 64px)` horizontal |

**This corner-radius philosophy is a direct divergence between the two web systems themselves** (22px+ soft "cockpit" rounding vs. 6–12px sharp "Apex" rounding) — before even comparing web to WPF/mobile.

---

## 3. WPF Desktop — Color Tokens (per theme)

All themes define the same semantic key set (`AppBackgroundBrush`,
`CardBackgroundBrush`, `AccentBrush`, `TextPrimaryBrush`,
`TextSecondaryBrush`, `BorderBrush`, etc.) so any theme can be hot-swapped
without changing view XAML. Values differ completely per theme.

### 3.1 ApexTheme.xaml ("Apex" — dark, racing red)

| Token | Hex |
|---|---|
| `ColorRaceBlack` / `AppBackgroundBrush` | `#FF090909` |
| `ColorPitLane` / `CardBackgroundBrush` | `#FF131313` |
| `ColorCockpit` / `CardHighlightBrush` | `#FF1C1C1C` |
| `ColorRacingRed` / `AccentBrush` | **`#FFE2231A`** |
| `ColorRedHot` | `#FFFF4B3E` |
| `ColorRedDeep` | `#FFA8160F` |
| `ColorTextWhite` / `TextPrimaryBrush` | `#FFF2F1EE` |
| `ColorTextMid` / `TextSecondaryBrush` | `#FF9B9B9B` |
| `ColorTextDim` | `#FF6B6B6B` |
| `ColorCarbonLine` / `BorderBrush` | `#FF2A2A2A` |
| `ColorSportOrange` / `AccentOrangeBrush` | `#FFFFB400` |
| `ColorNeonGreen` / `AccentGreenBrush` | `#FF2BD97E` |
| `FocusRingBrush` | `#FFFF4B3E` |

### 3.2 DefaultTheme.xaml ("Petrol Head" — dark, deeper red)

| Token | Hex |
|---|---|
| `AppBackgroundBrush` | `#FF080808` |
| `CardBackgroundBrush` | `#FF0E0E0E` |
| `CardHighlightBrush` | `#FF1E1E1E` |
| `AccentBrush` (`ColorRacingRed`) | **`#FFD40000`** |
| `ColorRedHot` | `#FFFF2020` |
| `ColorRedDeep` | `#FF8C0000` |
| `TextPrimaryBrush` | `#FFEBEBEB` |
| `TextSecondaryBrush` (`ColorTextDim`) | `#FF505050` |
| `BorderBrush` (`ColorCarbonLine`) | `#FF1A1A1A` |
| `AccentBlueBrush` | `#FF4090E0` |
| Note | `ColorSportOrange` is defined but hardcoded to the same value as `ColorRacingRed` (`#FFD40000`) — "legacy key → racing red" per the file's own comment; there is no distinct orange in this theme. |

### 3.3 CockpitTheme.xaml ("Cockpit Control Center" — dark, coral/red-orange)

| Token | Hex |
|---|---|
| `CkBgColor` / `AppBackgroundBrush` (override) | `#FF070708` |
| `CkPanelColor` / `CardBackgroundBrush` (override) | `#FF121217` |
| `CkPanel2Color` | `#FF16161C` |
| `CkBorderColor` / `BorderBrush` (override) | `#FF24242C` |
| `CkRedColor` / `AccentBrush` (override) | **`#FFFF3B3B`** |
| `CkOrangeColor` / `AccentOrangeBrush` (override) | `#FFFF8A3D` |
| `CkGreenColor` / `AccentGreenBrush` (override) | `#FF2BDB8F` |
| `CkTextColor` / `TextPrimaryBrush` (override) | `#FFF4F4F7` |
| `CkMutedColor` / `TextSecondaryBrush` (override) | `#FF8C8C9C` |
| `CkAccentBrush` (gradient) | `#FFFF7A44 → #FFFF3B3B → #FFE02A30` |
| `FocusRingBrush` (override) | `#FFFF7A44` |

Important: this file **redefines the global semantic keys** (`AppBackgroundBrush`, `AccentBrush`, etc.), not just its own `Ck*`-prefixed keys, and per `App.xaml` it is merged *after* `ApexTheme.xaml` — so on a freshly-launched desktop app, before any user theme choice, the app is actually skinned in Cockpit colors (`#FFFF3B3B` red-orange), not classic Apex racing red (`#FFE2231A`), even though `ApexTheme.xaml` is the one merged first and named "Apex."

### 3.4 AuroraTheme.xaml — mislabeled: internally "APEX WORKSHOP THEME" (light mode)

This is the most significant naming/identity divergence found in Round 2. The file is named `AuroraTheme.xaml` and selected via a theme picker presumably labeled "Aurora," but its internal header comment reads `<!-- APEX WORKSHOP THEME — Light workspace / Black sidebar / Orange accent -->`, and it is the **only light-background** theme in the WPF app.

| Token | Hex |
|---|---|
| `ColorWorkspace` / `AppBackgroundBrush` | `#FFF0EEEA` (light beige, not dark) |
| `ColorSurface` / `CardBackgroundBrush` | `#FFFFFFFF` (white) |
| `ColorSurface2` / `CardHighlightBrush` | `#FFE8E6E1` |
| `ColorSidebar` | `#FF111110` (dark sidebar on a light page) |
| `ColorAccent` / `AccentBrush` | **`#FFD4500C`** (burnt orange) |
| `ColorAccentHover` | `#FFBF4510` |
| `ColorDanger` / `DangerBrush` | `#FFDC2626` |
| `ColorSuccess` / `SuccessBrush` | `#FF16A34A` |
| `ColorAmber` | `#FFB45309` |
| `ColorText` / `TextPrimaryBrush` | `#FF1A1917` (near-black, for light background) |
| `ColorMuted` / `TextSecondaryBrush` | `#FF706D67` |
| `ColorLine` / `CardBorderBrush` | `#FFDEDCD7` |
| `ColorLineBright` / `BorderBrush` | `#FFC8C6C0` |
| `AccentVioletBrush` | `#FF7C5CFC` |

This is also the only WPF theme file that defines explicit `DangerBrush`/`SuccessBrush` semantic tokens and named badge styles (`BadgeAvailableStyle`, `BadgeReservedStyle`, `BadgeSoldStyle`, `BadgeDangerStyle`) — none of the other 8 WPF theme files define these, meaning badge coloring in every other theme falls back to ad hoc inline colors wherever badges are used in views.

### 3.5 AMGTheme.xaml ("Mercedes AMG" — dark, monochrome silver)

| Token | Hex |
|---|---|
| `AppBackgroundBrush` | `#111114` |
| `CardBackgroundBrush` | `#1A1A1E` |
| `CardHighlightBrush` | `#242428` |
| `AccentBrush` | **`#C8C8D0`** (cool silver, not a hue at all) |
| `AccentBlueBrush` | `#B0C4DE` |
| `TextPrimaryBrush` | `#F0F0F2` |
| `TextSecondaryBrush` | `#8888A0` |
| `BorderBrush` | `#28C8C8D0` (alpha-blended accent, not a separate neutral) |

### 3.6 BMWMTheme.xaml ("BMW M Power" — dark, blue)

| Token | Hex |
|---|---|
| `AppBackgroundBrush` | `#0D0D12` |
| `CardBackgroundBrush` | `#14141C` |
| `CardHighlightBrush` | `#1E1E2C` |
| `AccentBrush` | **`#1C69D4`** |
| `AccentBlueBrush` | `#5B9BD5` |
| `TextPrimaryBrush` | `#E8EAED` |
| `TextSecondaryBrush` | `#8899BB` |

### 3.7 LamboTheme.xaml — internally labeled "Squadra Corse" (dark, yellow)

| Token | Hex |
|---|---|
| `AppBackgroundBrush` | `#090909` |
| `CardBackgroundBrush` | `#131313` |
| `CardHighlightBrush` | `#1E1E1E` |
| `AccentBrush` | **`#FFD600`** |
| `AccentBlueBrush` | `#FFC400` |
| `TextPrimaryBrush` | `#F5F5F0` |
| `TextSecondaryBrush` | `#888878` |

Naming divergence: file name says "Lambo," in-file comment says "Squadra Corse" (Ferrari's racing division name, not Lamborghini's). Mobile's matching theme entry is keyed `"lambo"` / named `"Lambo"` — so the mobile-facing label and the WPF file name agree with each other, but the WPF file's own internal comment disagrees with both.

### 3.8 NeonGlowTheme.xaml ("Neon Glow" — dark, cyan)

| Token | Hex |
|---|---|
| `AppBackgroundBrush` | `#080810` |
| `CardBackgroundBrush` | `#0D0D1A` |
| `CardHighlightBrush` | `#141428` |
| `AccentBrush` | **`#00E5FF`** |
| `AccentBlueBrush` | `#00B0FF` |
| `TextPrimaryBrush` | `#E0F7FA` |
| `TextSecondaryBrush` | `#4DD0E1` |

### 3.9 PorscheRSTheme.xaml ("Porsche RS" — dark, red)

| Token | Hex |
|---|---|
| `AppBackgroundBrush` | `#0C0C0E` |
| `CardBackgroundBrush` | `#181818` |
| `CardHighlightBrush` | `#222222` |
| `AccentBrush` | **`#E30613`** (Porsche's actual guards-red brand hex) |
| `AccentBlueBrush` | `#FF6B6B` |
| `TextPrimaryBrush` | `#F2F2F2` |
| `TextSecondaryBrush` | `#888888` |

### 3.10 WPF typography and spacing

| Aspect | Convention |
|---|---|
| Base font family | `Segoe UI` on every theme's global `TextBlock` style (system font, no custom webfont/display font in WPF at all — unlike web's Oswald headings and mobile's Oswald headings) |
| Section header font | `Bahnschrift SemiBold` (`SectionHeaderTextStyle` in ApexTheme only) |
| Heading styles (Aurora only) | `HeadingLargeStyle` 26px Bold, `HeadingMediumStyle` 18px SemiBold, `SectionLabelStyle` 10px Bold — no equivalent named heading styles exist in any other WPF theme file |
| Price/metric styles (Aurora only) | `PriceTextStyle` 20px Bold `Consolas, Courier New`; `MetricValueStyle` 28px Bold — again unique to Aurora, not present in Apex/Default/Cockpit/AMG/BMW/Lambo/Neon/Porsche |
| Corner radius, ApexTheme | `2px` (TextBox/PasswordBox/Button/ComboBox), `3px` (CardStyle), tile buttons `3–4px` |
| Corner radius, CockpitTheme | `18px` (`CkPanelStyle`), `17px` (KPI cards), `12–13px` (buttons/search), `12px` (nav items) — matches web Cockpit's large-radius language |
| Corner radius, Aurora ("Apex Workshop") | `6px` (inputs/buttons), `8px` (cards/list), `10px` (KPI/section cards), `11px` (toggle switch) |
| Corner radius, AMG/BMW/Lambo/NeonGlow/PorscheRS | `10–12px` (brand tiles), `3–4px` (buttons/car tiles) — closer to Apex's tight radii than Cockpit's |
| Row/grid density, ApexTheme DataGrid | `RowHeight=42`, `ColumnHeaderHeight=40`, `FontSize=13` |
| Row/grid density, DefaultTheme DataGrid | `RowHeight=36`, `ColumnHeaderHeight=34`, `FontSize=13` (tighter than Apex) |
| Row/grid density, CockpitTheme DataGrid | `RowHeight=40`, no explicit header height override |

---

## 4. Mobile (React Native) — Color Tokens

Mobile has no separate CSS/XAML-style theme files; all 8 themes are entries
in the `wpfThemes` array inside `app-config.js`, consumed by
`createPalette(themeKey)` in `theme/styles.js`. Default theme key is
`"apex"` (`defaultThemeKey = "apex"`).

| Theme key | bg | surface | accent | accent2/violet | signal green |
|---|---|---|---|---|---|
| `apex` (default) | `#090909` | `#131313` | **`#e2231a`** | `#ffb400` | `#2bd97e` |
| `aurora` | `#080c14` | `#0e1420` | `#00c9a7` | `#7c5cfc` | `#25d366` |
| `carbon` | `#101114` | `#17191f` | `#ff5722` | (none) | `#25d366` |
| `amg` | `#111114` | `#1A1A1E` | `#C8C8D0` | (none) | `#25d366` |
| `bmw-m` | `#0D0D12` | `#14141C` | `#1C69D4` | (none) | `#25d366` |
| `lambo` | `#090909` | `#131313` | `#FFD600` | (none) | `#25d366` |
| `neon-glow` | `#080810` | `#0D0D1A` | `#00E5FF` | (none) | `#25d366` |
| `porsche-rs` | `#0C0C0E` | `#181818` | `#E30613` | (none) | `#25d366` |

Every mobile theme also defines: `surface2`, `sidebar`, `input`, `line`
(border), `text`, `muted`, `soft`, `whatsapp` (always `#25d366` — hardcoded
WhatsApp brand green, identical across every theme), and `danger` (ranges
`#ff3b30`–`#ff6b8a` depending on theme).

Full mobile `apex` (default) palette:

| Token | Hex | Role |
|---|---|---|
| `bg` | `#090909` | Page background |
| `surface` | `#131313` | Card/panel background |
| `surface2` | `#1c1c1c` | Nested surface |
| `sidebar` | `#050505` | Nav drawer background |
| `input` | `#0e0e0e` | Input field background |
| `line` | `#2a2a2a` | Border |
| `text` | `#f2f1ee` | Primary text |
| `muted` | `#9b9b9b` | Secondary text |
| `soft` | `#6b6b6b` | Tertiary text |
| `accent` | `#e2231a` | Primary accent |
| `accentViolet` | `#ffb400` | Secondary accent (despite the key name "violet," this is amber/gold, not violet — key name is stale) |
| `accent2` | `#2bd97e` | Tertiary accent / success |
| `whatsapp` | `#25d366` | WhatsApp brand green (fixed) |
| `danger` | `#ff3b30` | Signal: error/danger |

### 4.1 Mobile typography

| Role | Font | Notes |
|---|---|---|
| Display / heading | `Oswald_500Medium`, `Oswald_600SemiBold`, `Oswald_700Bold` | Same family as web's `--apx-display` (Oswald), used across ~70 distinct style rules in `styles.js` |
| Body | System default (no `fontFamily` set) | Matches web's implicit fallback pattern but web explicitly declares `"Inter"` — mobile relies on OS default unless a screen opts into Oswald |
| Font size range | 8px (`fontSize: 8`, badge/microcopy) to 48px (`fontSize: 48`, large numeric display) | Widest range of the three platforms; most body/label text clusters 10–13px |

### 4.2 Mobile spacing / corner-radius conventions

| Aspect | Convention |
|---|---|
| Corner radius | Overwhelmingly `2px` (hundreds of occurrences) — sharp, motorsport-flat corners, matching Apex Storefront's tight web radii far more closely than Cockpit's large radii |
| Pill/circle exceptions | `9999`/`999` used for badges, avatars, round buttons (~14 occurrences) |
| Larger-radius outliers | Isolated uses of `17px`, `18px`, `19px`, `26px` for specific hero/feature cards — small in number relative to the `2px` baseline |
| Spacing (`gap`) | Small, tight values throughout: mostly `6–16px`, matching Apex Storefront's `10–16px` gap convention rather than Cockpit's `12–22px` |
| Search bar / KPI shadow language | Mobile borrows the "glow" pattern from web Cockpit (`shadowColor: palette.accent, shadowOpacity: 0.18, shadowRadius: 18`) — a visual technique shared with Cockpit's `box-shadow` glows, not with Apex Storefront (which uses flat borders, minimal glow) |

---

## 5. Side-by-side comparison — every divergence catalogued

Round 1 found 7 issues, headlined by three different "Apex red" values and a
WPF dashboard ignoring the shared theme. This pass re-confirms those and adds
substantially more. Each row below is a distinct, independently-verifiable
divergence.

### 5.1 Accent/brand-red divergences (the "Apex red" family)

| # | Location | Value | Notes |
|---|---|---|---|
| D1 | WPF `ApexTheme.xaml` `AccentBrush` | `#E2231A` | The file literally named "Apex" |
| D2 | Mobile `apex` theme `accent` | `#e2231a` | Matches D1 exactly — mobile and WPF Apex agree with each other |
| D3 | WPF `DefaultTheme.xaml` ("Petrol Head") `AccentBrush` | `#D40000` | This is the theme loaded if no explicit Apex/Cockpit selection is made in some code paths — a third red |
| D4 | WPF `CockpitTheme.xaml` `AccentBrush` (global override) | `#FF3B3B` | Because `App.xaml` merges Cockpit last, this is the *actual effective* `AccentBrush` on desktop app launch, not D1 |
| D5 | Web Cockpit `--ck-red` | `#ff3b3b` | Matches D4 exactly — web Cockpit and WPF Cockpit agree |
| D6 | Web Apex Storefront `--apx-accent` | `#ff4b3a` | A fourth, distinct red — close to D4/D5 but not identical, and not identical to D1/D2 either |
| D7 | WPF `AuroraTheme.xaml` ("Apex Workshop," light mode) `AccentBrush` | `#D4500C` | A fifth value, burnt-orange rather than red, despite the in-file name containing "Apex" |

Round 1 called out 3 distinct "Apex red" values; this pass finds **5** distinct hex values all tracing back to a design system called "Apex" in some form, across the two codebases (web/WPF/mobile) — D1/D2 agree, D4/D5 agree, D3, D6, D7 are each unique.

### 5.2 Naming/identity mismatches (file name vs. in-file label vs. cross-platform key)

| # | File | File name says | In-file comment / actual content says | Cross-platform match |
|---|---|---|---|---|
| D8 | `AuroraTheme.xaml` | Aurora | `APEX WORKSHOP THEME` — and it's a **light-mode** theme | Mobile's `aurora` theme is dark (`bg: #080c14`) with teal accent `#00c9a7` — completely different palette and mode from WPF's file of the same name. This is a full identity mismatch, not just a color mismatch. |
| D9 | `LamboTheme.xaml` | Lambo | `Squadra Corse` (Ferrari racing division name) | Mobile's `lambo` key/name agrees with the WPF file name, disagreeing with the WPF file's own internal comment |
| D10 | Mobile `carbon` theme (`accent: #ff5722`) | — | — | No WPF theme file exists named "Carbon." The closest WPF theme by name is `CockpitTheme.xaml`, which is a completely different palette (`#FF3B3B` accent, not `#ff5722`). Mobile has a theme with no desktop counterpart at all. |
| D11 | WPF `CockpitTheme.xaml` / `DefaultTheme.xaml` | Cockpit / Default ("Petrol Head") | — | Neither has a mobile theme-key counterpart. Mobile's `wpfThemes` array (8 entries) omits both "cockpit" and "default"/"petrol-head" keys entirely. |
| D12 | Mobile `accentViolet` key | Key name implies violet/purple | Apex theme's `accentViolet` value is `#ffb400` — amber/gold, not violet at all | Internal mobile naming inconsistency, independent of cross-platform comparison |

### 5.3 Theme roster mismatch (which themes exist where)

| Theme identity | Web | WPF file | Mobile key |
|---|---|---|---|
| Apex | Yes (2 separate web systems both loosely "Apex"-branded: Cockpit and Storefront) | `ApexTheme.xaml` | `apex` |
| Aurora | No | `AuroraTheme.xaml` (light mode, "Apex Workshop") | `aurora` (dark mode, teal) — **name collides, content is unrelated** |
| Carbon | No | No file | `carbon` — **mobile-only, no desktop equivalent** |
| Cockpit | Yes (`cockpit.css`) | `CockpitTheme.xaml` | No — **web+WPF only, no mobile equivalent** |
| Default / Petrol Head | No | `DefaultTheme.xaml` | No — **WPF-only** |
| AMG | No | `AMGTheme.xaml` | `amg` |
| BMW M | No | `BMWMTheme.xaml` | `bmw-m` |
| Lambo / Squadra Corse | No | `LamboTheme.xaml` | `lambo` |
| Neon Glow | No | `NeonGlowTheme.xaml` | `neon-glow` |
| Porsche RS | No | `PorscheRSTheme.xaml` | `porsche-rs` |

Net result: **only 5 of 10 total theme identities are shared between WPF and mobile** (AMG, BMW M, Lambo, Neon Glow, Porsche RS — and those 5 do have matching hex values). Aurora is present in both but with unrelated content. Carbon exists only on mobile. Cockpit and Default exist only on WPF (and Cockpit also exists, separately, on web). Web has no equivalent to AMG/BMW M/Lambo/Neon Glow/Porsche RS at all — the web layer only ever renders Cockpit or Apex Storefront, never any of the car-brand-inspired themes that dominate WPF/mobile's theme list.

### 5.4 Structural/architectural divergence

| # | Finding |
|---|---|
| D13 | WPF's `App.xaml` merges `ApexTheme.xaml` first, then `CockpitTheme.xaml` second, and Cockpit's dictionary **redefines the shared semantic keys** (`AppBackgroundBrush`, `AccentBrush`, `TextPrimaryBrush`, etc.), not just its own `Ck*`-prefixed keys. This means the desktop app's actual default appearance at first launch is Cockpit-colored, not Apex-colored, regardless of the "Apex" theme file being loaded first — the file load order silently decides the winner. |
| D14 | Web has **two entirely separate, non-interoperable design systems** living in the same app (`cockpit.css` for back-office, `storefront-apex.css` for the customer storefront), each with its own prefix, its own color variables, its own corner-radius philosophy (22px+ soft vs 6-12px sharp), and no shared base tokens file between them. |
| D15 | Mobile is the only platform where a single `createStyles(palette)` function generates the *entire* app's styling from one palette object — meaning mobile's structural consistency across its own 8 themes is high, but that same discipline does not extend to keeping those palettes aligned with WPF or web. |
| D16 | Only the Aurora ("Apex Workshop") WPF theme defines explicit `DangerBrush`/`SuccessBrush` semantic tokens and reusable badge styles (`BadgeAvailableStyle`, `BadgeReservedStyle`, `BadgeSoldStyle`, `BadgeDangerStyle`). All 8 other WPF themes have no equivalent — any view using those badge styles will silently fall back to whatever Aurora's inherited resource happens to be, or fail to resolve depending on merge order, when a non-Aurora theme is active. |
| D17 | Only Aurora defines named typography styles (`HeadingLargeStyle`, `HeadingMediumStyle`, `SectionLabelStyle`, `PriceTextStyle` in `Consolas`, `MetricValueStyle`). No other WPF theme provides these, so any XAML view referencing them by key depends on Aurora's dictionary being merged, coupling unrelated themes together. |
| D18 | WPF base font is `Segoe UI` (system UI font) on every theme; web and mobile both use `Oswald` for display/heading text. WPF has no display-font layer at all — headings and body text use the same system font family, unlike web/mobile's two-tier (Oswald headings + Inter/system body) typography model. |
| D19 | Corner-radius language splits three ways even within a single platform: WPF Apex/AMG/BMW/Lambo/Neon/Porsche use tight 2–4px radii; WPF Cockpit uses 12–18px; WPF Aurora uses 6–11px. Web Cockpit uses 12–22px (matching WPF Cockpit); Web Apex Storefront uses 6–12px (matching WPF Aurora's range, coincidentally, despite unrelated palettes); Mobile uses 2px almost everywhere (matching WPF's non-Cockpit themes). No single radius scale is shared platform-to-platform under one name. |
| D20 | The `whatsapp` color token (`#25d366`) is the **only token that is perfectly identical across every mobile theme and hardcoded the same way everywhere it appears** — it is the sole genuinely consistent brand-color constant found in this audit, because it's pinned to WhatsApp's own external brand color rather than derived from any theme palette. |

### 5.5 Summary count

- Round 1 findings: 7 (headlined by 3 "Apex red" variants + 1 WPF screen ignoring the shared theme).
- Round 2 additional/deeper findings in this document: **20 distinct catalogued divergences** (D1–D20 above), which supersede and extend Round 1's 7 — the "3 reds" finding is now shown to be 5 distinct red/orange hex values once the Aurora/"Apex Workshop" file and the Apex Storefront web file are included, and the "WPF dashboard ignoring the shared theme" finding is now explained structurally (D13: Cockpit's dictionary is merged last and silently overrides Apex's semantic keys at the resource level, not just on one screen).

---

## 6. Recommended single-source-of-truth structure (not implemented — for future design change proposals only)

This section is descriptive of the gap, not a proposal to implement without
following the Design Change Workflow (mockups first, Ralph's approval,
then implementation). Noted here only so the next design change request has
a documented starting point:

- No file in the repository currently defines a canonical, platform-agnostic
  token set (e.g. a single JSON/YAML palette that both WPF XAML generation
  and mobile's `app-config.js` and web's CSS custom properties could all be
  generated from). `app-config.js`'s `wpfThemes` array is the closest thing
  to a shared source today, but it only feeds mobile — WPF's actual XAML
  files are hand-maintained separately and have already drifted (Aurora is
  the clearest example).
- Web has no theme-switching mechanism at all; both `cockpit.css` and
  `storefront-apex.css` are single fixed palettes with CSS custom properties
  scoped to their own wrapper class, whereas WPF and mobile both support
  runtime theme switching across many themes.
