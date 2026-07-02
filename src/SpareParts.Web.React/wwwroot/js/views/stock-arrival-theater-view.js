import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { asRows, displayCurrencyContext, displayMoneyFromBase, initials, money, shortDate } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";
import { smartPricingCoach, waitingCustomersByPart } from "../services/pricing-coach.js";
import { read } from "../admin/resource-utils.js";

const campaignLanguage = "ArabicEnglish";

const lanes = [
  { key: "photo", title: "Photograph", subtitle: "Parts and cars that should be shot before selling." },
  { key: "waiting", title: "Waiting Customers", subtitle: "Demand that can be contacted now." },
  { key: "campaign", title: "Campaigns", subtitle: "Ready-made audiences and arrival bundles." },
  { key: "pricing", title: "Price", subtitle: "Stock that needs a quote before it can move." }
];

function toNumber(value) {
  const parsed = Number(String(value || "").replace(/,/g, ".").trim());
  return Number.isFinite(parsed) ? parsed : 0;
}

function itemId(row) {
  return read(row, "id", "partId", "purchaseId");
}

function availableQuantity(part) {
  return Math.max(toNumber(read(part, "availableQuantity")), toNumber(read(part, "stockQuantity")), 0);
}

function uniqueNumbers(values) {
  return [...new Set(values.map((value) => Number(value)).filter((value) => Number.isFinite(value) && value > 0))];
}

function compactText(value, fallback = "-") {
  const text = String(value || "").trim();
  return text || fallback;
}

function partTitle(part) {
  const code = read(part, "internalCode");
  const name = read(part, "name", "partName", "title");
  return [code, name].filter(Boolean).join(" - ") || `Part #${itemId(part)}`;
}

function carTitle(car) {
  const title = read(car, "title", "usedCar", "car");
  const year = read(car, "modelYear");
  return `${title || "Used car"} ${year || ""}`.trim();
}

function assetKey(asset) {
  return `${read(asset, "assetType")}:${itemId(asset)}`;
}

function isPartAsset(asset) {
  return String(read(asset, "assetType")).toLowerCase() === "part";
}

function isCarAsset(asset) {
  return String(read(asset, "assetType")).toLowerCase() === "car";
}

function isActiveRequest(request) {
  const status = String(read(request, "status")).toLowerCase();
  return status === "open" || status === "contacted";
}

function isReadyRequest(request) {
  return Boolean(read(request, "isReadyToContact")) || (isActiveRequest(request) && toNumber(read(request, "availableQuantity")) > 0);
}

function requestCustomers(request) {
  return Math.max(1, toNumber(read(request, "waitingCustomerCount")));
}

function requestTitle(request) {
  return read(request, "matchedPartName", "requestedPartName") || "Requested part";
}

function dateValue(row) {
  const raw = read(row, "purchaseDate", "createdAt", "postedAt", "date");
  const value = raw ? new Date(raw).getTime() : 0;
  return Number.isFinite(value) ? value : 0;
}

function recentArrivalRows(purchases, usedCarPurchases) {
  return [
    ...purchases.map((purchase) => ({
      key: `part-purchase-${itemId(purchase)}`,
      type: "Part purchase",
      title: read(purchase, "purchaseNumber") || `Purchase #${itemId(purchase)}`,
      detail: money(read(purchase, "totalAmount"), read(purchase, "currencyCode") || "USD"),
      date: read(purchase, "purchaseDate")
    })),
    ...usedCarPurchases.map((purchase) => ({
      key: `used-car-purchase-${itemId(purchase)}`,
      type: "Used car",
      title: read(purchase, "purchaseNumber") || `Vehicle purchase #${itemId(purchase)}`,
      detail: read(purchase, "usedCar") || read(purchase, "supplierName") || compactText(read(purchase, "postingStatus"), "Draft"),
      date: read(purchase, "purchaseDate")
    }))
  ].sort((left, right) => dateValue(right) - dateValue(left)).slice(0, 5);
}

