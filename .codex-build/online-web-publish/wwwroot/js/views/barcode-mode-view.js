import { h, useCallback, useEffect, useMemo, useRef, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

const code128Patterns = [
  "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312",
  "132212", "221213", "221312", "231212", "112232", "122132", "122231", "113222",
  "123122", "123221", "223211", "221132", "221231", "213212", "223112", "312131",
  "311222", "321122", "321221", "312212", "322112", "322211", "212123", "212321",
  "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
  "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121",
  "313121", "211331", "231131", "213113", "213311", "213131", "311123", "311321",
  "331121", "312113", "312311", "332111", "314111", "221411", "431111", "111224",
  "111422", "121124", "121421", "141122", "141221", "112214", "112412", "122114",
  "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
  "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112",
  "421211", "212141", "214121", "412121", "111143", "111341", "131141", "114113",
  "114311", "411113", "411311", "113141", "114131", "311141", "411131", "211412",
  "211214", "211232", "2331112"
];

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
  const number = Number.parseFloat(String(value || "").replace(/,/g, ""));
  return Number.isFinite(number) ? number : 0;
}

function toId(value) {
  const id = Number.parseInt(String(value || ""), 10);
  return Number.isFinite(id) && id > 0 ? id : 0;
}

function todayInvoiceDate() {
  const now = new Date();
  return new Date(now.getFullYear(), now.getMonth(), now.getDate()).toISOString();
}

function partTitle(part) {
  return [read(part, "internalCode"), read(part, "name")].filter(Boolean).join(" - ") || `Part #${read(part, "id")}`;
}

function labelCode(part) {
  return String(read(part, "barcode") || read(part, "internalCode") || read(part, "id") || "").trim();
}

function qrPayload(part) {
  return `part:${labelCode(part)}`;
}

function warehouseName(warehouse) {
  return read(warehouse, "name") || `Warehouse #${read(warehouse, "id")}`;
}

function usedCarName(car) {
  const title = read(car, "car", "displayName", "name");
  const year = read(car, "modelYear");
  return [year, title].filter(Boolean).join(" ") || `Used car #${read(car, "id")}`;
}

function partMatches(part, query) {
  if (!query) return true;
  const normalized = query.toLowerCase();
  return [
    read(part, "internalCode"),
    read(part, "barcode"),
    read(part, "name"),
    read(part, "oemNumber")
  ].join(" ").toLowerCase().includes(normalized);
}

function stockSummary(stockRows) {
  return stockRows.reduce((summary, row) => ({
    quantity: summary.quantity + toNumber(read(row, "quantity")),
    reserved: summary.reserved + toNumber(read(row, "reservedQuantity")),
    available: summary.available + toNumber(read(row, "availableQuantity"))
  }), { quantity: 0, reserved: 0, available: 0 });
}

function normalizeBarcodeText(value) {
  const text = String(value || "").trim();
  const cleaned = Array.from(text)
    .map((char) => {
      const code = char.charCodeAt(0);
      return code >= 32 && code <= 127 ? char : "-";
    })
    .join("");
  return cleaned || "0";
}

function code128Sequence(value) {
  const text = normalizeBarcodeText(value);
  const codes = Array.from(text).map((char) => char.charCodeAt(0) - 32);
  const checksum = codes.reduce((sum, code, index) => sum + code * (index + 1), 104) % 103;
  return [104, ...codes, checksum, 106];
}

function Code128Svg({ value }) {
  const unit = 1.5;
  const height = 42;
  let x = 8;
  const bars = [];

  code128Sequence(value).forEach((code, codeIndex) => {
    const pattern = code128Patterns[code] || "";
    Array.from(pattern).forEach((widthChar, index) => {
      const width = Number(widthChar) * unit;
      if (index % 2 === 0) {
        bars.push({ x, width, key: `${codeIndex}-${index}-${x}` });
      }
      x += width;
    });
  });

  const width = Math.ceil(x + 8);
  return h("svg", {
    className: "barcode-svg",
    viewBox: `0 0 ${width} 54`,
    role: "img",
    "aria-label": `Barcode ${value}`
  },
    h("rect", { x: 0, y: 0, width, height: 54, fill: "#fff" }),
    bars.map((bar) => h("rect", { key: bar.key, x: bar.x, y: 7, width: bar.width, height, fill: "#111" }))
  );
}

