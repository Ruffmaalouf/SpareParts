import { h } from "../core/react-runtime.js";

export function LockedFeatureModal({ lock, onClose, onUpgrade, t }) {
  if (!lock) return null;

  const isLimit = lock.errorCode === "PLAN_LIMIT_EXCEEDED";
  const title = isLimit
    ? t("billing.lockTitleLimit", "Plan limit reached")
    : t("billing.lockTitleFeature", "Feature not available on your plan");

  return h("div", { className: "modal-overlay", role: "dialog", "aria-modal": "true" },
    h("div", { className: "modal-v2 modal-v2-sm" },
      h("div", { className: "modal-v2-header" },
        h("h3", { className: "modal-v2-title" }, title),
        h("button", { className: "modal-v2-close", type: "button", onClick: onClose, "aria-label": t("common.close", "Close") }, "x")
      ),
      h("div", { className: "modal-v2-body" },
        h("p", null, lock.message),
        lock.currentPlan && h("p", { className: "status-line muted" },
          `${t("billing.currentPlan", "Current plan")}: ${lock.currentPlan}`),
        lock.requiredPlans && lock.requiredPlans.length > 0 && h("p", { className: "status-line muted" },
          `${t("billing.requiredPlans", "Available on")}: ${lock.requiredPlans.join(", ")}`)
      ),
      h("div", { className: "modal-v2-footer" },
        h("button", { className: "ghost-button", type: "button", onClick: onClose }, t("common.close", "Close")),
        h("button", { className: "primary-button", type: "button", onClick: onUpgrade }, t("billing.viewPlans", "View Plans"))
      )
    )
  );
}
