import { h, useCallback, useEffect, useMemo, useRef, useState } from "../core/react-runtime.js";
import { displayCurrencyContext, displayMoneyFromCounter } from "../core/formatters.js";
import { screenRegistry } from "./screen-registry.js";
import { IconRail } from "../components/ignition/IconRail.js";
import { CommandBar } from "../components/ignition/CommandBar.js";
import { CommandPalette } from "../components/ignition/CommandPalette.js";
import { KpiBand } from "../components/ignition/KpiBand.js";
import { ActionQueueCard } from "../components/ignition/ActionQueueCard.js";
import { InsightPanel } from "../components/ignition/InsightPanel.js";
import { TrendChart } from "../components/ignition/TrendChart.js";
import { DataGrid } from "../components/ignition/DataGrid.js";
import { MoneyText } from "../components/ignition/MoneyText.js";
import { QuickActionDock } from "../components/ignition/QuickActionDock.js";
import {
  getQueueItemState,
  markQueueItemActed,
  markQueueItemDismissed,
  setDraftContext
} from "../services/ignition-context.js";
import {
  configureTelemetry,
  trackQueueItem,
  trackScreenView
} from "../services/telemetry-service.js";

const POLL_INTERVAL_MS = 45000; // "Live updates" poll window (Report 04 §04: 30–60s)

const railItems = [
  { key: "ignition-dashboard", icon: "grid", label: "Dashboard" },
  { key: "invoices", icon: "cart", label: "POS / Sales" },
  { key: "inventory", icon: "box", label: "Parts" },
  { key: "client-workspace", icon: "users", label: "Clients" },
  { key: "accounting", icon: "bank", label: "Accounting" },
  { key: "whatsapp", icon: "bell", label: "WhatsApp" },
  { key: "settings", icon: "cog", label: "Settings" }
];

