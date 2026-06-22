import { h, useMemo, useState } from "../../core/react-runtime.js";
import { BrandMark, NavIcon, SUPER_ADMIN_ROLE_ID, navGroups, superAdminOnlyKeys } from "../layout.js";

function normalize(value) {
  return String(value || "").trim().toLowerCase();
}

// Rebuilt navigation: collapsible grouped sections (expand state per group),
// active-item marker element, and a search-within-nav filter so the large
// "Other" bucket of ungrouped screens stays usable.
export function Sidebar({ screens, view, onView, onLogout, t, user, collapsed = false }) {
  const roleId = Number(user?.roleId ?? user?.RoleId);
  const visibleScreens = screens.filter((screen) => !superAdminOnlyKeys.has(screen.key) || roleId === SUPER_ADMIN_ROLE_ID);
  const screenMap = new Map(visibleScreens.map((screen) => [screen.key, screen]));
  const groupedKeys = new Set(navGroups.flatMap((group) => group.items));
  const extraScreens = visibleScreens.filter((screen) => !groupedKeys.has(screen.key));
  const groups = useMemo(() => [
    ...navGroups.map((group) => ({
      ...group,
      screens: group.items.map((key) => screenMap.get(key)).filter(Boolean)
    })),
    extraScreens.length ? { key: "extra", label: "Other", screens: extraScreens } : null
  ].filter((group) => group && group.screens.length > 0), [extraScreens, screenMap]);

  const [expanded, setExpanded] = useState(() => {
    const initial = {};
    groups.forEach((group) => { initial[group.key] = true; });
    return initial;
  });
  const [navFilter, setNavFilter] = useState("");

  const toggleGroup = (key) => {
    setExpanded((current) => ({ ...current, [key]: !current[key] }));
  };

  const query = normalize(navFilter);
  const filteredGroups = query
    ? groups
      .map((group) => ({
        ...group,
        screens: group.screens.filter((screen) => normalize(screen.label).includes(query) || normalize(screen.key).includes(query))
      }))
      .filter((group) => group.screens.length > 0)
    : groups;

  const displayName = user?.fullName || user?.name || user?.username || "Administrator";

  return h("aside", { className: collapsed ? "sidebar sidebar-v2 is-collapsed" : "sidebar sidebar-v2" },
    h("div", { className: "sidebar-brand" },
      h(BrandMark, { size: "small", label: t("login.brand", "Maalouf Auto Parts") }),
      !collapsed && h("div", null,
        h("strong", null, t("app.brand", "Maalouf")),
        h("span", null, t("app.subtitle", "Auto Parts"))
      ),
      !collapsed && h("b", { className: "sidebar-live" }, t("common.live", "Live"))
    ),
    !collapsed && h("div", { className: "sidebar-nav-search" },
      h("svg", { className: "sidebar-nav-search-icon", viewBox: "0 0 24 24", "aria-hidden": "true" },
        h("circle", { cx: "11", cy: "11", r: "7" }),
        h("path", { d: "M21 21l-4.3-4.3" })
      ),
      h("input", {
        type: "search",
        value: navFilter,
        placeholder: t("nav.filterPlaceholder", "Filter screens..."),
        onChange: (event) => setNavFilter(event.target.value),
        "aria-label": t("nav.filterPlaceholder", "Filter screens...")
      }),
      navFilter && h("button", {
        type: "button",
        className: "sidebar-nav-search-clear",
        onClick: () => setNavFilter(""),
        "aria-label": "Clear filter"
      }, "x")
    ),
    h("nav", { className: "nav-list nav-list-v2", "aria-label": t("nav.admin", "Admin navigation") },
      filteredGroups.map((group) => {
        const isOpen = query ? true : expanded[group.key] !== false;
        return h("section", { className: isOpen ? "nav-section nav-section-v2 is-open" : "nav-section nav-section-v2", key: group.key },
          h("button", {
            type: "button",
            className: "nav-section-header",
            onClick: () => toggleGroup(group.key),
            "aria-expanded": isOpen
          },
            h("span", { className: "nav-section-title" }, t(`nav.${group.key}`, group.label)),
            !collapsed && h("svg", { className: "nav-section-chevron", viewBox: "0 0 24 24", "aria-hidden": "true" },
              h("path", { d: "M6 9l6 6 6-6" })
            )
          ),
          isOpen && h("div", { className: "nav-section-items" },
            group.screens.map(({ key, label }) => {
              const isActive = key === view;
              return h("button", {
                key,
                className: isActive ? "nav-item active" : "nav-item",
                onClick: () => onView(key),
                type: "button",
                title: collapsed ? t(`screens.${key}`, label) : undefined
              },
                h("span", { className: isActive ? "nav-item-marker active" : "nav-item-marker", "aria-hidden": "true" }),
                h(NavIcon, { name: key }),
                !collapsed && h("span", { className: "nav-item-label" }, t(`screens.${key}`, label))
              );
            })
          )
        );
      }),
      filteredGroups.length === 0 && h("p", { className: "nav-empty-state" }, t("nav.noMatches", "No screens match that filter."))
    ),
    h("footer", { className: "sidebar-footer" },
      h("div", { className: "user-panel" },
        h("span", { className: "avatar" }, String(displayName).slice(0, 2).toUpperCase()),
        !collapsed && h("div", null,
          h("strong", null, displayName),
          h("span", null, roleId ? `Role ID ${roleId}` : "Role ID")
        ),
        !collapsed && onLogout && h("button", { className: "ghost-button", type: "button", onClick: onLogout }, t("common.signOut", "Sign out"))
      )
    )
  );
}
