import { h, useRef } from "../../core/react-runtime.js";

// Ignition TabStrip (Report 03 §04) — underline-style tab row driving the
// Client Workspace regions. Standard ARIA tabs pattern (role="tablist" /
// role="tab" aria-selected aria-controls) with roving tabindex and
// arrow-key navigation (Report 03 §09/§11).
// `tabs` is Array<{key,label,count?}>.
export function TabStrip({ tabs = [], activeKey, onChange, label = "Sections" }) {
  const stripRef = useRef(null);

  const focusTab = (index) => {
    const buttons = stripRef.current ? stripRef.current.querySelectorAll("[role=tab]") : [];
    const next = buttons[(index + tabs.length) % tabs.length];
    if (next) next.focus();
  };

  const handleKeyDown = (event, index) => {
    if (event.key === "ArrowRight") {
      event.preventDefault();
      focusTab(index + 1);
    } else if (event.key === "ArrowLeft") {
      event.preventDefault();
      focusTab(index - 1);
    } else if (event.key === "Home") {
      event.preventDefault();
      focusTab(0);
    } else if (event.key === "End") {
      event.preventDefault();
      focusTab(tabs.length - 1);
    }
  };

  return h("div", { className: "ig-tabstrip", role: "tablist", "aria-label": label, ref: stripRef },
    tabs.map((tab, index) =>
      h("button", {
        key: tab.key,
        type: "button",
        role: "tab",
        id: `ig-tab-${tab.key}`,
        className: "ig-tab",
        "aria-selected": tab.key === activeKey ? "true" : "false",
        "aria-controls": `ig-tabpanel-${tab.key}`,
        tabIndex: tab.key === activeKey ? 0 : -1,
        onClick: () => onChange && onChange(tab.key),
        onKeyDown: (event) => handleKeyDown(event, index)
      },
        tab.label,
        tab.count != null && h("span", { className: "ig-tab-count" }, String(tab.count))
      ))
  );
}
