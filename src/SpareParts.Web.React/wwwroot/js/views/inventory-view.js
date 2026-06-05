import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { asRows, money } from "../core/formatters.js";
import { CommunicationPayloadFactory } from "../services/communication-payload-factory.js";
import { PricingCoachSignal, smartPricingCoach, waitingCustomersByPart } from "../services/pricing-coach.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";
import { selectPartPassport } from "./part-passport-workspace-view.js";

const conditionLabels = new Map([
  [1, "New"],
  [2, "Used"],
  [3, "Rebuilt"]
]);
const conditionOptions = ["All", "New", "Used", "Rebuilt"];
const availabilityOptions = [
  "All",
  "Available",
  "Reserved",
  "Sold",
  "Damaged",
  "Low stock",
  "Donor parts",
  "Demand matches",
  "Quote ready",
  "Source needed",
  "Margin watch"
];

function conditionLabel(part) {
  const value = part?.condition;
  return conditionLabels.get(Number(value)) || String(value || "Unknown");
}

function defaultQuoteValidUntil() {
  const date = new Date();
  date.setDate(date.getDate() + 7);
  return date.toISOString().slice(0, 10);
}

function donorLabel(part, donor) {
  if (!part?.usedCarId) return "";
  const year = donor?.modelYear ? `${donor.modelYear} ` : "";
  return `${year}${donor?.car || `Donor car #${part.usedCarId}`}`.trim();
}

function donorLocation(donor) {
  return donor?.location || donor?.Location || "";
}

function stockStatus(part) {
  const notes = String(part?.notes || "").toLowerCase();
  const name = String(part?.name || "").toLowerCase();
  if (notes.includes("damaged") || name.includes("damaged")) return "Damaged";
  if (!part?.isActive) return "Sold";
  if (Number(part?.availableQuantity || 0) > 0) return "Available";
  if (Number(part?.reservedQuantity || 0) > 0) return "Reserved";
  return "Sold";
}

function stockStatusClass(part) {
  const status = stockStatus(part).toLowerCase().replace(/\s+/g, "-");
  return `inventory-badge status-${status}`;
}

function stockSubtitle(part) {
  const available = Number(part?.availableQuantity || 0);
  const reserved = Number(part?.reservedQuantity || 0);
  const stock = Number(part?.stockQuantity || 0);
  if (stockStatus(part) === "Damaged") return "Needs inspection";
  if (available > 0 && reserved > 0) return `${available} ready, ${reserved} reserved`;
  if (available > 0) return `${available} ready to sell`;
  if (reserved > 0) return `${reserved} reserved`;
  return "No available stock";
}

function normalizePhone(value) {
  return String(value || "").replace(/[^\d]/g, "");
}

function quoteLineTotal(item) {
  return Math.max(1, Number(item.quantity || 1)) * Number(item.part?.salePrice || 0);
}

function buildPartSearchText(part, donor) {
  return [
    part.internalCode,
    part.barcode,
    part.name,
    part.oemNumber,
    conditionLabel(part),
    part.currency,
    part.notes,
    part.usedCarId ? `donor ${part.usedCarId}` : "",
    donorLabel(part, donor),
    donorLocation(donor),
    donor?.supplierName
  ].filter(Boolean).join(" ").toLowerCase();
}

