import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { asRows, money } from "../core/formatters.js";
import { CommunicationPayloadFactory } from "../services/communication-payload-factory.js";
import { PricingCoachSignal, smartPricingCoach, waitingCustomersByPart } from "../services/pricing-coach.js";
import { Badge, DataTable, EmptyState, PageHeader, StatusLine } from "../components/shared.js";
import { selectPartPassport } from "./part-passport-workspace-view.js";

const STOCK_FILTER_OPTIONS = [
  { value: "all",      label: "All Parts" },
  { value: "in",       label: "In Stock" },
  { value: "low",      label: "Low Stock" },
  { value: "out",      label: "Out of Stock" },
  { value: "no-price", label: "No Sale Price" },
  { value: "no-oem",   label: "No OEM Number" }
];

function stockStatus(part) {
  const qty = part.availableQuantity ?? part.quantity ?? 0;
  const min = part.minStock ?? part.minimumStock ?? 0;
  if (qty <= 0) return "out";
  if (min > 0 && qty <= min) return "low";
  return "in";
}

function applyStockFilter(parts, stockFilter) {
  switch (stockFilter) {
    case "in":       return parts.filter((p) => stockStatus(p) === "in");
    case "low":      return parts.filter((p) => stockStatus(p) === "low");
    case "out":      return parts.filter((p) => stockStatus(p) === "out");
    case "no-price": return parts.filter((p) => !(p.salePrice > 0));
    case "no-oem":   return parts.filter((p) => !p.oemNumber);
    default:         return parts;
  }
}

function StockBadge({ part }) {
  const status = stockStatus(part);
  const qty = part.availableQuantity ?? part.quantity ?? 0;
  const tone = status === "out" ? "danger" : status === "low" ? "warning" : "success";
  const label = status === "out" ? "Out" : status === "low" ? "Low" : "In";
  return h("div", { className: "stock-cell" },
    h(Badge, { tone }, label),
    h("span", { className: "stock-qty" }, `×${qty}`)
  );
}

function InventoryHealthBar({ parts, activeFilter, onFilter }) {
  const noPriceCount = parts.filter((p) => !(p.salePrice > 0)).length;
  const noOemCount   = parts.filter((p) => !p.oemNumber).length;
  const outCount     = parts.filter((p) => stockStatus(p) === "out").length;
  const lowCount     = parts.filter((p) => stockStatus(p) === "low").length;

  const chips = [
    { filter: "out",      count: outCount,     label: "Out of stock",   tone: "danger" },
    { filter: "low",      count: lowCount,     label: "Low stock",      tone: "warning" },
    { filter: "no-price", count: noPriceCount, label: "No price",       tone: "warning" },
    { filter: "no-oem",   count: noOemCount,   label: "No OEM",         tone: "muted" }
  ].filter((chip) => chip.count > 0);

  if (chips.length === 0) return null;

  return h("div", { className: "inventory-health-bar" },
    h("span", { className: "health-bar-label" }, "Alerts:"),
    chips.map((chip) =>
      h("button", {
        key: chip.filter,
        type: "button",
        className: `health-chip health-chip-${chip.tone}${activeFilter === chip.filter ? " active" : ""}`,
        onClick: () => onFilter(activeFilter === chip.filter ? "all" : chip.filter)
      }, `${chip.count} ${chip.label}`)
    )
  );
}

function buildWhatsAppText(part) {
  const qty  = part.availableQuantity ?? part.quantity ?? 0;
  const name = part.name || part.internalCode;
  const oem  = part.oemNumber ? ` | OEM: ${part.oemNumber}` : "";
  const price = part.salePrice > 0 ? ` | Price: ${money(part.salePrice, part.currency)}` : "";
  return `${name}${oem}${price} | Qty: ${qty}`;
}

