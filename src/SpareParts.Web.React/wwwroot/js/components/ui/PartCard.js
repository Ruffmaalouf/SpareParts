import { h } from "../../core/react-runtime.js";

// Visual card for a part: image region, content region, footer actions region
// as three distinct nested blocks.
export function PartCard({ part, badges, details, actions, onSelect, thumbnail }) {
  return h("article", { className: "part-card part-card-v2" },
    h("div", { className: "part-card-v2-media", "aria-hidden": "true", onClick: onSelect },
      thumbnail || h("span", { className: "part-card-v2-media-fallback" }, String(part?.name || "P").slice(0, 2).toUpperCase()),
      part?.usedCarId ? h("b", { className: "part-card-v2-donor-flag" }, "Donor") : null
    ),
    h("div", { className: "part-card-v2-content" },
      h("div", { className: "part-card-v2-heading" },
        h("div", null,
          h("span", { className: "part-code" }, part?.internalCode || "No code"),
          h("h3", null, part?.name)
        ),
        h("strong", { className: "part-card-v2-price" }, part?.priceText)
      ),
      Array.isArray(badges) && badges.length > 0 && h("div", { className: "badge-row" }, badges),
      Array.isArray(details) && details.length > 0 && h("dl", { className: "part-card-details" },
        details.map((detail, index) =>
          h("div", { key: detail.key || index },
            h("dt", null, detail.label),
            h("dd", null, detail.value)
          )
        )
      )
    ),
    Array.isArray(actions) && actions.length > 0 && h("div", { className: "part-card-v2-footer part-card-actions" }, actions)
  );
}
