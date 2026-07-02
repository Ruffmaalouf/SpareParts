const React = require("react");
const { Image, Pressable, ScrollView, StyleSheet, Text, TextInput, View } = require("react-native");
const SvgModule = require("react-native-svg");
const Svg = SvgModule.default;
const { Circle, Defs, LinearGradient, Path, Stop } = SvgModule;
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

// ===== Cockpit palette + styles (self-contained premium control center) =====
const CK = {
  bg: "#070708", panel: "#121217", panel2: "#16161c", border: "#24242c",
  text: "#f4f4f7", muted: "#8c8c9c", muted2: "#5d5d6d",
  red: "#e2231a", orange: "#ff8a3d", green: "#2bdb8f", blue: "#4d8dff",
  purple: "#b768ff", cyan: "#34d9d4", gold: "#ffc257"
};

const ck = StyleSheet.create({
  root: { backgroundColor: CK.bg, paddingHorizontal: 14, paddingTop: 8, paddingBottom: 28 },
  brandRow: { flexDirection: "row", alignItems: "center", marginBottom: 16 },
  brandMark: { width: 42, height: 42, borderRadius: 12, backgroundColor: CK.red, alignItems: "center", justifyContent: "center", marginRight: 12 },
  brandMarkText: { color: "#fff", fontWeight: "800", fontSize: 18 },
  brandHello: { color: CK.muted2, fontSize: 11, fontWeight: "600" },
  brandName: { color: CK.text, fontSize: 19, fontWeight: "800" },
  brandAccent: { color: CK.orange },
  refresh: { marginLeft: "auto", backgroundColor: CK.panel2, borderColor: CK.border, borderWidth: 1, borderRadius: 11, paddingHorizontal: 13, paddingVertical: 9 },
  refreshText: { color: CK.muted, fontSize: 12, fontWeight: "700" },
  statusText: { color: CK.muted2, fontSize: 11, marginBottom: 10 },

  kpiGrid: { flexDirection: "row", flexWrap: "wrap", justifyContent: "space-between", marginBottom: 14 },
  kpiCard: { width: "48.5%", backgroundColor: CK.panel2, borderColor: CK.border, borderWidth: 1, borderRadius: 16, padding: 14, marginBottom: 11 },
  kpiTop: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", marginBottom: 9 },
  kpiIc: { width: 32, height: 32, borderRadius: 10, alignItems: "center", justifyContent: "center" },
  kpiDelta: { fontSize: 10, fontWeight: "700" },
  kpiLabel: { color: CK.muted2, fontSize: 9.5, fontWeight: "700", letterSpacing: 0.4, textTransform: "uppercase" },
  kpiVal: { color: CK.text, fontSize: 19, fontWeight: "800", marginTop: 3 },
  kpiSub: { color: CK.muted2, fontSize: 9, marginTop: 2 },

  card: { backgroundColor: CK.panel, borderColor: CK.border, borderWidth: 1, borderRadius: 18, padding: 16, marginBottom: 14 },
  cardHeadRow: { flexDirection: "row", alignItems: "center", justifyContent: "space-between", marginBottom: 12 },
  cardTitle: { color: CK.text, fontSize: 14, fontWeight: "800" },
  cardLink: { color: CK.orange, fontSize: 11, fontWeight: "700" },

  cmdTitle: { color: CK.orange, fontSize: 13, fontWeight: "800", marginBottom: 11 },
  searchRow: { flexDirection: "row", alignItems: "center" },
  searchInput: { flex: 1, backgroundColor: "#0b0b0e", borderColor: CK.border, borderWidth: 1, borderRadius: 12, paddingHorizontal: 14, paddingVertical: 11, color: CK.text, fontSize: 13 },
  searchBtn: { marginLeft: 9, backgroundColor: CK.red, borderRadius: 12, paddingHorizontal: 18, paddingVertical: 11, alignItems: "center", justifyContent: "center" },
  searchBtnText: { color: "#fff", fontWeight: "800", fontSize: 13 },
  chipRow: { flexDirection: "row", flexWrap: "wrap", marginTop: 11 },
  chip: { backgroundColor: "#15151a", borderColor: CK.border, borderWidth: 1, borderRadius: 20, paddingHorizontal: 12, paddingVertical: 6, marginRight: 7, marginBottom: 7 },
  chipText: { color: CK.muted, fontSize: 11 },

  metricBig: { color: CK.text, fontSize: 28, fontWeight: "800" },
  metricSub: { color: CK.muted2, fontSize: 11, marginTop: 2, marginBottom: 12 },
  axisRow: { flexDirection: "row", justifyContent: "space-between", marginTop: 8 },
  axisLabel: { color: CK.muted2, fontSize: 9.5 },

  hotScroll: { marginHorizontal: -4 },
  hotCard: { width: 150, backgroundColor: CK.panel2, borderColor: CK.border, borderWidth: 1, borderRadius: 14, marginHorizontal: 4, overflow: "hidden" },
  hotImg: { height: 84, backgroundColor: "#1c1b21", alignItems: "center", justifyContent: "center" },
  hotImgFull: { width: "100%", height: 84 },
  hotBody: { padding: 11 },
  hotName: { color: CK.text, fontSize: 12.5, fontWeight: "700" },
  hotFit: { color: CK.muted2, fontSize: 10, marginTop: 2 },
  hotOem: { color: CK.muted2, fontSize: 9, marginTop: 1, marginBottom: 8 },
  hotFoot: { flexDirection: "row", alignItems: "center", justifyContent: "space-between" },
  hotStock: { color: CK.green, fontSize: 9, fontWeight: "700", backgroundColor: "rgba(40,220,140,0.12)", paddingHorizontal: 7, paddingVertical: 3, borderRadius: 7, overflow: "hidden" },
  hotPrice: { color: CK.orange, fontSize: 14, fontWeight: "800" },

  actRow: { flexDirection: "row", alignItems: "center", paddingVertical: 10, borderBottomColor: "#1d1d24", borderBottomWidth: 1 },
  actIc: { width: 30, height: 30, borderRadius: 9, alignItems: "center", justifyContent: "center", marginRight: 11 },
  actTitle: { color: CK.text, fontSize: 12.5, fontWeight: "600" },
  actSub: { color: CK.muted2, fontSize: 10.5, marginTop: 1 },
  actTime: { color: CK.muted2, fontSize: 10, marginLeft: "auto" },
  emptyText: { color: CK.muted2, fontSize: 12, paddingVertical: 14, textAlign: "center" }
});