function QrCode({ value }) {
  let markup = "";
  try {
    if (typeof window !== "undefined" && typeof window.qrcode === "function") {
      const qr = window.qrcode(0, "M");
      qr.addData(value);
      qr.make();
      markup = qr.createSvgTag(3, 0);
    }
  } catch {
    markup = "";
  }

  return h("div", { className: "qr-box" },
    markup
      ? h("div", { className: "qr-svg", dangerouslySetInnerHTML: { __html: markup } })
      : h("span", null, value)
  );
}

function LabelCard({ part }) {
  const code = labelCode(part);
  return h("article", { className: "part-label-card" },
    h("div", { className: "label-qr-column" },
      h(QrCode, { value: qrPayload(part) }),
      h("b", null, "QR")
    ),
    h("div", { className: "label-main" },
      h("strong", null, read(part, "internalCode") || code),
      h("span", null, read(part, "name") || "Part"),
      h(Code128Svg, { value: code }),
      h("small", null, [
        read(part, "oemNumber") ? `OEM ${read(part, "oemNumber")}` : "",
        money(read(part, "salePrice"), read(part, "currency") || "USD")
      ].filter(Boolean).join(" / "))
    )
  );
}

function ScanResultActions({ row, onUse }) {
  return h("button", { className: "secondary-button", type: "button", onClick: () => onUse(row) }, "Use");
}

function visualPartTitle(match) {
  return [read(match, "internalCode"), read(match, "partName")].filter(Boolean).join(" - ") || `Part #${read(match, "partId")}`;
}

