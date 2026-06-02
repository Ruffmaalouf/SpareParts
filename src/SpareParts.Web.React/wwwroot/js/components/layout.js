import { h } from "../core/react-runtime.js";
import { languageOptions, wpfThemes } from "../core/config.js";

const iconPaths = {
  dashboard: "M4 13h6V4H4v9Zm10 7h6V4h-6v16ZM4 20h6v-5H4v5Zm10 0h6v-5h-6v5Z",
  invoices: "M6 3h12v18l-3-2-3 2-3-2-3 2V3Zm3 5h6M9 12h6M9 16h4",
  inventory: "M4 7h16v13H4V7Zm2-4h12v4H6V3Zm2 8h8M8 15h5",
  "part-passport": "M6 3h9l3 3v15H6V3Zm8 0v4h4M9 11h6m-6 4h6m-6 4h4",
  compatibility: "M6 12a3 3 0 1 0 0-6 3 3 0 0 0 0 6Zm12 7a3 3 0 1 0 0-6 3 3 0 0 0 0 6ZM18 8a2 2 0 1 0 0-4 2 2 0 0 0 0 4ZM8.5 10.5l7 4M8.6 7.7l7.6-2.1M8.6 9.2l7.8-3.1",
  "part-requests": "M5 5h14v10H8l-3 3V5Zm4 4h6m-6 3h4M17 17l2 2 4-5",
  contacts: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 8a7 7 0 0 1 14 0",
  management: "M4 5h7v7H4V5Zm9 0h7v7h-7V5ZM4 14h7v5H4v-5Zm9 0h7v5h-7v-5Z",
  settings: "M12 8a4 4 0 1 1 0 8 4 4 0 0 1 0-8Zm0-5 1.4 3 3.3.3-2.4 2.2.7 3.2-3-1.7-3 1.7.7-3.2-2.4-2.2 3.3-.3L12 3Z",
  "purchase-parts": "M5 6h14v12H5V6Zm3-3h8v3H8V3Zm1 7h6m-6 4h4",
  "used-car-purchases": "M4 15h2l2-5h8l2 5h2v4h-2a2 2 0 0 1-4 0h-4a2 2 0 0 1-4 0H4v-4Zm5-3-1 3h8l-1-3H9Z",
  "used-car-wholesale": "M3 15h2l2-6h10l2 6h2v4h-2a2 2 0 0 1-4 0H9a2 2 0 0 1-4 0H3v-4Zm5-4-1.2 4h10.4L16 11H8Zm7-7h6v4h-6V4Zm1 1v2h4V5h-4Z",
  "stock-arrival": "M4 5h16v4H4V5Zm2 7h5v7H6v-7Zm7 0h5v7h-5v-7Zm-8-2 3-3 3 3m2 0 3-3 3 3",
  "used-cars": "M3 15h2l2-6h10l2 6h2v4h-2a2 2 0 0 1-4 0H9a2 2 0 0 1-4 0H3v-4Zm5-4-1.2 4h10.4L16 11H8Z",
  "repair-prep": "M5 6h5l2 3h7v9H5V6Zm2 2v8h10v-5h-6L9 8H7Zm4 5 2-2 1.5 1.5L12.5 14l2 2L13 17.5l-2-2-2 2L7.5 16l2-2-2-2L9 10.5 11 13Z",
  stock: "M4 8 12 4l8 4-8 4-8-4Zm0 4 8 4 8-4M4 16l8 4 8-4",
  "dead-stock": "M5 4h14v16H5V4Zm3 4h8M8 12h5m-5 4h7M17 11l2 2-2 2",
  "growth-lab": "M4 19h16M6 17l3-5 3 2 5-8 2 3M17 6h3v3",
  accounting: "M5 4h14v16H5V4Zm3 4h8M8 12h8M8 16h5",
  "manual-journal": "M6 4h10l2 2v14H6V4Zm3 5h6M9 13h6M9 17h4",
  "report-builder": "M5 19V5h14v14H5Zm3-3h2v-5H8v5Zm4 0h2V8h-2v8Zm4 0h2v-3h-2v3Z",
  whatsapp: "M4 5a8 8 0 0 1 13 9l2 5-5-2A8 8 0 0 1 4 5Z",
  "business-assistant": "M12 3l1.4 4.1L18 8l-4.6 1L12 13l-1.4-4L6 8l4.6-.9L12 3Zm-5 10 .8 2.2L10 16l-2.2.8L7 19l-.8-2.2L4 16l2.2-.8L7 13Zm10 0 .8 2.2L20 16l-2.2.8L17 19l-.8-2.2L14 16l2.2-.8L17 13Z",
  ar: "M4 5h6v2H6v4H4V5Zm10 0h6v6h-2V7h-4V5ZM4 13h2v4h4v2H4v-6Zm14 0h2v6h-6v-2h4v-4ZM9 9h6v6H9V9Z",
  default: "M5 5h14v14H5V5Zm4 4h6v6H9V9Z"
};

const navGroups = [
  { key: "core", label: "Core", items: ["dashboard", "invoices", "inventory", "part-passport", "compatibility", "contacts", "management", "settings"] },
  { key: "operations", label: "Operations", items: ["growth-lab", "part-requests", "purchase-parts", "used-car-purchases", "used-car-wholesale", "stock-arrival", "used-cars", "repair-prep", "stock", "dead-stock", "reorder", "expiry-alerts", "loyalty", "warranty", "shipments"] },
  { key: "finance", label: "Finance", items: ["accounting", "manual-journal", "report-builder"] },
  { key: "tools", label: "Tools", items: ["whatsapp", "business-assistant", "ar", "activity-log"] }
];

