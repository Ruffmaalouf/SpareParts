const React = require("react");
const { Pressable, ScrollView, Switch, Text, View } = require("react-native");
const { money, shortDate, dateTime } = require("../core/formatters");
const { EmptyState, Field, ListRow, Panel, PrimaryButton, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { FEATURE_LABELS, LIMIT_LABELS } = require("./billing-screen");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

const FEATURE_CODES = Object.keys(FEATURE_LABELS);
const LIMIT_CODES = Object.keys(LIMIT_LABELS);
const SUPER_ADMIN_ROLE_ID = 5;

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

function toneStyle(styles, tone) {
  const toneKey = `billingBadge${String(tone || "muted").charAt(0).toUpperCase()}${String(tone || "muted").slice(1)}`;
  return styles[toneKey] || styles.billingBadgeMuted;
}

function Badge({ tone, children }) {
  const { styles } = useTheme();
  return el(Text, { style: [styles.billingBadge, toneStyle(styles, tone)] }, children);
}

function read(row, ...keys) {
  for (const key of keys) {
    const value = row && row[key];
    if (value !== undefined && value !== null && value !== "") return value;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row && row[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }
  return "";
}

function formatLimit(value) {
  return Number(value) === -1 ? "Unlimited" : Number(value || 0).toLocaleString();
}

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
    const match = (pkg.features || []).find((feature) => feature.featureCode === code);
    return [code, Boolean(match && match.isEnabled)];
  }));
  const limits = Object.fromEntries(LIMIT_CODES.map((code) => {
    const match = (pkg.limits || []).find((limit) => limit.limitCode === code);
    return [code, String(match ? match.limitValue : 0)];
  }));

  return {
    id: pkg.id,
    code: pkg.code,
    name: pkg.name,
    description: pkg.description || "",
    monthlyPrice: String(pkg.monthlyPrice ?? 0),
    yearlyPrice: String(pkg.yearlyPrice ?? 0),
    currency: pkg.currency || "USD",
    sortOrder: String(pkg.sortOrder ?? 0),
    isActive: Boolean(pkg.isActive),
    isCustomPricing: Boolean(pkg.isCustomPricing),
    features,
    limits
  };
}

function formToRequest(form) {
  return {
    code: String(form.code || "").trim().toUpperCase(),
    name: String(form.name || "").trim(),
    description: String(form.description || "").trim(),
    monthlyPrice: Number(form.monthlyPrice) || 0,
    yearlyPrice: Number(form.yearlyPrice) || 0,
    currency: String(form.currency || "USD").trim().toUpperCase() || "USD",
    sortOrder: Number(form.sortOrder) || 0,
    isActive: Boolean(form.isActive),
    isCustomPricing: Boolean(form.isCustomPricing),
    features: FEATURE_CODES.map((code) => ({ featureCode: code, isEnabled: Boolean(form.features[code]) })),
    limits: LIMIT_CODES.map((code) => ({ limitCode: code, limitValue: Number(form.limits[code]) }))
  };
}

function setField(setForm, key, value) {
  setForm((current) => ({ ...current, [key]: value }));
}

function setFeature(setForm, code, value) {
  setForm((current) => ({
    ...current,
    features: { ...current.features, [code]: value }
  }));
}

function setLimit(setForm, code, value) {
  setForm((current) => ({
    ...current,
    limits: { ...current.limits, [code]: value }
  }));
}

function ItemList({ rows, emptyText, renderRow }) {
  const { styles } = useTheme();

  return el(View, { style: styles.screenListFrame },
    el(ScrollView, {
      nestedScrollEnabled: true,
      showsVerticalScrollIndicator: true,
      contentContainerStyle: styles.screenListContent
    },
      rows.map(renderRow),
      rows.length === 0 && el(EmptyState, { text: emptyText })
    )
  );
}

