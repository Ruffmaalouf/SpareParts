const React = require("react");
const { ActivityIndicator, FlatList, Pressable, ScrollView, StyleSheet, Text, TextInput, View } = require("react-native");
const AsyncStorage = require("@react-native-async-storage/async-storage").default;
const { CommunicationPayloadFactory } = require("../core/communication-payload-factory");
const { counterNotesStore } = require("../core/counter-notes-store");
const { initials, money, rowAmount, rowSubtitle, rowTitle, shortDateTime } = require("../core/formatters");
const { offlineWriteQueue } = require("../core/offline-write-queue");
const { ignitionTokens } = require("../theme/ignition-theme");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useRef, useState } = React;
const el = React.createElement;
const IG = ignitionTokens.colors;
const MIN_TAP_TARGET = ignitionTokens.layout.minTouchTarget;

// Ignition Client Workspace, phone layout per Report 01 §14 (Figure 5) and Report 03 §8:
// the desktop split-pane collapses to a single-column stack — a full-screen searchable
// client list ("drawer" client rail) first; tapping a client pushes a full-screen profile
// whose tab strip renders as a horizontally-scrollable chip row; the two most common
// counter actions (Send Reminder, New Invoice) sit in a sticky bottom action bar. All
// interactive targets are >= 44x44 (Report 03 §8 touch behavior).
//
// Data comes strictly from the Report 04 workspace contracts:
//   GET /api/clients/search?q=&take=          (§06 — client-rail typeahead, q min 2 chars, debounced)
//   GET /api/clients/{id}/workspace           (§05 — header + aging + KPIs + vehicles)
//   GET /api/clients/{id}/invoices|payments|quotes|part-requests|timeline?page=&pageSize= (§06)
// The unfiltered initial list uses the existing GET /api/customers paged endpoint that
// this app already calls elsewhere (additive rule — old routes keep working, §11).
//
// Offline behavior per Report 05 §7: the client directory is a versioned local cache
// ("as of HH:MM" badge), reminder sends queue-and-sync through the offline write queue,
// and counter notes are stored device-local so the counter never blocks on a spinner.

const clientListCacheKey = "spareparts.mobile.ignition.clientListCache";
const searchDebounceMs = 275; // Report 04 §06: 250-300ms debounce is the frontend's job.

const workspaceTabs = [
  { key: "profile", label: "Profile" },
  { key: "invoices", label: "Invoices", path: "invoices" },
  { key: "payments", label: "Payments", path: "payments" },
  { key: "quotes", label: "Quotes", path: "quotes" },
  { key: "part-requests", label: "Requests", path: "part-requests" },
  { key: "timeline", label: "Timeline", path: "timeline" }
];

const agingSegments = [
  { key: "current", label: "Current", color: IG.success },
  { key: "days1To30", label: "1-30d", color: IG.warning },
  { key: "days31To60", label: "31-60d", color: IG.accent },
  { key: "days61To90", label: "61-90d", color: IG.accentHover },
  { key: "over90Days", label: "90+d", color: IG.danger }
];

function normalizeClientRow(row) {
  return {
    customerId: row?.customerId ?? row?.id,
    name: row?.name || row?.customerName || "",
    phone: row?.phone || "",
    balance: Number(row?.balance ?? row?.totalBalance ?? row?.openingBalance ?? 0) || 0,
    currencyCode: row?.currencyCode || "USD"
  };
}

