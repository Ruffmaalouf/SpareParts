import { h } from "../../core/react-runtime.js";

// Search input with leading icon region, trailing clear button region,
// and an optional scope/segment row beneath for narrowing results.
export function SearchBar({ value, onChange, placeholder, scopes, activeScope, onScope, autoFocus }) {
  return h("div", { className: "search-bar-v2" },
    h("div", { className: "search-bar-v2-field" },
      h("svg", { className: "search-bar-v2-icon", viewBox: "0 0 24 24", "aria-hidden": "true" },
        h("circle", { cx: "11", cy: "11", r: "7" }),
        h("path", { d: "M21 21l-4.3-4.3" })
      ),
      h("input", {
        type: "search",
        value: value || "",
        placeholder: placeholder || "Search...",
        autoFocus: Boolean(autoFocus),
        onChange: (event) => onChange && onChange(event.target.value),
        "aria-label": placeholder || "Search"
      }),
      value && h("button", {
        type: "button",
        className: "search-bar-v2-clear",
        onClick: () => onChange && onChange(""),
        "aria-label": "Clear search"
      }, "x")
    ),
    Array.isArray(scopes) && scopes.length > 0 && h("div", { className: "search-bar-v2-scopes" },
      scopes.map((scope) =>
        h("button", {
          key: scope.key,
          type: "button",
          className: scope.key === activeScope ? "search-bar-v2-scope active" : "search-bar-v2-scope",
          onClick: () => onScope && onScope(scope.key)
        }, scope.label)
      )
    )
  );
}