function buildPhotoOpportunities(parts, campaignAssets, usedCars, displayContextForCar) {
  const carAssets = campaignAssets
    .filter(isCarAsset)
    .sort((left, right) => toNumber(read(left, "imageCount")) - toNumber(read(right, "imageCount")))
    .map((asset) => ({
      key: `photo-car-${assetKey(asset)}`,
      lane: "photo",
      type: "Used car",
      title: read(asset, "title") || `Used car #${itemId(asset)}`,
      subtitle: read(asset, "subtitle") || "Add showcase photos",
      metric: `${toNumber(read(asset, "imageCount"))} photos`,
      action: toNumber(read(asset, "imageCount")) > 0 ? "Review gallery" : "Shoot gallery",
      targetView: "used-cars",
      evidence: [
        ["Asset", "Vehicle arrival"],
        ["Campaign status", toNumber(read(asset, "imageCount")) > 0 ? "Can be promoted" : "Needs photos"],
        ["Price", read(asset, "price") ? money(read(asset, "price"), read(asset, "currency") || "USD") : "-"]
      ]
    }));
  const campaignCarIds = new Set(campaignAssets.filter(isCarAsset).map((asset) => String(itemId(asset))));
  const fallbackCars = usedCars
    .filter((car) => !campaignCarIds.has(String(itemId(car))))
    .sort((left, right) => Number(Boolean(read(right, "isReceived"))) - Number(Boolean(read(left, "isReceived"))) || toNumber(read(right, "modelYear")) - toNumber(read(left, "modelYear")))
    .map((car) => ({
      key: `photo-used-car-${itemId(car)}`,
      lane: "photo",
      type: "Used car",
      title: carTitle(car),
      subtitle: [read(car, "barcode"), read(car, "location"), read(car, "supplierName")].filter(Boolean).join(" / ") || "Vehicle arrival",
      metric: read(car, "isReceived") ? "Received" : "Incoming",
      action: read(car, "isReceived") ? "Shoot gallery" : "Track arrival",
      targetView: "used-cars",
      evidence: [
        ["Status", read(car, "isReceived") ? "Received" : "Incoming"],
        ["Location", read(car, "location") || "-"],
        ["Cost", displayMoneyFromBase(read(car, "fullCostBase", "grandTotalBase", "price"), displayContextForCar(car))]
      ]
    }));
  const carCards = [...carAssets, ...fallbackCars].slice(0, 7);

  const partCards = parts
    .filter((part) => availableQuantity(part) > 0)
    .sort((left, right) => Number(Boolean(read(right, "usedCarId"))) - Number(Boolean(read(left, "usedCarId"))) || availableQuantity(right) - availableQuantity(left))
    .slice(0, Math.max(0, 8 - carCards.length))
    .map((part) => ({
      key: `photo-part-${itemId(part)}`,
      lane: "photo",
      type: read(part, "usedCarId") ? "Donor part" : "Part",
      title: partTitle(part),
      subtitle: read(part, "oemNumber") ? `OEM ${read(part, "oemNumber")}` : "Ready for product photos",
      metric: `${availableQuantity(part).toLocaleString()} in stock`,
      action: "Photograph",
      targetView: "stock",
      evidence: [
        ["Stock", availableQuantity(part).toLocaleString()],
        ["Current price", money(read(part, "salePrice"), read(part, "currency") || "USD")],
        ["Source", read(part, "usedCarId") ? `Used car #${read(part, "usedCarId")}` : "Parts inventory"]
      ]
    }));

  return [...carCards, ...partCards].slice(0, 8);
}

function buildWaitingOpportunities(requests) {
  return requests
    .filter(isReadyRequest)
    .sort((left, right) => requestCustomers(right) - requestCustomers(left) || dateValue(right) - dateValue(left))
    .slice(0, 10)
    .map((request) => ({
      key: `waiting-${itemId(request)}`,
      lane: "waiting",
      type: `${requestCustomers(request)} waiting`,
      title: requestTitle(request),
      subtitle: [read(request, "customerName"), read(request, "vehicleDetails")].filter(Boolean).join(" / ") || "Customer request",
      metric: read(request, "partInternalCode") || compactText(read(request, "requestedOemNumber"), "Match stock"),
      action: "Contact",
      targetView: "part-requests",
      evidence: [
        ["Customer", read(request, "customerName") || "-"],
        ["Phone", read(request, "customerPhone") || "-"],
        ["Available", toNumber(read(request, "availableQuantity")).toLocaleString()],
        ["Status", read(request, "status") || "-"]
      ]
    }));
}