const igStyles = StyleSheet.create({
  screen: {
    flex: 1,
    backgroundColor: IG.bgCanvas
  },
  listHeader: {
    paddingHorizontal: ignitionTokens.spacing.space4,
    paddingTop: 10,
    gap: 10
  },
  titleRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10
  },
  screenTitle: {
    color: IG.textPrimary,
    fontSize: 24,
    fontWeight: "800",
    letterSpacing: -0.4
  },
  pendingChip: {
    minHeight: MIN_TAP_TARGET,
    flexDirection: "row",
    alignItems: "center",
    gap: 6,
    borderRadius: ignitionTokens.radii.pill,
    borderWidth: 1,
    borderColor: IG.warning,
    backgroundColor: "rgba(251,191,36,0.12)",
    paddingHorizontal: 12
  },
  pendingChipText: {
    color: IG.warning,
    fontSize: 11,
    fontWeight: "800"
  },
  searchInput: {
    minHeight: MIN_TAP_TARGET + 4,
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface,
    color: IG.textPrimary,
    paddingHorizontal: 14,
    fontSize: 14
  },
  asOfChip: {
    alignSelf: "flex-start",
    borderRadius: ignitionTokens.radii.pill,
    borderWidth: 1,
    borderColor: IG.warning,
    backgroundColor: "rgba(251,191,36,0.12)",
    paddingHorizontal: 10,
    paddingVertical: 5
  },
  asOfText: {
    color: IG.warning,
    fontSize: 11,
    fontWeight: "800"
  },
  statusText: {
    color: IG.textMuted,
    fontSize: 12
  },
  listContent: {
    padding: ignitionTokens.spacing.space4,
    paddingTop: 10,
    gap: 9
  },
  clientRow: {
    minHeight: 64,
    flexDirection: "row",
    alignItems: "center",
    gap: 11,
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface,
    padding: 12
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: ignitionTokens.radii.pill,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: IG.bgElevated2,
    borderWidth: 1,
    borderColor: IG.border
  },
  avatarText: {
    color: IG.accent,
    fontSize: 13,
    fontWeight: "900"
  },
  clientCopy: {
    flex: 1,
    minWidth: 0,
    gap: 2
  },
  clientName: {
    color: IG.textPrimary,
    fontSize: 14,
    fontWeight: "800"
  },
  clientPhone: {
    color: IG.textSecondary,
    fontSize: 12
  },
  clientBalance: {
    maxWidth: 110,
    color: IG.textPrimary,
    fontSize: 12,
    fontWeight: "800",
    textAlign: "right"
  },
  emptyCard: {
    minHeight: 84,
    alignItems: "center",
    justifyContent: "center",
    gap: 4,
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface,
    padding: 14
  },
  emptyTitle: {
    color: IG.textPrimary,
    fontSize: 13,
    fontWeight: "700",
    textAlign: "center"
  },
  emptyText: {
    color: IG.textMuted,
    fontSize: 12,
    textAlign: "center"
  },
  syncPanel: {
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.warning,
    backgroundColor: IG.bgSurface,
    padding: 12,
    gap: 8
  },
  syncPanelTitle: {
    color: IG.textPrimary,
    fontSize: 13,
    fontWeight: "800"
  },
  syncRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8
  },
  syncRowCopy: {
    flex: 1,
    minWidth: 0
  },
  syncRowLabel: {
    color: IG.textSecondary,
    fontSize: 12,
    fontWeight: "700"
  },
  syncRowMeta: {
    color: IG.textMuted,
    fontSize: 11,
    marginTop: 1
  },
  syncRowError: {
    color: IG.danger,
    fontSize: 11,
    marginTop: 1
  },
  syncButton: {
    minHeight: MIN_TAP_TARGET,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: ignitionTokens.radii.pill,
    backgroundColor: IG.accent,
    paddingHorizontal: 14
  },
  syncButtonText: {
    color: IG.bgCanvas,
    fontSize: 12,
    fontWeight: "900"
  },
  syncRetryButton: {
    minHeight: MIN_TAP_TARGET,
    minWidth: MIN_TAP_TARGET,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: ignitionTokens.radii.pill,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgElevated,
    paddingHorizontal: 12
  },
  syncRetryText: {
    color: IG.textPrimary,
    fontSize: 11,
    fontWeight: "800"
  },
  profileTopBar: {
    minHeight: 56,
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
    paddingHorizontal: 10,
    borderBottomWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface
  },
  backButton: {
    minWidth: MIN_TAP_TARGET,
    minHeight: MIN_TAP_TARGET,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: ignitionTokens.radii.pill
  },
  backText: {
    color: IG.textPrimary,
    fontSize: 22,
    fontWeight: "800"
  },
  profileTopTitle: {
    flex: 1,
    minWidth: 0,
    color: IG.textPrimary,
    fontSize: 16,
    fontWeight: "800"
  },
  profileHeader: {
    flexDirection: "row",
    alignItems: "center",
    gap: 12,
    padding: ignitionTokens.spacing.space4,
    paddingBottom: 8
  },
  profileHeaderCopy: {
    flex: 1,
    minWidth: 0,
    gap: 2
  },
  profileName: {
    color: IG.textPrimary,
    fontSize: 18,
    fontWeight: "800"
  },
  profileMeta: {
    color: IG.textSecondary,
    fontSize: 12
  },
  profileOverdue: {
    color: IG.danger,
    fontSize: 12,
    fontWeight: "800"
  },
  chipRail: {
    flexGrow: 0,
    flexShrink: 0
  },
  chipRailContent: {
    paddingHorizontal: ignitionTokens.spacing.space4,
    paddingVertical: 8,
    gap: 8
  },
  tabChip: {
    minHeight: MIN_TAP_TARGET,
    justifyContent: "center",
    borderRadius: ignitionTokens.radii.pill,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface,
    paddingHorizontal: 16
  },
  tabChipActive: {
    borderColor: IG.accent,
    backgroundColor: "rgba(249,115,22,0.14)"
  },
  tabChipText: {
    color: IG.textSecondary,
    fontSize: 12,
    fontWeight: "800"
  },
  tabChipTextActive: {
    color: IG.accent
  },
  profileBody: {
    flex: 1,
    minHeight: 0
  },
  profileBodyContent: {
    padding: ignitionTokens.spacing.space4,
    paddingTop: 8,
    gap: 12,
    paddingBottom: 24
  },
  panel: {
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface,
    padding: 14,
    gap: 10,
    ...ignitionTokens.shadows.l1
  },
  panelTitle: {
    color: IG.textPrimary,
    fontSize: 14,
    fontWeight: "800"
  },
  agingBarTrack: {
    height: 12,
    flexDirection: "row",
    overflow: "hidden",
    borderRadius: ignitionTokens.radii.pill,
    backgroundColor: IG.bgElevated
  },
  agingLegendRow: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  agingLegendItem: {
    flexDirection: "row",
    alignItems: "center",
    gap: 5
  },
  agingDot: {
    width: 9,
    height: 9,
    borderRadius: ignitionTokens.radii.pill
  },
  agingLegendText: {
    color: IG.textSecondary,
    fontSize: 11,
    fontWeight: "700"
  },
  kpiGrid: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8
  },
  kpiCell: {
    width: "48%",
    minHeight: 58,
    justifyContent: "space-between",
    gap: 3,
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgElevated,
    padding: 10
  },
  kpiCellLabel: {
    color: IG.textMuted,
    fontSize: 10,
    fontWeight: "800",
    textTransform: "uppercase",
    letterSpacing: 0.4
  },
  kpiCellValue: {
    color: IG.textPrimary,
    fontSize: 15,
    fontWeight: "800"
  },
  listLine: {
    minHeight: MIN_TAP_TARGET,
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10,
    paddingVertical: 8,
    borderBottomWidth: 1,
    borderColor: IG.border
  },
  listLineCopy: {
    flex: 1,
    minWidth: 0
  },
  listLineTitle: {
    color: IG.textPrimary,
    fontSize: 13,
    fontWeight: "700"
  },
  listLineSubtitle: {
    color: IG.textMuted,
    fontSize: 11,
    marginTop: 2
  },
  listLineValue: {
    maxWidth: 120,
    color: IG.textSecondary,
    fontSize: 12,
    fontWeight: "800",
    textAlign: "right"
  },
  noteInputRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8
  },
  noteInput: {
    flex: 1,
    minHeight: MIN_TAP_TARGET,
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgElevated,
    color: IG.textPrimary,
    paddingHorizontal: 12,
    fontSize: 13
  },
  noteAddButton: {
    minHeight: MIN_TAP_TARGET,
    minWidth: MIN_TAP_TARGET,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: ignitionTokens.radii.pill,
    backgroundColor: IG.accent,
    paddingHorizontal: 14
  },
  noteAddText: {
    color: IG.bgCanvas,
    fontSize: 12,
    fontWeight: "900"
  },
  noteRow: {
    gap: 3,
    paddingVertical: 7,
    borderBottomWidth: 1,
    borderColor: IG.border
  },
  noteBody: {
    color: IG.textPrimary,
    fontSize: 13,
    lineHeight: 18
  },
  noteMetaRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8
  },
  noteMeta: {
    color: IG.textMuted,
    fontSize: 11
  },
  noteState: {
    color: IG.warning,
    fontSize: 10,
    fontWeight: "800",
    textTransform: "uppercase"
  },
  actionBar: {
    flexDirection: "row",
    gap: 10,
    padding: ignitionTokens.spacing.space4,
    paddingTop: 10,
    borderTopWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface
  },
  actionSecondary: {
    flex: 1,
    minHeight: MIN_TAP_TARGET + 4,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: ignitionTokens.radii.pill,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgElevated
  },
  actionSecondaryText: {
    color: IG.textPrimary,
    fontSize: 13,
    fontWeight: "800"
  },
  actionPrimary: {
    flex: 1,
    minHeight: MIN_TAP_TARGET + 4,
    alignItems: "center",
    justifyContent: "center",
    borderRadius: ignitionTokens.radii.pill,
    backgroundColor: IG.accent
  },
  actionPrimaryText: {
    // Dark text on orange per the Report 01 §15 contrast strategy (6.2:1 AA).
    color: IG.bgCanvas,
    fontSize: 13,
    fontWeight: "900"
  },
  loadingRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    paddingVertical: 8
  }
});