function readValue(row, ...keys) {
  for (const key of keys) {
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") return value;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row?.[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }

  return "";
}

function normalizeText(value) {
  return String(value || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .replace(/\s+/g, " ");
}

function requestStatus(request) {
  return normalizeText(readValue(request, "status"));
}

function isActiveRequest(request) {
  const status = requestStatus(request);
  return status === "open" || status === "contacted" || status === "reserved";
}

function requestCustomerLabel(request) {
  return readValue(request, "customerName") || readValue(request, "customerPhone") || "Customer";
}

function requestMatchesPart(request, part, donor) {
  const requestPartId = readValue(request, "partId");
  if (requestPartId && String(requestPartId) === String(part.id)) return true;

  const requestOem = normalizeText(readValue(request, "requestedOemNumber"));
  const partOem = normalizeText(part.oemNumber);
  if (requestOem && partOem && (partOem.includes(requestOem) || requestOem.includes(partOem))) return true;

  const requestedNameTokens = normalizeText(readValue(request, "requestedPartName"))
    .split(" ")
    .filter((token) => token.length >= 3);
  if (requestedNameTokens.length === 0) return false;

  const partText = normalizeText([
    part.internalCode,
    part.name,
    part.oemNumber,
    part.notes,
    donorLabel(part, donor),
    donorLocation(donor),
    donor?.supplierName
  ].filter(Boolean).join(" "));
  const vehicleTokens = normalizeText(readValue(request, "vehicleDetails"))
    .split(" ")
    .filter((token) => token.length >= 3);
  const allTokens = [...requestedNameTokens, ...vehicleTokens.slice(0, 4)];
  const matches = allTokens.filter((token) => partText.includes(token)).length;
  return matches >= Math.min(2, requestedNameTokens.length);
}

function marginWatch(part) {
  const salePrice = Number(part?.salePrice || 0);
  const costPrice = Number(part?.costPrice || 0);
  const averagePrice = Number(part?.estimatedMarketPrice || part?.averagePrice || 0);
  if (salePrice <= 0) return true;
  if (costPrice > 0 && (salePrice - costPrice) / salePrice < 0.22) return true;
  return averagePrice > 0 && salePrice < averagePrice * 0.9;
}

function buildDemandProfile(part, matches = [], waitingCount = 0) {
  const available = Number(part?.availableQuantity || 0);
  const reserved = Number(part?.reservedQuantity || 0);
  const minStock = Number(part?.minStock || 0);
  const matchCount = matches.length;
  const demandCount = Math.max(matchCount, Number(waitingCount || 0));
  const isLowStock = available <= minStock;
  const hasMarginWatch = marginWatch(part);
  const sourceNeeded = matchCount > 0 && available <= 0 && reserved <= 0;
  const quoteReady = matchCount > 0 && available > 0;
  const damaged = stockStatus(part) === "Damaged";
  const score = (sourceNeeded ? 24 : 0)
    + (quoteReady ? 18 : 0)
    + (damaged ? 14 : 0)
    + (isLowStock ? 10 : 0)
    + (hasMarginWatch ? 8 : 0)
    + demandCount * 5;

  if (sourceNeeded) {
    return {
      action: "Source now",
      detail: `${demandCount || 1} active request${demandCount === 1 ? "" : "s"} without ready stock.`,
      tone: "danger",
      score,
      matchCount,
      demandCount,
      quoteReady,
      sourceNeeded,
      marginWatch: hasMarginWatch,
      lowStock: isLowStock
    };
  }

  if (quoteReady) {
    return {
      action: "Quote now",
      detail: `${demandCount} customer request${demandCount === 1 ? "" : "s"} can be served from stock.`,
      tone: "success",
      score,
      matchCount,
      demandCount,
      quoteReady,
      sourceNeeded,
      marginWatch: hasMarginWatch,
      lowStock: isLowStock
    };
  }

  if (damaged) {
    return {
      action: "Inspect",
      detail: "Marked damaged or needing inspection before quoting.",
      tone: "danger",
      score,
      matchCount,
      demandCount,
      quoteReady,
      sourceNeeded,
      marginWatch: hasMarginWatch,
      lowStock: isLowStock
    };
  }

  if (isLowStock) {
    return {
      action: "Reorder soon",
      detail: `Available stock is at or below minimum (${minStock}).`,
      tone: "warning",
      score,
      matchCount,
      demandCount,
      quoteReady,
      sourceNeeded,
      marginWatch: hasMarginWatch,
      lowStock: isLowStock
    };
  }

  if (hasMarginWatch) {
    return {
      action: "Margin watch",
      detail: "Price is missing, below market, or too close to cost.",
      tone: "warning",
      score,
      matchCount,
      demandCount,
      quoteReady,
      sourceNeeded,
      marginWatch: hasMarginWatch,
      lowStock: isLowStock
    };
  }

  return {
    action: "Ready",
    detail: "No urgent demand, source, stock, or margin signal.",
    tone: "neutral",
    score,
    matchCount,
    demandCount,
    quoteReady,
    sourceNeeded,
    marginWatch: hasMarginWatch,
    lowStock: isLowStock
  };
}

function buildPartMessage(part, donor, quantity = 1) {
  const lines = [
    `Part: ${part.name}`,
    part.internalCode ? `Code: ${part.internalCode}` : "",
    part.oemNumber ? `OEM: ${part.oemNumber}` : "",
    `Condition: ${conditionLabel(part)}`,
    `Status: ${stockStatus(part)} (${stockSubtitle(part)})`,
    donorLabel(part, donor) ? `Donor: ${donorLabel(part, donor)}` : "",
    donorLocation(donor) ? `Location: ${donorLocation(donor)}` : "",
    `Quantity: ${Math.max(1, Number(quantity || 1))}`,
    `Price: ${money(part.salePrice, part.currency)}`,
    "Maalouf German Parts"
  ];

  return lines.filter(Boolean).join("\n");
}

function buildQuoteMessage({ customerName, quoteRows, discount, validUntil, notes }) {
  const subtotal = quoteRows.reduce((sum, item) => sum + quoteLineTotal(item), 0);
  const discountValue = Math.max(0, Number(discount || 0));
  const total = Math.max(0, subtotal - discountValue);
  const currency = quoteRows[0]?.part?.currency || "USD";
  const lines = [
    `Quotation for ${customerName || "Customer"}`,
    "",
    ...quoteRows.map((item, index) =>
      `${index + 1}. ${item.part.name} x${Math.max(1, Number(item.quantity || 1))} - ${money(quoteLineTotal(item), item.part.currency)}`
    ),
    "",
    `Subtotal: ${money(subtotal, currency)}`,
    discountValue > 0 ? `Discount: ${money(discountValue, currency)}` : "",
    `Total: ${money(total, currency)}`,
    validUntil ? `Valid until: ${validUntil}` : "",
    notes ? `Notes: ${notes}` : "",
    "Maalouf German Parts"
  ];

  return lines.filter((line) => line !== "").join("\n");
}

export function InventoryView({ api, onView }) {
  const [parts, setParts] = useState([]);
  const [partRequests, setPartRequests] = useState([]);
  const [usedCars, setUsedCars] = useState([]);
  const [filter, setFilter] = useState("");
  const [conditionFilter, setConditionFilter] = useState("All");
  const [availabilityFilter, setAvailabilityFilter] = useState("All");
  const [locationFilter, setLocationFilter] = useState("");
  const [minPrice, setMinPrice] = useState("");
  const [maxPrice, setMaxPrice] = useState("");
  const [viewMode, setViewMode] = useState("cards");
  const [phone, setPhone] = useState("");
  const [name, setName] = useState("");
  const [quoteItems, setQuoteItems] = useState({});
  const [quoteDiscount, setQuoteDiscount] = useState("");
  const [quoteNotes, setQuoteNotes] = useState("");
  const [quoteValidUntil, setQuoteValidUntil] = useState(defaultQuoteValidUntil);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading inventory...");
    try {
      const [nextParts, nextRequests, nextUsedCars] = await Promise.all([
        api.list("/api/parts"),
        api.get("/api/partrequests?status=Active").catch(() => []),
        api.get("/api/usedcars").catch(() => [])
      ]);
      setParts(asRows(nextParts));
      setPartRequests(asRows(nextRequests));
      setUsedCars(asRows(nextUsedCars));
      setStatus("Inventory loaded.");
    } catch (error) {
      setPartRequests([]);
      setStatus(error.message || "Could not load inventory.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const donorById = useMemo(
    () => new Map(usedCars.map((car) => [String(car.id), car])),
    [usedCars]
  );

  const activeRequests = useMemo(
    () => partRequests.filter(isActiveRequest),
    [partRequests]
  );

  const waitingByPart = useMemo(
    () => waitingCustomersByPart(partRequests),
    [partRequests]
  );

  const demandByPart = useMemo(() => {
    const groups = new Map();
    parts.forEach((part) => {
      const donor = donorById.get(String(part.usedCarId));
      const matches = activeRequests.filter((request) => requestMatchesPart(request, part, donor));
      if (matches.length > 0) {
        groups.set(String(part.id), matches);
      }
    });

    return groups;
  }, [activeRequests, donorById, parts]);

  const demandProfiles = useMemo(() => {
    const profiles = new Map();
    parts.forEach((part) => {
      const key = String(part.id);
      profiles.set(key, buildDemandProfile(part, demandByPart.get(key) || [], waitingByPart.get(key) || 0));
    });

    return profiles;
  }, [demandByPart, parts, waitingByPart]);

  const procurementRadar = useMemo(() => parts
    .map((part) => ({
      part,
      profile: demandProfiles.get(String(part.id)) || buildDemandProfile(part)
    }))
    .filter((item) => item.profile.score > 0)
    .sort((left, right) => right.profile.score - left.profile.score || String(left.part.name).localeCompare(String(right.part.name)))
    .slice(0, 6), [demandProfiles, parts]);

  const demandSummary = useMemo(() => {
    const matchedRequestIds = new Set();
    demandByPart.forEach((requests) => {
      requests.forEach((request) => matchedRequestIds.add(String(readValue(request, "id"))));
    });

    return {
      active: activeRequests.length,
      matched: matchedRequestIds.size,
      quoteReady: parts.filter((part) => demandProfiles.get(String(part.id))?.quoteReady).length,
      sourceNeeded: parts.filter((part) => demandProfiles.get(String(part.id))?.sourceNeeded).length,
      marginWatch: parts.filter((part) => demandProfiles.get(String(part.id))?.marginWatch).length,
      unmatched: activeRequests.filter((request) => !matchedRequestIds.has(String(readValue(request, "id")))).length
    };
  }, [activeRequests, demandByPart, demandProfiles, parts]);

  const visibleParts = useMemo(() => {
    const query = filter.trim().toLowerCase();
    const locationQuery = locationFilter.trim().toLowerCase();
    const min = minPrice === "" ? null : Number(minPrice);
    const max = maxPrice === "" ? null : Number(maxPrice);

    return parts.filter((part) => {
      const donor = donorById.get(String(part.usedCarId));
      const searchText = buildPartSearchText(part, donor);
      const statusValue = stockStatus(part);
      const available = Number(part.availableQuantity || 0);
      const minStock = Number(part.minStock || 0);
      const salePrice = Number(part.salePrice || 0);
      const profile = demandProfiles.get(String(part.id)) || buildDemandProfile(part);
      const demandText = (demandByPart.get(String(part.id)) || [])
        .map((request) => [
          readValue(request, "customerName"),
          readValue(request, "customerPhone"),
          readValue(request, "requestedPartName"),
          readValue(request, "requestedOemNumber"),
          readValue(request, "vehicleDetails"),
          readValue(request, "notes")
        ].filter(Boolean).join(" "))
        .join(" ")
        .toLowerCase();

      if (query && !searchText.includes(query) && !demandText.includes(query)) return false;
      if (conditionFilter !== "All" && conditionLabel(part) !== conditionFilter) return false;
      if (locationQuery && !donorLocation(donor).toLowerCase().includes(locationQuery)) return false;
      if (min !== null && Number.isFinite(min) && salePrice < min) return false;
      if (max !== null && Number.isFinite(max) && salePrice > max) return false;

      if (availabilityFilter === "Available") return available > 0;
      if (availabilityFilter === "Reserved") return Number(part.reservedQuantity || 0) > 0;
      if (availabilityFilter === "Sold") return statusValue === "Sold";
      if (availabilityFilter === "Damaged") return statusValue === "Damaged";
      if (availabilityFilter === "Low stock") return available <= minStock;
      if (availabilityFilter === "Donor parts") return Boolean(part.usedCarId);
      if (availabilityFilter === "Demand matches") return profile.matchCount > 0;
      if (availabilityFilter === "Quote ready") return profile.quoteReady;
      if (availabilityFilter === "Source needed") return profile.sourceNeeded;
      if (availabilityFilter === "Margin watch") return profile.marginWatch;
      return true;
    });
  }, [availabilityFilter, conditionFilter, demandByPart, demandProfiles, donorById, filter, locationFilter, maxPrice, minPrice, parts]);
  const displayedParts = useMemo(() => visibleParts.slice(0, 150), [visibleParts]);

  const quoteRows = useMemo(() => Object.values(quoteItems), [quoteItems]);
  const quoteSubtotal = useMemo(
    () => quoteRows.reduce((sum, item) => sum + quoteLineTotal(item), 0),
    [quoteRows]
  );
  const quoteTotal = Math.max(0, quoteSubtotal - Math.max(0, Number(quoteDiscount || 0)));
  const quoteCurrency = quoteRows[0]?.part?.currency || "USD";
  const quoteMessage = useMemo(() => buildQuoteMessage({
    customerName: name,
    quoteRows,
    discount: quoteDiscount,
    validUntil: quoteValidUntil,
    notes: quoteNotes
  }), [name, quoteDiscount, quoteNotes, quoteRows, quoteValidUntil]);
  const quoteWhatsAppLink = `https://wa.me/${normalizePhone(phone)}?text=${encodeURIComponent(quoteMessage)}`;
  const stockSummary = useMemo(() => ({
    total: parts.length,
    available: parts.filter((part) => Number(part.availableQuantity || 0) > 0).length,
    reserved: parts.filter((part) => Number(part.reservedQuantity || 0) > 0).length,
    sold: parts.filter((part) => stockStatus(part) === "Sold").length,
    lowStock: parts.filter((part) => Number(part.availableQuantity || 0) <= Number(part.minStock || 0)).length,
    donor: parts.filter((part) => part.usedCarId).length
  }), [parts]);

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

  const openWhatsappShare = useCallback((part) => {
    const donor = donorById.get(String(part.usedCarId));
    const phonePath = normalizePhone(phone);
    const href = `https://wa.me/${phonePath}?text=${encodeURIComponent(buildPartMessage(part, donor))}`;
    window.open(href, "_blank", "noopener,noreferrer");
    setStatus(phonePath ? "WhatsApp share opened." : "WhatsApp share opened without a recipient number.");
  }, [donorById, phone]);

  const copyPartDetails = useCallback(async (part) => {
    const donor = donorById.get(String(part.usedCarId));
    const text = buildPartMessage(part, donor);
    try {
      await navigator.clipboard.writeText(text);
      setStatus("Part details copied.");
    } catch {
      setStatus(text);
    }
  }, [donorById]);

  const addToQuote = useCallback((part) => {
    setQuoteItems((current) => ({
      ...current,
      [part.id]: {
        part,
        quantity: Math.max(1, Number(current[part.id]?.quantity || 0) + 1)
      }
    }));
    setStatus(`${part.name} added to quote draft.`);
  }, []);

  const updateQuoteQuantity = useCallback((partId, quantity) => {
    setQuoteItems((current) => {
      const existing = current[partId];
      if (!existing) return current;
      return {
        ...current,
        [partId]: {
          ...existing,
          quantity: Math.max(1, Number(quantity || 1))
        }
      };
    });
  }, []);

  const removeFromQuote = useCallback((partId) => {
    setQuoteItems((current) => {
      const next = { ...current };
      delete next[partId];
      return next;
    });
  }, []);

  const printLabel = useCallback((part) => {
    const win = window.open("", "_blank");
    win.document.write(`<!DOCTYPE html><html><head><title>Label ${part.internalCode || part.name}</title>
<style>
  @page { size: 80mm 40mm; margin: 2mm; }
  body { font-family: Arial, sans-serif; margin: 2mm; width: 76mm; height: 36mm; overflow: hidden; display: flex; flex-direction: column; justify-content: space-between; }
  .barcode { font-family: "Libre Barcode 39", "Courier New", monospace; font-size: 2rem; letter-spacing: 3px; line-height: 1; word-break: break-all; }
  .name { font-size: 0.65rem; color: #333; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
  .price { font-size: 0.8rem; font-weight: bold; text-align: right; }
  @media print { body { margin: 0; } }
</style></head><body>
<div class="barcode">${part.internalCode || part.id}</div>
<div class="name">${part.name || ""}</div>
<div class="price">${money(part.salePrice, part.currency)}</div>
<script>window.onload = function() { window.print(); }<\/script>
</body></html>`);
    win.document.close();
    setStatus(`Label printed for ${part.name || part.internalCode}.`);
  }, []);

  const clearQuote = useCallback(() => {
    setQuoteItems({});
    setQuoteDiscount("");
    setQuoteNotes("");
    setQuoteValidUntil(defaultQuoteValidUntil());
    setStatus("Quote draft cleared.");
  }, []);

  const shareQuote = useCallback(() => {
    if (quoteRows.length === 0) {
      setStatus("Add at least one part before sharing a quote.");
      return;
    }

    window.open(quoteWhatsAppLink, "_blank", "noopener,noreferrer");
    setStatus("Quote opened in WhatsApp.");
  }, [quoteRows.length, quoteWhatsAppLink]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Smart Parts Inventory",
      subtitle: "Search stock, donor links, reservations, and quote-ready parts in one workspace.",
      action: h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h("section", { className: "inventory-summary-grid" },
      h("button", { type: "button", onClick: () => setAvailabilityFilter("All") }, h("span", null, "Total parts"), h("strong", null, stockSummary.total)),
      h("button", { type: "button", onClick: () => setAvailabilityFilter("Available") }, h("span", null, "Available"), h("strong", null, stockSummary.available)),
      h("button", { type: "button", onClick: () => setAvailabilityFilter("Reserved") }, h("span", null, "Reserved"), h("strong", null, stockSummary.reserved)),
      h("button", { type: "button", onClick: () => setAvailabilityFilter("Sold") }, h("span", null, "Sold / out"), h("strong", null, stockSummary.sold)),
      h("button", { type: "button", onClick: () => setAvailabilityFilter("Low stock") }, h("span", null, "Low stock"), h("strong", null, stockSummary.lowStock)),
      h("button", { type: "button", onClick: () => setAvailabilityFilter("Donor parts") }, h("span", null, "Donor parts"), h("strong", null, stockSummary.donor))
    ),
    h("section", { className: "inventory-demand-panel" },
      h("div", { className: "panel-heading-row" },
        h("div", null,
          h("h3", null, "Demand & Reorder Radar"),
          h("span", null, "Active requests, quote-ready stock, source gaps, and margin-watch parts")
        ),
        h("button", { className: "secondary-button", type: "button", onClick: () => setAvailabilityFilter("Demand matches") }, "Show demand")
      ),
      h("div", { className: "inventory-demand-grid" },
        h("button", { type: "button", onClick: () => setAvailabilityFilter("Demand matches") },
          h("span", null, "Active requests"),
          h("strong", null, demandSummary.active),
          h("em", null, `${demandSummary.matched} matched`)
        ),
        h("button", { type: "button", onClick: () => setAvailabilityFilter("Quote ready") },
          h("span", null, "Quote ready"),
          h("strong", null, demandSummary.quoteReady),
          h("em", null, "In stock")
        ),
        h("button", { type: "button", onClick: () => setAvailabilityFilter("Source needed") },
          h("span", null, "Source needed"),
          h("strong", null, demandSummary.sourceNeeded),
          h("em", null, `${demandSummary.unmatched} unmatched`)
        ),
        h("button", { type: "button", onClick: () => setAvailabilityFilter("Margin watch") },
          h("span", null, "Margin watch"),
          h("strong", null, demandSummary.marginWatch),
          h("em", null, "Price check")
        )
      ),
      h("div", { className: "procurement-radar-list" },
        procurementRadar.map(({ part, profile }, index) =>
          h("button", {
            className: `procurement-radar-row tone-${profile.tone}`,
            key: part.id,
            type: "button",
            onClick: () => {
              setFilter(part.internalCode || part.name || "");
              setAvailabilityFilter(profile.sourceNeeded ? "Source needed" : profile.quoteReady ? "Quote ready" : profile.marginWatch ? "Margin watch" : "All");
            }
          },
            h("b", null, String(index + 1).padStart(2, "0")),
            h("span", null,
              h("strong", null, part.name || "Part"),
              h("em", null, `${part.internalCode || "No code"} | ${profile.detail}`)
            ),
            h("i", null, profile.action)
          )
        ),
        procurementRadar.length === 0 && h("p", { className: "empty-state" }, "No demand, reorder, or margin actions right now.")
      )
    ),
    h("section", { className: "inventory-workspace" },
      h("div", { className: "inventory-main" },
        h("section", { className: "inventory-search-panel" },
          h("div", { className: "inventory-search-topline" },
            h("input", { value: filter, onChange: (event) => setFilter(event.target.value), placeholder: "Search part name, code, OEM, donor car, model year, VIN note" }),
            h("div", { className: "segmented-control", role: "group", "aria-label": "Inventory view" },
              h("button", { className: viewMode === "cards" ? "active" : "", type: "button", onClick: () => setViewMode("cards") }, "Cards"),
              h("button", { className: viewMode === "table" ? "active" : "", type: "button", onClick: () => setViewMode("table") }, "Table")
            )
          ),
          h("div", { className: "inventory-filter-grid" },
            h("label", null, "Condition",
              h("select", { value: conditionFilter, onChange: (event) => setConditionFilter(event.target.value) },
                conditionOptions.map((option) => h("option", { key: option, value: option }, option))
              )
            ),
            h("label", null, "Availability",
              h("select", { value: availabilityFilter, onChange: (event) => setAvailabilityFilter(event.target.value) },
                availabilityOptions.map((option) => h("option", { key: option, value: option }, option))
              )
            ),
            h("label", null, "Location / shelf",
              h("input", { value: locationFilter, onChange: (event) => setLocationFilter(event.target.value), placeholder: "Donor location" })
            ),
            h("label", null, "Min price",
              h("input", { type: "number", min: "0", value: minPrice, onChange: (event) => setMinPrice(event.target.value) })
            ),
            h("label", null, "Max price",
              h("input", { type: "number", min: "0", value: maxPrice, onChange: (event) => setMaxPrice(event.target.value) })
            ),
            h("label", null, "Recipient name",
              h("input", { value: name, onChange: (event) => setName(event.target.value), placeholder: "Customer" })
            ),
            h("label", null, "WhatsApp phone",
              h("input", { value: phone, onChange: (event) => setPhone(event.target.value), placeholder: "+961..." })
            )
          )
        ),
        h(StatusLine, { status }),
        viewMode === "cards"
          ? h("section", { className: "inventory-card-grid" },
            displayedParts.map((part) => {
              const donor = donorById.get(String(part.usedCarId));
              const waiting = waitingByPart.get(String(part.id)) || 0;
              const matchedRequests = demandByPart.get(String(part.id)) || [];
              const profile = demandProfiles.get(String(part.id)) || buildDemandProfile(part, matchedRequests, waiting);
              return h("article", { className: "part-card", key: part.id },
                h("div", { className: "part-card-photo", "aria-hidden": "true" },
                  h("span", null, String(part.name || "P").slice(0, 2).toUpperCase()),
                  part.usedCarId && h("b", null, "Donor")
                ),
                h("div", { className: "part-card-body" },
                  h("div", { className: "part-card-heading" },
                    h("div", null,
                      h("span", { className: "part-code" }, part.internalCode || "No code"),
                      h("h3", null, part.name)
                    ),
                    h("strong", null, money(part.salePrice, part.currency))
                  ),
                  h("div", { className: "badge-row" },
                    h("span", { className: stockStatusClass(part) }, stockStatus(part)),
                    h("span", { className: "inventory-badge condition" }, conditionLabel(part)),
                    profile.score > 0 && h("span", { className: `inventory-badge action-${profile.tone}` }, profile.action),
                    waiting > 0 && h("span", { className: "inventory-badge demand" }, `${waiting} waiting`),
                    matchedRequests.length > 0 && h("span", { className: "inventory-badge demand" }, `${matchedRequests.length} match${matchedRequests.length === 1 ? "" : "es"}`),
                    part.usedCarId && h("span", { className: "inventory-badge donor" }, `#${part.usedCarId}`)
                  ),
                  h("dl", { className: "part-card-details" },
                    h("div", null, h("dt", null, "OEM"), h("dd", null, part.oemNumber || "Not set")),
                    h("div", null, h("dt", null, "Stock"), h("dd", null, stockSubtitle(part))),
                    h("div", null, h("dt", null, "Donor"), h("dd", null, donorLabel(part, donor) || "Standalone")),
                    h("div", null, h("dt", null, "Location"), h("dd", null, donorLocation(donor) || "Not set")),
                    h("div", null, h("dt", null, "Demand"), h("dd", null, matchedRequests[0] ? requestCustomerLabel(matchedRequests[0]) : "No active match")),
                    h("div", null, h("dt", null, "Action"), h("dd", null, profile.detail))
                  ),
                  h("div", { className: "part-card-actions" },
                    h("button", { type: "button", onClick: () => selectPartPassport(part, onView) }, "Passport"),
                    h("button", { type: "button", onClick: () => addToQuote(part) }, "Quote"),
                    h("button", { type: "button", onClick: () => openWhatsappShare(part) }, "WhatsApp"),
                    h("button", { type: "button", onClick: () => sendAvailability(part) }, "Send"),
                    h("button", { type: "button", onClick: () => copyPartDetails(part) }, "Copy")
                  )
                )
              );
            }),
            displayedParts.length === 0 && h("p", { className: "empty-state" }, "No parts match this search.")
          )
          : h("section", { className: "table-panel" },
            h(DataTable, {
              columns: [
                { key: "code", label: "Code", render: (part) => part.internalCode },
                { key: "part", label: "Part", render: (part) => part.name },
                { key: "oem", label: "OEM", render: (part) => part.oemNumber || "" },
                { key: "condition", label: "Condition", render: (part) => conditionLabel(part) },
                { key: "available", label: "Avail", render: (part) => part.availableQuantity },
                { key: "reserved", label: "Reserved", render: (part) => part.reservedQuantity || "" },
                { key: "cost", label: "Cost", render: (part) => money(part.costPrice, part.currency) },
                { key: "sale", label: "Sale", render: (part) => money(part.salePrice, part.currency) },
                {
                  key: "coach",
                  label: "Coach",
                  render: (part) => h(PricingCoachSignal, {
                    h,
                    coach: smartPricingCoach(part, waitingByPart.get(String(part.id)) || 0)
                  })
                },
                {
                  key: "demand",
                  label: "Demand",
                  render: (part) => {
                    const matches = demandByPart.get(String(part.id)) || [];
                    return matches.length > 0 ? `${matches.length} match${matches.length === 1 ? "" : "es"}` : "";
                  }
                },
                {
                  key: "next",
                  label: "Next",
                  render: (part) => {
                    const profile = demandProfiles.get(String(part.id)) || buildDemandProfile(part);
                    return profile.score > 0
                      ? h("span", { className: `inventory-badge action-${profile.tone}` }, profile.action)
                      : "";
                  }
                },
                { key: "status", label: "Status", render: (part) => h("span", { className: stockStatusClass(part) }, stockStatus(part)) },
                {
                  key: "donor",
                  label: "Donor",
                  render: (part) => donorLabel(part, donorById.get(String(part.usedCarId))) || ""
                },
                { key: "action", label: "Action", render: (part) => h("div", { className: "row-actions" },
                  h("button", { type: "button", onClick: () => selectPartPassport(part, onView) }, "Passport"),
                  h("button", { type: "button", onClick: () => addToQuote(part) }, "Quote"),
                  h("button", { type: "button", onClick: () => openWhatsappShare(part) }, "Share"),
                  h("button", { type: "button", onClick: () => sendAvailability(part) }, "Send"),
                  h("button", { type: "button", onClick: () => printLabel(part) }, "Print Label")
                ) }
              ],
              rows: displayedParts,
              getRowKey: (part) => part.id,
              emptyText: "No parts found."
            })
          ),
        visibleParts.length > displayedParts.length && h("p", { className: "inventory-list-note" },
          `Showing the first ${displayedParts.length} of ${visibleParts.length} matching parts. Refine the filter to find a specific row.`
        )
      ),
      h("aside", { className: "quote-draft-panel" },
        h("div", { className: "quote-draft-heading" },
          h("div", null,
            h("span", null, "Quotation draft"),
            h("h3", null, `${quoteRows.length} selected part${quoteRows.length === 1 ? "" : "s"}`)
          ),
          h("button", { type: "button", onClick: clearQuote, disabled: quoteRows.length === 0 }, "Clear")
        ),
        h("div", { className: "quote-line-list" },
          quoteRows.map((item) =>
            h("div", { className: "quote-line", key: item.part.id },
              h("div", null,
                h("strong", null, item.part.name),
                h("span", null, `${item.part.internalCode || "No code"} | ${money(item.part.salePrice, item.part.currency)}`)
              ),
              h("input", { type: "number", min: "1", value: item.quantity, onChange: (event) => updateQuoteQuantity(item.part.id, event.target.value) }),
              h("button", { type: "button", onClick: () => removeFromQuote(item.part.id) }, "Remove")
            )
          ),
          quoteRows.length === 0 && h("p", { className: "empty-state" }, "Use Quote on any part card to build a quick customer quotation.")
        ),
        h("label", null, "Discount",
          h("input", { type: "number", min: "0", value: quoteDiscount, onChange: (event) => setQuoteDiscount(event.target.value) })
        ),
        h("label", null, "Valid until",
          h("input", { type: "date", value: quoteValidUntil, onChange: (event) => setQuoteValidUntil(event.target.value) })
        ),
        h("label", null, "Notes",
          h("textarea", { rows: 3, value: quoteNotes, onChange: (event) => setQuoteNotes(event.target.value), placeholder: "Warranty, pickup, deposit, fitment notes" })
        ),
        h("div", { className: "quote-total-box" },
          h("span", null, "Subtotal", h("strong", null, money(quoteSubtotal, quoteCurrency))),
          h("span", null, "Total", h("strong", null, money(quoteTotal, quoteCurrency)))
        ),
        h("div", { className: "quote-actions" },
          h("button", { className: "primary-button", type: "button", onClick: shareQuote, disabled: quoteRows.length === 0 }, "Send quotation by WhatsApp"),
          h("button", { className: "secondary-button", type: "button", onClick: () => window.print(), disabled: quoteRows.length === 0 }, "Print draft")
        )
      )
    ),
    h("section", { className: "ai-placeholder-panel" },
      h("strong", null, "AI-ready placeholders"),
      h("span", null, "VIN decode, compatibility suggestions, auto descriptions, and photo recognition stay disabled until a configured AI key exists.")
    )
  );
}
