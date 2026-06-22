import { h } from "../../core/react-runtime.js";
import { Icon } from "./CockpitIcons.js";

// Cockpit topbar: mobile menu toggle, global search slot, New Sale CTA, grouped
// utility icons, language pill and profile pill — connected to the sidebar via
// the shared black-glass surface.
export function Topbar({ onMenuToggle, search, user, onNewSale, notificationsCount, languageKey, actions }) {
  const displayName = user?.fullName || user?.name || user?.username || "Admin";
  const initials = String(displayName).slice(0, 2).toUpperCase();
  const badge = Number(notificationsCount || 0);

  return h("header", { className: "ck-topbar" },
    h("button", { type: "button", className: "ck-menu-btn", onClick: onMenuToggle, "aria-label": "Toggle navigation" },
      h(Icon, { name: "menu" })
    ),
    h("div", { className: "ck-search-bar" },
      h(Icon, { name: "search", size: 16 }),
      h("div", { className: "ck-search-slot" }, search),
      h("span", { className: "ck-kbd" }, "Ctrl K")
    ),
    h("div", { className: "ck-topbar-spacer" }),
    onNewSale && h("button", { type: "button", className: "ck-btn-primary", onClick: onNewSale },
      h(Icon, { name: "cart", size: 16 }), "New Sale", h(Icon, { name: "chevron", size: 13 })
    ),
    h("div", { className: "ck-tb-divider" }),
    h("button", { type: "button", className: "ck-icon-btn", "aria-label": "Quick copy" }, h(Icon, { name: "dup", size: 17 })),
    h("button", { type: "button", className: "ck-icon-btn", "aria-label": "Notifications" },
      h(Icon, { name: "bell", size: 17 }),
      badge > 0 && h("span", { className: "ck-badge-dot" }, badge > 99 ? "99+" : badge)
    ),
    actions ? h("div", { className: "ck-utility-slot" }, actions) : null,
    h("div", { className: "ck-tb-divider" }),
    h("div", { className: "ck-profile-pill" },
      h("div", { className: "ck-av" }, initials),
      h("div", null,
        h("div", { className: "ck-nm" }, displayName),
        h("div", { className: "ck-rl" }, languageKey ? String(languageKey).toUpperCase() : "EN")
      ),
      h(Icon, { name: "chevron", size: 12 })
    )
  );
}
