import { h } from "../../core/react-runtime.js";

// Filter rail: grouped filter sections (label + control region), a footer
// region for apply/reset actions, and an active-filter-count badge on the
// section heading for quick scanning.
export function FilterPanel({ title, groups, onReset, footer, collapsed }) {
  if (collapsed) return null;

  return h("aside", { className: "filter-panel-v2" },
    h("div", { className: "filter-panel-v2-head" },
      h("strong", null, title || "Filters"),
      onReset && h("button", { type: "button", className: "filter-panel-v2-reset", onClick: onReset }, "Reset")
    ),
    h("div", { className: "filter-panel-v2-groups" },
      (groups || []).map((group, index) => {
        const activeCount = Number(group.activeCount || 0);
        return h("section", { className: "filter-panel-v2-group", key: group.key || index },
          h("div", { className: "filter-panel-v2-group-head" },
            h("span", null, group.label),
            activeCount > 0 && h("b", { className: "filter-panel-v2-count" }, activeCount)
          ),
          h("div", { className: "filter-panel-v2-group-body" }, group.control)
        );
      })
    ),
    footer && h("div", { className: "filter-panel-v2-footer" }, footer)
  );
}