function buildCampaignOpportunities(parts, requests, campaignAssets) {
  const readyRequests = requests.filter(isReadyRequest);
  const waitingPartIds = uniqueNumbers(readyRequests.map((request) => read(request, "partId")));
  const stockedPartIds = uniqueNumbers(
    parts
      .filter((part) => availableQuantity(part) > 0 && toNumber(read(part, "salePrice")) > 0)
      .map((part) => itemId(part))
  ).slice(0, 12);
  const photographedCarIds = uniqueNumbers(
    campaignAssets
      .filter((asset) => isCarAsset(asset) && toNumber(read(asset, "imageCount")) > 0)
      .map((asset) => itemId(asset))
  ).slice(0, 8);
  const partAssetIds = uniqueNumbers(
    campaignAssets
      .filter((asset) => isPartAsset(asset) && toNumber(read(asset, "price")) > 0)
      .map((asset) => itemId(asset))
  ).slice(0, 12);
  const readyPartIds = waitingPartIds.length ? waitingPartIds : partAssetIds.length ? partAssetIds : stockedPartIds;

  return [
    readyRequests.length > 0 && {
      key: "campaign-waiting-parts",
      lane: "campaign",
      type: "Waiting parts",
      title: "Back-in-stock message",
      subtitle: `${readyRequests.length} matched request${readyRequests.length === 1 ? "" : "s"} can be contacted.`,
      metric: `${readyRequests.reduce((total, request) => total + requestCustomers(request), 0).toLocaleString()} customers`,
      action: "Preview",
      targetView: "whatsapp",
      campaignRequest: {
        segment: "WaitingForParts",
        language: campaignLanguage,
        includeImages: true,
        maxRecipients: 100,
        partIds: readyPartIds,
        usedCarIds: [],
        note: "Newly arrived stock is ready for customers who were waiting."
      },
      evidence: [
        ["Audience", "WaitingForParts"],
        ["Matched parts", readyPartIds.length.toLocaleString()],
        ["Requests", readyRequests.length.toLocaleString()]
      ]
    },
    stockedPartIds.length > 0 && {
      key: "campaign-fresh-parts",
      lane: "campaign",
      type: "Fresh arrivals",
      title: "New stock campaign",
      subtitle: "Promote recently available priced parts to active customers.",
      metric: `${stockedPartIds.length.toLocaleString()} parts`,
      action: "Preview",
      targetView: "whatsapp",
      campaignRequest: {
        segment: "ActiveCustomers",
        language: campaignLanguage,
        includeImages: true,
        maxRecipients: 100,
        partIds: stockedPartIds,
        usedCarIds: [],
        note: "Fresh arrivals available now."
      },
      evidence: [
        ["Audience", "ActiveCustomers"],
        ["Parts", stockedPartIds.length.toLocaleString()],
        ["Ready", "Priced stock"]
      ]
    },
    photographedCarIds.length > 0 && {
      key: "campaign-used-cars",
      lane: "campaign",
      type: "Used cars",
      title: "Vehicle arrivals showcase",
      subtitle: "Cars with images can anchor a WhatsApp showcase.",
      metric: `${photographedCarIds.length.toLocaleString()} cars`,
      action: "Preview",
      targetView: "whatsapp",
      campaignRequest: {
        segment: "AllCustomers",
        language: campaignLanguage,
        includeImages: true,
        maxRecipients: 100,
        partIds: [],
        usedCarIds: photographedCarIds,
        note: "Fresh used-car arrivals and donor inventory now available."
      },
      evidence: [
        ["Audience", "AllCustomers"],
        ["Cars", photographedCarIds.length.toLocaleString()],
        ["Ready", "Photos attached"]
      ]
    }
  ].filter(Boolean);
}

