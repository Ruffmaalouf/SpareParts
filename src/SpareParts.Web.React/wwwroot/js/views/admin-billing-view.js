import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { money, shortDate, dateTime } from "../core/formatters.js";
import { Badge, DataTable, PageHeader, StatusLine } from "../components/shared.js";
import { FEATURE_LABELS, LIMIT_LABELS } from "./billing-view.js";

const FEATURE_CODES = Object.keys(FEATURE_LABELS);
const LIMIT_CODES = Object.keys(LIMIT_LABELS);

const TABS = [
  { key: "packages", label: "Packages" },
  { key: "subscriptions", label: "Subscriptions" },
  { key: "payments", label: "Payments" },
  { key: "invoices", label: "Invoices" },
  { key: "webhooks", label: "Webhook Events" }
];

const SUBSCRIPTION_STATUS_TONES = {
  Active: "success",
  Trialing: "accent",
  PastDue: "warning",
  Cancelled: "muted",
  Expired: "danger"
};

const PAYMENT_STATUS_TONES = {
  Succeeded: "success",
  Failed: "danger",
  Refunded: "muted",
  Pending: "warning"
};

const INVOICE_STATUS_TONES = {
  Paid: "success",
  Void: "muted",
  Draft: "warning",
  Issued: "warning"
};

const WEBHOOK_STATUS_TONES = {
  Processed: "success",
  Failed: "danger",
  Ignored: "muted",
  Pending: "warning"
};

function emptyPackageForm() {
  return {
    id: null,
    code: "",
    name: "",
    description: "",
    monthlyPrice: "0",
    yearlyPrice: "0",
    currency: "USD",
    sortOrder: "0",
    isActive: true,
    isCustomPricing: false,
    features: Object.fromEntries(FEATURE_CODES.map((code) => [code, false])),
    limits: Object.fromEntries(LIMIT_CODES.map((code) => [code, "0"]))
  };
}

function packageToForm(pkg) {
  const features = Object.fromEntries(FEATURE_CODES.map((code) => {
    const match = pkg.features.find((feature) => feature.featureCode === code);
    return [code, Boolean(match?.isEnabled)];
  }));
  const limits = Object.fromEntries(LIMIT_CODES.map((code) => {
    const match = pkg.limits.find((limit) => limit.limitCode === code);
    return [code, String(match ? match.limitValue : 0)];
  }));

  return {
    id: pkg.id,
    code: pkg.code,
    name: pkg.name,
    description: pkg.description || "",
    monthlyPrice: String(pkg.monthlyPrice),
    yearlyPrice: String(pkg.yearlyPrice),
    currency: pkg.currency,
    sortOrder: String(pkg.sortOrder),
    isActive: pkg.isActive,
    isCustomPricing: pkg.isCustomPricing,
    features,
    limits
  };
}

function formToRequest(form) {
  return {
    code: form.code.trim().toUpperCase(),
    name: form.name.trim(),
    description: form.description.trim(),
    monthlyPrice: Number(form.monthlyPrice) || 0,
    yearlyPrice: Number(form.yearlyPrice) || 0,
    currency: form.currency.trim().toUpperCase() || "USD",
    sortOrder: Number(form.sortOrder) || 0,
    isActive: Boolean(form.isActive),
    isCustomPricing: Boolean(form.isCustomPricing),
    features: FEATURE_CODES.map((code) => ({ featureCode: code, isEnabled: Boolean(form.features[code]) })),
    limits: LIMIT_CODES.map((code) => ({ limitCode: code, limitValue: Number(form.limits[code]) }))
  };
}

