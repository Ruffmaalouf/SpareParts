import { h } from "../../core/react-runtime.js";

// Port of the ckLinePath() math from dashboard-view.js (logic ported, not
// rewritten), hardened for the single-point case: one data point renders a
// flat reference line instead of producing NaN from division by zero.
function linePath(values, width, height) {
  const max = Math.max(...values, 1);
  const pad = 14;
  const step = values.length > 1 ? width / (values.length - 1) : 0;
  const points = values.map((value, index) => ({
    x: values.length > 1 ? Math.round(index * step) : Math.round(width / 2),
    y: Math.round(height - pad - (Number(value || 0) / max) * (height - pad * 2))
  }));
  if (points.length === 1) {
    points.unshift({ x: 0, y: points[0].y });
    points.push({ x: width, y: points[1].y });
  }
  const line = points.map((point, index) => `${index === 0 ? "M" : "L"}${point.x},${point.y}`).join(" ");
  const area = `${line} L${points[points.length - 1].x},${height} L${points[0].x},${height} Z`;
  return { line, area, points };
}

// Ignition TrendChart (Report 03 §03). `series` is Array<{label,value}> —
// the same shape as the legacy weekly array. All inline SVG, reusing the
// gradient-fill technique from dashboard-view.js; stroke/fill reference
// --ig-accent so theme switching repaints for free. States: default,
// loading (skeleton bars, fixed height — no layout shift), empty,
// single-point.
export function TrendChart({ series = [], variant = "area", height = 168, formatValue, loading = false }) {
  const width = 480;

  if (loading) {
    return h("div", { className: "ig-trend", style: { height: `${height}px`, display: "flex", alignItems: "flex-end", gap: "8px" }, "aria-hidden": "true" },
      [0.4, 0.7, 0.55, 0.8, 0.65, 1, 0.85].map((factor, index) =>
        h("span", { key: index, className: "ig-skeleton", style: { flex: 1, height: `${Math.round(factor * (height - 24))}px` } }))
    );
  }

  if (!series.length) {
    return h("div", { className: "ig-trend ig-trend-empty", style: { height: `${height}px` } },
      "No sales recorded this period");
  }

  const values = series.map((point) => Number(point?.value || 0));
  const peakIndex = values.reduce((best, value, index) => (value > values[best] ? index : best), 0);
  const format = typeof formatValue === "function" ? formatValue : (value) => String(value);

  let plot;
  if (variant === "bar") {
    const max = Math.max(...values, 1);
    const barWidth = width / values.length;
    plot = values.map((value, index) =>
      h("rect", {
        key: index,
        x: Math.round(index * barWidth + barWidth * 0.15),
        y: Math.round(height - 8 - (value / max) * (height - 24)),
        width: Math.round(barWidth * 0.7),
        height: Math.max(2, Math.round((value / max) * (height - 24))),
        rx: 3,
        fill: index === peakIndex ? "var(--ig-accent)" : "var(--ig-accent-soft)"
      }));
  } else {
    const chart = linePath(values, width, height);
    plot = [
      variant === "area" && h("path", { key: "area", d: chart.area, fill: "url(#igTrendArea)" }),
      h("path", { key: "line", d: chart.line, fill: "none", stroke: "var(--ig-accent)", strokeWidth: 2.6, strokeLinecap: "round", strokeLinejoin: "round" }),
      series.length > 1 && chart.points.map((point, index) =>
        h("circle", {
          key: `pt-${index}`, cx: point.x, cy: point.y,
          r: index === peakIndex ? 4 : 2.2,
          fill: "var(--ig-bg-surface)",
          stroke: "var(--ig-accent)",
          strokeWidth: index === peakIndex ? 2.2 : 1.4
        }))
    ];
  }

  return h("div", { className: "ig-trend", role: "img", "aria-label": `Sales trend: peak ${series[peakIndex]?.label || ""} at ${format(values[peakIndex])}` },
    h("svg", { width: "100%", height, viewBox: `0 0 ${width} ${height}`, preserveAspectRatio: "none" },
      h("defs", null,
        h("linearGradient", { id: "igTrendArea", x1: 0, y1: 0, x2: 0, y2: 1 },
          h("stop", { offset: "0%", stopColor: "#f97316", stopOpacity: 0.35 }),
          h("stop", { offset: "100%", stopColor: "#f97316", stopOpacity: 0 }))),
      [0.25, 0.5, 0.75].map((factor) =>
        h("line", { key: factor, x1: 0, y1: Math.round(height * factor), x2: width, y2: Math.round(height * factor), stroke: "var(--ig-border)" })),
      plot
    ),
    h("div", { className: "ig-trend-axis", "aria-hidden": "true" },
      series.map((point, index) => h("span", { key: point.label || index }, point.label)))
  );
}
