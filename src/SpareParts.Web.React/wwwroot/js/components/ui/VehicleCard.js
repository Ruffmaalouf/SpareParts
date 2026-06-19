import { h } from "../../core/react-runtime.js";

// Card for used cars / donor vehicles: image region, identity+status region,
// actions region.
export function VehicleCard({ vehicle, title, subtitle, statusPill, details, actions, thumbnail, onSelect }) {
  return h("article", { className: "vehicle-card" },
    h("div", { className: "vehicle-card-media", "aria-hidden": "true", onClick: onSelect },
      thumbnail || h("span", { className: "vehicle-card-media-fallback" }, String(title || vehicle?.car || "V").slice(0, 2).toUpperCase())
    ),
    h("div", { className: "vehicle-card-identity" },
      h("div", { className: "vehicle-card-identity-top" },
        h("h3", null, title || vehicle?.car),
        statusPill || null
      ),
      subtitle && h("p", { className: "vehicle-card-subtitle" }, subtitle),
      Array.isArray(details) && details.length > 0 && h("dl", { className: "vehicle-card-details" },
        details.map((detail, index) =>
          h("div", { key: detail.key || index },
            h("dt", null, detail.label),
            h("dd", null, detail.value)
          )
        )
      )
    ),
    Array.isArray(actions) && actions.length > 0 && h("div", { className: "vehicle-card-actions" }, actions)
  );
}