function ClientListRow({ client, onPress }) {
  return el(Pressable, {
    accessibilityRole: "button",
    accessibilityLabel: client.name,
    style: igStyles.clientRow,
    onPress
  },
    el(View, { style: igStyles.avatar },
      el(Text, { style: igStyles.avatarText }, initials(client.name))
    ),
    el(View, { style: igStyles.clientCopy },
      el(Text, { style: igStyles.clientName, numberOfLines: 1 }, client.name),
      Boolean(client.phone) && el(Text, { style: igStyles.clientPhone, numberOfLines: 1 }, client.phone)
    ),
    el(Text, { style: igStyles.clientBalance, numberOfLines: 1 }, money(client.balance, client.currencyCode))
  );
}

function PendingSyncPanel({ items, t, onSyncNow, onRetry }) {
  return el(View, { style: igStyles.syncPanel },
    el(View, { style: igStyles.syncRow },
      el(Text, { style: igStyles.syncPanelTitle },
        t("ignition.sync.queuedWrites", "Queued writes (pending sync)")),
      el(Pressable, { accessibilityRole: "button", style: igStyles.syncButton, onPress: onSyncNow },
        el(Text, { style: igStyles.syncButtonText }, t("ignition.sync.syncNow", "Sync now"))
      )
    ),
    items.map((item) => el(View, { key: item.id, style: igStyles.syncRow },
      el(View, { style: igStyles.syncRowCopy },
        el(Text, { style: igStyles.syncRowLabel, numberOfLines: 1 }, item.label || item.path),
        el(Text, { style: igStyles.syncRowMeta, numberOfLines: 1 },
          `${t("ignition.sync.queuedAt", "Queued")} ${shortDateTime(item.queuedAt)} · ${item.attempts || 0}/3`),
        item.status === "attention" && el(Text, { style: igStyles.syncRowError, numberOfLines: 1 },
          item.lastError || t("ignition.sync.needsAttention", "Needs attention"))
      ),
      item.status === "attention" && el(Pressable, {
        accessibilityRole: "button",
        style: igStyles.syncRetryButton,
        onPress: () => onRetry(item.id)
      },
        el(Text, { style: igStyles.syncRetryText }, t("ignition.sync.retry", "Retry"))
      )
    ))
  );
}

