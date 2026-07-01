const React = require("react");
const { Alert, Linking, Pressable, Text, View } = require("react-native");
const { money, shortDate } = require("../core/formatters");
const { EmptyState, ListRow, Panel, PrimaryButton, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

const FEATURE_LABELS = {
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

const LIMIT_LABELS = {
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

function Badge({ tone = "muted", children }) {
  const { styles } = useTheme();
  const toneKey = `billingBadge${tone.charAt(0).toUpperCase()}${tone.slice(1)}`;
  return el(Text, { style: [styles.billingBadge, styles[toneKey] || styles.billingBadgeMuted] }, children);
}

function planActionLabel(action, t) {
  switch (action) {
    case "contact": return t("billing.contactSales", "Contact sales");
    case "upgrade": return t("billing.upgrade", "Upgrade");
    case "downgrade": return t("billing.switchPlan", "Switch plan");
    default: return t("billing.switchPlan", "Switch plan");
  }
}

function BillingScreen({ api }) {
  const { styles, t } = useTheme();
  const [subscription, setSubscription] = useState(null);
  const [packages, setPackages] = useState([]);
  const [payments, setPayments] = useState([]);
  const [invoices, setInvoices] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [billingCycle, setBillingCycle] = useState("Monthly");
  const [busyCode, setBusyCode] = useState(null);
  const [checkoutInfo, setCheckoutInfo] = useState(null);
  const hasLoadedRef = React.useRef(false);

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
    } catch (error) {
      setStatus(error.message || t("billing.loadError", "Failed to load billing information."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => {
    if (hasLoadedRef.current) return;
    hasLoadedRef.current = true;
    load();
  }, [load]);

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
    } catch (error) {
      setStatus(error.message || t("billing.actionError", "The request could not be completed."));
    } finally {
      setBusyCode(null);
    }
  }, [t]);

  const handleUpgrade = useCallback((packageCode) => runAction(packageCode, async () => {
    const response = await api.post("/api/subscription/checkout", { packageCode, billingCycle });
    if (response.status === "redirect" && response.redirectUrl) {
      await Linking.openURL(response.redirectUrl);
      setStatus(t("billing.redirecting", "Complete your payment, then refresh this screen."));
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
    Alert.alert(
      immediate
        ? t("billing.confirmCancelImmediateTitle", "Cancel & downgrade now?")
        : t("billing.confirmCancelPeriodEndTitle", "Cancel at period end?"),
      immediate
        ? t("billing.confirmCancelImmediate", "Cancel now and downgrade to the Free plan immediately?")
        : t("billing.confirmCancelPeriodEnd", "Cancel at the end of the current billing period?"),
      [
        { text: t("common.no", "No"), style: "cancel" },
        {
          text: t("common.yes", "Yes"),
          style: "destructive",
          onPress: () => runAction("cancel", async () => {
            await api.post("/api/subscription/cancel", { immediate });
            setStatus(t("billing.cancelSaved", "Your cancellation has been saved."));
            await load();
          })
        }
      ]
    );
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

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: t("billing.title", "Billing & Subscription"),
      actionTitle: t("common.refresh", "Refresh"),
      onAction: load,
      loading: isLoading
    }),
    el(StatusText, { value: status }),

    subscription && el(Panel, { title: subscription.packageName },
      el(View, { style: styles.billingPlanHeader },
        el(Badge, { tone: STATUS_TONES[subscription.status] || "muted" }, subscription.status)
      ),
      el(ListRow, { title: t("billing.billingCycle", "Billing cycle"), value: subscription.billingCycle }),
      el(ListRow, { title: t("billing.periodStart", "Current period start"), value: shortDate(subscription.currentPeriodStart) }),
      el(ListRow, { title: t("billing.periodEnd", "Current period end"), value: shortDate(subscription.currentPeriodEnd) }),
      subscription.trialEndsAt && el(ListRow, { title: t("billing.trialEnds", "Trial ends"), value: shortDate(subscription.trialEndsAt) }),
      subscription.cancelAtPeriodEnd && el(StatusText, { value: t("billing.cancelAtPeriodEnd", "Cancels at period end") }),
      subscription.packageCode !== "FREE" && el(View, { style: styles.inlineButtons },
        !subscription.cancelAtPeriodEnd && el(SecondaryButton, {
          title: t("billing.cancelAtEnd", "Cancel at period end"),
          onPress: () => handleCancel(false)
        }),
        el(SecondaryButton, {
          title: t("billing.cancelImmediately", "Cancel & downgrade now"),
          onPress: () => handleCancel(true)
        })
      )
    ),

    subscription && el(Panel, { title: t("billing.usageLimits", "Usage & Limits") },
      (subscription.limits || []).map((limit) => el(ListRow, {
        key: limit.limitCode,
        title: LIMIT_LABELS[limit.limitCode] || limit.limitCode,
        value: formatLimit(limit.limitValue)
      })),
      (subscription.usage || []).map((counter) => el(ListRow, {
        key: `usage-${counter.counterCode}`,
        title: LIMIT_LABELS[counter.counterCode] || counter.counterCode,
        value: `${Number(counter.currentValue || 0).toLocaleString()} / ${formatLimit(counter.limitValue)}`
      }))
    ),

    subscription && el(Panel, { title: t("billing.features", "Plan Features") },
      (subscription.features || []).map((feature) => el(ListRow, {
        key: feature.featureCode,
        title: FEATURE_LABELS[feature.featureCode] || feature.featureCode,
        value: feature.isEnabled ? t("billing.included", "Included") : t("billing.locked", "Locked")
      }))
    ),

    checkoutInfo && checkoutInfo.status === "manual" && el(Panel, { title: t("billing.manualPayment", "Manual Payment Instructions") },
      el(Text, { style: styles.mutedText },
        checkoutInfo.instructions || t("billing.manualPaymentDefault", "Please complete the payment as instructed and an administrator will activate your plan once it is confirmed.")),
      el(StatusText, { value: `${t("billing.paymentReference", "Payment reference")}: #${checkoutInfo.paymentId}` })
    ),

    el(Panel, { title: t("billing.availablePlans", "Available Plans") },
      el(View, { style: { flexDirection: "row", gap: 8 } },
        el(Pressable, {
          style: [styles.segmentButton, billingCycle === "Monthly" && styles.segmentButtonActive],
          onPress: () => setBillingCycle("Monthly")
        }, el(Text, { style: [styles.segmentText, billingCycle === "Monthly" && styles.segmentTextActive] }, t("billing.monthly", "Monthly"))),
        el(Pressable, {
          style: [styles.segmentButton, billingCycle === "Yearly" && styles.segmentButtonActive],
          onPress: () => setBillingCycle("Yearly")
        }, el(Text, { style: [styles.segmentText, billingCycle === "Yearly" && styles.segmentTextActive] }, t("billing.yearly", "Yearly")))
      ),
      packages.map((pkg) => {
        const action = planAction(pkg);
        const price = billingCycle === "Yearly" ? pkg.yearlyPrice : pkg.monthlyPrice;
        const priceLabel = pkg.isCustomPricing
          ? t("billing.customPricing", "Custom pricing")
          : `${money(price, pkg.currency)} / ${billingCycle === "Yearly" ? t("billing.year", "year") : t("billing.month", "month")}`;
        const isBusy = busyCode === pkg.code || busyCode === `trial-${pkg.code}`;

        return el(View, { key: pkg.code, style: [styles.billingPlanCard, action === "current" && styles.billingPlanCardActive] },
          el(View, { style: styles.billingPlanHeader },
            el(Text, { style: styles.billingPlanName }, pkg.name),
            action === "current" && el(Badge, { tone: "accent" }, t("billing.currentPlan", "Current"))
          ),
          el(Text, { style: styles.billingPlanDescription }, pkg.description),
          el(Text, { style: styles.billingPlanPrice }, priceLabel),
          el(View, { style: styles.inlineButtons },
            action === "contact" && el(SecondaryButton, {
              title: t("billing.contactSales", "Contact sales"),
              onPress: () => Linking.openURL("mailto:sales@spareparts.app")
            }),
            action === "upgrade" && el(PrimaryButton, {
              title: planActionLabel(action, t),
              onPress: () => handleUpgrade(pkg.code),
              disabled: isBusy
            }),
            action === "downgrade" && el(SecondaryButton, {
              title: planActionLabel(action, t),
              onPress: () => handleSwitchPlan(pkg.code)
            }),
            action === "upgrade" && canStartTrial && el(SecondaryButton, {
              title: t("billing.startTrial", "Start 14-day trial"),
              onPress: () => handleStartTrial(pkg.code)
            })
          )
        );
      })
    ),

    el(Panel, { title: t("billing.paymentHistory", "Payment History") },
      payments.length === 0
        ? el(EmptyState, { text: t("billing.noPayments", "No payments yet.") })
        : payments.map((payment) => el(ListRow, {
          key: String(payment.id),
          title: `${shortDate(payment.createdAt)} - ${payment.packageCode} (${payment.billingCycle})`,
          subtitle: payment.providerCode,
          value: `${money(payment.amount, payment.currency)} - ${payment.status}`
        }))
    ),

    el(Panel, { title: t("billing.invoices", "Invoices") },
      invoices.length === 0
        ? el(EmptyState, { text: t("billing.noInvoices", "No invoices yet.") })
        : invoices.map((invoice) => el(ListRow, {
          key: String(invoice.id),
          title: invoice.invoiceNumber,
          subtitle: `${shortDate(invoice.issuedAt)} - ${invoice.status}`,
          value: money(invoice.totalAmount, invoice.currency)
        }))
    )
  );
}

module.exports = { BillingScreen };
module.exports.FEATURE_LABELS = FEATURE_LABELS;
module.exports.LIMIT_LABELS = LIMIT_LABELS;
