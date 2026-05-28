const React = require("react");
const { Pressable, Text, View } = require("react-native");
const { displayCurrencyContext, displayMoneyFromCounter } = require("../core/formatters");
const { EmptyState, Panel, ScreenHeader, ScreenScroll, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useState } = React;
const el = React.createElement;

function DashboardActionRow({ title, subtitle, value, onPress }) {
  const { styles } = useTheme();

  return el(Pressable, {
    accessibilityRole: "button",
    style: styles.dashboardActionRow,
    onPress
  },
    el(View, { style: styles.listRowCopy },
      el(Text, { style: styles.listRowTitle, numberOfLines: 1 }, title),
      Boolean(subtitle) && el(Text, { style: styles.listRowSubtitle, numberOfLines: 1 }, subtitle)
    ),
    Boolean(value) && el(Text, { style: styles.listRowValue, numberOfLines: 1 }, value)
  );
}

function signalStyle(styles, signal) {
  const normalized = String(signal || "yellow").toLowerCase();
  if (normalized === "green") return styles.heatmapSignalGreen;
  if (normalized === "red") return styles.heatmapSignalRed;
  return styles.heatmapSignalYellow;
}

function tileSignalStyle(styles, signal) {
  const normalized = String(signal || "yellow").toLowerCase();
  if (normalized === "green") return styles.heatmapTileGreen;
  if (normalized === "red") return styles.heatmapTileRed;
  return styles.heatmapTileYellow;
}

function percent(value) {
  const number = Number(value || 0);
  return `${Number.isFinite(number) ? number.toFixed(1) : "0.0"}%`;
}

function units(value) {
  const number = Number(value || 0);
  return Number.isFinite(number) ? number.toLocaleString(undefined, { maximumFractionDigits: 0 }) : "0";
}

function ProfitHeatmapTile({ row, formatMoney, onPress }) {
  const { styles } = useTheme();

  return el(Pressable, {
    accessibilityRole: "button",
    style: [styles.heatmapTile, tileSignalStyle(styles, row.overallSignal)],
    onPress
  },
    el(View, { style: styles.heatmapTileTopline },
      el(Text, { style: styles.heatmapTileTitle, numberOfLines: 1 }, row.categoryName || "Category"),
      el(Text, { style: styles.heatmapScore, numberOfLines: 1 }, `${Number(row.score || 0)}/100`)
    ),
    el(View, { style: styles.heatmapSignalRail },
      el(View, { style: [styles.heatmapSignal, signalStyle(styles, row.profitSignal)] },
        el(Text, { style: styles.heatmapSignalText }, "Profit")
      ),
      el(View, { style: [styles.heatmapSignal, signalStyle(styles, row.turnoverSignal)] },
        el(Text, { style: styles.heatmapSignalText }, "Turnover")
      ),
      el(View, { style: [styles.heatmapSignal, signalStyle(styles, row.deadStockSignal)] },
        el(Text, { style: styles.heatmapSignalText }, "Dead stock")
      )
    ),
    el(View, { style: styles.heatmapMetricRow },
      el(View, { style: styles.heatmapMetricCell },
        el(Text, { style: styles.heatmapMetricLabel }, "Profit"),
        el(Text, { style: styles.heatmapMetricValue, numberOfLines: 1, adjustsFontSizeToFit: true }, formatMoney(row.profit)),
        el(Text, { style: styles.heatmapMetricMeta }, percent(row.profitMarginPercent))
      ),
      el(View, { style: styles.heatmapMetricCell },
        el(Text, { style: styles.heatmapMetricLabel }, "Turnover"),
        el(Text, { style: styles.heatmapMetricValue }, units(row.turnoverUnits)),
        el(Text, { style: styles.heatmapMetricMeta }, percent(row.turnoverRatePercent))
      ),
      el(View, { style: styles.heatmapMetricCell },
        el(Text, { style: styles.heatmapMetricLabel }, "Dead"),
        el(Text, { style: styles.heatmapMetricValue }, units(row.deadStockUnits)),
        el(Text, { style: styles.heatmapMetricMeta }, percent(row.deadStockPercent))
      )
    ),
    el(Text, { style: styles.heatmapStockLine, numberOfLines: 1 },
      `${units(row.stockUnits)} on hand | ${formatMoney(row.deadStockValue)} dead value`
    )
  );
}

