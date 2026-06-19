import { h, useEffect } from "../../core/react-runtime.js";

// Slide-in side panel: backdrop region, header region, scrollable body
// region, footer actions region. Distinct from Modal (edge-anchored vs
// centered) for detail/edit flows that want context of the page behind.
// Closes on backdrop click and Escape, same as Modal.
export function Drawer({ open, title, subtitle, onClose, children, footer, side = "right", size = "md" }) {
  useEffect(() => {
    if (!open || !onClose) return undefined;
    const handleKeyDown = (event) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [open, onClose]);

  if (!open) return null;

  return h("div", {
    className: "drawer-v2-backdrop",
    onClick: (event) => { if (event.target === event.currentTarget && onClose) onClose(); }
  },
    h("aside", { className: `drawer-v2 drawer-v2-${side} drawer-v2-${size}` },
      h("div", { className: "drawer-v2-header" },
        h("div", null,
          h("strong", null, title),
          subtitle && h("span", null, subtitle)
        ),
        onClose && h("button", { type: "button", className: "drawer-v2-close", onClick: onClose, "aria-label": "Close" }, "x")
      ),
      h("div", { className: "drawer-v2-body" }, children),
      footer && h("div", { className: "drawer-v2-footer" }, footer)
    )
  );
}