function PackageEditorModal({ form, onChange, onClose, onSave, isSaving, t }) {
  if (!form) return null;

  const setField = (key, value) => onChange({ ...form, [key]: value });
  const setFeature = (code, value) => onChange({ ...form, features: { ...form.features, [code]: value } });
  const setLimit = (code, value) => onChange({ ...form, limits: { ...form.limits, [code]: value } });

  return h("div", { className: "modal-overlay", role: "dialog", "aria-modal": "true" },
    h("div", { className: "modal-v2 modal-v2-lg" },
      h("div", { className: "modal-v2-header" },
        h("h3", { className: "modal-v2-title" }, form.id
          ? t("adminBilling.editPackage", "Edit Package")
          : t("adminBilling.newPackage", "New Package")),
        h("button", { className: "modal-v2-close", type: "button", onClick: onClose, "aria-label": t("common.close", "Close") }, "x")
      ),
      h("div", { className: "modal-v2-body" },
        h("div", { className: "form-grid" },
          h("label", null, t("adminBilling.code", "Code"),
            h("input", { value: form.code, onChange: (e) => setField("code", e.target.value), placeholder: "BASIC" })),
          h("label", null, t("adminBilling.name", "Name"),
            h("input", { value: form.name, onChange: (e) => setField("name", e.target.value) })),
          h("label", null, t("adminBilling.monthlyPrice", "Monthly price"),
            h("input", { type: "number", step: "0.01", value: form.monthlyPrice, onChange: (e) => setField("monthlyPrice", e.target.value) })),
          h("label", null, t("adminBilling.yearlyPrice", "Yearly price"),
            h("input", { type: "number", step: "0.01", value: form.yearlyPrice, onChange: (e) => setField("yearlyPrice", e.target.value) })),
          h("label", null, t("adminBilling.currency", "Currency"),
            h("input", { value: form.currency, onChange: (e) => setField("currency", e.target.value) })),
          h("label", null, t("adminBilling.sortOrder", "Sort order"),
            h("input", { type: "number", value: form.sortOrder, onChange: (e) => setField("sortOrder", e.target.value) }))
        ),
        h("label", null, t("adminBilling.description", "Description"),
          h("textarea", { value: form.description, onChange: (e) => setField("description", e.target.value), rows: 2 })),
        h("div", { className: "row-actions" },
          h("label", null,
            h("input", { type: "checkbox", checked: form.isActive, onChange: (e) => setField("isActive", e.target.checked) }),
            ` ${t("adminBilling.isActive", "Active")}`),
          h("label", null,
            h("input", { type: "checkbox", checked: form.isCustomPricing, onChange: (e) => setField("isCustomPricing", e.target.checked) }),
            ` ${t("adminBilling.isCustomPricing", "Custom pricing (contact sales)")}`)
        ),
        h("h4", null, t("adminBilling.features", "Features")),
        h("div", { className: "form-grid" },
          FEATURE_CODES.map((code) =>
            h("label", { key: code },
              h("input", {
                type: "checkbox",
                checked: Boolean(form.features[code]),
                onChange: (e) => setFeature(code, e.target.checked)
              }),
              ` ${FEATURE_LABELS[code]}`)
          )
        ),
        h("h4", null, t("adminBilling.limits", "Limits")),
        h("p", { className: "status-line muted" }, t("adminBilling.limitsHint", "Use -1 for unlimited.")),
        h("div", { className: "form-grid" },
          LIMIT_CODES.map((code) =>
            h("label", { key: code }, LIMIT_LABELS[code],
              h("input", {
                type: "number",
                value: form.limits[code],
                onChange: (e) => setLimit(code, e.target.value)
              }))
          )
        )
      ),
      h("div", { className: "modal-v2-footer" },
        h("button", { className: "ghost-button", type: "button", onClick: onClose }, t("common.cancel", "Cancel")),
        h("button", { className: "primary-button", type: "button", disabled: isSaving, onClick: onSave }, t("common.save", "Save"))
      )
    )
  );
}