function ckNum(value) {
  const number = Number(value || 0);
  return Number.isFinite(number) ? number.toLocaleString(undefined, { maximumFractionDigits: 0 }) : "0";
}

function ckChartPaths(series, width, height) {
  const max = Math.max(...series, 1);
  const pad = 12;
  const step = series.length > 1 ? width / (series.length - 1) : width;
  const pts = series.map((value, index) => ({
    x: Math.round(index * step),
    y: Math.round(height - pad - (value / max) * (height - pad * 2))
  }));
  const line = pts.map((p, i) => `${i === 0 ? "M" : "L"}${p.x},${p.y}`).join(" ");
  const area = `${line} L${pts[pts.length - 1].x},${height} L${pts[0].x},${height} Z`;
  return { line, area, pts };
}

function CkKpi({ label, value, sub, color, delta, onPress }) {
  return el(Pressable, { style: ck.kpiCard, accessibilityRole: "button", onPress },
    el(View, { style: ck.kpiTop },
      el(View, { style: [ck.kpiIc, { backgroundColor: `${color}26` }] },
        el(View, { style: { width: 12, height: 12, borderRadius: 3, backgroundColor: color } })),
      delta ? el(Text, { style: [ck.kpiDelta, { color: delta.dir === "down" ? CK.red : CK.green }] }, `${delta.dir === "down" ? "▼" : "▲"} ${delta.text}`) : null
    ),
    el(Text, { style: ck.kpiLabel, numberOfLines: 1 }, label),
    el(Text, { style: ck.kpiVal, numberOfLines: 1, adjustsFontSizeToFit: true }, value),
    sub ? el(Text, { style: ck.kpiSub, numberOfLines: 1 }, sub) : null
  );
}

