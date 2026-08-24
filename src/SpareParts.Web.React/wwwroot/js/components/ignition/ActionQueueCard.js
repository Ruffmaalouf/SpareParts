import { h } from "../../core/react-runtime.js";

const validTones = new Set(["danger", "warning", "success", "neutral"]);

// Ignition ActionQueueCard (Report 03 §03) — 1:1 replacement for the task
// shape computed by buildActionQueue() in dashboard-view.js
// ({tone,label,title,detail,value,onOpen}); danger/warning/neutral/success
// tones map directly to Ignition danger/amber/info/success tokens. Also
// carries the Report 05 §4 lifecycle affordances: `acted` dims the card and
// `onDismiss` renders the snooze/dismiss control when the caller's role may
// dismiss.
export function ActionQueueCard({ tone = "neutral", label, title, detail, value, onOpen, onDismiss, acted = false }) {
  const safeTone = validTones.has(tone) ? tone : "neutral";
  const pillTone = safeTone === "neutral" ? "info" : safeTone;

  return h("div", { className: `ig-queue-card is-${safeTone}${acted ? " is-acted" : ""}`, role: "listitem" },
    h("button", { type: "button", className: "ig-queue-body", onClick: onOpen },
      h("div", { className: "ig-cluster" },
        h("span", { className: `ig-pill ig-pill-${pillTone}` }, label),
        acted && h("span", { className: "ig-pill ig-pill-neutral" }, "Acted")
      ),
      h("div", { className: "ig-queue-title" }, title),
      detail && h("div", { className: "ig-queue-detail" }, detail)
    ),
    value != null && h("span", { className: "ig-queue-value" }, value),
    typeof onDismiss === "function" && h("button", {
      type: "button",
      className: "ig-queue-dismiss",
      title: "Dismiss for today",
      "aria-label": `Dismiss: ${title}`,
      onClick: onDismiss
    }, "✕")
  );
}
