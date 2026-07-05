import { h } from "../../core/react-runtime.js";

// Ignition InsightPanel (Report 03 §03) — generic elevated card shell that
// replaces the ad-hoc ck-panel divs. Every dashboard panel (Inventory
// Snapshot, Top Categories, Sales Overview, Recent Activity) is one
// InsightPanel instance with different children.
export function InsightPanel({ title, subtitle, children, footer, headerAction }) {
  return h("section", { className: "ig-panel", "aria-label": title },
    (title || subtitle || headerAction) && h("div", { className: "ig-panel-head" },
      h("div", null,
        title && h("h3", null, title),
        subtitle && h("div", { className: "ig-panel-sub" }, subtitle)
      ),
      headerAction || null
    ),
    children,
    footer && h("div", { className: "ig-panel-foot" }, footer)
  );
}
