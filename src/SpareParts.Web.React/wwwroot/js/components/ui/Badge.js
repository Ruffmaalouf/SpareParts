import { h } from "../../core/react-runtime.js";

// Backward-compatible with components/shared.js Badge({tone, children}),
// extended with an optional leading dot region and size variant.
export function Badge({ tone = "neutral", size = "md", dot = false, children }) {
  return h("span", { className: `badge badge-v2 badge-${tone} badge-v2-${size}` },
    dot && h("span", { className: "badge-v2-dot", "aria-hidden": "true" }),
    h("span", { className: "badge-v2-label" }, children)
  );
}
