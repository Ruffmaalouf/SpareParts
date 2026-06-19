import { h } from "../../core/react-runtime.js";

// Maps domain status strings (availability/condition/verification states)
// to a consistent {tone, icon} pairing.
const STATUS_MAP = {
  available: { tone: "success", icon: "M5 13l4 4L19 7" },
  reserved: { tone: "warning", icon: "M12 7v5l3 3" },
  sold: { tone: "neutral", icon: "M5 12h14" },
  damaged: { tone: "danger", icon: "M12 9v4m0 4h.01M10.3 3.9 2.7 18a1 1 0 0 0 .9 1.5h16.8a1 1 0 0 0 .9-1.5L13.7 3.9a1 1 0 0 0-1.7 0Z" },
  verified: { tone: "info", icon: "M9 12l2 2 4-4m4 2a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" },
  pending: { tone: "warning", icon: "M12 7v5l3 3" },
  open: { tone: "info", icon: "M12 7v5l3 3" },
  active: { tone: "info", icon: "M5 13l4 4L19 7" },
  contacted: { tone: "info", icon: "M12 7v5l3 3" },
  quoted: { tone: "warning", icon: "M12 7v5l3 3" },
  paid: { tone: "success", icon: "M5 13l4 4L19 7" },
  completed: { tone: "success", icon: "M5 13l4 4L19 7" },
  closed: { tone: "success", icon: "M5 13l4 4L19 7" },
  fulfilled: { tone: "success", icon: "M5 13l4 4L19 7" },
  cancelled: { tone: "danger", icon: "M6 6l12 12M18 6 6 18" },
  failed: { tone: "danger", icon: "M6 6l12 12M18 6 6 18" },
  overdue: { tone: "danger", icon: "M12 9v4m0 4h.01" },
  expired: { tone: "danger", icon: "M12 9v4m0 4h.01" },
  draft: { tone: "neutral", icon: "M12 7v5l3 3" },
  unverified: { tone: "neutral", icon: "M12 9v4m0 4h.01" }
};

function normalizeKey(status) {
  return String(status || "").trim().toLowerCase();
}

// Status pill for availability/condition/verification states with
// consistent color-coding + icon. Falls back gracefully for unknown values.
export function StatusPill({ status, tone, label }) {
  const key = normalizeKey(status);
  const mapping = STATUS_MAP[key] || { tone: "neutral", icon: "M12 8v4m0 4h.01" };
  const resolvedTone = tone || mapping.tone;

  return h("span", { className: `status-pill status-pill-${resolvedTone}` },
    h("svg", { className: "status-pill-icon", viewBox: "0 0 24 24", "aria-hidden": "true" },
      h("path", { d: mapping.icon })
    ),
    h("span", { className: "status-pill-label" }, label || status || "Unknown")
  );
}
