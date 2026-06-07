import { h, useCallback, useEffect, useMemo, useRef, useState } from "../core/react-runtime.js";
import { displayCurrencyContext, displayMoneyFromBase, initials, money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const emptyForm = {
  barcode: "",
  supplierId: "",
  carModelId: "",
  modelYear: "",
  priceCurrency: "USD",
  price: "",
  locationId: "",
  isReceived: false,
  isShipped: false,
  partOut: "",
  shipping: "",
  customs: "",
  repairs: "",
  expectedSellThroughRate: "0.8"
};

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

function toArray(value) {
  return Array.isArray(value) ? value : value ? [value] : [];
}

function toNumber(value) {
  const normalized = String(value || "").replace(/,/g, ".").trim();
  if (!normalized) return 0;
  const parsed = Number(normalized);
  return Number.isFinite(parsed) ? parsed : 0;
}

function toInt(value) {
  const parsed = Number.parseInt(String(value || ""), 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function itemId(row) {
  return read(row, "id", "locationId");
}

function imageSrc(image) {
  const imageData = read(image, "imageData");
  if (!imageData) return "";
  return `data:${read(image, "mimeType") || "image/jpeg"};base64,${imageData}`;
}

function carTitle(car, t) {
  const year = read(car, "modelYear");
  const name = read(car, "car") || t("usedCars.title", "Used Cars");
  return `${year || ""} ${name}`.trim();
}

function modelTitle(model) {
  return `${read(model, "carBrandName") || ""} ${read(model, "name") || ""}${read(model, "bodyType") ? ` (${read(model, "bodyType")})` : ""}`.trim();
}

function carPrice(car) {
  return money(read(car, "price"), read(car, "priceCurrency", "baseCurrencyCode") || "USD");
}

function baseCurrency(car) {
  return read(car, "baseCurrencyCode") || "USD";
}

function percent(value, total) {
  if (total <= 0) return 0;
  return Math.max(0, Math.round((value / total) * 100));
}

function partUnitCost(part) {
  return toNumber(read(part, "minimumSellPrice"))
    || toNumber(read(part, "allocatedCost"))
    || toNumber(read(part, "costPrice"))
    || toNumber(read(part, "averagePrice"));
}

function partTargetPrice(part) {
  return toNumber(read(part, "recommendedPrice"))
    || toNumber(read(part, "salePrice"))
    || toNumber(read(part, "estimatedMarketPrice"))
    || toNumber(read(part, "averagePrice"));
}

function partAvailableQuantity(part) {
  return Math.max(
    toNumber(read(part, "availableQuantity")),
    toNumber(read(part, "stockQuantity")),
    0
  );
}

function rankPart(part, isLinked) {
  const salePrice = partTargetPrice(part);
  const unitCost = partUnitCost(part);
  const quantity = partAvailableQuantity(part);
  const usefulQuantity = quantity > 0 ? quantity : 0;
  const expectedSale = salePrice * usefulQuantity;
  const expectedMargin = (salePrice - unitCost) * usefulQuantity;
  const marginPercent = salePrice > 0 ? Math.round(((salePrice - unitCost) / salePrice) * 100) : 0;
  return {
    part,
    isLinked,
    quantity: usefulQuantity,
    salePrice,
    unitCost,
    expectedSale,
    expectedMargin,
    marginPercent,
    score: expectedSale + Math.max(expectedMargin, 0) * 0.6 + (isLinked ? 10 : 0)
  };
}

function buildPartRecommendations(linkedParts, unassignedParts) {
  const linkedCandidates = linkedParts
    .map((part) => rankPart(part, true))
    .filter((item) => item.quantity > 0 && (item.salePrice > 0 || item.unitCost > 0));
  const source = linkedCandidates.length > 0
    ? linkedCandidates
    : unassignedParts
      .map((part) => rankPart(part, false))
      .filter((item) => item.quantity > 0 && (item.salePrice > 0 || item.unitCost > 0));

  return source
    .sort((left, right) =>
      right.score - left.score
      || right.expectedMargin - left.expectedMargin
      || String(read(left.part, "name")).localeCompare(String(read(right.part, "name"))))
    .slice(0, 5);
}

function partValueInCarBase(item, car) {
  const value = item.expectedSale || item.salePrice;
  const partCurrency = String(read(item.part, "currency") || "").trim().toUpperCase();
  const carBaseCurrency = String(baseCurrency(car)).trim().toUpperCase();
  if (!partCurrency || partCurrency === carBaseCurrency) return value;

  const counterCurrency = String(read(car, "counterCurrencyCode") || "").trim().toUpperCase();
  const counterRateToBase = toNumber(read(car, "counterRateToBase"));
  return partCurrency === counterCurrency && counterRateToBase > 0
    ? value * counterRateToBase
    : value;
}

function buildPushParts(car, recommendations, breakEvenGap) {
  let cumulativeSale = 0;
  return recommendations.slice(0, 3).map((item) => {
    const gapWasOpen = cumulativeSale < breakEvenGap;
    cumulativeSale += partValueInCarBase(item, car);
    return {
      ...item,
      closesGap: breakEvenGap > 0 && gapWasOpen && cumulativeSale >= breakEvenGap
    };
  });
}

function estimatedDaysToProfit(car, project, recommendations) {
  if (project.breakEvenGap <= 0) return 0;

  const expectedSellThroughRate = Math.min(
    Math.max(toNumber(read(car, "expectedSellThroughRate")) || 0.8, 0.1),
    1
  );
  const projectedThirtyDayRecovery = recommendations
    .slice(0, 3)
    .reduce((total, item) => total + partValueInCarBase(item, car), 0)
    * expectedSellThroughRate;

  if (projectedThirtyDayRecovery <= 0) return null;
  return Math.max(1, Math.ceil(project.breakEvenGap / (projectedThirtyDayRecovery / 30)));
}

function buildProfitProject(car) {
  const bought = toNumber(read(car, "purchaseCostBase", "priceBase"));
  const fullCost = toNumber(read(car, "fullCostBase", "grandTotalBase"));
  const teardownCosts = Math.max(fullCost - bought, 0);
  const partsRemovedCount = toNumber(read(car, "partsRemovedCount"));
  const partsRemovedValue = toNumber(read(car, "partsRemovedValueBase"));
  const soldQuantity = toNumber(read(car, "partsSoldQuantity"));
  const soldAmount = read(car, "isWholesaleSold")
    ? toNumber(read(car, "salePriceBase"))
    : toNumber(read(car, "partsSoldAmountBase"));
  const remainingQuantity = toNumber(read(car, "remainingStockQuantity"));
  const remainingStockValue = toNumber(read(car, "remainingStockValueBase"));
  const recoveredValue = soldAmount;
  const netProfitLossValue = read(car, "netProfitLossBase");
  const netProfitLoss = netProfitLossValue === "" ? recoveredValue - fullCost : toNumber(netProfitLossValue);
  const breakEvenGap = Math.max(fullCost - recoveredValue, 0);

  return {
    bought,
    fullCost,
    teardownCosts,
    partsRemovedCount,
    partsRemovedValue,
    soldQuantity,
    soldAmount,
    remainingQuantity,
    remainingStockValue,
    recoveredValue,
    netProfitLoss,
    breakEvenGap,
    recoveredPercent: percent(recoveredValue, fullCost),
    soldPercent: percent(soldAmount, fullCost),
    stockPercent: percent(remainingStockValue, fullCost)
  };
}

function buildDonorAction(car, project, linkedParts, t) {
  if (read(car, "isWholesaleSold")) {
    return {
      key: "sold",
      tone: "positive",
      label: t("usedCars.actionSold", "Sold wholesale"),
      detail: read(car, "wholesaleBuyerName")
        ? t("usedCars.soldToBuyer", "Sold to {name}", { name: read(car, "wholesaleBuyerName") })
        : t("usedCars.wholesaleProjectClosed", "Wholesale project closed")
    };
  }

  if (!read(car, "isReceived")) {
    return {
      key: "receive",
      tone: "warning",
      label: t("usedCars.actionReceive", "Receive & cost"),
      detail: t("usedCars.actionReceiveDetail", "Confirm arrival, customs, and landed cost.")
    };
  }

  if (!read(car, "isShipped")) {
    return {
      key: "ship",
      tone: "warning",
      label: t("usedCars.actionShip", "Ship / clear"),
      detail: t("usedCars.actionShipDetail", "Close shipping status before teardown.")
    };
  }

  if (project.fullCost <= 0) {
    return {
      key: "cost",
      tone: "danger",
      label: t("usedCars.actionConfirmCosts", "Confirm costs"),
      detail: t("usedCars.actionConfirmCostsDetail", "Add purchase and teardown costs before profit tracking.")
    };
  }

  if (project.breakEvenGap <= 0) {
    return {
      key: "profit",
      tone: "positive",
      label: t("usedCars.actionProfit", "Profit unlocked"),
      detail: t("usedCars.actionProfitDetail", "Keep selling remaining donor stock.")
    };
  }

  if (linkedParts.length === 0 && project.partsRemovedCount === 0) {
    return {
      key: "link",
      tone: "danger",
      label: t("usedCars.actionLinkParts", "Link donor parts"),
      detail: t("usedCars.actionLinkPartsDetail", "Assign harvested parts so recovery can be tracked.")
    };
  }

  if (project.remainingStockValue >= project.breakEvenGap && project.remainingStockValue > 0) {
    return {
      key: "sell-stock",
      tone: "success",
      label: t("usedCars.actionSellStock", "Sell stocked parts"),
      detail: t("usedCars.actionSellStockDetail", "Current stock value can close the break-even gap.")
    };
  }

  if (project.partsRemovedCount > 0) {
    return {
      key: "quote",
      tone: "warning",
      label: t("usedCars.actionPushParts", "Push removed parts"),
      detail: t("usedCars.actionPushPartsDetail", "Quote high-value removed parts first.")
    };
  }

  return {
    key: "harvest",
    tone: "neutral",
    label: t("usedCars.actionHarvest", "Harvest next parts"),
    detail: t("usedCars.actionHarvestDetail", "Start with high-value parts and common requests.")
  };
}

function buildDonorProject(car, linkedParts, unassignedParts, t) {
  const project = buildProfitProject(car);
  const recommendations = buildPartRecommendations(linkedParts, unassignedParts);
  return {
    car,
    project,
    linkedParts,
    recommendations,
    pushParts: buildPushParts(car, recommendations, project.breakEvenGap),
    estimatedDays: estimatedDaysToProfit(car, project, recommendations),
    action: buildDonorAction(car, project, linkedParts, t)
  };
}

function donorSearchHaystack(entry) {
  const { car, action } = entry;
  return [
    `${read(car, "modelYear") || ""} ${read(car, "car") || ""}`,
    read(car, "barcode"),
    read(car, "supplierName"),
    read(car, "location"),
    read(car, "wholesaleBuyerName"),
    read(car, "wholesaleSaleNumber"),
    action.label,
    action.detail
  ].join(" ").toLowerCase();
}

function matchesDonorFilter(entry, filter) {
  const { car, project, linkedParts, action } = entry;
  if (filter === "profit") return project.breakEvenGap <= 0;
  if (filter === "gap") return project.breakEvenGap > 0;
  if (filter === "needs-linking") return linkedParts.length === 0 && project.partsRemovedCount === 0;
  if (filter === "action") return ["receive", "ship", "cost", "link", "harvest", "sell-stock", "quote"].includes(action.key);
  if (filter === "wholesale") return Boolean(read(car, "isWholesaleSold"));
  if (filter === "transit") return !read(car, "isReceived") || !read(car, "isShipped");
  return true;
}

function DonorMetric({ label, value, detail, tone }) {
  return h("div", { className: `donor-metric ${tone || ""}` },
    h("span", null, label),
    h("strong", null, value),
    detail && h("small", null, detail)
  );
}

function DonorCockpit({
  entries,
  filteredEntries,
  metrics,
  search,
  onSearch,
  filter,
  onFilter,
  onSelect,
  t,
  displayContextForCar
}) {
  const filters = [
    { key: "all", label: t("usedCars.filterAllDonors", "All") },
    { key: "action", label: t("usedCars.filterAction", "Action") },
    { key: "gap", label: t("usedCars.filterGapOpen", "Gap open") },
    { key: "needs-linking", label: t("usedCars.filterNeedsLinking", "Needs linking") },
    { key: "profit", label: t("usedCars.filterProfit", "Profit") },
    { key: "transit", label: t("usedCars.filterTransit", "Transit") },
    { key: "wholesale", label: t("usedCars.filterWholesale", "Wholesale") }
  ];
  const focusEntries = filteredEntries
    .filter((entry) => entry.action.key !== "sold")
    .slice(0, 5);

  return h("article", { className: "panel donor-cockpit" },
    h("div", { className: "donor-cockpit-heading" },
      h("div", null,
        h("h2", null, t("usedCars.donorCockpit", "Donor Car Cockpit")),
        h("p", null, t("usedCars.donorCockpitSubtitle", "Search half-cuts, spot break-even risk, and pick the next staff action from one place."))
      ),
      h("strong", null, t("usedCars.filteredDonors", "{shown} / {total} shown", { shown: filteredEntries.length, total: entries.length }))
    ),
    h("div", { className: "donor-metric-grid" },
      h(DonorMetric, {
        label: t("usedCars.donorProjects", "Donor projects"),
        value: entries.length.toLocaleString(),
        detail: t("usedCars.readyForTeardown", "{count} ready for teardown", { count: metrics.readyForTeardown.toLocaleString() })
      }),
      h(DonorMetric, {
        label: t("usedCars.profitUnlocked", "Profit unlocked"),
        value: metrics.profitUnlocked.toLocaleString(),
        detail: t("usedCars.keepSelling", "Keep selling remaining stock"),
        tone: "positive"
      }),
      h(DonorMetric, {
        label: t("usedCars.breakEvenRisk", "Break-even risk"),
        value: metrics.gapOpen.toLocaleString(),
        detail: t("usedCars.gapOpenDetail", "Need targeted quoting"),
        tone: "warning"
      }),
      h(DonorMetric, {
        label: t("usedCars.partsLinked", "Parts linked"),
        value: metrics.linkedParts.toLocaleString(),
        detail: t("usedCars.needsLinkingCount", "{count} donors need linking", { count: metrics.needsLinking.toLocaleString() }),
        tone: metrics.needsLinking > 0 ? "danger" : "positive"
      })
    ),
    h("div", { className: "donor-toolbar" },
      h("input", {
        value: search,
        onChange: (event) => onSearch(event.target.value),
        placeholder: t("usedCars.searchDonors", "Search donor, barcode, supplier, buyer, action...")
      }),
      h("div", { className: "donor-filter-strip" },
        filters.map((item) => h("button", {
          key: item.key,
          className: item.key === filter ? "chip active" : "chip",
          type: "button",
          onClick: () => onFilter(item.key)
        }, item.label))
      )
    ),
    h("div", { className: "donor-action-queue" },
      h("div", { className: "profit-section-heading" },
        h("h4", null, t("usedCars.todayFocus", "Today Focus")),
        h("span", null, t("usedCars.todayFocusHint", "Sorted by recovery risk and next action"))
      ),
      focusEntries.length > 0
        ? focusEntries.map((entry) => {
          const { car, project, action, linkedParts } = entry;
          const displayMoney = (value) => displayMoneyFromBase(value, displayContextForCar(car));
          return h("button", {
            key: itemId(car),
            className: "donor-action-card",
            type: "button",
            onClick: () => onSelect(itemId(car))
          },
            h("span", { className: `donor-action-pill ${action.tone}` }, action.label),
            h("strong", null, carTitle(car, t)),
            h("small", null, action.detail),
            h("em", null, `${displayMoney(project.breakEvenGap)} ${t("usedCars.gap", "gap")} / ${linkedParts.length.toLocaleString()} ${t("usedCars.linked", "linked")}`)
          );
        })
        : h("p", { className: "empty-state" }, t("usedCars.noFocusDonors", "No donor cars match this focus."))
    )
  );
}

function buildDonorPassportText(entry, displayMoney, t) {
  const { car, project, linkedParts, action } = entry;
  return [
    carTitle(car, t),
    `${t("usedCars.nextAction", "Next Action")}: ${action.label}`,
    `${t("usedCars.supplier", "Supplier")}: ${read(car, "supplierName") || "-"}`,
    `${t("usedCars.location", "Location")}: ${read(car, "location") || "-"}`,
    `${t("usedCars.barcode", "Barcode")}: ${read(car, "barcode") || "-"}`,
    `${t("usedCars.fullCost", "Full Cost")}: ${displayMoney(project.fullCost)}`,
    `${t("usedCars.recovered", "Recovered")}: ${project.recoveredPercent}%`,
    `${t("usedCars.breakEven", "Break Even")}: ${project.breakEvenGap > 0 ? displayMoney(project.breakEvenGap) : t("usedCars.done", "Done")}`,
    `${t("usedCars.linkedParts", "Linked Parts")}: ${linkedParts.length.toLocaleString()}`,
    `${t("usedCars.remainingStock", "Remaining Stock")}: ${displayMoney(project.remainingStockValue)}`
  ].join("\n");
}

function DonorPassport({ entry, displayContext, t, onCopy, onWhatsapp }) {
  if (!entry) return null;

  const { car, project, action, linkedParts, pushParts } = entry;
  const displayMoney = (value) => displayMoneyFromBase(value, displayContext);

  return h("article", { className: "panel donor-passport" },
    h("div", { className: "donor-passport-heading" },
      h("div", null,
        h("h3", null, t("usedCars.donorPassport", "Donor Passport")),
        h("span", null, carTitle(car, t))
      ),
      h("div", { className: "row-actions" },
        h("button", { className: "secondary-button", type: "button", onClick: onCopy }, t("usedCars.copyPassport", "Copy Passport")),
        h("button", { className: "secondary-button whatsapp-button", type: "button", onClick: onWhatsapp }, "WhatsApp")
      )
    ),
    h("div", { className: "donor-passport-body" },
      h("div", { className: `donor-next-action ${action.tone}` },
        h("span", null, t("usedCars.nextAction", "Next Action")),
        h("strong", null, action.label),
        h("small", null, action.detail)
      ),
      h("div", { className: "detail-grid" },
        h(DetailTile, { label: t("usedCars.fullCost", "Full Cost"), value: displayMoney(project.fullCost) }),
        h(DetailTile, { label: t("usedCars.breakEven", "Break Even"), value: project.breakEvenGap > 0 ? displayMoney(project.breakEvenGap) : t("usedCars.done", "Done") }),
        h(DetailTile, { label: t("usedCars.recovered", "Recovered"), value: `${project.recoveredPercent}%` }),
        h(DetailTile, { label: t("usedCars.linkedParts", "Linked Parts"), value: linkedParts.length.toLocaleString() }),
        h(DetailTile, { label: t("usedCars.remainingStock", "Remaining Stock"), value: displayMoney(project.remainingStockValue) }),
        h(DetailTile, { label: t("usedCars.saleStatus", "Sale Status"), value: read(car, "isWholesaleSold") ? t("usedCars.soldWholesale", "Sold wholesale") : t("usedCars.available", "Available") })
      ),
      h("div", { className: "donor-push-strip" },
        h("span", null, t("usedCars.nextPushParts", "Next push parts")),
        pushParts.length > 0
          ? pushParts.map((item) => h("strong", { key: itemId(item.part) },
            `${read(item.part, "internalCode") || read(item.part, "name") || `#${itemId(item.part)}`} · ${money(item.expectedSale || item.salePrice, read(item.part, "currency") || baseCurrency(car))}`
          ))
          : h("small", null, t("usedCars.linkPartsToForecast", "Link parts to forecast profit"))
      )
    )
  );
}

function InputField({ label, value, onChange, type = "text", placeholder }) {
  return h("label", null,
    h("span", null, label),
    h("input", {
      value: value ?? "",
      type,
      placeholder,
      onChange: (event) => onChange(event.target.value)
    })
  );
}

function ToggleField({ label, value, onChange }) {
  return h("label", { className: "checkbox-field" },
    h("input", {
      type: "checkbox",
      checked: Boolean(value),
      onChange: (event) => onChange(event.target.checked)
    }),
    h("span", null, label)
  );
}

function SelectField({ label, value, onChange, options, getValue, getLabel }) {
  return h("label", null,
    h("span", null, label),
    h("select", { value: value ?? "", onChange: (event) => onChange(event.target.value) },
      h("option", { value: "" }, "-"),
      options.map((option, index) =>
        h("option", { key: getValue(option) || index, value: getValue(option) }, getLabel(option))
      )
    )
  );
}

function DetailTile({ label, value }) {
  return h("div", { className: "detail-tile" },
    h("span", null, label),
    h("strong", null, value || "-")
  );
}

function ProfitStep({ label, value, meta, tone }) {
  return h("div", { className: `profit-flow-step ${tone || ""}` },
    h("span", null, label),
    h("strong", null, value),
    meta && h("small", null, meta)
  );
}

function ProfitMetric({ label, value, detail, tone }) {
  return h("div", { className: `profit-metric ${tone || ""}` },
    h("span", null, label),
    h("strong", null, value),
    detail && h("small", null, detail)
  );
}

function PartRecommendation({ item, car, t, onAssign }) {
  const part = item.part;
  const currency = read(part, "currency") || baseCurrency(car);
  return h("div", { className: "part-recommendation-row" },
    h("div", { className: "part-recommendation-main" },
      h("strong", null, read(part, "internalCode") || `#${itemId(part)}`),
      h("span", null, `${read(part, "name") || "Part"}${read(part, "oemNumber") ? ` / ${read(part, "oemNumber")}` : ""}`)
    ),
    h("div", { className: "part-recommendation-metrics" },
      h("span", null, t("usedCars.expectedSale", "Expected Sale")),
      h("strong", null, money(item.expectedSale || item.salePrice, currency)),
      h("small", null, `${t("usedCars.stock", "Stock")} ${item.quantity.toLocaleString()} / ${t("usedCars.min", "Min")} ${money(item.unitCost, currency)} / ${t("usedCars.margin", "Margin")} ${item.marginPercent}%`)
    ),
    item.isLinked
      ? h("span", { className: "profit-status-chip" }, t("usedCars.linked", "Linked"))
      : h("button", { className: "secondary-button", type: "button", onClick: () => onAssign(itemId(part)) }, t("usedCars.assign", "Assign"))
  );
}

function BreakEvenRacer({ entry, index, isActive, onSelect, t, displayContext }) {
  const { car, estimatedDays, project, pushParts } = entry;
  const displayMoney = (value) => displayMoneyFromBase(value, displayContext);
  const recoveredPercent = Math.min(project.recoveredPercent, 100);
  const isProfitable = project.fullCost > 0 && project.breakEvenGap <= 0;

  return h("button", {
    className: `break-even-racer ${isActive ? "active" : ""}`,
    type: "button",
    onClick: () => onSelect(itemId(car))
  },
    h("div", { className: "break-even-racer-topline" },
      h("span", { className: "break-even-position" }, `#${String(index + 1).padStart(2, "0")}`),
      h("span", { className: `break-even-state ${isProfitable ? "positive" : "watch"}` },
        isProfitable ? t("usedCars.profitUnlocked", "Profit unlocked") : t("usedCars.closingGap", "Closing gap"))
    ),
    h("div", { className: "break-even-racer-heading" },
      h("h3", null, carTitle(car, t)),
      h("strong", null, `${project.recoveredPercent}%`)
    ),
    h("div", { className: "break-even-racer-progress", "aria-label": t("usedCars.recovered", "Recovered") },
      h("span", { style: { width: `${recoveredPercent}%` } })
    ),
    h("div", { className: "break-even-racer-summary" },
      h("strong", null, isProfitable
        ? t("usedCars.inProfit", "In profit")
        : `${displayMoney(project.breakEvenGap)} ${t("usedCars.remaining", "Remaining").toLowerCase()}`),
      h("span", null, isProfitable
        ? t("usedCars.breakEvenReached", "Break-even reached")
        : estimatedDays
          ? t("usedCars.estimatedDaysToProfit", "Estimated {count} days to profit", { count: estimatedDays })
          : t("usedCars.linkPartsToForecast", "Link parts to forecast profit"))
    ),
    h("div", { className: "break-even-push-parts" },
      h("span", { className: "break-even-push-label" }, t("usedCars.topPushParts", "Top push parts")),
      pushParts.length > 0
        ? pushParts.map((item) => {
          const part = item.part;
          const currency = read(part, "currency") || baseCurrency(car);
          return h("span", {
            className: `break-even-push-part ${item.closesGap ? "closes-gap" : ""}`,
            key: itemId(part)
          },
            h("b", null, read(part, "internalCode") || read(part, "name") || `#${itemId(part)}`),
            h("small", null, money(item.expectedSale || item.salePrice, currency)),
            item.closesGap && h("em", null, t("usedCars.closesGap", "Closes gap"))
          );
        })
        : h("small", { className: "break-even-no-parts" }, t("usedCars.noPushParts", "No ranked parts yet."))
    )
  );
}

function BreakEvenRace({ entries, selectedId, onSelect, t, displayContextForCar }) {
  return h("article", { className: "panel break-even-race" },
    h("div", { className: "break-even-race-heading" },
      h("div", null,
        h("h2", null, t("usedCars.breakEvenRace", "Break-Even Race")),
        h("p", null, t("usedCars.breakEvenRaceSubtitle", "Turn donor cars into live projects. Closest to profit first, with the parts most likely to push each car over the line."))
      ),
      h("strong", null, t("usedCars.donorProjects", "{count} donor projects", { count: entries.length }))
    ),
    entries.length > 0
      ? h("div", { className: "break-even-race-grid" },
        entries.map((entry, index) => h(BreakEvenRacer, {
          entry,
          index,
          isActive: String(itemId(entry.car)) === String(selectedId),
          key: itemId(entry.car),
          onSelect,
          t,
          displayContext: displayContextForCar(entry.car)
        }))
      )
      : h("p", { className: "empty-state" }, t("usedCars.noCars", "No used cars returned."))
  );
}

function ProfitProjectMap({ car, recommendations, onAssignPart, t, displayContext }) {
  if (!car) return null;

  const project = buildProfitProject(car);
  const displayMoney = (value) => displayMoneyFromBase(value, displayContext);
  const resultTone = project.fullCost > 0 && project.netProfitLoss >= 0 ? "positive" : "negative";
  const soldWidth = Math.min(project.soldPercent, 100);
  const flowSteps = [
    {
      label: t("usedCars.boughtFor", "Bought For"),
      value: displayMoney(project.bought),
      meta: carPrice(car)
    },
    {
      label: t("usedCars.teardownCosts", "Teardown Costs"),
      value: displayMoney(project.teardownCosts),
      meta: t("usedCars.fullCost", "Full Cost")
    },
    {
      label: t("usedCars.partsRemoved", "Parts Removed"),
      value: displayMoney(project.partsRemovedValue),
      meta: t("usedCars.partsCount", "{count} parts", { count: project.partsRemovedCount.toLocaleString() })
    },
    {
      label: t("usedCars.partsSold", "Parts Sold"),
      value: displayMoney(project.soldAmount),
      meta: t("usedCars.partsCount", "{count} parts", { count: project.soldQuantity.toLocaleString() }),
      tone: "sold"
    },
    {
      label: t("usedCars.remainingStock", "Remaining Stock"),
      value: displayMoney(project.remainingStockValue),
      meta: t("usedCars.partsCount", "{count} parts", { count: project.remainingQuantity.toLocaleString() })
    },
    {
      label: t("usedCars.profitLoss", "Profit/Loss"),
      value: displayMoney(project.netProfitLoss),
      meta: project.breakEvenGap > 0
        ? t("usedCars.breakEvenGap", "{amount} to break even", { amount: displayMoney(project.breakEvenGap) })
        : t("usedCars.breakEvenReached", "Break-even reached"),
      tone: resultTone
    }
  ];

  return h("article", { className: "panel profit-project-map" },
    h("div", { className: "profit-map-heading" },
      h("div", null,
        h("h3", null, t("usedCars.profitMap", "Teardown Profit Map")),
        h("span", null, carTitle(car, t))
      ),
      h("strong", { className: `profit-result-pill ${resultTone}` }, displayMoney(project.netProfitLoss))
    ),
    h("div", { className: "profit-flow-grid" },
      flowSteps.map((step) => h(ProfitStep, {
        key: step.label,
        label: step.label,
        value: step.value,
        meta: step.meta,
        tone: step.tone
      }))
    ),
    h("div", { className: "profit-recovery-strip" },
      h("div", { className: "profit-recovery-labels" },
        h("span", null, t("usedCars.recovered", "Recovered")),
        h("strong", null, `${project.recoveredPercent}%`)
      ),
      h("div", { className: "profit-progress-track", "aria-label": t("usedCars.recovered", "Recovered") },
        h("span", { className: "profit-progress-segment sold", style: { width: `${soldWidth}%` } })
      ),
      h("div", { className: "profit-legend" },
        h("span", null, t("usedCars.soldCash", "Sold cash"))
      )
    ),
    h("div", { className: "profit-metric-grid" },
      h(ProfitMetric, {
        label: t("usedCars.fullCost", "Full Cost"),
        value: displayMoney(project.fullCost),
        detail: t("usedCars.buyPlusCosts", "Bought plus teardown costs")
      }),
      h(ProfitMetric, {
        label: t("usedCars.recoveredValue", "Recovered Value"),
        value: displayMoney(project.recoveredValue),
        detail: t("usedCars.soldCashOnly", "Sold cash only")
      }),
      h(ProfitMetric, {
        label: t("usedCars.breakEven", "Break Even"),
        value: project.breakEvenGap > 0 ? displayMoney(project.breakEvenGap) : t("usedCars.done", "Done"),
        detail: project.breakEvenGap > 0 ? t("usedCars.remaining", "Remaining") : t("usedCars.profitableProject", "Profitable project"),
        tone: project.breakEvenGap > 0 ? "watch" : "positive"
      })
    ),
    h("section", { className: "next-parts-section" },
      h("div", { className: "profit-section-heading" },
        h("h4", null, t("usedCars.bestNextParts", "Best Next Parts To Remove")),
        h("span", null, t("usedCars.rankByValue", "Ranked by sale value and margin"))
      ),
      recommendations.length > 0
        ? h("div", { className: "part-recommendation-list" },
          recommendations.map((item) => h(PartRecommendation, {
            key: `${item.isLinked ? "linked" : "candidate"}-${itemId(item.part)}`,
            item,
            car,
            t,
            onAssign: onAssignPart
          }))
        )
        : h("p", { className: "empty-state" }, t("usedCars.noNextParts", "Link parts to this car to reveal next removal candidates."))
    )
  );
}

function PartRow({ part, actionTitle, onClick }) {
  return h("div", { className: "part-link-row" },
    h("div", null,
      h("strong", null, read(part, "internalCode") || `#${itemId(part)}`),
      h("span", null, `${read(part, "name") || "Part"}${read(part, "oemNumber") ? ` / ${read(part, "oemNumber")}` : ""}`)
    ),
    h("button", { className: "secondary-button", type: "button", onClick }, actionTitle)
  );
}

function GalleryModal({
  car,
  images,
  index,
  onClose,
  onIndex,
  t
}) {
  const dragRef = useRef(null);
  const [zoom, setZoom] = useState(1);
  const [pan, setPan] = useState({ x: 0, y: 0 });
  const image = images[index] || null;
  const src = image ? imageSrc(image) : "";

  const clampIndex = useCallback((next) => {
    const length = Math.max(images.length, 1);
    onIndex((next + length) % length);
    setZoom(1);
    setPan({ x: 0, y: 0 });
  }, [images.length, onIndex]);

  useEffect(() => {
    function onKeyDown(event) {
      if (event.key === "Escape") onClose();
      if (event.key === "ArrowLeft") clampIndex(index - 1);
      if (event.key === "ArrowRight") clampIndex(index + 1);
    }
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [clampIndex, index, onClose]);

  const startDrag = useCallback((event) => {
    if (zoom <= 1) return;
    dragRef.current = {
      x: event.clientX,
      y: event.clientY,
      pan
    };
    event.currentTarget.setPointerCapture(event.pointerId);
  }, [pan, zoom]);

  const drag = useCallback((event) => {
    if (!dragRef.current || zoom <= 1) return;
    const dx = event.clientX - dragRef.current.x;
    const dy = event.clientY - dragRef.current.y;
    setPan({ x: dragRef.current.pan.x + dx, y: dragRef.current.pan.y + dy });
  }, [zoom]);

  const endDrag = useCallback(() => {
    dragRef.current = null;
  }, []);

  const setNextZoom = useCallback((nextZoom) => {
    const clamped = Math.min(Math.max(nextZoom, 1), 4);
    setZoom(clamped);
    if (clamped === 1) setPan({ x: 0, y: 0 });
  }, []);

  return h("div", { className: "gallery-modal", role: "dialog", "aria-modal": "true" },
    h("div", { className: "gallery-topbar" },
      h("div", null,
        h("strong", null, car ? carTitle(car, t) : t("usedCars.gallery", "Gallery")),
        h("span", null, `${index + 1} / ${images.length}`)
      ),
      h("button", { className: "ghost-button", type: "button", onClick: onClose }, t("common.close", "Close"))
    ),
    h("button", { className: "gallery-arrow left", type: "button", onClick: () => clampIndex(index - 1), disabled: images.length < 2 }, "<"),
    h("div", {
      className: "gallery-stage",
      onWheel: (event) => {
        event.preventDefault();
        setNextZoom(zoom + (event.deltaY < 0 ? 0.2 : -0.2));
      },
      onPointerDown: startDrag,
      onPointerMove: drag,
      onPointerUp: endDrag,
      onPointerCancel: endDrag
    },
      src
        ? h("img", {
          src,
          alt: car ? carTitle(car, t) : t("usedCars.gallery", "Gallery"),
          style: { transform: `translate(${pan.x}px, ${pan.y}px) scale(${zoom})` }
        })
        : h("div", { className: "gallery-placeholder" }, initials(car ? carTitle(car, t) : "MAP"))
    ),
    h("button", { className: "gallery-arrow right", type: "button", onClick: () => clampIndex(index + 1), disabled: images.length < 2 }, ">"),
    h("div", { className: "zoom-toolbar" },
      h("button", { type: "button", onClick: () => setNextZoom(zoom - 0.25), disabled: zoom <= 1 }, "-"),
      h("button", { type: "button", onClick: () => setNextZoom(1) }, `${Math.round(zoom * 100)}%`),
      h("button", { type: "button", onClick: () => setNextZoom(zoom + 0.25), disabled: zoom >= 4 }, "+"),
      h("button", { type: "button", onClick: () => setNextZoom(1) }, t("usedCars.resetZoom", "Reset zoom"))
    ),
    images.length > 1 && h("div", { className: "gallery-thumb-rail" },
      images.map((thumb, thumbIndex) =>
        h("button", {
          key: itemId(thumb) || thumbIndex,
          className: thumbIndex === index ? "gallery-thumb active" : "gallery-thumb",
          type: "button",
          onClick: () => clampIndex(thumbIndex)
        },
          imageSrc(thumb)
            ? h("img", { src: imageSrc(thumb), alt: `${thumbIndex + 1}` })
            : h("span", null, thumbIndex + 1)
        )
      )
    )
  );
}

function ListingPackageModal({ pkg, onClose, t }) {
  const [status, setStatus] = useState("");

  const copyText = useCallback(async (label, value) => {
    try {
      await navigator.clipboard.writeText(value);
      setStatus(t("usedCars.listingCopied", "{label} copied to clipboard.", { label }));
    } catch {
      setStatus(t("usedCars.listingCopyError", "Could not copy to clipboard."));
    }
  }, [t]);

  if (!pkg) return null;

  return h("div", { className: "gallery-modal listing-package-modal", role: "dialog", "aria-modal": "true" },
    h("div", { className: "gallery-topbar" },
      h("div", null,
        h("strong", null, t("usedCars.listingPackage", "Listing Package")),
        h("span", null, pkg.title)
      ),
      h("button", { className: "ghost-button", type: "button", onClick: onClose }, t("common.close", "Close"))
    ),
    h("div", { className: "panel listing-package-body" },
      h("label", null, t("usedCars.listingTitle", "Title")),
      h("textarea", { readOnly: true, rows: 2, value: pkg.title }),
      h("button", { className: "secondary-button", type: "button", onClick: () => copyText(t("usedCars.listingTitle", "Title"), pkg.title) }, t("common.copy", "Copy")),

      h("label", null, t("usedCars.listingDescription", "Description")),
      h("textarea", { readOnly: true, rows: 8, value: pkg.description }),
      h("button", { className: "secondary-button", type: "button", onClick: () => copyText(t("usedCars.listingDescription", "Description"), pkg.description) }, t("common.copy", "Copy")),

      h("p", null, t("usedCars.listingPhotoCount", "{count} photo(s) ready in the gallery — download or drag them into the marketplace listing after pasting the text above.", { count: pkg.photoCount })),

      h("h4", null, t("usedCars.listingMarketplaces", "Open a marketplace to post")),
      h("div", { className: "row-actions" },
        (pkg.marketplaceLinks || []).map((link) =>
          h("a", {
            key: link.name,
            className: "secondary-button",
            href: link.url,
            target: "_blank",
            rel: "noopener noreferrer"
          }, link.name)
        )
      ),
      status && h("p", { className: "status-text" }, status)
    )
  );
}

export function UsedCarsView({ api, t }) {
  const fileInputRef = useRef(null);
  const [cars, setCars] = useState([]);
  const [images, setImages] = useState([]);
  const [parts, setParts] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [carModels, setCarModels] = useState([]);
  const [locations, setLocations] = useState([]);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [currencyCodes, setCurrencyCodes] = useState(["USD"]);
  const [selectedId, setSelectedId] = useState("");
  const [selectedImageId, setSelectedImageId] = useState("");
  const [form, setForm] = useState({ ...emptyForm, modelYear: String(new Date().getFullYear()) });
  const [carSearch, setCarSearch] = useState("");
  const [carFilter, setCarFilter] = useState("all");
  const [partSearch, setPartSearch] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [isGalleryOpen, setIsGalleryOpen] = useState(false);
  const [galleryIndex, setGalleryIndex] = useState(0);
  const [listingPackage, setListingPackage] = useState(null);
  const [isGeneratingListing, setIsGeneratingListing] = useState(false);

  const selectedCar = useMemo(
    () => cars.find((car) => String(itemId(car)) === String(selectedId)) || null,
    [cars, selectedId]
  );
  const selectedImage = useMemo(
    () => images.find((image) => String(itemId(image)) === String(selectedImageId)) || images[0] || null,
    [images, selectedImageId]
  );
  const selectedCarParts = useMemo(() =>
    parts.filter((part) => String(read(part, "usedCarId") || "") === String(selectedId)),
  [parts, selectedId]);
  const unassignedParts = useMemo(() =>
    parts.filter((part) => !read(part, "usedCarId")),
  [parts]);
  const partsByUsedCarId = useMemo(() => {
    const grouped = new Map();
    parts.forEach((part) => {
      const usedCarId = String(read(part, "usedCarId") || "");
      if (!usedCarId) return;
      const current = grouped.get(usedCarId) || [];
      current.push(part);
      grouped.set(usedCarId, current);
    });
    return grouped;
  }, [parts]);
  const donorProjects = useMemo(() => cars
    .map((car) => buildDonorProject(
      car,
      partsByUsedCarId.get(String(itemId(car))) || [],
      unassignedParts,
      t
    ))
    .sort((left, right) => {
      const leftActionRank = left.action.key === "sold" ? 1 : 0;
      const rightActionRank = right.action.key === "sold" ? 1 : 0;
      return leftActionRank - rightActionRank
        || (right.project.breakEvenGap > 0 ? 1 : 0) - (left.project.breakEvenGap > 0 ? 1 : 0)
        || right.project.breakEvenGap - left.project.breakEvenGap
        || carTitle(left.car, t).localeCompare(carTitle(right.car, t));
    }),
  [cars, partsByUsedCarId, t, unassignedParts]);
  const filteredDonorProjects = useMemo(() => {
    const query = carSearch.trim().toLowerCase();
    return donorProjects.filter((entry) =>
      matchesDonorFilter(entry, carFilter)
        && (!query || donorSearchHaystack(entry).includes(query))
    );
  }, [carFilter, carSearch, donorProjects]);
  const selectedDonorProject = useMemo(() =>
    donorProjects.find((entry) => String(itemId(entry.car)) === String(selectedId)) || null,
  [donorProjects, selectedId]);
  const donorMetrics = useMemo(() => donorProjects.reduce((summary, entry) => {
    const isReadyForTeardown = read(entry.car, "isReceived") && read(entry.car, "isShipped") && !read(entry.car, "isWholesaleSold");
    return {
      readyForTeardown: summary.readyForTeardown + (isReadyForTeardown ? 1 : 0),
      profitUnlocked: summary.profitUnlocked + (entry.project.fullCost > 0 && entry.project.breakEvenGap <= 0 ? 1 : 0),
      gapOpen: summary.gapOpen + (entry.project.breakEvenGap > 0 ? 1 : 0),
      needsLinking: summary.needsLinking + (entry.linkedParts.length === 0 && entry.project.partsRemovedCount === 0 ? 1 : 0),
      linkedParts: summary.linkedParts + entry.linkedParts.length
    };
  }, {
    readyForTeardown: 0,
    profitUnlocked: 0,
    gapOpen: 0,
    needsLinking: 0,
    linkedParts: 0
  }), [donorProjects]);
  const assignedParts = useMemo(() =>
    selectedCarParts.filter((part) =>
      [read(part, "internalCode"), read(part, "name"), read(part, "oemNumber")].join(" ").toLowerCase().includes(partSearch.toLowerCase())),
  [partSearch, selectedCarParts]);
  const availableParts = useMemo(() =>
    unassignedParts.filter((part) =>
      [read(part, "internalCode"), read(part, "name"), read(part, "oemNumber")].join(" ").toLowerCase().includes(partSearch.toLowerCase())),
  [partSearch, unassignedParts]);
  const nextPartRecommendations = useMemo(() =>
    selectedCar ? buildPartRecommendations(selectedCarParts, unassignedParts) : [],
  [selectedCar, selectedCarParts, unassignedParts]);
  const breakEvenEntries = useMemo(() => donorProjects.slice()
    .sort((left, right) => {
      const leftProfitable = left.project.fullCost > 0 && left.project.breakEvenGap <= 0 ? 1 : 0;
      const rightProfitable = right.project.fullCost > 0 && right.project.breakEvenGap <= 0 ? 1 : 0;
      return leftProfitable - rightProfitable
        || right.project.recoveredPercent - left.project.recoveredPercent
        || left.project.breakEvenGap - right.project.breakEvenGap
        || carTitle(left.car, t).localeCompare(carTitle(right.car, t));
    }),
  [donorProjects, t]);
  const displayContextForCar = useCallback((car) => displayCurrencyContext({
    constants: appConstants,
    rates: currencyRates,
    baseCurrencyCode: read(car, "baseCurrencyCode"),
    counterCurrencyCode: read(car, "counterCurrencyCode")
  }), [appConstants, currencyRates]);
  const selectedDisplayContext = useMemo(
    () => displayContextForCar(selectedCar),
    [displayContextForCar, selectedCar]
  );
  const selectedPassportText = useMemo(() => {
    if (!selectedDonorProject) return "";
    return buildDonorPassportText(
      selectedDonorProject,
      (value) => displayMoneyFromBase(value, selectedDisplayContext),
      t
    );
  }, [selectedDisplayContext, selectedDonorProject, t]);

  const setFormValue = useCallback((key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  }, []);

  const fillFormFromCar = useCallback((car) => {
    if (!car) {
      setForm({ ...emptyForm, modelYear: String(new Date().getFullYear()) });
      return;
    }

    setForm({
      barcode: String(read(car, "barcode") || ""),
      supplierId: String(read(car, "supplierId") || ""),
      carModelId: String(read(car, "carModelId") || ""),
      modelYear: String(read(car, "modelYear") || ""),
      priceCurrency: String(read(car, "priceCurrency") || "USD"),
      price: String(read(car, "price") || ""),
      locationId: String(read(car, "locationId") || ""),
      isReceived: Boolean(read(car, "isReceived")),
      isShipped: Boolean(read(car, "isShipped")),
      partOut: String(read(car, "partOut") || ""),
      shipping: String(read(car, "shipping") || ""),
      customs: String(read(car, "customs") || ""),
      repairs: String(read(car, "repairs") || ""),
      expectedSellThroughRate: String(read(car, "expectedSellThroughRate") || "0.8")
    });
  }, []);

  const loadReferenceData = useCallback(async () => {
    try {
      const [nextSuppliers, nextModels, nextLocations, nextCurrencies, nextAppConstants] = await Promise.all([
        api.get("/api/suppliers?page=1&pageSize=500"),
        api.get("/api/carmodels"),
        api.get("/api/locations"),
        api.get("/api/currencies"),
        api.get("/api/appconstants")
      ]);
      const currencyRows = toArray(nextCurrencies);
      const appConstantRows = toArray(nextAppConstants);
      const referenceContext = displayCurrencyContext({ constants: appConstantRows, rates: currencyRows });
      setSuppliers(toArray(nextSuppliers));
      setCarModels(toArray(nextModels).filter((model) => read(model, "isActive") !== false));
      setLocations(toArray(nextLocations));
      setCurrencyRates(currencyRows);
      setAppConstants(appConstantRows);
      setCurrencyCodes(Array.from(new Set([
        "USD",
        referenceContext.baseCurrencyCode,
        referenceContext.counterCurrencyCode,
        referenceContext.code,
        ...currencyRows.map((currency) => read(currency, "code")).filter(Boolean)
      ])));
    } catch (error) {
      setStatus(error.message || t("usedCars.referenceLoadError", "Could not load used-car reference data."));
    }
  }, [api, t]);

  const loadCars = useCallback(async (preferredId) => {
    setIsLoading(true);
    setStatus(t("usedCars.loadingWorkspace", "Loading used car workspace..."));
    try {
      const nextCars = toArray(await api.get("/api/usedcars"));
      setCars(nextCars);
      setSelectedId((current) => {
        if (preferredId) return String(preferredId);
        if (current && nextCars.some((car) => String(itemId(car)) === String(current))) return current;
        return String(itemId(nextCars[0]) || "");
      });
      setStatus(t("usedCars.loadedCount", "{count} used cars loaded.", { count: nextCars.length }));
    } catch (error) {
      setCars([]);
      setImages([]);
      setStatus(error.message || t("usedCars.loadError", "Could not load used cars."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  const loadParts = useCallback(async () => {
    try {
      setParts(toArray(await api.list("/api/parts")));
    } catch (error) {
      setParts([]);
      setStatus(error.message || t("usedCars.partsLoadError", "Could not load linked parts."));
    }
  }, [api, t]);

  useEffect(() => {
    loadReferenceData();
    loadCars();
    loadParts();
  }, [loadCars, loadParts, loadReferenceData]);

  useEffect(() => {
    fillFormFromCar(selectedCar);
  }, [fillFormFromCar, selectedCar]);

  useEffect(() => {
    let cancelled = false;
    async function loadImages() {
      if (!selectedCar) {
        setImages([]);
        setSelectedImageId("");
        return;
      }

      try {
        const nextImages = toArray(await api.get(`/api/usedcars/${itemId(selectedCar)}/images`));
        if (cancelled) return;
        setImages(nextImages);
        setSelectedImageId(String(itemId(nextImages[0]) || ""));
      } catch (error) {
        if (cancelled) return;
        setImages([]);
        setSelectedImageId("");
        setStatus(error.message || t("usedCars.galleryLoadError", "Could not load this vehicle gallery."));
      }
    }

    loadImages();
    return () => {
      cancelled = true;
    };
  }, [api, selectedCar, t]);

  const buildRequest = useCallback(() => ({
    barcode: form.barcode.trim() || null,
    supplierId: toInt(form.supplierId),
    carModelId: toInt(form.carModelId),
    modelYear: toInt(form.modelYear),
    priceCurrency: String(form.priceCurrency || "USD").trim().toUpperCase(),
    price: toNumber(form.price),
    locationId: toInt(form.locationId),
    isReceived: Boolean(form.isReceived),
    isShipped: Boolean(form.isShipped),
    partOut: toNumber(form.partOut),
    shipping: toNumber(form.shipping),
    customs: toNumber(form.customs),
    repairs: toNumber(form.repairs),
    expectedSellThroughRate: toNumber(form.expectedSellThroughRate) || 0.8
  }), [form]);

  const startNew = useCallback(() => {
    setSelectedId("");
    fillFormFromCar(null);
    setStatus(t("usedCars.newReady", "New used car form ready."));
  }, [fillFormFromCar, t]);

  const saveCar = useCallback(async () => {
    const request = buildRequest();
    if (!request.carModelId || !request.supplierId || !request.locationId || !request.modelYear) {
      setStatus(t("usedCars.requiredBeforeSave", "Select model, supplier, location, and year before saving."));
      return;
    }

    if (request.expectedSellThroughRate <= 0 || request.expectedSellThroughRate > 1) {
      setStatus("Expected sell-through rate must be between 0 and 1.");
      return;
    }

    setIsSaving(true);
    try {
      let savedId = selectedCar ? itemId(selectedCar) : "";
      if (selectedCar) {
        await api.put(`/api/usedcars/${savedId}`, request);
      } else {
        savedId = await api.post("/api/usedcars", request);
      }
      setStatus(selectedCar ? t("usedCars.updated", "Used car updated.") : t("usedCars.created", "Used car created."));
      await loadCars(savedId);
      await loadParts();
    } catch (error) {
      setStatus(error.message || t("usedCars.saveError", "Could not save used car."));
    } finally {
      setIsSaving(false);
    }
  }, [api, buildRequest, loadCars, loadParts, selectedCar, t]);

  const deleteCar = useCallback(async () => {
    if (!selectedCar || !window.confirm(`${t("common.delete", "Delete")} ${carTitle(selectedCar, t)}?`)) return;
    setIsSaving(true);
    try {
      await api.delete(`/api/usedcars/${itemId(selectedCar)}`);
      setStatus(t("usedCars.deleted", "Used car deleted."));
      setSelectedId("");
      fillFormFromCar(null);
      await loadCars("");
      await loadParts();
    } catch (error) {
      setStatus(error.message || t("usedCars.deleteError", "Could not delete used car."));
    } finally {
      setIsSaving(false);
    }
  }, [api, fillFormFromCar, loadCars, loadParts, selectedCar, t]);

  const generateListing = useCallback(async () => {
    if (!selectedCar) return;
    setIsGeneratingListing(true);
    setStatus("");
    try {
      const pkg = await api.get(`/api/usedcars/${itemId(selectedCar)}/listing-package`);
      setListingPackage(pkg);
    } catch (error) {
      setStatus(error.message || t("usedCars.listingError", "Could not generate the listing package."));
    } finally {
      setIsGeneratingListing(false);
    }
  }, [api, selectedCar, t]);

  const uploadImages = useCallback(async (files) => {
    if (!selectedCar) {
      setStatus(t("usedCars.saveBeforePhotos", "Save or select a used car before adding photos."));
      return;
    }
    const selectedFiles = Array.from(files || []);
    if (selectedFiles.length === 0) return;

    setIsUploading(true);
    setStatus(t("usedCars.uploadingPhotos", "Uploading used-car photos..."));
    try {
      for (const file of selectedFiles) {
        const formData = new FormData();
        formData.append("image", file, file.name);
        await api.postForm(`/api/usedcars/${itemId(selectedCar)}/images`, formData);
      }
      const nextImages = toArray(await api.get(`/api/usedcars/${itemId(selectedCar)}/images`));
      setImages(nextImages);
      setSelectedImageId(String(itemId(nextImages[0]) || ""));
      setStatus(t("usedCars.imagesUploaded", "{count} image(s) uploaded.", { count: selectedFiles.length }));
    } catch (error) {
      setStatus(error.message || t("usedCars.uploadError", "Could not upload images."));
    } finally {
      setIsUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }, [api, selectedCar, t]);

  const deleteImage = useCallback(async () => {
    if (!selectedImage) return;
    setIsUploading(true);
    try {
      await api.delete(`/api/usedcars/images/${itemId(selectedImage)}`);
      const nextImages = selectedCar ? toArray(await api.get(`/api/usedcars/${itemId(selectedCar)}/images`)) : [];
      setImages(nextImages);
      setSelectedImageId(String(itemId(nextImages[0]) || ""));
      setStatus(t("usedCars.imageDeleted", "Image deleted."));
    } catch (error) {
      setStatus(error.message || t("usedCars.imageDeleteError", "Could not delete image."));
    } finally {
      setIsUploading(false);
    }
  }, [api, selectedCar, selectedImage, t]);

  const setPartUsedCar = useCallback(async (partId, nextUsedCarId) => {
    try {
      await api.put(`/api/parts/${partId}/usedcar`, { usedCarId: nextUsedCarId });
      await loadParts();
      await loadCars(selectedId);
      setStatus(nextUsedCarId ? t("usedCars.partAssigned", "Part assigned to used car.") : t("usedCars.partRemoved", "Part removed from used car."));
    } catch (error) {
      setStatus(error.message || t("usedCars.partUpdateError", "Could not update part assignment."));
    }
  }, [api, loadCars, loadParts, selectedId, t]);

  const copyDonorPassport = useCallback(async () => {
    if (!selectedPassportText) return;

    try {
      await navigator.clipboard.writeText(selectedPassportText);
      setStatus(t("usedCars.passportCopied", "Donor passport copied."));
    } catch (error) {
      setStatus(error.message || t("usedCars.passportCopyError", "Could not copy donor passport."));
    }
  }, [selectedPassportText, t]);

  const openDonorWhatsapp = useCallback(() => {
    if (!selectedPassportText) return;
    window.open(`https://wa.me/?text=${encodeURIComponent(selectedPassportText)}`, "_blank", "noopener,noreferrer");
    setStatus(t("usedCars.whatsappDraftReady", "WhatsApp donor summary opened."));
  }, [selectedPassportText, t]);

  const selectedImageIndex = Math.max(0, images.findIndex((image) => String(itemId(image)) === String(itemId(selectedImage))));
  const selectedModel = carModels.find((model) => String(read(model, "id")) === String(form.carModelId));

  return h("section", { className: "screen used-cars-screen" },
    isGalleryOpen && h(GalleryModal, {
      car: selectedCar,
      images,
      index: Math.min(galleryIndex, Math.max(images.length - 1, 0)),
      onClose: () => setIsGalleryOpen(false),
      onIndex: setGalleryIndex,
      t
    }),
    listingPackage && h(ListingPackageModal, {
      pkg: listingPackage,
      onClose: () => setListingPackage(null),
      t
    }),
    h(PageHeader, {
      title: t("usedCars.title", "Used Cars"),
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: () => { loadCars(selectedId); loadParts(); },
        disabled: isLoading
      }, isLoading ? t("common.loading", "Loading") : t("common.refresh", "Refresh"))
    }),
    h(StatusLine, { status }),
    h(DonorCockpit, {
      entries: donorProjects,
      filteredEntries: filteredDonorProjects,
      metrics: donorMetrics,
      search: carSearch,
      onSearch: setCarSearch,
      filter: carFilter,
      onFilter: setCarFilter,
      onSelect: (id) => setSelectedId(String(id)),
      displayContextForCar,
      t
    }),
    h(BreakEvenRace, {
      entries: breakEvenEntries,
      selectedId,
      onSelect: (id) => setSelectedId(String(id)),
      displayContextForCar,
      t
    }),
    h("section", { className: "used-car-layout" },
      h("div", { className: "used-car-primary" },
        h("article", { className: "panel used-car-gallery" },
          h("div", { className: "admin-panel-header" },
            h("h3", null, t("usedCars.gallery", "Gallery")),
            h("div", { className: "row-actions" },
              h("button", { className: "secondary-button", type: "button", onClick: () => fileInputRef.current?.click(), disabled: isUploading }, t("usedCars.addPhotos", "Add Photos")),
              h("button", { className: "secondary-button", type: "button", onClick: generateListing, disabled: !selectedCar || isGeneratingListing }, isGeneratingListing ? t("usedCars.generatingListing", "Generating...") : t("usedCars.generateListing", "Generate Listing")),
              h("button", { className: "secondary-button", type: "button", onClick: () => { setGalleryIndex(selectedImageIndex); setIsGalleryOpen(true); }, disabled: images.length === 0 }, t("usedCars.fullscreen", "Fullscreen")),
              h("button", { className: "secondary-button danger-button", type: "button", onClick: deleteImage, disabled: !selectedImage || isUploading }, t("usedCars.deletePhoto", "Delete Photo"))
            )
          ),
          h("input", {
            ref: fileInputRef,
            type: "file",
            accept: "image/*",
            multiple: true,
            hidden: true,
            onChange: (event) => uploadImages(event.target.files)
          }),
          selectedCar
            ? h("div", { className: "gallery-preview" },
              h("button", {
                className: "gallery-preview-frame",
                type: "button",
                onClick: () => { setGalleryIndex(selectedImageIndex); setIsGalleryOpen(true); },
                disabled: images.length === 0
              },
                selectedImage && imageSrc(selectedImage)
                  ? h("img", { src: imageSrc(selectedImage), alt: carTitle(selectedCar, t) })
                  : h("span", null, initials(carTitle(selectedCar, t)))
              ),
              h("div", null,
                h("h2", null, carTitle(selectedCar, t)),
                h("p", null, `${read(selectedCar, "location") || t("usedCars.location", "Location")} / ${carPrice(selectedCar)}`),
                h("div", { className: "gallery-thumb-rail inline" },
                  images.map((image, index) =>
                    h("button", {
                      key: itemId(image) || index,
                      className: String(itemId(image)) === String(itemId(selectedImage)) ? "gallery-thumb active" : "gallery-thumb",
                      type: "button",
                      onClick: () => setSelectedImageId(String(itemId(image)))
                    }, imageSrc(image) ? h("img", { src: imageSrc(image), alt: `${index + 1}` }) : h("span", null, index + 1))
                  ),
                  images.length === 0 && h("p", { className: "empty-state" }, t("usedCars.noPhotos", "No photos uploaded for this vehicle yet."))
                )
              )
            )
            : h("p", { className: "empty-state" }, t("usedCars.selectForGallery", "Select a used car or save a new one to manage its gallery."))
        ),
        h("article", { className: "panel" },
          h("h3", null, t("usedCars.inventory", "Inventory")),
          h("div", { className: "used-car-list-frame" },
            filteredDonorProjects.map((entry, index) => {
              const { car, project, action, linkedParts } = entry;
              const active = String(itemId(car)) === String(selectedId);
              return h("button", {
                key: itemId(car) || index,
                className: active ? "used-car-row active" : "used-car-row",
                type: "button",
                onClick: () => setSelectedId(String(itemId(car)))
              },
                h("span", { className: "avatar" }, initials(carTitle(car, t))),
                h("div", null,
                  h("strong", null, carTitle(car, t)),
                  h("small", null, `${read(car, "supplierName") || t("usedCars.supplier", "Supplier")} / ${read(car, "barcode") || "-"}`),
                  h("small", { className: `donor-row-action ${action.tone}` }, `${action.label} / ${project.recoveredPercent}% / ${linkedParts.length} linked`)
                ),
                h("b", null, carPrice(car))
              );
            }),
            filteredDonorProjects.length === 0 && h("p", { className: "empty-state" }, t("usedCars.noCars", "No used cars returned."))
          )
        ),
        selectedDonorProject && h(DonorPassport, {
          entry: selectedDonorProject,
          displayContext: selectedDisplayContext,
          t,
          onCopy: copyDonorPassport,
          onWhatsapp: openDonorWhatsapp
        }),
        selectedCar && h("article", { className: "panel" },
          h("h3", null, t("usedCars.vehicleDetails", "Vehicle Details")),
          h("div", { className: "detail-grid" },
            h(DetailTile, { label: t("usedCars.supplier", "Supplier"), value: read(selectedCar, "supplierName") }),
            h(DetailTile, { label: "Barcode", value: read(selectedCar, "barcode") }),
            h(DetailTile, { label: t("usedCars.location", "Location"), value: read(selectedCar, "location") }),
            h(DetailTile, { label: t("usedCars.price", "Price"), value: carPrice(selectedCar) }),
            h(DetailTile, { label: "Full Cost", value: displayMoneyFromBase(read(selectedCar, "fullCostBase"), selectedDisplayContext) }),
            h(DetailTile, { label: "Sell-Through", value: `${Math.round(toNumber(read(selectedCar, "expectedSellThroughRate")) * 100)}%` }),
            h(DetailTile, { label: "Net P/L", value: displayMoneyFromBase(read(selectedCar, "netProfitLossBase"), selectedDisplayContext) }),
            selectedDonorProject && h(DetailTile, { label: t("usedCars.nextAction", "Next Action"), value: selectedDonorProject.action.label })
          )
        ),
        selectedCar && h(ProfitProjectMap, {
          car: selectedCar,
          recommendations: nextPartRecommendations,
          onAssignPart: (partId) => setPartUsedCar(partId, itemId(selectedCar)),
          displayContext: selectedDisplayContext,
          t
        })
      ),
      h("aside", { className: "used-car-side" },
        h("article", { className: "panel" },
          h("div", { className: "admin-panel-header" },
            h("h3", null, selectedCar ? t("usedCars.editUsedCar", "Edit Used Car") : t("usedCars.newUsedCar", "New Used Car")),
            h("div", { className: "row-actions" },
              h("button", { className: "secondary-button", type: "button", onClick: startNew }, t("common.new", "New")),
              h("button", { className: "primary-button", type: "button", onClick: saveCar, disabled: isSaving }, selectedCar ? t("common.save", "Save") : t("common.create", "Create")),
              h("button", { className: "secondary-button danger-button", type: "button", onClick: deleteCar, disabled: !selectedCar || isSaving }, t("common.delete", "Delete"))
            )
          ),
          h("div", { className: "editor-grid two" },
            h(InputField, { label: "Barcode", value: form.barcode, onChange: (value) => setFormValue("barcode", value) }),
            h(SelectField, {
              label: t("usedCars.supplier", "Supplier"),
              value: form.supplierId,
              onChange: (value) => setFormValue("supplierId", value),
              options: suppliers,
              getValue: (supplier) => itemId(supplier),
              getLabel: (supplier) => read(supplier, "name") || `#${itemId(supplier)}`
            }),
            h(SelectField, {
              label: t("usedCars.carModel", "Car Model"),
              value: form.carModelId,
              onChange: (value) => setFormValue("carModelId", value),
              options: carModels,
              getValue: (model) => itemId(model),
              getLabel: modelTitle
            }),
            h(SelectField, {
              label: t("usedCars.location", "Location"),
              value: form.locationId,
              onChange: (value) => setFormValue("locationId", value),
              options: locations,
              getValue: (location) => itemId(location),
              getLabel: (location) => read(location, "name") || `#${itemId(location)}`
            }),
            h(InputField, { label: t("usedCars.modelYear", "Model Year"), value: form.modelYear, type: "number", onChange: (value) => setFormValue("modelYear", value) }),
            h(InputField, { label: t("usedCars.price", "Price"), value: form.price, type: "number", onChange: (value) => setFormValue("price", value) })
          ),
          h("div", { className: "currency-row" },
            currencyCodes.slice(0, 8).map((code) =>
              h("button", {
                key: code,
                className: String(form.priceCurrency).toUpperCase() === String(code).toUpperCase() ? "chip active" : "chip",
                type: "button",
                onClick: () => setFormValue("priceCurrency", code)
              }, code)
            )
          ),
          h("div", { className: "editor-grid two" },
            h(ToggleField, { label: t("usedCars.received", "Received"), value: form.isReceived, onChange: (value) => setFormValue("isReceived", value) }),
            h(ToggleField, { label: t("usedCars.shipped", "Shipped"), value: form.isShipped, onChange: (value) => setFormValue("isShipped", value) }),
            h(InputField, { label: t("usedCars.partOut", "Part-Out"), value: form.partOut, type: "number", onChange: (value) => setFormValue("partOut", value) }),
            h(InputField, { label: t("usedCars.shipping", "Shipping"), value: form.shipping, type: "number", onChange: (value) => setFormValue("shipping", value) }),
            h(InputField, { label: t("usedCars.customs", "Customs"), value: form.customs, type: "number", onChange: (value) => setFormValue("customs", value) }),
            h(InputField, { label: t("usedCars.repairs", "Repairs"), value: form.repairs, type: "number", onChange: (value) => setFormValue("repairs", value) }),
            h(InputField, { label: "Expected Sell-Through Rate", value: form.expectedSellThroughRate, type: "number", onChange: (value) => setFormValue("expectedSellThroughRate", value) })
          ),
          h("p", { className: "empty-state" }, selectedModel ? modelTitle(selectedModel) : t("usedCars.carModel", "Car Model"))
        ),
        h("article", { className: "panel" },
          h("h3", null, t("usedCars.linkedParts", "Linked Parts")),
          selectedCar
            ? h("div", { className: "linked-parts" },
              h("input", { value: partSearch, onChange: (event) => setPartSearch(event.target.value), placeholder: "Search by code, name, or OEM" }),
              h("div", { className: "part-list-columns" },
                h("section", null,
                  h("h4", null, `${t("usedCars.remove", "Remove")} (${assignedParts.length})`),
                  h("div", { className: "part-list-frame" },
                    assignedParts.map((part) => h(PartRow, {
                      key: itemId(part),
                      part,
                      actionTitle: t("usedCars.remove", "Remove"),
                      onClick: () => setPartUsedCar(itemId(part), null)
                    })),
                    assignedParts.length === 0 && h("p", { className: "empty-state" }, t("usedCars.noLinkedParts", "No parts linked to this car."))
                  )
                ),
                h("section", null,
                  h("h4", null, `${t("usedCars.assign", "Assign")} (${availableParts.length})`),
                  h("div", { className: "part-list-frame" },
                    availableParts.map((part) => h(PartRow, {
                      key: itemId(part),
                      part,
                      actionTitle: t("usedCars.assign", "Assign"),
                      onClick: () => setPartUsedCar(itemId(part), itemId(selectedCar))
                    })),
                    availableParts.length === 0 && h("p", { className: "empty-state" }, t("usedCars.noAvailableParts", "No available parts match this search."))
                  )
                )
              )
            )
            : h("p", { className: "empty-state" }, t("usedCars.selectForGallery", "Select a used car or save a new one to manage its gallery."))
        )
      )
    )
  );
}
