const React = require("react");
const { Linking, Modal, Pressable, ScrollView, Share, Text, View } = require("react-native");
const { asRows, money } = require("../core/formatters");
const { CommunicationPayloadFactory } = require("../core/communication-payload-factory");
const { Field, ListRow, Panel, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

const conditionLabels = new Map([
  [1, "New"],
  [2, "Used"],
  [3, "Rebuilt"]
]);
const statusFilters = ["All", "Available", "Reserved", "Sold", "Low stock", "Demand", "Action"];
const conditionFilters = ["All", "New", "Used", "Rebuilt"];

function conditionLabel(part) {
  return conditionLabels.get(Number(part && part.condition)) || String(part && part.condition || "Unknown");
}

function stockStatus(part) {
  if (!part || part.isActive === false) return "Sold";
  if (Number(part.availableQuantity || 0) > 0) return "Available";
  if (Number(part.reservedQuantity || 0) > 0) return "Reserved";
  return "Sold";
}

function stockSubtitle(part) {
  const available = Number(part.availableQuantity || 0);
  const reserved = Number(part.reservedQuantity || 0);
  if (available > 0 && reserved > 0) return `${available} ready, ${reserved} reserved`;
  if (available > 0) return `${available} ready to sell`;
  if (reserved > 0) return `${reserved} reserved`;
  return "No available stock";
}

function normalizePhone(value) {
  return String(value || "").replace(/[^\d]/g, "");
}

function normalizeText(value) {
  return String(value || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .replace(/\s+/g, " ");
}

function activeRequest(request) {
  const status = normalizeText(request && request.status);
  return status === "open" || status === "contacted" || status === "reserved";
}

function requestMatchesPart(request, part) {
  if (request && request.partId && String(request.partId) === String(part.id)) return true;

  const requestedOem = normalizeText(request && request.requestedOemNumber);
  const partOem = normalizeText(part && part.oemNumber);
  if (requestedOem && partOem && (requestedOem.includes(partOem) || partOem.includes(requestedOem))) return true;

  const requestedTokens = normalizeText(request && request.requestedPartName)
    .split(" ")
    .filter((token) => token.length >= 3);
  if (requestedTokens.length === 0) return false;

  const partText = normalizeText([part.name, part.internalCode, part.oemNumber, part.notes].filter(Boolean).join(" "));
  const matches = requestedTokens.filter((token) => partText.includes(token)).length;
  return matches >= Math.min(2, requestedTokens.length);
}

function marginWatch(part) {
  const salePrice = Number(part && part.salePrice || 0);
  const costPrice = Number(part && part.costPrice || 0);
  const marketPrice = Number(part && (part.estimatedMarketPrice || part.averagePrice) || 0);
  if (salePrice <= 0) return true;
  if (costPrice > 0 && (salePrice - costPrice) / salePrice < 0.22) return true;
  return marketPrice > 0 && salePrice < marketPrice * 0.9;
}

function priorityProfile(part, matchCount) {
  const available = Number(part.availableQuantity || 0);
  const reserved = Number(part.reservedQuantity || 0);
  const lowStock = available <= Number(part.minStock || 0);
  const margin = marginWatch(part);
  if (matchCount > 0 && available <= 0 && reserved <= 0) return { label: "Source now", tone: "danger", score: 30 + matchCount * 5 };
  if (matchCount > 0 && available > 0) return { label: "Quote now", tone: "success", score: 24 + matchCount * 5 };
  if (lowStock) return { label: "Reorder", tone: "warning", score: 14 };
  if (margin) return { label: "Margin", tone: "warning", score: 10 };
  return { label: "Ready", tone: "neutral", score: 0 };
}

function partSearchText(part) {
  return [
    part.internalCode,
    part.barcode,
    part.name,
    part.oemNumber,
    conditionLabel(part),
    part.currency,
    part.notes,
    part.usedCarId ? `donor ${part.usedCarId}` : ""
  ].filter(Boolean).join(" ").toLowerCase();
}

function buildPartMessage(part, quantity) {
  return [
    `Part: ${part.name}`,
    part.internalCode ? `Code: ${part.internalCode}` : "",
    part.oemNumber ? `OEM: ${part.oemNumber}` : "",
    `Condition: ${conditionLabel(part)}`,
    `Status: ${stockStatus(part)} (${stockSubtitle(part)})`,
    part.usedCarId ? `Donor car: #${part.usedCarId}` : "",
    `Quantity: ${Math.max(1, Number(quantity || 1))}`,
    `Price: ${money(part.salePrice, part.currency)}`,
    "Maalouf German Parts"
  ].filter(Boolean).join("\n");
}

function Chip({ title, active, onPress }) {
  const { styles } = useTheme();
  return el(Pressable, {
    accessibilityRole: "button",
    style: [styles.partsChip, active && styles.partsChipActive],
    onPress
  }, el(Text, { style: [styles.partsChipText, active && styles.partsChipTextActive] }, title));
}

function Metric({ label, value }) {
  const { styles } = useTheme();
  return el(View, { style: styles.partsMetric },
    el(Text, { style: styles.partsMetricLabel }, label),
    el(Text, { style: styles.partsMetricValue, numberOfLines: 1, adjustsFontSizeToFit: true }, String(value))
  );
}

function PartCard({ part, matchCount, priority, onSend, onShare, onListing, isGeneratingListing }) {
  const { styles } = useTheme();
  const status = stockStatus(part);
  const statusStyle = status === "Available"
    ? styles.partsStatusAvailable
    : status === "Reserved"
      ? styles.partsStatusReserved
      : styles.partsStatusSold;
  const priorityToneStyle = priority.tone === "success"
    ? styles.partsPrioritySuccess
    : priority.tone === "danger"
      ? styles.partsPriorityDanger
      : priority.tone === "warning"
        ? styles.partsPriorityWarning
        : styles.partsPriorityNeutral;

  return el(View, { style: styles.partCard },
    el(View, { style: styles.partPhoto },
      el(Text, { style: styles.partPhotoText }, String(part.name || "P").slice(0, 2).toUpperCase()),
      Boolean(part.usedCarId) && el(Text, { style: styles.partPhotoBadge }, "DONOR")
    ),
    el(View, { style: styles.partCardBody },
      el(View, { style: styles.partCardTopline },
        el(View, { style: styles.partCardCopy },
          el(Text, { style: styles.partCode, numberOfLines: 1 }, part.internalCode || "No code"),
          el(Text, { style: styles.partTitle, numberOfLines: 2 }, part.name || "Part")
        ),
        el(Text, { style: styles.partPrice, numberOfLines: 1, adjustsFontSizeToFit: true }, money(part.salePrice, part.currency))
      ),
      el(View, { style: styles.partBadgeRow },
        el(Text, { style: [styles.partsStatus, statusStyle] }, status),
        priority.score > 0 && el(Text, { style: [styles.partsPriority, priorityToneStyle] }, priority.label),
        matchCount > 0 && el(Text, { style: styles.partsDemandBadge }, `${matchCount} match${matchCount === 1 ? "" : "es"}`),
        el(Text, { style: styles.partsCondition }, conditionLabel(part)),
        Boolean(part.usedCarId) && el(Text, { style: styles.partsDonorBadge }, `#${part.usedCarId}`)
      ),
      el(Text, { style: styles.partMeta, numberOfLines: 1 }, `${part.oemNumber || "No OEM"} | ${stockSubtitle(part)}`),
      el(View, { style: styles.partActionRow },
        el(SecondaryButton, { title: "Send", onPress: () => onSend(part) }),
        el(SecondaryButton, { title: "WhatsApp", onPress: () => onShare(part) }),
        el(SecondaryButton, { title: isGeneratingListing ? "Generating..." : "Listing", disabled: isGeneratingListing, onPress: () => onListing(part) })
      )
    )
  );
}

function PartsScreen({ api }) {
  const { styles, t } = useTheme();
  const [parts, setParts] = useState([]);
  const [partRequests, setPartRequests] = useState([]);
  const [filter, setFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("All");
  const [conditionFilter, setConditionFilter] = useState("All");
  const [minPrice, setMinPrice] = useState("");
  const [maxPrice, setMaxPrice] = useState("");
  const [recipientName, setRecipientName] = useState("");
  const [recipientPhone, setRecipientPhone] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [listingPackage, setListingPackage] = useState(null);
  const [isListingOpen, setIsListingOpen] = useState(false);
  const [generatingListingId, setGeneratingListingId] = useState(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("parts.loading", "Loading parts..."));
    try {
      const [nextParts, nextRequests] = await Promise.all([
        api.list("/api/parts"),
        api.get("/api/partrequests?status=Active").catch(() => [])
      ]);
      setParts(asRows(nextParts));
      setPartRequests(asRows(nextRequests));
      setStatus(t("parts.loaded", "Parts loaded."));
    } catch (error) {
      setStatus(error.message || t("parts.loadError", "Could not load parts."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => { load(); }, [load]);

  const activeRequests = useMemo(
    () => partRequests.filter(activeRequest),
    [partRequests]
  );

  const demandByPart = useMemo(() => {
    const groups = new Map();
    parts.forEach((part) => {
      const matches = activeRequests.filter((request) => requestMatchesPart(request, part));
      if (matches.length > 0) groups.set(String(part.id), matches);
    });
    return groups;
  }, [activeRequests, parts]);

  const priorityByPart = useMemo(() => {
    const groups = new Map();
    parts.forEach((part) => {
      const matchCount = (demandByPart.get(String(part.id)) || []).length;
      groups.set(String(part.id), priorityProfile(part, matchCount));
    });
    return groups;
  }, [demandByPart, parts]);

  const visibleParts = useMemo(() => {
    const query = filter.trim().toLowerCase();
    const min = minPrice === "" ? null : Number(minPrice);
    const max = maxPrice === "" ? null : Number(maxPrice);

    return parts.filter((part) => {
      const available = Number(part.availableQuantity || 0);
      const reserved = Number(part.reservedQuantity || 0);
      const salePrice = Number(part.salePrice || 0);
      const matchCount = (demandByPart.get(String(part.id)) || []).length;
      const priority = priorityByPart.get(String(part.id)) || priorityProfile(part, matchCount);
      if (query && !partSearchText(part).includes(query)) return false;
      if (conditionFilter !== "All" && conditionLabel(part) !== conditionFilter) return false;
      if (min !== null && Number.isFinite(min) && salePrice < min) return false;
      if (max !== null && Number.isFinite(max) && salePrice > max) return false;
      if (statusFilter === "Available") return available > 0;
      if (statusFilter === "Reserved") return reserved > 0;
      if (statusFilter === "Sold") return stockStatus(part) === "Sold";
      if (statusFilter === "Low stock") return available <= Number(part.minStock || 0);
      if (statusFilter === "Demand") return matchCount > 0;
      if (statusFilter === "Action") return priority.score > 0;
      return true;
    });
  }, [conditionFilter, demandByPart, filter, maxPrice, minPrice, parts, priorityByPart, statusFilter]);
  const displayedParts = useMemo(() => visibleParts.slice(0, 120), [visibleParts]);
  const focusParts = useMemo(() => parts
    .map((part) => ({
      part,
      matchCount: (demandByPart.get(String(part.id)) || []).length,
      priority: priorityByPart.get(String(part.id)) || priorityProfile(part, 0)
    }))
    .filter((item) => item.priority.score > 0)
    .sort((left, right) => right.priority.score - left.priority.score || String(left.part.name).localeCompare(String(right.part.name)))
    .slice(0, 4), [demandByPart, parts, priorityByPart]);
  const metrics = useMemo(() => ({
    total: parts.length,
    available: parts.filter((part) => Number(part.availableQuantity || 0) > 0).length,
    reserved: parts.filter((part) => Number(part.reservedQuantity || 0) > 0).length,
    lowStock: parts.filter((part) => Number(part.availableQuantity || 0) <= Number(part.minStock || 0)).length,
    demand: parts.filter((part) => (demandByPart.get(String(part.id)) || []).length > 0).length,
    action: parts.filter((part) => (priorityByPart.get(String(part.id)) || priorityProfile(part, 0)).score > 0).length
  }), [demandByPart, parts, priorityByPart]);

  const sendAvailability = useCallback(async (part) => {
    if (!recipientPhone.trim()) {
      setStatus(t("parts.enterPhone", "Enter a WhatsApp phone number first."));
      return;
    }

    setStatus(t("parts.sending", "Sending availability..."));
    try {
      await api.post("/api/communications/send", CommunicationPayloadFactory.partAvailability({
        recipientName: recipientName || t("contacts.customers", "Customer"),
        recipientPhone,
        part
      }));
      setStatus(t("parts.sent", "Availability sent."));
    } catch (error) {
      setStatus(error.message || t("parts.sendError", "Could not send availability."));
    }
  }, [api, recipientName, recipientPhone, t]);

  const generateListing = useCallback(async (part) => {
    setGeneratingListingId(part.id);
    setStatus("");
    try {
      const pkg = await api.get(`/api/parts/${part.id}/listing-package`);
      setListingPackage(pkg);
      setIsListingOpen(true);
    } catch (error) {
      setStatus(error.message || t("parts.listingError", "Could not generate the listing package."));
    } finally {
      setGeneratingListingId(null);
    }
  }, [api, t]);

  const closeListing = useCallback(() => {
    setIsListingOpen(false);
  }, []);

  const shareListing = useCallback(async () => {
    if (!listingPackage) return;
    try {
      await Share.share({ message: `${listingPackage.title}\n\n${listingPackage.description}` });
    } catch {
      setStatus(t("parts.listingShareError", "Could not open the share sheet."));
    }
  }, [listingPackage, t]);

  const openMarketplace = useCallback((url) => {
    Linking.openURL(url).catch(() => setStatus(t("parts.listingLinkError", "Could not open that marketplace.")));
  }, [t]);

  const shareAvailability = useCallback((part) => {
    const phone = normalizePhone(recipientPhone);
    const href = `https://wa.me/${phone}?text=${encodeURIComponent(buildPartMessage(part, 1))}`;
    Linking.openURL(href);
    setStatus(phone ? t("parts.whatsappOpened", "WhatsApp share opened.") : t("parts.whatsappNoPhone", "WhatsApp share opened without a recipient number."));
  }, [recipientPhone, t]);

  const listingModal = el(Modal, {
    animationType: "slide",
    transparent: true,
    visible: isListingOpen,
    onRequestClose: closeListing
  },
    el(View, { style: styles.usedCarListingOverlay },
      el(View, { style: styles.usedCarListingCard },
        el(View, { style: styles.usedCarListingHeader },
          el(Text, { style: styles.usedCarHeroTitle }, t("parts.listingPackage", "Listing Package")),
          el(Pressable, { onPress: closeListing }, el(Text, { style: styles.secondaryButtonText }, t("common.close", "Close")))
        ),
        listingPackage && el(ScrollView, { style: styles.usedCarListingScroll },
          el(Text, { style: styles.usedCarHeroSubtitle }, listingPackage.title),
          el(Text, { style: styles.usedCarCardMeta }, listingPackage.description),
          el(Text, { style: styles.usedCarCardMeta },
            t("parts.listingPhotoCount", "{count} photo(s) ready in the donor car's gallery to attach to the listing.", { count: listingPackage.photoCount })
          ),
          el(View, { style: styles.inlineButtons },
            el(Pressable, { style: styles.secondaryButton, onPress: shareListing },
              el(Text, { style: styles.secondaryButtonText }, t("parts.shareListing", "Share"))
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
    listingModal,
    el(ScreenHeader, { title: t("parts.title", "Parts"), actionTitle: t("common.refresh", "Refresh"), onAction: load, loading: isLoading }),
    el(View, { style: styles.partsMetricGrid },
      el(Metric, { label: "Total", value: metrics.total }),
      el(Metric, { label: "Available", value: metrics.available }),
      el(Metric, { label: "Reserved", value: metrics.reserved }),
      el(Metric, { label: "Low stock", value: metrics.lowStock }),
      el(Metric, { label: "Demand", value: metrics.demand }),
      el(Metric, { label: "Action", value: metrics.action })
    ),
    el(Panel, { title: "Today Focus" },
      el(View, { style: styles.partsFocusList },
        focusParts.map(({ part, matchCount, priority }, index) =>
          el(Pressable, {
            key: String(part.id),
            accessibilityRole: "button",
            style: styles.partsFocusRow,
            onPress: () => {
              setFilter(part.internalCode || part.name || "");
              setStatusFilter(priority.label === "Quote now" ? "Demand" : "Action");
            }
          },
            el(Text, { style: styles.partsFocusRank }, String(index + 1).padStart(2, "0")),
            el(View, { style: styles.partsFocusCopy },
              el(Text, { style: styles.partsFocusTitle, numberOfLines: 1 }, part.name || "Part"),
              el(Text, { style: styles.partsFocusSubtitle, numberOfLines: 1 }, `${part.internalCode || "No code"} | ${matchCount} demand match${matchCount === 1 ? "" : "es"}`)
            ),
            el(Text, { style: styles.partsFocusAction, numberOfLines: 1 }, priority.label)
          )
        ),
        focusParts.length === 0 && el(Text, { style: styles.partMeta }, "No demand, reorder, or margin actions right now.")
      )
    ),
    el(Field, { label: t("common.filter", "Filter"), value: filter, onChangeText: setFilter, placeholder: "Name, code, OEM, donor car" }),
    el(ScrollView, { horizontal: true, showsHorizontalScrollIndicator: false, contentContainerStyle: styles.partsChipRail },
      statusFilters.map((option) => el(Chip, { key: option, title: option, active: statusFilter === option, onPress: () => setStatusFilter(option) }))
    ),
    el(ScrollView, { horizontal: true, showsHorizontalScrollIndicator: false, contentContainerStyle: styles.partsChipRail },
      conditionFilters.map((option) => el(Chip, { key: option, title: option, active: conditionFilter === option, onPress: () => setConditionFilter(option) }))
    ),
    el(View, { style: styles.partsPriceRow },
      el(View, { style: styles.partsPriceField }, el(Field, { label: "Min price", value: minPrice, onChangeText: setMinPrice, keyboardType: "numeric" })),
      el(View, { style: styles.partsPriceField }, el(Field, { label: "Max price", value: maxPrice, onChangeText: setMaxPrice, keyboardType: "numeric" }))
    ),
    el(Field, { label: t("parts.recipientName", "Recipient name"), value: recipientName, onChangeText: setRecipientName }),
    el(Field, { label: t("parts.whatsappPhone", "WhatsApp phone"), value: recipientPhone, onChangeText: setRecipientPhone, keyboardType: "phone-pad" }),
    el(StatusText, { value: status }),
    el(Panel, { title: `${t("parts.inventory", "Inventory")} (${visibleParts.length})` },
      el(View, { style: styles.screenListFrameLarge },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          displayedParts.map((part) => el(PartCard, {
            key: String(part.id),
            part,
            matchCount: (demandByPart.get(String(part.id)) || []).length,
            priority: priorityByPart.get(String(part.id)) || priorityProfile(part, 0),
            onSend: sendAvailability,
            onShare: shareAvailability,
            onListing: generateListing,
            isGeneratingListing: generatingListingId === part.id
          })),
          visibleParts.length > displayedParts.length && el(ListRow, {
            title: `Showing ${displayedParts.length} of ${visibleParts.length}`,
            subtitle: "Refine the search to find a specific part."
          }),
          visibleParts.length === 0 && el(ListRow, { title: t("parts.noParts", "No parts found"), subtitle: t("parts.tryFilter", "Try a different filter.") })
        )
      )
    )
  );
}

module.exports = { PartsScreen };