export function AdminBillingView({ api, t = (key, fallback) => fallback }) {
  const [activeTab, setActiveTab] = useState("packages");
  const [packages, setPackages] = useState([]);
  const [subscriptions, setSubscriptions] = useState([]);
  const [payments, setPayments] = useState([]);
  const [invoices, setInvoices] = useState([]);
  const [webhooks, setWebhooks] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [busyKey, setBusyKey] = useState(null);
  const [packageForm, setPackageForm] = useState(null);
  const [activateForm, setActivateForm] = useState({ tenantId: "", packageCode: "", billingCycle: "Monthly", paymentId: "", durationDays: "" });

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const [packagesResult, subscriptionsResult, paymentsResult, invoicesResult, webhooksResult] = await Promise.all([
        api.get("/api/admin/pricing/packages"),
        api.get("/api/admin/subscriptions"),
        api.get("/api/admin/payments"),
        api.get("/api/admin/invoices"),
        api.get("/api/admin/webhook-events")
      ]);
      setPackages(Array.isArray(packagesResult) ? packagesResult : []);
      setSubscriptions(Array.isArray(subscriptionsResult) ? subscriptionsResult : []);
      setPayments(Array.isArray(paymentsResult) ? paymentsResult : []);
      setInvoices(Array.isArray(invoicesResult) ? invoicesResult : []);
      setWebhooks(Array.isArray(webhooksResult) ? webhooksResult : []);
    } catch (e) {
      setStatus(e.message || t("adminBilling.loadError", "Failed to load billing administration data."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => { load(); }, [load]);

  const runTask = useCallback(async (key, action, successMessage) => {
    setBusyKey(key);
    setStatus("");
    try {
      await action();
      if (successMessage) setStatus(successMessage);
      await load();
    } catch (e) {
      setStatus(e.message || t("billing.actionError", "The request could not be completed."));
    } finally {
      setBusyKey(null);
    }
  }, [load, t]);

  const openNewPackage = useCallback(() => setPackageForm(emptyPackageForm()), []);
  const openEditPackage = useCallback((pkg) => setPackageForm(packageToForm(pkg)), []);
  const closePackageForm = useCallback(() => setPackageForm(null), []);

  const savePackage = useCallback(() => runTask("package-save", async () => {
    const request = formToRequest(packageForm);
    if (packageForm.id) {
      await api.put(`/api/admin/pricing/packages/${packageForm.id}`, request);
    } else {
      await api.post("/api/admin/pricing/packages", request);
    }
    setPackageForm(null);
  }, t("adminBilling.packageSaved", "Package saved.")), [api, packageForm, runTask, t]);

  const togglePackageActive = useCallback((pkg) => runTask(`package-active-${pkg.id}`, async () => {
    await api.put(`/api/admin/pricing/packages/${pkg.id}/active`, { isActive: !pkg.isActive });
  }), [api, runTask]);

  const activateSubscription = useCallback(() => runTask("activate-subscription", async () => {
    const tenantId = Number(activateForm.tenantId);
    if (!tenantId || !activateForm.packageCode) {
      throw new Error(t("adminBilling.activateValidation", "Tenant ID and package are required."));
    }
    await api.post("/api/admin/subscriptions/activate", {
      tenantId,
      packageCode: activateForm.packageCode,
      billingCycle: activateForm.billingCycle,
      paymentId: activateForm.paymentId ? Number(activateForm.paymentId) : null,
      durationDays: activateForm.durationDays ? Number(activateForm.durationDays) : null
    });
    setActivateForm({ tenantId: "", packageCode: "", billingCycle: "Monthly", paymentId: "", durationDays: "" });
  }, t("adminBilling.subscriptionActivated", "Subscription activated.")), [activateForm, api, runTask, t]);

  const runMaintenance = useCallback(() => runTask("run-maintenance", async () => {
    const updated = await api.post("/api/admin/subscriptions/run-maintenance", {});
    setStatus(t("adminBilling.maintenanceDone", "Maintenance complete. {count} subscriptions updated.", { count: updated }));
  }), [api, runTask, t]);

  const markPaid = useCallback((payment) => runTask(`mark-paid-${payment.id}`, async () => {
    await api.post(`/api/admin/payments/${payment.id}/mark-paid`, {});
  }, t("adminBilling.paymentMarkedPaid", "Payment marked as paid.")), [api, runTask, t]);

  const packageCodeOptions = useMemo(() => packages.map((pkg) => pkg.code), [packages]);

  return h("section", { className: "screen admin-billing-screen" },
    h(PageHeader, {
      kicker: t("adminBilling.kicker", "Platform Administration"),
      title: t("adminBilling.title", "Pricing & Subscriptions"),
      subtitle: t("adminBilling.subtitle", "Manage pricing packages, tenant subscriptions, payments, invoices, and webhook events."),
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: load,
        disabled: isLoading
      }, isLoading ? t("common.loading", "Loading") : t("common.refresh", "Refresh"))
    }),
    h(StatusLine, { status }),

    h("div", { className: "row-actions" },
      TABS.map((tab) =>
        h("button", {
          key: tab.key,
          type: "button",
          className: tab.key === activeTab ? "segment active" : "segment",
          onClick: () => setActiveTab(tab.key)
        }, t(`adminBilling.tab.${tab.key}`, tab.label))
      )
    ),

    activeTab === "packages" && h("section", { className: "panel" },
      h("div", { className: "admin-panel-header" },
        h("h3", null, t("adminBilling.tab.packages", "Packages")),
        h("button", { className: "primary-button", type: "button", onClick: openNewPackage }, t("adminBilling.newPackage", "New Package"))
      ),
      h(DataTable, {
        columns: [
          { key: "code", label: t("adminBilling.code", "Code"), render: (row) => row.code },
          { key: "name", label: t("adminBilling.name", "Name"), render: (row) => row.name },
          { key: "monthlyPrice", label: t("billing.monthly", "Monthly"), render: (row) => money(row.monthlyPrice, row.currency) },
          { key: "yearlyPrice", label: t("billing.yearly", "Yearly"), render: (row) => money(row.yearlyPrice, row.currency) },
          { key: "sortOrder", label: t("adminBilling.sortOrder", "Sort order"), render: (row) => row.sortOrder },
          { key: "isActive", label: t("common.status", "Status"), render: (row) => h(Badge, { tone: row.isActive ? "success" : "muted" }, row.isActive ? t("adminBilling.isActive", "Active") : t("adminBilling.inactive", "Inactive")) },
          {
            key: "actions", label: t("common.actions", "Actions"), render: (row) => h("div", { className: "row-actions" },
              h("button", { className: "ghost-button", type: "button", onClick: () => openEditPackage(row) }, t("common.edit", "Edit")),
              h("button", {
                className: "ghost-button",
                type: "button",
                disabled: busyKey === `package-active-${row.id}`,
                onClick: () => togglePackageActive(row)
              }, row.isActive ? t("adminBilling.deactivate", "Deactivate") : t("adminBilling.activate", "Activate"))
            )
          }
        ],
        rows: packages,
        getRowKey: (row) => row.id,
        emptyText: t("adminBilling.noPackages", "No packages defined.")
      })
    ),

    activeTab === "subscriptions" && h("section", { className: "panel" },
      h("div", { className: "admin-panel-header" },
        h("h3", null, t("adminBilling.tab.subscriptions", "Subscriptions")),
        h("button", {
          className: "secondary-button",
          type: "button",
          disabled: busyKey === "run-maintenance",
          onClick: runMaintenance
        }, t("adminBilling.runMaintenance", "Run maintenance"))
      ),
      h(DataTable, {
        columns: [
          { key: "tenantName", label: t("adminBilling.tenant", "Tenant"), render: (row) => `${row.tenantName} (#${row.tenantId})` },
          { key: "packageName", label: t("adminBilling.package", "Package"), render: (row) => row.packageName },
          { key: "status", label: t("common.status", "Status"), render: (row) => h(Badge, { tone: SUBSCRIPTION_STATUS_TONES[row.status] || "muted" }, row.status) },
          { key: "billingCycle", label: t("billing.billingCycle", "Billing cycle"), render: (row) => row.billingCycle },
          { key: "currentPeriodEnd", label: t("billing.periodEnd", "Current period end"), render: (row) => shortDate(row.currentPeriodEnd) },
          { key: "trialEndsAt", label: t("billing.trialEnds", "Trial ends"), render: (row) => row.trialEndsAt ? shortDate(row.trialEndsAt) : "-" },
          { key: "cancelAtPeriodEnd", label: t("billing.cancellation", "Cancellation"), render: (row) => row.cancelAtPeriodEnd ? t("billing.cancelAtPeriodEnd", "Cancels at period end") : "-" }
        ],
        rows: subscriptions,
        getRowKey: (row) => row.id,
        emptyText: t("adminBilling.noSubscriptions", "No subscriptions found.")
      }),
      h("h4", null, t("adminBilling.activateSubscription", "Activate subscription")),
      h("div", { className: "form-grid" },
        h("label", null, t("adminBilling.tenantId", "Tenant ID"),
          h("input", { type: "number", value: activateForm.tenantId, onChange: (e) => setActivateForm({ ...activateForm, tenantId: e.target.value }) })),
        h("label", null, t("adminBilling.package", "Package"),
          h("select", { value: activateForm.packageCode, onChange: (e) => setActivateForm({ ...activateForm, packageCode: e.target.value }) },
            h("option", { value: "" }, t("adminBilling.selectPackage", "Select a package")),
            packageCodeOptions.map((code) => h("option", { key: code, value: code }, code)))),
        h("label", null, t("billing.billingCycle", "Billing cycle"),
          h("select", { value: activateForm.billingCycle, onChange: (e) => setActivateForm({ ...activateForm, billingCycle: e.target.value }) },
            h("option", { value: "Monthly" }, t("billing.monthly", "Monthly")),
            h("option", { value: "Yearly" }, t("billing.yearly", "Yearly")))),
        h("label", null, t("adminBilling.paymentId", "Payment ID (optional)"),
          h("input", { type: "number", value: activateForm.paymentId, onChange: (e) => setActivateForm({ ...activateForm, paymentId: e.target.value }) })),
        h("label", null, t("adminBilling.durationDays", "Duration (days, optional)"),
          h("input", { type: "number", value: activateForm.durationDays, onChange: (e) => setActivateForm({ ...activateForm, durationDays: e.target.value }) }))
      ),
      h("div", { className: "row-actions" },
        h("button", {
          className: "primary-button",
          type: "button",
          disabled: busyKey === "activate-subscription",
          onClick: activateSubscription
        }, t("adminBilling.activate", "Activate"))
      )
    ),

    activeTab === "payments" && h("section", { className: "panel" },
      h("h3", null, t("adminBilling.tab.payments", "Payments")),
      h(DataTable, {
        columns: [
          { key: "id", label: "ID", render: (row) => row.id },
          { key: "tenantId", label: t("adminBilling.tenant", "Tenant"), render: (row) => `#${row.tenantId}` },
          { key: "packageCode", label: t("billing.plan", "Plan"), render: (row) => row.packageCode },
          { key: "billingCycle", label: t("billing.billingCycle", "Billing cycle"), render: (row) => row.billingCycle },
          { key: "amount", label: t("billing.amount", "Amount"), render: (row) => money(row.amount, row.currency) },
          { key: "providerCode", label: t("billing.provider", "Provider"), render: (row) => row.providerCode },
          { key: "status", label: t("common.status", "Status"), render: (row) => h(Badge, { tone: PAYMENT_STATUS_TONES[row.status] || "muted" }, row.status) },
          { key: "createdAt", label: t("billing.date", "Date"), render: (row) => shortDate(row.createdAt) },
          {
            key: "actions", label: t("common.actions", "Actions"), render: (row) => row.status === "Pending"
              ? h("button", {
                className: "ghost-button",
                type: "button",
                disabled: busyKey === `mark-paid-${row.id}`,
                onClick: () => markPaid(row)
              }, t("adminBilling.markPaid", "Mark paid"))
              : "-"
          }
        ],
        rows: payments,
        getRowKey: (row) => row.id,
        emptyText: t("billing.noPayments", "No payments yet.")
      })
    ),

    activeTab === "invoices" && h("section", { className: "panel" },
      h("h3", null, t("adminBilling.tab.invoices", "Invoices")),
      h(DataTable, {
        columns: [
          { key: "invoiceNumber", label: t("billing.invoiceNumber", "Invoice #"), render: (row) => row.invoiceNumber },
          { key: "tenantId", label: t("adminBilling.tenant", "Tenant"), render: (row) => `#${row.tenantId}` },
          { key: "issuedAt", label: t("billing.issued", "Issued"), render: (row) => shortDate(row.issuedAt) },
          { key: "totalAmount", label: t("billing.total", "Total"), render: (row) => money(row.totalAmount, row.currency) },
          { key: "status", label: t("common.status", "Status"), render: (row) => h(Badge, { tone: INVOICE_STATUS_TONES[row.status] || "muted" }, row.status) },
          { key: "paidAt", label: t("billing.paidAt", "Paid"), render: (row) => row.paidAt ? shortDate(row.paidAt) : "-" }
        ],
        rows: invoices,
        getRowKey: (row) => row.id,
        emptyText: t("billing.noInvoices", "No invoices yet.")
      })
    ),

    activeTab === "webhooks" && h("section", { className: "panel" },
      h("h3", null, t("adminBilling.tab.webhooks", "Webhook Events")),
      h(DataTable, {
        columns: [
          { key: "provider", label: t("billing.provider", "Provider"), render: (row) => row.provider },
          { key: "eventType", label: t("adminBilling.eventType", "Event type"), render: (row) => row.eventType },
          { key: "status", label: t("common.status", "Status"), render: (row) => h(Badge, { tone: WEBHOOK_STATUS_TONES[row.status] || "muted" }, row.status) },
          { key: "createdAt", label: t("adminBilling.received", "Received"), render: (row) => dateTime(row.createdAt) },
          { key: "processedAt", label: t("adminBilling.processed", "Processed"), render: (row) => row.processedAt ? dateTime(row.processedAt) : "-" },
          { key: "error", label: t("adminBilling.error", "Error"), render: (row) => row.error || "-" }
        ],
        rows: webhooks,
        getRowKey: (row) => row.id,
        emptyText: t("adminBilling.noWebhooks", "No webhook events recorded.")
      })
    ),

    h(PackageEditorModal, {
      form: packageForm,
      onChange: setPackageForm,
      onClose: closePackageForm,
      onSave: savePackage,
      isSaving: busyKey === "package-save",
      t
    })
  );
}
