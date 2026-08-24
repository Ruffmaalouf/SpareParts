import { h } from "../../core/react-runtime.js";
import { initials } from "../../core/formatters.js";
import { Icon } from "../ui/CockpitIcons.js";
import { MoneyText } from "./MoneyText.js";

// Risk derivation ported from customer-aging-view.js agingClass() logic:
// danger if over90Days > 0, warning if any 1-90 bucket > 0, success otherwise.
export function deriveRiskTone(balance) {
  if (!balance) return "success";
  if (Number(balance.over90Days || 0) > 0) return "danger";
  const overdue = Number(balance.days1To30 || 0) + Number(balance.days31To60 || 0) + Number(balance.days61To90 || 0);
  return overdue > 0 ? "warning" : "success";
}

const riskLabel = { danger: "Overdue 90+", warning: "Overdue", success: "In good standing" };

// Ignition ClientHeader (Report 03 §04) — identity card + balance +
// risk pill + quick actions for the selected client.
// `client` = {name, phone, email, customerType, isActive, ...} from
// GET /api/clients/{id}/workspace; `balance` = {total, currency};
// `quickActions` = Array<{label, icon, onClick}> (New Sale, Send Reminder,
// New Quote, Call/WhatsApp).
export function ClientHeader({ client, balance, riskTone, quickActions = [], formatMoney, loading }) {
  if (loading) {
    return h("div", { className: "ig-client-header", "aria-hidden": "true" },
      h("span", { className: "ig-skeleton", style: { width: "44px", height: "44px", borderRadius: "999px" } }),
      h("span", { className: "ig-skeleton", style: { flex: 1, height: "40px" } }),
      h("span", { className: "ig-skeleton", style: { width: "140px", height: "40px" } }));
  }

  if (!client) return null;

  const tone = riskTone || deriveRiskTone(client.balance);
  const meta = [client.customerType, client.phone, client.email].filter(Boolean).join(" · ");

  return h("div", { className: "ig-client-header" },
    h("span", { className: "ig-client-avatar", style: { width: "44px", height: "44px", fontSize: "14px" }, "aria-hidden": "true" },
      initials(client.name)),
    h("div", { className: "ig-client-header-id" },
      h("div", { className: "ig-cluster" },
        h("h2", null, client.name),
        h("span", { className: `ig-pill ig-pill-${tone}` }, riskLabel[tone] || tone),
        client.isActive === false && h("span", { className: "ig-pill ig-pill-neutral" }, "Inactive")
      ),
      meta && h("div", { className: "ig-client-header-meta" }, meta)
    ),
    balance && h("div", { className: "ig-client-header-balance" },
      h("div", { className: "ig-label" }, "Balance"),
      h(MoneyText, { value: balance.total, currency: balance.currency, size: "xl", format: formatMoney })
    ),
    quickActions.length > 0 && h("div", { className: "ig-client-header-actions" },
      quickActions.map((action, index) =>
        h("button", {
          key: action.label,
          type: "button",
          className: index === 0 ? "ig-btn ig-btn-primary" : "ig-btn",
          onClick: action.onClick,
          disabled: action.disabled
        },
          action.icon && h(Icon, { name: action.icon, size: 14 }),
          action.label)))
  );
}
