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

function plural(count, singular, pluralValue) {
  return count === 1 ? singular : (pluralValue || `${singular}s`);
}

function isRedSignal(row) {
  return [
    row?.overallSignal,
    row?.profitSignal,
    row?.turnoverSignal,
    row?.deadStockSignal
  ].some((signal) => String(signal || "").trim().toLowerCase() === "red");
}

function isFailedMessage(message) {
  return String(message?.status || "").trim().toLowerCase().includes("fail");
}

function transactionAmount(item) {
  return Number(item?.remainingAmount ?? item?.balance ?? item?.amount ?? item?.totalAmount ?? 0) || 0;
}

function signedExpense(value) {
  return -(Number(value || 0) || 0);
}

function buildActionQueue({ dashboard, messages, currencyMarginRows, formatMoney, t }) {
  if (!dashboard) return [];

  const tasks = [];
  const netProfitLoss = Number(dashboard.dailyProfitLoss?.netProfitLoss ?? dashboard.todaySalesProfit ?? 0);
  if (netProfitLoss < 0) {
    tasks.push({
      key: "daily-profit-loss",
      tone: "danger",
      label: t("dashboard.queueProfitLoss", "P&L"),
      title: t("dashboard.queueProfitLossTitle", "Daily profit is negative"),
      detail: t("dashboard.queueProfitLossDetail", "Rent, labor, and operating payments are above gross profit today."),
      value: formatMoney(netProfitLoss),
      route: "accounting"
    });
  }

  const unpaidTransactions = dashboard.unpaidTransactions || [];
  const unpaidAmount = unpaidTransactions.reduce((sum, item) => sum + transactionAmount(item), 0);
  if (unpaidTransactions.length > 0) {
    tasks.push({
      key: "receivables",
      tone: "danger",
      label: t("dashboard.queueReceivables", "Receivables"),
      title: t("dashboard.queueReceivablesTitle", "Follow up unpaid transactions"),
      detail: t(
        "dashboard.queueReceivablesDetail",
        `${unpaidTransactions.length} open ${plural(unpaidTransactions.length, "transaction")} waiting for payment.`
      ),
      value: formatMoney(unpaidAmount),
      route: "accounting"
    });
  }

  const redSegments = (dashboard.profitHeatmap || []).filter(isRedSignal);
  if (redSegments.length > 0) {
    const categoryNames = redSegments
      .slice(0, 3)
      .map((row) => row.categoryName || t("dashboard.category", "category"))
      .join(", ");
    tasks.push({
      key: "inventory-risk",
      tone: "danger",
      label: t("dashboard.queueInventory", "Inventory"),
      title: t("dashboard.queueInventoryTitle", "Triage red stock segments"),
      detail: t(
        "dashboard.queueInventoryDetail",
        `${redSegments.length} ${plural(redSegments.length, "segment")} need margin, turnover, or dead-stock review: ${categoryNames}.`
      ),
      value: t("dashboard.stock", "Stock"),
      route: "stock"
    });
  }

  if (currencyMarginRows.length > 0) {
    tasks.push({
      key: "currency-margin",
      tone: "warning",
      label: t("dashboard.queueMargin", "Margin"),
      title: t("dashboard.queueMarginTitle", "Protect currency-exposed parts"),
      detail: t(
        "dashboard.queueMarginDetail",
        `${currencyMarginRows.length} ${plural(currencyMarginRows.length, "part")} show exchange-rate pressure.`
      ),
      value: t("dashboard.invoices", "Invoices"),
      route: "invoices"
    });
  }

  const failedMessages = (messages || []).filter(isFailedMessage);
  if (failedMessages.length > 0) {
    tasks.push({
      key: "message-failures",
      tone: "warning",
      label: t("dashboard.queueMessages", "Messages"),
      title: t("dashboard.queueMessagesTitle", "Retry failed customer messages"),
      detail: t(
        "dashboard.queueMessagesDetail",
        `${failedMessages.length} recent ${plural(failedMessages.length, "message")} failed delivery.`
      ),
      value: t("dashboard.whatsapp", "WhatsApp"),
      route: "whatsapp"
    });
  }

  if (Number(dashboard.todaySalesAmount || 0) <= 0) {
    tasks.push({
      key: "first-sale",
      tone: "neutral",
      label: t("dashboard.queueSales", "Sales desk"),
      title: t("dashboard.queueSalesTitle", "No sales posted today"),
      detail: t("dashboard.queueSalesDetail", "Open POS when the counter is ready for the first invoice."),
      value: t("dashboard.pos", "POS"),
      route: "invoices"
    });
  }

  if (tasks.length === 0) {
    tasks.push({
      key: "all-clear",
      tone: "success",
      label: t("dashboard.queueAllClear", "All clear"),
      title: t("dashboard.queueAllClearTitle", "Critical signals are quiet"),
      detail: t("dashboard.queueAllClearDetail", "No urgent receivables, margin, stock, or message exceptions in the dashboard feed."),
      value: t("dashboard.reports", "Reports"),
      route: "report-builder"
    });
  }

  return tasks.slice(0, 4);
}

