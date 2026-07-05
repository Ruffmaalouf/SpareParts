import { h, useEffect } from "../../core/react-runtime.js";
import { Icon } from "../ui/CockpitIcons.js";

// Ignition CommandBar (Report 03 §03) — the collapsed trigger for the
// command palette: icon + "Search or Ctrl+K" placeholder. Registers the
// global Ctrl/Cmd+K shortcut while mounted (Ignition screens only —
// legacy screens are untouched). The open palette itself is the
// CommandPalette overlay.
export function CommandBar({ onOpen, placeholder = "Search or run a command…" }) {
  useEffect(() => {
    const handleKeyDown = (event) => {
      if ((event.ctrlKey || event.metaKey) && String(event.key).toLowerCase() === "k") {
        event.preventDefault();
        if (typeof onOpen === "function") onOpen();
      }
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [onOpen]);

  return h("button", { type: "button", className: "ig-cmdbar", onClick: onOpen, "aria-haspopup": "dialog" },
    h(Icon, { name: "search", size: 15 }),
    h("span", null, placeholder),
    h("kbd", null, "Ctrl+K")
  );
}
