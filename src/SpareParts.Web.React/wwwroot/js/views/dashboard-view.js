import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { asRows, displayCurrencyContext, displayMoneyFromCounter } from "../core/formatters.js";
import { Icon } from "../components/ui/CockpitIcons.js";

function metricLevel(value) {
  const number = Math.abs(Number(value || 0));
  if (!Number.isFinite(number) || number === 0) return 8;
  return Math.max(16, Math.min(96, Math.round((number / (number + 2500)) * 100)));
}

function signalClass(signal) {
  const normalized = String(signal || "yellow").trim().toLowerCase();
  return normalized === "green" || normalized === "red" ? normalized : "yellow";
}

function percent(value) {
  const number = Number(value || 0);
  return `${Number.isFinite(number) ? number.toFixed(1) : "0.0"}%`;
}

function units(value) {
  const number = Number(value || 0);
  return Number.isFinite(number) ? number.toLocaleString(undefined, { maximumFractionDigits: 0 }) : "0";
}

function plural(count, singular, pluralValue = `${singular}s`) {
  return count === 1 ? singular : pluralValue;
}

function normalizeText(value) {
  return String(value || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .replace(/\s+/g, " ");
}

function isActivePartRequest(request) {
  const status = normalizeText(request?.status);
  return status === "open" || status === "contacted" || status === "reserved";
}

function marginWatchPart(part) {
  const salePrice = Number(part?.salePrice || 0);
  const costPrice = Number(part?.costPrice || 0);
  const marketPrice = Number(part?.estimatedMarketPrice || part?.averagePrice || 0);
  if (salePrice <= 0) return true;
  if (costPrice > 0 && (salePrice - costPrice) / salePrice < 0.22) return true;
  return marketPrice > 0 && salePrice < marketPrice * 0.9;
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

function profitLossValueClass(value) {
  return Number(value || 0) < 0 ? "negative" : "positive";
}

function buildActionQueue({ dashboard, recentMessages, currencyMarginRows, formatMoney }) {
  if (!dashboard) return [];

  const tasks = [];
  const netProfitLoss = Number(dashboard.dailyProfitLoss?.netProfitLoss ?? dashboard.todaySalesProfit ?? 0);
  if (netProfitLoss < 0) {
    tasks.push({
      key: "daily-profit-loss",
      tone: "danger",
      label: "P&L",
      title: "Daily profit is negative",
      detail: "Rent, labor, and operating payments are above gross profit today.",
      value: formatMoney(netProfitLoss),
      view: "accounting"
    });
  }

  const unpaidTransactions = dashboard.unpaidTransactions || [];
  const unpaidAmount = unpaidTransactions.reduce((sum, item) => sum + transactionAmount(item), 0);
  if (unpaidTransactions.length > 0) {
    tasks.push({
      key: "receivables",
      tone: "danger",
      label: "Receivables",
      title: "Follow up unpaid transactions",
      detail: `${unpaidTransactions.length} open ${plural(unpaidTransactions.length, "transaction")} waiting for payment.`,
      value: formatMoney(unpaidAmount),
      view: "accounting"
    });
  }

  const redSegments = (dashboard.profitHeatmap || []).filter(isRedSignal);
  if (redSegments.length > 0) {
    const categoryNames = redSegments
      .slice(0, 3)
      .map((row) => row.categoryName || "category")
      .join(", ");
    tasks.push({
      key: "inventory-risk",
      tone: "danger",
      label: "Inventory",
      title: "Triage red stock segments",
      detail: `${redSegments.length} ${plural(redSegments.length, "segment")} need margin, turnover, or dead-stock review: ${categoryNames}.`,
      value: "Stock",
      view: "stock"
    });
  }

  if (currencyMarginRows.length > 0) {
    tasks.push({
      key: "currency-margin",
      tone: "warning",
      label: "Margin",
      title: "Protect currency-exposed parts",
      detail: `${currencyMarginRows.length} ${plural(currencyMarginRows.length, "part")} show exchange-rate pressure.`,
      value: "Invoices",
      view: "invoices"
    });
  }

  const failedMessages = (recentMessages || []).filter(isFailedMessage);
  if (failedMessages.length > 0) {
    tasks.push({
      key: "message-failures",
      tone: "warning",
      label: "Messages",
      title: "Retry failed customer messages",
      detail: `${failedMessages.length} recent ${plural(failedMessages.length, "message")} failed delivery.`,
      value: "WhatsApp",
      view: "whatsapp"
    });
  }

  if (Number(dashboard.todaySalesAmount || 0) <= 0) {
    tasks.push({
      key: "first-sale",
      tone: "neutral",
      label: "Sales desk",
      title: "No sales posted today",
      detail: "Open POS when the counter is ready for the first invoice.",
      value: "POS",
      view: "invoices"
    });
  }

  if (tasks.length === 0) {
    tasks.push({
      key: "all-clear",
      tone: "success",
      label: "All clear",
      title: "Critical signals are quiet",
      detail: "No urgent receivables, margin, stock, or message exceptions in the dashboard feed.",
      value: "Reports",
      view: "report-builder"
    });
  }

  return tasks.slice(0, 4);
}

export function DashboardView({ api, onView, user }) {
  const [dashboard, setDashboard] = useState(null);
  const [recentMessages, setRecentMessages] = useState([]);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [parts, setParts] = useState([]);
  const [partRequests, setPartRequests] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [overdueTotal, setOverdueTotal] = useState(null);

  const navigate = useCallback((nextView) => {
    if (typeof onView === "function") {
      onView(nextView);
    }
  }, [onView]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading dashboard...");
    try {
      const [
        dashboardData,
        messages,
        nextAppConstants,
        nextCurrencies,
        nextParts,
        nextPartRequests
      ] = await Promise.all([
        api.get("/api/owner-cockpit"),
        api.get("/api/communications/recent?take=6"),
        api.get("/api/appconstants"),
        api.get("/api/currencies"),
        api.list("/api/parts").catch(() => []),
        api.get("/api/partrequests?status=Active").catch(() => [])
      ]);
      setDashboard(dashboardData);
      setRecentMessages(messages);
      setAppConstants(Array.isArray(nextAppConstants) ? nextAppConstants : []);
      setCurrencyRates(Array.isArray(nextCurrencies) ? nextCurrencies : []);
      setParts(asRows(nextParts));
      setPartRequests(asRows(nextPartRequests));
      setStatus("Dashboard loaded.");
    } catch (error) {
      setStatus(error.message || "Dashboard failed.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => {
    api.get("/api/customers/aging").then((data) => {
      const rows = Array.isArray(data) ? data : [];
      const total = rows.reduce((sum, row) =>
        sum + Number(row.days1To30 || 0) + Number(row.days31To60 || 0) + Number(row.days61To90 || 0) + Number(row.over90Days || 0), 0);
      setOverdueTotal(total);
    }).catch(() => setOverdueTotal(null));
  }, [api]);

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
    .slice(0, 6);
  const actionQueue = buildActionQueue({
    dashboard,
    recentMessages,
    currencyMarginRows,
    formatMoney: formatDashboardMoney
  });
  const dailyProfitLoss = dashboard
    ? dashboard.dailyProfitLoss || {
      grossSales: dashboard.todaySalesAmount,
      costOfGoodsSold: Number(dashboard.todaySalesAmount || 0) - Number(dashboard.todaySalesProfit || 0),
      grossProfit: dashboard.todaySalesProfit,
      rentExpense: 0,
      laborExpense: 0,
      otherOperatingExpenses: 0,
      totalOperatingExpenses: 0,
      netProfitLoss: dashboard.todaySalesProfit,
      expenseBreakdown: []
    }
    : null;
  const netProfitLoss = Number(dailyProfitLoss?.netProfitLoss || 0);
  const profitLossRows = dailyProfitLoss
    ? [
      { key: "grossSales", label: "Gross sales", value: dailyProfitLoss.grossSales },
      { key: "cost", label: "Cost of goods", value: signedExpense(dailyProfitLoss.costOfGoodsSold) },
      { key: "grossProfit", label: "Gross profit", value: dailyProfitLoss.grossProfit },
      { key: "rent", label: "Rent payments", value: signedExpense(dailyProfitLoss.rentExpense) },
      { key: "labor", label: "Labor payments", value: signedExpense(dailyProfitLoss.laborExpense) },
      { key: "other", label: "Other expenses", value: signedExpense(dailyProfitLoss.otherOperatingExpenses) }
    ]
    : [];
  const metrics = [
    { key: "sales", label: "Sales Today", value: dashboard?.todaySalesAmount, view: "invoices", action: "Open sales" },
    { key: "profit", label: "Profit Today", value: dashboard?.todaySalesProfit, view: "invoices", action: "Review invoices" },
    { key: "cash", label: "Cash Balance", value: dashboard?.cashBalance, view: "accounting", action: "Open accounting" },
    { key: "supplierDebt", label: "Supplier Debt", value: dashboard?.supplierDebt, view: "accounting", action: "Review payables" },
    { key: "customerDebt", label: "Customer Debt", value: dashboard?.customerDebt, view: "accounting", action: "Review receivables" },
    { key: "stock", label: "Stock Value", value: dashboard?.stockValue, view: "stock", action: "Open stock" }
  ];
  const overdueCard = overdueTotal !== null
    ? { key: "overdue", label: "Overdue Receivables", value: overdueTotal, view: "customer-aging", action: "View aging" }
    : null;
  const quickActions = [
    { key: "invoices", badge: "POS", title: "New sale", subtitle: "Create or search invoices." },
    { key: "inventory", badge: "Catalog", title: "Find parts", subtitle: "Search codes, OEM, and stock." },
    { key: "management", badge: "Web", title: "Web users", subtitle: "Manage web access, users, roles, and setup." },
    { key: "whatsapp", badge: "Chat", title: "Message customer", subtitle: "Open conversations and reminders." },
    { key: "accounting", badge: "Money", title: "Money owed", subtitle: "Review ledgers and statements." },
    { key: "report-builder", badge: "Reports", title: "Build report", subtitle: "Open saved reports and schema tools." }
  ];
  const activePartRequests = partRequests.filter(isActivePartRequest);
  const partById = new Map(parts.map((part) => [String(part.id), part]));
  const quoteReadyRequests = activePartRequests.filter((request) => {
    const part = partById.get(String(request.partId));
    return request.isReadyToContact || Number(part?.availableQuantity || request.availableQuantity || 0) > 0;
  });
  const sourceGapRequests = activePartRequests.filter((request) => {
    const part = partById.get(String(request.partId));
    return !part || Number(part.availableQuantity || request.availableQuantity || 0) <= 0;
  });
  const inventorySnapshot = {
    total: parts.length,
    available: parts.filter((part) => Number(part.availableQuantity || 0) > 0).length,
    reserved: parts.filter((part) => Number(part.reservedQuantity || 0) > 0).length,
    lowStock: parts.filter((part) => Number(part.availableQuantity || 0) <= Number(part.minStock || 0)).length,
    donorParts: parts.filter((part) => part.usedCarId).length,
    newRequests: activePartRequests.length,
    quoteReady: quoteReadyRequests.length,
    sourceGaps: sourceGapRequests.length,
    marginWatch: parts.filter(marginWatchPart).length
  };
  const inventorySnapshotCards = [
    { key: "total", label: "Total parts", value: inventorySnapshot.total, view: "inventory" },
    { key: "available", label: "Available now", value: inventorySnapshot.available, view: "inventory" },
    { key: "reserved", label: "Reserved", value: inventorySnapshot.reserved, view: "part-requests" },
    { key: "lowStock", label: "Low stock", value: inventorySnapshot.lowStock, view: "reorder" },
    { key: "donorParts", label: "Donor parts", value: inventorySnapshot.donorParts, view: "used-cars" },
    { key: "newRequests", label: "Active requests", value: inventorySnapshot.newRequests, view: "part-requests" },
    { key: "quoteReady", label: "Quote ready", value: inventorySnapshot.quoteReady, view: "inventory" },
    { key: "sourceGaps", label: "Source gaps", value: inventorySnapshot.sourceGaps, view: "part-requests" },
    { key: "marginWatch", label: "Margin watch", value: inventorySnapshot.marginWatch, view: "inventory" }
  ];

  // ===== Cockpit dashboard view (approved design baseline) =====
  const displayName = user?.fullName || user?.name || user?.username || "Administrator";
  const weekly = (() => {
    const src = dashboard?.salesByDay || dashboard?.weeklySales || dashboard?.salesTrend;
    if (Array.isArray(src) && src.length) {
      return src.slice(-7).map((point) => Number(point?.amount ?? point?.value ?? point ?? 0));
    }
    const base = Number(dashboard?.todaySalesAmount || 0);
    const scale = base > 0 ? base : 1;
    return [0.62, 0.78, 0.70, 0.84, 0.79, 1.0, 0.9].map((factor) => Math.round(scale * factor));
  })();
  const weekTotal = weekly.reduce((sum, value) => sum + value, 0);
  const peakIndex = weekly.reduce((best, value, index) => (value > weekly[best] ? index : best), 0);
  const dayNames = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
  const chart = ckLinePath(weekly, 480, 168);
  const axisMax = Math.max(...weekly, 1);

  const categoryTotals = new Map();
  parts.forEach((part) => {
    const name = part.categoryName || part.category || part.partType || "Other";
    const value = Number(part.salePrice || 0) * Math.max(1, Number(part.availableQuantity || 0));
    categoryTotals.set(name, (categoryTotals.get(name) || 0) + value);
  });
  const catColors = ["#ff8a3d", "#4d8dff", "#b768ff", "#2bdb8f", "#ffc257", "#8c8c9c"];
  const catIcons = ["cog", "car", "tag", "link", "bolt", "box"];
  const topCategories = [...categoryTotals.entries()].sort((a, b) => b[1] - a[1]).slice(0, 6);
  const catMax = topCategories.length ? topCategories[0][1] : 1;

  const hotParts = parts
    .slice()
    .sort((a, b) => Number(b.availableQuantity || 0) - Number(a.availableQuantity || 0))
    .slice(0, 5);

  const activity = (recentMessages.length
    ? recentMessages.slice(0, 4).map((message, index) => ({
      key: message.id || `msg-${index}`,
      icon: "cart",
      tone: "rgba(77,140,255,.14)",
      color: "#4d8dff",
      title: message.subject || message.title || "Communication",
      sub: message.customerName || message.recipient || message.channel || "Customer",
      time: message.status || ""
    }))
    : activePartRequests.slice(0, 4).map((request, index) => ({
      key: request.id || `req-${index}`,
      icon: "clipboard",
      tone: "rgba(183,104,255,.14)",
      color: "#b768ff",
      title: "Part request",
      sub: request.requestedPartName || request.partName || "Requested part",
      time: request.status || ""
    })));

  const inv = inventorySnapshot;
  const invTotal = Math.max(1, inv.total);
  const otherCount = Math.max(0, inv.total - inv.available - inv.lowStock - inv.reserved - inv.donorParts);
  const donutSegments = [
    { key: "available", label: "Available", value: inv.available, color: "#2bdb8f" },
    { key: "lowStock", label: "Low Stock", value: inv.lowStock, color: "#ff5b5b" },
    { key: "reserved", label: "Reserved", value: inv.reserved, color: "#4d8dff" },
    { key: "donor", label: "Donor Parts", value: inv.donorParts, color: "#b768ff" },
    { key: "other", label: "Other", value: otherCount, color: "#5d5d6d" }
  ];
  let donutOffset = 25;
  const donutCircles = donutSegments.map((segment) => {
    const dash = (segment.value / invTotal) * 100;
    const circle = h("circle", {
      key: segment.key, cx: 21, cy: 21, r: 15.9, fill: "transparent",
      stroke: segment.color, strokeWidth: 6,
      strokeDasharray: `${dash.toFixed(2)} ${(100 - dash).toFixed(2)}`,
      strokeDashoffset: donutOffset.toFixed(2), transform: "rotate(-90 21 21)"
    });
    donutOffset -= dash;
    return circle;
  });

  const kpis = [
    { label: "Sales Today", value: formatDashboardMoney(dashboard?.todaySalesAmount), bg: "rgba(255,60,60,.15)", color: "#ff5b5b", icon: "dollar", spark: "#ff5b5b", delta: ckPct(dashboard?.salesChangePercent), view: "invoices" },
    { label: "Profit Today", value: formatDashboardMoney(dashboard?.todaySalesProfit), bg: "rgba(255,160,60,.15)", color: "#ffa23c", icon: "trend", spark: "#2bdb8f", delta: ckPct(dashboard?.profitChangePercent), view: "invoices" },
    { label: "Cash Balance", value: formatDashboardMoney(dashboard?.cashBalance), bg: "rgba(77,140,255,.15)", color: "#4d8dff", icon: "wallet", spark: "#4d8dff", delta: ckPct(dashboard?.cashChangePercent), view: "accounting" },
    { label: "Active Requests", value: ckNum(activePartRequests.length), bg: "rgba(183,104,255,.15)", color: "#b768ff", icon: "inbox", spark: "#b768ff", delta: null, view: "part-requests" },
    { label: "Available Parts", value: ckNum(inv.available), bg: "rgba(52,217,212,.15)", color: "#34d9d4", icon: "pkg", spark: "#34d9d4", delta: null, view: "inventory" },
    { label: "Low Stock Items", value: ckNum(inv.lowStock), bg: "rgba(255,60,60,.15)", color: "#ff5b5b", icon: "warn", spark: "#ff5b5b", delta: null, view: "reorder" },
    { label: "Donor Parts", value: ckNum(inv.donorParts), bg: "rgba(255,194,87,.15)", color: "#ffc257", icon: "car", spark: "#ffc257", delta: null, view: "used-cars" },
    { label: "Reserved Items", value: ckNum(inv.reserved), bg: "rgba(183,104,255,.15)", color: "#b768ff", icon: "bookmark", spark: "#b768ff", delta: null, view: "part-requests" }
  ];

  const tabs = ["Parts", "OEM Code", "Barcode", "Vehicle", "Customer", "Supplier", "Invoice", "Donor Car", "Compatibility"];
  const quick = [
    { label: "New Sale", icon: "cart", grad: "linear-gradient(150deg,#ff7a44,#ff3b3b)", view: "invoices" },
    { label: "Find Parts", icon: "search", grad: "linear-gradient(150deg,#ffa23c,#ff6a2c)", view: "inventory" },
    { label: "Part Request", icon: "clipboard", grad: "linear-gradient(150deg,#4d8dff,#2f6ad6)", view: "part-requests" },
    { label: "Compatibility", icon: "link", grad: "linear-gradient(150deg,#2bdb8f,#1aa86c)", view: "compatibility" },
    { label: "Contacts", icon: "users", grad: "linear-gradient(150deg,#b768ff,#8a3fe0)", view: "contacts" },
    { label: "Stock", icon: "warehouse", grad: "linear-gradient(150deg,#34d9d4,#1aa8a4)", view: "stock" },
    { label: "Reports", icon: "trend", grad: "linear-gradient(150deg,#ffc257,#e89a26)", view: "report-builder" },
    { label: "Settings", icon: "cog", grad: "linear-gradient(150deg,#8c8c9c,#5d5d6d)", view: "settings" }
  ];

  const bottomStats = [
    { label: "Sales Today", value: formatDashboardMoney(dashboard?.todaySalesAmount), color: "#ff5b5b", icon: "dollar" },
    { label: "Profit Today", value: formatDashboardMoney(dashboard?.todaySalesProfit), color: "#2bdb8f", icon: "trend" },
    { label: "Cash Balance", value: formatDashboardMoney(dashboard?.cashBalance), color: "#4d8dff", icon: "wallet" },
    { label: "Supplier Debt", value: formatDashboardMoney(dashboard?.supplierDebt), color: "#b768ff", icon: "bank" },
    { label: "Customer Debt", value: formatDashboardMoney(dashboard?.customerDebt), color: "#ff5b5b", icon: "doc" },
    { label: "Stock Value", value: formatDashboardMoney(dashboard?.stockValue), color: "#ffc257", icon: "box" },
    { label: "Overdue", value: overdueTotal !== null ? formatDashboardMoney(overdueTotal) : "--", color: "#34d9d4", icon: "warn" },
    { label: "Low Stock", value: ckNum(inv.lowStock), color: "#2bdb8f", icon: "pkg" }
  ];

  return h("div", { className: "ck-dash" },
    h("div", { className: "ck-page-head" },
      h("div", null,
        h("h1", null, "Welcome back, ", h("span", null, displayName)),
        h("p", null, "Here's what's happening with your business today.")
      ),
      h("div", { className: "ck-head-filters" },
        h("button", { type: "button", className: "ck-filter-pill", onClick: () => navigate("stock") },
          h(Icon, { name: "warehouse", size: 14 }), "All Warehouses", h(Icon, { name: "chevron", size: 11 })),
        h("button", { type: "button", className: "ck-filter-pill", onClick: load, disabled: isLoading },
          h(Icon, { name: "cal", size: 14 }), isLoading ? "Refreshing..." : "Refresh", h(Icon, { name: "chevron", size: 11 })),
        h("span", { className: "ck-live-pill" }, h("span", { className: "ck-live-dot" }), status || "Live")
      )
    ),

    // KPI ROW
    h("div", { className: "ck-kpi-row" },
      kpis.map((kpi) =>
        h("button", { key: kpi.label, type: "button", className: "ck-kpi-card", onClick: () => navigate(kpi.view) },
          h("div", { className: "ck-kpi-ic", style: { background: kpi.bg, color: kpi.color } }, h(Icon, { name: kpi.icon, size: 15 })),
          h("div", { className: "ck-kpi-label" }, kpi.label),
          h("div", { className: "ck-kpi-val" }, kpi.value),
          kpi.delta && h("div", { className: `ck-kpi-delta ${kpi.delta.dir === "down" ? "ck-down" : "ck-up"}` },
            h(Icon, { name: kpi.delta.dir === "down" ? "arr-down" : "arr-up", size: 10 }), kpi.delta.text, h("i", null, "vs yesterday")),
          ckSpark(kpi.spark)
        )
      )
    ),

    // COMMAND CENTER
    h("div", { className: "ck-cmd-panel" },
      h("div", { className: "ck-cmd-layout" },
        h("div", { className: "ck-cmd-left" },
          h("div", { className: "ck-cmd-title" }, h(Icon, { name: "bolt", size: 16 }), "Smart Search / Command Center"),
          h("div", { className: "ck-cmd-tabs" }, tabs.map((tab, index) =>
            h("button", { key: tab, type: "button", className: index === 0 ? "ck-cmd-tab is-active" : "ck-cmd-tab", onClick: () => navigate("inventory") }, tab))),
          h("div", { className: "ck-cmd-search" },
            h("input", { className: "ck-cmd-input", placeholder: "Search by part name, OEM code, barcode, or fitment (e.g. BMW N54 ignition coil)", onKeyDown: (event) => { if (event.key === "Enter") navigate("inventory"); } }),
            h("button", { type: "button", className: "ck-cmd-btn", onClick: () => navigate("inventory") }, h(Icon, { name: "search", size: 15 }), "Search")
          ),
          h("div", { className: "ck-popular" },
            h("span", { className: "ck-lbl" }, "Popular searches:"),
            (hotParts.length ? hotParts.map((part) => part.name).filter(Boolean).slice(0, 5) : ["N54 Ignition Coil", "W205 Brake Pads", "D58 Service Kit"]).map((term) =>
              h("button", { key: term, type: "button", className: "ck-chip", onClick: () => navigate("inventory") }, term))
          )
        ),
        h("div", { className: "ck-cmd-help" },
          h("div", { className: "ck-cmd-help-img" },
            h(Icon, { name: "disc", size: 70, className: "ck-icn ck-p1" }),
            h(Icon, { name: "piston", size: 54, className: "ck-icn ck-p2" }),
            h(Icon, { name: "cog", size: 46, className: "ck-icn ck-p3" })
          ),
          h("button", { type: "button", className: "ck-cmd-cta", onClick: () => navigate("compatibility") },
            h("div", { className: "ck-cta-ic" }, h(Icon, { name: "link", size: 18 })),
            h("div", { className: "ck-cta-txt" }, h("b", null, "Need help finding the right part?"), h("span", null, "Use Compatibility Search")),
            h(Icon, { name: "chevron-r", size: 16, className: "ck-icn ck-cta-arrow" })
          )
        )
      )
    ),

    // GRID 4
    h("div", { className: "ck-grid-4" },
      // Inventory Snapshot
      h("div", { className: "ck-panel" },
        h("div", { className: "ck-panel-head" }, h("h3", null, "Inventory Snapshot"), h("span", { className: "ck-sub" }, `${ckNum(inv.total)} total`)),
        h("div", { className: "ck-donut-wrap" },
          h("svg", { width: 112, height: 112, viewBox: "0 0 42 42" },
            h("circle", { cx: 21, cy: 21, r: 15.9, fill: "transparent", stroke: "#1a1a20", strokeWidth: 6 }),
            donutCircles,
            h("text", { x: 21, y: 19.5, textAnchor: "middle", fill: "#fff", fontSize: 7, fontWeight: 800 }, ckNum(inv.total)),
            h("text", { x: 21, y: 26, textAnchor: "middle", fill: "#5d5d6d", fontSize: 3.4 }, "Total Parts")
          ),
          h("div", { style: { flex: 1, minWidth: "120px" } },
            donutSegments.map((segment) =>
              h("div", { key: segment.key, className: "ck-legend-row" },
                h("span", null, h("span", { className: "ck-legend-dot", style: { background: segment.color, color: segment.color } }), segment.label),
                h("b", null, `${ckNum(segment.value)} · ${Math.round((segment.value / invTotal) * 100)}%`))
            )
          )
        )
      ),
      // Top Categories
      h("div", { className: "ck-panel" },
        h("div", { className: "ck-panel-head" }, h("h3", null, "Top Categories"), h("span", { className: "ck-sub" }, "By value")),
        topCategories.length ? topCategories.map(([name, value], index) =>
          h("div", { className: "ck-cat-row", key: name },
            h("div", { className: "ck-top" },
              h("span", { className: "ck-nm" }, h(Icon, { name: catIcons[index % catIcons.length], size: 12, className: "ck-icn" }), name),
              h("span", { className: "ck-val" }, formatDashboardMoney(value))),
            h("div", { className: "ck-bar-track" }, h("div", { className: "ck-bar-fill", style: { width: `${Math.max(6, (value / catMax) * 100)}%`, background: `linear-gradient(90deg,${catColors[index % catColors.length]},#ff8a3d)`, color: catColors[index % catColors.length] } })))
        ) : h("p", { className: "ck-empty" }, "No category data yet.")
      ),
      // Sales Overview
      h("div", { className: "ck-panel" },
        h("div", { className: "ck-panel-head" }, h("h3", null, "Sales Overview"), h("span", { className: "ck-sub" }, "This Week")),
        h("div", null, h("span", { className: "ck-metric-big" }, formatDashboardMoney(weekTotal))),
        h("div", { className: "ck-metric-sub" }, `Peak ${dayNames[peakIndex]} · ${formatDashboardMoney(weekly[peakIndex])}`),
        h("div", { className: "ck-chart-card" },
          h("div", { className: "ck-chart-body" },
            h("div", { className: "ck-y-axis" }, [1, 0.75, 0.5, 0.25, 0].map((factor) =>
              h("span", { key: factor }, ckCompact(axisMax * factor)))),
            h("div", { className: "ck-chart-plot" },
              h("svg", { width: "100%", height: 168, viewBox: "0 0 480 168", preserveAspectRatio: "none" },
                h("defs", null,
                  h("linearGradient", { id: "ckArea", x1: 0, y1: 0, x2: 0, y2: 1 },
                    h("stop", { offset: "0%", stopColor: "#ff5b5b", stopOpacity: 0.45 }),
                    h("stop", { offset: "100%", stopColor: "#ff7a44", stopOpacity: 0 })),
                  h("linearGradient", { id: "ckStroke", x1: 0, y1: 0, x2: 1, y2: 0 },
                    h("stop", { offset: "0%", stopColor: "#ff3b3b" }), h("stop", { offset: "100%", stopColor: "#ff8a3d" }))),
                [0.25, 0.5, 0.75].map((factor) => h("line", { key: factor, x1: 0, y1: 168 * factor, x2: 480, y2: 168 * factor, stroke: "#1b1b21" })),
                h("path", { d: chart.area, fill: "url(#ckArea)" }),
                h("path", { d: chart.line, fill: "none", stroke: "url(#ckStroke)", strokeWidth: 3.2, strokeLinecap: "round", strokeLinejoin: "round" }),
                chart.points.map((point, index) => h("circle", { key: index, cx: point.x, cy: point.y, r: index === peakIndex ? 4.5 : 2.4, fill: "#0c0c0e", stroke: index === peakIndex ? "#ff8a3d" : "#ff6a3d", strokeWidth: index === peakIndex ? 2.4 : 1.6 }))
              ),
              chart.points[peakIndex] && h("div", { className: "ck-tooltip-box", style: { left: `${(chart.points[peakIndex].x / 480) * 100}%`, top: "8px", transform: "translateX(-50%)" } },
                h("div", { className: "ck-tt-day" }, dayNames[peakIndex]),
                h("div", { className: "ck-tt-val" }, formatDashboardMoney(weekly[peakIndex])))
            )
          ),
          h("div", { className: "ck-axis-x", style: { paddingLeft: "52px" } }, dayNames.map((day) => h("span", { key: day }, day)))
        )
      ),
      // Recent Activity
      h("div", { className: "ck-panel" },
        h("div", { className: "ck-panel-head" }, h("h3", null, "Recent Activity"), h("button", { type: "button", className: "ck-view-all", onClick: () => navigate("activity-log") }, "View all")),
        activity.length ? activity.map((item) =>
          h("div", { className: "ck-act-item", key: item.key },
            h("div", { className: "ck-act-ic", style: { background: item.tone, color: item.color } }, h(Icon, { name: item.icon, size: 14 })),
            h("div", null, h("div", { className: "ck-act-title" }, item.title), h("div", { className: "ck-act-sub" }, item.sub)),
            item.time && h("div", { className: "ck-act-time" }, item.time))
        ) : h("p", { className: "ck-empty" }, "No recent activity.")
      )
    ),

    // HOT PARTS + QUICK ACTIONS
    h("div", { className: "ck-grid-hot" },
      h("div", { className: "ck-panel" },
        h("div", { className: "ck-panel-head" }, h("h3", null, "Hot Parts"), h("button", { type: "button", className: "ck-view-all", onClick: () => navigate("inventory") }, "View all")),
        hotParts.length ? h("div", { className: "ck-hot-row" }, hotParts.map((part, index) => {
          const stock = Number(part.availableQuantity || 0);
          const fitment = [part.make || part.vehicleMake, part.model || part.vehicleModel].filter(Boolean).join(" / ") || part.compatibility || "Universal";
          const oem = part.oemCode || part.partNumber || part.sku || part.barcode || "--";
          return h("button", { key: part.id || index, type: "button", className: "ck-hot-card", onClick: () => navigate("inventory") },
            h("div", { className: "ck-hot-img" },
              h("span", { className: "ck-bookmark" }, h(Icon, { name: "bookmark", size: 13 })),
              part.imageUrl ? h("img", { src: part.imageUrl, alt: part.name || "Part" }) : h(Icon, { name: catIcons[index % catIcons.length], size: 30 })),
            h("div", { className: "ck-hot-body" },
              h("div", { className: "ck-hot-name" }, part.name || "Part"),
              h("div", { className: "ck-hot-fit" }, fitment),
              h("div", { className: "ck-hot-oem" }, `OEM ${oem}`),
              h("div", { className: "ck-hot-foot" },
                h("span", { className: stock <= Number(part.minStock || 0) ? "ck-hot-stock is-low" : "ck-hot-stock" }, `${ckNum(stock)} in stock`),
                h("span", { className: "ck-hot-price" }, formatDashboardMoney(part.salePrice)))));
        })) : h("p", { className: "ck-empty" }, "No parts available yet.")
      ),
      h("div", { className: "ck-panel" },
        h("div", { className: "ck-panel-head" }, h("h3", null, "Quick Actions")),
        h("div", { className: "ck-qa-grid" }, quick.map((action) =>
          h("button", { key: action.label, type: "button", className: "ck-qa-btn", onClick: () => navigate(action.view) },
            h("div", { className: "ck-qa-ic", style: { background: action.grad } }, h(Icon, { name: action.icon, size: 15 })), action.label)))
      )
    ),

    // BOTTOM STAT BAR
    h("div", { className: "ck-bottom-bar" }, bottomStats.map((stat) =>
      h("div", { className: "ck-stat", key: stat.label },
        h(Icon, { name: stat.icon, size: 13, className: "ck-icn" }), h("b", { style: { color: stat.color } }, stat.value), stat.label)))
  );
}

// ===== Cockpit render helpers (module scope, hoisted) =====
function ckNum(value) {
  const number = Number(value || 0);
  return Number.isFinite(number) ? number.toLocaleString(undefined, { maximumFractionDigits: 0 }) : "0";
}

function ckPct(value) {
  if (value === null || value === undefined || value === "" || Number.isNaN(Number(value))) return null;
  const number = Number(value);
  return { text: `${Math.abs(number).toFixed(1)}%`, dir: number < 0 ? "down" : "up" };
}

function ckCompact(value) {
  const number = Number(value || 0);
  if (number >= 1000) return `${Math.round(number / 1000)}K`;
  return String(Math.round(number));
}

function ckSpark(color) {
  return h("svg", { className: "ck-spark", viewBox: "0 0 90 22", preserveAspectRatio: "none", style: { color } },
    h("polyline", { points: "0,16 12,18 24,9 36,13 48,5 60,11 72,4 90,8", fill: "none", stroke: color, strokeWidth: 2 }));
}

function ckLinePath(series, width, height) {
  const max = Math.max(...series, 1);
  const pad = 14;
  const step = series.length > 1 ? width / (series.length - 1) : width;
  const points = series.map((value, index) => ({
    x: Math.round(index * step),
    y: Math.round(height - pad - (value / max) * (height - pad * 2))
  }));
  const line = points.map((point, index) => `${index === 0 ? "M" : "L"}${point.x},${point.y}`).join(" ");
  const area = `${line} L${points[points.length - 1].x},${height} L${points[0].x},${height} Z`;
  return { line, area, points };
}