function queueToneStyle(styles, tone) {
  if (tone === "danger") return styles.dashboardQueueRowDanger;
  if (tone === "warning") return styles.dashboardQueueRowWarning;
  if (tone === "success") return styles.dashboardQueueRowSuccess;
  return null;
}

function ActionQueueRow({ item, rank, onPress }) {
  const { styles } = useTheme();

  return el(Pressable, {
    accessibilityRole: "button",
    style: [styles.dashboardQueueRow, queueToneStyle(styles, item.tone)],
    onPress
  },
    el(Text, { style: styles.dashboardQueueRank }, String(rank).padStart(2, "0")),
    el(View, { style: styles.dashboardQueueCopy },
      el(Text, { style: styles.dashboardQueueLabel, numberOfLines: 1 }, item.label),
      el(Text, { style: styles.dashboardQueueTitle, numberOfLines: 1 }, item.title),
      el(Text, { style: styles.dashboardQueueDetail, numberOfLines: 2 }, item.detail)
    ),
    el(Text, { style: styles.dashboardQueueValue, numberOfLines: 1 }, item.value)
  );
}

function profitLossValueStyle(styles, value) {
  return Number(value || 0) < 0 ? styles.profitLossValueNegative : styles.profitLossValuePositive;
}

function ProfitLossRow({ label, value, formatMoney }) {
  const { styles } = useTheme();

  return el(View, { style: styles.profitLossRow },
    el(Text, { style: styles.profitLossRowLabel, numberOfLines: 1 }, label),
    el(Text, {
      style: [styles.profitLossRowValue, profitLossValueStyle(styles, value)],
      numberOfLines: 1,
      adjustsFontSizeToFit: true
    }, formatMoney(value))
  );
}

function ProfitLossPanel({ dailyProfitLoss, dashboard, formatMoney, t }) {
  const { styles } = useTheme();
  if (!dashboard) {
    return null;
  }

  const report = dailyProfitLoss || {
    grossSales: dashboard.todaySalesAmount,
    costOfGoodsSold: Number(dashboard.todaySalesAmount || 0) - Number(dashboard.todaySalesProfit || 0),
    grossProfit: dashboard.todaySalesProfit,
    rentExpense: 0,
    laborExpense: 0,
    otherOperatingExpenses: 0,
    netProfitLoss: dashboard.todaySalesProfit
  };
  const netProfitLoss = Number(report.netProfitLoss || 0);
  const rows = [
    { key: "grossSales", label: t("dashboard.grossSales", "Gross sales"), value: report.grossSales },
    { key: "cost", label: t("dashboard.costOfGoods", "Cost of goods"), value: signedExpense(report.costOfGoodsSold) },
    { key: "grossProfit", label: t("dashboard.grossProfit", "Gross profit"), value: report.grossProfit },
    { key: "rent", label: t("dashboard.rentPayments", "Rent payments"), value: signedExpense(report.rentExpense) },
    { key: "labor", label: t("dashboard.laborPayments", "Labor payments"), value: signedExpense(report.laborExpense) },
    { key: "other", label: t("dashboard.otherExpenses", "Other expenses"), value: signedExpense(report.otherOperatingExpenses) }
  ];

  return el(Panel, { title: t("dashboard.dailyProfitLoss", "Daily Profit & Loss") },
    el(View, { style: styles.profitLossHero },
      el(Text, { style: styles.profitLossHeroLabel }, t("dashboard.netProfitLoss", "Net P&L")),
      el(Text, {
        style: [styles.profitLossHeroValue, profitLossValueStyle(styles, netProfitLoss)],
        numberOfLines: 1,
        adjustsFontSizeToFit: true
      }, formatMoney(netProfitLoss)),
      el(Text, { style: styles.profitLossHeroMeta }, netProfitLoss < 0 ? t("dashboard.lossToday", "Loss today") : t("dashboard.profitToday", "Profit today"))
    ),
    el(View, { style: styles.profitLossRows },
      rows.map((row) => el(ProfitLossRow, {
        key: row.key,
        label: row.label,
        value: row.value,
        formatMoney
      }))
    )
  );
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
  const actionQueue = buildActionQueue({
    dashboard,
    messages,
    currencyMarginRows,
    formatMoney: formatDashboardMoney,
    t
  });
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
    el(ProfitLossPanel, {
      dailyProfitLoss: dashboard?.dailyProfitLoss,
      dashboard,
      formatMoney: formatDashboardMoney,
      t
    }),
    el(Panel, { title: t("dashboard.actionQueue", "Today's Action Queue") },
      actionQueue.map((item, index) => el(ActionQueueRow, {
        key: item.key,
        item,
        rank: index + 1,
        onPress: () => navigate(item.route)
      })),
      actionQueue.length === 0 && el(EmptyState, { text: t("dashboard.actionQueueEmpty", "Load the dashboard to assemble action items.") })
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
