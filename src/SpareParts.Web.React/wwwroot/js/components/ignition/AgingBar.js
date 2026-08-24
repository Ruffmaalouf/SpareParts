import { h } from "../../core/react-runtime.js";

const BUCKETS = [
  { key: "current", label: "Current", className: "is-current" },
  { key: "d1to30", label: "1-30d", className: "is-d30" },
  { key: "d31to60", label: "31-60d", className: "is-d60" },
  { key: "d61to90", label: "61-90d", className: "is-d90" },
  { key: "over90", label: "90+d", className: "is-over90" }
];

const DOT_COLORS = {
  current: "var(--ig-success)",
  d1to30: "var(--ig-info)",
  d31to60: "var(--ig-warning)",
  d61to90: "var(--ig-accent)",
  over90: "var(--ig-danger)"
};

// Ignition AgingBar (Report 03 §04) — segmented horizontal bar, width
// proportional to each bucket's share. Bucket math ported from
// customer-aging-view.js; the field mapping matches the existing
// /api/customers/aging row shape and the workspace `balance` block
// ({current, days1To30, days31To60, days61To90, over90Days}).
// Buckets beyond 60 days keep amber/danger tokens even at small
// proportions, and the legend always carries the amounts as text so the
// bar never relies on size or color alone (Report 03 §11).
export function AgingBar({ buckets = {}, total, onBucketClick, formatMoney, error, onRetry }) {
  if (error) {
    return h("div", { className: "ig-error-banner", role: "alert" },
      h("span", null, `Could not load aging data. ${error}`),
      typeof onRetry === "function" && h("button", { type: "button", className: "ig-btn", onClick: onRetry }, "Retry"));
  }

  const values = {
    current: Number(buckets.current || 0),
    d1to30: Number(buckets.d1to30 ?? buckets.days1To30 ?? 0),
    d31to60: Number(buckets.d31to60 ?? buckets.days31To60 ?? 0),
    d61to90: Number(buckets.d61to90 ?? buckets.days61To90 ?? 0),
    over90: Number(buckets.over90 ?? buckets.over90Days ?? 0)
  };
  const sum = Number(total) > 0
    ? Number(total)
    : Object.values(values).reduce((acc, value) => acc + Math.max(0, value), 0);
  const format = typeof formatMoney === "function"
    ? formatMoney
    : (value) => value.toLocaleString(undefined, { maximumFractionDigits: 2 });

  return h("div", { className: "ig-aging" },
    h("div", { className: "ig-aging-track", role: "img", "aria-label": `Aging: ${BUCKETS.map((bucket) => `${bucket.label} ${format(values[bucket.key])}`).join(", ")}` },
      BUCKETS.map((bucket) => {
        const value = Math.max(0, values[bucket.key]);
        if (value <= 0 || sum <= 0) return null;
        // Late buckets keep a visible minimum width — never rely on size alone.
        const minShare = bucket.key === "d61to90" || bucket.key === "over90" ? 4 : 1;
        const share = Math.max(minShare, (value / sum) * 100);
        return h("span", {
          key: bucket.key,
          className: `ig-aging-seg ${bucket.className}`,
          style: { width: `${share}%`, cursor: onBucketClick ? "pointer" : "default" },
          title: `${bucket.label}: ${format(value)}`,
          onClick: onBucketClick ? () => onBucketClick(bucket.key) : undefined
        });
      })),
    h("div", { className: "ig-aging-legend" },
      BUCKETS.map((bucket) =>
        h("button", {
          key: bucket.key,
          type: "button",
          style: { color: "inherit" },
          onClick: onBucketClick ? () => onBucketClick(bucket.key) : undefined
        },
          h("span", { className: "ig-dot", style: { background: DOT_COLORS[bucket.key] }, "aria-hidden": "true" }),
          `${bucket.label} `,
          h("span", { className: "ig-money" }, format(values[bucket.key]))
        )))
  );
}
