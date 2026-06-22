import { h, useState } from "../../core/react-runtime.js";

// Cockpit Control Center frame: carbon rail + sidebar (rendered by the sidebar
// slot) beside a main column (topbar + scrolling content). Owns the mobile
// nav-drawer open state and exposes a toggle to the topbar slot.
export function AppShell({ sidebar, topbar, children }) {
  const [navOpen, setNavOpen] = useState(false);

  const closeNav = () => setNavOpen(false);
  const toggleNav = () => setNavOpen((value) => !value);

  const sidebarContent = typeof sidebar === "function" ? sidebar({ closeNav }) : sidebar;
  const topbarContent = typeof topbar === "function" ? topbar({ onMenuToggle: toggleNav }) : topbar;

  return h("div", { className: navOpen ? "ck-shell is-nav-open" : "ck-shell" },
    h("div", { className: "ck-nav-overlay", onClick: closeNav, "aria-hidden": "true" }),
    sidebarContent,
    h("div", { className: "ck-main" },
      topbarContent,
      h("div", { className: "ck-content" }, children)
    )
  );
}