function AgingBar({ balance, currencyCode, t }) {
  const values = agingSegments.map((segment) => Math.max(0, Number(balance?.[segment.key] || 0)));
  const total = values.reduce((sum, value) => sum + value, 0);

  return el(View, { style: igStyles.panel },
    el(Text, { style: igStyles.panelTitle }, t("ignition.workspace.aging", "Aging")),
    // Segment order fixed left->right: current -> 1-30 -> 31-60 -> 61-90 -> 90+ (Report 02 §12).
    el(View, { style: igStyles.agingBarTrack },
      agingSegments.map((segment, index) => el(View, {
        key: segment.key,
        style: {
          flexGrow: total > 0 ? values[index] : 1,
          flexBasis: 1,
          backgroundColor: total > 0 && values[index] === 0 ? "transparent" : segment.color
        }
      }))
    ),
    el(View, { style: igStyles.agingLegendRow },
      agingSegments.map((segment, index) => el(View, { key: segment.key, style: igStyles.agingLegendItem },
        el(View, { style: [igStyles.agingDot, { backgroundColor: segment.color }] }),
        el(Text, { style: igStyles.agingLegendText },
          `${segment.label} ${money(values[index], currencyCode)}`)
      ))
    )
  );
}

function WorkspaceKpiGrid({ kpis, currencyCode, t }) {
  const cells = [
    { key: "lifetimeRevenue", label: t("ignition.workspace.lifetimeRevenue", "Lifetime revenue"), value: money(kpis?.lifetimeRevenue || 0, currencyCode) },
    { key: "lifetimeInvoiceCount", label: t("ignition.workspace.invoiceCount", "Invoices"), value: String(kpis?.lifetimeInvoiceCount ?? 0) },
    { key: "averageDaysToPay", label: t("ignition.workspace.daysToPay", "Avg days to pay"), value: `${kpis?.averageDaysToPay ?? 0}d` },
    { key: "openQuotesCount", label: t("ignition.workspace.openQuotes", "Open quotes"), value: String(kpis?.openQuotesCount ?? 0) },
    { key: "activePartRequestsCount", label: t("ignition.workspace.partRequests", "Part requests"), value: String(kpis?.activePartRequestsCount ?? 0) },
    { key: "loyaltyPoints", label: t("ignition.workspace.loyalty", "Loyalty"), value: `${Number(kpis?.loyaltyPoints || 0).toLocaleString()}${kpis?.loyaltyTier ? ` · ${kpis.loyaltyTier}` : ""}` }
  ];

  return el(View, { style: igStyles.panel },
    el(Text, { style: igStyles.panelTitle }, t("ignition.workspace.kpis", "Client KPIs")),
    el(View, { style: igStyles.kpiGrid },
      cells.map((cell) => el(View, { key: cell.key, style: igStyles.kpiCell },
        el(Text, { style: igStyles.kpiCellLabel, numberOfLines: 1 }, cell.label),
        el(Text, { style: igStyles.kpiCellValue, numberOfLines: 1, adjustsFontSizeToFit: true }, cell.value)
      ))
    )
  );
}

