import { h } from "../../core/react-runtime.js";

// Page title/breadcrumb region + a search slot + global action buttons slot.
export function Topbar({ breadcrumb, title, search, actions, children }) {
  return h("header", { className: "topbar-v2" },
    h("div", { className: "topbar-v2-identity" },
      breadcrumb && h("nav", { className: "topbar-v2-breadcrumb", "aria-label": "Breadcrumb" },
        (Array.isArray(breadcrumb) ? breadcrumb : [breadcrumb]).map((crumb, index, all) =>
          h("span", { key: index, className: "topbar-v2-breadcrumb-item" },
            crumb,
            index < all.length - 1 ? h("i", { "aria-hidden": "true" }, "/") : null
          )
        )
      ),
      title && h("strong", { className: "topbar-v2-title" }, title)
    ),
    search && h("div", { className: "topbar-v2-search-zone" }, search),
    h("div", { className: "topbar-v2-actions" },
      actions || null,
      children || null
    )
  );
}
