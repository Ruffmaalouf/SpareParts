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
  repairs: ""
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
  return toNumber(read(part, "averagePrice")) || toNumber(read(part, "costPrice"));
}

function partAvailableQuantity(part) {
  return Math.max(
    toNumber(read(part, "availableQuantity")),
    toNumber(read(part, "stockQuantity")),
    0
  );
}

function rankPart(part, isLinked) {
  const salePrice = toNumber(read(part, "salePrice"));
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

function buildProfitProject(car) {
  const bought = toNumber(read(car, "purchaseCostBase", "priceBase"));
  const fullCost = toNumber(read(car, "fullCostBase", "grandTotalBase"));
  const teardownCosts = Math.max(fullCost - bought, 0);
  const partsRemovedCount = toNumber(read(car, "partsRemovedCount"));
  const partsRemovedValue = toNumber(read(car, "partsRemovedValueBase"));
  const soldQuantity = toNumber(read(car, "partsSoldQuantity"));
  const soldAmount = toNumber(read(car, "partsSoldAmountBase", "salePriceBase"));
  const remainingQuantity = toNumber(read(car, "remainingStockQuantity"));
  const remainingStockValue = toNumber(read(car, "remainingStockValueBase"));
  const recoveredValue = soldAmount + remainingStockValue;
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
      h("small", null, `${t("usedCars.stock", "Stock")} ${item.quantity.toLocaleString()} / ${t("usedCars.margin", "Margin")} ${item.marginPercent}%`)
    ),
    item.isLinked
      ? h("span", { className: "profit-status-chip" }, t("usedCars.linked", "Linked"))
      : h("button", { className: "secondary-button", type: "button", onClick: () => onAssign(itemId(part)) }, t("usedCars.assign", "Assign"))
  );
}

function ProfitProjectMap({ car, recommendations, onAssignPart, t, displayContext }) {
  if (!car) return null;

  const project = buildProfitProject(car);
  const displayMoney = (value) => displayMoneyFromBase(value, displayContext);
  const resultTone = project.netProfitLoss >= 0 ? "positive" : "negative";
  const soldWidth = Math.min(project.soldPercent, 100);
  const stockWidth = Math.min(project.stockPercent, Math.max(100 - soldWidth, 0));
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
        h("span", { className: "profit-progress-segment sold", style: { width: `${soldWidth}%` } }),
        h("span", { className: "profit-progress-segment stock", style: { width: `${stockWidth}%` } })
      ),
      h("div", { className: "profit-legend" },
        h("span", null, t("usedCars.soldCash", "Sold cash")),
        h("span", null, t("usedCars.stockValue", "Stock value"))
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
        detail: t("usedCars.soldPlusStock", "Sold plus remaining stock")
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
  const [partSearch, setPartSearch] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [isGalleryOpen, setIsGalleryOpen] = useState(false);
  const [galleryIndex, setGalleryIndex] = useState(0);

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
      repairs: String(read(car, "repairs") || "")
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
      setParts(toArray(await api.get("/api/parts?page=1&pageSize=500")));
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
    repairs: toNumber(form.repairs)
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
    h("section", { className: "used-car-layout" },
      h("div", { className: "used-car-primary" },
        h("article", { className: "panel used-car-gallery" },
          h("div", { className: "admin-panel-header" },
            h("h3", null, t("usedCars.gallery", "Gallery")),
            h("div", { className: "row-actions" },
              h("button", { className: "secondary-button", type: "button", onClick: () => fileInputRef.current?.click(), disabled: isUploading }, t("usedCars.addPhotos", "Add Photos")),
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
            cars.map((car, index) => {
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
                  h("small", null, `${read(car, "supplierName") || t("usedCars.supplier", "Supplier")} / ${read(car, "barcode") || "-"}`)
                ),
                h("b", null, carPrice(car))
              );
            }),
            cars.length === 0 && h("p", { className: "empty-state" }, t("usedCars.noCars", "No used cars returned."))
          )
        ),
        selectedCar && h("article", { className: "panel" },
          h("h3", null, t("usedCars.vehicleDetails", "Vehicle Details")),
          h("div", { className: "detail-grid" },
            h(DetailTile, { label: t("usedCars.supplier", "Supplier"), value: read(selectedCar, "supplierName") }),
            h(DetailTile, { label: "Barcode", value: read(selectedCar, "barcode") }),
            h(DetailTile, { label: t("usedCars.location", "Location"), value: read(selectedCar, "location") }),
            h(DetailTile, { label: t("usedCars.price", "Price"), value: carPrice(selectedCar) }),
            h(DetailTile, { label: "Full Cost", value: displayMoneyFromBase(read(selectedCar, "fullCostBase"), selectedDisplayContext) }),
            h(DetailTile, { label: "Net P/L", value: displayMoneyFromBase(read(selectedCar, "netProfitLossBase"), selectedDisplayContext) })
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
            h(InputField, { label: t("usedCars.repairs", "Repairs"), value: form.repairs, type: "number", onChange: (value) => setFormValue("repairs", value) })
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
