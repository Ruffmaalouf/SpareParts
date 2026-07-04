const React = require("react");
const { Pressable, ScrollView, StyleSheet, Text, View, useWindowDimensions } = require("react-native");
const AsyncStorage = require("@react-native-async-storage/async-storage").default;
const SvgModule = require("react-native-svg");
const { money, shortDateTime } = require("../core/formatters");
const { offlineWriteQueue } = require("../core/offline-write-queue");
const { ignitionTokens } = require("../theme/ignition-theme");
const { useTheme } = require("../theme/theme-context");

const Svg = SvgModule.default;
const { Circle, Defs, LinearGradient, Path, Stop } = SvgModule;

const { useCallback, useEffect, useState } = React;
const el = React.createElement;
const IG = ignitionTokens.colors;
const MIN_TAP_TARGET = ignitionTokens.layout.minTouchTarget;

// Ignition phone dashboard per Report 01 §12 (Figure 3 — Dashboard, Mobile <=428px):
// single-column stack, the Action Queue is the very first element (non-negotiable across
// breakpoints), the KPI band is a horizontally-scrollable snap carousel (Report 03 §8,
// phone breakpoint), followed by the sales trend and quick actions. Data comes from the
// composed GET /api/dashboard/summary endpoint exactly as documented in Report 04 §02-§03.
// Reads are cache-first per Report 05 §7: the last good payload renders instantly with an
// "as of HH:MM" badge while a background revalidation refreshes it.

const summaryCacheKey = "spareparts.mobile.ignition.dashboardSummaryCache";

// Report 04 §03 deepLink values are web-style routes ("/accounting?filter=unpaid");
// map their first path segment onto this app's screen-registry keys.
const deepLinkScreenKeys = {
  accounting: "accounting",
  stock: "stock",
  whatsapp: "whatsapp",
  reorder: "reorder",
  invoices: "invoices",
  parts: "parts",
  clients: "client-workspace"
};

const actionTypeScreenKeys = {
  receivables: "accounting",
  inventory: "stock",
  messages: "whatsapp"
};

function screenKeyForAction(item) {
  const segment = String(item?.deepLink || "").split("?")[0].replace(/^\/+/, "").split("/")[0];
  return deepLinkScreenKeys[segment] || actionTypeScreenKeys[item?.type] || "ignition-dashboard";
}

function severityColor(severity) {
  const normalized = String(severity || "").toLowerCase();
  if (normalized === "danger") return IG.danger;
  if (normalized === "warning") return IG.warning;
  if (normalized === "success") return IG.success;
  return IG.info;
}

function directionGlyph(direction) {
  const normalized = String(direction || "flat").toLowerCase();
  if (normalized === "up") return "▲";
  if (normalized === "down") return "▼";
  return "—";
}

function directionColor(direction) {
  const normalized = String(direction || "flat").toLowerCase();
  if (normalized === "up") return IG.success;
  if (normalized === "down") return IG.danger;
  return IG.textMuted;
}

function formatDelta(kpi) {
  const delta = kpi?.deltaPercent;
  if (delta == null || !Number.isFinite(Number(delta))) return "";
  return `${Math.abs(Number(delta)).toFixed(1)}%`;
}

function formatCount(value) {
  const number = Number(value || 0);
  return Number.isFinite(number) ? number.toLocaleString(undefined, { maximumFractionDigits: 0 }) : "0";
}

function trendPaths(series, width, height) {
  const amounts = series.map((point) => Number(point?.amount || 0));
  const max = Math.max(...amounts, 1);
  const pad = 10;
  const step = amounts.length > 1 ? width / (amounts.length - 1) : width;
  const points = amounts.map((value, index) => ({
    x: Math.round(index * step),
    y: Math.round(height - pad - (value / max) * (height - pad * 2))
  }));
  const line = points.map((point, index) => `${index === 0 ? "M" : "L"}${point.x},${point.y}`).join(" ");
  const area = points.length
    ? `${line} L${points[points.length - 1].x},${height} L${points[0].x},${height} Z`
    : "";
  return { line, area, points };
}

