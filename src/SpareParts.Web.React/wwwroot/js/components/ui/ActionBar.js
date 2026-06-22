import { h } from "../../core/react-runtime.js";

// Sticky bottom/top action bar: a summary region (e.g. "3 selected") on one
// side and a button cluster region on the other — used for bulk actions and
// form save/cancel bars so they don't scroll away with content.
export function ActionBar({ summary, children, sticky = true, tone = "neutral" }) {
  return h("div", { className: `action-bar-v2 action-bar-v2-${tone} ${sticky ? "is-sticky" : ""}`.trim() },
    summary && h("div", { className: "action-bar-v2-summary" }, summary),
    h("div", { className: "action-bar-v2-buttons" }, children)
  );
}
