import { h } from "../../core/react-runtime.js";
import { Icon } from "./CockpitIcons.js";

// Languages shown in the cockpit topbar pill (matches the approved mockup's
// clean "EN ▾" dropdown — no theme color rail, no EN/AR/FR segment clutter).
const languageOptions = [
  { key: "en", label: "EN" },
  { key: "ar", label: "AR" },
  { key: "fr", label: "FR" }
];

// Cockpit topbar: mobile menu toggle, global search slot, New Sale CTA, grouped
// utility icons, a single language dropdown and the profile pill — connected to
// the sidebar via the shared black-glass surface.
export function Topbar({ onMenuToggle, search, user, onNewSale, notificationsCount, languageKey, onLanguage }) {
  const displayName = user?.fullName || user?.name || user?.username || "Admin";
  const roleLabel = user?.roleName || user?.role || "Administrator";
  const initials = String(displayName).slice(0, 2).toUpperCase();
  const badge = Number(notificationsCount || 0);
  const currentLanguage = String(languageKey || "en").toLowerCase();

  return h("header", { className: "ck-topbar" },
    h("button", { type: "button", className: "ck-menu-btn", onClick: onMenuToggle, "aria-label": "Toggle navigation" },
      h(Icon, { name: "menu" })
    ),
    h("div", { className: "ck-search-bar" },
      h(Icon, { name: "search", size: 16 }),
      h("div", { className: "ck-search-slot" }, search),
      h("span", { className: "ck-kbd" }, "⌘K")
    ),
    h("div", { className: "ck-topbar-spacer" }),
    onNewSale && h("button", { type: "button", className: "ck-btn-primary", onClick: onNewSale },
      h(Icon, { name: "plus", size: 16 }), "New Sale", h(Icon, { name: "chevron", size: 13 })
    ),
    h("div", { className: "ck-tb-divider" }),
    h("button", { type: "button", className: "ck-icon-btn", "aria-label": "Quick copy" }, h(Icon, { name: "dup", size: 17 })),
    h("button", { type: "button", className: "ck-icon-btn", "aria-label": "Notifications" },
      h(Icon, { name: "bell", size: 17 }),
      badge > 0 && h("span", { className: "ck-badge-dot" }, badge > 99 ? "99+" : badge)
    ),
    h("label", { className: "ck-lang-pill" },
      h("span", { className: "ck-lang-val" }, (languageOptions.find((option) => option.key === currentLanguage) || languageOptions[0]).label),
      h(Icon, { name: "chevron", size: 12, className: "ck-icn ck-lang-arrow" }),
      h("select", {
        className: "ck-lang-select",
        value: currentLanguage,
        "aria-label": "Language",
        onChange: (event) => { if (typeof onLanguage === "function") onLanguage(event.target.value); }
      }, languageOptions.map((option) => h("option", { key: option.key, value: option.key }, option.label)))
    ),
    h("div", { className: "ck-tb-divider" }),
    h("div", { className: "ck-profile-pill" },
      h("div", { className: "ck-av" }, initials),
      h("div", null,
        h("div", { className: "ck-nm" }, displayName),
        h("div", { className: "ck-rl" }, roleLabel)
      ),
      h(Icon, { name: "chevron", size: 12 })
    )
  );
}
