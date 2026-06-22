import { h } from "../../core/react-runtime.js";

// KPI tile with three distinct nested regions: icon, value, trend.
export function StatsCard({ label, value, trend, icon, tone = "neutral", onClick }) {
  const trendDirection = trend && Number(trend.delta) < 0 ? "down" : "up";
  const Tag = onClick ? "button" : "div";

  return h(Tag, {
    className: `stats-card stats-card-${tone}`,
    type: onClick ? "button" : undefined,
    onClick: onClick || undefined
  },
    h("div", { className: "stats-card-icon", "aria-hidden": "true" },
      icon || h("svg", { viewBox: "0 0 24 24" }, h("path", { d: "M4 19h16M6 17l3-5 3 2 5-8 2 3" }))
    ),
    h("div", { className: "stats-card-value-region" },
      h("span", { className: "stats-card-label" }, label),
      h("strong", { className: "stats-card-value" }, value)
    ),
    trend && h("div", { className: `stats-card-trend stats-card-trend-${trendDirection}` },
      h("svg", { viewBox: "0 0 24 24", "aria-hidden": "true" },
        h("path", { d: trendDirection === "up" ? "M6 16l5-5 4 4 5-7" : "M6 8l5 5 4-4 5 7" })
      ),
      h("span", null, trend.label || trend.delta)
    )
  );
}
