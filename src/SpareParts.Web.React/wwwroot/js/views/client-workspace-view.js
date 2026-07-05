import { h, useCallback, useEffect, useMemo, useRef, useState } from "../core/react-runtime.js";
import { displayCurrencyContext, displayMoneyFromCounter, shortDate } from "../core/formatters.js";
import { screenRegistry } from "./screen-registry.js";
import { CommunicationPayloadFactory } from "../services/communication-payload-factory.js";
import { IconRail } from "../components/ignition/IconRail.js";
import { CommandBar } from "../components/ignition/CommandBar.js";
import { CommandPalette } from "../components/ignition/CommandPalette.js";
import { ClientRail } from "../components/ignition/ClientRail.js";
import { ClientHeader, deriveRiskTone } from "../components/ignition/ClientHeader.js";
import { AgingBar } from "../components/ignition/AgingBar.js";
import { ActivityTimeline } from "../components/ignition/ActivityTimeline.js";
import { TabStrip } from "../components/ignition/TabStrip.js";
import { DataGrid } from "../components/ignition/DataGrid.js";
import { MoneyText } from "../components/ignition/MoneyText.js";
import { setDraftContext, takeDraftContext } from "../services/ignition-context.js";
import {
  configureTelemetry,
  trackReminder,
  trackScreenView,
  trackTaskComplete,
  trackTaskStart
} from "../services/telemetry-service.js";

const PAGE_SIZE = 25; // default page size per the Report 04 §06 contract

const railItems = [
  { key: "ignition-dashboard", icon: "grid", label: "Dashboard" },
  { key: "invoices", icon: "cart", label: "POS / Sales" },
  { key: "inventory", icon: "box", label: "Parts" },
  { key: "client-workspace", icon: "users", label: "Clients" },
  { key: "accounting", icon: "bank", label: "Accounting" },
  { key: "whatsapp", icon: "bell", label: "WhatsApp" },
  { key: "settings", icon: "cog", label: "Settings" }
];

const TABS = [
  { key: "overview", label: "Overview" },
  { key: "invoices", label: "Invoices" },
  { key: "payments", label: "Payments" },
  { key: "quotes", label: "Quotes" },
  { key: "part-requests", label: "Requests" },
  { key: "timeline", label: "Timeline" }
];

function normalizeClient(row, kind) {
  return {
    id: row.id ?? row.customerId,
    name: row.name ?? row.customerName ?? "Unnamed",
    phone: row.phone || "",
    balance: Number(row.balance ?? row.totalBalance ?? row.openingBalance ?? 0),
    riskTone: Number(row.over90Days || 0) > 0 ? "danger"
      : (Number(row.days1To30 || 0) + Number(row.days31To60 || 0) + Number(row.days61To90 || 0)) > 0 ? "warning" : "success",
    kind
  };
}

// Oldest-first allocation across open invoices (Report 05 §5 flow 1).
export function allocateOldestFirst(amount, openInvoices) {
  let remaining = Number(amount || 0);
  return openInvoices.map((invoice) => {
    const due = Math.max(0, Number(invoice.remainingAmount ?? invoice.totalAmount ?? 0) - Number(invoice.paidAmount || 0) * (invoice.remainingAmount != null ? 0 : 1));
    const applied = Math.max(0, Math.min(due, remaining));
    remaining -= applied;
    return { invoice, applied };
  }).filter((entry) => entry.applied > 0);
}