function DashboardScreen({ api, onNavigate }) {
  const { styles, t } = useTheme();
  const [dashboard, setDashboard] = useState(null);
  const [messages, setMessages] = useState([]);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const navigate = useCallback((key) => {
    if (typeof onNavigate === "function") {
      onNavigate(key);
    }
  }, [onNavigate]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("dashboard.loading", "Loading dashboard..."));
    try {
      const [dashboardData, recentMessages, nextAppConstants, nextCurrencies] = await Promise.all([
        api.get("/api/owner-cockpit"),
        api.get("/api/communications/recent?take=5"),
        api.get("/api/appconstants"),
        api.get("/api/currencies")
      ]);
      setDashboard(dashboardData);
      setMessages(recentMessages);
      setAppConstants(Array.isArray(nextAppConstants) ? nextAppConstants : []);
      setCurrencyRates(Array.isArray(nextCurrencies) ? nextCurrencies : []);
      setStatus(t("dashboard.loaded", "Dashboard loaded."));
    } catch (error) {
      setStatus(error.message || t("dashboard.loadError", "Could not load dashboard."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => { load(); }, [load]);

  const displayContext = displayCurrencyContext({
    constants: appConstants,
    rates: currencyRates,
    counterCurrencyCode: dashboard?.currencyCode
  });
  const formatDashboardMoney = (value) => displayMoneyFromCounter(value, displayContext);
  const unpaidTransactions = dashboard?.unpaidTransactions || [];
  const profitHeatmap = dashboard?.profitHeatmap || [];
  const currencyMarginRows = (dashboard?.profitPerPart || [])
    .filter((row) => row.currencyMovementEatsProfit || row.currencyWarning || Number(row.currencyMovementImpact || 0) < 0)
    .slice(0, 5);
  const metrics = [
    { key: "sales", label: t("dashboard.sales", "Sales"), value: formatDashboardMoney(dashboard?.todaySalesAmount), route: "invoices", action: t("dashboard.openSales", "Open sales") },
    { key: "profit", label: t("dashboard.profit", "Profit"), value: formatDashboardMoney(dashboard?.todaySalesProfit), route: "invoices", action: t("dashboard.openInvoices", "Review invoices") },
    { key: "cash", label: t("dashboard.cash", "Cash"), value: formatDashboardMoney(dashboard?.cashBalance), route: "accounting", action: t("dashboard.openAccounting", "Open accounting") },
    { key: "supplierDebt", label: t("dashboard.supplierDebt", "Supplier debt"), value: formatDashboardMoney(dashboard?.supplierDebt), route: "accounting", action: t("dashboard.openPayables", "Review payables") },
    { key: "customerDebt", label: t("dashboard.customerDebt", "Customer debt"), value: formatDashboardMoney(dashboard?.customerDebt), route: "accounting", action: t("dashboard.openReceivables", "Review receivables") },
    { key: "stock", label: t("dashboard.stock", "Stock"), value: formatDashboardMoney(dashboard?.stockValue), route: "stock", action: t("dashboard.openStock", "Open stock") }
  ];
  const quickActions = [
    { key: "invoices", badge: t("dashboard.quickSaleBadge", "POS"), title: t("dashboard.quickSale", "New sale"), subtitle: t("dashboard.quickSaleHint", "Create or search invoices.") },
    { key: "parts", badge: t("dashboard.quickPartsBadge", "Stock"), title: t("dashboard.quickParts", "Find parts"), subtitle: t("dashboard.quickPartsHint", "Search catalog, OEM, and barcodes.") },
    { key: "management", badge: t("dashboard.quickWebBadge", "Web"), title: t("dashboard.quickWeb", "Web users"), subtitle: t("dashboard.quickWebHint", "Manage web access, users, roles, and catalog setup.") },
    { key: "whatsapp", badge: t("dashboard.quickWhatsAppBadge", "Chat"), title: t("dashboard.quickWhatsApp", "Message customer"), subtitle: t("dashboard.quickWhatsAppHint", "Open recent WhatsApp conversations.") },
    { key: "accounting", badge: t("dashboard.quickMoneyBadge", "Money"), title: t("dashboard.quickMoney", "Money owed"), subtitle: t("dashboard.quickMoneyHint", "Review ledgers and account statements.") },
    { key: "report-builder", badge: t("dashboard.quickReportBadge", "Reports"), title: t("dashboard.quickReport", "Build report"), subtitle: t("dashboard.quickReportHint", "Open saved reports and schema tools.") }
  ];

  return el(ScreenScroll, null,
    el(ScreenHeader, { title: t("dashboard.title", "Dashboard"), actionTitle: t("common.refresh", "Refresh"), onAction: load, loading: isLoading }),
    el(StatusText, { value: status }),
    el(Panel, { title: t("dashboard.adminActions", "Admin Actions") },
      el(View, { style: styles.dashboardActionGrid },
        quickActions.map((action) => el(Pressable, {
          key: action.key,
          accessibilityRole: "button",
          style: [styles.dashboardActionButton, action.key === "management" && styles.dashboardActionButtonFeatured],
          onPress: () => navigate(action.key)
        },
          el(Text, { style: styles.dashboardActionBadge, numberOfLines: 1 }, action.badge),
          el(Text, { style: styles.dashboardActionTitle, numberOfLines: 1 }, action.title),
          el(Text, { style: styles.dashboardActionSubtitle, numberOfLines: 2 }, action.subtitle)
        ))
      )
    ),
    el(View, { style: styles.metricGrid },
      metrics.map((metric) => el(Pressable, {
        key: metric.key,
        accessibilityRole: "button",
        style: styles.metricTile,
        onPress: () => navigate(metric.route)
      },
        el(Text, { style: styles.metricLabel }, metric.label),
        el(Text, { style: styles.metricValue, numberOfLines: 1, adjustsFontSizeToFit: true }, metric.value),
        el(Text, { style: styles.metricAction, numberOfLines: 1 }, metric.action)
      ))
    ),
    el(Panel, { title: t("dashboard.profitHeatmap", "Live Profit Heatmap") },
      el(Text, { style: styles.statusText }, t("dashboard.profitHeatmapScope", "30-day margin, 30-day turnover, 90-day dead stock.")),
      el(View, { style: styles.heatmapGrid },
        profitHeatmap.map((row) => el(ProfitHeatmapTile, {
          key: row.segmentKey || row.categoryName,
          row,
          formatMoney: formatDashboardMoney,
          onPress: () => navigate("stock")
        })),
        profitHeatmap.length === 0 && el(EmptyState, { text: t("dashboard.noProfitHeatmap", "No category heatmap data yet.") })
      )
    ),
    el(Panel, { title: t("dashboard.currencyMarginWatch", "Currency Margin Watch") },
      currencyMarginRows.map((row, index) => el(DashboardActionRow, {
        key: `${row.name}-${index}`,
        title: row.name,
        subtitle: row.currencyWarning || t("dashboard.currencyMarginReduced", "Currency movement reduced margin"),
        value: `${formatDashboardMoney(row.marginAtPurchaseRate)} → ${formatDashboardMoney(row.marginAtCurrentRate)}`,
        onPress: () => navigate("invoices")
      })),
      currencyMarginRows.length === 0 && el(EmptyState, { text: t("dashboard.noCurrencyMarginWarnings", "No currency margin warnings.") })
    ),
    el(Panel, { title: t("dashboard.unpaidTransactions", "Unpaid Transactions") },
      unpaidTransactions.slice(0, 5).map((item, index) => {
        const title = item.transactionNumber || item.referenceNumber || t("dashboard.transaction", "Transaction");
        const subtitle = item.counterparty || item.partnerName || item.partner || item.transactionType || "";
        const value = formatDashboardMoney(item.remainingAmount ?? item.balance ?? item.amount ?? item.totalAmount);

        return el(DashboardActionRow, {
          key: `${title}-${index}`,
          title,
          subtitle,
          value,
          onPress: () => navigate("accounting")
        });
      }),
      unpaidTransactions.length === 0 && el(EmptyState, { text: t("dashboard.noUnpaidTransactions", "No unpaid transactions returned.") })
    ),
    el(Panel, { title: t("dashboard.recentCommunications", "Recent Communications") },
      messages.map((message) => el(DashboardActionRow, {
        key: String(message.id),
        title: message.recipientName || message.recipientPhone,
        subtitle: `${message.channel} · ${message.templateKey}`,
        value: message.status,
        onPress: () => navigate("whatsapp")
      })),
      messages.length === 0 && el(EmptyState, { text: t("dashboard.noMessages", "No messages yet.") })
    )
  );
}

module.exports = { DashboardScreen };
