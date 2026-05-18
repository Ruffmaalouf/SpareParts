const React = require("react");
const { Pressable, ScrollView, Text, View } = require("react-native");
const { money, shortDate } = require("../core/formatters");
const { Field, Panel, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

function toNumber(value, fallback) {
  const parsed = Number(String(value || "").replace(/[^\d.]/g, ""));
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function formatCount(value) {
  return Number(value || 0).toLocaleString();
}

function formatDate(value) {
  return value ? shortDate(value) : "never";
}

function DeadStockScreen({ api }) {
  const { styles, t } = useTheme();
  const [minDays, setMinDays] = useState("90");
  const [take, setTake] = useState("35");
  const [filter, setFilter] = useState("");
  const [report, setReport] = useState(null);
  const [selectedPartId, setSelectedPartId] = useState(null);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const hasLoadedRef = React.useRef(false);

  const load = useCallback(async () => {
    const days = Math.min(720, Math.max(30, toNumber(minDays, 90)));
    const rows = Math.min(100, Math.max(1, toNumber(take, 35)));
    setIsLoading(true);
    setStatus(t("deadStock.loading", "Finding dead stock..."));
    try {
      const nextReport = await api.get(`/api/parts/dead-stock?minDormantDays=${days}&take=${rows}`);
      setReport(nextReport);
      const first = Array.isArray(nextReport.items) ? nextReport.items[0] : null;
      setSelectedPartId(first ? first.partId : null);
      setStatus(
        first
          ? t("deadStock.loaded", "Dead stock candidates loaded.")
          : t("deadStock.empty", "No dead stock found for this threshold.")
      );
    } catch (error) {
      setStatus(error.message || t("deadStock.loadError", "Could not load dead stock."));
    } finally {
      setIsLoading(false);
    }
  }, [api, minDays, take, t]);

  useEffect(() => {
    if (hasLoadedRef.current) return;
    hasLoadedRef.current = true;
    load();
  }, [load]);

  const items = Array.isArray(report?.items) ? report.items : [];
  const visibleItems = useMemo(() => {
    const query = filter.trim().toLowerCase();
    if (!query) return items;
    return items.filter((item) =>
      [
        item.internalCode,
        item.partName,
        item.oemNumber,
        item.primaryAction
      ].some((value) => String(value || "").toLowerCase().includes(query))
    );
  }, [filter, items]);

  const selected = useMemo(() => {
    if (!items.length) return null;
    return items.find((item) => item.partId === selectedPartId) || visibleItems[0] || items[0];
  }, [items, selectedPartId, visibleItems]);

  const totalUnits = useMemo(
    () => items.reduce((sum, item) => sum + Number(item.onHand || 0), 0),
    [items]
  );
  const totalStockValue = useMemo(
    () => items.reduce((sum, item) => sum + Number(item.stockValue || 0), 0),
    [items]
  );
  const currencies = useMemo(
    () => Array.from(new Set(items.map((item) => item.currency || "USD"))),
    [items]
  );
  const valueLabel = currencies.length === 1
    ? money(totalStockValue, currencies[0])
    : t("deadStock.mixedCurrency", "Mixed currency");

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: t("deadStock.title", "Dead Stock"),
      actionTitle: t("common.refresh", "Refresh"),
      onAction: load,
      loading: isLoading
    }),
    el(View, { style: styles.deadStockControls },
      el(View, { style: styles.deadStockControlCell },
        el(Field, {
          label: t("deadStock.minDays", "Dormant days"),
          value: minDays,
          onChangeText: setMinDays,
          keyboardType: "number-pad"
        })
      ),
      el(View, { style: styles.deadStockControlCell },
        el(Field, {
          label: t("deadStock.rows", "Rows"),
          value: take,
          onChangeText: setTake,
          keyboardType: "number-pad"
        })
      )
    ),
    el(Field, {
      label: t("common.filter", "Filter"),
      value: filter,
      onChangeText: setFilter,
      placeholder: t("deadStock.filterPlaceholder", "Code, name, OEM, action")
    }),
    el(StatusText, { value: status }),
    el(View, { style: styles.deadStockMetricGrid },
      el(View, { style: styles.deadStockMetric },
        el(Text, { style: styles.deadStockMetricLabel }, t("deadStock.candidates", "Candidates")),
        el(Text, { style: styles.deadStockMetricValue }, formatCount(items.length)),
        el(Text, { style: styles.deadStockMetricDetail }, `${formatCount(Number(report?.minDormantDays || minDays))}+ days`)
      ),
      el(View, { style: styles.deadStockMetric },
        el(Text, { style: styles.deadStockMetricLabel }, t("deadStock.units", "Units")),
        el(Text, { style: styles.deadStockMetricValue }, formatCount(totalUnits)),
        el(Text, { style: styles.deadStockMetricDetail }, t("deadStock.onShelf", "On shelf"))
      ),
      el(View, { style: styles.deadStockMetric },
        el(Text, { style: styles.deadStockMetricLabel }, t("deadStock.value", "Value")),
        el(Text, { style: styles.deadStockMetricValue }, valueLabel),
        el(Text, { style: styles.deadStockMetricDetail }, t("deadStock.estimated", "Estimated"))
      )
    ),
    el(Panel, { title: t("deadStock.queue", "Recovery Queue") },
      el(View, { style: styles.deadStockQueueFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          visibleItems.map((item) => el(Pressable, {
            key: String(item.partId),
            style: [
              styles.deadStockQueueItem,
              selected?.partId === item.partId && styles.deadStockQueueItemActive
            ],
            onPress: () => setSelectedPartId(item.partId)
          },
            el(View, { style: styles.deadStockQueueTop },
              el(Text, { style: styles.deadStockPartName, numberOfLines: 2 }, item.partName || t("parts.noCode", "Unnamed part")),
              el(Text, { style: styles.deadStockDormant }, `${formatCount(item.dormantDays)}d`)
            ),
            el(Text, { style: styles.deadStockPartMeta }, `${item.internalCode || `PART-${item.partId}`} - OEM ${item.oemNumber || "not set"}`),
            el(View, { style: styles.deadStockQueueBottom },
              el(Text, { style: styles.deadStockStockValue }, money(item.stockValue, item.currency)),
              el(Text, { style: styles.deadStockActionPill }, item.primaryAction || t("deadStock.review", "Review"))
            )
          )),
          visibleItems.length === 0 && el(Text, { style: styles.emptyState }, t("deadStock.none", "No candidates match this filter."))
        )
      )
    ),
    selected && el(Panel, { title: t("deadStock.selected", "Selected Plan") },
      el(View, { style: styles.deadStockSelectedHeader },
        el(View, { style: styles.deadStockSelectedCopy },
          el(Text, { style: styles.deadStockSelectedTitle }, selected.partName),
          el(Text, { style: styles.deadStockSelectedMeta }, `${selected.internalCode || `PART-${selected.partId}`} - OEM ${selected.oemNumber || "not set"}`)
        ),
        el(Text, { style: styles.deadStockSelectedBadge }, selected.primaryAction)
      ),
      el(View, { style: styles.deadStockFacts },
        el(Text, { style: styles.deadStockFact }, `${formatCount(selected.onHand)} on hand`),
        el(Text, { style: styles.deadStockFact }, `${formatCount(selected.availableQuantity)} available`),
        el(Text, { style: styles.deadStockFact }, `${formatCount(selected.dormantDays)} days dormant`),
        el(Text, { style: styles.deadStockFact }, `Last sale ${formatDate(selected.lastSoldAt)}`),
        el(Text, { style: styles.deadStockFact }, `Last receipt ${formatDate(selected.lastReceivedAt)}`),
        el(Text, { style: styles.deadStockFact }, `Sale ${money(selected.salePrice, selected.currency)}`)
      ),
      el(View, { style: styles.deadStockActions },
        (selected.suggestedActions || []).map((action) => el(View, { key: action.key || action.label, style: styles.deadStockActionCard },
          el(View, { style: styles.deadStockActionHeader },
            el(Text, { style: styles.deadStockActionTitle }, action.label),
            el(Text, { style: styles.deadStockActionTone }, action.tone || "Neutral")
          ),
          el(Text, { style: styles.deadStockActionDetail }, action.detail)
        ))
      ),
      el(SecondaryButton, { title: t("deadStock.reload", "Refresh Candidates"), onPress: load })
    )
  );
}

module.exports = { DeadStockScreen };
