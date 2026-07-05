import { h } from "../../core/react-runtime.js";
import { Icon } from "../ui/CockpitIcons.js";

// Ignition QuickActionDock (Report 03 §04) — grid of primary shortcuts on
// desktop; sticks as a bottom-right dock on tablet/phone via components.css.
// `actions` is Array<{key,label,icon,onClick}>.
export function QuickActionDock({ actions = [] }) {
  return h("div", { className: "ig-dock", role: "group", "aria-label": "Quick actions" },
    actions.map((action) =>
      h("button", {
        key: action.key || action.label,
        type: "button",
        className: "ig-dock-btn",
        onClick: action.onClick
      },
        h("span", { className: "ig-dock-icon", "aria-hidden": "true" }, h(Icon, { name: action.icon || "bolt", size: 16 })),
        action.label
      ))
  );
}
