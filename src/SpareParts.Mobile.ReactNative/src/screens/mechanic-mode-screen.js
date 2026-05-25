const React = require("react");
const { Image, Pressable, ScrollView, Text, View } = require("react-native");
const ImagePicker = require("expo-image-picker");
const { CommunicationPayloadFactory } = require("../core/communication-payload-factory");
const { asRows, money } = require("../core/formatters");
const { EmptyState, Field, ListRow, Panel, PrimaryButton, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

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

function toNumber(value, fallback = 0) {
  const parsed = Number(String(value || "").replace(/,/g, ".").trim());
  return Number.isFinite(parsed) ? parsed : fallback;
}

function toPositiveInteger(value, fallback = 1) {
  const parsed = Math.floor(toNumber(value, fallback));
  return parsed > 0 ? parsed : fallback;
}

function partId(part) {
  return read(part, "id", "targetId", "partId");
}

function partCode(part) {
  return read(part, "internalCode", "code", "partInternalCode") || `PART-${partId(part) || ""}`.trim();
}

function partTitle(part) {
  return read(part, "name", "displayText", "matchedPartName", "requestedPartName") || "Selected part";
}

function partOem(part) {
  return read(part, "oemNumber", "OEMNumber", "secondaryText", "requestedOemNumber");
}

function partCurrency(part) {
  return read(part, "currency") || "USD";
}

function totalStock(rows, key) {
  return rows.reduce((total, row) => total + toNumber(read(row, key)), 0);
}

function buildAttachment(asset) {
  if (!asset || !asset.base64) return null;
  const mimeType = asset.mimeType || "image/jpeg";
  const extension = mimeType.includes("png") ? "png" : mimeType.includes("webp") ? "webp" : "jpg";
  return {
    fileName: asset.fileName || `mechanic-part-${Date.now()}.${extension}`,
    mimeType,
    base64Content: asset.base64
  };
}

function defaultReservationExpiresAt() {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  date.setHours(18, 0, 0, 0);
  return date;
}

function MechanicModeScreen({ api }) {
  const { styles, t } = useTheme();
  const [parts, setParts] = useState([]);
  const [partSearch, setPartSearch] = useState("");
  const [scanCode, setScanCode] = useState("");
  const [scanRows, setScanRows] = useState([]);
  const [selectedPart, setSelectedPart] = useState(null);
  const [stockRows, setStockRows] = useState([]);
  const [selectedWarehouseId, setSelectedWarehouseId] = useState("");
  const [reservationQuantity, setReservationQuantity] = useState("1");
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [vehicleDetails, setVehicleDetails] = useState("");
  const [requestNotes, setRequestNotes] = useState("");
  const [activeRequestId, setActiveRequestId] = useState("");
  const [photoAsset, setPhotoAsset] = useState(null);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isWorking, setIsWorking] = useState(false);

  const selectedPartId = partId(selectedPart);
  const selectedStock = useMemo(() => ({
    quantity: totalStock(stockRows, "quantity"),
    reserved: totalStock(stockRows, "reservedQuantity"),
    available: totalStock(stockRows, "availableQuantity")
  }), [stockRows]);

  const filteredParts = useMemo(() => {
    const query = partSearch.trim().toLowerCase();
    const visible = query
      ? parts.filter((part) => [
        read(part, "internalCode"),
        read(part, "barcode"),
        read(part, "name"),
        read(part, "oemNumber")
      ].join(" ").toLowerCase().includes(query))
      : parts.slice(0, 16);
    return visible.slice(0, 24);
  }, [partSearch, parts]);

  const hydrateSelectedPart = useCallback((part) => {
    const id = String(partId(part) || "");
    const loadedPart = id
      ? parts.find((item) => String(partId(item)) === id)
      : null;
    const nextPart = loadedPart ? { ...part, ...loadedPart } : part;
    setSelectedPart(nextPart);
    setPartSearch(partTitle(nextPart));
    setRequestNotes((current) => current || `Mechanic request from ${partCode(nextPart)} scan.`);
  }, [parts]);

  const loadStock = useCallback(async (part) => {
    const id = partId(part);
    if (!id) {
      setStockRows([]);
      setSelectedWarehouseId("");
      return;
    }

    const rows = asRows(await api.get(`/api/parts/${id}/stock`));
    setStockRows(rows);
    setSelectedWarehouseId((current) => {
      if (current && rows.some((row) => String(read(row, "warehouseId")) === String(current))) {
        return current;
      }

      const firstAvailable = rows.find((row) => toNumber(read(row, "availableQuantity")) > 0) || rows[0];
      return firstAvailable ? String(read(firstAvailable, "warehouseId")) : "";
    });
  }, [api]);

  const selectPart = useCallback(async (part) => {
    hydrateSelectedPart(part);
    setActiveRequestId("");
    setIsWorking(true);
    setStatus(t("mechanic.checkingStock", "Checking stock..."));
    try {
      await loadStock(part);
      setStatus(t("mechanic.stockReady", "Stock is ready."));
    } catch (error) {
      setStockRows([]);
      setStatus(error.message || t("mechanic.stockError", "Could not check stock."));
    } finally {
      setIsWorking(false);
    }
  }, [hydrateSelectedPart, loadStock, t]);

  const loadParts = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("mechanic.loadingParts", "Loading mechanic stock..."));
    try {
      const rows = asRows(await api.get("/api/parts?page=1&pageSize=250"));
      setParts(rows);
      setStatus(t("mechanic.partsLoaded", "{count} parts ready.", { count: rows.length }));
    } catch (error) {
      setParts([]);
      setStatus(error.message || t("mechanic.partsError", "Could not load parts."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => { loadParts(); }, [loadParts]);

  const resolveScan = useCallback(async () => {
    const code = scanCode.trim();
    if (!code) {
      setStatus(t("mechanic.scanRequired", "Scan or type a part code first."));
      return;
    }

    setIsWorking(true);
    setStatus(t("mechanic.scanning", "Resolving scan..."));
    try {
      const rows = asRows(await api.get(`/api/scans/resolve?code=${encodeURIComponent(code)}`));
      const partRows = rows.filter((row) => String(read(row, "targetType")).toLowerCase() === "part");
      setScanRows(partRows);

      if (partRows[0]) {
        await selectPart(partRows[0]);
        return;
      }

      const fallback = parts.find((part) => [
        read(part, "barcode"),
        read(part, "internalCode"),
        String(read(part, "id"))
      ].some((value) => String(value || "").toLowerCase() === code.toLowerCase()));

      if (fallback) {
        await selectPart(fallback);
        return;
      }

      setStatus(t("mechanic.noScanMatch", "No part matched this scan. Create a quick request below."));
      setSelectedPart(null);
      setActiveRequestId("");
      setStockRows([]);
      setRequestNotes((current) => current || `Scan not found: ${code}`);
    } catch (error) {
      setScanRows([]);
      setStatus(error.message || t("mechanic.scanError", "Could not resolve this scan."));
    } finally {
      setIsWorking(false);
    }
  }, [api, parts, scanCode, selectPart, t]);

  const reservePart = useCallback(async (expirationAction = "AutoRelease") => {
    if (!selectedPartId) {
      setStatus(t("mechanic.selectPartFirst", "Select a part first."));
      return;
    }

    if (!customerName.trim()) {
      setStatus(t("mechanic.customerRequired", "Enter the customer name first."));
      return;
    }

    setIsWorking(true);
    setStatus(t("mechanic.reserving", "Reserving stock..."));
    try {
      const quantity = toPositiveInteger(reservationQuantity);
      const requestId = activeRequestId || await api.post("/api/partrequests", {
        partId: Number(selectedPartId),
        customerId: null,
        customerName,
        customerPhone,
        requestedPartName: partTitle(selectedPart),
        requestedOemNumber: partOem(selectedPart),
        vehicleDetails,
        quantity,
        notes: requestNotes || `Reserved from Mechanic Mode for ${partCode(selectedPart)}.`
      });
      const reservation = await api.post(`/api/partrequests/${requestId}/reserve`, {
        quantity,
        warehouseId: selectedWarehouseId ? Number(selectedWarehouseId) : null,
        expiresAt: defaultReservationExpiresAt().toISOString(),
        expirationAction
      });
      setActiveRequestId(String(requestId));
      await loadStock(selectedPart);
      setStatus(t("mechanic.reserved", "Reserved {count} unit(s) on request #{id}.", {
        count: read(reservation, "quantity") || quantity,
        id: requestId
      }));
    } catch (error) {
      setStatus(error.message || t("mechanic.reserveError", "Could not reserve this part."));
    } finally {
      setIsWorking(false);
    }
  }, [activeRequestId, api, customerName, customerPhone, loadStock, requestNotes, reservationQuantity, selectedPart, selectedPartId, selectedWarehouseId, t, vehicleDetails]);

  const takePhoto = useCallback(async () => {
    try {
      const permission = await ImagePicker.requestCameraPermissionsAsync();
      if (!permission.granted) {
        setStatus(t("mechanic.photoPermission", "Camera permission is required to send a part photo."));
        return;
      }

      const result = await ImagePicker.launchCameraAsync({
        base64: true,
        mediaTypes: ["images"],
        quality: 0.74
      });

      if (result.canceled || !result.assets || result.assets.length === 0) return;
      setPhotoAsset(result.assets[0]);
      setStatus(t("mechanic.photoReady", "Photo ready to send."));
    } catch (error) {
      setStatus(error.message || t("mechanic.photoError", "Could not open the camera."));
    }
  }, [t]);

  const sendPhoto = useCallback(async () => {
    if (!customerPhone.trim()) {
      setStatus(t("mechanic.phoneRequired", "Enter the customer WhatsApp number first."));
      return;
    }

    const attachment = buildAttachment(photoAsset);
    if (!attachment) {
      setStatus(t("mechanic.photoRequired", "Take a part photo first."));
      return;
    }

    setIsWorking(true);
    setStatus(t("mechanic.sendingPhoto", "Sending photo to customer..."));
    try {
      const name = customerName || t("contacts.customers", "Customer");
      const body = [
        `Hello ${name},`,
        selectedPart ? `Photo for ${partTitle(selectedPart)}.` : "Here is the part photo from the workshop.",
        selectedPart ? `Code: ${partCode(selectedPart)}. OEM: ${partOem(selectedPart) || "N/A"}.` : "",
        selectedPart ? `Available: ${selectedStock.available}. Price: ${money(read(selectedPart, "salePrice"), partCurrency(selectedPart))}.` : "",
        requestNotes.trim()
      ].filter(Boolean).join("\n");

      await api.post("/api/communications/send", CommunicationPayloadFactory.freeText({
        recipientName: name,
        recipientPhone: customerPhone,
        body,
        attachments: [attachment]
      }));
      setStatus(t("mechanic.photoSent", "Part photo sent."));
    } catch (error) {
      setStatus(error.message || t("mechanic.sendPhotoError", "Could not send the photo."));
    } finally {
      setIsWorking(false);
    }
  }, [api, customerName, customerPhone, photoAsset, requestNotes, selectedPart, selectedStock.available, t]);

  const createRequest = useCallback(async () => {
    const requestedPartName = selectedPart ? partTitle(selectedPart) : partSearch.trim();
    if (!customerName.trim()) {
      setStatus(t("mechanic.customerRequired", "Enter the customer name first."));
      return;
    }

    if (!requestedPartName.trim()) {
      setStatus(t("mechanic.requestPartRequired", "Enter or scan the requested part."));
      return;
    }

    setIsWorking(true);
    setStatus(t("mechanic.creatingRequest", "Creating quick request..."));
    try {
      const id = await api.post("/api/partrequests", {
        partId: selectedPartId ? Number(selectedPartId) : null,
        customerId: null,
        customerName,
        customerPhone,
        requestedPartName,
        requestedOemNumber: selectedPart ? partOem(selectedPart) : "",
        vehicleDetails,
        quantity: toPositiveInteger(reservationQuantity),
        notes: requestNotes
      });
      setActiveRequestId(String(id));
      setStatus(t("mechanic.requestCreated", "Quick request #{id} created.", { id }));
    } catch (error) {
      setStatus(error.message || t("mechanic.requestError", "Could not create the quick request."));
    } finally {
      setIsWorking(false);
    }
  }, [api, customerName, customerPhone, partSearch, requestNotes, reservationQuantity, selectedPart, selectedPartId, t, vehicleDetails]);

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: t("mechanic.title", "Mechanic Mode"),
      actionTitle: t("common.refresh", "Refresh"),
      onAction: loadParts,
      loading: isLoading
    }),
    el(View, { style: styles.mechanicHero },
      el(View, { style: styles.mechanicHeroCopy },
        el(Text, { style: styles.mechanicHeroTitle }, t("mechanic.fastLane", "Scan. Stock. Reserve.")),
        el(Text, { style: styles.mechanicHeroMeta }, t("mechanic.activePart", selectedPart ? partTitle(selectedPart) : "No part selected"))
      ),
      el(View, { style: styles.mechanicHeroMark },
        el(Text, { style: styles.mechanicHeroMarkText }, selectedStock.available > 0 ? String(selectedStock.available) : "--"),
        el(Text, { style: styles.mechanicHeroMarkLabel }, t("mechanic.avail", "Avail"))
      )
    ),
    el(Panel, { title: t("mechanic.scanPart", "Scan part") },
      el(Field, {
        label: t("mechanic.scanCode", "Scan / type code"),
        value: scanCode,
        onChangeText: setScanCode,
        onSubmitEditing: resolveScan,
        placeholder: t("mechanic.scanPlaceholder", "Barcode, QR, internal code"),
        returnKeyType: "search",
        autoCapitalize: "characters"
      }),
      el(View, { style: styles.inlineButtons },
        el(PrimaryButton, {
          title: isWorking ? t("common.loading", "Loading") : t("mechanic.scan", "Scan"),
          onPress: resolveScan,
          disabled: isWorking,
          compact: true
        }),
        el(SecondaryButton, { title: t("mechanic.clear", "Clear"), onPress: () => {
          setScanCode("");
          setScanRows([]);
          setSelectedPart(null);
          setActiveRequestId("");
          setStockRows([]);
        } })
      ),
      scanRows.length > 1 && el(View, { style: styles.mechanicResultStack },
        scanRows.map((row) =>
          el(Pressable, { key: `${read(row, "targetType")}-${read(row, "targetId")}`, onPress: () => selectPart(row) },
            el(ListRow, { title: partTitle(row), subtitle: [partCode(row), partOem(row)].filter(Boolean).join(" / ") })
          )
        )
      )
    ),
    el(Panel, { title: t("mechanic.findPart", "Find part") },
      el(Field, {
        label: t("common.search", "Search"),
        value: partSearch,
        onChangeText: setPartSearch,
        placeholder: t("mechanic.findPlaceholder", "Name, OEM, barcode, code")
      }),
      el(View, { style: styles.mechanicPickerFrame },
        el(ScrollView, { nestedScrollEnabled: true, contentContainerStyle: styles.screenListContent },
          filteredParts.map((part) =>
            el(Pressable, { key: String(partId(part)), onPress: () => selectPart(part) },
              el(ListRow, {
                title: partTitle(part),
                subtitle: [partCode(part), partOem(part) ? `OEM ${partOem(part)}` : ""].filter(Boolean).join(" / "),
                value: money(read(part, "salePrice"), partCurrency(part))
              })
            )
          ),
          filteredParts.length === 0 && el(EmptyState, { text: t("mechanic.noParts", "No parts found.") })
        )
      )
    ),
    selectedPart && el(Panel, { title: t("mechanic.stock", "Stock") },
      el(View, { style: styles.mechanicPartCard },
        el(View, { style: styles.mechanicPartMark },
          el(Text, { style: styles.mechanicPartMarkText }, partCode(selectedPart).slice(0, 2).toUpperCase())
        ),
        el(View, { style: styles.mechanicPartCopy },
          el(Text, { style: styles.mechanicPartTitle }, partTitle(selectedPart)),
          el(Text, { style: styles.mechanicPartMeta }, [partCode(selectedPart), partOem(selectedPart) ? `OEM ${partOem(selectedPart)}` : ""].filter(Boolean).join(" / "))
        )
      ),
      el(View, { style: styles.mechanicMetricRow },
        el(View, { style: styles.mechanicMetric },
          el(Text, { style: styles.mechanicMetricLabel }, t("mechanic.onHand", "On hand")),
          el(Text, { style: styles.mechanicMetricValue }, String(selectedStock.quantity))
        ),
        el(View, { style: styles.mechanicMetric },
          el(Text, { style: styles.mechanicMetricLabel }, t("mechanic.reservedCount", "Reserved")),
          el(Text, { style: styles.mechanicMetricValue }, String(selectedStock.reserved))
        ),
        el(View, { style: [styles.mechanicMetric, selectedStock.available > 0 && styles.mechanicMetricGood] },
          el(Text, { style: styles.mechanicMetricLabel }, t("mechanic.available", "Available")),
          el(Text, { style: styles.mechanicMetricValue }, String(selectedStock.available))
        )
      ),
      el(ScrollView, { horizontal: true, showsHorizontalScrollIndicator: false, contentContainerStyle: styles.segmentRail },
        stockRows.map((row) => {
          const warehouseId = String(read(row, "warehouseId"));
          const active = warehouseId === String(selectedWarehouseId);
          return el(Pressable, {
            key: warehouseId,
            style: [styles.mechanicWarehouseChip, active && styles.mechanicWarehouseChipActive],
            onPress: () => setSelectedWarehouseId(warehouseId)
          },
            el(Text, { style: [styles.mechanicWarehouseName, active && styles.mechanicWarehouseNameActive] }, read(row, "warehouseName") || `Warehouse ${warehouseId}`),
            el(Text, { style: styles.mechanicWarehouseMeta }, `${read(row, "availableQuantity") || 0} available`)
          );
        }),
        stockRows.length === 0 && el(EmptyState, { text: t("mechanic.noStock", "No stock rows found.") })
      ),
      el(Field, {
        label: t("mechanic.reserveQuantity", "Reserve quantity"),
        value: reservationQuantity,
        onChangeText: setReservationQuantity,
        keyboardType: "number-pad"
      }),
      el(View, { style: styles.inlineButtons },
        el(PrimaryButton, {
          title: t("mechanic.reserve", "Reserve"),
          onPress: () => reservePart("AutoRelease"),
          disabled: isWorking || selectedStock.available <= 0,
          compact: true
        }),
        el(PrimaryButton, {
          title: t("mechanic.remindStaff", "Remind Staff"),
          onPress: () => reservePart("StaffReminder"),
          disabled: isWorking || selectedStock.available <= 0,
          compact: true
        })
      )
    ),
    el(Panel, { title: t("mechanic.customerPhoto", "Customer photo") },
      el(Field, {
        label: t("parts.recipientName", "Recipient name"),
        value: customerName,
        onChangeText: setCustomerName
      }),
      el(Field, {
        label: t("parts.whatsappPhone", "WhatsApp phone"),
        value: customerPhone,
        onChangeText: setCustomerPhone,
        keyboardType: "phone-pad"
      }),
      el(View, { style: styles.mechanicPhotoFrame },
        photoAsset
          ? el(Image, { source: { uri: photoAsset.uri }, style: styles.mechanicPhoto })
          : el(View, { style: styles.mechanicPhotoEmpty },
            el(Text, { style: styles.mechanicPhotoEmptyTitle }, t("mechanic.noPhoto", "No photo yet")),
            el(Text, { style: styles.mechanicPhotoEmptyText }, t("mechanic.noPhotoDetail", "Take a close part photo before sending."))
          )
      ),
      el(View, { style: styles.inlineButtons },
        el(SecondaryButton, { title: t("mechanic.takePhoto", "Take Photo"), onPress: takePhoto }),
        el(PrimaryButton, {
          title: isWorking ? t("common.loading", "Loading") : t("mechanic.sendPhoto", "Send Photo"),
          onPress: sendPhoto,
          disabled: isWorking,
          compact: true
        })
      )
    ),
    el(Panel, { title: t("mechanic.quickRequest", "Quick request") },
      el(Field, {
        label: t("mechanic.vehicle", "Vehicle"),
        value: vehicleDetails,
        onChangeText: setVehicleDetails,
        placeholder: t("mechanic.vehiclePlaceholder", "BMW F30 2016, VIN, plate")
      }),
      !selectedPart && el(Field, {
        label: t("mechanic.requestedPart", "Requested part"),
        value: partSearch,
        onChangeText: setPartSearch,
        placeholder: t("mechanic.requestedPartPlaceholder", "Customer needs...")
      }),
      el(Field, {
        label: t("mechanic.notes", "Notes"),
        value: requestNotes,
        onChangeText: setRequestNotes,
        multiline: true,
        numberOfLines: 3,
        placeholder: t("mechanic.notesPlaceholder", "Mechanic note, symptoms, side, urgency")
      }),
      el(PrimaryButton, {
        title: isWorking ? t("common.loading", "Loading") : t("mechanic.createRequest", "Create Request"),
        onPress: createRequest,
        disabled: isWorking
      })
    ),
    el(StatusText, { value: status })
  );
}

module.exports = { MechanicModeScreen };
