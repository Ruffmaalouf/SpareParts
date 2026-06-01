import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { displayCurrencyContext, displayMoneyFromCounter } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

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

export function DashboardView({ api, onView }) {
  const [dashboard, setDashboard] = useState(null);
  const [recentMessages, setRecentMessages] = useState([]);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const navigate = useCallback((nextView) => {
    if (typeof onView === "function") {
      onView(nextView);
    }
  }, [onView]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading dashboard...");
    try {
      const [dashboardData, messages, nextAppConstants, nextCurrencies] = await Promise.all([
        api.get("/api/owner-cockpit"),
        api.get("/api/communications/recent?take=6"),
        api.get("/api/appconstants"),
        api.get("/api/currencies")
      ]);
      setDashboard(dashboardData);
      setRecentMessages(messages);
      setAppConstants(Array.isArray(nextAppConstants) ? nextAppConstants : []);
      setCurrencyRates(Array.isArray(nextCurrencies) ? nextCurrencies : []);
      setStatus("Dashboard loaded.");
    } catch (error) {
      setStatus(error.message || "Dashboard failed.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

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
    .slice(0, 6);
  const metrics = [
    { key: "sales", label: "Sales Today", value: dashboard?.todaySalesAmount, view: "invoices", action: "Open sales" },
    { key: "profit", label: "Profit Today", value: dashboard?.todaySalesProfit, view: "invoices", action: "Review invoices" },
    { key: "cash", label: "Cash Balance", value: dashboard?.cashBalance, view: "accounting", action: "Open accounting" },
    { key: "supplierDebt", label: "Supplier Debt", value: dashboard?.supplierDebt, view: "accounting", action: "Review payables" },
    { key: "customerDebt", label: "Customer Debt", value: dashboard?.customerDebt, view: "accounting", action: "Review receivables" },
    { key: "stock", label: "Stock Value", value: dashboard?.stockValue, view: "stock", action: "Open stock" }
  ];
  const quickActions = [
    { key: "invoices", badge: "POS", title: "New sale", subtitle: "Create or search invoices." },
    { key: "inventory", badge: "Catalog", title: "Find parts", subtitle: "Search codes, OEM, and stock." },
    { key: "management", badge: "Web", title: "Web users", subtitle: "Manage web access, users, roles, and setup." },
    { key: "whatsapp", badge: "Chat", title: "Message customer", subtitle: "Open conversations and reminders." },
    { key: "accounting", badge: "Money", title: "Money owed", subtitle: "Review ledgers and statements." },
    { key: "report-builder", badge: "Reports", title: "Build report", subtitle: "Open saved reports and schema tools." }
  ];

  return h("section", { className: "screen dashboard-screen" },
    h(PageHeader, {
      title: "Operations Dashboard",
      kicker: "Admin console",
      subtitle: "Sales, cash, stock, communication, and report shortcuts in one scan.",
      action: h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h(StatusLine, { status }),
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
    h("div", { className: "metric-grid" },
      metrics.map((metric) =>
        h("button", {
          className: `metric clickable-metric metric-${metric.key}`,
          key: metric.key,
          style: { "--metric-level": `${metricLevel(metric.value)}%` },
          type: "button",
          onClick: () => navigate(metric.view)
        },
          h("i", { className: "metric-visual", "aria-hidden": "true" }, h("span", null)),
          h("span", null, metric.label),
          h("strong", null, formatDashboardMoney(metric.value)),
          h("em", null, metric.action)
        )
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
        h("div", { className: "dense-list" },
          unpaidTransactions.slice(0, 8).map((item, index) => {
            const title = item.transactionNumber || item.referenceNumber || "Transaction";
            const subtitle = item.counterparty || item.partnerName || item.partner || item.transactionType || "";
            const amount = item.remainingAmount ?? item.balance ?? item.amount ?? item.totalAmount;

            return h("button", {
              className: "list-row action-row",
              key: `${title}-${index}`,
              type: "button",
              onClick: () => navigate("accounting")
            },
              h("i", { className: "row-marker", "aria-hidden": "true" }),
              h("div", null, h("strong", null, title), h("span", null, subtitle)),
              h("b", null, formatDashboardMoney(amount))
            );
          }),
          unpaidTransactions.length === 0 && h("p", { className: "empty-state" }, "No unpaid transactions returned.")
        )
      ),
      h("section", { className: "panel" },
        h("h3", null, "Recent Communications"),
        h("div", { className: "dense-list" },
          recentMessages.map((message) =>
            h("button", {
              className: "list-row action-row",
              key: message.id,
              type: "button",
              onClick: () => navigate("whatsapp")
            },
              h("i", { className: "row-marker success", "aria-hidden": "true" }),
              h("div", null,
                h("strong", null, message.recipientName || message.recipientPhone),
                h("span", null, `${message.channel} · ${message.templateKey}`)
              ),
              h("b", { className: message.status === "Failed" ? "danger-text" : "success-text" }, message.status)
            )
          ),
          recentMessages.length === 0 && h("p", { className: "empty-state" }, "No messages yet.")
        )
      )
    )
  );
}
