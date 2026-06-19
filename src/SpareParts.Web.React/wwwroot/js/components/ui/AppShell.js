import { h, useState } from "../../core/react-runtime.js";

// Structural shell: collapsible sidebar rail + topbar + content region.
// Slots: sidebar (node or render-prop receiving {collapsed}), topbar (node),
// children (main content).
export function AppShell({ sidebar, topbar, children, collapsedDefault = false }) {
  const [collapsed, setCollapsed] = useState(collapsedDefault);

  const sidebarContent = typeof sidebar === "function" ? sidebar({ collapsed, setCollapsed }) : sidebar;

  return h("div", { className: collapsed ? "app-shell-v2 is-collapsed" : "app-shell-v2" },
    h("div", { className: "app-shell-sidebar-rail" },
      h("button", {
        type: "button",
        className: "sidebar-collapse-toggle",
        onClick: () => setCollapsed((value) => !value),
        "aria-label": collapsed ? "Expand navigation" : "Collapse navigation",
        title: collapsed ? "Expand navigation" : "Collapse navigation"
      },
        h("svg", { viewBox: "0 0 24 24", "aria-hidden": "true" },
          h("path", { d: collapsed ? "M9 6l6 6-6 6" : "M15 6l-6 6 6 6" })
        )
      ),
      sidebarContent
    ),
    h("div", { className: "app-shell-main-column" },
      h("div", { className: "app-shell-topbar-slot" }, topbar),
      h("div", { className: "app-shell-content-region" },
        h("div", { className: "app-shell-content-inner" }, children)
      )
    )
  );
}
