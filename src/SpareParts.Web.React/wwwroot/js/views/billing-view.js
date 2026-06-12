import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, DataTable, PageHeader, StatusLine } from "../components/shared.js";

export const FEATURE_LABELS = {
  PART_LISTING: "Part Listings",
  PART_IMAGES: "Part Photos",
  BUYER_LEADS: "Buyer Leads",
  MOBILE_SELLER_APP: "Mobile Seller App",
  REPORTS_BASIC: "Reports",
  REPORTS_ADVANCED: "Advanced / Scheduled Reports",
  MULTI_BRANCH: "Multiple Branches / Warehouses",
  BULK_IMPORT: "Bulk Import (Excel)",
  AI_TITLE: "AI Title Generation",
  AI_DESCRIPTION: "AI Description & Notes",
  AI_PRICE_SUGGESTION: "AI Price Suggestions",
  PROMOTED_LISTINGS: "Promoted Listings",
  FEATURED_PARTS: "Featured Parts",
  API_ACCESS: "API Access",
  PRIORITY_SUPPORT: "Priority Support",
  CUSTOM_INTEGRATIONS: "Custom Integrations",
  WHITE_LABEL: "White-label Branding"
};

export const LIMIT_LABELS = {
  USERS_COUNT: "User accounts",
  BRANCHES_COUNT: "Warehouses / branches",
  ACTIVE_PARTS_COUNT: "Active part listings",
  IMAGES_PER_PART: "Photos per part",
  BUYER_LEADS_MONTHLY: "Buyer leads / month",
  PROMOTED_PARTS_MONTHLY: "Promoted listings / month",
  FEATURED_PARTS_MONTHLY: "Featured parts / month",
  API_CALLS_MONTHLY: "API calls / month"
};

const STATUS_TONES = {
  Active: "success",
  Trialing: "accent",
  PastDue: "warning",
  Cancelled: "muted",
  Expired: "danger"
};

function formatLimit(value) {
  return value === -1 ? "Unlimited" : Number(value || 0).toLocaleString();
}

function planActionLabel(action, t) {
  switch (action) {
    case "current": return t("billing.currentPlan", "Current plan");
    case "contact": return t("billing.contactSales", "Contact sales");
    case "upgrade": return t("billing.upgrade", "Upgrade");
    case "downgrade": return t("billing.switchPlan", "Switch plan");
    default: return t("billing.switchPlan", "Switch plan");
  }
}