// Ignition Client Workspace (Report 03 §07, Report 04 §05–06, Report 05 §5).
// One selectedClientId drives every region; tabs lazy-load their paged child
// route only when opened. Replaces the fragmented Contacts/Aging/per-screen
// lookups — those legacy screens stay registered and untouched.
export function ClientWorkspaceView({ api, onView, user }) {
  const [clients, setClients] = useState([]);
  const [clientsLoading, setClientsLoading] = useState(true);
  const [clientsError, setClientsError] = useState("");
  const [filterKind, setFilterKind] = useState("all");
  const [selectedId, setSelectedId] = useState(null);
  const [workspace, setWorkspace] = useState(null);
  const [workspaceLoading, setWorkspaceLoading] = useState(false);
  const [workspaceError, setWorkspaceError] = useState("");
  const [activeTab, setActiveTab] = useState("overview");
  const [tabData, setTabData] = useState({});
  const [invoiceBucketFilter, setInvoiceBucketFilter] = useState(null);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const [paymentSheet, setPaymentSheet] = useState(null);
  const [reminderSheet, setReminderSheet] = useState(null);
  const [status, setStatus] = useState("");
  const searchSeq = useRef(0);

  useEffect(() => {
    configureTelemetry({ role: user?.roleId ?? user?.RoleId ?? null });
    trackScreenView("client-workspace", "sidebar");
  }, [user]);

  const navigate = useCallback((nextView, source = "workspace") => {
    trackScreenView(nextView, source);
    if (typeof onView === "function") onView(nextView);
  }, [onView]);

  const formatMoney = useCallback((value) => {
    const context = displayCurrencyContext({ counterCurrencyCode: workspace?.currencyCode });
    return displayMoneyFromCounter(value, context);
  }, [workspace]);

  // --- Client rail: merged customers + suppliers (Report 03 §04) ---
  const loadClients = useCallback(async () => {
    setClientsLoading(true);
    setClientsError("");
    try {
      const [customers, suppliers] = await Promise.all([
        api.get("/api/customers?page=1&pageSize=100").catch(() => []),
        api.get("/api/suppliers?page=1&pageSize=100").catch(() => [])
      ]);
      const merged = [
        ...(Array.isArray(customers) ? customers : []).map((row) => normalizeClient(row, "customer")),
        ...(Array.isArray(suppliers) ? suppliers : []).map((row) => normalizeClient(row, "supplier"))
      ];
      setClients(merged);
    } catch (error) {
      setClientsError(error.message || "Could not load clients.");
    } finally {
      setClientsLoading(false);
    }
  }, [api]);

  useEffect(() => { loadClients(); }, [loadClients]);

  // Typeahead fallback past the loaded rows — GET /api/clients/search
  // (Report 04 §06); local filtering in ClientRail stays instant.
  const handleRailSearch = useCallback(async (query) => {
    const term = String(query || "").trim();
    if (term.length < 2) return;
    const seq = ++searchSeq.current;
    try {
      const rows = await api.get(`/api/clients/search?q=${encodeURIComponent(term)}&take=10`);
      if (seq !== searchSeq.current || !Array.isArray(rows)) return;
      setClients((current) => {
        const known = new Set(current.map((client) => String(client.id)));
        const extra = rows
          .filter((row) => !known.has(String(row.customerId ?? row.id)))
          .map((row) => normalizeClient(row, "customer"));
        return extra.length ? [...current, ...extra] : current;
      });
    } catch { /* typeahead is additive; local filter already ran */ }
  }, [api]);

  // --- Workspace read model: GET /api/clients/{id}/workspace (Report 04 §05) ---
  const loadWorkspace = useCallback(async (clientId) => {
    if (clientId == null) return;
    setWorkspaceLoading(true);
    setWorkspaceError("");
    try {
      const data = await api.get(`/api/clients/${clientId}/workspace`);
      setWorkspace(data);
    } catch (error) {
      setWorkspace(null);
      setWorkspaceError(error.message || "Could not load the client workspace.");
    } finally {
      setWorkspaceLoading(false);
    }
  }, [api]);

  const selectClient = useCallback((clientId) => {
    setSelectedId(clientId);
    setActiveTab("overview");
    setTabData({});
    setInvoiceBucketFilter(null);
    loadWorkspace(clientId);
  }, [loadWorkspace]);

  // --- Paged child tabs, lazy-loaded on first open (Report 04 §06) ---
  const loadTab = useCallback(async (tabKey, page = 1, append = false) => {
    if (selectedId == null || tabKey === "overview") return;
    setTabData((current) => ({
      ...current,
      [tabKey]: { ...(current[tabKey] || { rows: [] }), loading: true, error: "" }
    }));
    try {
      const rows = await api.get(`/api/clients/${selectedId}/${tabKey}?page=${page}&pageSize=${PAGE_SIZE}`);
      const list = Array.isArray(rows) ? rows : [];
      setTabData((current) => ({
        ...current,
        [tabKey]: {
          rows: append ? [...(current[tabKey]?.rows || []), ...list] : list,
          page,
          hasMore: list.length >= PAGE_SIZE,
          loading: false,
          error: ""
        }
      }));
    } catch (error) {
      setTabData((current) => ({
        ...current,
        [tabKey]: { ...(current[tabKey] || { rows: [] }), loading: false, error: error.message || "Could not load this tab." }
      }));
    }
  }, [api, selectedId]);

  const openTab = useCallback((tabKey) => {
    setActiveTab(tabKey);
    if (tabKey !== "overview" && !tabData[tabKey]) loadTab(tabKey);
  }, [loadTab, tabData]);

  // --- Draft context handoff from the palette / dashboard deep links ---
  useEffect(() => {
    const draft = takeDraftContext("client-workspace");
    if (!draft) return;
    if (draft.clientId != null) {
      selectClient(draft.clientId);
      if (draft.type === "payment-sheet") {
        setPaymentSheet({ amount: draft.amount || null, pending: true });
        setActiveTab("payments");
      } else if (draft.type === "open-messages") {
        setActiveTab("timeline");
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Payment sheet opened via deep link waits for invoices to be available.
  useEffect(() => {
    if (paymentSheet?.pending && selectedId != null) {
      loadTab("invoices");
      setPaymentSheet((sheet) => (sheet ? { ...sheet, pending: false } : sheet));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [paymentSheet?.pending, selectedId]);

  // "/" focuses the rail search while the workspace is active (Report 03 §09).
  useEffect(() => {
    const handleKeyDown = (event) => {
      if (event.key !== "/" || event.ctrlKey || event.metaKey || event.altKey) return;
      const tag = String(event.target?.tagName || "").toLowerCase();
      if (tag === "input" || tag === "textarea" || tag === "select") return;
      const input = document.getElementById("ig-client-rail-search");
      if (input) {
        event.preventDefault();
        input.focus();
      }
    };
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, []);

  // --- Quick actions (Report 05 §5) ---
  const startInvoiceFromContext = useCallback(() => {
    setDraftContext({ view: "invoices", type: "draft-invoice", clientId: selectedId, clientName: workspace?.name });
    navigate("invoices", "workspace-context");
  }, [navigate, selectedId, workspace]);

  const startQuoteFromContext = useCallback(() => {
    setDraftContext({ view: "quotes", type: "draft-quote", clientId: selectedId, clientName: workspace?.name });
    navigate("quotes", "workspace-context");
  }, [navigate, selectedId, workspace]);

  const openPaymentSheet = useCallback(() => {
    trackTaskStart("T4");
    if (!tabData.invoices) loadTab("invoices");
    setPaymentSheet({ amount: Number(workspace?.balance?.totalBalance || 0) || null });
  }, [loadTab, tabData.invoices, workspace]);

  const openReminderSheet = useCallback(() => {
    const balance = workspace?.balance;
    const draftBody = `Hello ${workspace?.name || "customer"}, a friendly reminder that your account balance is `
      + `${formatMoney(balance?.totalBalance || 0)}`
      + (Number(balance?.over90Days || 0) > 0 ? `, of which ${formatMoney(balance.over90Days)} is more than 90 days overdue` : "")
      + ". Please arrange payment at your convenience. Thank you — Maalouf Auto Parts.";
    trackReminder("previewed", selectedId);
    setReminderSheet({ body: draftBody, edited: false, sending: false });
  }, [formatMoney, selectedId, workspace]);

  const sendReminder = useCallback(async () => {
    if (!reminderSheet || selectedId == null) return;
    setReminderSheet((sheet) => ({ ...sheet, sending: true }));
    try {
      if (reminderSheet.edited) {
        trackReminder("edited", selectedId);
        await api.post("/api/communications/send", CommunicationPayloadFactory.freeText({
          recipientName: workspace?.name,
          recipientPhone: workspace?.phone,
          body: reminderSheet.body
        }));
      } else {
        // Reuses the exact payload the legacy aging screen sends.
        await api.post("/api/communications/send", CommunicationPayloadFactory.accountBalanceReminder(selectedId));
      }
      trackReminder("sent", selectedId, { edited: reminderSheet.edited });
      setStatus(`Reminder sent to ${workspace?.name}.`);
      setReminderSheet(null);
      if (tabData.timeline) loadTab("timeline");
    } catch (error) {
      setStatus(error.message || "Reminder failed to send.");
      setReminderSheet((sheet) => (sheet ? { ...sheet, sending: false } : sheet));
    }
  }, [api, loadTab, reminderSheet, selectedId, tabData.timeline, workspace]);

  // Open invoices, oldest first, for the allocation preview.
  const openInvoices = useMemo(() => {
    const rows = tabData.invoices?.rows || [];
    return rows
      .filter((row) => Number(row.remainingAmount ?? (Number(row.totalAmount || 0) - Number(row.paidAmount || 0))) > 0.005)
      .sort((left, right) => new Date(left.transactionDate || left.date || 0) - new Date(right.transactionDate || right.date || 0));
  }, [tabData.invoices]);

  const paymentAllocations = useMemo(() => {
    if (!paymentSheet || paymentSheet.amount == null) return [];
    return allocateOldestFirst(paymentSheet.amount, openInvoices.map((invoice) => ({
      ...invoice,
      remainingAmount: Number(invoice.remainingAmount ?? (Number(invoice.totalAmount || 0) - Number(invoice.paidAmount || 0)))
    })));
  }, [openInvoices, paymentSheet]);

  // Optimistic payment posting (Report 03 §09 pattern: apply locally,
  // reconcile with the server, roll back on ApiError).
  const submitPayment = useCallback(async () => {
    if (!paymentSheet || !paymentAllocations.length || selectedId == null) return;
    const originalInvoices = tabData.invoices?.rows || [];
    const originalWorkspace = workspace;
    const totalApplied = paymentAllocations.reduce((sum, entry) => sum + entry.applied, 0);

    // 1. Optimistic patch — rows and header balance update instantly.
    setTabData((current) => ({
      ...current,
      invoices: {
        ...(current.invoices || {}),
        rows: (current.invoices?.rows || []).map((row) => {
          const allocation = paymentAllocations.find((entry) => entry.invoice.id === row.id);
          if (!allocation) return row;
          const remaining = Number(row.remainingAmount ?? (Number(row.totalAmount || 0) - Number(row.paidAmount || 0)));
          return { ...row, remainingAmount: Math.max(0, remaining - allocation.applied), status: "Pending sync" };
        })
      }
    }));
    setWorkspace((current) => current
      ? {
        ...current,
        balance: {
          ...current.balance,
          totalBalance: Math.max(0, Number(current.balance?.totalBalance || 0) - totalApplied)
        }
      }
      : current);
    setPaymentSheet((sheet) => ({ ...sheet, submitting: true }));

    try {
      for (const entry of paymentAllocations) {
        // 2. Reconcile with server truth, invoice by invoice.
        await api.post(`/api/sales/${entry.invoice.id}/payments`, { amount: entry.applied });
      }
      trackTaskComplete("T4", { amount: totalApplied, invoices: paymentAllocations.length });
      setStatus(`Payment of ${formatMoney(totalApplied)} recorded for ${workspace?.name}.`);
      setPaymentSheet(null);
      loadWorkspace(selectedId);
      loadTab("invoices");
      if (tabData.payments) loadTab("payments");
      if (tabData.timeline) loadTab("timeline");
    } catch (error) {
      // 3. Rollback + surface ApiError.message.
      setTabData((current) => ({ ...current, invoices: { ...(current.invoices || {}), rows: originalInvoices } }));
      setWorkspace(originalWorkspace);
      setPaymentSheet((sheet) => (sheet ? { ...sheet, submitting: false } : sheet));
      setStatus(error.message || "Payment failed — changes reverted.");
    }
  }, [api, formatMoney, loadTab, loadWorkspace, paymentAllocations, paymentSheet, selectedId, tabData, workspace]);

  const handlePaletteAction = useCallback((action) => {
    switch (action.type) {
      case "navigate":
        navigate(action.view, "palette");
        break;
      case "open-workspace":
        if (action.clientId != null) selectClient(action.clientId);
        break;
      case "payment-sheet":
        if (action.clientId != null) {
          selectClient(action.clientId);
          setPaymentSheet({ amount: action.amount || null, pending: true });
        }
        break;
      case "open-messages":
        if (action.clientId != null) {
          selectClient(action.clientId);
          setActiveTab("timeline");
        }
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
  }, [navigate, selectClient]);

  // --- Tab bodies ---
  const timelineEvents = useMemo(() => (tabData.timeline?.rows || []).map((row, index) => ({
    id: row.id || `timeline-${index}`,
    kind: row.kind || row.type || "note",
    title: row.title || row.subject || row.description || "Event",
    detail: row.detail || row.summary || (row.amount != null ? formatMoney(row.amount) : ""),
    timestamp: row.timestamp || row.eventDate || row.date || row.createdAt,
    actor: row.actor || row.userName || ""
  })), [formatMoney, tabData.timeline]);

  const renderDocumentsGrid = (tabKey, extraColumns = []) => {
    const state = tabData[tabKey] || {};
    if (state.error) {
      return h("div", { className: "ig-error-banner", role: "alert" },
        h("span", null, state.error),
        h("button", { type: "button", className: "ig-btn", onClick: () => loadTab(tabKey) }, "Retry"));
    }
    let rows = state.rows || [];
    if (tabKey === "invoices" && invoiceBucketFilter) {
      rows = rows.filter((row) => Number(row.remainingAmount ?? (Number(row.totalAmount || 0) - Number(row.paidAmount || 0))) > 0.005);
    }
    return h("div", { className: "ig-stack" },
      tabKey === "invoices" && invoiceBucketFilter && h("div", { className: "ig-cluster" },
        h("span", { className: "ig-pill ig-pill-orange" }, `Filtered: open invoices (${invoiceBucketFilter})`),
        h("button", { type: "button", className: "ig-btn", onClick: () => setInvoiceBucketFilter(null) }, "Clear")),
      h(DataGrid, {
        caption: `Client ${tabKey}`,
        loading: Boolean(state.loading && !rows.length),
        columns: [
          { key: "number", label: "Number", render: (row) => row.transactionNumber || row.number || row.quoteNumber || `#${row.id}`, sortable: true, sortValue: (row) => row.transactionNumber || row.number || String(row.id) },
          { key: "date", label: "Date", render: (row) => shortDate(row.transactionDate || row.date || row.createdAt), sortable: true, sortValue: (row) => String(row.transactionDate || row.date || "") },
          { key: "amount", label: "Amount", render: (row) => h(MoneyText, { value: row.totalAmount ?? row.amount ?? row.quotedPrice, format: formatMoney }), sortable: true, sortValue: (row) => Number(row.totalAmount ?? row.amount ?? 0) },
          ...extraColumns,
          { key: "status", label: "Status", render: (row) => h("span", { className: "ig-pill ig-pill-neutral" }, row.status || "--") }
        ],
        rows,
        getRowKey: (row, index) => row.id ?? `${tabKey}-${index}`,
        emptyText: `No ${tabKey.replace("-", " ")} for this client yet.`,
        stickyColumns: 1
      }),
      state.hasMore && h("button", {
        type: "button",
        className: "ig-btn ig-cw-loadmore",
        disabled: state.loading,
        onClick: () => loadTab(tabKey, (state.page || 1) + 1, true)
      }, state.loading ? "Loading…" : "Load more"));
  };

  const renderTabBody = () => {
    if (workspaceError) {
      return h("div", { className: "ig-error-banner", role: "alert" },
        h("span", null, workspaceError),
        h("button", { type: "button", className: "ig-btn", onClick: () => loadWorkspace(selectedId) }, "Retry"));
    }
    if (activeTab === "overview") {
      const kpis = workspace?.kpis || {};
      return h("div", { className: "ig-stack" },
        h(AgingBar, {
          buckets: workspace?.balance,
          total: workspace?.balance?.totalBalance,
          formatMoney,
          onBucketClick: (bucket) => {
            setInvoiceBucketFilter(bucket);
            openTab("invoices");
          }
        }),
        h("div", { className: "ig-cw-kpis" },
          [
            { label: "Lifetime revenue", value: h(MoneyText, { value: kpis.lifetimeRevenue, format: formatMoney }) },
            { label: "Invoices", value: String(kpis.lifetimeInvoiceCount ?? "--") },
            { label: "Avg days to pay", value: String(kpis.averageDaysToPay ?? "--") },
            { label: "Open quotes", value: String(kpis.openQuotesCount ?? "--") },
            { label: "Active requests", value: String(kpis.activePartRequestsCount ?? "--") },
            { label: "Loyalty", value: kpis.loyaltyTier ? `${kpis.loyaltyTier} · ${kpis.loyaltyPoints ?? 0}` : "--" },
            { label: "Vehicles", value: String((workspace?.vehicles || []).length) },
            { label: "Last activity", value: workspace?.lastActivityAt ? shortDate(workspace.lastActivityAt) : "--" }
          ].map((kpi) =>
            h("div", { key: kpi.label, className: "ig-cw-kpi" },
              h("div", { className: "ig-label" }, kpi.label),
              h("div", { className: "ig-kpi-value", style: { fontSize: "16px" } }, kpi.value))))
      );
    }
    if (activeTab === "timeline") {
      const state = tabData.timeline || {};
      if (state.error) {
        return h("div", { className: "ig-error-banner", role: "alert" },
          h("span", null, state.error),
          h("button", { type: "button", className: "ig-btn", onClick: () => loadTab("timeline") }, "Retry"));
      }
      return h(ActivityTimeline, {
        events: timelineEvents,
        groupByDay: true,
        loading: Boolean(state.loading),
        hasMore: Boolean(state.hasMore),
        onLoadMore: () => loadTab("timeline", (state.page || 1) + 1, true)
      });
    }
    if (activeTab === "payments") {
      return renderDocumentsGrid("payments");
    }
    if (activeTab === "invoices") {
      return renderDocumentsGrid("invoices", [
        { key: "remaining", label: "Remaining", render: (row) => h(MoneyText, { value: row.remainingAmount ?? (Number(row.totalAmount || 0) - Number(row.paidAmount || 0)), format: formatMoney }), sortable: true, sortValue: (row) => Number(row.remainingAmount || 0) }
      ]);
    }
    return renderDocumentsGrid(activeTab);
  };

  const quickActions = workspace ? [
    { label: "New Sale", icon: "cart", onClick: startInvoiceFromContext },
    { label: "Receive Payment", icon: "wallet", onClick: openPaymentSheet },
    { label: "New Quote", icon: "clipboard", onClick: startQuoteFromContext },
    { label: "Send Reminder", icon: "bell", onClick: openReminderSheet, disabled: !workspace.phone }
  ] : [];

  const riskTone = workspace ? deriveRiskTone(workspace.balance) : undefined;

  return h("div", { className: "ignition-root", id: "ig-client-workspace" },
    h(IconRail, { items: railItems, activeKey: "client-workspace", onSelect: (key) => navigate(key, "rail") }),
    h(ClientRail, {
      clients,
      selectedId,
      onSelect: selectClient,
      onSearch: handleRailSearch,
      filterKind,
      onFilterKind: setFilterKind,
      loading: clientsLoading,
      error: clientsError,
      onRetry: loadClients,
      formatMoney
    }),
    h("div", { className: "ig-cw-main" },
      h("div", { className: "ig-cluster", style: { justifyContent: "space-between" } },
        h(CommandBar, { onOpen: () => setPaletteOpen(true) }),
        status && h("span", { className: "ig-muted", role: "status" }, status)),
      selectedId == null
        ? h("div", { className: "ig-cw-body" },
          h("div", { className: "ig-cw-empty" },
            h("h3", null, "Pick a client to open their workspace"),
            h("p", null, "Identity, balance, aging, invoices, payments, quotes, requests, and messages — one record, no screen hopping."),
            h("p", { className: "ig-muted" }, "Tip: press / to search, or Ctrl+K and type c <name>.")))
        : h("div", { className: "ig-stack", style: { minHeight: 0 } },
          h(ClientHeader, {
            client: workspace,
            balance: workspace ? { total: workspace.balance?.totalBalance, currency: workspace.currencyCode } : null,
            riskTone,
            quickActions,
            formatMoney,
            loading: workspaceLoading && !workspace
          }),
          h("div", { className: "ig-cw-body" },
            h(TabStrip, { tabs: TABS, activeKey: activeTab, onChange: openTab, label: "Client sections" }),
            h("div", { role: "tabpanel", id: `ig-tabpanel-${activeTab}`, "aria-labelledby": `ig-tab-${activeTab}` },
              renderTabBody())))),

    // Payment sheet (Report 05 §5 flow 1 — receive payment without leaving).
    paymentSheet && h("div", {
      className: "ig-sheet-backdrop",
      onClick: (event) => { if (event.target === event.currentTarget) setPaymentSheet(null); }
    },
      h("div", { className: "ig-sheet", role: "dialog", "aria-modal": "true", "aria-label": "Receive payment" },
        h("div", { className: "ig-sheet-head" },
          h("h3", null, `Receive payment — ${workspace?.name || ""}`),
          h("button", { type: "button", className: "ig-queue-dismiss", onClick: () => setPaymentSheet(null), "aria-label": "Close" }, "✕")),
        h("div", { className: "ig-sheet-body" },
          h("label", null,
            h("span", { className: "ig-label" }, "Amount (defaults to full balance)"),
            h("input", {
              type: "number",
              min: 0,
              step: "0.01",
              value: paymentSheet.amount ?? "",
              onChange: (event) => setPaymentSheet((sheet) => ({ ...sheet, amount: event.target.value === "" ? null : Number(event.target.value) })),
              style: { width: "100%", marginTop: "6px" }
            })),
          h("div", null,
            h("div", { className: "ig-label", style: { marginBottom: "6px" } }, "Oldest-first allocation"),
            tabData.invoices?.loading
              ? h("span", { className: "ig-skeleton", style: { display: "block", height: "44px" } })
              : paymentAllocations.length
                ? paymentAllocations.map((entry) =>
                  h("div", { key: entry.invoice.id, className: "ig-alloc-row" },
                    h("span", null, entry.invoice.transactionNumber || `#${entry.invoice.id}`,
                      h("span", { className: "ig-muted" }, ` · ${shortDate(entry.invoice.transactionDate || entry.invoice.date)}`)),
                    h(MoneyText, { value: entry.applied, format: formatMoney })))
                : h("p", { className: "ig-muted" }, openInvoices.length ? "Enter an amount to preview the allocation." : "No open invoices — the payment will post on account."))),
        h("div", { className: "ig-sheet-foot" },
          h("button", { type: "button", className: "ig-btn", onClick: () => setPaymentSheet(null) }, "Cancel"),
          h("button", {
            type: "button",
            className: "ig-btn ig-btn-primary",
            disabled: paymentSheet.submitting || !paymentAllocations.length,
            onClick: submitPayment
          }, paymentSheet.submitting ? "Posting…" : "Post payment")))),

    // Reminder preview (Report 05 §5 flow 4 — no more blind fire-and-forget).
    reminderSheet && h("div", {
      className: "ig-sheet-backdrop",
      onClick: (event) => { if (event.target === event.currentTarget) setReminderSheet(null); }
    },
      h("div", { className: "ig-sheet", role: "dialog", "aria-modal": "true", "aria-label": "Reminder preview" },
        h("div", { className: "ig-sheet-head" },
          h("h3", null, `Reminder — ${workspace?.name || ""}`),
          h("button", { type: "button", className: "ig-queue-dismiss", onClick: () => setReminderSheet(null), "aria-label": "Close" }, "✕")),
        h("div", { className: "ig-sheet-body" },
          h("p", { className: "ig-muted" },
            "Preview and edit before sending. Unedited sends reuse the standard account-balance template; edited text is sent as written."),
          h("textarea", {
            rows: 7,
            value: reminderSheet.body,
            onChange: (event) => setReminderSheet((sheet) => ({ ...sheet, body: event.target.value, edited: true })),
            style: { width: "100%", resize: "vertical" }
          })),
        h("div", { className: "ig-sheet-foot" },
          h("button", { type: "button", className: "ig-btn", onClick: () => setReminderSheet(null) }, "Cancel"),
          h("button", {
            type: "button",
            className: "ig-btn ig-btn-primary",
            disabled: reminderSheet.sending,
            onClick: sendReminder
          }, reminderSheet.sending ? "Sending…" : "Send via WhatsApp")))),

    h(CommandPalette, {
      open: paletteOpen,
      onClose: () => setPaletteOpen(false),
      api,
      screens: screenRegistry.items,
      onAction: handlePaletteAction
    })
  );
}
