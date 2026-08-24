import { h, useMemo } from "../../core/react-runtime.js";
import { Icon } from "../ui/CockpitIcons.js";

const kindIcon = {
  invoice: "doc",
  payment: "wallet",
  quote: "clipboard",
  message: "bell",
  note: "tag",
  vehicle: "car"
};

function dayLabel(timestamp) {
  const date = new Date(timestamp);
  if (Number.isNaN(date.getTime())) return "Undated";
  const today = new Date();
  const yesterday = new Date(today.getTime() - 86400000);
  const sameDay = (a, b) => a.toDateString() === b.toDateString();
  if (sameDay(date, today)) return "Today";
  if (sameDay(date, yesterday)) return "Yesterday";
  return date.toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" });
}

function timeLabel(timestamp) {
  const date = new Date(timestamp);
  if (Number.isNaN(date.getTime())) return "";
  return date.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
}

// Ignition ActivityTimeline (Report 03 §04) — merged event feed for a
// client (kind ∈ invoice/payment/quote/message/note/vehicle), sorted by
// timestamp desc with sticky day-divider labels when `groupByDay`.
// `onLoadMore` drives cursor pagination (25/page per the Report 04
// /timeline contract).
export function ActivityTimeline({ events = [], groupByDay = true, onLoadMore, loading, hasMore = false }) {
  const sorted = useMemo(
    () => [...events].sort((left, right) => new Date(right.timestamp) - new Date(left.timestamp)),
    [events]
  );

  if (loading && !sorted.length) {
    return h("div", { className: "ig-stack", "aria-hidden": "true" },
      [0, 1, 2, 3].map((index) => h("span", { key: index, className: "ig-skeleton", style: { height: "44px" } })));
  }

  if (!sorted.length) {
    return h("div", { className: "ig-palette-empty" }, "No activity recorded for this client yet.");
  }

  const blocks = [];
  let lastDay = null;
  sorted.forEach((event) => {
    const label = groupByDay ? dayLabel(event.timestamp) : null;
    if (groupByDay && label !== lastDay) {
      lastDay = label;
      blocks.push(h("div", { key: `day-${label}-${blocks.length}`, className: "ig-timeline-day ig-label" }, label));
    }
    blocks.push(
      h("div", { key: event.id || `event-${blocks.length}`, className: "ig-timeline-event" },
        h("span", { className: "ig-timeline-icon", "aria-hidden": "true" },
          h(Icon, { name: kindIcon[event.kind] || "doc", size: 13 })),
        h("div", { className: "ig-timeline-body" },
          h("div", { className: "ig-timeline-title" }, event.title),
          event.detail && h("div", { className: "ig-timeline-detail" }, event.detail),
          event.actor && h("div", { className: "ig-timeline-detail" }, event.actor)),
        h("span", { className: "ig-timeline-time" }, timeLabel(event.timestamp)))
    );
  });

  return h("div", { className: "ig-timeline", role: "feed", "aria-busy": loading ? "true" : "false" },
    blocks,
    hasMore && typeof onLoadMore === "function" && h("button", {
      type: "button",
      className: "ig-btn ig-cw-loadmore",
      onClick: onLoadMore,
      disabled: loading
    }, loading ? "Loading..." : "Load more")
  );
}