function buildPricingOpportunities(parts, requests) {
  const waitingByPart = waitingCustomersByPart(requests);

  return parts
    .filter((part) => availableQuantity(part) > 0)
    .map((part) => {
      const waiting = waitingByPart.get(String(itemId(part))) || 0;
      const coach = smartPricingCoach(part, waiting);
      const salePrice = toNumber(read(part, "salePrice"));
      const currentPrice = salePrice > 0 ? money(salePrice, read(part, "currency") || "USD") : "No sale price";
      return { part, waiting, coach, salePrice, currentPrice };
    })
    .filter(({ coach, salePrice, waiting }) =>
      salePrice <= 0
      || coach.tone === "warning"
      || (waiting > 0 && toNumber(coach.suggestedPrice) > salePrice)
    )
    .sort((left, right) =>
      Number(left.salePrice > 0) - Number(right.salePrice > 0)
      || right.waiting - left.waiting
      || toNumber(right.coach.suggestedPrice) - toNumber(left.coach.suggestedPrice))
    .slice(0, 10)
    .map(({ part, waiting, coach, currentPrice }) => ({
      key: `pricing-${itemId(part)}`,
      lane: "pricing",
      type: coach.badge || "Price check",
      title: partTitle(part),
      subtitle: coach.message,
      metric: currentPrice,
      action: "Set price",
      targetView: "stock",
      evidence: [
        ["Stock", availableQuantity(part).toLocaleString()],
        ["Waiting", waiting.toLocaleString()],
        ["Suggested", coach.suggestedPrice ? money(coach.suggestedPrice, read(part, "currency") || "USD") : "-"],
        ["Cost", money(read(part, "costPrice"), read(part, "currency") || "USD")]
      ]
    }));
}

function buildBoard({ parts, requests, usedCars, campaignAssets, appConstants, currencyRates }) {
  const displayContextForCar = (car) => displayCurrencyContext({
    constants: appConstants,
    rates: currencyRates,
    baseCurrencyCode: read(car, "baseCurrencyCode"),
    counterCurrencyCode: read(car, "counterCurrencyCode")
  });

  return {
    photo: buildPhotoOpportunities(parts, campaignAssets, usedCars, displayContextForCar),
    waiting: buildWaitingOpportunities(requests),
    campaign: buildCampaignOpportunities(parts, requests, campaignAssets),
    pricing: buildPricingOpportunities(parts, requests)
  };
}

function OpportunityCard({ item, active, onSelect }) {
  return h("button", {
    className: active ? "arrival-card active" : "arrival-card",
    type: "button",
    onClick: onSelect
  },
    h("span", { className: "arrival-card-type" }, item.type),
    h("strong", null, item.title),
    h("p", null, item.subtitle),
    h("span", { className: "arrival-card-footer" },
      h("b", null, item.metric),
      h("small", null, item.action)
    )
  );
}

function MetricTile({ label, value, detail }) {
  return h("article", { className: "arrival-metric" },
    h("span", null, label),
    h("strong", null, value),
    detail && h("small", null, detail)
  );
}

function EvidenceRow({ label, value }) {
  return h("div", { className: "arrival-evidence-row" },
    h("span", null, label),
    h("strong", null, value || "-")
  );
}

function PreviewPanel({ preview }) {
  if (!preview) return null;

  const recipients = Array.isArray(preview.recipients) ? preview.recipients : [];
  return h("section", { className: "arrival-preview-panel" },
    h("header", null,
      h("strong", null, "Campaign Preview"),
      h("span", null, `${Number(read(preview, "recipientCount") || 0).toLocaleString()} recipients / ${Number(read(preview, "attachmentCount") || 0).toLocaleString()} images`)
    ),
    h("textarea", { readOnly: true, value: read(preview, "messageBody") || "" }),
    recipients.length > 0 && h("div", { className: "arrival-recipient-strip" },
      recipients.slice(0, 5).map((recipient) =>
        h("span", { key: `${read(recipient, "phone")}-${read(recipient, "name")}` },
          h("i", null, initials(read(recipient, "name"))),
          h("b", null, read(recipient, "name"))
        )
      )
    )
  );
}