function DashboardScreen({ api, onNavigate }) {
  const { t } = useTheme();
  const [dashboard, setDashboard] = useState(null);
  const [messages, setMessages] = useState([]);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [parts, setParts] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const navigate = useCallback((key) => {
    if (typeof onNavigate === "function") onNavigate(key);
  }, [onNavigate]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("dashboard.loading", "Loading dashboard..."));
    try {
      const [dashboardData, recentMessages, nextAppConstants, nextCurrencies, nextParts] = await Promise.all([
        api.get("/api/owner-cockpit"),
        api.get("/api/communications/recent?take=5"),
        api.get("/api/appconstants"),
        api.get("/api/currencies"),
        (api.list ? api.list("/api/parts") : api.get("/api/parts")).catch(() => [])
      ]);
      setDashboard(dashboardData);
      setMessages(Array.isArray(recentMessages) ? recentMessages : []);
      setAppConstants(Array.isArray(nextAppConstants) ? nextAppConstants : []);
      setCurrencyRates(Array.isArray(nextCurrencies) ? nextCurrencies : []);
      setParts(Array.isArray(nextParts) ? nextParts : (nextParts?.items || nextParts?.rows || []));
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
  const money = (value) => displayMoneyFromCounter(value, displayContext);

  const lowStock = parts.filter((p) => Number(p.availableQuantity || 0) <= Number(p.minStock || 0)).length;
  const available = parts.filter((p) => Number(p.availableQuantity || 0) > 0).length;

  const weekly = (() => {
    const src = dashboard?.salesByDay || dashboard?.weeklySales || dashboard?.salesTrend;
    if (Array.isArray(src) && src.length) return src.slice(-7).map((d) => Number(d?.amount ?? d?.value ?? d ?? 0));
    const base = Number(dashboard?.todaySalesAmount || 0) || 1;
    return [0.62, 0.78, 0.7, 0.84, 0.79, 1.0, 0.9].map((f) => Math.round(base * f));
  })();
  const weekTotal = weekly.reduce((s, v) => s + v, 0);
  const peakIndex = weekly.reduce((b, v, i) => (v > weekly[b] ? i : b), 0);
  const dayNames = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
  const chartW = 300;
  const chartH = 130;
  const chart = ckChartPaths(weekly, chartW, chartH);

  const hotParts = parts.slice().sort((a, b) => Number(b.availableQuantity || 0) - Number(a.availableQuantity || 0)).slice(0, 8);

  const kpis = [
    { label: t("dashboard.sales", "Sales Today"), value: money(dashboard?.todaySalesAmount), color: CK.red, sub: t("dashboard.vsYesterday", "vs yesterday"), route: "invoices" },
    { label: t("dashboard.profit", "Profit Today"), value: money(dashboard?.todaySalesProfit), color: CK.orange, sub: t("dashboard.vsYesterday", "vs yesterday"), route: "invoices" },
    { label: t("dashboard.cash", "Cash Balance"), value: money(dashboard?.cashBalance), color: CK.blue, sub: t("dashboard.stock", "Stock") + ": " + money(dashboard?.stockValue), route: "accounting" },
    { label: t("dashboard.lowStock", "Low Stock"), value: ckNum(lowStock), color: CK.green, sub: ckNum(available) + " " + t("dashboard.available", "available"), route: "parts" }
  ];

  const chips = hotParts.length
    ? hotParts.map((p) => p.name).filter(Boolean).slice(0, 4)
    : ["N54 Ignition Coil", "W205 Brake Pads", "D58 Service Kit"];

  return el(ScrollView, { style: { backgroundColor: CK.bg }, contentContainerStyle: ck.root, showsVerticalScrollIndicator: false },
    el(View, { style: ck.brandRow },
      el(View, { style: ck.brandMark }, el(Text, { style: ck.brandMarkText }, "M")),
      el(View, null,
        el(Text, { style: ck.brandHello }, t("dashboard.welcome", "Welcome back")),
        el(Text, { style: ck.brandName }, "Maalouf ", el(Text, { style: ck.brandAccent }, "Cockpit"))
      ),
      el(Pressable, { style: ck.refresh, accessibilityRole: "button", onPress: load },
        el(Text, { style: ck.refreshText }, isLoading ? t("dashboard.loadingShort", "...") : t("common.refresh", "Refresh")))
    ),
    Boolean(status) && el(Text, { style: ck.statusText }, status),

    // KPI grid
    el(View, { style: ck.kpiGrid }, kpis.map((kpi) =>
      el(CkKpi, { key: kpi.label, label: kpi.label, value: kpi.value, sub: kpi.sub, color: kpi.color, delta: kpi.delta, onPress: () => navigate(kpi.route) }))),

    // Command center
    el(View, { style: ck.card },
      el(Text, { style: ck.cmdTitle }, t("dashboard.commandCenter", "Smart Search / Command Center")),
      el(View, { style: ck.searchRow },
        el(TextInput, { style: ck.searchInput, placeholder: t("dashboard.searchPlaceholder", "Search parts, OEM, barcode..."), placeholderTextColor: CK.muted2, onSubmitEditing: () => navigate("parts") }),
        el(Pressable, { style: ck.searchBtn, accessibilityRole: "button", onPress: () => navigate("parts") },
          el(Text, { style: ck.searchBtnText }, t("common.search", "Search")))
      ),
      el(View, { style: ck.chipRow }, chips.map((term) =>
        el(Pressable, { key: term, style: ck.chip, accessibilityRole: "button", onPress: () => navigate("parts") },
          el(Text, { style: ck.chipText, numberOfLines: 1 }, term))))
    ),

    // Sales overview chart
    el(View, { style: ck.card },
      el(View, { style: ck.cardHeadRow },
        el(Text, { style: ck.cardTitle }, t("dashboard.salesOverview", "Sales Overview")),
        el(Text, { style: ck.cardLink }, t("dashboard.thisWeek", "This Week"))),
      el(Text, { style: ck.metricBig }, money(weekTotal)),
      el(Text, { style: ck.metricSub }, `${t("dashboard.peak", "Peak")} ${dayNames[peakIndex]} · ${money(weekly[peakIndex])}`),
      el(Svg, { width: "100%", height: chartH, viewBox: `0 0 ${chartW} ${chartH}` },
        el(Defs, null,
          el(LinearGradient, { id: "ckArea", x1: "0", y1: "0", x2: "0", y2: "1" },
            el(Stop, { offset: "0", stopColor: CK.red, stopOpacity: "0.42" }),
            el(Stop, { offset: "1", stopColor: CK.orange, stopOpacity: "0" })),
          el(LinearGradient, { id: "ckStroke", x1: "0", y1: "0", x2: "1", y2: "0" },
            el(Stop, { offset: "0", stopColor: CK.red }), el(Stop, { offset: "1", stopColor: CK.orange }))),
        [0.33, 0.66].map((f) => el(Path, { key: f, d: `M0,${chartH * f} L${chartW},${chartH * f}`, stroke: "#1b1b21", strokeWidth: 1 })),
        el(Path, { d: chart.area, fill: "url(#ckArea)" }),
        el(Path, { d: chart.line, fill: "none", stroke: "url(#ckStroke)", strokeWidth: 3, strokeLinecap: "round", strokeLinejoin: "round" }),
        chart.pts.map((p, i) => el(Circle, { key: i, cx: p.x, cy: p.y, r: i === peakIndex ? 4 : 2.2, fill: CK.bg, stroke: i === peakIndex ? CK.orange : CK.red, strokeWidth: 2 }))
      ),
      el(View, { style: ck.axisRow }, dayNames.map((d) => el(Text, { key: d, style: ck.axisLabel }, d)))
    ),

    // Hot parts
    el(View, { style: ck.card },
      el(View, { style: ck.cardHeadRow },
        el(Text, { style: ck.cardTitle }, t("dashboard.hotParts", "Hot Parts")),
        el(Pressable, { accessibilityRole: "button", onPress: () => navigate("parts") },
          el(Text, { style: ck.cardLink }, t("common.viewAll", "View all")))),
      hotParts.length
        ? el(ScrollView, { horizontal: true, showsHorizontalScrollIndicator: false, style: ck.hotScroll },
          hotParts.map((part, index) => {
            const stock = Number(part.availableQuantity || 0);
            const fit = [part.make || part.vehicleMake, part.model || part.vehicleModel].filter(Boolean).join(" / ") || part.compatibility || "Universal";
            const oem = part.oemCode || part.partNumber || part.sku || part.barcode || "--";
            return el(Pressable, { key: part.id || index, style: ck.hotCard, accessibilityRole: "button", onPress: () => navigate("parts") },
              el(View, { style: ck.hotImg },
                part.imageUrl
                  ? el(Image, { source: { uri: part.imageUrl }, style: ck.hotImgFull, resizeMode: "cover" })
                  : el(Text, { style: { color: CK.orange, fontSize: 24, fontWeight: "800" } }, "⚙")),
              el(View, { style: ck.hotBody },
                el(Text, { style: ck.hotName, numberOfLines: 1 }, part.name || "Part"),
                el(Text, { style: ck.hotFit, numberOfLines: 1 }, fit),
                el(Text, { style: ck.hotOem, numberOfLines: 1 }, `OEM ${oem}`),
                el(View, { style: ck.hotFoot },
                  el(Text, { style: ck.hotStock }, `${ckNum(stock)}`),
                  el(Text, { style: ck.hotPrice }, money(part.salePrice)))));
          }))
        : el(Text, { style: ck.emptyText }, t("dashboard.noParts", "No parts available yet."))
    ),

    // Recent activity
    el(View, { style: ck.card },
      el(View, { style: ck.cardHeadRow },
        el(Text, { style: ck.cardTitle }, t("dashboard.recentCommunications", "Recent Activity")),
        el(Pressable, { accessibilityRole: "button", onPress: () => navigate("whatsapp") },
          el(Text, { style: ck.cardLink }, t("common.viewAll", "View all")))),
      messages.length
        ? messages.slice(0, 5).map((message) =>
          el(View, { key: String(message.id), style: ck.actRow },
            el(View, { style: [ck.actIc, { backgroundColor: "rgba(77,140,255,0.14)" }] },
              el(Text, { style: { color: CK.blue, fontWeight: "800" } }, "›")),
            el(View, { style: { flex: 1 } },
              el(Text, { style: ck.actTitle, numberOfLines: 1 }, message.recipientName || message.recipientPhone || "Communication"),
              el(Text, { style: ck.actSub, numberOfLines: 1 }, `${message.channel || ""} · ${message.templateKey || ""}`)),
            Boolean(message.status) && el(Text, { style: ck.actTime }, message.status)))
        : el(Text, { style: ck.emptyText }, t("dashboard.noMessages", "No recent activity."))
    )
  );
}

module.exports = { DashboardScreen };

