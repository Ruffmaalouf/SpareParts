import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { asRows, displayCurrencyContext, displayMoneyFromCounter } from "../core/formatters.js";
import { StatusLine } from "../components/shared.js";
import { PageHeader } from "../components/ui/PageHeader.js";
import { StatsCard } from "../components/ui/StatsCard.js";
import { EmptyState } from "../components/ui/EmptyState.js";
import { DataTable } from "../components/ui/DataTable.js";

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

export function DashboardView({ api, onView }) {
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

  return h("section", { className: "screen dashboard-screen" },
    h(PageHeader, {
      title: "Operations Dashboard",
      kicker: "Admin console",
      subtitle: "Sales, cash, stock, communication, and report shortcuts in one scan.",
      action: h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh"),
      stats: [
        { key: "sales", label: "Sales today", value: formatDashboardMoney(dashboard?.todaySalesAmount) },
        { key: "profit", label: "Profit today", value: formatDashboardMoney(dashboard?.todaySalesProfit) },
        { key: "cash", label: "Cash balance", value: formatDashboardMoney(dashboard?.cashBalance) }
      ]
    }),
    h(StatusLine, { status }),
    h("section", { className: "shop-floor-snapshot" },
      h("div", { className: "panel-heading-row" },
        h("div", null,
          h("h3", null, "Shop Floor Snapshot"),
          h("span", null, "Parts, reservations, low-stock pressure, donor inventory, and new customer demand")
        ),
        h("button", { className: "secondary-button", type: "button", onClick: () => navigate("inventory") }, "Open inventory")
      ),
      h("div", { className: "shop-floor-grid stats-card-grid" },
        inventorySnapshotCards.map((card) =>
          h(StatsCard, {
            key: card.key,
            label: card.label,
            value: Number(card.value || 0).toLocaleString(),
            onClick: () => navigate(card.view)
          })
        )
      )
    ),
    h("section", { className: "admin-action-strip", "aria-label": "Admin shortcuts" },
      quickActions.map((action) =>
        h("button", {
          key: action.key,
          className: action.key === "management" ? `admin-action-card action-${action.key} featured` : `admin-action-card action-${action.key}`,
          type: "button",
          onClick: () => navigate(action.key)
        },
          h("i", { className: "action-visual", "aria-hidden": "true" }),
          h("span", null, action.badge),
          h("strong", null, action.title),
          h("small", null, action.subtitle)
        )
      )
    ),
    h("div", { className: "metric-grid stats-card-grid" },
      metrics.map((metric) =>
        h(StatsCard, {
          key: metric.key,
          label: metric.label,
          value: formatDashboardMoney(metric.value),
          trend: { label: metric.action, delta: metricLevel(metric.value) },
          onClick: () => navigate(metric.view)
        })
      ),
      overdueCard && h(StatsCard, {
        key: "overdue",
        label: overdueCard.label,
        value: formatDashboardMoney(overdueCard.value),
        tone: overdueCard.value > 0 ? "danger" : "neutral",
        trend: { label: overdueCard.action, delta: metricLevel(overdueCard.value) },
        onClick: () => navigate(overdueCard.view)
      })
    ),
    dailyProfitLoss && h("section", { className: `panel profit-loss-panel ${netProfitLoss < 0 ? "is-loss" : "is-profit"}` },
      h("div", { className: "panel-heading-row" },
        h("div", null,
          h("h3", null, "Daily Profit & Loss"),
          h("span", null, "Sales after stock cost, rent, labor, and operating expenses")
        ),
        h("button", { className: "secondary-button", type: "button", onClick: () => navigate("accounting") }, "Post expense")
      ),
      h("div", { className: "profit-loss-layout" },
        h("div", { className: "profit-loss-net" },
          h("span", null, "Net P&L"),
          h("strong", { className: profitLossValueClass(netProfitLoss) }, formatDashboardMoney(netProfitLoss)),
          h("em", null, netProfitLoss < 0 ? "Loss today" : "Profit today")
        ),
        h("div", { className: "profit-loss-lines" },
          profitLossRows.map((row) =>
            h("div", { className: "profit-loss-line", key: row.key },
              h("span", null, row.label),
              h("strong", { className: profitLossValueClass(row.value) }, formatDashboardMoney(row.value))
            )
          )
        )
      ),
      (dailyProfitLoss.expenseBreakdown || []).length > 0 && h("div", { className: "profit-loss-breakdown" },
        (dailyProfitLoss.expenseBreakdown || []).slice(0, 4).map((row, index) =>
          h("span", { key: `${row.category}-${row.accountCode || index}` },
            h("b", null, row.category),
            `${row.accountCode ? ` ${row.accountCode}` : ""} ${row.accountName || ""}`,
            h("strong", null, formatDashboardMoney(signedExpense(row.amount)))
          )
        )
      )
    ),
    h("section", { className: "panel action-queue-panel" },
      h("div", { className: "panel-heading-row" },
        h("div", null,
          h("h3", null, "Today's Action Queue"),
          h("span", null, "Priority work assembled from live dashboard signals")
        ),
        h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, "Update queue")
      ),
      h("div", { className: "action-queue-grid" },
        actionQueue.map((task, index) =>
          h("button", {
            className: `action-queue-card tone-${task.tone}`,
            key: task.key,
            type: "button",
            onClick: () => navigate(task.view)
          },
            h("span", { className: "queue-rank" }, String(index + 1).padStart(2, "0")),
            h("span", { className: "queue-copy" },
              h("em", null, task.label),
              h("strong", null, task.title),
              h("small", null, task.detail)
            ),
            h("b", null, task.value)
          )
        ),
        actionQueue.length === 0 && h("p", { className: "empty-state" }, "Load the dashboard to assemble action items.")
      )
    ),
    h("section", { className: "panel heatmap-panel" },
      h("div", { className: "panel-heading-row" },
        h("div", null,
          h("h3", null, "Live Profit Heatmap"),
          h("span", null, "30-day margin, 30-day turnover, 90-day dead stock")
        ),
        h("div", { className: "heatmap-legend", "aria-label": "Heatmap legend" },
          h("span", { className: "heatmap-dot red", "aria-hidden": "true" }),
          h("span", null, "Red"),
          h("span", { className: "heatmap-dot yellow", "aria-hidden": "true" }),
          h("span", null, "Yellow"),
          h("span", { className: "heatmap-dot green", "aria-hidden": "true" }),
          h("span", null, "Green")
        )
      ),
      h("div", { className: "profit-heatmap-grid" },
        profitHeatmap.map((row) =>
          h("button", {
            key: row.segmentKey || row.categoryName,
            className: `heatmap-tile heatmap-${signalClass(row.overallSignal)}`,
            type: "button",
            onClick: () => navigate("stock")
          },
            h("span", { className: "heatmap-tile-topline" },
              h("strong", null, row.categoryName || "Category"),
              h("b", null, `${Number(row.score || 0)}/100`)
            ),
            h("span", { className: "heatmap-signal-row" },
              h("span", { className: `heatmap-signal ${signalClass(row.profitSignal)}` }, "Profit"),
              h("span", { className: `heatmap-signal ${signalClass(row.turnoverSignal)}` }, "Turnover"),
              h("span", { className: `heatmap-signal ${signalClass(row.deadStockSignal)}` }, "Dead stock")
            ),
            h("span", { className: "heatmap-metric-row" },
              h("span", null, h("small", null, "Profit"), h("b", null, formatDashboardMoney(row.profit)), h("em", null, percent(row.profitMarginPercent))),
              h("span", null, h("small", null, "Turnover"), h("b", null, units(row.turnoverUnits)), h("em", null, percent(row.turnoverRatePercent))),
              h("span", null, h("small", null, "Dead"), h("b", null, units(row.deadStockUnits)), h("em", null, percent(row.deadStockPercent)))
            ),
            h("span", { className: "heatmap-stock-line" },
              `${units(row.stockUnits)} on hand | ${formatDashboardMoney(row.deadStockValue)} dead value`
            )
          )
        ),
        profitHeatmap.length === 0 && h("p", { className: "empty-state" }, "No category heatmap data yet.")
      )
    ),
    h("section", { className: "panel" },
      h("h3", null, "Currency Margin Watch"),
      h("div", { className: "dense-list" },
        currencyMarginRows.map((row, index) =>
          h("button", {
            className: "list-row action-row",
            key: `${row.name}-${index}`,
            type: "button",
            onClick: () => navigate("invoices")
          },
            h("i", { className: row.currencyMovementEatsProfit ? "row-marker" : "row-marker success", "aria-hidden": "true" }),
            h("div", null,
              h("strong", null, row.name),
              h("span", null, row.currencyWarning || "Currency movement reduced margin")
            ),
            h("b", { className: row.currencyMovementEatsProfit ? "danger-text" : "" },
              `${formatDashboardMoney(row.marginAtPurchaseRate)} → ${formatDashboardMoney(row.marginAtCurrentRate)}`)
          )
        ),
        currencyMarginRows.length === 0 && h("p", { className: "empty-state" }, "No currency margin warnings.")
      )
    ),
    h("div", { className: "two-column" },
      h("section", { className: "panel" },
        h("h3", null, "Unpaid Transactions"),
        h(DataTable, {
          columns: [
            {
              key: "title",
              label: "Transaction",
              render: (item) => h("div", null,
                h("strong", null, item.transactionNumber || item.referenceNumber || "Transaction"),
                h("span", null, item.counterparty || item.partnerName || item.partner || item.transactionType || "")
              )
            },
            { key: "amount", label: "Balance", render: (item) => h("b", null, formatDashboardMoney(transactionAmount(item))) }
          ],
          rows: unpaidTransactions.slice(0, 8),
          getRowKey: (item, index) => item.id || `${item.transactionNumber || "txn"}-${index}`,
          emptyText: "No unpaid transactions returned.",
          onRowClick: () => navigate("accounting")
        })
      ),
      h("section", { className: "panel" },
        h("h3", null, "Recent Communications"),
        h(DataTable, {
          columns: [
            {
              key: "recipient",
              label: "Recipient",
              render: (message) => h("div", null,
                h("strong", null, message.recipientName || message.recipientPhone),
                h("span", null, `${message.channel} · ${message.templateKey}`)
              )
            },
            {
              key: "status",
              label: "Status",
              render: (message) => h("b", { className: message.status === "Failed" ? "danger-text" : "success-text" }, message.status)
            }
          ],
          rows: recentMessages,
          getRowKey: (message) => message.id,
          emptyText: "No messages yet.",
          onRowClick: () => navigate("whatsapp")
        })
      )
    )
  );
}
