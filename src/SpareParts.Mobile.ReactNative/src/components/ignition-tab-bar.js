const React = require("react");
const { Pressable, StyleSheet, Text, View } = require("react-native");
const SvgModule = require("react-native-svg");
const { useSafeAreaInsets } = require("react-native-safe-area-context");
const { ignitionTokens } = require("../theme/ignition-theme");
const { useTheme } = require("../theme/theme-context");

const Svg = SvgModule.default;
const { Circle, Line, Path, Rect } = SvgModule;

const el = React.createElement;
const IG = ignitionTokens.colors;
const MIN_TAP_TARGET = ignitionTokens.layout.minTouchTarget;

// Bottom tab bar for the Ignition screens per Report 01 §12 (Figure 3) and Report 03 §8:
// below the tablet breakpoint the desktop 72px icon rail collapses to a bottom tab bar
// with 5 primary destinations + "More", every target >= 44x44 (the sanctioned mobile
// deviations called out in Reports 02 §13 and 06). "More" opens the full destination
// structure — on this app that is the existing drawer sidebar, presented full-screen.
const ignitionTabs = [
  { key: "ignition-dashboard", label: "Dash", fallbackLabel: "Dash", icon: "gauge" },
  { key: "client-workspace", label: "Clients", fallbackLabel: "Clients", icon: "clients" },
  { key: "invoices", label: "Sales", fallbackLabel: "Sales", icon: "sales" },
  { key: "parts", label: "Parts", fallbackLabel: "Parts", icon: "parts" },
  { key: "__more", label: "More", fallbackLabel: "More", icon: "more" }
];

const styles = StyleSheet.create({
  bar: {
    flexShrink: 0,
    flexDirection: "row",
    alignItems: "stretch",
    paddingHorizontal: 8,
    paddingTop: 6,
    borderTopWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface,
    ...ignitionTokens.shadows.l1
  },
  tab: {
    flex: 1,
    minWidth: MIN_TAP_TARGET,
    minHeight: MIN_TAP_TARGET,
    alignItems: "center",
    justifyContent: "center",
    gap: 3,
    paddingVertical: 6,
    borderRadius: ignitionTokens.radii.lg
  },
  tabActive: {
    backgroundColor: IG.bgElevated
  },
  label: {
    maxWidth: "100%",
    color: IG.textMuted,
    fontSize: 11,
    fontWeight: "700",
    textAlign: "center"
  },
  labelActive: {
    color: IG.accent,
    fontWeight: "800"
  },
  indicator: {
    width: 18,
    height: 3,
    borderRadius: ignitionTokens.radii.pill,
    backgroundColor: "transparent"
  },
  indicatorActive: {
    backgroundColor: IG.accent
  }
});

function IgnitionTabIcon({ name, isActive }) {
  const stroke = isActive ? IG.accent : IG.textMuted;
  const common = {
    fill: "none",
    stroke,
    strokeWidth: 1.9,
    strokeLinecap: "round",
    strokeLinejoin: "round"
  };

  if (name === "gauge") {
    return el(Svg, { width: 26, height: 22, viewBox: "0 0 26 22" },
      el(Path, { d: "M3.4 17.4a10.2 10.2 0 0 1 19.2 0", ...common }),
      el(Line, { x1: 13, y1: 16.6, x2: 18.2, y2: 10.4, ...common }),
      el(Circle, { cx: 13, cy: 16.8, r: 2, fill: stroke })
    );
  }

  if (name === "clients") {
    return el(Svg, { width: 26, height: 22, viewBox: "0 0 26 22" },
      el(Circle, { cx: 9.6, cy: 7.4, r: 3.4, ...common }),
      el(Path, { d: "M3.4 18.6c0-3.4 2.8-5.6 6.2-5.6s6.2 2.2 6.2 5.6", ...common }),
      el(Circle, { cx: 18.4, cy: 8.4, r: 2.6, ...common }),
      el(Path, { d: "M17.6 13.2c2.9.2 5 2.1 5 4.9", ...common })
    );
  }

  if (name === "sales") {
    return el(Svg, { width: 26, height: 22, viewBox: "0 0 26 22" },
      el(Path, { d: "M7 2.6h12v16.8l-2.4-1.6-2.4 1.6-2.4-1.6-2.4 1.6-2.4-1.6V2.6Z", ...common }),
      el(Line, { x1: 10, y1: 7, x2: 16, y2: 7, ...common }),
      el(Line, { x1: 10, y1: 10.6, x2: 16, y2: 10.6, ...common }),
      el(Line, { x1: 10, y1: 14.2, x2: 13.6, y2: 14.2, ...common })
    );
  }

  if (name === "parts") {
    return el(Svg, { width: 26, height: 22, viewBox: "0 0 26 22" },
      el(Circle, { cx: 13, cy: 11, r: 5.4, ...common }),
      el(Circle, { cx: 13, cy: 11, r: 1.8, fill: stroke }),
      el(Path, { d: "M13 3.2v2.4M13 16.4v2.4M5.2 11h2.4M18.4 11h2.4M7.5 5.5l1.7 1.7M16.8 14.8l1.7 1.7M18.5 5.5l-1.7 1.7M9.2 14.8l-1.7 1.7", ...common })
    );
  }

  return el(Svg, { width: 26, height: 22, viewBox: "0 0 26 22" },
    el(Rect, { x: 4, y: 3, width: 7, height: 7, rx: 2, ...common }),
    el(Rect, { x: 15, y: 3, width: 7, height: 7, rx: 2, ...common }),
    el(Rect, { x: 4, y: 13, width: 7, height: 7, rx: 2, ...common }),
    el(Rect, { x: 15, y: 13, width: 7, height: 7, rx: 2, ...common })
  );
}

function IgnitionTabBar({ activeKey, onSelect, onMore }) {
  const { t } = useTheme();
  const insets = useSafeAreaInsets();
  const bottomInset = Math.max(insets.bottom || 0, 8);

  return el(View, { style: [styles.bar, { paddingBottom: bottomInset }] },
    ignitionTabs.map((tab) => {
      const isActive = tab.key === activeKey;
      const isMore = tab.key === "__more";
      const label = isMore
        ? t("ignition.tabs.more", tab.fallbackLabel)
        : t(`ignition.tabs.${tab.key}`, tab.fallbackLabel);

      return el(Pressable, {
        key: tab.key,
        accessibilityLabel: label,
        accessibilityRole: "button",
        accessibilityState: { selected: isActive },
        hitSlop: 6,
        style: [styles.tab, isActive && styles.tabActive],
        onPress: () => (isMore ? onMore && onMore() : onSelect(tab.key))
      },
        el(IgnitionTabIcon, { name: tab.icon, isActive }),
        el(Text, { style: [styles.label, isActive && styles.labelActive], numberOfLines: 1 }, label),
        el(View, { style: [styles.indicator, isActive && styles.indicatorActive] })
      );
    })
  );
}

module.exports = { IgnitionTabBar };