const igStyles = StyleSheet.create({
  root: {
    backgroundColor: IG.bgCanvas,
    paddingHorizontal: ignitionTokens.spacing.space4,
    paddingTop: 10,
    paddingBottom: 28,
    gap: 14
  },
  headerRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 12
  },
  headerCopy: {
    flex: 1,
    minWidth: 0
  },
  hello: {
    color: IG.textSecondary,
    fontSize: 12,
    fontWeight: "600"
  },
  title: {
    color: IG.textPrimary,
    fontSize: 24,
    fontWeight: "800",
    letterSpacing: -0.4,
    marginTop: 2
  },
  refreshButton: {
    minWidth: 84,
    minHeight: MIN_TAP_TARGET,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: 14,
    borderRadius: ignitionTokens.radii.pill,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgElevated
  },
  refreshText: {
    color: IG.textSecondary,
    fontSize: 12,
    fontWeight: "800"
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
  sectionLabel: {
    color: IG.textMuted,
    fontSize: 11,
    fontWeight: "800",
    letterSpacing: 1.1,
    textTransform: "uppercase"
  },
  queuePanel: {
    gap: 9
  },
  queueRow: {
    minHeight: 64,
    flexDirection: "row",
    alignItems: "center",
    gap: 11,
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface,
    padding: 12,
    ...ignitionTokens.shadows.l1
  },
  queueMark: {
    width: 4,
    alignSelf: "stretch",
    borderRadius: ignitionTokens.radii.pill
  },
  queueCopy: {
    flex: 1,
    minWidth: 0,
    gap: 2
  },
  queueTitle: {
    color: IG.textPrimary,
    fontSize: 14,
    fontWeight: "800"
  },
  queueDetail: {
    color: IG.textSecondary,
    fontSize: 12,
    lineHeight: 17
  },
  queueAmount: {
    maxWidth: 104,
    color: IG.textPrimary,
    fontSize: 12,
    fontWeight: "800",
    textAlign: "right"
  },
  queueBadge: {
    borderRadius: ignitionTokens.radii.pill,
    overflow: "hidden",
    paddingHorizontal: 8,
    paddingVertical: 3,
    fontSize: 10,
    fontWeight: "900",
    textTransform: "uppercase"
  },
  kpiRail: {
    marginHorizontal: -ignitionTokens.spacing.space4
  },
  kpiRailContent: {
    paddingHorizontal: ignitionTokens.spacing.space4,
    gap: 10
  },
  kpiCard: {
    minHeight: 108,
    justifyContent: "space-between",
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgElevated,
    padding: 14,
    ...ignitionTokens.shadows.l1
  },
  kpiTopRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 8
  },
  kpiLabel: {
    flex: 1,
    minWidth: 0,
    color: IG.textMuted,
    fontSize: 10,
    fontWeight: "800",
    letterSpacing: 0.7,
    textTransform: "uppercase"
  },
  kpiDelta: {
    fontSize: 11,
    fontWeight: "800"
  },
  kpiValue: {
    color: IG.textPrimary,
    fontSize: 22,
    fontWeight: "800",
    letterSpacing: -0.5
  },
  kpiHint: {
    color: IG.accent,
    fontSize: 10,
    fontWeight: "800",
    textTransform: "uppercase",
    letterSpacing: 0.5
  },
  trendCard: {
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgSurface,
    padding: 14,
    gap: 8,
    ...ignitionTokens.shadows.l1
  },
  trendHeadRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    gap: 10
  },
  trendTitle: {
    color: IG.textPrimary,
    fontSize: 15,
    fontWeight: "800"
  },
  trendRange: {
    color: IG.accent,
    fontSize: 11,
    fontWeight: "800"
  },
  trendPeak: {
    color: IG.textSecondary,
    fontSize: 12
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
  quickGrid: {
    flexDirection: "row",
    gap: 10
  },
  quickButton: {
    flex: 1,
    minHeight: 76,
    justifyContent: "space-between",
    gap: 6,
    borderRadius: ignitionTokens.radii.lg,
    borderWidth: 1,
    borderColor: IG.border,
    backgroundColor: IG.bgElevated,
    padding: 12
  },
  quickButtonPrimary: {
    borderColor: IG.accent,
    backgroundColor: "rgba(249,115,22,0.14)"
  },
  quickTitle: {
    color: IG.textPrimary,
    fontSize: 13,
    fontWeight: "800"
  },
  quickHint: {
    color: IG.textMuted,
    fontSize: 10,
    fontWeight: "700",
    textTransform: "uppercase",
    letterSpacing: 0.4
  }
});

