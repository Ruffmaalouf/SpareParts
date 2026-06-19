import { h, useEffect } from "../../core/react-runtime.js";

// Centered modal: backdrop region, header region (title + close), body
// region, footer actions region. Closes on backdrop click and Escape.
export function Modal({ open, title, onClose, children, footer, size = "md" }) {
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
    className: "modal-v2-backdrop",
    onClick: (event) => { if (event.target === event.currentTarget && onClose) onClose(); }
  },
    h("div", { className: `modal-v2 modal-v2-${size}`, role: "dialog", "aria-modal": "true" },
      h("div", { className: "modal-v2-header" },
        h("strong", null, title),
        onClose && h("button", { type: "button", className: "modal-v2-close", onClick: onClose, "aria-label": "Close" }, "x")
      ),
      h("div", { className: "modal-v2-body" }, children),
      footer && h("div", { className: "modal-v2-footer" }, footer)
    )
  );
}