function CounterNotesPanel({ notes, draft, onChangeDraft, onAdd, t }) {
  return el(View, { style: igStyles.panel },
    el(Text, { style: igStyles.panelTitle }, t("ignition.workspace.counterNotes", "Counter Notes")),
    el(Text, { style: igStyles.noteMeta },
      t("ignition.workspace.counterNotesHint", "Offline-first: notes save on this device even with no connection.")),
    el(View, { style: igStyles.noteInputRow },
      el(TextInput, {
        style: igStyles.noteInput,
        value: draft,
        onChangeText: onChangeDraft,
        placeholder: t("ignition.workspace.notePlaceholder", "Add a note from the counter..."),
        placeholderTextColor: IG.textMuted
      }),
      el(Pressable, { accessibilityRole: "button", style: igStyles.noteAddButton, onPress: onAdd },
        el(Text, { style: igStyles.noteAddText }, t("common.save", "Save"))
      )
    ),
    notes.map((note) => el(View, { key: note.id, style: igStyles.noteRow },
      el(Text, { style: igStyles.noteBody }, note.body),
      el(View, { style: igStyles.noteMetaRow },
        el(Text, { style: igStyles.noteMeta }, shortDateTime(note.createdAt)),
        el(Text, { style: igStyles.noteState }, t("ignition.workspace.noteOnDevice", "On this device"))
      )
    )),
    notes.length === 0 && el(Text, { style: igStyles.emptyText },
      t("ignition.workspace.noNotes", "No counter notes for this client yet."))
  );
}