function NavIcon({ name }) {
  return h("svg", {
    className: "nav-icon",
    viewBox: "0 0 24 24",
    "aria-hidden": "true"
  }, h("path", { d: iconPaths[name] || iconPaths.default }));
}

export function BrandMark({ label = "Maalouf Auto Parts", size = "", variant = "admin" }) {
  const className = [
    "brand-mark",
    "brand-logo",
    size,
    variant ? `brand-logo-${variant}` : ""
  ].filter(Boolean).join(" ");

  return h("span", { className, role: "img", "aria-label": label },
    h("span", { className: "brand-logo-core", "aria-hidden": "true" }),
    h("span", { className: "brand-logo-road", "aria-hidden": "true" }),
    h("span", { className: "brand-logo-spark", "aria-hidden": "true" })
  );
}

export function Sidebar({ screens, view, onView, onLogout, t, user }) {
  const screenMap = new Map(screens.map((screen) => [screen.key, screen]));
  const groupedKeys = new Set(navGroups.flatMap((group) => group.items));
  const extraScreens = screens.filter((screen) => !groupedKeys.has(screen.key));
  const groups = [
    ...navGroups.map((group) => ({
      ...group,
      screens: group.items.map((key) => screenMap.get(key)).filter(Boolean)
    })),
    extraScreens.length ? { key: "extra", label: "Other", screens: extraScreens } : null
  ].filter((group) => group && group.screens.length > 0);
  const displayName = user?.fullName || user?.name || user?.username || "Administrator";
  const roleId = user?.roleId ?? user?.RoleId;

  return h("aside", { className: "sidebar" },
    h("div", { className: "sidebar-brand" },
      h(BrandMark, { size: "small", label: t("login.brand", "Maalouf Auto Parts") }),
      h("div", null,
        h("strong", null, t("app.brand", "Maalouf")),
        h("span", null, t("app.subtitle", "Auto Parts"))
      ),
      h("b", { className: "sidebar-live" }, t("common.live", "Live"))
    ),
    h("nav", { className: "nav-list", "aria-label": t("nav.admin", "Admin navigation") },
      groups.map((group) =>
        h("section", { className: "nav-section", key: group.key },
          h("span", { className: "nav-section-title" }, t(`nav.${group.key}`, group.label)),
          group.screens.map(({ key, label }) =>
            h("button", {
              key,
              className: key === view ? "nav-item active" : "nav-item",
              onClick: () => onView(key),
              type: "button"
            },
              h(NavIcon, { name: key }),
              h("span", null, t(`screens.${key}`, label))
            )
          )
        )
      )
    ),
    h("footer", { className: "sidebar-footer" },
      h("div", { className: "user-panel" },
        h("span", { className: "avatar" }, String(displayName).slice(0, 2).toUpperCase()),
        h("div", null,
          h("strong", null, displayName),
          h("span", null, roleId ? `Role ID ${roleId}` : "Role ID")
        ),
        onLogout && h("button", { className: "ghost-button", type: "button", onClick: onLogout }, t("common.signOut", "Sign out"))
      )
    )
  );
}

export function ThemePicker({ value, onChange, t }) {
  return h("section", { className: "theme-picker", "aria-label": "WPF theme" },
    h("span", { className: "theme-title" }, t ? t("common.themes", "Themes") : "Themes"),
    h("div", { className: "theme-grid" },
      wpfThemes.map((theme) =>
        h("button", {
          key: theme.key,
          className: theme.key === value ? "theme-button active" : "theme-button",
          onClick: () => onChange(theme.key),
          title: theme.name,
          "aria-label": theme.name
        },
          h("span", {
            className: "theme-swatch",
            style: {
              background: theme.colors.accent,
              borderColor: theme.colors.line
            }
          }),
          h("span", null, theme.name)
        )
      )
    )
  );
}

export function ThemeRail({ value, onChange, t }) {
  const activeTheme = wpfThemes.find((theme) => theme.key === value) || wpfThemes[0];
  return h("section", { className: "admin-theme-rail", "aria-label": t ? t("common.themes", "Themes") : "Themes" },
    h("span", { className: "admin-control-label" }, t ? t("common.themes", "Themes") : "Themes"),
    h("strong", { className: "admin-theme-current" }, activeTheme.name),
    h("div", { className: "admin-theme-options" },
      wpfThemes.map((theme) =>
        h("button", {
          key: theme.key,
          className: theme.key === value ? "admin-theme-option active" : "admin-theme-option",
          onClick: () => onChange(theme.key),
          "aria-label": theme.name,
          title: theme.name,
          type: "button"
        },
          h("span", {
            className: "theme-swatch",
            style: {
              background: theme.colors.accent,
              borderColor: theme.colors.line
            }
          })
        )
      )
    )
  );
}

export function LanguageSegment({ value, onChange, t }) {
  return h("section", { className: "admin-language-segment", "aria-label": t ? t("settings.language", "Language") : "Language" },
    h("span", { className: "admin-control-label" }, t ? t("settings.language", "Language") : "Language"),
    h("div", { className: "admin-language-options" },
      languageOptions.map((language) =>
        h("button", {
          key: language.key,
          className: language.key === value ? "admin-language-option active" : "admin-language-option",
          onClick: () => onChange(language.key),
          type: "button"
        }, language.key.toUpperCase())
      )
    )
  );
}

export function LanguagePicker({ value, onChange, t }) {
  return h("section", { className: "theme-picker", "aria-label": "Language" },
    h("span", { className: "theme-title" }, t ? t("settings.language", "Language") : "Language"),
    h("div", { className: "language-grid" },
      languageOptions.map((language) =>
        h("button", {
          key: language.key,
          className: language.key === value ? "language-button active" : "language-button",
          onClick: () => onChange(language.key),
          type: "button"
        }, t ? t(`languages.${language.key}`, language.name) : language.name)
      )
    )
  );
}