export function StockArrivalTheaterView({ api, onView, t }) {
  const [data, setData] = useState({
    parts: [],
    requests: [],
    usedCars: [],
    campaignAssets: [],
    purchases: [],
    usedCarPurchases: [],
    appConstants: [],
    currencyRates: []
  });
  const [selectedKey, setSelectedKey] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [previewByKey, setPreviewByKey] = useState({});
  const [isPreviewing, setIsPreviewing] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading stock arrival opportunities...");
    const sources = [
      ["parts", api.list("/api/parts")],
      ["requests", api.get("/api/partrequests?status=Active")],
      ["usedCars", api.get("/api/usedcars")],
      ["campaignAssets", api.get("/api/communications/campaign-assets")],
      ["purchases", api.get("/api/purchases")],
      ["usedCarPurchases", api.get("/api/purchases/used-cars")],
      ["appConstants", api.get("/api/appconstants")],
      ["currencyRates", api.get("/api/currencies")]
    ];

    try {
      const results = await Promise.allSettled(sources.map(([, promise]) => promise));
      const next = {};
      results.forEach((result, index) => {
        const key = sources[index][0];
        next[key] = result.status === "fulfilled" ? asRows(result.value) : [];
      });
      setData(next);
      const failed = results.filter((result) => result.status === "rejected").length;
      setStatus(failed
        ? `Board loaded with ${failed} source${failed === 1 ? "" : "s"} unavailable.`
        : "Stock arrival board loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load stock arrival opportunities.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => {
    load();
  }, [load]);

  const board = useMemo(() => buildBoard(data), [data]);
  const allOpportunities = useMemo(() => lanes.flatMap((lane) => board[lane.key] || []), [board]);
  const visibleBoard = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return board;
    return Object.fromEntries(lanes.map((lane) => [
      lane.key,
      (board[lane.key] || []).filter((item) =>
        [item.title, item.subtitle, item.type, item.metric].join(" ").toLowerCase().includes(query)
      )
    ]));
  }, [board, search]);

  useEffect(() => {
    setSelectedKey((current) => {
      if (current && allOpportunities.some((item) => item.key === current)) return current;
      return allOpportunities[0]?.key || "";
    });
  }, [allOpportunities]);

  const selected = allOpportunities.find((item) => item.key === selectedKey) || allOpportunities[0] || null;
  const arrivals = useMemo(
    () => recentArrivalRows(data.purchases, data.usedCarPurchases),
    [data.purchases, data.usedCarPurchases]
  );
  const readyRequestCount = board.waiting.length;
  const waitingCustomerCount = board.waiting.reduce((total, item) => {
    const match = String(item.type || "").match(/\d+/);
    return total + (match ? Number(match[0]) : 0);
  }, 0);
  const pricedStockCount = data.parts.filter((part) => availableQuantity(part) > 0 && toNumber(read(part, "salePrice")) > 0).length;

  const previewCampaign = useCallback(async (item) => {
    if (!item?.campaignRequest) return;
    setIsPreviewing(true);
    setStatus("Building campaign preview...");
    try {
      const preview = await api.post("/api/communications/campaign-preview", item.campaignRequest);
      setPreviewByKey((current) => ({ ...current, [item.key]: preview }));
      setStatus(`Campaign preview ready for ${Number(read(preview, "recipientCount") || 0).toLocaleString()} recipient(s).`);
    } catch (error) {
      setStatus(error.message || "Could not build campaign preview.");
    } finally {
      setIsPreviewing(false);
    }
  }, [api]);

  const openTarget = useCallback((targetView) => {
    if (targetView && onView) onView(targetView);
  }, [onView]);

  return h("section", { className: "screen stock-arrival-screen" },
    h(PageHeader, {
      kicker: "Operations",
      title: t("stockArrival.title", "Stock Arrival Theater"),
      subtitle: t("stockArrival.subtitle", "Turn newly arrived purchase and used-car stock into photos, follow-ups, campaigns, and prices."),
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: load,
        disabled: isLoading
      }, isLoading ? t("common.loading", "Loading") : t("common.refresh", "Refresh"))
    }),
    h(StatusLine, { status }),
    h("section", { className: "arrival-control-bar" },
      h("div", { className: "arrival-metrics" },
        h(MetricTile, { label: "Recent arrivals", value: arrivals.length.toLocaleString(), detail: "Purchases and cars" }),
        h(MetricTile, { label: "Ready requests", value: readyRequestCount.toLocaleString(), detail: `${waitingCustomerCount.toLocaleString()} waiting customers` }),
        h(MetricTile, { label: "Campaigns", value: board.campaign.length.toLocaleString(), detail: "Suggested sends" }),
        h(MetricTile, { label: "Priced stock", value: pricedStockCount.toLocaleString(), detail: "Available to sell" })
      ),
      h("label", { className: "arrival-search" },
        h("span", null, "Search board"),
        h("input", {
          value: search,
          placeholder: "Part, customer, campaign, vehicle",
          onChange: (event) => setSearch(event.target.value)
        })
      )
    ),
    h("section", { className: "arrival-layout" },
      h("div", { className: "arrival-board", "aria-label": "Stock arrival opportunity board" },
        lanes.map((lane) => {
          const items = visibleBoard[lane.key] || [];
          return h("section", { className: "arrival-lane", key: lane.key },
            h("header", null,
              h("div", null,
                h("strong", null, lane.title),
                h("span", null, lane.subtitle)
              ),
              h("b", null, items.length)
            ),
            h("div", { className: "arrival-card-list" },
              items.map((item) =>
                h(OpportunityCard, {
                  key: item.key,
                  item,
                  active: selected?.key === item.key,
                  onSelect: () => setSelectedKey(item.key)
                })
              ),
              items.length === 0 && h("p", { className: "empty-state" }, "Nothing in this lane right now.")
            )
          );
        })
      ),
      h("aside", { className: "arrival-inspector" },
        selected
          ? h("div", null,
            h("div", { className: "arrival-inspector-hero" },
              h("span", null, initials(selected.title)),
              h("div", null,
                h("small", null, selected.type),
                h("h3", null, selected.title),
                h("p", null, selected.subtitle)
              )
            ),
            h("section", { className: "arrival-inspector-section" },
              h("header", null,
                h("h3", null, "Next move"),
                h("span", null, selected.metric)
              ),
              h("p", null, selected.action),
              h("div", { className: "arrival-action-row" },
                selected.campaignRequest && h("button", {
                  className: "primary-button",
                  type: "button",
                  disabled: isPreviewing,
                  onClick: () => previewCampaign(selected)
                }, isPreviewing ? "Previewing" : "Preview campaign"),
                selected.targetView && h("button", {
                  className: "secondary-button",
                  type: "button",
                  onClick: () => openTarget(selected.targetView)
                }, selected.targetView === "whatsapp" ? "Open WhatsApp" : "Open workspace")
              )
            ),
            h("section", { className: "arrival-inspector-section" },
              h("header", null,
                h("h3", null, "Evidence"),
                h("span", null, "Live data")
              ),
              (selected.evidence || []).map(([label, value]) =>
                h(EvidenceRow, { key: label, label, value })
              )
            ),
            h(PreviewPanel, { preview: previewByKey[selected.key] })
          )
          : h("p", { className: "empty-state" }, "No arrival opportunities yet.")
      )
    ),
    h("section", { className: "arrival-feed" },
      h("header", null,
        h("h3", null, "Arrival Feed"),
        h("span", null, "Latest purchase signals")
      ),
      h("div", { className: "arrival-feed-list" },
        arrivals.map((arrival) =>
          h("article", { key: arrival.key, className: "arrival-feed-row" },
            h("span", null, arrival.type),
            h("strong", null, arrival.title),
            h("small", null, [arrival.detail, shortDate(arrival.date)].filter(Boolean).join(" / "))
          )
        ),
        arrivals.length === 0 && h("p", { className: "empty-state" }, "No purchase arrivals found yet.")
      )
    )
  );
}