function IgnitionActionQueueRow({ item, currencyCode, onPress }) {
  const color = severityColor(item.severity);
  const amount = item.amount != null ? money(item.amount, item.currencyCode || currencyCode) : "";

  return el(Pressable, {
    accessibilityRole: "button",
    accessibilityLabel: `${item.title}. ${item.detail || ""}`,
    style: igStyles.queueRow,
    onPress
  },
    el(View, { style: [igStyles.queueMark, { backgroundColor: color }] }),
    el(View, { style: igStyles.queueCopy },
      el(Text, {
        style: [igStyles.queueBadge, { alignSelf: "flex-start", color, backgroundColor: `${color}1f` }]
      }, String(item.type || item.severity || "task")),
      el(Text, { style: igStyles.queueTitle, numberOfLines: 1 }, item.title),
      Boolean(item.detail) && el(Text, { style: igStyles.queueDetail, numberOfLines: 2 }, item.detail)
    ),
    Boolean(amount) && el(Text, { style: igStyles.queueAmount, numberOfLines: 1 }, amount)
  );
}

function IgnitionKpiCard({ label, value, delta, direction, hint, width, onPress }) {
  return el(Pressable, {
    accessibilityRole: "button",
    accessibilityLabel: `${label}: ${value}`,
    style: [igStyles.kpiCard, { width }],
    onPress
  },
    el(View, { style: igStyles.kpiTopRow },
      el(Text, { style: igStyles.kpiLabel, numberOfLines: 1 }, label),
      Boolean(delta) && el(Text, {
        style: [igStyles.kpiDelta, { color: directionColor(direction) }]
      }, `${directionGlyph(direction)} ${delta}`)
    ),
    el(Text, { style: igStyles.kpiValue, numberOfLines: 1, adjustsFontSizeToFit: true }, value),
    el(Text, { style: igStyles.kpiHint, numberOfLines: 1 }, hint)
  );
}

function IgnitionSalesTrendCard({ salesTrend, currencyCode, t }) {
  const series = Array.isArray(salesTrend?.series) ? salesTrend.series : [];
  if (series.length === 0) {
    // Empty states never show a blank panel (Report 02 §12): one-line explanation instead.
    return el(View, { style: igStyles.emptyCard },
      el(Text, { style: igStyles.emptyTitle }, t("ignition.dashboard.noTrendTitle", "No sales recorded yet today")),
      el(Text, { style: igStyles.emptyText }, t("ignition.dashboard.noTrendText", "Chart fills in as invoices are posted"))
    );
  }

  const chartW = 320;
  const chartH = 96;
  const chart = trendPaths(series, chartW, chartH);
  const peakIndex = series.findIndex((point) => point?.date === salesTrend?.peakDate);
  const rangeDays = Number(salesTrend?.rangeDays || series.length);

  return el(View, { style: igStyles.trendCard },
    el(View, { style: igStyles.trendHeadRow },
      el(Text, { style: igStyles.trendTitle }, t("ignition.dashboard.salesTrend", "Sales — This Week")),
      el(Text, { style: igStyles.trendRange }, t("ignition.dashboard.trendRange", `${rangeDays}d`))
    ),
    el(Svg, { width: "100%", height: chartH, viewBox: `0 0 ${chartW} ${chartH}` },
      el(Defs, null,
        el(LinearGradient, { id: "igTrendArea", x1: "0", y1: "0", x2: "0", y2: "1" },
          el(Stop, { offset: "0", stopColor: IG.accent, stopOpacity: "0.34" }),
          el(Stop, { offset: "1", stopColor: IG.accent, stopOpacity: "0" })
        )
      ),
      el(Path, { d: chart.area, fill: "url(#igTrendArea)" }),
      el(Path, { d: chart.line, fill: "none", stroke: IG.accent, strokeWidth: 2.5, strokeLinecap: "round", strokeLinejoin: "round" }),
      chart.points.map((point, index) => el(Circle, {
        key: `${point.x}-${point.y}`,
        cx: point.x,
        cy: point.y,
        r: index === peakIndex ? 4 : 2,
        fill: IG.bgSurface,
        stroke: index === peakIndex ? IG.warning : IG.accent,
        strokeWidth: 2
      }))
    ),
    salesTrend?.peakAmount != null && el(Text, { style: igStyles.trendPeak, numberOfLines: 1 },
      `${t("ignition.dashboard.peak", "Peak")} ${salesTrend.peakDate || ""} · ${money(salesTrend.peakAmount, currencyCode)}`
    )
  );
}

