const React = require("react");
const { Pressable, Text, View } = require("react-native");
const SvgModule = require("react-native-svg");
const Svg = SvgModule.default;
const { Circle, Ellipse, G, Line, Path, Polygon, Polyline, Rect } = SvgModule;
const { useTheme } = require("../theme/theme-context");

const el = React.createElement;

const tabIcons = {
  contacts: "spark",
  dashboard: "gauge",
  invoices: "receipt",
  management: "spark",
  parts: "piston",
  settings: "settings",
  "store-account": "key",
  "store-cart": "cart",
  "store-checkout": "flag",
  "store-home": "gauge",
  "store-parts": "piston"
};

function iconShell(children) {
  return el(Svg, { width: 36, height: 30, viewBox: "0 0 36 30" }, ...children);
}

function TabIcon({ name, isActive, palette }) {
  const stroke = isActive ? palette.accent : palette.muted;
  const accent = isActive ? palette.text : palette.soft;
  const hot = isActive ? palette.danger : palette.soft;
  const glow = isActive ? "rgba(255,255,255,0.065)" : "rgba(255,255,255,0.035)";
  const ghost = isActive ? "rgba(255,255,255,0.2)" : "rgba(255,255,255,0.08)";
  const common = {
    fill: "none",
    stroke,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    strokeWidth: 2.05
  };
  const accentLine = {
    fill: "none",
    stroke: accent,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    strokeWidth: 2.15
  };

  if (name === "piston") {
    return iconShell([
      el(Circle, { cx: 14.2, cy: 15.1, r: 8.7, fill: glow, stroke, strokeWidth: 1.9 }),
      el(Circle, { cx: 14.2, cy: 15.1, r: 2.25, fill: stroke }),
      el(Path, { d: "M14.2 6.4v5.4M14.2 18.5v5.3M5.5 15.1h5.3M17.7 15.1h5.2M8 8.9l3.8 3.8M16.6 17.5l3.8 3.8M20.4 8.9l-3.8 3.8M11.8 17.5L8 21.3", stroke: ghost, strokeWidth: 1.25, strokeLinecap: "round" }),
      el(Path, { d: "M23.2 8.7c3.2 1.4 5.1 3.6 5.1 6.4s-1.9 5.1-5.1 6.4", stroke: hot, strokeWidth: 1.8, strokeLinecap: "round", fill: "none" }),
      el(Path, { d: "M26.8 10.8h3.3M26.8 19.5h3.3", stroke: hot, strokeWidth: 1.45, strokeLinecap: "round" }),
      el(Path, { d: "M6.2 25.1h19.5", ...accentLine }),
      el(Circle, { cx: 29.7, cy: 5.5, r: 2.1, fill: "none", stroke: accent, strokeWidth: 1.55 })
    ]);
  }

  if (name === "cart") {
    return iconShell([
      el(Path, { d: "M4.5 8.1h4.4l2.4 11.2h15.1l3.2-8.2H11.1", ...common }),
      el(Path, { d: "M13.2 13.2h13.7M14.4 16.2h10.1", stroke: ghost, strokeWidth: 1.25, strokeLinecap: "round" }),
      el(Path, { d: "M3.2 5.2h6.2", stroke: accent, strokeWidth: 2.2, strokeLinecap: "round" }),
      el(Path, { d: "M23.6 6.7l2.4-2.2M28.6 6.7l2.1-1.2", stroke: hot, strokeWidth: 1.55, strokeLinecap: "round" }),
      el(Circle, { cx: 14.4, cy: 24, r: 2.65, fill: stroke }),
      el(Circle, { cx: 25.5, cy: 24, r: 2.65, fill: stroke }),
      el(Circle, { cx: 14.4, cy: 24, r: 0.9, fill: palette.bg }),
      el(Circle, { cx: 25.5, cy: 24, r: 0.9, fill: palette.bg })
    ]);
  }

  if (name === "flag") {
    return iconShell([
      el(Line, { x1: 8.2, y1: 4.5, x2: 8.2, y2: 25.2, stroke, strokeWidth: 2.2, strokeLinecap: "round" }),
      el(Path, { d: "M8.2 5.2c7.1-3.8 11.4 3.7 20 0v12.2c-8.6 3.8-12.9-3.6-20 0V5.2Z", fill: glow, stroke, strokeWidth: 1.75, strokeLinejoin: "round" }),
      el(Path, { d: "M12 7.2h4.2v4.2H12ZM20.4 7.2h4.2v4.2h-4.2ZM16.2 11.4h4.2v4.2h-4.2ZM24.6 11.4h3.1v4.2h-3.1Z", fill: accent }),
      el(Path, { d: "M10.8 22.8h11.5", stroke: hot, strokeWidth: 1.65, strokeLinecap: "round" })
    ]);
  }

  if (name === "key") {
    return iconShell([
      el(Circle, { cx: 10.8, cy: 13.4, r: 5.4, fill: glow, stroke, strokeWidth: 2 }),
      el(Circle, { cx: 10.8, cy: 13.4, r: 1.7, fill: accent }),
      el(Path, { d: "M16 13.4h13.8", ...common }),
      el(Path, { d: "M24.1 13.4v4.6M28.9 13.4v-4", stroke: accent, strokeWidth: 2.05, strokeLinecap: "round" }),
      el(Path, { d: "M5.9 21.6l2.1-2.1M6.4 5l1.7 2.2M18.7 6.3l2.4-2", stroke: hot, strokeWidth: 1.55, strokeLinecap: "round" }),
      el(Path, { d: "M20 18.3h5", stroke: ghost, strokeWidth: 1.2, strokeLinecap: "round" })
    ]);
  }

  if (name === "receipt") {
    return iconShell([
      el(Path, { d: "M10 3.8h15v21.6l-3.1-2.1-3.1 2.1-3.1-2.1-3.1 2.1-2.6-1.7V3.8Z", fill: glow, stroke, strokeWidth: 1.85, strokeLinejoin: "round" }),
      el(Path, { d: "M14 9.1h7.9M14 13.4h8.8M14 17.5h5.8", stroke, strokeWidth: 1.6, strokeLinecap: "round" }),
      el(Path, { d: "M23.4 8.2h3.7M23.4 12.3h3.7", stroke: hot, strokeWidth: 1.45, strokeLinecap: "round" }),
      el(Path, { d: "M13.2 21.3h8.1", stroke: accent, strokeWidth: 1.9, strokeLinecap: "round" }),
      el(Polyline, { points: "6,9 8.2,7.1 6.8,5", fill: "none", stroke: accent, strokeWidth: 1.55, strokeLinecap: "round", strokeLinejoin: "round" })
    ]);
  }

  if (name === "spark") {
    return iconShell([
      el(Path, { d: "M14.5 4.2h8.4l1.5 3-2.1 2.2v7.3l-3 2.9h-5.1l-3-2.9V9.4L13 7.2l1.5-3Z", fill: glow, stroke, strokeWidth: 1.85, strokeLinejoin: "round" }),
      el(Path, { d: "M12.7 8.8h10.8M13.7 12.2h8.8M14.6 15.4h6.9", stroke: ghost, strokeWidth: 1.15, strokeLinecap: "round" }),
      el(Path, { d: "M16.2 19.6l-4.5 5.6h6.5l-2 3.1", stroke: accent, strokeWidth: 1.85, strokeLinecap: "round", strokeLinejoin: "round", fill: "none" }),
      el(Path, { d: "M25.6 14.1l4-2.5M25.8 18.5l3.7 1.4M8 13.4l-3.5-2", stroke: hot, strokeWidth: 1.5, strokeLinecap: "round" })
    ]);
  }

  if (name === "settings" || name === "wrench") {
    return iconShell([
      el(Path, { d: "M18 4.4v3M18 22.6v3M7 15h3M26 15h3M10.2 7.2l2.1 2.1M23.7 20.7l2.1 2.1M25.8 7.2l-2.1 2.1M12.3 20.7l-2.1 2.1", stroke: accent, strokeWidth: 1.65, strokeLinecap: "round" }),
      el(Circle, { cx: 18, cy: 15, r: 8, fill: glow, stroke, strokeWidth: 1.9 }),
      el(Circle, { cx: 18, cy: 15, r: 3.1, fill: "none", stroke, strokeWidth: 1.75 }),
      el(Path, { d: "M8.7 24.1l12.6-12.6", stroke, strokeWidth: 2.25, strokeLinecap: "round" }),
      el(Path, { d: "M21.5 7.7a4 4 0 0 1 4.8 5l-3.1-1.5-1.8 1.9 1.5 3.1a4.1 4.1 0 0 1-5-4.7", fill: palette.sidebar, stroke: hot, strokeWidth: 1.55, strokeLinejoin: "round" }),
      el(Circle, { cx: 8.3, cy: 24.5, r: 2.15, fill: "none", stroke: hot, strokeWidth: 1.5 })
    ]);
  }

  return iconShell([
    el(Ellipse, { cx: 18, cy: 21.8, rx: 12.1, ry: 2.4, fill: glow }),
    el(Path, { d: "M6.2 21.2a11.8 11.8 0 0 1 23.6 0", fill: "none", stroke, strokeWidth: 2.2, strokeLinecap: "round" }),
    el(Path, { d: "M8.8 18.2l-2.2-.8M12.2 13.3l-1.4-1.8M18 11.4V8.8M23.8 13.3l1.4-1.8M27.2 18.2l2.2-.8", stroke, strokeWidth: 1.35, strokeLinecap: "round" }),
    el(Path, { d: "M18 21.1l7.6-8.3", stroke: accent, strokeWidth: 2.25, strokeLinecap: "round" }),
    el(Circle, { cx: 18, cy: 21.2, r: 2.85, fill: stroke }),
    el(Path, { d: "M23.2 6.7c4.4 1.9 7.2 6.2 7.6 11.1", stroke: hot, strokeWidth: 1.75, strokeLinecap: "round" }),
    el(Polygon, { points: "28.3,7.2 31.2,7.9 29.1,10", fill: hot })
  ]);
}

