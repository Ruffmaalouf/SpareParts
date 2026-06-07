const React = require("react");
const {
  ActivityIndicator,
  Alert,
  Animated,
  FlatList,
  Image,
  Linking,
  Modal,
  PanResponder,
  Pressable,
  ScrollView,
  Share,
  Switch,
  Text,
  TextInput,
  useWindowDimensions,
  View
} = require("react-native");
const ImagePicker = require("expo-image-picker");
const { useSafeAreaInsets } = require("react-native-safe-area-context");
const { EmptyState, Panel, ScreenHeader, ScreenScroll, StatusText } = require("../components/ui");
const { displayCurrencyContext, displayMoneyFromBase, initials, money } = require("../core/formatters");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useRef, useState } = React;
const el = React.createElement;

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
    const value = row && row[key];
    if (value !== undefined && value !== null && value !== "") return value;

    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row && row[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }

  return "";
}

function itemId(row) {
  return read(row, "id");
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

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

function touchDistance(touches) {
  if (!touches || touches.length < 2) return 0;
  const [first, second] = touches;
  const dx = first.pageX - second.pageX;
  const dy = first.pageY - second.pageY;
  return Math.sqrt((dx * dx) + (dy * dy));
}

function toInt(value) {
  const parsed = Number.parseInt(String(value || ""), 10);
  return Number.isFinite(parsed) ? parsed : 0;
}

function carTitle(car, t) {
  const year = read(car, "modelYear");
  const name = read(car, "car") || (t ? t("usedCars.usedCar", "Used car") : "Used car");
  return `${year || ""} ${name}`.trim();
}

function modelTitle(model) {
  const brand = read(model, "carBrandName");
  const name = read(model, "name");
  const body = read(model, "bodyType");
  return `${brand || ""} ${name || ""}${body ? ` (${body})` : ""}`.trim();
}

function carPrice(car) {
  const currency = read(car, "priceCurrency") || read(car, "baseCurrencyCode") || "USD";
  return money(read(car, "price"), currency);
}

function imageUri(image) {
  const imageData = read(image, "imageData");
  if (!imageData) return "";
  const mimeType = read(image, "mimeType") || "image/jpeg";
  return `data:${mimeType};base64,${imageData}`;
}

function statusLabel(car, t) {
  const badges = [];
  if (read(car, "isReceived")) badges.push(t ? t("usedCars.received", "Received") : "Received");
  if (read(car, "isShipped")) badges.push(t ? t("usedCars.shipped", "Shipped") : "Shipped");
  if (Number(read(car, "partsRemovedCount") || 0) > 0) badges.push(t ? t("usedCars.partsCount", "{count} parts", { count: read(car, "partsRemovedCount") }) : `${read(car, "partsRemovedCount")} parts`);
  return badges.length ? badges : [t ? t("usedCars.inventory", "Inventory") : "Inventory"];
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
  const expectedSale = salePrice * quantity;
  const expectedMargin = (salePrice - unitCost) * quantity;
  return {
    part,
    isLinked,
    quantity,
    salePrice,
    unitCost,
    expectedSale,
    expectedMargin,
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
    .slice(0, 3);
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

function buildProfitProject(car) {
  const bought = toNumber(read(car, "purchaseCostBase", "priceBase"));
  const fullCost = toNumber(read(car, "fullCostBase", "grandTotalBase"));
  const soldAmount = read(car, "isWholesaleSold")
    ? toNumber(read(car, "salePriceBase"))
    : toNumber(read(car, "partsSoldAmountBase"));
  const remainingStockValue = toNumber(read(car, "remainingStockValueBase"));
  const netProfitLossValue = read(car, "netProfitLossBase");
  const netProfitLoss = netProfitLossValue === "" ? soldAmount - fullCost : toNumber(netProfitLossValue);

  return {
    bought,
    fullCost,
    partsRemovedCount: toNumber(read(car, "partsRemovedCount")),
    partsRemovedValue: toNumber(read(car, "partsRemovedValueBase")),
    soldQuantity: toNumber(read(car, "partsSoldQuantity")),
    soldAmount,
    remainingQuantity: toNumber(read(car, "remainingStockQuantity")),
    remainingStockValue,
    netProfitLoss,
    breakEvenGap: Math.max(fullCost - soldAmount, 0),
    recoveredPercent: percent(soldAmount, fullCost)
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
    action: buildDonorAction(car, project, linkedParts, t),
    recommendedValueBase: recommendations.reduce((sum, item) => sum + partValueInCarBase(item, car), 0)
  };
}

function matchesDonorProject(entry, search) {
  if (!search.trim()) return true;
  const needle = search.trim().toLowerCase();
  const car = entry.car;
  return [
    carTitle(car),
    read(car, "barcode"),
    read(car, "supplierName"),
    read(car, "location"),
    read(car, "wholesaleBuyerName"),
    entry.action.label,
    entry.action.detail
  ].join(" ").toLowerCase().includes(needle);
}

function matchesDonorFilter(entry, filter) {
  if (filter === "profit") return entry.project.breakEvenGap <= 0;
  if (filter === "gap") return entry.project.breakEvenGap > 0;
  if (filter === "needs-linking") return entry.linkedParts.length === 0 && entry.project.partsRemovedCount === 0;
  if (filter === "action") return ["receive", "ship", "cost", "link", "harvest", "sell-stock", "quote"].includes(entry.action.key);
  if (filter === "transit") return !read(entry.car, "isReceived") || !read(entry.car, "isShipped");
  return true;
}

function matchesText(row, term, keys) {
  if (!term.trim()) return true;
  const needle = term.trim().toLowerCase();
  return keys.some((key) => String(read(row, key) || "").toLowerCase().includes(needle));
}

function optionKey(option) {
  return String(read(option, "id", "locationId"));
}

function InputField({ label, value, onChangeText, keyboardType, placeholder }) {
  const { styles, palette } = useTheme();

  return el(View, { style: styles.usedCarField },
    el(Text, { style: styles.fieldLabel }, label),
    el(TextInput, {
      style: styles.input,
      value: String(value ?? ""),
      onChangeText,
      keyboardType,
      placeholder,
      placeholderTextColor: palette.soft
    })
  );
}

function ToggleField({ label, value, onValueChange }) {
  const { styles, palette } = useTheme();

  return el(View, { style: styles.usedCarToggle },
    el(Text, { style: styles.usedCarToggleText }, label),
    el(Switch, {
      value: Boolean(value),
      onValueChange,
      trackColor: { false: palette.line, true: palette.accent },
      thumbColor: palette.text
    })
  );
}

function Selector({ label, value, search, onSearch, options, onSelect, getTitle, getMeta }) {
  const { styles, palette, t } = useTheme();
  const selected = options.find((item) => optionKey(item) === String(value));
  const visibleOptions = options.filter((item) =>
    matchesText(item, search, ["name", "carBrandName", "bodyType", "phone", "email"])
  ).slice(0, 8);

  return el(View, { style: styles.usedCarSelector },
    el(Text, { style: styles.fieldLabel }, label),
    el(TextInput, {
      style: styles.input,
      value: search,
      onChangeText: onSearch,
      placeholder: selected ? getTitle(selected) : t("usedCars.searchLabel", "Search {label}", { label: label.toLowerCase() }),
      placeholderTextColor: palette.soft
    }),
    el(View, { style: styles.usedCarChipWrap },
      visibleOptions.map((item) => {
        const key = optionKey(item);
        const active = key === String(value);
        return el(Pressable, {
          key,
          style: [styles.usedCarSelectChip, active && styles.usedCarSelectChipActive],
          onPress: () => onSelect(key)
        },
          el(Text, { style: [styles.usedCarSelectChipText, active && styles.usedCarSelectChipTextActive], numberOfLines: 1 }, getTitle(item)),
          Boolean(getMeta && getMeta(item)) && el(Text, { style: styles.usedCarSelectChipMeta, numberOfLines: 1 }, getMeta(item))
        );
      }),
      visibleOptions.length === 0 && el(Text, { style: styles.usedCarHelpText }, t("usedCars.noMatches", "No matches."))
    )
  );
}

function DetailTile({ label, value }) {
  const { styles } = useTheme();

  return el(View, { style: styles.usedCarDetailTile },
    el(Text, { style: styles.usedCarDetailLabel }, label),
    el(Text, { style: styles.usedCarDetailValue, numberOfLines: 2 }, value || "-")
  );
}

function PartRow({ part, actionTitle, onPress }) {
  const { styles, t } = useTheme();

  return el(View, { style: styles.usedCarPartRow },
    el(View, { style: styles.usedCarPartCopy },
      el(Text, { style: styles.usedCarPartTitle, numberOfLines: 1 }, read(part, "internalCode") || `#${itemId(part)}`),
      el(Text, { style: styles.usedCarPartMeta, numberOfLines: 2 }, `${read(part, "name") || t("usedCars.part", "Part")}${read(part, "oemNumber") ? ` - ${read(part, "oemNumber")}` : ""}`)
    ),
    el(Pressable, { style: styles.usedCarMiniButton, onPress },
      el(Text, { style: styles.usedCarMiniButtonText }, actionTitle)
    )
  );
}

function UsedCarsScreen({ api }) {
  const { width, height } = useWindowDimensions();
  const insets = useSafeAreaInsets();
  const { styles, palette, t } = useTheme();
  const galleryListRef = useRef(null);
  const zoomScale = useRef(new Animated.Value(1)).current;
  const zoomPan = useRef(new Animated.ValueXY({ x: 0, y: 0 })).current;
  const zoomLevelRef = useRef(1);
  const zoomPanRef = useRef({ x: 0, y: 0 });
  const zoomPanStartRef = useRef({ x: 0, y: 0 });
  const zoomStartDistanceRef = useRef(0);
  const zoomStartScaleRef = useRef(1);
  const fullscreenSlideHeight = Math.max(height - insets.top - insets.bottom - 154, 320);
  const isWide = width >= 900;
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
  const [form, setForm] = useState(emptyForm);
  const [supplierSearch, setSupplierSearch] = useState("");
  const [modelSearch, setModelSearch] = useState("");
  const [locationSearch, setLocationSearch] = useState("");
  const [carSearch, setCarSearch] = useState("");
  const [carFilter, setCarFilter] = useState("all");
  const [partSearch, setPartSearch] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isImagesLoading, setIsImagesLoading] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [isPartsLoading, setIsPartsLoading] = useState(false);
  const [isGalleryOpen, setIsGalleryOpen] = useState(false);
  const [galleryIndex, setGalleryIndex] = useState(0);
  const [listingPackage, setListingPackage] = useState(null);
  const [isListingOpen, setIsListingOpen] = useState(false);
  const [isGeneratingListing, setIsGeneratingListing] = useState(false);
  const [zoomLevel, setZoomLevel] = useState(1);

  const selectedCar = useMemo(
    () => cars.find((car) => String(itemId(car)) === String(selectedId)) || null,
    [cars, selectedId]
  );
  const selectedImage = useMemo(
    () => images.find((image) => String(itemId(image)) === String(selectedImageId)) || images[0] || null,
    [images, selectedImageId]
  );
  const selectedImageIndex = useMemo(() => {
    const index = images.findIndex((image) => String(itemId(image)) === String(itemId(selectedImage)));
    return index >= 0 ? index : 0;
  }, [images, selectedImage]);
  const selectedImageUri = selectedImage ? imageUri(selectedImage) : "";
  const selectedModel = useMemo(
    () => carModels.find((model) => optionKey(model) === String(form.carModelId)),
    [carModels, form.carModelId]
  );
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
      const leftSold = left.action.key === "sold" ? 1 : 0;
      const rightSold = right.action.key === "sold" ? 1 : 0;
      return leftSold - rightSold
        || (right.project.breakEvenGap > 0 ? 1 : 0) - (left.project.breakEvenGap > 0 ? 1 : 0)
        || right.project.breakEvenGap - left.project.breakEvenGap
        || carTitle(left.car, t).localeCompare(carTitle(right.car, t));
    }),
  [cars, partsByUsedCarId, t, unassignedParts]);
  const filteredDonorProjects = useMemo(() => donorProjects.filter((entry) =>
    matchesDonorProject(entry, carSearch) && matchesDonorFilter(entry, carFilter)
  ), [carFilter, carSearch, donorProjects]);
  const selectedDonorProject = useMemo(() =>
    donorProjects.find((entry) => String(itemId(entry.car)) === String(selectedId)) || null,
  [donorProjects, selectedId]);
  const donorMetrics = useMemo(() => donorProjects.reduce((summary, entry) => ({
    readyForTeardown: summary.readyForTeardown + (read(entry.car, "isReceived") && read(entry.car, "isShipped") && !read(entry.car, "isWholesaleSold") ? 1 : 0),
    profitUnlocked: summary.profitUnlocked + (entry.project.fullCost > 0 && entry.project.breakEvenGap <= 0 ? 1 : 0),
    gapOpen: summary.gapOpen + (entry.project.breakEvenGap > 0 ? 1 : 0),
    needsLinking: summary.needsLinking + (entry.linkedParts.length === 0 && entry.project.partsRemovedCount === 0 ? 1 : 0),
    linkedParts: summary.linkedParts + entry.linkedParts.length
  }), {
    readyForTeardown: 0,
    profitUnlocked: 0,
    gapOpen: 0,
    needsLinking: 0,
    linkedParts: 0
  }), [donorProjects]);
  const assignedParts = useMemo(() =>
    parts.filter((part) =>
      String(read(part, "usedCarId") || "") === String(selectedId)
        && matchesText(part, partSearch, ["internalCode", "name", "oemNumber"])
    ),
  [parts, partSearch, selectedId]);
  const availableParts = useMemo(() =>
    parts.filter((part) =>
      !read(part, "usedCarId")
        && matchesText(part, partSearch, ["internalCode", "name", "oemNumber"])
    ),
  [parts, partSearch]);

  const setFormValue = useCallback((key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  }, []);

  const fillFormFromCar = useCallback((car) => {
    if (!car) {
      setForm({ ...emptyForm, modelYear: String(new Date().getFullYear()) });
      setSupplierSearch("");
      setModelSearch("");
      setLocationSearch("");
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
    setSupplierSearch(read(car, "supplierName") || "");
    setModelSearch(read(car, "car") || "");
    setLocationSearch(read(car, "location") || "");
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
      const codes = Array.from(new Set([
        "USD",
        referenceContext.baseCurrencyCode,
        referenceContext.counterCurrencyCode,
        referenceContext.code,
        ...currencyRows.map((currency) => read(currency, "code")).filter(Boolean)
      ]));

      setSuppliers(toArray(nextSuppliers));
      setCarModels(toArray(nextModels).filter((model) => read(model, "isActive") !== false));
      setLocations(toArray(nextLocations));
      setCurrencyRates(currencyRows);
      setAppConstants(appConstantRows);
      setCurrencyCodes(codes);
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
    setIsPartsLoading(true);
    try {
      setParts(toArray(await api.list("/api/parts")));
    } catch (error) {
      setParts([]);
      setStatus(error.message || t("usedCars.partsLoadError", "Could not load linked parts."));
    } finally {
      setIsPartsLoading(false);
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
    if (!selectedCar) {
      setImages([]);
      setSelectedImageId("");
      return undefined;
    }

    let isMounted = true;
    async function loadImages() {
      setIsImagesLoading(true);
      try {
        const nextImages = toArray(await api.get(`/api/usedcars/${itemId(selectedCar)}/images`));
        if (!isMounted) return;
        setImages(nextImages);
        setSelectedImageId(String(itemId(nextImages[0]) || ""));
      } catch (error) {
        if (!isMounted) return;
        setImages([]);
        setSelectedImageId("");
        setStatus(error.message || t("usedCars.galleryLoadError", "Could not load this vehicle gallery."));
      } finally {
        if (isMounted) setIsImagesLoading(false);
      }
    }

    loadImages();
    return () => {
      isMounted = false;
    };
  }, [api, selectedCar, t]);

  const startNew = useCallback(() => {
    setSelectedId("");
    fillFormFromCar(null);
    setStatus(t("usedCars.newReady", "New used car form ready."));
  }, [fillFormFromCar, t]);

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

  const saveCar = useCallback(async () => {
    const request = buildRequest();
    if (!request.carModelId || !request.supplierId || !request.locationId || !request.modelYear) {
      setStatus(t("usedCars.requiredBeforeSave", "Select model, supplier, location, and year before saving."));
      return;
    }

    if (request.isReceived && request.customs <= 0) {
      setStatus(t("usedCars.customsRequired", "Customs should be different than 0 when the car is marked as received."));
      return;
    }

    if (request.expectedSellThroughRate <= 0 || request.expectedSellThroughRate > 1) {
      setStatus(t("usedCars.sellThroughInvalid", "Expected sell-through rate must be between 0 and 1."));
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
    if (!selectedCar) return;

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

  const confirmDeleteCar = useCallback(() => {
    if (!selectedCar) {
      setStatus(t("usedCars.selectFirst", "Select a used car first."));
      return;
    }

    Alert.alert(
      t("usedCars.deleteQuestion", "Delete used car?"),
      t("usedCars.deleteMessage", "Delete {name} from inventory?", { name: carTitle(selectedCar, t) }),
      [
        { text: t("common.cancel", "Cancel"), style: "cancel" },
        { text: t("common.delete", "Delete"), style: "destructive", onPress: () => { deleteCar(); } }
      ]
    );
  }, [deleteCar, selectedCar, t]);

  const uploadImages = useCallback(async () => {
    if (!selectedCar) {
      setStatus(t("usedCars.saveBeforePhotos", "Save or select a used car before adding photos."));
      return;
    }

    try {
      const permission = await ImagePicker.requestMediaLibraryPermissionsAsync();
      if (!permission.granted) {
        setStatus(t("usedCars.photoPermission", "Photo permission is required to add used-car images."));
        return;
      }

      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ["images"],
        allowsMultipleSelection: true,
        selectionLimit: 8,
        quality: 0.9
      });

      if (result.canceled || !result.assets || result.assets.length === 0) return;

      setIsUploading(true);
      setStatus(t("usedCars.uploadingPhotos", "Uploading used-car photos..."));
      for (const [index, asset] of result.assets.entries()) {
        const formData = new FormData();
        const name = asset.fileName || `used-car-${itemId(selectedCar)}-${Date.now()}-${index}.jpg`;
        const type = asset.mimeType || "image/jpeg";
        formData.append("image", { uri: asset.uri, name, type });
        await api.postForm(`/api/usedcars/${itemId(selectedCar)}/images`, formData);
      }

      const nextImages = toArray(await api.get(`/api/usedcars/${itemId(selectedCar)}/images`));
      setImages(nextImages);
      setSelectedImageId(String(itemId(nextImages[0]) || ""));
      setStatus(t("usedCars.imagesUploaded", "{count} image(s) uploaded.", { count: result.assets.length }));
    } catch (error) {
      setStatus(error.message || t("usedCars.uploadError", "Could not upload images."));
    } finally {
      setIsUploading(false);
    }
  }, [api, selectedCar, t]);

  const deleteImage = useCallback(async () => {
    if (!selectedImage) {
      setStatus(t("usedCars.selectImage", "Select an image to delete."));
      return;
    }

    try {
      setIsUploading(true);
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

  const confirmDeleteImage = useCallback(() => {
    if (!selectedImage) {
      setStatus(t("usedCars.selectImage", "Select an image to delete."));
      return;
    }

    Alert.alert(
      t("usedCars.deleteImageQuestion", "Delete image?"),
      t("usedCars.deleteImageMessage", "Remove this photo from the used-car gallery?"),
      [
        { text: t("common.cancel", "Cancel"), style: "cancel" },
        { text: t("common.delete", "Delete"), style: "destructive", onPress: () => { deleteImage(); } }
      ]
    );
  }, [deleteImage, selectedImage, t]);

  const setPartUsedCar = useCallback(async (partId, nextUsedCarId) => {
    setIsPartsLoading(true);
    try {
      await api.put(`/api/parts/${partId}/usedcar`, { usedCarId: nextUsedCarId });
      await loadParts();
      await loadCars(selectedId);
      setStatus(nextUsedCarId ? t("usedCars.partAssigned", "Part assigned to used car.") : t("usedCars.partRemoved", "Part removed from used car."));
    } catch (error) {
      setStatus(error.message || t("usedCars.partUpdateError", "Could not update part assignment."));
    } finally {
      setIsPartsLoading(false);
    }
  }, [api, loadCars, loadParts, selectedId, t]);

  useEffect(() => {
    const listenerId = zoomPan.addListener((value) => {
      zoomPanRef.current = value;
    });

    return () => {
      zoomPan.removeListener(listenerId);
    };
  }, [zoomPan]);

  const applyZoom = useCallback((nextZoom, animated) => {
    const nextLevel = clamp(nextZoom, 1, 4);
    const maxX = (width * (nextLevel - 1)) / 2;
    const maxY = (fullscreenSlideHeight * (nextLevel - 1)) / 2;
    const nextPan = nextLevel <= 1.01
      ? { x: 0, y: 0 }
      : {
        x: clamp(zoomPanRef.current.x, -maxX, maxX),
        y: clamp(zoomPanRef.current.y, -maxY, maxY)
      };

    zoomLevelRef.current = nextLevel;
    setZoomLevel(Number(nextLevel.toFixed(2)));

    if (animated) {
      Animated.parallel([
        Animated.spring(zoomScale, {
          toValue: nextLevel,
          friction: 8,
          tension: 80,
          useNativeDriver: true
        }),
        Animated.spring(zoomPan, {
          toValue: nextPan,
          friction: 8,
          tension: 80,
          useNativeDriver: true
        })
      ]).start();
      return;
    }

    zoomScale.setValue(nextLevel);
    zoomPan.setValue(nextPan);
  }, [fullscreenSlideHeight, width, zoomPan, zoomScale]);

  const resetZoom = useCallback(() => {
    applyZoom(1, true);
  }, [applyZoom]);

  const zoomIn = useCallback(() => {
    applyZoom(zoomLevelRef.current + 0.75, true);
  }, [applyZoom]);

  const zoomOut = useCallback(() => {
    applyZoom(zoomLevelRef.current - 0.75, true);
  }, [applyZoom]);

  const toggleZoom = useCallback(() => {
    if (zoomLevelRef.current > 1.05) {
      resetZoom();
      return;
    }

    applyZoom(2.25, true);
  }, [applyZoom, resetZoom]);

  const zoomResponder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: (event) =>
      event.nativeEvent.touches.length > 1 || zoomLevelRef.current > 1.05,
    onMoveShouldSetPanResponder: (event) =>
      event.nativeEvent.touches.length > 1 || zoomLevelRef.current > 1.05,
    onPanResponderGrant: (event) => {
      zoomPanStartRef.current = zoomPanRef.current;
      if (event.nativeEvent.touches.length > 1) {
        zoomStartDistanceRef.current = touchDistance(event.nativeEvent.touches);
        zoomStartScaleRef.current = zoomLevelRef.current;
      }
    },
    onPanResponderMove: (event, gestureState) => {
      const touches = event.nativeEvent.touches;

      if (touches.length > 1) {
        const distance = touchDistance(touches);
        const startDistance = zoomStartDistanceRef.current || distance || 1;
        const nextLevel = clamp((distance / startDistance) * zoomStartScaleRef.current, 1, 4);
        applyZoom(nextLevel, false);
        return;
      }

      if (zoomLevelRef.current <= 1.05) return;

      const maxX = (width * (zoomLevelRef.current - 1)) / 2;
      const maxY = (fullscreenSlideHeight * (zoomLevelRef.current - 1)) / 2;
      zoomPan.setValue({
        x: clamp(zoomPanStartRef.current.x + gestureState.dx, -maxX, maxX),
        y: clamp(zoomPanStartRef.current.y + gestureState.dy, -maxY, maxY)
      });
    },
    onPanResponderRelease: () => {
      if (zoomLevelRef.current <= 1.05) resetZoom();
    },
    onPanResponderTerminate: () => {
      if (zoomLevelRef.current <= 1.05) resetZoom();
    }
  }), [applyZoom, fullscreenSlideHeight, resetZoom, width, zoomPan]);

  const openGallery = useCallback((index) => {
    if (images.length === 0) return;
    const nextIndex = Math.min(Math.max(Number.isFinite(index) ? index : selectedImageIndex, 0), images.length - 1);
    const nextImage = images[nextIndex];
    if (nextImage) setSelectedImageId(String(itemId(nextImage)));
    setGalleryIndex(nextIndex);
    setIsGalleryOpen(true);
  }, [images, selectedImageIndex]);

  const closeGallery = useCallback(() => {
    resetZoom();
    setIsGalleryOpen(false);
  }, [resetZoom]);

  const generateListing = useCallback(async () => {
    if (!selectedCar) return;
    setIsGeneratingListing(true);
    setStatus("");
    try {
      const pkg = await api.get(`/api/usedcars/${itemId(selectedCar)}/listing-package`);
      setListingPackage(pkg);
      setIsListingOpen(true);
    } catch (error) {
      setStatus(error.message || t("usedCars.listingError", "Could not generate the listing package."));
    } finally {
      setIsGeneratingListing(false);
    }
  }, [api, selectedCar, t]);

  const closeListing = useCallback(() => {
    setIsListingOpen(false);
  }, []);

  const shareListing = useCallback(async () => {
    if (!listingPackage) return;
    try {
      await Share.share({ message: `${listingPackage.title}\n\n${listingPackage.description}` });
    } catch {
      setStatus(t("usedCars.listingShareError", "Could not open the share sheet."));
    }
  }, [listingPackage, t]);

  const openMarketplace = useCallback((url) => {
    Linking.openURL(url).catch(() => setStatus(t("usedCars.listingLinkError", "Could not open that marketplace.")));
  }, [t]);

  const syncGalleryIndex = useCallback((index) => {
    if (images.length === 0) return;
    const nextIndex = Math.min(Math.max(index, 0), images.length - 1);
    const nextImage = images[nextIndex];
    resetZoom();
    setGalleryIndex(nextIndex);
    if (nextImage) setSelectedImageId(String(itemId(nextImage)));
  }, [images, resetZoom]);

  useEffect(() => {
    if (!isGalleryOpen || images.length === 0) return undefined;
    const nextIndex = Math.min(galleryIndex, images.length - 1);
    const timer = setTimeout(() => {
      galleryListRef.current?.scrollToIndex({ index: nextIndex, animated: false });
    }, 40);

    return () => clearTimeout(timer);
  }, [galleryIndex, images.length, isGalleryOpen]);

  useEffect(() => {
    if (isGalleryOpen && images.length === 0) {
      setIsGalleryOpen(false);
    }
  }, [images.length, isGalleryOpen]);

  const donorFilters = [
    { key: "all", label: t("usedCars.filterAllDonors", "All") },
    { key: "action", label: t("usedCars.filterAction", "Action") },
    { key: "gap", label: t("usedCars.filterGapOpen", "Gap open") },
    { key: "needs-linking", label: t("usedCars.filterNeedsLinking", "Needs linking") },
    { key: "profit", label: t("usedCars.filterProfit", "Profit") },
    { key: "transit", label: t("usedCars.filterTransit", "Transit") }
  ];
  const focusProjects = filteredDonorProjects
    .filter((entry) => entry.action.key !== "sold")
    .slice(0, 4);

  const galleryModal = el(Modal, {
    animationType: "fade",
    visible: isGalleryOpen,
    onRequestClose: closeGallery,
    presentationStyle: "fullScreen",
    statusBarTranslucent: true
  },
    el(View, { style: [styles.usedCarFullscreenRoot, { paddingTop: insets.top + 12, paddingBottom: insets.bottom + 12 }] },
      el(View, { style: styles.usedCarFullscreenHeader },
        el(View, { style: styles.usedCarFullscreenHeaderCopy },
          el(Text, { style: styles.usedCarFullscreenTitle, numberOfLines: 1 }, selectedCar ? carTitle(selectedCar, t) : t("usedCars.gallery", "Gallery")),
          el(Text, { style: styles.usedCarFullscreenMeta },
            t("usedCars.galleryCounter", "{current} / {total}", { current: Math.min(galleryIndex + 1, images.length), total: images.length })
          )
        ),
        el(Pressable, { style: styles.usedCarFullscreenClose, onPress: closeGallery },
          el(Text, { style: styles.usedCarFullscreenCloseText }, t("common.close", "Close"))
        )
      ),
      images.length > 0
        ? el(FlatList, {
          ref: galleryListRef,
          data: images,
          horizontal: true,
          pagingEnabled: true,
          scrollEnabled: zoomLevel <= 1.05,
          keyExtractor: (item, index) => String(itemId(item) || `fullscreen-image-${index}`),
          showsHorizontalScrollIndicator: false,
          initialScrollIndex: Math.min(galleryIndex, images.length - 1),
          getItemLayout: (_, index) => ({ length: width, offset: width * index, index }),
          onScrollToIndexFailed: ({ index }) => {
            setTimeout(() => galleryListRef.current?.scrollToOffset({ offset: width * index, animated: false }), 60);
          },
          onMomentumScrollEnd: (event) => {
            const nextIndex = Math.round(event.nativeEvent.contentOffset.x / Math.max(width, 1));
            syncGalleryIndex(nextIndex);
          },
          renderItem: ({ item }) => {
            const uri = imageUri(item);
            return el(View, { style: [styles.usedCarFullscreenSlide, { width, minHeight: fullscreenSlideHeight }] },
              el(Pressable, {
                style: styles.usedCarFullscreenZoomSurface,
                onPress: toggleZoom,
                accessibilityRole: "imagebutton",
                ...zoomResponder.panHandlers
              },
                uri
                  ? el(Animated.Image, {
                    source: { uri },
                    style: [
                      styles.usedCarFullscreenImage,
                      {
                        transform: [
                          { translateX: zoomPan.x },
                          { translateY: zoomPan.y },
                          { scale: zoomScale }
                        ]
                      }
                    ],
                    resizeMode: "contain"
                  })
                  : el(Animated.View, {
                    style: [
                      styles.usedCarFullscreenPlaceholder,
                      {
                        transform: [
                          { translateX: zoomPan.x },
                          { translateY: zoomPan.y },
                          { scale: zoomScale }
                        ]
                      }
                    ]
                  },
                    el(Text, { style: styles.usedCarHeroInitials }, selectedCar ? initials(carTitle(selectedCar, t)) : "MAP")
                  )
              )
            );
          }
        })
        : el(EmptyState, { text: t("usedCars.noPhotos", "No photos uploaded for this vehicle yet.") }),
      el(View, { style: styles.usedCarZoomToolbar },
        el(Pressable, { style: styles.usedCarZoomButton, onPress: zoomOut, disabled: zoomLevel <= 1.05 },
          el(Text, { style: styles.usedCarZoomButtonText }, "-")
        ),
        el(Pressable, { style: styles.usedCarZoomReadout, onPress: resetZoom },
          el(Text, { style: styles.usedCarZoomReadoutText }, `${Math.round(zoomLevel * 100)}%`)
        ),
        el(Pressable, { style: styles.usedCarZoomButton, onPress: zoomIn, disabled: zoomLevel >= 3.95 },
          el(Text, { style: styles.usedCarZoomButtonText }, "+")
        ),
        el(Pressable, { style: styles.usedCarZoomReset, onPress: resetZoom, disabled: zoomLevel <= 1.05 },
          el(Text, { style: styles.usedCarZoomResetText }, t("usedCars.resetZoom", "Reset"))
        )
      ),
      el(Text, { style: styles.usedCarFullscreenHint },
        zoomLevel > 1.05
          ? t("usedCars.zoomedHint", "Drag to inspect. Reset zoom to swipe pictures.")
          : t("usedCars.zoomHint", "Pinch or tap to zoom. Swipe to change pictures.")
      ),
      images.length > 1 && el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.usedCarFullscreenThumbRail
      },
        images.map((image, index) => {
          const uri = imageUri(image);
          const active = index === galleryIndex;
          return el(Pressable, {
            key: itemId(image) || `fullscreen-thumb-${index}`,
            style: [styles.usedCarFullscreenThumb, active && styles.usedCarFullscreenThumbActive],
            onPress: () => {
              syncGalleryIndex(index);
              galleryListRef.current?.scrollToIndex({ index, animated: true });
            }
          },
            uri
              ? el(Image, { source: { uri }, style: styles.usedCarThumbImage, resizeMode: "cover" })
              : el(Text, { style: styles.usedCarCardMeta }, `${index + 1}`)
          );
        })
      )
    )
  );

  const listingModal = el(Modal, {
    animationType: "slide",
    transparent: true,
    visible: isListingOpen,
    onRequestClose: closeListing
  },
    el(View, { style: styles.usedCarListingOverlay },
      el(View, { style: [styles.usedCarListingCard, { paddingBottom: insets.bottom + 16 }] },
        el(View, { style: styles.usedCarListingHeader },
          el(Text, { style: styles.usedCarHeroTitle }, t("usedCars.listingPackage", "Listing Package")),
          el(Pressable, { onPress: closeListing }, el(Text, { style: styles.secondaryButtonText }, t("common.close", "Close")))
        ),
        listingPackage && el(ScrollView, { style: styles.usedCarListingScroll },
          el(Text, { style: styles.usedCarHeroSubtitle }, listingPackage.title),
          el(Text, { style: styles.usedCarCardMeta }, listingPackage.description),
          el(Text, { style: styles.usedCarCardMeta },
            t("usedCars.listingPhotoCount", "{count} photo(s) ready in the gallery to attach to the listing.", { count: listingPackage.photoCount })
          ),
          el(View, { style: styles.inlineButtons },
            el(Pressable, { style: styles.secondaryButton, onPress: shareListing },
              el(Text, { style: styles.secondaryButtonText }, t("usedCars.shareListing", "Share"))
            ),
            (listingPackage.marketplaceLinks || []).map((link) =>
              el(Pressable, {
                key: link.name,
                style: styles.secondaryButton,
                onPress: () => openMarketplace(link.url)
              },
                el(Text, { style: styles.secondaryButtonText }, link.name)
              )
            )
          )
        )
      )
    )
  );

  return el(ScreenScroll, null,
    galleryModal,
    listingModal,
    el(ScreenHeader, { title: t("usedCars.title", "Used Cars"), actionTitle: t("common.refresh", "Refresh"), onAction: () => { loadCars(selectedId); loadParts(); }, loading: isLoading || isPartsLoading }),
    el(StatusText, { value: status }),
    el(Panel, { title: t("usedCars.donorCockpit", "Donor Car Cockpit") },
      el(View, { style: styles.usedCarCockpit },
        el(View, { style: styles.usedCarMetricGrid },
          el(View, { style: styles.usedCarMetricTile },
            el(Text, { style: styles.usedCarMetricLabel }, t("usedCars.donorProjects", "Donor projects")),
            el(Text, { style: styles.usedCarMetricValue }, donorProjects.length.toLocaleString()),
            el(Text, { style: styles.usedCarMetricMeta }, t("usedCars.readyForTeardown", "{count} ready for teardown", { count: donorMetrics.readyForTeardown }))
          ),
          el(View, { style: [styles.usedCarMetricTile, styles.usedCarMetricPositive] },
            el(Text, { style: styles.usedCarMetricLabel }, t("usedCars.profitUnlocked", "Profit unlocked")),
            el(Text, { style: styles.usedCarMetricValue }, donorMetrics.profitUnlocked.toLocaleString()),
            el(Text, { style: styles.usedCarMetricMeta }, t("usedCars.keepSelling", "Keep selling"))
          ),
          el(View, { style: [styles.usedCarMetricTile, styles.usedCarMetricWarning] },
            el(Text, { style: styles.usedCarMetricLabel }, t("usedCars.breakEvenRisk", "Break-even risk")),
            el(Text, { style: styles.usedCarMetricValue }, donorMetrics.gapOpen.toLocaleString()),
            el(Text, { style: styles.usedCarMetricMeta }, t("usedCars.gapOpenDetail", "Need quoting"))
          ),
          el(View, { style: [styles.usedCarMetricTile, donorMetrics.needsLinking > 0 && styles.usedCarMetricDanger] },
            el(Text, { style: styles.usedCarMetricLabel }, t("usedCars.partsLinked", "Parts linked")),
            el(Text, { style: styles.usedCarMetricValue }, donorMetrics.linkedParts.toLocaleString()),
            el(Text, { style: styles.usedCarMetricMeta }, t("usedCars.needsLinkingCount", "{count} donors need linking", { count: donorMetrics.needsLinking }))
          )
        ),
        el(TextInput, {
          style: styles.input,
          value: carSearch,
          onChangeText: setCarSearch,
          placeholder: t("usedCars.searchDonors", "Search donor, barcode, supplier, buyer, action..."),
          placeholderTextColor: palette.soft
        }),
        el(ScrollView, {
          horizontal: true,
          showsHorizontalScrollIndicator: false,
          contentContainerStyle: styles.usedCarFilterRail
        },
          donorFilters.map((item) => {
            const active = item.key === carFilter;
            return el(Pressable, {
              key: item.key,
              style: [styles.usedCarSelectChip, active && styles.usedCarSelectChipActive],
              onPress: () => setCarFilter(item.key)
            },
              el(Text, { style: [styles.usedCarSelectChipText, active && styles.usedCarSelectChipTextActive] }, item.label)
            );
          })
        ),
        el(View, { style: styles.usedCarFocusList },
          el(Text, { style: styles.usedCarPartSectionTitle }, t("usedCars.todayFocus", "Today Focus")),
          focusProjects.map((entry) => {
            const displayMoney = (value) => displayMoneyFromBase(value, displayContextForCar(entry.car));
            return el(Pressable, {
              key: itemId(entry.car),
              style: styles.usedCarFocusCard,
              onPress: () => setSelectedId(String(itemId(entry.car)))
            },
              el(View, { style: styles.usedCarFocusTop },
                el(Text, { style: [styles.usedCarActionPill, styles[`usedCarActionPill_${entry.action.tone}`]] }, entry.action.label),
                el(Text, { style: styles.usedCarFocusGap }, entry.project.fullCost <= 0 ? t("usedCars.addCosts", "Add costs") : entry.project.breakEvenGap > 0 ? displayMoney(entry.project.breakEvenGap) : t("usedCars.done", "Done"))
              ),
              el(Text, { style: styles.usedCarFocusTitle, numberOfLines: 1 }, carTitle(entry.car, t)),
              el(Text, { style: styles.usedCarCardMeta, numberOfLines: 2 }, entry.action.detail),
              el(Text, { style: styles.usedCarCardMeta }, t("usedCars.focusRecoveryLine", "{percent}% recovered · {count} linked", { percent: entry.project.recoveredPercent, count: entry.linkedParts.length }))
            );
          }),
          focusProjects.length === 0 && el(EmptyState, { text: t("usedCars.noFocusDonors", "No donor cars match this focus.") })
        )
      )
    ),
    el(View, { style: [styles.usedCarWorkspaceGrid, isWide && styles.usedCarWorkspaceGridWide] },
      el(View, { style: [styles.usedCarWorkspacePrimary, isWide && styles.usedCarWorkspacePrimaryWide] },
        el(Panel, { title: t("usedCars.gallery", "Gallery") },
          selectedCar
            ? el(View, { style: styles.usedCarHero },
              el(Pressable, {
                style: styles.usedCarGalleryFrame,
                onPress: () => openGallery(selectedImageIndex),
                disabled: !selectedImageUri,
                accessibilityRole: "button"
              },
                selectedImageUri
                  ? el(Image, { source: { uri: selectedImageUri }, style: styles.usedCarHeroImage, resizeMode: "cover" })
                  : el(View, { style: styles.usedCarHeroPlaceholder },
                    isImagesLoading
                      ? el(ActivityIndicator, { color: palette.accent })
                      : el(Text, { style: styles.usedCarHeroInitials }, initials(carTitle(selectedCar, t)))
                  )
              ),
              el(View, { style: styles.usedCarHeroCopy },
                el(Text, { style: styles.usedCarHeroTitle }, carTitle(selectedCar, t)),
                el(Text, { style: styles.usedCarHeroSubtitle }, `${read(selectedCar, "location") || t("usedCars.noLocation", "No location")} - ${carPrice(selectedCar)}`),
                el(View, { style: styles.usedCarBadgeRow },
                  statusLabel(selectedCar, t).map((badge) =>
                    el(View, { key: badge, style: styles.usedCarBadge },
                      el(Text, { style: styles.usedCarBadgeText }, badge)
                    )
                  )
                ),
                el(View, { style: styles.inlineButtons },
                  el(Pressable, { style: styles.secondaryButton, onPress: uploadImages, disabled: isUploading },
                    isUploading ? el(ActivityIndicator, { color: palette.text, size: "small" }) : el(Text, { style: styles.secondaryButtonText }, t("usedCars.addPhotos", "Add Photos"))
                  ),
                  el(Pressable, { style: styles.secondaryButton, onPress: () => openGallery(selectedImageIndex), disabled: images.length === 0 },
                    el(Text, { style: styles.secondaryButtonText }, t("usedCars.fullscreen", "Fullscreen"))
                  ),
                  el(Pressable, { style: [styles.secondaryButton, styles.usedCarDangerSoft], onPress: confirmDeleteImage, disabled: isUploading || !selectedImage },
                    el(Text, { style: styles.secondaryButtonText }, t("usedCars.deletePhoto", "Delete Photo"))
                  ),
                  el(Pressable, { style: styles.secondaryButton, onPress: generateListing, disabled: isGeneratingListing },
                    isGeneratingListing
                      ? el(ActivityIndicator, { color: palette.text, size: "small" })
                      : el(Text, { style: styles.secondaryButtonText }, t("usedCars.generateListing", "Generate Listing"))
                  )
)
              ),
              images.length > 1 && el(ScrollView, {
                horizontal: true,
                showsHorizontalScrollIndicator: false,
                contentContainerStyle: styles.usedCarThumbRail
              },
                images.map((image, index) => {
                  const uri = imageUri(image);
                  const active = String(itemId(image)) === String(itemId(selectedImage));
                  return el(Pressable, {
                    key: itemId(image) || `image-${index}`,
                    style: [styles.usedCarThumb, active && styles.usedCarThumbActive],
                    onPress: () => {
                      setSelectedImageId(String(itemId(image)));
                      openGallery(index);
                    }
                  },
                    uri
                      ? el(Image, { source: { uri }, style: styles.usedCarThumbImage, resizeMode: "cover" })
                      : el(Text, { style: styles.usedCarCardMeta }, `${index + 1}`)
                  );
                })
              ),
              images.length === 0 && !isImagesLoading && el(EmptyState, { text: t("usedCars.noPhotos", "No photos uploaded for this vehicle yet.") })
            )
            : el(EmptyState, { text: t("usedCars.selectForGallery", "Select a used car or save a new one to manage its gallery.") })
        ),
        el(Panel, { title: t("usedCars.inventory", "Inventory") },
          filteredDonorProjects.length === 0 && !isLoading && el(EmptyState, { text: t("usedCars.noCars", "No used cars returned.") }),
          el(View, { style: styles.usedCarInventoryListFrame },
            el(ScrollView, {
              nestedScrollEnabled: true,
              showsVerticalScrollIndicator: true,
              contentContainerStyle: styles.usedCarInventoryList
            },
              filteredDonorProjects.map((entry, index) => {
                const car = entry.car;
                const active = String(itemId(car)) === String(selectedId);
                return el(Pressable, {
                  key: itemId(car) || `car-${index}`,
                  style: [styles.usedCarInventoryRow, active && styles.usedCarInventoryRowActive],
                  onPress: () => setSelectedId(String(itemId(car)))
                },
                  el(View, { style: styles.usedCarInventoryMark },
                    el(Text, { style: styles.usedCarInventoryMarkText }, initials(carTitle(car, t)))
                  ),
                  el(View, { style: styles.usedCarInventoryCopy },
                    el(Text, { style: [styles.usedCarCardTitle, active && styles.usedCarSelectChipTextActive], numberOfLines: 1 }, carTitle(car, t)),
                    el(Text, { style: styles.usedCarCardMeta, numberOfLines: 1 }, `${read(car, "supplierName") || t("usedCars.noSupplier", "No supplier")} - ${read(car, "barcode") || t("usedCars.noBarcode", "No barcode")}`),
                    el(Text, { style: [styles.usedCarInventoryAction, styles[`usedCarInventoryAction_${entry.action.tone}`]], numberOfLines: 1 },
                      `${entry.action.label} - ${entry.project.recoveredPercent}% - ${entry.linkedParts.length} linked`
                    )
                  ),
                  el(Text, { style: styles.usedCarInventoryValue }, carPrice(car))
                );
              })
            )
          )
        ),
        selectedCar && el(Panel, { title: t("usedCars.vehicleDetails", "Vehicle Details") },
          el(View, { style: styles.usedCarDetailGrid },
            el(DetailTile, { label: t("usedCars.supplier", "Supplier"), value: read(selectedCar, "supplierName") }),
            el(DetailTile, { label: t("fields.barcode", "Barcode"), value: read(selectedCar, "barcode") }),
            el(DetailTile, { label: t("usedCars.location", "Location"), value: read(selectedCar, "location") }),
            el(DetailTile, { label: t("usedCars.price", "Price"), value: carPrice(selectedCar) }),
            el(DetailTile, { label: t("usedCars.fullCost", "Full Cost"), value: displayMoneyFromBase(read(selectedCar, "fullCostBase"), selectedDisplayContext) }),
            el(DetailTile, { label: t("usedCars.sellThrough", "Sell-Through"), value: `${Math.round(toNumber(read(selectedCar, "expectedSellThroughRate")) * 100)}%` }),
            el(DetailTile, { label: t("usedCars.netPl", "Net P/L"), value: displayMoneyFromBase(read(selectedCar, "netProfitLossBase"), selectedDisplayContext) }),
            selectedDonorProject && el(DetailTile, { label: t("usedCars.nextAction", "Next Action"), value: selectedDonorProject.action.label }),
            selectedDonorProject && el(DetailTile, { label: t("usedCars.breakEven", "Break Even"), value: selectedDonorProject.project.fullCost <= 0 ? t("usedCars.addCosts", "Add costs") : selectedDonorProject.project.breakEvenGap > 0 ? displayMoneyFromBase(selectedDonorProject.project.breakEvenGap, selectedDisplayContext) : t("usedCars.done", "Done") })
          )
        ),
        selectedDonorProject && el(Panel, { title: t("usedCars.profitRecovery", "Profit Recovery") },
          el(View, { style: styles.usedCarRecoveryPanel },
            el(View, { style: styles.usedCarFocusTop },
              el(Text, { style: [styles.usedCarActionPill, styles[`usedCarActionPill_${selectedDonorProject.action.tone}`]] }, selectedDonorProject.action.label),
              el(Text, { style: styles.usedCarFocusGap }, `${selectedDonorProject.project.recoveredPercent}%`)
            ),
            el(View, { style: styles.usedCarProgressTrack },
              el(View, { style: [styles.usedCarProgressFill, { width: `${Math.min(selectedDonorProject.project.recoveredPercent, 100)}%` }] })
            ),
            el(Text, { style: styles.usedCarHelpText }, selectedDonorProject.action.detail),
            selectedDonorProject.recommendations.length > 0
              ? el(View, { style: styles.usedCarPushParts },
                selectedDonorProject.recommendations.map((item) =>
                  el(View, { key: itemId(item.part), style: styles.usedCarPushPart },
                    el(Text, { style: styles.usedCarPartTitle, numberOfLines: 1 }, read(item.part, "internalCode") || read(item.part, "name") || `#${itemId(item.part)}`),
                    el(Text, { style: styles.usedCarPartMeta }, money(item.expectedSale || item.salePrice, read(item.part, "currency") || baseCurrency(selectedCar)))
                  )
                )
              )
              : el(EmptyState, { text: t("usedCars.linkPartsToForecast", "Link parts to forecast profit.") })
          )
        )
      ),
      el(View, { style: [styles.usedCarWorkspaceSide, isWide && styles.usedCarWorkspaceSideWide] },
        el(Panel, { title: selectedCar ? t("usedCars.editUsedCar", "Edit Used Car") : t("usedCars.newUsedCar", "New Used Car") },
          el(View, { style: styles.inlineButtons },
            el(Pressable, { style: styles.secondaryButton, onPress: startNew },
              el(Text, { style: styles.secondaryButtonText }, t("common.new", "New"))
            ),
            el(Pressable, { style: styles.primaryButton, onPress: saveCar, disabled: isSaving },
              isSaving ? el(ActivityIndicator, { color: palette.text, size: "small" }) : el(Text, { style: styles.primaryButtonText }, selectedCar ? t("common.save", "Save") : t("common.create", "Create"))
            ),
            el(Pressable, { style: [styles.secondaryButton, styles.usedCarDangerSoft], onPress: confirmDeleteCar, disabled: isSaving || !selectedCar },
              el(Text, { style: styles.secondaryButtonText }, t("common.delete", "Delete"))
            )
          ),
          el(InputField, { label: t("fields.barcode", "Barcode"), value: form.barcode, onChangeText: (value) => setFormValue("barcode", value), placeholder: t("usedCars.autoIfEmpty", "Auto if empty") }),
          el(Selector, {
            label: t("usedCars.supplier", "Supplier"),
            value: form.supplierId,
            search: supplierSearch,
            onSearch: setSupplierSearch,
            options: suppliers,
            onSelect: (value) => setFormValue("supplierId", value),
            getTitle: (item) => read(item, "name") || t("usedCars.supplierNumber", "Supplier #{id}", { id: itemId(item) }),
            getMeta: (item) => read(item, "phone") || read(item, "email")
          }),
          el(Selector, {
            label: t("usedCars.carModel", "Car Model"),
            value: form.carModelId,
            search: modelSearch,
            onSearch: setModelSearch,
            options: carModels,
            onSelect: (value) => {
              setFormValue("carModelId", value);
              const model = carModels.find((item) => optionKey(item) === String(value));
              if (model) setModelSearch(modelTitle(model));
            },
            getTitle: modelTitle,
            getMeta: (item) => read(item, "bodyType") || read(item, "carBrandName")
          }),
          el(View, { style: styles.usedCarFormGrid },
            el(InputField, { label: t("usedCars.modelYear", "Model Year"), value: form.modelYear, keyboardType: "number-pad", onChangeText: (value) => setFormValue("modelYear", value) }),
            el(InputField, { label: t("usedCars.price", "Price"), value: form.price, keyboardType: "decimal-pad", onChangeText: (value) => setFormValue("price", value) })
          ),
          el(View, { style: styles.usedCarCurrencyRow },
            currencyCodes.slice(0, 6).map((code) => {
              const active = String(form.priceCurrency).toUpperCase() === String(code).toUpperCase();
              return el(Pressable, {
                key: code,
                style: [styles.usedCarCurrencyChip, active && styles.usedCarSelectChipActive],
                onPress: () => setFormValue("priceCurrency", code)
              },
                el(Text, { style: [styles.usedCarSelectChipText, active && styles.usedCarSelectChipTextActive] }, code)
              );
            })
          ),
          el(Selector, {
            label: t("usedCars.location", "Location"),
            value: form.locationId,
            search: locationSearch,
            onSearch: setLocationSearch,
            options: locations,
            onSelect: (value) => {
              setFormValue("locationId", value);
              const location = locations.find((item) => optionKey(item) === String(value));
              if (location) setLocationSearch(read(location, "name"));
            },
            getTitle: (item) => read(item, "name") || t("usedCars.locationNumber", "Location #{id}", { id: read(item, "locationId") }),
            getMeta: (item) => t("usedCars.shippingMeta", "{amount} shipping", { amount: money(read(item, "shippingFees"), read(item, "shippingFeesCurrencyCode") || "USD") })
          }),
          el(View, { style: styles.usedCarFormGrid },
            el(ToggleField, { label: t("usedCars.received", "Received"), value: form.isReceived, onValueChange: (value) => setFormValue("isReceived", value) }),
            el(ToggleField, { label: t("usedCars.shipped", "Shipped"), value: form.isShipped, onValueChange: (value) => setFormValue("isShipped", value) })
          ),
          el(View, { style: styles.usedCarFormGrid },
            el(InputField, { label: t("usedCars.partOut", "Part-Out"), value: form.partOut, keyboardType: "decimal-pad", onChangeText: (value) => setFormValue("partOut", value) }),
            el(InputField, { label: t("usedCars.shipping", "Shipping"), value: form.shipping, keyboardType: "decimal-pad", onChangeText: (value) => setFormValue("shipping", value) }),
            el(InputField, { label: t("usedCars.customs", "Customs"), value: form.customs, keyboardType: "decimal-pad", onChangeText: (value) => setFormValue("customs", value) }),
            el(InputField, { label: t("usedCars.repairs", "Repairs"), value: form.repairs, keyboardType: "decimal-pad", onChangeText: (value) => setFormValue("repairs", value) }),
            el(InputField, { label: t("usedCars.expectedSellThroughRate", "Expected Sell-Through Rate"), value: form.expectedSellThroughRate, keyboardType: "decimal-pad", onChangeText: (value) => setFormValue("expectedSellThroughRate", value) })
          ),
          el(Text, { style: styles.usedCarHelpText },
            selectedModel ? t("usedCars.selectedModel", "Selected: {model}", { model: modelTitle(selectedModel) }) : t("usedCars.chooseModel", "Choose a model from the matching chips.")
          )
        ),
        el(Panel, { title: t("usedCars.linkedParts", "Linked Parts") },
          selectedCar
            ? el(View, { style: styles.usedCarPartsPanel },
              el(TextInput, {
                style: styles.input,
                value: partSearch,
                onChangeText: setPartSearch,
                placeholder: t("usedCars.partSearchPlaceholder", "Search by code, name, or OEM"),
                placeholderTextColor: palette.soft
              }),
              el(Text, { style: styles.usedCarHelpText }, t("usedCars.partsSummary", "{assigned} assigned · {available} available", { assigned: assignedParts.length, available: availableParts.length })),
              el(Text, { style: styles.usedCarPartSectionTitle }, t("usedCars.assigned", "Assigned")),
              el(View, { style: styles.usedCarPartsListFrame },
                el(ScrollView, {
                  nestedScrollEnabled: true,
                  showsVerticalScrollIndicator: true,
                  contentContainerStyle: styles.usedCarPartsListContent
                },
                  assignedParts.map((part) =>
                    el(PartRow, {
                      key: itemId(part),
                      part,
                      actionTitle: t("usedCars.remove", "Remove"),
                      onPress: () => setPartUsedCar(itemId(part), null)
                    })
                  ),
                  assignedParts.length === 0 && el(EmptyState, { text: t("usedCars.noLinkedParts", "No parts linked to this car.") })
                )
              ),
              el(Text, { style: styles.usedCarPartSectionTitle }, t("usedCars.available", "Available")),
              el(View, { style: styles.usedCarPartsListFrame },
                el(ScrollView, {
                  nestedScrollEnabled: true,
                  showsVerticalScrollIndicator: true,
                  contentContainerStyle: styles.usedCarPartsListContent
                },
                  availableParts.map((part) =>
                    el(PartRow, {
                      key: itemId(part),
                      part,
                      actionTitle: t("usedCars.assign", "Assign"),
                      onPress: () => setPartUsedCar(itemId(part), itemId(selectedCar))
                    })
                  ),
                  availableParts.length === 0 && el(EmptyState, { text: t("usedCars.noAvailableParts", "No available parts match this search.") })
                )
              )
            )
            : el(EmptyState, { text: t("usedCars.selectForParts", "Select a used car to assign or remove parts.") })
        )
      )
    )
  );
}

module.exports = { UsedCarsScreen };
