import { h } from "../../core/react-runtime.js";

// Shop/seller profile card: avatar/name region, rating/verification badge
// region, contact actions region — three distinct nested blocks.
export function SellerCard({ name, subtitle, avatarText, ratingNode, verificationBadge, contactActions, details }) {
  return h("article", { className: "seller-card" },
    h("div", { className: "seller-card-identity" },
      h("span", { className: "seller-card-avatar" }, avatarText || String(name || "S").slice(0, 2).toUpperCase()),
      h("div", null,
        h("strong", null, name),
        subtitle && h("span", { className: "seller-card-subtitle" }, subtitle)
      )
    ),
    h("div", { className: "seller-card-rating-region" },
      ratingNode || null,
      verificationBadge || null
    ),
    Array.isArray(details) && details.length > 0 && h("dl", { className: "seller-card-details" },
      details.map((detail, index) =>
        h("div", { key: detail.key || index },
          h("dt", null, detail.label),
          h("dd", null, detail.value)
        )
      )
    ),
    Array.isArray(contactActions) && contactActions.length > 0 && h("div", { className: "seller-card-actions" }, contactActions)
  );
}
