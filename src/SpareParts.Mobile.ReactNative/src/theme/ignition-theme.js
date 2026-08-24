// Ignition design tokens — React Native side of the cross-platform token contract
// defined in docs/redesign/02-visual-design-system.html §13 ("Token Mapping — Web, WPF, Mobile").
//
// One token system, three implementations:
//   CSS custom property (--ig-*)  <->  WPF ResourceDictionary key (IgBrush*/IgFont*/Ig*)  <->  this module.
//
// Per Report 02 §13 the mobile implementation exports the same values as a flat theme.js
// object. The 72px icon rail is n/a on phone — bottom tabs + 44px touch targets are the
// only sanctioned deviations from the desktop spec (Report 06, Phase 3).
//
// NOTE on fonts: the token contract names Plus Jakarta Sans / Inter / JetBrains Mono.
// Those families are NOT yet packaged in this Expo app (adding @expo-google-fonts/*
// packages requires approval per CLAUDE.md). Until they ship, Ignition screens use the
// system font with explicit weights; the token values below stay contract-exact so the
// token audit can compare them 1:1 against --ig-* and IgFont*.

const ignitionTokens = {
  key: "ignition",
  name: "Ignition",
  colors: {
    bgCanvas: "#0A0A0B", // --ig-bg-canvas / IgBrushCanvas — carbon canvas
    bgSurface: "#131316", // --ig-bg-surface / IgBrushSurface
    bgElevated: "#1B1B1F", // --ig-bg-elevated / IgBrushElevated
    bgElevated2: "#232328", // --ig-bg-elevated-2 / IgBrushElevated2
    border: "#26262B", // --ig-border / IgBrushBorder
    textPrimary: "#FAFAFA", // --ig-text-primary / IgBrushTextPrimary
    textSecondary: "#A1A1AA", // --ig-text-secondary / IgBrushTextSecondary
    textMuted: "#6B6B74", // --ig-text-muted / IgBrushTextMuted
    accent: "#F97316", // --ig-accent / IgBrushAccent — accent orange
    accentHover: "#EA580C", // --ig-accent-hover / IgBrushAccentHover
    success: "#22C55E", // --ig-success / IgBrushSuccess
    warning: "#FBBF24", // --ig-warning / IgBrushWarning
    danger: "#EF4444", // --ig-danger / IgBrushDanger
    info: "#38BDF8" // --ig-info / IgBrushInfo
  },
  fonts: {
    display: "PlusJakartaSans-Bold", // --ig-font-display / IgFontDisplay
    body: "Inter-Regular", // --ig-font-body / IgFontBody
    mono: "JetBrainsMono-Medium" // --ig-font-mono / IgFontMono
  },
  radii: {
    lg: 16, // --ig-radius-lg / IgCornerRadiusLg
    pill: 999 // --ig-radius-pill / IgCornerRadiusPill
  },
  shadows: {
    // --ig-shadow-1 / IgShadowL1 (DropShadowEffect, BlurRadius 14) -> elevation:4 / shadowRadius:14
    l1: {
      shadowColor: "#000000",
      shadowOpacity: 0.32,
      shadowRadius: 14,
      shadowOffset: { width: 0, height: 6 },
      elevation: 4
    }
  },
  spacing: {
    space4: 16 // --ig-space-4 / IgSpace4
  },
  motion: {
    durationBase: 220 // --ig-duration-base / IgDurationBase (0:0:0.22)
  },
  layout: {
    railW: 72, // --ig-rail-w / IgRailWidth — n/a on phone: bottom tabs used instead (Report 02 §13)
    minTouchTarget: 44 // Report 01 §12/§15 + Report 03 §8: all interactive targets >= 44x44 on phone
  }
};

// The same tokens expressed in the app's existing palette shape (see createPalette /
// createStyles in src/theme/styles.js), so Ignition surfaces can also plug into the
// shared StyleSheet factory without touching the legacy theme roster in app-config.
const ignitionPalette = {
  bg: ignitionTokens.colors.bgCanvas,
  surface: ignitionTokens.colors.bgSurface,
  surface2: ignitionTokens.colors.bgElevated,
  sidebar: ignitionTokens.colors.bgCanvas,
  input: ignitionTokens.colors.bgSurface,
  line: ignitionTokens.colors.border,
  text: ignitionTokens.colors.textPrimary,
  muted: ignitionTokens.colors.textSecondary,
  soft: ignitionTokens.colors.textMuted,
  accent: ignitionTokens.colors.accent,
  accentViolet: ignitionTokens.colors.warning,
  accent2: ignitionTokens.colors.success,
  whatsapp: "#25D366",
  danger: ignitionTokens.colors.danger
};

module.exports = { ignitionPalette, ignitionTokens };
