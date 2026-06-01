import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

function read(row, ...keys) {
  for (const key of keys) {
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") return value;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row?.[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }
  return "";
}

function toPositiveNumber(value, fallback) {
  const parsed = Number(String(value || "").replace(/[^\d.]/g, ""));
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

function formatCount(value) {
  return Number(value || 0).toLocaleString();
}

function formatDate(value) {
  return value ? shortDate(value) : "Never";
}

function itemId(item) {
  return read(item, "partId", "id");
}

function partCode(item) {
  return read(item, "internalCode") || `PART-${itemId(item) || ""}`.trim();
}

function matchesItem(item, query) {
  if (!query) return true;
  return [
    read(item, "internalCode"),
    read(item, "partName", "name"),
    read(item, "oemNumber"),
    read(item, "primaryAction")
  ].join(" ").toLowerCase().includes(query);
}

function MetricTile({ label, value, detail }) {
  return h("article", { className: "dead-stock-metric" },
    h("span", null, label),
    h("strong", null, value),
    detail && h("small", null, detail)
  );
}

function ActionCard({ action }) {
  return h("article", { className: "dead-stock-action-card" },
    h("header", null,
      h("strong", null, read(action, "label") || "Review"),
      h("span", null, read(action, "tone") || "Neutral")
    ),
    h("p", null, read(action, "detail") || "Review this item before the next inventory cycle.")
  );
}

export function DeadStockView({ api, t }) {
  const [minDays, setMinDays] = useState("90");
  const [take, setTake] = useState("35");
  const [search, setSearch] = useState("");
  const [report, setReport] = useState(null);
  const [selectedPartId, setSelectedPartId] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    const days = Math.min(720, Math.max(30, toPositiveNumber(minDays, 90)));
    const rows = Math.min(100, Math.max(1, toPositiveNumber(take, 35)));
    setIsLoading(true);
    setStatus(t("deadStock.loading", "Finding dead stock..."));
    try {
      const nextReport = await api.get(`/api/parts/dead-stock?minDormantDays=${days}&take=${rows}`);
      const items = Array.isArray(nextReport?.items) ? nextReport.items : [];
      setReport(nextReport);
      setSelectedPartId((current) => {
        if (current && items.some((item) => String(itemId(item)) === String(current))) return current;
        return String(itemId(items[0]) || "");
      });
      setStatus(items.length
        ? t("deadStock.loaded", "Dead stock candidates loaded.")
        : t("deadStock.empty", "No dead stock found for this threshold."));
    } catch (error) {
      setReport(null);
      setSelectedPartId("");
      setStatus(error.message || t("deadStock.loadError", "Could not load dead stock."));
    } finally {
      setIsLoading(false);
    }
  }, [api, minDays, take, t]);

  useEffect(() => {
    load();
  }, [load]);

  const items = Array.isArray(report?.items) ? report.items : [];
  const visibleItems = useMemo(() => {
    const query = search.trim().toLowerCase();
    return items.filter((item) => matchesItem(item, query));
  }, [items, search]);

  const selected = useMemo(() => {
    if (!items.length) return null;
    return items.find((item) => String(itemId(item)) === String(selectedPartId)) || visibleItems[0] || items[0];
  }, [items, selectedPartId, visibleItems]);

  const totals = useMemo(() => {
    const currencies = new Set();
    const summary = items.reduce((acc, item) => {
      acc.units += Number(read(item, "onHand") || 0);
      acc.value += Number(read(item, "stockValue") || 0);
      currencies.add(read(item, "currency") || "USD");
      return acc;
    }, { units: 0, value: 0 });
    return {
      ...summary,
      currencyLabel: currencies.size === 1 ? money(summary.value, [...currencies][0]) : t("deadStock.mixedCurrency", "Mixed currency")
    };
  }, [items, t]);

  return h("section", { className: "screen dead-stock-screen" },
    h(PageHeader, {
      kicker: "Inventory",
      title: t("deadStock.title", "Dead Stock"),
      subtitle: t("deadStock.subtitle", "Find dormant inventory and choose a recovery action before it eats shelf space."),
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: load,
        disabled: isLoading
      }, isLoading ? t("common.loading", "Loading") : t("common.refresh", "Refresh"))
    }),
    h("section", { className: "dead-stock-controls" },
      h("label", null,
        h("span", null, t("deadStock.minDays", "Dormant days")),
        h("input", {
          value: minDays,
          inputMode: "numeric",
          onChange: (event) => setMinDays(event.target.value)
        })
      ),
      h("label", null,
        h("span", null, t("deadStock.rows", "Rows")),
        h("input", {
          value: take,
          inputMode: "numeric",
          onChange: (event) => setTake(event.target.value)
        })
      ),
      h("label", { className: "dead-stock-search" },
        h("span", null, t("common.search", "Search")),
        h("input", {
          value: search,
          onChange: (event) => setSearch(event.target.value),
          placeholder: t("deadStock.filterPlaceholder", "Code, name, OEM, action")
        })
      )
    ),
    h(StatusLine, { status }),
    h("section", { className: "dead-stock-metrics" },
      h(MetricTile, { label: t("deadStock.candidates", "Candidates"), value: formatCount(items.length), detail: `${formatCount(read(report, "minDormantDays") || minDays)}+ days` }),
      h(MetricTile, { label: t("deadStock.units", "Units"), value: formatCount(totals.units), detail: t("deadStock.onShelf", "On shelf") }),
      h(MetricTile, { label: t("deadStock.value", "Value"), value: totals.currencyLabel, detail: t("deadStock.estimated", "Estimated") })
    ),
    h("section", { className: "dead-stock-layout" },
      h("aside", { className: "panel dead-stock-queue" },
        h("div", { className: "admin-panel-header" },
          h("h3", null, t("deadStock.queue", "Recovery Queue")),
          h("span", null, `${visibleItems.length}`)
        ),
        h("div", { className: "dead-stock-list" },
          visibleItems.map((item) => {
            const active = String(itemId(item)) === String(itemId(selected));
            return h("button", {
              key: itemId(item),
              className: active ? "dead-stock-row active" : "dead-stock-row",
              type: "button",
              onClick: () => setSelectedPartId(String(itemId(item)))
            },
              h("span", { className: "dead-stock-row-main" },
                h("strong", null, read(item, "partName", "name") || "Unnamed part"),
                h("small", null, `${partCode(item)} / OEM ${read(item, "oemNumber") || "not set"}`)
              ),
              h("span", { className: "dead-stock-row-side" },
                h("b", null, `${formatCount(read(item, "dormantDays"))}d`),
                h("small", null, money(read(item, "stockValue"), read(item, "currency") || "USD"))
              ),
              h("em", null, read(item, "primaryAction") || t("deadStock.review", "Review"))
            );
          }),
          visibleItems.length === 0 && h("p", { className: "empty-state" }, t("deadStock.none", "No candidates match this filter."))
        )
      ),
      h("main", { className: "dead-stock-detail" },
        selected
          ? h("section", { className: "panel dead-stock-selected" },
            h("div", { className: "dead-stock-selected-header" },
              h("div", null,
                h("span", null, t("deadStock.selected", "Selected Plan")),
                h("h3", null, read(selected, "partName", "name") || "Unnamed part"),
                h("p", null, `${partCode(selected)} / OEM ${read(selected, "oemNumber") || "not set"}`)
              ),
              h("strong", null, read(selected, "primaryAction") || t("deadStock.review", "Review"))
            ),
            h("div", { className: "detail-grid" },
              h("div", { className: "detail-tile" }, h("span", null, "On hand"), h("strong", null, formatCount(read(selected, "onHand")))),
              h("div", { className: "detail-tile" }, h("span", null, "Available"), h("strong", null, formatCount(read(selected, "availableQuantity")))),
              h("div", { className: "detail-tile" }, h("span", null, "Dormant"), h("strong", null, `${formatCount(read(selected, "dormantDays"))} days`)),
              h("div", { className: "detail-tile" }, h("span", null, "Last sale"), h("strong", null, formatDate(read(selected, "lastSoldAt")))),
              h("div", { className: "detail-tile" }, h("span", null, "Last receipt"), h("strong", null, formatDate(read(selected, "lastReceivedAt")))),
              h("div", { className: "detail-tile" }, h("span", null, "Sale price"), h("strong", null, money(read(selected, "salePrice"), read(selected, "currency") || "USD")))
            ),
            h("section", { className: "dead-stock-actions" },
              (read(selected, "suggestedActions") || []).map((action) =>
                h(ActionCard, { key: read(action, "key") || read(action, "label"), action })
              ),
              (!read(selected, "suggestedActions") || read(selected, "suggestedActions").length === 0) &&
                h("p", { className: "empty-state" }, t("deadStock.noActions", "No suggested actions returned."))
            )
          )
          : h("section", { className: "panel dead-stock-empty" },
            h("h3", null, t("deadStock.noSelection", "No candidate selected")),
            h("p", null, t("deadStock.noSelectionHint", "Refresh the report or lower the dormant-day threshold."))
          )
      )
    )
  );
}
