import { h } from "../../core/react-runtime.js";
import { Icon } from "../ui/CockpitIcons.js";

// Ignition IconRail (Report 03 §03) — 72px collapsed rail; hover/focus
// reveals the label flyout via CSS :hover/:focus-within, no JS state.
// `items` come from the screen registry's curated keys (same source as the
// legacy Sidebar railItems); `onSelect` delegates to the onView callback
// already threaded through every view. Icons come from the existing
// CockpitIcons registry (kept — the icon set is design-agnostic).
export function IconRail({ items = [], activeKey, onSelect }) {
  return h("nav", { className: "ig-rail", "aria-label": "Primary" },
    items.map((item) =>
      h("button", {
        key: item.key,
        type: "button",
        className: item.key === activeKey ? "ig-rail-item is-active" : "ig-rail-item",
        "aria-current": item.key === activeKey ? "page" : undefined,
        onClick: () => onSelect && onSelect(item.key)
      },
        h(Icon, { name: item.icon || "box", size: 20 }),
        h("span", { className: "ig-sr-only" }, item.label),
        h("span", { className: "ig-rail-flyout", "aria-hidden": "true" }, item.label),
        item.badge ? h("span", { className: "ig-rail-badge", "aria-hidden": "true" }) : null
      ))
  );
}
