import { h } from "../../core/react-runtime.js";
import { Icon } from "../ui/CockpitIcons.js";

const toneColorVar = {
  success: "var(--ig-success)",
  danger: "var(--ig-danger)",
  info: "var(--ig-info)",
  orange: "var(--ig-accent)",
  neutral: "var(--ig-text-secondary)"
};

const toneSoftVar = {
  success: "var(--ig-success-soft)",
  danger: "var(--ig-danger-soft)",
  info: "var(--ig-info-soft)",
  orange: "var(--ig-accent-soft)",
  neutral: "var(--ig-bg-elevated)"
};

function sparkPoints(values, width, height) {
  const max = Math.max(...values, 1);
  const step = values.length > 1 ? width / (values.length - 1) : width;
  return values
    .map((value, index) => `${Math.round(index * step)},${Math.round(height - 2 - (Number(value || 0) / max) * (height - 4))}`)
    .join(" ");
}

// Ignition KpiTile (Report 03 §03). `value` arrives pre-formatted by the
// caller (via MoneyText or a ckNum-style formatter) — the tile does not
// format. `delta` keeps the {text,dir} shape of the existing ckPct/ckDelta
// helpers in dashboard-view.js. `sparkline` is an inline SVG polyline of
// up to 7 points — no chart library. Loading state renders a fixed-height
// skeleton so the band never shifts layout (Report 03 §10 CLS budget).
export function KpiTile({ label, value, delta, icon, sparkline, tone = "neutral", onClick, loading }) {
  const color = toneColorVar[tone] || toneColorVar.neutral;
  const soft = toneSoftVar[tone] || toneSoftVar.neutral;

  if (loading) {
    return h("div", { className: "ig-kpi-tile", "aria-hidden": "true" },
      h("span", { className: "ig-skeleton", style: { width: "28px", height: "28px" } }),
      h("span", { className: "ig-skeleton", style: { width: "60%", height: "10px" } }),
      h("span", { className: "ig-skeleton", style: { width: "80%", height: "20px" } })
    );
  }

  const deltaDir = delta ? (delta.dir === "down" ? "is-down" : delta.dir === "flat" ? "is-flat" : "is-up") : "";
  const body = [
    icon && h("span", { key: "icon", className: "ig-kpi-icon", style: { background: soft, color }, "aria-hidden": "true" },
      h(Icon, { name: icon, size: 14 })),
    h("span", { key: "label", className: "ig-label" }, label),
    h("span", { key: "value", className: "ig-kpi-value" }, value ?? "--"),
    delta && h("span", { key: "delta", className: `ig-kpi-delta ${deltaDir}`, "aria-label": `${delta.dir === "down" ? "down" : "up"} ${delta.text} versus yesterday` },
      h("span", { "aria-hidden": "true" }, delta.dir === "down" ? "▼" : delta.dir === "flat" ? "–" : "▲"),
      delta.text),
    Array.isArray(sparkline) && sparkline.length > 1 && h("svg", {
      key: "spark", className: "ig-kpi-spark", viewBox: "0 0 72 22", preserveAspectRatio: "none", "aria-hidden": "true"
    },
      h("polyline", { points: sparkPoints(sparkline.slice(-7), 72, 22), fill: "none", stroke: color, strokeWidth: 2 }))
  ];

  if (typeof onClick === "function") {
    return h("button", { type: "button", className: "ig-kpi-tile", onClick }, body);
  }
  return h("div", { className: "ig-kpi-tile" }, body);
}