function IgnitionDashboardScreen({ api, onNavigate, user }) {
  const { t } = useTheme();
  const { width } = useWindowDimensions();
  const [summary, setSummary] = useState(null);
  const [asOf, setAsOf] = useState(null);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const navigate = useCallback((key) => {
    if (typeof onNavigate === "function") onNavigate(key);
  }, [onNavigate]);

  const readCache = useCallback(async () => {
    try {
      const raw = await AsyncStorage.getItem(summaryCacheKey);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }, []);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const nextSummary = await api.get("/api/dashboard/summary");
      setSummary(nextSummary);
      setAsOf(null);
      setStatus("");
      await AsyncStorage.setItem(summaryCacheKey, JSON.stringify({
        summary: nextSummary,
        cachedAt: new Date().toISOString()
      })).catch(() => {});
      // Network is back: drain any queued offline counter writes (Report 05 §7).
      offlineWriteQueue.drain(api).catch(() => {});
    } catch (error) {
      const cached = await readCache();
      if (cached && cached.summary) {
        setSummary(cached.summary);
        setAsOf(cached.cachedAt);
        setStatus("");
      } else {
        setStatus(error.message || t("ignition.dashboard.loadError", "Could not load the dashboard."));
      }
    } finally {
      setIsLoading(false);
    }
  }, [api, readCache, t]);

  useEffect(() => {
    let isMounted = true;
    // Cache-first read path (Report 05 §7): render the last snapshot instantly with an
    // "as of" badge, then revalidate in the background.
    readCache().then((cached) => {
      if (isMounted && cached && cached.summary) {
        setSummary(cached.summary);
        setAsOf(cached.cachedAt);
      }
    });
    load();
    return () => {
      isMounted = false;
    };
  }, [load, readCache]);

  const currencyCode = summary?.currencyCode || "USD";
  const moneyOf = useCallback((value) => money(value, currencyCode), [currencyCode]);
  const kpis = summary?.kpis || {};
  const actionQueue = Array.isArray(summary?.actionQueue) ? summary.actionQueue : [];
  const userName = String(user?.fullName || user?.username || user?.name || "").trim();
  const kpiCardWidth = Math.min(Math.max(Math.round(width * 0.56), 176), 240);

  // KPI order per the Report 04 §03 contract block.
  const kpiCards = [
    { key: "salesToday", label: t("ignition.dashboard.kpiSales", "Sales Today"), kpi: kpis.salesToday, format: moneyOf, route: "invoices" },
    { key: "profitToday", label: t("ignition.dashboard.kpiProfit", "Profit Today"), kpi: kpis.profitToday, format: moneyOf, route: "accounting" },
    { key: "cashBalance", label: t("ignition.dashboard.kpiCash", "Cash Balance"), kpi: kpis.cashBalance, format: moneyOf, route: "accounting" },
    { key: "receivables", label: t("ignition.dashboard.kpiReceivables", "Receivables"), kpi: kpis.receivables, format: moneyOf, route: "accounting" },
    { key: "stockValue", label: t("ignition.dashboard.kpiStockValue", "Stock Value"), kpi: kpis.stockValue, format: moneyOf, route: "stock" },
    { key: "lowStockCount", label: t("ignition.dashboard.kpiLowStock", "Low Stock"), kpi: kpis.lowStockCount, format: formatCount, route: "reorder" }
  ];

  const quickActions = [
    { key: "new-sale", title: t("ignition.dashboard.newSale", "New Sale"), hint: t("ignition.dashboard.newSaleHint", "POS"), route: "invoices", primary: true },
    { key: "find-part", title: t("ignition.dashboard.findPart", "Find Part"), hint: t("ignition.dashboard.findPartHint", "Inventory"), route: "parts" },
    { key: "new-client", title: t("ignition.dashboard.newClient", "New Client"), hint: t("ignition.dashboard.newClientHint", "Clients"), route: "client-workspace" }
  ];

  return el(ScrollView, {
    style: { flex: 1, backgroundColor: IG.bgCanvas },
    contentContainerStyle: igStyles.root,
    showsVerticalScrollIndicator: false
  },
    el(View, { style: igStyles.headerRow },
      el(View, { style: igStyles.headerCopy },
        el(Text, { style: igStyles.hello, numberOfLines: 1 },
          userName
            ? `${t("ignition.dashboard.hello", "Hi")}, ${userName}`
            : t("ignition.dashboard.welcome", "Welcome back")),
        el(Text, { style: igStyles.title, numberOfLines: 1 }, t("screens.ignition-dashboard", "Dashboard"))
      ),
      el(Pressable, { accessibilityRole: "button", style: igStyles.refreshButton, onPress: load },
        el(Text, { style: igStyles.refreshText },
          isLoading ? t("dashboard.loadingShort", "...") : t("common.refresh", "Refresh"))
      )
    ),
    Boolean(asOf) && el(View, { style: igStyles.asOfChip },
      el(Text, { style: igStyles.asOfText },
        `${t("ignition.dashboard.asOf", "Offline — as of")} ${shortDateTime(asOf)}`)
    ),
    Boolean(status) && el(Text, { style: igStyles.statusText }, status),

    // 1 — Action Queue: always the first block (Report 01 §12, non-negotiable).
    el(Text, { style: igStyles.sectionLabel }, t("ignition.dashboard.actionQueue", "Action Queue")),
    el(View, { style: igStyles.queuePanel },
      actionQueue.length > 0
        ? actionQueue.map((item) => el(IgnitionActionQueueRow, {
          key: item.id || item.type,
          item,
          currencyCode,
          onPress: () => navigate(screenKeyForAction(item))
        }))
        : el(View, { style: igStyles.emptyCard },
          el(Text, { style: igStyles.emptyTitle }, t("ignition.dashboard.queueEmptyTitle", "Critical signals are quiet")),
          el(Text, { style: igStyles.emptyText }, t("ignition.dashboard.queueEmptyText", "No receivables, inventory, or message exceptions right now."))
        )
    ),

    // 2 — KPI carousel: single-row horizontal scroll-snap strip (Report 03 §8, phone).
    el(Text, { style: igStyles.sectionLabel }, t("ignition.dashboard.kpiRail", "KPI · Swipe")),
    el(ScrollView, {
      horizontal: true,
      showsHorizontalScrollIndicator: false,
      decelerationRate: "fast",
      snapToInterval: kpiCardWidth + 10,
      snapToAlignment: "start",
      style: igStyles.kpiRail,
      contentContainerStyle: igStyles.kpiRailContent
    },
      kpiCards.map((card) => el(IgnitionKpiCard, {
        key: card.key,
        label: card.label,
        value: card.format(card.kpi?.value ?? 0),
        delta: formatDelta(card.kpi),
        direction: card.kpi?.direction,
        hint: t("ignition.dashboard.tapToDrill", "Tap to drill"),
        width: kpiCardWidth,
        onPress: () => navigate(card.route)
      }))
    ),

    // 3 — Sales trend from the contract's salesTrend block.
    el(IgnitionSalesTrendCard, { salesTrend: summary?.salesTrend, currencyCode, t }),

    // 4 — Quick actions (Report 01 §12: New Sale / Find Part / New Client).
    el(Text, { style: igStyles.sectionLabel }, t("ignition.dashboard.quickActions", "Quick Actions")),
    el(View, { style: igStyles.quickGrid },
      quickActions.map((action) => el(Pressable, {
        key: action.key,
        accessibilityRole: "button",
        style: [igStyles.quickButton, action.primary && igStyles.quickButtonPrimary],
        onPress: () => navigate(action.route)
      },
        el(Text, { style: igStyles.quickTitle, numberOfLines: 1 }, action.title),
        el(Text, { style: igStyles.quickHint, numberOfLines: 1 }, action.hint)
      ))
    )
  );
}

module.exports = { IgnitionDashboardScreen };