function ClientWorkspaceScreen({ api, onNavigate }) {
  const { t } = useTheme();
  const [mode, setMode] = useState("list");
  const [query, setQuery] = useState("");
  const [clients, setClients] = useState([]);
  const [listAsOf, setListAsOf] = useState(null);
  const [status, setStatus] = useState("");
  const [isSearching, setIsSearching] = useState(false);
  const [selected, setSelected] = useState(null);
  const [workspace, setWorkspace] = useState(null);
  const [isWorkspaceLoading, setIsWorkspaceLoading] = useState(false);
  const [activeTab, setActiveTab] = useState("profile");
  const [tabRows, setTabRows] = useState([]);
  const [isTabLoading, setIsTabLoading] = useState(false);
  const [notes, setNotes] = useState([]);
  const [noteDraft, setNoteDraft] = useState("");
  const [queueItems, setQueueItems] = useState([]);
  const [isQueuePanelOpen, setIsQueuePanelOpen] = useState(false);
  const [actionStatus, setActionStatus] = useState("");
  const debounceRef = useRef(null);
  const baseClientsRef = useRef([]);

  const navigate = useCallback((key) => {
    if (typeof onNavigate === "function") onNavigate(key);
  }, [onNavigate]);

  // Keep the pending-sync chip live (visible state, no mystery — Report 05 §7).
  useEffect(() => {
    let isMounted = true;
    const unsubscribe = offlineWriteQueue.subscribe((items) => {
      if (isMounted) setQueueItems(items);
    });
    offlineWriteQueue.list().then((items) => {
      if (isMounted) setQueueItems(items);
    });
    return () => {
      isMounted = false;
      unsubscribe();
    };
  }, []);

  const loadClients = useCallback(async () => {
    setStatus(t("ignition.workspace.loadingClients", "Loading clients..."));
    try {
      const rows = await api.get("/api/customers?page=1&pageSize=100");
      const normalized = (Array.isArray(rows) ? rows : []).map(normalizeClientRow);
      baseClientsRef.current = normalized;
      setClients(normalized);
      setListAsOf(null);
      setStatus("");
      await AsyncStorage.setItem(clientListCacheKey, JSON.stringify({
        rows: normalized,
        cachedAt: new Date().toISOString()
      })).catch(() => {});
      offlineWriteQueue.drain(api).catch(() => {});
    } catch (error) {
      try {
        const raw = await AsyncStorage.getItem(clientListCacheKey);
        const cached = raw ? JSON.parse(raw) : null;
        if (cached && Array.isArray(cached.rows)) {
          baseClientsRef.current = cached.rows;
          setClients(cached.rows);
          setListAsOf(cached.cachedAt);
          setStatus("");
          return;
        }
      } catch {
        // fall through to the error status below
      }
      setStatus(error.message || t("ignition.workspace.clientsError", "Could not load clients."));
    }
  }, [api, t]);

  useEffect(() => {
    loadClients();
  }, [loadClients]);

  // Client-rail typeahead per Report 04 §06: q min 2 chars, debounced 250-300ms.
  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    const trimmed = query.trim();

    if (trimmed.length < 2) {
      setClients(baseClientsRef.current);
      setIsSearching(false);
      return undefined;
    }

    debounceRef.current = setTimeout(async () => {
      setIsSearching(true);
      try {
        const rows = await api.get(`/api/clients/search?q=${encodeURIComponent(trimmed)}&take=25`);
        setClients((Array.isArray(rows) ? rows : []).map(normalizeClientRow));
      } catch {
        // Offline fallback: filter the cached directory locally (Report 05 §7 read path).
        const lowered = trimmed.toLowerCase();
        setClients(baseClientsRef.current.filter((client) =>
          `${client.name} ${client.phone}`.toLowerCase().includes(lowered)));
      } finally {
        setIsSearching(false);
      }
    }, searchDebounceMs);

    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
  }, [api, query]);

  const openClient = useCallback(async (client) => {
    setSelected(client);
    setMode("profile");
    setActiveTab("profile");
    setTabRows([]);
    setWorkspace(null);
    setActionStatus("");
    setIsWorkspaceLoading(true);
    counterNotesStore.listForCustomer(client.customerId).then(setNotes);
    try {
      const payload = await api.get(`/api/clients/${client.customerId}/workspace`);
      setWorkspace(payload);
    } catch (error) {
      setActionStatus(error.message || t("ignition.workspace.workspaceError", "Could not load the client workspace."));
    } finally {
      setIsWorkspaceLoading(false);
    }
  }, [api, t]);

  const closeProfile = useCallback(() => {
    setMode("list");
    setSelected(null);
    setWorkspace(null);
    setTabRows([]);
    setActionStatus("");
  }, []);

  const selectTab = useCallback(async (tabKey) => {
    setActiveTab(tabKey);
    const tab = workspaceTabs.find((item) => item.key === tabKey);
    if (!tab || !tab.path || !selected) {
      setTabRows([]);
      return;
    }

    setIsTabLoading(true);
    setTabRows([]);
    try {
      // Paged child resources per Report 04 §06 (default pageSize 25).
      const rows = await api.get(`/api/clients/${selected.customerId}/${tab.path}?page=1&pageSize=25`);
      setTabRows(Array.isArray(rows) ? rows : []);
    } catch (error) {
      setActionStatus(error.message || t("ignition.workspace.tabError", "Could not load this tab."));
    } finally {
      setIsTabLoading(false);
    }
  }, [api, selected, t]);

  const addNote = useCallback(async () => {
    if (!selected) return;
    const note = await counterNotesStore.add({
      customerId: selected.customerId,
      customerName: selected.name,
      body: noteDraft
    });
    if (note) {
      setNoteDraft("");
      setNotes(await counterNotesStore.listForCustomer(selected.customerId));
    }
  }, [noteDraft, selected]);

  // Send Reminder: queue-and-sync write path (Report 05 §7). The reminder targets the
  // client's most recent unpaid invoice (Report 04 §06 status filter); the documented
  // send route is the existing POST /api/communications/send this app already uses.
  const sendReminder = useCallback(async () => {
    if (!selected) return;
    setActionStatus(t("ignition.workspace.reminderPreparing", "Preparing reminder..."));
    try {
      let unpaid = [];
      try {
        const rows = await api.get(`/api/clients/${selected.customerId}/invoices?page=1&pageSize=25&status=unpaid`);
        unpaid = Array.isArray(rows) ? rows : [];
      } catch {
        unpaid = [];
      }

      const invoice = unpaid[0];
      if (!invoice) {
        setActionStatus(t("ignition.workspace.noUnpaidInvoice", "No unpaid invoice found for this client."));
        return;
      }

      const payload = CommunicationPayloadFactory.paymentReminder(invoice.id ?? invoice.invoiceId ?? invoice.salesInvoiceId);
      const result = await offlineWriteQueue.sendOrEnqueue(api, {
        kind: "payment-reminder",
        label: `${t("ignition.workspace.reminderLabel", "Payment reminder")} — ${selected.name}`,
        path: "/api/communications/send",
        payload
      });

      setActionStatus(result.delivered
        ? t("ignition.workspace.reminderSent", "Reminder sent.")
        : t("ignition.workspace.reminderQueued", "Offline — reminder queued, pending sync."));
    } catch (error) {
      setActionStatus(error.message || t("ignition.workspace.reminderError", "Could not send the reminder."));
    }
  }, [api, selected, t]);

  const syncNow = useCallback(() => {
    offlineWriteQueue.drain(api).catch(() => {});
  }, [api]);

  const retryQueued = useCallback((itemId) => {
    offlineWriteQueue.retry(api, itemId).catch(() => {});
  }, [api]);

  const pendingChip = queueItems.length > 0 && el(Pressable, {
    accessibilityRole: "button",
    style: igStyles.pendingChip,
    onPress: () => setIsQueuePanelOpen((value) => !value)
  },
    el(Text, { style: igStyles.pendingChipText },
      `⟳ ${queueItems.length} ${t("ignition.sync.pending", "pending sync")}`)
  );

  if (mode === "profile" && selected) {
    const balance = workspace?.balance || {};
    const currencyCode = workspace?.currencyCode || selected.currencyCode || "USD";
    const overdue = ["days1To30", "days31To60", "days61To90", "over90Days"]
      .reduce((sum, key) => sum + (Number(balance[key]) || 0), 0);
    const vehicles = Array.isArray(workspace?.vehicles) ? workspace.vehicles : [];

    return el(View, { style: igStyles.screen },
      el(View, { style: igStyles.profileTopBar },
        el(Pressable, {
          accessibilityRole: "button",
          accessibilityLabel: t("common.close", "Back"),
          style: igStyles.backButton,
          onPress: closeProfile
        },
          el(Text, { style: igStyles.backText }, "‹")
        ),
        el(Text, { style: igStyles.profileTopTitle, numberOfLines: 1 }, workspace?.name || selected.name),
        pendingChip
      ),
      isQueuePanelOpen && queueItems.length > 0 && el(View, { style: { paddingHorizontal: ignitionTokens.spacing.space4, paddingTop: 8 } },
        el(PendingSyncPanel, { items: queueItems, t, onSyncNow: syncNow, onRetry: retryQueued })
      ),
      el(View, { style: igStyles.profileHeader },
        el(View, { style: igStyles.avatar },
          el(Text, { style: igStyles.avatarText }, initials(workspace?.name || selected.name))
        ),
        el(View, { style: igStyles.profileHeaderCopy },
          el(Text, { style: igStyles.profileName, numberOfLines: 1 }, workspace?.name || selected.name),
          Boolean(workspace?.phone || selected.phone) &&
            el(Text, { style: igStyles.profileMeta, numberOfLines: 1 }, workspace?.phone || selected.phone),
          el(Text, { style: overdue > 0 ? igStyles.profileOverdue : igStyles.profileMeta, numberOfLines: 1 },
            overdue > 0
              ? `${money(balance.totalBalance ?? selected.balance, currencyCode)} · ${money(overdue, currencyCode)} ${t("ignition.workspace.overdue", "overdue")}`
              : money(balance.totalBalance ?? selected.balance, currencyCode))
        )
      ),
      // Tab strip as a horizontally-scrollable chip row (Report 01 §14).
      el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        style: igStyles.chipRail,
        contentContainerStyle: igStyles.chipRailContent
      },
        workspaceTabs.map((tab) => {
          const isActive = tab.key === activeTab;
          return el(Pressable, {
            key: tab.key,
            accessibilityRole: "button",
            accessibilityState: { selected: isActive },
            style: [igStyles.tabChip, isActive && igStyles.tabChipActive],
            onPress: () => selectTab(tab.key)
          },
            el(Text, {
              style: [igStyles.tabChipText, isActive && igStyles.tabChipTextActive]
            }, t(`ignition.workspace.tabs.${tab.key}`, tab.label))
          );
        })
      ),
      el(ScrollView, {
        style: igStyles.profileBody,
        contentContainerStyle: igStyles.profileBodyContent,
        keyboardShouldPersistTaps: "handled"
      },
        Boolean(actionStatus) && el(Text, { style: igStyles.statusText }, actionStatus),
        isWorkspaceLoading && el(View, { style: igStyles.loadingRow },
          el(ActivityIndicator, { color: IG.accent, size: "small" }),
          el(Text, { style: igStyles.statusText }, t("common.loading", "Loading"))
        ),
        activeTab === "profile" && workspace && el(AgingBar, { balance, currencyCode, t }),
        activeTab === "profile" && workspace && el(WorkspaceKpiGrid, { kpis: workspace.kpis, currencyCode, t }),
        activeTab === "profile" && vehicles.length > 0 && el(View, { style: igStyles.panel },
          el(Text, { style: igStyles.panelTitle }, t("ignition.workspace.vehicles", "Vehicles")),
          vehicles.map((vehicle) => el(View, { key: String(vehicle.vehicleId ?? `${vehicle.make}-${vehicle.plateNumber}`), style: igStyles.listLine },
            el(View, { style: igStyles.listLineCopy },
              el(Text, { style: igStyles.listLineTitle, numberOfLines: 1 },
                [vehicle.make, vehicle.model, vehicle.year].filter(Boolean).join(" ")),
              Boolean(vehicle.plateNumber) &&
                el(Text, { style: igStyles.listLineSubtitle, numberOfLines: 1 }, vehicle.plateNumber)
            )
          ))
        ),
        activeTab === "profile" && el(CounterNotesPanel, {
          notes,
          draft: noteDraft,
          onChangeDraft: setNoteDraft,
          onAdd: addNote,
          t
        }),
        activeTab !== "profile" && el(View, { style: igStyles.panel },
          el(Text, { style: igStyles.panelTitle },
            t(`ignition.workspace.tabs.${activeTab}`, workspaceTabs.find((tab) => tab.key === activeTab)?.label || "")),
          isTabLoading && el(View, { style: igStyles.loadingRow },
            el(ActivityIndicator, { color: IG.accent, size: "small" }),
            el(Text, { style: igStyles.statusText }, t("common.loading", "Loading"))
          ),
          !isTabLoading && tabRows.map((row, index) => el(View, {
            key: String(row.id ?? row.invoiceId ?? row.quoteId ?? row.eventId ?? index),
            style: igStyles.listLine
          },
            el(View, { style: igStyles.listLineCopy },
              el(Text, { style: igStyles.listLineTitle, numberOfLines: 1 }, rowTitle(row)),
              Boolean(rowSubtitle(row)) &&
                el(Text, { style: igStyles.listLineSubtitle, numberOfLines: 1 }, rowSubtitle(row))
            ),
            Boolean(rowAmount(row)) &&
              el(Text, { style: igStyles.listLineValue, numberOfLines: 1 }, rowAmount(row))
          )),
          !isTabLoading && tabRows.length === 0 && el(Text, { style: igStyles.emptyText },
            t("ignition.workspace.tabEmpty", "Nothing here yet for this client."))
        )
      ),
      // Sticky action bar: the two most common counter actions stay one-thumb reachable
      // without scrolling (Report 01 §14 annotation).
      el(View, { style: igStyles.actionBar },
        el(Pressable, { accessibilityRole: "button", style: igStyles.actionSecondary, onPress: sendReminder },
          el(Text, { style: igStyles.actionSecondaryText }, t("ignition.workspace.sendReminder", "Send Reminder"))
        ),
        el(Pressable, { accessibilityRole: "button", style: igStyles.actionPrimary, onPress: () => navigate("invoices") },
          el(Text, { style: igStyles.actionPrimaryText }, t("ignition.workspace.newInvoice", "New Invoice"))
        )
      )
    );
  }

  return el(View, { style: igStyles.screen },
    el(View, { style: igStyles.listHeader },
      el(View, { style: igStyles.titleRow },
        el(Text, { style: igStyles.screenTitle }, t("screens.client-workspace", "Clients")),
        pendingChip
      ),
      isQueuePanelOpen && queueItems.length > 0 && el(PendingSyncPanel, {
        items: queueItems,
        t,
        onSyncNow: syncNow,
        onRetry: retryQueued
      }),
      el(TextInput, {
        style: igStyles.searchInput,
        value: query,
        onChangeText: setQuery,
        placeholder: t("ignition.workspace.searchPlaceholder", "Search clients (name or phone)..."),
        placeholderTextColor: IG.textMuted,
        autoCapitalize: "none",
        returnKeyType: "search"
      }),
      Boolean(listAsOf) && el(View, { style: igStyles.asOfChip },
        el(Text, { style: igStyles.asOfText },
          `${t("ignition.dashboard.asOf", "Offline — as of")} ${shortDateTime(listAsOf)}`)
      ),
      Boolean(status) && el(Text, { style: igStyles.statusText }, status),
      isSearching && el(View, { style: igStyles.loadingRow },
        el(ActivityIndicator, { color: IG.accent, size: "small" }),
        el(Text, { style: igStyles.statusText }, t("common.search", "Search"))
      )
    ),
    el(FlatList, {
      data: clients,
      keyExtractor: (item, index) => String(item.customerId ?? index),
      contentContainerStyle: igStyles.listContent,
      keyboardShouldPersistTaps: "handled",
      renderItem: ({ item }) => el(ClientListRow, { client: item, onPress: () => openClient(item) }),
      ListEmptyComponent: el(View, { style: igStyles.emptyCard },
        el(Text, { style: igStyles.emptyTitle }, t("ignition.workspace.noClientsTitle", "No clients to show")),
        el(Text, { style: igStyles.emptyText },
          query.trim().length >= 2
            ? t("ignition.workspace.noMatches", "No clients match that search.")
            : t("ignition.workspace.noClients", "Clients appear here once they are created."))
      )
    })
  );
}

module.exports = { ClientWorkspaceScreen };
