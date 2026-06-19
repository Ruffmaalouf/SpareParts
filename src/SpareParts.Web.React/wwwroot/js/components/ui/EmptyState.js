import { h } from "../../core/react-runtime.js";

// Rebuilt empty state: icon region, copy region, action region — backward
// compatible with the legacy {title, subtitle, action} contract from
// components/shared.js.
export function EmptyState({ title, subtitle, action, icon, tone = "neutral" }) {
  return h("div", { className: `empty-state-v2 empty-state-v2-${tone}` },
    h("div", { className: "empty-state-v2-icon", "aria-hidden": "true" },
      icon || h("svg", { viewBox: "0 0 24 24" }, h("path", { d: "M4 7h16M4 12h16M4 17h10" }))
    ),
    h("div", { className: "empty-state-v2-copy" },
      h("strong", null, title),
      subtitle ? h("p", null, subtitle) : null
    ),
    action && h("div", { className: "empty-state-v2-action" }, action)
  );
}
