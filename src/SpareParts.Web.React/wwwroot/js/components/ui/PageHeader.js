import { h } from "../../core/react-runtime.js";

// Rebuilt PageHeader: title + kicker + subtitle, right-aligned action slot
// supporting multiple buttons via `actions: []`, plus an optional stats row.
// Backward compatible with the single `action` prop.
export function PageHeader({ title, action, actions, kicker, subtitle, stats }) {
  const actionList = Array.isArray(actions) ? actions : null;

  return h("header", { className: "page-header page-header-v2" },
    h("div", { className: "page-header-v2-top" },
      h("div", { className: "page-title" },
        kicker && h("span", { className: "page-header-v2-kicker" }, kicker),
        h("h2", null, title),
        subtitle && h("p", null, subtitle)
      ),
      h("div", { className: "page-header-v2-actions" },
        actionList ? actionList.map((node, index) => h("span", { key: index, className: "page-header-v2-action" }, node)) : null,
        action || null
      )
    ),
    Array.isArray(stats) && stats.length > 0 && h("div", { className: "page-header-v2-stats" },
      stats.map((stat, index) =>
        h("div", { className: "page-header-v2-stat", key: stat.key || index },
          h("span", null, stat.label),
          h("strong", null, stat.value)
        )
      )
    )
  );
}
