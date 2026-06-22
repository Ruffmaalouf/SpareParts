import { h } from "../../core/react-runtime.js";

// Cockpit icon set (stroke paths) used across the Control Center shell and
// dashboard. Keeps the premium consistent line weight from the approved mockup.
const paths = {
  grid: ["M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z"],
  cart: ["M2 3h2l2.6 12.4a2 2 0 0 0 2 1.6h8.8a2 2 0 0 0 2-1.6L21 7H6", "M9 20a1.4 1.4 0 1 0 0-2.8M18 20a1.4 1.4 0 1 0 0-2.8"],
  box: ["M21 8l-9-5-9 5 9 5 9-5z", "M3 8v9l9 5 9-5V8", "M12 13v9"],
  link: ["M9 17H7A5 5 0 0 1 7 7h2", "M15 7h2a5 5 0 0 1 0 10h-2", "M8 12h8"],
  car: ["M3 13l1.5-5A2 2 0 0 1 6.4 6.5h11.2A2 2 0 0 1 19.5 8L21 13", "M2 13h20v5H2zM7 18.5a1.6 1.6 0 1 0 0-.1M17 18.5a1.6 1.6 0 1 0 0-.1"],
  bank: ["M3 21h18", "M4 21V10l8-6 8 6v11", "M9 21v-7", "M15 21v-7"],
  users: ["M9 8a3.2 3.2 0 1 0 0-.1", "M3 20c0-3.4 2.7-6 6-6s6 2.6 6 6", "M17.5 9a2.4 2.4 0 1 0 0-.1", "M22 20c0-2.6-1.7-4.6-4-5.3"],
  cog: ["M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8z", "M19 12a7 7 0 0 0-.1-1.2l2-1.6-2-3.4-2.4 1a7 7 0 0 0-2-1.2L14 1h-4l-.5 2.6a7 7 0 0 0-2 1.2l-2.4-1-2 3.4 2 1.6A7 7 0 0 0 5 12a7 7 0 0 0 .1 1.2l-2 1.6 2 3.4 2.4-1a7 7 0 0 0 2 1.2L10 23h4l.5-2.6a7 7 0 0 0 2-1.2l2.4 1 2-3.4-2-1.6A7 7 0 0 0 19 12z"],
  dollar: ["M12 1v22", "M17 5.5C17 3.6 14.8 2 12 2S7 3.6 7 5.5 9.2 9 12 9s5 1.5 5 3.5-2.2 3.5-5 3.5-5-1.4-5-3.5"],
  trend: ["M3 17l6-6 4 4 8-9", "M15 6h6v6"],
  wallet: ["M2 6h20v14H2zM2 10h20", "M17 14a1.4 1.4 0 1 0 0-.1"],
  inbox: ["M22 12h-6l-2 3h-4l-2-3H2", "M5.5 5h13l3.5 7v7a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1v-7z"],
  pkg: ["M21 8l-9-5-9 5v8l9 5 9-5z", "M3 8l9 5 9-5", "M12 13v8"],
  warn: ["M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z", "M12 9v4", "M12 17h0"],
  search: ["M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14z", "M21 21l-4.4-4.4"],
  menu: ["M4 7h16", "M4 12h16", "M4 17h16"],
  plus: ["M12 5v14", "M5 12h14"],
  dup: ["M9 9h11v11H9z", "M5 15H4a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v1"],
  bell: ["M6 9a6 6 0 0 1 12 0v5l2 3H4l2-3z", "M9.5 20a2.5 2.5 0 0 0 5 0"],
  globe: ["M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18z", "M3 12h18M12 3a13 13 0 0 1 0 18 13 13 0 0 1 0-18z"],
  warehouse: ["M3 21V9l9-5 9 5v12", "M3 21h18", "M9 21v-6h6v6"],
  cal: ["M3 5h18v16H3zM3 10h18M8 2v4M16 2v4"],
  doc: ["M7 3h7l4 4v13a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1z", "M14 3v4h4"],
  clipboard: ["M6 3h12v18H6zM9 1.5h6v3H9z", "M9 9h6M9 13h6"],
  bookmark: ["M6 3h12v18l-6-4-6 4z"],
  bolt: ["M13 2 3 14h8l-2 8 12-12h-8l2-8z"],
  tag: ["M20.6 11.1 12.9 3.4a2 2 0 0 0-1.4-.6H4a1 1 0 0 0-1 1v7.5a2 2 0 0 0 .6 1.4l7.7 7.7a2 2 0 0 0 2.8 0l6.5-6.5a2 2 0 0 0 0-2.8z", "M8.5 8.5h0"],
  "arr-up": ["M12 19V5", "M6 11l6-6 6 6"],
  "arr-down": ["M12 5v14", "M6 13l6 6 6-6"],
  chevron: ["M6 9l6 6 6-6"],
  "chevron-r": ["M9 6l6 6-6 6"],
  power: ["M12 3v8", "M6.5 7a8 8 0 1 0 11 0"],
  disc: ["M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18z", "M12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6z", "M12 3v3M12 18v3M3 12h3M18 12h3"],
  piston: ["M7 3h10v9H7z", "M7 6h10M7 8.5h10", "M11 12v4l-1 5h4l-1-5v-4"],
  "return": ["M3 9h11a5 5 0 1 1 0 10H8", "M7 5 3 9l4 4"],
  star: ["M12 2l2.9 6.26 6.85.7-5.1 4.6 1.45 6.74L12 17.77 5.99 21.1l1.45-6.74-5.1-4.6 6.85-.7z"]
};

export function Icon({ name, size, className }) {
  const list = paths[name] || paths.grid;
  const props = { className: className || "ck-icn", viewBox: "0 0 24 24", "aria-hidden": "true" };
  if (size) { props.width = size; props.height = size; }
  return h("svg", props, list.map((d, index) => h("path", { key: index, d })));
}

export function hasIcon(name) {
  return Boolean(paths[name]);
}
