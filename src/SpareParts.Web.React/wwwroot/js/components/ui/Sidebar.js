import { h } from "../../core/react-runtime.js";
import { SUPER_ADMIN_ROLE_ID, superAdminOnlyKeys } from "../layout.js";
import { Icon } from "./CockpitIcons.js";

// Rail: illuminated cockpit shortcut buttons mapped to real screen keys.
const railItems = [
  { key: "dashboard", icon: "grid" },
  { key: "invoices", icon: "cart" },
  { key: "inventory", icon: "box" },
  { key: "compatibility", icon: "link" },
  { key: "used-cars", icon: "car" },
  { key: "accounting", icon: "bank" },
  { key: "contacts", icon: "users" }
];

// Curated cockpit navigation groups (match the approved mockup) mapped onto the
// real screen registry keys. Any registered screen not listed here is appended
// to a "More" group so every existing route stays reachable.
const cockpitGroups = [
  { key: "control", label: "Control Center", items: ["dashboard"] },
  { key: "sales", label: "Sales", items: ["invoices", "sales-returns"] },
  { key: "parts", label: "Parts & Inventory", items: ["inventory", "part-requests"] },
  { key: "compatibility", label: "Compatibility", items: ["compatibility", "does-it-fit"] },
  { key: "donor", label: "Donor & Vehicles", items: ["used-cars", "stock", "stock-arrival"] },
  { key: "finance", label: "Finance", items: ["billing", "report-builder", "accounting"] },
  { key: "users", label: "Users & Settings", items: ["management", "settings"] }
];

const keyIcon = {
  dashboard: "grid", invoices: "cart", "sales-returns": "return", inventory: "box",
  "part-requests": "clipboard", compatibility: "search", "does-it-fit": "car",
  "used-cars": "car", stock: "warehouse", "stock-arrival": "pkg", billing: "doc",
  "report-builder": "trend", accounting: "bank", management: "users", settings: "cog"
};

function iconFor(key) {
  return keyIcon[key] || "box";
}

export function Sidebar({ screens, view, onView, onLogout, t, user, closeNav }) {
  const roleId = Number(user?.roleId ?? user?.RoleId);
  const visible = (screens || []).filter((screen) => !superAdminOnlyKeys.has(screen.key) || roleId === SUPER_ADMIN_ROLE_ID);
  const screenMap = new Map(visible.map((screen) => [screen.key, screen]));

  const usedKeys = new Set(cockpitGroups.flatMap((group) => group.items));
  const groups = cockpitGroups
    .map((group) => ({ ...group, screens: group.items.map((key) => screenMap.get(key)).filter(Boolean) }))
    .filter((group) => group.screens.length > 0);
  const moreScreens = visible.filter((screen) => !usedKeys.has(screen.key));
  if (moreScreens.length) groups.push({ key: "more", label: "More Modules", screens: moreScreens });

  const select = (key) => {
    onView(key);
    if (typeof closeNav === "function") closeNav();
  };

  const displayName = user?.fullName || user?.name || user?.username || "Administrator";
  const roleLabel = roleId === SUPER_ADMIN_ROLE_ID ? "Super Admin" : roleId ? `Role ${roleId}` : "Operator";
  const initials = String(displayName).slice(0, 2).toUpperCase();

  return h("div", { className: "ck-sidebar-wrap", style: { display: "contents" } },
    // ===== Rail =====
    h("div", { className: "ck-rail" },
      h("div", { className: "ck-logo-mark" }, "M"),
      railItems.map((item) => screenMap.get(item.key) && h("button", {
        key: item.key,
        type: "button",
        className: item.key === view ? "ck-rail-ico is-active" : "ck-rail-ico",
        title: t(`screens.${item.key}`, screenMap.get(item.key)?.label || item.key),
        onClick: () => select(item.key)
      }, h(Icon, { name: item.icon }))),
      h("div", { className: "ck-rail-spacer" }),
      screenMap.get("settings") && h("button", {
        type: "button",
        className: "settings" === view ? "ck-rail-ico is-active" : "ck-rail-ico",
        title: t("screens.settings", "Settings"),
        onClick: () => select("settings")
      }, h(Icon, { name: "cog" })),
      h("div", { className: "ck-rail-divider" }),
      h("div", { className: "ck-rail-avatar" }, initials)
    ),
    // ===== Sidebar panel =====
    h("aside", { className: "ck-sidebar" },
      h("div", { className: "ck-brand" },
        h("div", { className: "ck-brand-mark" }, "M"),
        h("div", { className: "ck-brand-title" }, t("app.brand", "MAALOUF"), h("small", null, t("app.subtitle", "AUTO PARTS")))
      ),
      h("nav", { className: "ck-nav-scroll", "aria-label": t("nav.admin", "Admin navigation") },
        groups.map((group) =>
          h("div", { key: group.key },
            h("div", { className: "ck-nav-group" }, t(`nav.${group.key}`, group.label)),
            group.screens.map(({ key, label }) =>
              h("button", {
                key,
                type: "button",
                className: key === view ? "ck-nav-item is-active" : "ck-nav-item",
                onClick: () => select(key)
              },
                h(Icon, { name: iconFor(key), className: "ck-icn ck-ic" }),
                h("span", { className: "ck-nav-item-label" }, t(`screens.${key}`, label))
              )
            )
          )
        )
      ),
      h("div", { className: "ck-sidebar-bottom" },
        h("div", { className: "ck-user-card" },
          h("span", { className: "ck-av" }, initials),
          h("div", null,
            h("div", { className: "ck-nm" }, displayName),
            h("div", { className: "ck-rl" }, t("common.online", "Online"), h("span", { className: "ck-role-badge" }, roleLabel.toUpperCase()))
          )
        ),
        onLogout && h("button", { type: "button", className: "ck-signout", onClick: onLogout },
          h(Icon, { name: "power", size: 14 }), t("common.signOut", "Sign out"))
      )
    )
  );
}