function BottomTabBar({ activeKey, tabs, onSelect }) {
  const { palette, styles, t } = useTheme();

  return el(View, { style: styles.bottomTabBar },
    tabs.map((item) => {
      const isActive = item.key === activeKey;
      const label = t(`screens.${item.key}`, item.label);
      const description = item.description
        ? t(`tabs.descriptions.${item.key}`, item.description)
        : "";

      return el(Pressable, {
        key: item.key,
        accessibilityLabel: `${label}${description ? `, ${description}` : ""}`,
        accessibilityRole: "button",
        hitSlop: 6,
        style: [styles.bottomTabButton, isActive && styles.bottomTabButtonActive],
        onPress: () => onSelect(item.key)
      },
        el(View, { style: [styles.bottomTabMark, isActive && styles.bottomTabMarkActive] },
          el(TabIcon, { name: item.icon || tabIcons[item.key] || "gauge", isActive, palette })
        ),
        el(Text, {
          style: [styles.bottomTabLabel, isActive && styles.bottomTabLabelActive],
          numberOfLines: 1
        }, label),
        Boolean(description) && el(Text, {
          style: [styles.bottomTabDescription, isActive && styles.bottomTabDescriptionActive],
          numberOfLines: 1
        }, description)
      );
    })
  );
}

module.exports = { BottomTabBar };