export function BillingView({ api, t = (key, fallback) => fallback }) {
  const [subscription, setSubscription] = useState(null);
  const [packages, setPackages] = useState([]);
  const [payments, setPayments] = useState([]);
  const [invoices, setInvoices] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [billingCycle, setBillingCycle] = useState("Monthly");
  const [busyCode, setBusyCode] = useState(null);
  const [checkoutInfo, setCheckoutInfo] = useState(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const [subscriptionResult, packagesResult, paymentsResult, invoicesResult] = await Promise.all([
        api.get("/api/subscription/current"),
        api.get("/api/pricing/packages"),
        api.get("/api/payments/history"),
        api.get("/api/invoices")
      ]);
      setSubscription(subscriptionResult);
      setPackages(Array.isArray(packagesResult) ? packagesResult : []);
      setPayments(Array.isArray(paymentsResult) ? paymentsResult : []);
      setInvoices(Array.isArray(invoicesResult) ? invoicesResult : []);
      if (subscriptionResult?.billingCycle) {
        setBillingCycle(subscriptionResult.billingCycle);
      }
    } catch (e) {
      setStatus(e.message || t("billing.loadError", "Failed to load billing information."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => { load(); }, [load]);

  const currentPackage = useMemo(
    () => packages.find((pkg) => pkg.code === subscription?.packageCode) || null,
    [packages, subscription]
  );

  const runAction = useCallback(async (code, action) => {
    setBusyCode(code);
    setStatus("");
    setCheckoutInfo(null);
    try {
      await action();
    } catch (e) {
      setStatus(e.message || t("billing.actionError", "The request could not be completed."));
    } finally {
      setBusyCode(null);
    }
  }, [t]);

  const handleUpgrade = useCallback((packageCode) => runAction(packageCode, async () => {
    const response = await api.post("/api/subscription/checkout", { packageCode, billingCycle });
    if (response.status === "redirect" && response.redirectUrl) {
      window.open(response.redirectUrl, "_blank", "noopener");
      setStatus(t("billing.redirecting", "Complete your payment in the new tab, then refresh this page."));
    } else if (response.status === "manual") {
      setCheckoutInfo(response);
      setStatus(t("billing.manualInstructions", "Follow the manual payment instructions below."));
    } else {
      setStatus(t("billing.upgraded", "Your plan has been upgraded."));
    }
    await load();
  }), [api, billingCycle, load, runAction, t]);

  const handleSwitchPlan = useCallback((packageCode) => runAction(packageCode, async () => {
    await api.post("/api/subscription/change-plan", { packageCode, billingCycle });
    setStatus(t("billing.planChanged", "Your plan has been updated."));
    await load();
  }), [api, billingCycle, load, runAction, t]);

  const handleStartTrial = useCallback((packageCode) => runAction(`trial-${packageCode}`, async () => {
    await api.post("/api/subscription/start-trial", { packageCode, trialDays: 14 });
    setStatus(t("billing.trialStarted", "Your 14-day trial has started."));
    await load();
  }), [api, load, runAction, t]);

  const handleCancel = useCallback((immediate) => {
    const confirmMessage = immediate
      ? t("billing.confirmCancelImmediate", "Cancel now and downgrade to the Free plan immediately?")
      : t("billing.confirmCancelPeriodEnd", "Cancel at the end of the current billing period?");
    if (!window.confirm(confirmMessage)) return;

    return runAction("cancel", async () => {
      await api.post("/api/subscription/cancel", { immediate });
      setStatus(t("billing.cancelSaved", "Your cancellation has been saved."));
      await load();
    });
  }, [api, load, runAction, t]);

  const planAction = useCallback((pkg) => {
    if (!subscription) return "current";
    if (pkg.code === subscription.packageCode) return "current";
    if (pkg.isCustomPricing) return "contact";
    if (pkg.code === "FREE") return "downgrade";
    if (currentPackage && pkg.sortOrder > currentPackage.sortOrder) return "upgrade";
    return "downgrade";
  }, [currentPackage, subscription]);

  const canStartTrial = subscription?.packageCode === "FREE";

  return h("section", { className: "screen billing-screen" },
    h(PageHeader, {
      kicker: t("billing.kicker", "Subscription"),
      title: t("billing.title", "Billing & Subscription"),
      subtitle: t("billing.subtitle", "Manage your plan, payment methods, invoices, and usage limits."),
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: load,
        disabled: isLoading
      }, isLoading ? t("common.loading", "Loading") : t("common.refresh", "Refresh"))
    }),
    h(StatusLine, { status }),

    subscription && h("section", { className: "panel" },
      h("div", { className: "admin-panel-header" },
        h("h3", null, subscription.packageName),
        h(Badge, { tone: STATUS_TONES[subscription.status] || "muted" }, subscription.status)
      ),
      h("div", { className: "detail-grid" },
        h("div", { className: "detail-tile" }, h("span", null, t("billing.billingCycle", "Billing cycle")), h("strong", null, subscription.billingCycle)),
        h("div", { className: "detail-tile" }, h("span", null, t("billing.periodStart", "Current period start")), h("strong", null, shortDate(subscription.currentPeriodStart))),
        h("div", { className: "detail-tile" }, h("span", null, t("billing.periodEnd", "Current period end")), h("strong", null, shortDate(subscription.currentPeriodEnd))),
        subscription.trialEndsAt && h("div", { className: "detail-tile" }, h("span", null, t("billing.trialEnds", "Trial ends")), h("strong", null, shortDate(subscription.trialEndsAt))),
        subscription.cancelAtPeriodEnd && h("div", { className: "detail-tile" }, h("span", null, t("billing.cancellation", "Cancellation")), h("strong", null, t("billing.cancelAtPeriodEnd", "Cancels at period end")))
      ),
      subscription.packageCode !== "FREE" && h("div", { className: "row-actions" },
        !subscription.cancelAtPeriodEnd && h("button", {
          className: "ghost-button",
          type: "button",
          disabled: busyCode === "cancel",
          onClick: () => handleCancel(false)
        }, t("billing.cancelAtEnd", "Cancel at period end")),
        h("button", {
          className: "ghost-button",
          type: "button",
          disabled: busyCode === "cancel",
          onClick: () => handleCancel(true)
        }, t("billing.cancelImmediately", "Cancel & downgrade now"))
      )
    ),

    subscription && h("section", { className: "panel" },
      h("h3", null, t("billing.usageLimits", "Usage & Limits")),
      h("div", { className: "detail-grid" },
        subscription.limits.map((limit) =>
          h("div", { className: "detail-tile", key: limit.limitCode },
            h("span", null, LIMIT_LABELS[limit.limitCode] || limit.limitCode),
            h("strong", null, formatLimit(limit.limitValue))
          )
        ),
        subscription.usage.map((counter) =>
          h("div", { className: "detail-tile", key: `usage-${counter.counterCode}` },
            h("span", null, LIMIT_LABELS[counter.counterCode] || counter.counterCode),
            h("strong", null, `${Number(counter.currentValue || 0).toLocaleString()} / ${formatLimit(counter.limitValue)}`)
          )
        )
      )
    ),

    subscription && h("section", { className: "panel" },
      h("h3", null, t("billing.features", "Plan Features")),
      h("div", { className: "detail-grid" },
        subscription.features.map((feature) =>
          h("div", { className: "detail-tile", key: feature.featureCode },
            h("span", null, FEATURE_LABELS[feature.featureCode] || feature.featureCode),
            h(Badge, { tone: feature.isEnabled ? "success" : "muted" }, feature.isEnabled
              ? t("billing.included", "Included")
              : t("billing.locked", "Locked"))
          )
        )
      )
    ),

    checkoutInfo && checkoutInfo.status === "manual" && h("section", { className: "panel" },
      h("h3", null, t("billing.manualPayment", "Manual Payment Instructions")),
      h("p", null, checkoutInfo.instructions || t("billing.manualPaymentDefault", "Please complete the payment as instructed and an administrator will activate your plan once it is confirmed.")),
      h("p", { className: "status-line muted" }, `${t("billing.paymentReference", "Payment reference")}: #${checkoutInfo.paymentId}`)
    ),

    h("section", { className: "panel" },
      h("div", { className: "admin-panel-header" },
        h("h3", null, t("billing.availablePlans", "Available Plans")),
        h("div", { className: "row-actions" },
          h("button", {
            className: billingCycle === "Monthly" ? "segment active" : "segment",
            type: "button",
            onClick: () => setBillingCycle("Monthly")
          }, t("billing.monthly", "Monthly")),
          h("button", {
            className: billingCycle === "Yearly" ? "segment active" : "segment",
            type: "button",
            onClick: () => setBillingCycle("Yearly")
          }, t("billing.yearly", "Yearly"))
        )
      ),
      h("div", { className: "detail-grid" },
        packages.map((pkg) => {
          const action = planAction(pkg);
          const price = billingCycle === "Yearly" ? pkg.yearlyPrice : pkg.monthlyPrice;
          const priceLabel = pkg.isCustomPricing
            ? t("billing.customPricing", "Custom pricing")
            : `${money(price, pkg.currency)} / ${billingCycle === "Yearly" ? t("billing.year", "year") : t("billing.month", "month")}`;
          const isBusy = busyCode === pkg.code || busyCode === `trial-${pkg.code}`;

          return h("div", { className: "detail-tile", key: pkg.code },
            h("div", { className: "admin-panel-header" },
              h("strong", null, pkg.name),
              action === "current" && h(Badge, { tone: "accent" }, t("billing.currentPlan", "Current"))
            ),
            h("p", null, pkg.description),
            h("strong", null, priceLabel),
            h("div", { className: "row-actions" },
              action === "current" && h("button", { className: "ghost-button", type: "button", disabled: true }, t("billing.currentPlan", "Current plan")),
              action === "contact" && h("a", { className: "secondary-button", href: "mailto:sales@spareparts.app" }, t("billing.contactSales", "Contact sales")),
              action === "upgrade" && h("button", {
                className: "primary-button",
                type: "button",
                disabled: isBusy,
                onClick: () => handleUpgrade(pkg.code)
              }, planActionLabel(action, t)),
              action === "downgrade" && h("button", {
                className: "secondary-button",
                type: "button",
                disabled: isBusy,
                onClick: () => handleSwitchPlan(pkg.code)
              }, planActionLabel(action, t)),
              action === "upgrade" && canStartTrial && h("button", {
                className: "ghost-button",
                type: "button",
                disabled: isBusy,
                onClick: () => handleStartTrial(pkg.code)
              }, t("billing.startTrial", "Start 14-day trial"))
            )
          );
        })
      )
    ),

    h("section", { className: "panel" },
      h("h3", null, t("billing.paymentHistory", "Payment History")),
      h(DataTable, {
        columns: [
          { key: "createdAt", label: t("billing.date", "Date"), render: (row) => shortDate(row.createdAt) },
          { key: "packageCode", label: t("billing.plan", "Plan"), render: (row) => row.packageCode },
          { key: "billingCycle", label: t("billing.billingCycle", "Billing cycle"), render: (row) => row.billingCycle },
          { key: "amount", label: t("billing.amount", "Amount"), render: (row) => money(row.amount, row.currency) },
          { key: "providerCode", label: t("billing.provider", "Provider"), render: (row) => row.providerCode },
          { key: "status", label: t("common.status", "Status"), render: (row) => h(Badge, { tone: row.status === "Succeeded" ? "success" : row.status === "Failed" ? "danger" : "warning" }, row.status) }
        ],
        rows: payments,
        getRowKey: (row) => row.id,
        emptyText: t("billing.noPayments", "No payments yet.")
      })
    ),

    h("section", { className: "panel" },
      h("h3", null, t("billing.invoices", "Invoices")),
      h(DataTable, {
        columns: [
          { key: "invoiceNumber", label: t("billing.invoiceNumber", "Invoice #"), render: (row) => row.invoiceNumber },
          { key: "issuedAt", label: t("billing.issued", "Issued"), render: (row) => shortDate(row.issuedAt) },
          { key: "totalAmount", label: t("billing.total", "Total"), render: (row) => money(row.totalAmount, row.currency) },
          { key: "status", label: t("common.status", "Status"), render: (row) => h(Badge, { tone: row.status === "Paid" ? "success" : row.status === "Void" ? "muted" : "warning" }, row.status) },
          { key: "paidAt", label: t("billing.paidAt", "Paid"), render: (row) => row.paidAt ? shortDate(row.paidAt) : "-" }
        ],
        rows: invoices,
        getRowKey: (row) => row.id,
        emptyText: t("billing.noInvoices", "No invoices yet.")
      })
    )
  );
}