function AdminBillingScreen({ api, user }) {
  const { styles, t } = useTheme();
  const [activeTab, setActiveTab] = useState("packages");
  const [packages, setPackages] = useState([]);
  const [subscriptions, setSubscriptions] = useState([]);
  const [payments, setPayments] = useState([]);
  const [invoices, setInvoices] = useState([]);
  const [webhooks, setWebhooks] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [busyKey, setBusyKey] = useState("");
  const [packageForm, setPackageForm] = useState(null);
  const [activateForm, setActivateForm] = useState({ tenantId: "", packageCode: "", billingCycle: "Monthly", paymentId: "", durationDays: "" });

  const roleId = Number(user?.roleId ?? user?.RoleId ?? user?.roleID ?? user?.role_id);
  const packageCodeOptions = useMemo(() => packages.map((pkg) => pkg.code), [packages]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("");
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
    } catch (error) {
      setStatus(error.message || t("adminBilling.loadError", "Failed to load billing administration data."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => {
    if (roleId !== SUPER_ADMIN_ROLE_ID) return;
    load();
  }, [load, roleId]);

  const runTask = useCallback(async (key, action, successMessage) => {
    setBusyKey(key);
    setStatus("");
    try {
      await action();
      if (successMessage) setStatus(successMessage);
      await load();
    } catch (error) {
      setStatus(error.message || t("billing.actionError", "The request could not be completed."));
    } finally {
      setBusyKey("");
    }
  }, [load, t]);

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
  }, pkg.isActive
    ? t("adminBilling.packageDeactivated", "Package deactivated.")
    : t("adminBilling.packageActivated", "Package activated.")), [api, runTask, t]);

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

  if (roleId !== SUPER_ADMIN_ROLE_ID) {
    return el(ScreenScroll, null,
      el(ScreenHeader, { title: t("adminBilling.title", "Pricing & Subscriptions") }),
      el(StatusText, { value: t("adminBilling.forbidden", "Only platform administrators can open this workspace.") })
    );
  }

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: t("adminBilling.title", "Pricing & Subscriptions"),
      actionTitle: t("common.refresh", "Refresh"),
      onAction: load,
      loading: isLoading
    }),
    el(Panel, { title: t("adminBilling.kicker", "Platform Administration") },
      el(ListRow, {
        title: t("adminBilling.subtitle", "Manage pricing packages, tenant subscriptions, payments, invoices, and webhook events.")
      })
    ),
    el(View, { style: styles.inlineButtons },
      TABS.map((tab) => el(Pressable, {
        key: tab.key,
        style: [styles.segmentButton, activeTab === tab.key && styles.segmentButtonActive],
        onPress: () => setActiveTab(tab.key)
      },
      el(Text, { style: [styles.segmentText, activeTab === tab.key && styles.segmentTextActive] }, t(`adminBilling.tab.${tab.key}`, tab.label)))))
    ,
    el(StatusText, { value: status }),

    activeTab === "packages" && el(React.Fragment, null,
      el(Panel, { title: t("adminBilling.packageEditor", "Package editor") },
        el(View, { style: styles.inlineButtons },
          el(PrimaryButton, {
            title: packageForm?.id
              ? t("adminBilling.editingPackage", "Editing package")
              : t("adminBilling.newPackage", "New Package"),
            onPress: () => setPackageForm(packageForm || emptyPackageForm()),
            compact: true
          }),
          packageForm && el(SecondaryButton, {
            title: t("common.cancel", "Cancel"),
            onPress: () => setPackageForm(null)
          })
        ),
        packageForm && el(React.Fragment, null,
          el(Field, { label: t("adminBilling.code", "Code"), value: packageForm.code, onChangeText: (value) => setField(setPackageForm, "code", value), autoCapitalize: "characters" }),
          el(Field, { label: t("adminBilling.name", "Name"), value: packageForm.name, onChangeText: (value) => setField(setPackageForm, "name", value) }),
          el(Field, { label: t("adminBilling.description", "Description"), value: packageForm.description, onChangeText: (value) => setField(setPackageForm, "description", value), multiline: true, numberOfLines: 3 }),
          el(View, { style: styles.inlineButtons },
            el(View, { style: { flex: 1, minWidth: 140 } },
              el(Field, { label: t("adminBilling.monthlyPrice", "Monthly price"), value: packageForm.monthlyPrice, onChangeText: (value) => setField(setPackageForm, "monthlyPrice", value), keyboardType: "decimal-pad" })),
            el(View, { style: { flex: 1, minWidth: 140 } },
              el(Field, { label: t("adminBilling.yearlyPrice", "Yearly price"), value: packageForm.yearlyPrice, onChangeText: (value) => setField(setPackageForm, "yearlyPrice", value), keyboardType: "decimal-pad" }))
          ),
          el(View, { style: styles.inlineButtons },
            el(View, { style: { flex: 1, minWidth: 140 } },
              el(Field, { label: t("adminBilling.currency", "Currency"), value: packageForm.currency, onChangeText: (value) => setField(setPackageForm, "currency", value), autoCapitalize: "characters" })),
            el(View, { style: { flex: 1, minWidth: 140 } },
              el(Field, { label: t("adminBilling.sortOrder", "Sort order"), value: packageForm.sortOrder, onChangeText: (value) => setField(setPackageForm, "sortOrder", value), keyboardType: "number-pad" }))
          ),
          el(ListRow, {
            title: t("adminBilling.isActive", "Active"),
            subtitle: t("adminBilling.isActiveHint", "Controls whether the package can be selected.")
          }),
          el(Switch, { value: packageForm.isActive, onValueChange: (value) => setField(setPackageForm, "isActive", value) }),
          el(ListRow, {
            title: t("adminBilling.isCustomPricing", "Custom pricing"),
            subtitle: t("adminBilling.isCustomPricingHint", "Use contact-sales flow instead of direct checkout.")
          }),
          el(Switch, { value: packageForm.isCustomPricing, onValueChange: (value) => setField(setPackageForm, "isCustomPricing", value) }),
          el(Panel, { title: t("adminBilling.features", "Features") },
            FEATURE_CODES.map((code) => el(View, { key: code, style: styles.listRow },
              el(View, { style: styles.listRowCopy },
                el(Text, { style: styles.listRowTitle }, FEATURE_LABELS[code])),
              el(Switch, { value: Boolean(packageForm.features[code]), onValueChange: (value) => setFeature(setPackageForm, code, value) })
            ))
          ),
          el(Panel, { title: t("adminBilling.limits", "Limits") },
            el(StatusText, { value: t("adminBilling.limitsHint", "Use -1 for unlimited.") }),
            LIMIT_CODES.map((code) => el(Field, {
              key: code,
              label: LIMIT_LABELS[code],
              value: packageForm.limits[code],
              onChangeText: (value) => setLimit(setPackageForm, code, value),
              keyboardType: "number-pad"
            }))
          ),
          el(PrimaryButton, {
            title: busyKey === "package-save" ? t("common.loading", "Loading") : t("common.save", "Save"),
            onPress: savePackage,
            disabled: busyKey === "package-save"
          })
        )
      ),
      el(Panel, { title: t("adminBilling.tab.packages", "Packages") },
        el(ItemList, {
          rows: packages,
          emptyText: t("adminBilling.noPackages", "No packages defined."),
          renderRow: (pkg) => el(View, { key: `package-${pkg.id}`, style: styles.screenListItem },
            el(ListRow, {
              title: `${pkg.name} (${pkg.code})`,
              subtitle: `${money(pkg.monthlyPrice, pkg.currency)} / ${t("billing.month", "month")} • ${money(pkg.yearlyPrice, pkg.currency)} / ${t("billing.year", "year")}`,
              value: `#${pkg.sortOrder}`
            }),
            el(View, { style: styles.inlineButtons },
              el(Badge, { tone: pkg.isActive ? "success" : "muted" }, pkg.isActive ? t("adminBilling.isActive", "Active") : t("adminBilling.inactive", "Inactive")),
              el(SecondaryButton, { title: t("common.edit", "Edit"), onPress: () => setPackageForm(packageToForm(pkg)) }),
              el(PrimaryButton, {
                title: busyKey === `package-active-${pkg.id}`
                  ? t("common.loading", "Loading")
                  : pkg.isActive ? t("adminBilling.deactivate", "Deactivate") : t("adminBilling.activate", "Activate"),
                onPress: () => togglePackageActive(pkg),
                disabled: busyKey === `package-active-${pkg.id}`,
                compact: true
              })
            )
          )
        })
      )
    ),

    activeTab === "subscriptions" && el(React.Fragment, null,
      el(Panel, { title: t("adminBilling.activateSubscription", "Activate subscription") },
        el(Field, { label: t("adminBilling.tenantId", "Tenant ID"), value: activateForm.tenantId, onChangeText: (value) => setActivateForm((current) => ({ ...current, tenantId: value })), keyboardType: "number-pad" }),
        el(Field, { label: t("adminBilling.package", "Package"), value: activateForm.packageCode, onChangeText: (value) => setActivateForm((current) => ({ ...current, packageCode: value.toUpperCase() })), placeholder: packageCodeOptions.join(", "), autoCapitalize: "characters" }),
        el(View, { style: styles.inlineButtons },
          el(Pressable, {
            style: [styles.segmentButton, activateForm.billingCycle === "Monthly" && styles.segmentButtonActive],
            onPress: () => setActivateForm((current) => ({ ...current, billingCycle: "Monthly" }))
          }, el(Text, { style: [styles.segmentText, activateForm.billingCycle === "Monthly" && styles.segmentTextActive] }, t("billing.monthly", "Monthly"))),
          el(Pressable, {
            style: [styles.segmentButton, activateForm.billingCycle === "Yearly" && styles.segmentButtonActive],
            onPress: () => setActivateForm((current) => ({ ...current, billingCycle: "Yearly" }))
          }, el(Text, { style: [styles.segmentText, activateForm.billingCycle === "Yearly" && styles.segmentTextActive] }, t("billing.yearly", "Yearly")))
        ),
        el(Field, { label: t("adminBilling.paymentId", "Payment ID (optional)"), value: activateForm.paymentId, onChangeText: (value) => setActivateForm((current) => ({ ...current, paymentId: value })), keyboardType: "number-pad" }),
        el(Field, { label: t("adminBilling.durationDays", "Duration (days, optional)"), value: activateForm.durationDays, onChangeText: (value) => setActivateForm((current) => ({ ...current, durationDays: value })), keyboardType: "number-pad" }),
        el(View, { style: styles.inlineButtons },
          el(PrimaryButton, {
            title: busyKey === "activate-subscription" ? t("common.loading", "Loading") : t("adminBilling.activate", "Activate"),
            onPress: activateSubscription,
            disabled: busyKey === "activate-subscription",
            compact: true
          }),
          el(SecondaryButton, { title: t("adminBilling.runMaintenance", "Run maintenance"), onPress: runMaintenance })
        )
      ),
      el(Panel, { title: t("adminBilling.tab.subscriptions", "Subscriptions") },
        el(ItemList, {
          rows: subscriptions,
          emptyText: t("adminBilling.noSubscriptions", "No subscriptions found."),
          renderRow: (row) => el(View, { key: `subscription-${row.id}`, style: styles.screenListItem },
            el(ListRow, {
              title: `${row.tenantName} (#${row.tenantId})`,
              subtitle: `${row.packageName} • ${row.billingCycle} • ${shortDate(row.currentPeriodEnd)}`,
              value: row.cancelAtPeriodEnd ? t("billing.cancelAtPeriodEnd", "Cancels at period end") : ""
            }),
            el(Badge, { tone: SUBSCRIPTION_STATUS_TONES[row.status] || "muted" }, row.status)
          )
        })
      )
    ),

    activeTab === "payments" && el(Panel, { title: t("adminBilling.tab.payments", "Payments") },
      el(ItemList, {
        rows: payments,
        emptyText: t("billing.noPayments", "No payments yet."),
        renderRow: (payment) => el(View, { key: `payment-${payment.id}`, style: styles.screenListItem },
          el(ListRow, {
            title: `${payment.packageCode} • ${money(payment.amount, payment.currency)}`,
            subtitle: `#${payment.id} • Tenant #${payment.tenantId} • ${payment.billingCycle} • ${shortDate(payment.createdAt)}`,
            value: payment.providerCode
          }),
          el(View, { style: styles.inlineButtons },
            el(Badge, { tone: PAYMENT_STATUS_TONES[payment.status] || "muted" }, payment.status),
            payment.status === "Pending" && el(PrimaryButton, {
              title: busyKey === `mark-paid-${payment.id}` ? t("common.loading", "Loading") : t("adminBilling.markPaid", "Mark paid"),
              onPress: () => markPaid(payment),
              disabled: busyKey === `mark-paid-${payment.id}`,
              compact: true
            })
          )
        )
      })
    ),

    activeTab === "invoices" && el(Panel, { title: t("adminBilling.tab.invoices", "Invoices") },
      el(ItemList, {
        rows: invoices,
        emptyText: t("billing.noInvoices", "No invoices yet."),
        renderRow: (invoice) => el(View, { key: `invoice-${invoice.id}`, style: styles.screenListItem },
          el(ListRow, {
            title: invoice.invoiceNumber,
            subtitle: `Tenant #${invoice.tenantId} • ${shortDate(invoice.issuedAt)}`,
            value: money(invoice.totalAmount, invoice.currency)
          }),
          el(Badge, { tone: INVOICE_STATUS_TONES[invoice.status] || "muted" }, invoice.status)
        )
      })
    ),

    activeTab === "webhooks" && el(Panel, { title: t("adminBilling.tab.webhooks", "Webhook Events") },
      el(ItemList, {
        rows: webhooks,
        emptyText: t("adminBilling.noWebhooks", "No webhook events recorded."),
        renderRow: (row) => el(View, { key: `webhook-${row.id}`, style: styles.screenListItem },
          el(ListRow, {
            title: `${row.provider} • ${row.eventType}`,
            subtitle: `${dateTime(row.createdAt)}${row.processedAt ? ` • ${dateTime(row.processedAt)}` : ""}`,
            value: row.error || ""
          }),
          el(Badge, { tone: WEBHOOK_STATUS_TONES[row.status] || "muted" }, row.status)
        )
      })
    )
  );
}

module.exports = { AdminBillingScreen };
