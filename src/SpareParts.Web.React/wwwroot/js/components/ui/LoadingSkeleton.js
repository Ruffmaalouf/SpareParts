import { h } from "../../core/react-runtime.js";

// Skeleton placeholder region. Variants render distinct nested shapes so
// screens can show a loading silhouette that matches their real layout.
export function LoadingSkeleton({ variant = "rows", count = 4 }) {
  const items = Array.from({ length: Math.max(1, count) }, (_, index) => index);

  if (variant === "cards") {
    return h("div", { className: "skeleton-v2 skeleton-v2-cards", "aria-hidden": "true" },
      items.map((index) =>
        h("div", { className: "skeleton-v2-card", key: index },
          h("span", { className: "skeleton-v2-block skeleton-v2-media" }),
          h("span", { className: "skeleton-v2-block skeleton-v2-line wide" }),
          h("span", { className: "skeleton-v2-block skeleton-v2-line narrow" })
        )
      )
    );
  }

  if (variant === "stats") {
    return h("div", { className: "skeleton-v2 skeleton-v2-stats", "aria-hidden": "true" },
      items.map((index) =>
        h("div", { className: "skeleton-v2-stat", key: index },
          h("span", { className: "skeleton-v2-block skeleton-v2-icon" }),
          h("span", { className: "skeleton-v2-block skeleton-v2-line" })
        )
      )
    );
  }

  return h("div", { className: "skeleton-v2 skeleton-v2-rows", "aria-hidden": "true" },
    items.map((index) =>
      h("div", { className: "skeleton-v2-row", key: index },
        h("span", { className: "skeleton-v2-block skeleton-v2-avatar" }),
        h("span", { className: "skeleton-v2-block skeleton-v2-line wide" }),
        h("span", { className: "skeleton-v2-block skeleton-v2-line narrow" })
      )
    )
  );
}