export function BarcodeModeView({ api, onNavigate, onView, t }) {
  const navigate = onView || onNavigate;
  const visualFileInputRef = useRef(null);
  const [parts, setParts] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [usedCars, setUsedCars] = useState([]);
  const [stockRows, setStockRows] = useState([]);
  const [scanCode, setScanCode] = useState("");
  const [scanResults, setScanResults] = useState([]);
  const [selectedPartId, setSelectedPartId] = useState("");
  const [saleWarehouseId, setSaleWarehouseId] = useState("");
  const [saleQuantity, setSaleQuantity] = useState("1");
  const [salePaidAmount, setSalePaidAmount] = useState("");
  const [transferFromWarehouseId, setTransferFromWarehouseId] = useState("");
  const [transferToWarehouseId, setTransferToWarehouseId] = useState("");
  const [transferQuantity, setTransferQuantity] = useState("1");
  const [attachUsedCarId, setAttachUsedCarId] = useState("");
  const [labelSearch, setLabelSearch] = useState("");
  const [labelPartIds, setLabelPartIds] = useState([]);
  const [visualFile, setVisualFile] = useState(null);
  const [visualHint, setVisualHint] = useState("");
  const [visualPreviewUrl, setVisualPreviewUrl] = useState("");
  const [visualResponse, setVisualResponse] = useState(null);
  const [visualResults, setVisualResults] = useState([]);
  const [status, setStatus] = useState("Scan or type a label code to start.");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isVisualSearching, setIsVisualSearching] = useState(false);

  const selectedPart = useMemo(
    () => parts.find((part) => String(read(part, "id")) === String(selectedPartId)) || null,
    [parts, selectedPartId]
  );

  const selectedStock = useMemo(() => stockSummary(stockRows), [stockRows]);

  const labelParts = useMemo(() => {
    const byId = new Map(parts.map((part) => [String(read(part, "id")), part]));
    return labelPartIds.map((id) => byId.get(String(id))).filter(Boolean);
  }, [labelPartIds, parts]);

  const filteredLabelParts = useMemo(
    () => parts.filter((part) => partMatches(part, labelSearch)).slice(0, 36),
    [labelSearch, parts]
  );

  const loadStock = useCallback(async (partId) => {
    if (!toId(partId)) {
      setStockRows([]);
      return [];
    }

    const nextStockRows = toArray(await api.get(`/api/parts/${partId}/stock`));
    setStockRows(nextStockRows);
    const firstStockWarehouse = nextStockRows.find((row) => toNumber(read(row, "availableQuantity")) > 0);
    setTransferFromWarehouseId((current) => current || String(read(firstStockWarehouse, "warehouseId") || saleWarehouseId || ""));
    return nextStockRows;
  }, [api, saleWarehouseId]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading AR search mode...");
    try {
      const [nextParts, nextWarehouses, nextUsedCars] = await Promise.all([
        api.get("/api/parts?page=1&pageSize=500"),
        api.get("/api/warehouses"),
        api.get("/api/usedcars")
      ]);
      const partRows = toArray(nextParts);
      const warehouseRows = toArray(nextWarehouses);
      setParts(partRows);
      setWarehouses(warehouseRows);
      setUsedCars(toArray(nextUsedCars));
      setSelectedPartId((current) => {
        if (current && partRows.some((part) => String(read(part, "id")) === String(current))) return current;
        return String(read(partRows[0], "id") || "");
      });
      setLabelPartIds((current) => current.length ? current : partRows.slice(0, 4).map((part) => String(read(part, "id"))));
      const mainWarehouse = warehouseRows.find((warehouse) => read(warehouse, "isMain")) || warehouseRows[0];
      const secondWarehouse = warehouseRows.find((warehouse) => String(read(warehouse, "id")) !== String(read(mainWarehouse, "id"))) || warehouseRows[0];
      setSaleWarehouseId((current) => current || String(read(mainWarehouse, "id") || ""));
      setTransferFromWarehouseId((current) => current || String(read(mainWarehouse, "id") || ""));
      setTransferToWarehouseId((current) => current || String(read(secondWarehouse, "id") || ""));
      setStatus(`Ready. ${partRows.length} part(s), ${warehouseRows.length} warehouse(s), ${toArray(nextUsedCars).length} used car(s).`);
    } catch (error) {
      setParts([]);
      setWarehouses([]);
      setUsedCars([]);
      setStatus(error.message || "Could not load AR search mode.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  useEffect(() => () => {
    if (visualPreviewUrl) {
      URL.revokeObjectURL(visualPreviewUrl);
    }
  }, [visualPreviewUrl]);

  useEffect(() => {
    if (!selectedPartId) {
      setStockRows([]);
      return;
    }

    let cancelled = false;
    loadStock(selectedPartId)
      .then((rows) => {
        if (cancelled) return;
        const part = parts.find((item) => String(read(item, "id")) === String(selectedPartId));
        if (part) {
          setAttachUsedCarId(read(part, "usedCarId") ? String(read(part, "usedCarId")) : "");
        }
        const firstAvailable = rows.find((row) => toNumber(read(row, "availableQuantity")) > 0);
        if (firstAvailable) {
          setSaleWarehouseId((current) => current || String(read(firstAvailable, "warehouseId")));
        }
      })
      .catch((error) => {
        if (!cancelled) setStatus(error.message || "Could not load stock for selected part.");
      });
    return () => {
      cancelled = true;
    };
  }, [loadStock, parts, selectedPartId]);

  const useScanResult = useCallback((row) => {
    const targetType = String(read(row, "targetType")).toLowerCase();
    const targetId = String(read(row, "targetId") || "");
    if (targetType === "part" && targetId) {
      setSelectedPartId(targetId);
      setLabelPartIds((current) => current.includes(targetId) ? current : [targetId, ...current].slice(0, 12));
      setStatus(`${read(row, "displayText") || "Part"} selected.`);
      return;
    }

    if (targetType === "warehouse" && targetId) {
      setSaleWarehouseId(targetId);
      setTransferFromWarehouseId(targetId);
      setStatus(`${read(row, "displayText") || "Warehouse"} selected as source warehouse.`);
      return;
    }

    if (targetType === "used_car" && targetId) {
      setAttachUsedCarId(targetId);
      setStatus(`${read(row, "displayText") || "Used car"} selected for attachment.`);
      return;
    }

    setStatus(read(row, "displayText") || "Scan result selected.");
  }, []);

  const resolveScan = useCallback(async () => {
    const code = scanCode.trim();
    if (!code) {
      setStatus("Scan or type a code first.");
      return;
    }

    setIsLoading(true);
    setStatus("Resolving scan...");
    try {
      const rows = toArray(await api.get(`/api/scans/resolve?code=${encodeURIComponent(code)}`));
      setScanResults(rows);
      if (rows.length === 1) {
        useScanResult(rows[0]);
      } else {
        setStatus(rows.length ? `${rows.length} result(s) found.` : "No record matched that scan.");
      }
    } catch (error) {
      setScanResults([]);
      setStatus(error.message || "Could not resolve scan.");
    } finally {
      setIsLoading(false);
    }
  }, [api, scanCode, useScanResult]);

  const chooseVisualFile = useCallback((files) => {
    const file = Array.from(files || [])[0];
    if (!file) return;

    setVisualFile(file);
    setVisualResponse(null);
    setVisualResults([]);
    setStatus("Picture ready. Add a hint if needed, then search.");

    setVisualPreviewUrl((current) => {
      if (current) URL.revokeObjectURL(current);
      return URL.createObjectURL(file);
    });
  }, []);

  const useVisualResult = useCallback((match) => {
    const targetId = String(read(match, "partId") || "");
    if (!targetId) return;

    setSelectedPartId(targetId);
    setLabelPartIds((current) => current.includes(targetId) ? current : [targetId, ...current].slice(0, 12));
    setStatus(`${visualPartTitle(match)} selected from picture search.`);
  }, []);

  const searchByPicture = useCallback(async () => {
    if (!visualFile) {
      setStatus("Capture or choose a part picture first.");
      return;
    }

    setIsVisualSearching(true);
    setStatus("Searching catalog from picture...");
    try {
      const formData = new FormData();
      formData.append("image", visualFile, visualFile.name || "part-picture.jpg");
      formData.append("hint", visualHint.trim());
      formData.append("limit", "10");
      const response = await api.postForm("/api/scans/visual-search", formData);
      const matches = toArray(read(response, "matches"));
      setVisualResponse(response);
      setVisualResults(matches);

      if (matches[0]) {
        useVisualResult(matches[0]);
      }

      setStatus(matches.length
        ? `${matches.length} picture match(es) found.`
        : read(response, "message") || "No part matched that picture.");
    } catch (error) {
      setVisualResponse(null);
      setVisualResults([]);
      setStatus(error.message || "Could not search by picture.");
    } finally {
      setIsVisualSearching(false);
      if (visualFileInputRef.current) visualFileInputRef.current.value = "";
    }
  }, [api, useVisualResult, visualFile, visualHint]);

  const addLabelPart = useCallback((part) => {
    const id = String(read(part, "id"));
    if (!id) return;
    setLabelPartIds((current) => current.includes(id) ? current : [...current, id].slice(-24));
    setSelectedPartId(id);
    setStatus(`${partTitle(part)} added to label sheet.`);
  }, []);

  const createSale = useCallback(async () => {
    if (!selectedPart) {
      setStatus("Select a part before selling.");
      return;
    }
    const warehouseId = toId(saleWarehouseId);
    if (!warehouseId) {
      setStatus("Choose a selling warehouse.");
      return;
    }

    const quantity = Math.max(1, Math.trunc(toNumber(saleQuantity)));
    const unitPrice = Math.max(0, toNumber(read(selectedPart, "salePrice")));
    setIsSaving(true);
    setStatus("Creating scanned sale...");
    try {
      const total = quantity * unitPrice;
      const response = await api.post("/api/sales", {
        invoiceDate: todayInvoiceDate(),
        customerId: null,
        warehouseId,
        paymentMethod: "Cash",
        paidAmount: salePaidAmount === "" ? total : Math.max(0, toNumber(salePaidAmount)),
        notes: `Created from barcode scan ${labelCode(selectedPart)}.`,
        items: [{
          partId: toId(read(selectedPart, "id")),
          quantity,
          unitPrice,
          discountAmount: 0,
          taxRate: 0
        }]
      });
      setSaleQuantity("1");
      setSalePaidAmount("");
      await load();
      await loadStock(read(selectedPart, "id"));
      setStatus(`Sale ${read(response, "invoiceNumber") || ""} created for ${partTitle(selectedPart)}.`.trim());
    } catch (error) {
      setStatus(error.message || "Could not create sale from scan.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load, loadStock, salePaidAmount, saleQuantity, saleWarehouseId, selectedPart]);

  const transferStock = useCallback(async () => {
    if (!selectedPart) {
      setStatus("Select a part before moving stock.");
      return;
    }

    const fromWarehouseId = toId(transferFromWarehouseId);
    const toWarehouseId = toId(transferToWarehouseId);
    const quantity = Math.max(1, Math.trunc(toNumber(transferQuantity)));
    if (!fromWarehouseId || !toWarehouseId || fromWarehouseId === toWarehouseId) {
      setStatus("Choose two different warehouses for the move.");
      return;
    }

    setIsSaving(true);
    setStatus("Moving warehouse stock...");
    try {
      await api.post(`/api/parts/${read(selectedPart, "id")}/transfer`, {
        fromWarehouseId,
        toWarehouseId,
        quantity
      });
      await load();
      await loadStock(read(selectedPart, "id"));
      setStatus(`${quantity} ${partTitle(selectedPart)} moved between warehouses.`);
    } catch (error) {
      setStatus(error.message || "Could not move warehouse stock.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load, loadStock, selectedPart, transferFromWarehouseId, transferQuantity, transferToWarehouseId]);

  const attachToUsedCar = useCallback(async () => {
    if (!selectedPart) {
      setStatus("Select a part before attaching it to a used car.");
      return;
    }

    setIsSaving(true);
    setStatus("Updating used-car link...");
    try {
      await api.put(`/api/parts/${read(selectedPart, "id")}/usedcar`, {
        usedCarId: attachUsedCarId ? toId(attachUsedCarId) : null
      });
      await load();
      await loadStock(read(selectedPart, "id"));
      setStatus(attachUsedCarId ? "Part attached to selected used car." : "Part removed from used-car inventory.");
    } catch (error) {
      setStatus(error.message || "Could not update used-car link.");
    } finally {
      setIsSaving(false);
    }
  }, [api, attachUsedCarId, load, loadStock, selectedPart]);

  const checkStock = useCallback(async () => {
    if (!selectedPart) {
      setStatus("Select a part before checking stock.");
      return;
    }

    setIsLoading(true);
    setStatus("Checking stock...");
    try {
      const rows = await loadStock(read(selectedPart, "id"));
      const summary = stockSummary(rows);
      setStatus(`Stock checked: ${summary.available} available, ${summary.reserved} reserved, ${summary.quantity} on hand.`);
    } catch (error) {
      setStatus(error.message || "Could not check stock.");
    } finally {
      setIsLoading(false);
    }
  }, [loadStock, selectedPart]);

  const selectedWarehouse = warehouses.find((warehouse) => String(read(warehouse, "id")) === String(saleWarehouseId));

  return h("section", { className: "screen barcode-mode-screen" },
    h(PageHeader, {
      title: "AR Picture Search",
      subtitle: "Capture a part, overlay ranked matches, and act on the part in hand.",
      action: h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, isLoading ? "Loading" : "Refresh")
    }),
    h("section", { className: "barcode-command-panel" },
      h("div", { className: "scan-console" },
        h("label", null, "Scan input",
          h("input", {
            value: scanCode,
            onChange: (event) => setScanCode(event.target.value),
            onKeyDown: (event) => event.key === "Enter" && resolveScan(),
            placeholder: "Scan barcode, QR payload, invoice, warehouse, or used-car code",
            autoComplete: "off"
          })
        ),
        h("button", { className: "primary-button", type: "button", onClick: resolveScan, disabled: isLoading }, "Resolve"),
        h("button", { className: "secondary-button", type: "button", onClick: () => window.print(), disabled: labelParts.length === 0 }, "Print labels")
      ),
      selectedPart
        ? h("article", { className: "selected-scan-part" },
          h("span", null, "Selected part"),
          h("strong", null, partTitle(selectedPart)),
          h("div", { className: "scan-part-metrics" },
            h("b", null, `${selectedStock.available} available`),
            h("b", null, money(read(selectedPart, "salePrice"), read(selectedPart, "currency") || "USD")),
            h("b", null, read(selectedPart, "usedCarId") ? `Used car #${read(selectedPart, "usedCarId")}` : "Shelf stock")
          )
        )
        : h("article", { className: "selected-scan-part empty" },
          h("span", null, "Selected part"),
          h("strong", null, "No part selected")
        )
    ),
    h(StatusLine, { status }),
    h("section", { className: "visual-search-workspace" },
      h("article", { className: "visual-camera-panel" },
        h("div", { className: "panel-heading-row" },
          h("div", null,
            h("h3", null, "Picture search"),
            h("span", null, read(visualResponse, "source") === "ai-vision" ? "AI vision labels enabled" : "Camera photo with catalog matching")
          ),
          h("button", {
            className: "secondary-button",
            type: "button",
            onClick: () => visualFileInputRef.current?.click()
          }, "Camera / photo")
        ),
        h("input", {
          ref: visualFileInputRef,
          className: "hidden-file-input",
          type: "file",
          accept: "image/*",
          capture: "environment",
          onChange: (event) => chooseVisualFile(event.target.files)
        }),
        h("div", { className: "visual-capture-row" },
          h("label", null, "Hint",
            h("input", {
              value: visualHint,
              onChange: (event) => setVisualHint(event.target.value),
              placeholder: "Optional: BMW headlight, brake sensor, oil filter"
            })
          ),
          h("button", {
            className: "primary-button",
            type: "button",
            onClick: searchByPicture,
            disabled: isVisualSearching || !visualFile
          }, isVisualSearching ? "Searching" : "Search picture")
        ),
        h("div", { className: "visual-preview-frame" },
          visualPreviewUrl
            ? h("img", { src: visualPreviewUrl, alt: "Captured part for AR search" })
            : h("div", { className: "visual-preview-empty" },
              h("strong", null, "No picture selected"),
              h("span", null, "Use the camera or upload a part photo.")
            ),
          visualPreviewUrl && visualResults.slice(0, 5).map((match, index) =>
            h("button", {
              key: `pin-${read(match, "partId") || index}`,
              className: "visual-ar-pin",
              style: {
                left: `${Math.round(toNumber(read(match, "anchorX")) * 100)}%`,
                top: `${Math.round(toNumber(read(match, "anchorY")) * 100)}%`
              },
              type: "button",
              onClick: () => useVisualResult(match),
              title: visualPartTitle(match)
            }, `#${index + 1}`)
          )
        )
      ),
      h("article", { className: "visual-match-panel" },
        h("div", { className: "panel-heading-row" },
          h("div", null,
            h("h3", null, "Ranked matches"),
            h("span", null, read(visualResponse, "searchText") || read(visualResponse, "message") || "Search a picture to see matches")
          ),
          h("span", null, `${visualResults.length} result(s)`)
        ),
        h("div", { className: "visual-tag-row" },
          toArray(read(visualResponse, "visualTags")).map((tag) => h("span", { key: tag }, tag))
        ),
        h("div", { className: "visual-match-list" },
          visualResults.map((match, index) => h("button", {
            key: `visual-${read(match, "partId") || index}`,
            className: "visual-match-row",
            type: "button",
            onClick: () => useVisualResult(match)
          },
            h("span", { className: "visual-match-rank" }, `#${index + 1}`),
            h("span", { className: "visual-match-copy" },
              h("strong", null, visualPartTitle(match)),
              h("small", null, [
                read(match, "matchReason"),
                read(match, "oemNumber") ? `OEM ${read(match, "oemNumber")}` : "",
                `${read(match, "availableQuantity") || 0} available`
              ].filter(Boolean).join(" / "))
            ),
            h("b", null, money(read(match, "salePrice"), read(match, "currency") || "USD"))
          )),
          visualResults.length === 0 && h("p", { className: "empty-state" }, "Capture a part picture to match catalog inventory.")
        )
      )
    ),
    h("section", { className: "barcode-action-grid" },
      h("article", { className: "scan-action-panel" },
        h("h3", null, "Open"),
        h("p", null, selectedPart ? labelCode(selectedPart) : "Select a part from a scan or the label list."),
        h("button", {
          className: "secondary-button",
          type: "button",
          onClick: () => selectedPart && navigate ? navigate("inventory") : setStatus("Select a part first."),
          disabled: !selectedPart
        }, "Open part")
      ),
      h("article", { className: "scan-action-panel" },
        h("h3", null, "Sell"),
        h("div", { className: "compact-control-grid" },
          h("label", null, "Warehouse",
            h("select", { value: saleWarehouseId, onChange: (event) => setSaleWarehouseId(event.target.value) },
              h("option", { value: "" }, "Choose warehouse"),
              warehouses.map((warehouse) => h("option", { key: read(warehouse, "id"), value: read(warehouse, "id") }, warehouseName(warehouse)))
            )
          ),
          h("label", null, "Qty",
            h("input", { value: saleQuantity, inputMode: "numeric", onChange: (event) => setSaleQuantity(event.target.value) })
          ),
          h("label", null, "Paid",
            h("input", { value: salePaidAmount, inputMode: "decimal", placeholder: "Auto", onChange: (event) => setSalePaidAmount(event.target.value) })
          )
        ),
        h("button", { className: "primary-button", type: "button", onClick: createSale, disabled: isSaving || !selectedPart }, "Sell scanned part")
      ),
      h("article", { className: "scan-action-panel" },
        h("h3", null, "Move"),
        h("div", { className: "compact-control-grid" },
          h("label", null, "From",
            h("select", { value: transferFromWarehouseId, onChange: (event) => setTransferFromWarehouseId(event.target.value) },
              h("option", { value: "" }, "Source"),
              warehouses.map((warehouse) => h("option", { key: read(warehouse, "id"), value: read(warehouse, "id") }, warehouseName(warehouse)))
            )
          ),
          h("label", null, "To",
            h("select", { value: transferToWarehouseId, onChange: (event) => setTransferToWarehouseId(event.target.value) },
              h("option", { value: "" }, "Destination"),
              warehouses.map((warehouse) => h("option", { key: read(warehouse, "id"), value: read(warehouse, "id") }, warehouseName(warehouse)))
            )
          ),
          h("label", null, "Qty",
            h("input", { value: transferQuantity, inputMode: "numeric", onChange: (event) => setTransferQuantity(event.target.value) })
          )
        ),
        h("button", { className: "secondary-button", type: "button", onClick: transferStock, disabled: isSaving || !selectedPart }, "Move warehouse")
      ),
      h("article", { className: "scan-action-panel" },
        h("h3", null, "Stock"),
        h("p", null, selectedWarehouse ? `Selling from ${warehouseName(selectedWarehouse)}.` : "Choose a warehouse to check and sell from."),
        h("button", { className: "secondary-button", type: "button", onClick: checkStock, disabled: isLoading || !selectedPart }, "Check stock")
      ),
      h("article", { className: "scan-action-panel" },
        h("h3", null, "Attach"),
        h("label", null, "Used car",
          h("select", { value: attachUsedCarId, onChange: (event) => setAttachUsedCarId(event.target.value) },
            h("option", { value: "" }, "No used car"),
            usedCars.map((car) => h("option", { key: read(car, "id"), value: read(car, "id") }, usedCarName(car)))
          )
        ),
        h("button", { className: "secondary-button", type: "button", onClick: attachToUsedCar, disabled: isSaving || !selectedPart }, "Attach part")
      )
    ),
    h("section", { className: "two-column barcode-detail-layout" },
      h("section", { className: "table-panel compact-table-panel" },
        h("div", { className: "panel-heading-row" },
          h("h3", null, "Scan results"),
          h("span", null, `${scanResults.length} result(s)`)
        ),
        h(DataTable, {
          columns: [
            { key: "type", label: "Type", render: (row) => read(row, "targetType") || "-" },
            { key: "code", label: "Code", render: (row) => read(row, "code") || "-" },
            { key: "result", label: "Result", render: (row) => read(row, "displayText") || `#${read(row, "targetId")}` },
            { key: "use", label: "", render: (row) => h(ScanResultActions, { row, onUse: useScanResult }) }
          ],
          rows: scanResults,
          getRowKey: (row, index) => `${read(row, "targetType")}-${read(row, "targetId") || read(row, "code") || index}`,
          emptyText: "Scan results will appear here."
        })
      ),
      h("section", { className: "table-panel compact-table-panel" },
        h("div", { className: "panel-heading-row" },
          h("h3", null, "Warehouse stock"),
          h("span", null, `${selectedStock.available} available`)
        ),
        h(DataTable, {
          columns: [
            { key: "warehouse", label: "Warehouse", render: (row) => read(row, "warehouseName") || `#${read(row, "warehouseId")}` },
            { key: "quantity", label: "On hand", render: (row) => read(row, "quantity") || 0 },
            { key: "reserved", label: "Reserved", render: (row) => read(row, "reservedQuantity") || 0 },
            { key: "available", label: "Available", render: (row) => read(row, "availableQuantity") || 0 }
          ],
          rows: stockRows,
          getRowKey: (row, index) => `stock-${read(row, "warehouseId") || index}`,
          emptyText: "Select a part to see warehouse stock."
        })
      )
    ),
    h("section", { className: "label-workspace" },
      h("div", { className: "panel-heading-row" },
        h("div", null,
          h("h3", null, "Part labels"),
          h("span", null, `${labelParts.length} label(s) queued`)
        ),
        h("div", { className: "label-actions" },
          selectedPart && h("button", { className: "secondary-button", type: "button", onClick: () => addLabelPart(selectedPart) }, "Add selected"),
          h("button", { className: "ghost-button", type: "button", onClick: () => setLabelPartIds([]), disabled: labelParts.length === 0 }, "Clear")
        )
      ),
      h("div", { className: "label-picker-row" },
        h("label", null, "Find parts for labels",
          h("input", { value: labelSearch, onChange: (event) => setLabelSearch(event.target.value), placeholder: "Search code, barcode, OEM, or name" })
        )
      ),
      h("div", { className: "label-part-chooser" },
        filteredLabelParts.map((part) => {
          const id = String(read(part, "id"));
          const active = labelPartIds.includes(id);
          return h("button", {
            key: id,
            className: active ? "label-part-chip active" : "label-part-chip",
            type: "button",
            onClick: () => addLabelPart(part)
          },
            h("strong", null, read(part, "internalCode") || labelCode(part)),
            h("span", null, read(part, "name") || "Part")
          );
        })
      ),
      h("section", { className: "label-sheet", "aria-label": "Printable part labels" },
        labelParts.map((part) => h("div", { className: "label-print-item", key: read(part, "id") },
          h("button", {
            className: "label-remove",
            type: "button",
            onClick: () => setLabelPartIds((current) => current.filter((id) => String(id) !== String(read(part, "id")))),
            "aria-label": `Remove ${partTitle(part)} label`
          }, "x"),
          h(LabelCard, { part })
        )),
        labelParts.length === 0 && h("p", { className: "empty-state" }, "Add parts to preview and print labels.")
      )
    )
  );
}