export function InventoryView({ api, onView }) {
  const [parts, setParts]             = useState([]);
  const [partRequests, setPartRequests] = useState([]);
  const [filter, setFilter]           = useState("");
  const [stockFilter, setStockFilter] = useState("all");
  const [phone, setPhone]             = useState("");
  const [name, setName]               = useState("");
  const [status, setStatus]           = useState("");
  const [isLoading, setIsLoading]     = useState(false);
  const [copyFeedback, setCopyFeedback] = useState(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading inventory...");
    try {
      const [nextParts, nextRequests] = await Promise.all([
        api.list("/api/parts"),
        api.get("/api/partrequests?status=Active").catch(() => [])
      ]);
      setParts(asRows(nextParts));
      setPartRequests(asRows(nextRequests));
      setStatus("");
    } catch (error) {
      setPartRequests([]);
      setStatus(error.message || "Could not load inventory.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const textFilteredParts = useMemo(() => {
    const query = filter.trim().toLowerCase();
    if (!query) return parts;
    return parts.filter((part) =>
      [part.internalCode, part.name, part.oemNumber, part.currency].some((value) =>
        String(value || "").toLowerCase().includes(query)
      )
    );
  }, [parts, filter]);

  const visibleParts = useMemo(
    () => applyStockFilter(textFilteredParts, stockFilter),
    [textFilteredParts, stockFilter]
  );

  const displayedParts = useMemo(() => visibleParts.slice(0, 150), [visibleParts]);

  const waitingByPart = useMemo(
    () => waitingCustomersByPart(partRequests),
    [partRequests]
  );

  const copyShare = useCallback((part) => {
    const text = buildWhatsAppText(part);
    window.navigator?.clipboard?.writeText(text).then(() => {
      setCopyFeedback(part.id);
      window.setTimeout(() => setCopyFeedback(null), 1800);
    }).catch(() => {
      setStatus("Clipboard unavailable. Copy manually: " + text);
    });
  }, []);

  const sendAvailability = useCallback(async (part) => {
    if (!phone.trim()) {
      setStatus("Enter a WhatsApp phone number first.");
      return;
    }
    setStatus("Sending part availability...");
    try {
      await api.post("/api/communications/send", CommunicationPayloadFactory.partAvailability({
        recipientName: name || "Customer",
        recipientPhone: phone,
        part
      }));
      setStatus("Part availability sent.");
    } catch (error) {
      setStatus(error.message || "Could not send availability.");
    }
  }, [api, name, phone]);

  const emptyMessage = isLoading
    ? null
    : stockFilter !== "all"
      ? `No parts match the "${STOCK_FILTER_OPTIONS.find((o) => o.value === stockFilter)?.label}" filter.`
      : filter.trim()
        ? `No parts match "${filter.trim()}".`
        : "No parts in inventory yet.";

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Inventory",
      action: h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h("div", { className: "filters-row" },
      h("input", {
        value: filter,
        onChange: (event) => setFilter(event.target.value),
        placeholder: "Filter by code, name, OEM"
      }),
      h("select", {
        className: "stock-filter-select",
        value: stockFilter,
        onChange: (event) => setStockFilter(event.target.value)
      },
        STOCK_FILTER_OPTIONS.map((opt) =>
          h("option", { key: opt.value, value: opt.value }, opt.label)
        )
      ),
      h("input", { value: name, onChange: (event) => setName(event.target.value), placeholder: "Recipient name" }),
      h("input", { value: phone, onChange: (event) => setPhone(event.target.value), placeholder: "WhatsApp phone" })
    ),
    h(InventoryHealthBar, {
      parts: textFilteredParts,
      activeFilter: stockFilter,
      onFilter: setStockFilter
    }),
    h(StatusLine, { status }),
    isLoading
      ? h("p", { className: "loading-pill" }, "Loading inventory…")
      : h("section", { className: "table-panel" },
          displayedParts.length === 0
            ? h(EmptyState, {
                title: emptyMessage,
                subtitle: stockFilter !== "all" ? "Click the chip above or change the filter to see other parts." : null,
                action: stockFilter !== "all"
                  ? h("button", { type: "button", className: "secondary-button", onClick: () => setStockFilter("all") }, "Clear filter")
                  : null
              })
            : h(DataTable, {
                columns: [
                  { key: "code",  label: "Code",  render: (part) => part.internalCode },
                  { key: "part",  label: "Part",  render: (part) => part.name },
                  { key: "oem",   label: "OEM",   render: (part) => part.oemNumber || h("span", { className: "muted-text" }, "—") },
                  { key: "cost",  label: "Cost",  render: (part) => money(part.costPrice, part.currency) },
                  { key: "sale",  label: "Sale",  render: (part) => part.salePrice > 0 ? money(part.salePrice, part.currency) : h(Badge, { tone: "warning" }, "No price") },
                  {
                    key: "coach",
                    label: "Coach",
                    render: (part) => h(PricingCoachSignal, {
                      h,
                      coach: smartPricingCoach(part, waitingByPart.get(String(part.id)) || 0)
                    })
                  },
                  { key: "stock", label: "Stock", render: (part) => h(StockBadge, { part }) },
                  {
                    key: "action",
                    label: "Action",
                    render: (part) => h("div", { className: "row-actions" },
                      h("button", { type: "button", onClick: () => selectPartPassport(part, onView) }, "Passport"),
                      h("button", {
                        type: "button",
                        title: "Copy availability text to clipboard",
                        onClick: () => copyShare(part)
                      }, copyFeedback === part.id ? "Copied!" : "Copy Share"),
                      h("button", {
                        type: "button",
                        disabled: !phone.trim(),
                        title: phone.trim() ? "Send WhatsApp availability" : "Enter a phone number above",
                        onClick: () => sendAvailability(part)
                      }, "Send")
                    )
                  }
                ],
                rows: displayedParts,
                getRowKey: (part) => part.id,
                emptyText: emptyMessage
              }),
          visibleParts.length > displayedParts.length && h("p", { className: "inventory-list-note" },
            `Showing ${displayedParts.length} of ${visibleParts.length} matching parts. Refine the filter to narrow results.`
          )
        )
  );
}