// Maps the server deepLink route strings (Report 04 §03 payload) onto the
// SPA's registered screen keys.
function deepLinkToView(deepLink) {
  const path = String(deepLink || "").split("?")[0].replace(/^\//, "");
  const map = {
    accounting: "accounting",
    stock: "stock",
    whatsapp: "whatsapp",
    reorder: "reorder",
    invoices: "invoices",
    "customer-aging": "customer-aging",
    "client-workspace": "client-workspace"
  };
  return map[path] || "accounting";
}

const severityTone = { danger: "danger", warning: "warning", info: "neutral", success: "success" };

// Ignition Dashboard (Report 03 §06 composition, Report 04 §02–04 contract).
// One composed GET /api/dashboard/summary call replaces the legacy view's
// seven; appconstants + currencies stay as the two pre-existing
// reference-data calls (cached client-side across views). The legacy
// dashboard-view.js stays registered and untouched — this screen is the
// additive Ignition rebuild, opt-in from the sidebar/palette.
export function IgnitionDashboardView({ api, onView, user }) {
  const [summary, setSummary] = useState(null);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [lastSync, setLastSync] = useState(null);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [queueVersion, setQueueVersion] = useState(0);
  const etagRef = useRef(null);
  const shownQueueKeys = useRef(new Set());

  useEffect(() => {
    configureTelemetry({ role: user?.roleId ?? user?.RoleId ?? null });
    trackScreenView("ignition-dashboard", "sidebar");
  }, [user]);

  const navigate = useCallback((nextView, source = "dashboard") => {
    trackScreenView(nextView, source);
    if (typeof onView === "function") onView(nextView);
  }, [onView]);

  // Summary fetch with If-None-Match revalidation: unchanged data returns
  // 304 with no body (ApiClient surfaces non-2xx as ApiError, so 304 is
  // caught and treated as "still fresh") — Report 04 §04/§08.
  const loadSummary = useCallback(async ({ silent = false } = {}) => {
    if (!silent) setIsLoading(true);
    try {
      const headers = etagRef.current ? { "If-None-Match": etagRef.current } : undefined;
      const response = await api.requestResponse("/api/dashboard/summary", headers ? { headers } : {});
      etagRef.current = response.headers.get("ETag") || etagRef.current;
      const data = await api.readResponse(response);
      if (data) setSummary(data);
      setError("");
      setLastSync(new Date());
    } catch (requestError) {
      if (requestError && requestError.status === 304) {
        setLastSync(new Date());
      } else if (!silent || !summary) {
        setError(requestError.message || "Dashboard failed to load.");
      }
    } finally {
      if (!silent) setIsLoading(false);
    }
  }, [api, summary]);

  const loadReferenceData = useCallback(async () => {
    try {
      const [constants, currencies] = await Promise.all([
        api.get("/api/appconstants").catch(() => []),
        api.get("/api/currencies").catch(() => [])
      ]);
      setAppConstants(Array.isArray(constants) ? constants : []);
      setCurrencyRates(Array.isArray(currencies) ? currencies : []);
    } catch { /* reference data is non-blocking */ }
  }, [api]);

  useEffect(() => {
    loadSummary();
    loadReferenceData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [api]);

  useEffect(() => {
    const timer = setInterval(() => loadSummary({ silent: true }), POLL_INTERVAL_MS);
    return () => clearInterval(timer);
  }, [loadSummary]);

  const displayContext = useMemo(() => displayCurrencyContext({
    constants: appConstants,
    rates: currencyRates,
    counterCurrencyCode: summary?.currencyCode
  }), [appConstants, currencyRates, summary]);
  const formatMoney = useCallback((value) => displayMoneyFromCounter(value, displayContext), [displayContext]);

  // --- KPI band (Report 04 §03 `kpis` block) ---
  const kpiOrder = [
    { key: "salesToday", label: "Sales Today", icon: "dollar", tone: "orange", money: true, view: "invoices" },
    { key: "profitToday", label: "Profit Today", icon: "trend", tone: "success", money: true, view: "invoices" },
    { key: "cashBalance", label: "Cash Balance", icon: "wallet", tone: "info", money: true, view: "accounting" },
    { key: "receivables", label: "Receivables", icon: "inbox", tone: "danger", money: true, view: "accounting" },
    { key: "stockValue", label: "Stock Value", icon: "warehouse", tone: "neutral", money: true, view: "stock" },
    { key: "lowStockCount", label: "Low Stock Items", icon: "warn", tone: "danger", money: false, view: "reorder" }
  ];
  const trendValues = (summary?.salesTrend?.series || []).map((point) => Number(point.amount || 0));
  const kpiTiles = kpiOrder.map((kpi) => {
    const entry = summary?.kpis?.[kpi.key];
    const delta = entry && entry.deltaPercent != null
      ? { text: `${Math.abs(Number(entry.deltaPercent)).toFixed(1)}%`, dir: entry.direction === "down" ? "down" : entry.direction === "flat" ? "flat" : "up" }
      : null;
    return {
      key: kpi.key,
      label: kpi.label,
      value: entry
        ? (kpi.money
          ? h(MoneyText, { value: entry.value, currency: summary?.currencyCode, format: formatMoney })
          : Number(entry.value || 0).toLocaleString())
        : "--",
      delta,
      icon: kpi.icon,
      tone: kpi.tone,
      sparkline: kpi.key === "salesToday" ? trendValues : undefined,
      onClick: () => navigate(kpi.view)
    };
  });

  // --- Action Queue with lifecycle persistence (Report 05 §4) ---
  const queueItems = useMemo(() => {
    void queueVersion; // re-derive after act/dismiss
    return (summary?.actionQueue || []).map((item) => {
      const state = getQueueItemState(item.type || item.id, item.id);
      return { ...item, lifecycle: state };
    }).filter((item) => !(item.lifecycle && item.lifecycle.status === "dismissed"));
  }, [summary, queueVersion]);

  useEffect(() => {
    queueItems.forEach((item) => {
      if (!shownQueueKeys.current.has(item.id)) {
        shownQueueKeys.current.add(item.id);
        trackQueueItem("shown", item.id, { severity: item.severity, amount: item.amount });
      }
    });
  }, [queueItems]);

  const openQueueItem = useCallback((item) => {
    markQueueItemActed(item.type || item.id, item.id);
    trackQueueItem("acted", item.id, { severity: item.severity });
    setQueueVersion((version) => version + 1);
    navigate(deepLinkToView(item.deepLink), "deep-link");
  }, [navigate]);

  const dismissQueueItem = useCallback((item) => {
    markQueueItemDismissed(item.type || item.id, item.id, 1);
    trackQueueItem("dismissed", item.id, { severity: item.severity });
    setQueueVersion((version) => version + 1);
  }, []);

  // --- Palette actions ---
  const handlePaletteAction = useCallback((action) => {
    switch (action.type) {
      case "navigate":
        navigate(action.view, "palette");
        break;
      case "open-workspace":
      case "open-messages":
      case "payment-sheet":
        setDraftContext({ view: "client-workspace", ...action });
        navigate("client-workspace", "palette");
        break;
      case "draft-invoice":
        setDraftContext({ view: "invoices", ...action });
        navigate("invoices", "palette");
        break;
      case "draft-quote":
        setDraftContext({ view: "quotes", ...action });
        navigate("quotes", "palette");
        break;
      case "create-client":
        setDraftContext({ view: "contacts", ...action });
        navigate("contacts", "palette");
        break;
      case "open-document":
        setDraftContext({ view: "invoices", ...action });
        navigate("invoices", "palette");
        break;
      default:
        break;
    }
  }, [navigate]);

  // --- Panels (Report 04 §03 lowStock/deadStock/unpaidTransactions) ---
  const lowStock = summary?.lowStockPanel;
  const deadStock = summary?.deadStockPanel;
  const unpaid = summary?.unpaidTransactionsPanel;
  const trendSeries = (summary?.salesTrend?.series || []).map((point) => ({
    label: new Date(point.date).toLocaleDateString(undefined, { weekday: "short" }),
    value: Number(point.amount || 0)
  }));

  const quickActions = [
    { key: "new-sale", label: "New Sale", icon: "cart", onClick: () => handlePaletteAction({ type: "draft-invoice", clientId: null }) },
    { key: "find-parts", label: "Find Parts", icon: "search", onClick: () => setPaletteOpen(true) },
    { key: "clients", label: "Clients", icon: "users", onClick: () => navigate("client-workspace") },
    { key: "receive-payment", label: "Receive Payment", icon: "wallet", onClick: () => navigate("client-workspace") },
    { key: "quotes", label: "New Quote", icon: "clipboard", onClick: () => handlePaletteAction({ type: "draft-quote", clientId: null }) },
    { key: "whatsapp", label: "Messages", icon: "bell", onClick: () => navigate("whatsapp") },
    { key: "reports", label: "Reports", icon: "trend", onClick: () => navigate("report-builder") },
    { key: "legacy", label: "Classic Dashboard", icon: "grid", onClick: () => navigate("dashboard") }
  ];

  const displayName = user?.fullName || user?.name || user?.username || "Administrator";

  return h("div", { className: "ignition-root", id: "ig-dashboard" },
    h(IconRail, { items: railItems, activeKey: "ignition-dashboard", onSelect: (key) => navigate(key, "rail") }),
    h("main", { className: "ig-dash-main", "aria-label": "Dashboard" },
      h("div", { className: "ig-dash-topline" },
        h("div", { className: "ig-dash-title" },
          h("h1", null, "Command Center"),
          h("p", null, `Welcome back, ${displayName} — here is what needs attention.`)),
        h(CommandBar, { onOpen: () => setPaletteOpen(true) }),
        h("span", { className: "ig-dash-live" },
          h("span", { className: "ig-live-dot", "aria-hidden": "true" }),
          isLoading ? "Updating…" : lastSync ? `Live · ${lastSync.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" })}` : "Live updates"),
        h("button", { type: "button", className: "ig-btn", onClick: () => loadSummary(), disabled: isLoading }, "Refresh")),

      error && h("div", { className: "ig-error-banner", role: "alert" },
        h("span", null, error),
        h("button", { type: "button", className: "ig-btn", onClick: () => loadSummary() }, "Retry")),

      h(KpiBand, { tiles: kpiTiles, columns: 6, loading: isLoading && !summary }),

      h("div", { className: "ig-dash-grid" },
        h("div", { className: "ig-dash-col" },
          h(InsightPanel, {
            title: "Sales Trend",
            subtitle: summary?.salesTrend ? `Last ${summary.salesTrend.rangeDays} days` : "Last 7 days",
            headerAction: summary?.salesTrend?.peakAmount != null && h("span", { className: "ig-panel-sub" },
              "Peak ", h(MoneyText, { value: summary.salesTrend.peakAmount, format: formatMoney }))
          },
            h(TrendChart, { series: trendSeries, variant: "area", height: 168, formatValue: formatMoney, loading: isLoading && !summary })),
          h("div", { className: "ig-dash-panels" },
            h(InsightPanel, { title: "Low Stock", subtitle: lowStock ? `${lowStock.totalCount} items below minimum` : "" },
              (lowStock?.topItems || []).length
                ? h(DataGrid, {
                  caption: "Parts below minimum stock",
                  columns: [
                    { key: "name", label: "Part", render: (row) => row.name },
                    { key: "onHand", label: "On hand", render: (row) => h("span", { className: "ig-datagrid-num" }, String(row.onHand)) },
                    { key: "minStock", label: "Min", render: (row) => h("span", { className: "ig-datagrid-num" }, String(row.minStock)) }
                  ],
                  rows: lowStock.topItems,
                  getRowKey: (row) => row.partId,
                  emptyText: "No low-stock parts."
                })
                : h("p", { className: "ig-muted" }, isLoading ? "Loading…" : "No low-stock parts."),
              h("button", { type: "button", className: "ig-btn", onClick: () => navigate("reorder") }, "Open reorder")),
            h(InsightPanel, { title: "Dead Stock", subtitle: deadStock ? `${deadStock.totalUnits} units idle` : "" },
              deadStock
                ? h("div", { className: "ig-stack" },
                  h(MoneyText, { value: deadStock.totalValue, size: "lg", format: formatMoney }),
                  (deadStock.topSegments || []).map((segment) =>
                    h("div", { key: segment.segmentKey, className: "ig-cluster", style: { justifyContent: "space-between" } },
                      h("span", null, segment.categoryName),
                      h(MoneyText, { value: segment.deadStockValue, format: formatMoney }))))
                : h("p", { className: "ig-muted" }, isLoading ? "Loading…" : "No dead stock flagged."),
              h("button", { type: "button", className: "ig-btn", onClick: () => navigate("dead-stock") }, "Review"))),
          h(InsightPanel, { title: "Unpaid Transactions", subtitle: unpaid ? `${unpaid.count} open` : "" },
            (unpaid?.topItems || []).length
              ? h(DataGrid, {
                caption: "Unpaid transactions",
                columns: [
                  { key: "number", label: "Invoice", render: (row) => row.transactionNumber, sortable: true, sortValue: (row) => row.transactionNumber },
                  { key: "counterparty", label: "Counterparty", render: (row) => row.counterparty },
                  { key: "remaining", label: "Remaining", render: (row) => h(MoneyText, { value: row.remainingAmount, format: formatMoney }), sortable: true, sortValue: (row) => Number(row.remainingAmount || 0) },
                  { key: "overdue", label: "Days overdue", render: (row) => h("span", { className: "ig-datagrid-num" }, String(row.daysOverdue ?? "--")), sortable: true, sortValue: (row) => Number(row.daysOverdue || 0) }
                ],
                rows: unpaid.topItems,
                getRowKey: (row) => row.transactionNumber,
                emptyText: "Nothing unpaid."
              })
              : h("p", { className: "ig-muted" }, isLoading ? "Loading…" : "Nothing unpaid — receivables are clear."),
            unpaid && h("div", { className: "ig-cluster", style: { justifyContent: "space-between" } },
              h("span", { className: "ig-label" }, "Total outstanding"),
              h(MoneyText, { value: unpaid.totalRemaining, size: "lg", format: formatMoney })))),

        h("div", { className: "ig-dash-col" },
          h(InsightPanel, { title: "Action Queue", subtitle: "Scored by severity × money at stake" },
            h("div", { className: "ig-stack", role: "list", "aria-label": "Action queue" },
              isLoading && !summary
                ? [0, 1, 2].map((index) => h("span", { key: index, className: "ig-skeleton", style: { height: "64px" } }))
                : queueItems.length
                  ? queueItems.map((item) =>
                    h(ActionQueueCard, {
                      key: item.id,
                      tone: severityTone[item.severity] || "neutral",
                      label: item.type || item.severity,
                      title: item.title,
                      detail: item.detail,
                      value: item.amount != null ? h(MoneyText, { value: item.amount, format: formatMoney }) : null,
                      acted: item.lifecycle?.status === "acted",
                      onOpen: () => openQueueItem(item),
                      onDismiss: () => dismissQueueItem(item)
                    }))
                  : h("p", { className: "ig-muted" }, "All clear — no exceptions in the feed."))),
          h(InsightPanel, { title: "Quick Actions" },
            h(QuickActionDock, { actions: quickActions }))))),

    h(CommandPalette, {
      open: paletteOpen,
      onClose: () => setPaletteOpen(false),
      api,
      screens: screenRegistry.items,
      onAction: handlePaletteAction
    })
  );
}
