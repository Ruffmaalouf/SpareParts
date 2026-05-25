import { React, h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { asRows, money, shortDate } from "../core/formatters.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";
import { genericColumns } from "./management-view.js";
import { PricingCoachCard, PricingCoachSignal, smartPricingCoach, waitingCustomersByPart } from "../services/pricing-coach.js";

const scanColumns = [
  { key: "type", label: "Type", render: (row) => row.targetType || "-" },
  { key: "code", label: "Code", render: (row) => row.code || "-" },
  { key: "result", label: "Result", render: (row) => row.displayText || `#${row.targetId || ""}`.trim() },
  { key: "detail", label: "Detail", render: (row) => row.secondaryText || row.apiRoute || "-" }
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

function reportCell(value) {
  if (value === null || value === undefined || value === "") return "-";
  if (typeof value === "boolean") return value ? "Yes" : "No";
  if (typeof value === "number") return Number.isInteger(value) ? String(value) : value.toLocaleString(undefined, { maximumFractionDigits: 2 });
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}T/.test(value)) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
  }
  return String(value);
}

function tableKey(table) {
  return String(read(table, "key") || "");
}

function tableTitle(table) {
  return read(table, "displayName") || tableKey(table) || "Table";
}

function columnKey(column) {
  return String(read(column, "key") || read(column, "qualifiedColumnName") || "");
}

function columnTitle(column) {
  return read(column, "qualifiedDisplayName", "displayName", "columnName") || "Column";
}

function todayInputValue() {
  return new Date().toISOString().slice(0, 10);
}

function toNumber(value) {
  const number = Number(value);
  return Number.isFinite(number) ? number : 0;
}

function toId(value) {
  const id = Number(value);
  return Number.isFinite(id) && id > 0 ? id : 0;
}

function selectFirstId(rows) {
  return String(read(rows[0], "id") || "");
}

function supplierName(row) {
  return read(row, "name", "supplierName", "fullName") || `Supplier #${read(row, "id")}`;
}

function warehouseName(row) {
  return read(row, "name", "warehouseName") || `Warehouse #${read(row, "id")}`;
}

function usedCarName(row) {
  return read(row, "usedCar", "car", "name", "displayName") || `Used car #${read(row, "id")}`;
}

function accountName(row) {
  return read(row, "displayName") || `${read(row, "code") || read(row, "id")} - ${read(row, "name") || "Account"}`;
}

function partName(row) {
  const code = read(row, "internalCode", "barcode");
  const name = read(row, "name") || `Part #${read(row, "id")}`;
  return code ? `${code} - ${name}` : name;
}

function partMatches(part, query) {
  if (!query) return true;
  const haystack = [
    read(part, "internalCode"),
    read(part, "barcode"),
    read(part, "name"),
    read(part, "oemNumber"),
    read(part, "notes")
  ].join(" ").toLowerCase();
  return haystack.includes(query.toLowerCase());
}

function totalPurchaseItems(items) {
  return items.reduce((sum, item) => sum + toNumber(item.quantity) * toNumber(item.unitCost) * (1 + toNumber(item.taxRate) / 100), 0);
}

function ModuleSummary({ module, t }) {
  return h(React.Fragment, null,
    h("section", { className: "module-summary" },
      h("div", null,
        h("span", null, t("module.wpfWorkspace", "WPF source")),
        h("strong", null, module.source)
      ),
      h("div", null,
        h("span", null, t("module.api", "API")),
        h("strong", null, module.endpoint)
      )
    ),
    h("div", { className: "module-grid" },
      module.capabilities.map((capability) =>
        h("article", { className: "module-card", key: capability },
          h("span", null, t("module.capability", "Capability")),
          h("strong", null, capability)
        )
      )
    )
  );
}

function ReportBuilderWorkspace({ api, module, t }) {
  const [tables, setTables] = useState([]);
  const [columns, setColumns] = useState([]);
  const [savedReports, setSavedReports] = useState([]);
  const [backgroundRuns, setBackgroundRuns] = useState([]);
  const [selectedTableKey, setSelectedTableKey] = useState("");
  const [selectedColumnKeys, setSelectedColumnKeys] = useState([]);
  const [maxRows, setMaxRows] = useState("100");
  const [result, setResult] = useState(null);
  const [status, setStatus] = useState(t("reports.ready", "Choose a table, select columns, then run a report."));
  const [isLoading, setIsLoading] = useState(false);

  const selectedTable = useMemo(
    () => tables.find((table) => tableKey(table) === selectedTableKey) || null,
    [selectedTableKey, tables]
  );

  const resultColumns = useMemo(() => {
    const reportColumns = asRows(read(result, "columns"));
    return reportColumns.map((column) => {
      const key = read(column, "columnName", "key");
      return {
        key,
        label: read(column, "displayName", "key") || key,
        render: (row) => reportCell(row?.[key])
      };
    });
  }, [result]);

  const loadOverview = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("reports.loading", "Loading report builder..."));
    try {
      const [nextTables, nextSavedReports, nextRuns] = await Promise.all([
        api.get("/api/reportbuilder/tables"),
        api.get("/api/reportbuilder/saved-reports"),
        api.get("/api/reportbuilder/background-runs")
      ]);
      const tableRows = asRows(nextTables);
      setTables(tableRows);
      setSavedReports(asRows(nextSavedReports));
      setBackgroundRuns(asRows(nextRuns));
      setSelectedTableKey((current) => current || tableKey(tableRows[0]) || "");
      setStatus(t("reports.loaded", "Report builder loaded."));
    } catch (error) {
      setTables([]);
      setColumns([]);
      setSavedReports([]);
      setBackgroundRuns([]);
      setStatus(error.message || t("reports.loadError", "Could not load report builder."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  const loadColumns = useCallback(async () => {
    if (!selectedTableKey) {
      setColumns([]);
      setSelectedColumnKeys([]);
      return;
    }

    try {
      const nextColumns = asRows(await api.get(`/api/reportbuilder/columns?tableKey=${encodeURIComponent(selectedTableKey)}`));
      setColumns(nextColumns);
      setSelectedColumnKeys((current) => {
        const available = new Set(nextColumns.map(columnKey));
        const retained = current.filter((key) => available.has(key));
        return retained.length ? retained : nextColumns.slice(0, 6).map(columnKey).filter(Boolean);
      });
    } catch (error) {
      setColumns([]);
      setSelectedColumnKeys([]);
      setStatus(error.message || t("reports.columnsError", "Could not load report columns."));
    }
  }, [api, selectedTableKey, t]);

  const runReport = useCallback(async (layout) => {
    const request = layout || {
      tableKey: selectedTableKey,
      columns: selectedColumnKeys,
      joins: [],
      groupByColumns: [],
      calculatedColumns: [],
      aggregates: [],
      filters: [],
      maxRows: Math.max(1, Math.min(5000, Number(maxRows) || 100)),
      includeSqlPreview: false,
      currencySettings: {}
    };

    if (!request.tableKey) {
      setStatus(t("reports.tableRequired", "Choose a table before running the report."));
      return;
    }

    setIsLoading(true);
    setStatus(t("reports.running", "Running report..."));
    try {
      const nextResult = await api.post("/api/reportbuilder/run", request);
      setResult(nextResult);
      setStatus(t("reports.ran", "{count} row(s) returned.", { count: read(nextResult, "rowCount") || asRows(read(nextResult, "rows")).length }));
    } catch (error) {
      setResult(null);
      setStatus(error.message || t("reports.runError", "Could not run report."));
    } finally {
      setIsLoading(false);
    }
  }, [api, maxRows, selectedColumnKeys, selectedTableKey, t]);

  const runSavedReport = useCallback(async (report) => {
    setIsLoading(true);
    setStatus(t("reports.loadingSaved", "Loading saved report..."));
    try {
      const detail = await api.get(`/api/reportbuilder/saved-reports/${read(report, "id")}`);
      const layout = read(detail, "layout") || {};
      setSelectedTableKey(read(layout, "tableKey") || selectedTableKey);
      setSelectedColumnKeys(asRows(read(layout, "columns")).map(String));
      await runReport({
        ...layout,
        tableKey: read(layout, "tableKey") || selectedTableKey,
        maxRows: read(layout, "maxRows") || 500,
        includeSqlPreview: false
      });
    } catch (error) {
      setStatus(error.message || t("reports.savedError", "Could not open saved report."));
    } finally {
      setIsLoading(false);
    }
  }, [api, runReport, selectedTableKey, t]);

  const toggleColumn = useCallback((key) => {
    setSelectedColumnKeys((current) =>
      current.includes(key)
        ? current.filter((item) => item !== key)
        : [...current, key]
    );
  }, []);

  useEffect(() => { loadOverview(); }, [loadOverview]);
  useEffect(() => { loadColumns(); }, [loadColumns]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: module.title,
      action: h("button", { className: "secondary-button", type: "button", onClick: loadOverview, disabled: isLoading }, t("common.refresh", "Refresh"))
    }),
    h(ModuleSummary, { module, t }),
    h("section", { className: "report-controls" },
      h("label", null,
        t("reports.table", "Table"),
        h("select", { value: selectedTableKey, onChange: (event) => setSelectedTableKey(event.target.value) },
          tables.map((table) => h("option", { key: tableKey(table), value: tableKey(table) }, tableTitle(table)))
        )
      ),
      h("label", null,
        t("reports.maxRows", "Max rows"),
        h("input", { value: maxRows, onChange: (event) => setMaxRows(event.target.value), inputMode: "numeric" })
      ),
      h("label", null,
        t("reports.selected", "Selected"),
        h("input", { value: `${selectedColumnKeys.length || "All"} column(s)`, readOnly: true })
      ),
      h("div", { className: "report-buttons" },
        h("button", { className: "secondary-button", type: "button", onClick: loadColumns, disabled: isLoading || !selectedTableKey }, t("common.load", "Load")),
        h("button", { className: "primary-button", type: "button", onClick: () => runReport(), disabled: isLoading || !selectedTableKey }, t("reports.run", "Run"))
      )
    ),
    h("section", { className: "two-column" },
      h("section", { className: "selector-panel" },
        h("h3", null, t("reports.schema", "Schema")),
        h("div", { className: "chip-scroll" },
          tables.map((table) => {
            const key = tableKey(table);
            return h("button", {
              key,
              className: key === selectedTableKey ? "selector-chip active" : "selector-chip",
              type: "button",
              onClick: () => setSelectedTableKey(key)
            },
              h("strong", null, tableTitle(table)),
              h("span", null, `${read(table, "schemaName") || ""}.${read(table, "tableName") || ""}`),
              h("span", null, t("reports.columnCount", "{count} columns", { count: read(table, "columnCount") || 0 }))
            );
          }),
          tables.length === 0 && h("p", { className: "empty-state" }, t("reports.noTables", "No reportable tables found."))
        )
      ),
      h("section", { className: "selector-panel" },
        h("h3", null, selectedTable ? tableTitle(selectedTable) : t("reports.columns", "Columns")),
        h("div", { className: "chip-scroll" },
          columns.map((column) => {
            const key = columnKey(column);
            return h("button", {
              key,
              className: selectedColumnKeys.includes(key) ? "selector-chip active" : "selector-chip",
              type: "button",
              onClick: () => toggleColumn(key)
            },
              h("strong", null, columnTitle(column)),
              h("span", null, read(column, "sqlDataType", "dataType") || "-"),
              h("span", null, selectedColumnKeys.includes(key) ? t("reports.included", "Included") : t("reports.tapToInclude", "Tap to include"))
            );
          }),
          columns.length === 0 && h("p", { className: "empty-state" }, t("reports.noColumns", "No columns loaded yet."))
        )
      )
    ),
    h(StatusLine, { status }),
    h("section", { className: "two-column" },
      h("section", { className: "table-panel compact-table-panel" },
        h(DataTable, {
          columns: [
            { key: "name", label: t("reports.savedReport", "Saved report"), render: (row) => read(row, "name") || `#${read(row, "id")}` },
            { key: "table", label: t("reports.table", "Table"), render: (row) => read(row, "tableDisplayName", "tableKey") || "-" },
            { key: "run", label: "", render: (row) => h("button", { className: "secondary-button", type: "button", onClick: () => runSavedReport(row), disabled: isLoading }, t("reports.run", "Run")) }
          ],
          rows: savedReports,
          getRowKey: (row, index) => `saved-${read(row, "id") || index}`,
          emptyText: t("reports.noSavedReports", "No saved reports yet.")
        })
      ),
      h("section", { className: "table-panel compact-table-panel" },
        h(DataTable, {
          columns: [
            { key: "name", label: t("reports.backgroundRun", "Background run"), render: (row) => read(row, "reportName") || `#${read(row, "id")}` },
            { key: "status", label: t("reports.status", "Status"), render: (row) => read(row, "status") || "-" },
            { key: "rows", label: t("reports.rows", "Rows"), render: (row) => reportCell(read(row, "rowCount")) }
          ],
          rows: backgroundRuns,
          getRowKey: (row, index) => `run-${read(row, "id") || index}`,
          emptyText: t("reports.noRuns", "No background runs yet.")
        })
      )
    ),
    h("section", { className: "selector-panel" },
      h("h3", null, t("reports.results", "Results")),
      h("section", { className: "table-panel report-table-panel" },
        h(DataTable, {
          columns: resultColumns,
          rows: asRows(read(result, "rows")),
          getRowKey: (row, index) => `result-${index}`,
          emptyText: t("reports.noResultRows", "Run a report to see results.")
        })
      )
    )
  );
}

function PartPurchasesWorkspace({ api, module, t }) {
  const [purchases, setPurchases] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [parts, setParts] = useState([]);
  const [search, setSearch] = useState("");
  const [form, setForm] = useState({
    purchaseDate: todayInputValue(),
    supplierId: "",
    warehouseId: "",
    paidAmount: "0",
    paymentStatus: "Unpaid"
  });
  const [line, setLine] = useState({ partId: "", quantity: "1", unitCost: "0", taxRate: "0" });
  const [items, setItems] = useState([]);
  const [status, setStatus] = useState("Load suppliers, warehouse, and parts to create a purchase.");
  const [isLoading, setIsLoading] = useState(false);

  const filteredParts = useMemo(
    () => parts.filter((part) => partMatches(part, search)).slice(0, 160),
    [parts, search]
  );

  const draftTotal = useMemo(() => totalPurchaseItems(items), [items]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading purchase workspace...");
    try {
      const [nextPurchases, nextSuppliers, nextWarehouses, nextParts] = await Promise.all([
        api.get("/api/purchases"),
        api.get("/api/suppliers?pageSize=500"),
        api.get("/api/warehouses"),
        api.get("/api/parts?pageSize=500")
      ]);
      const supplierRows = asRows(nextSuppliers);
      const warehouseRows = asRows(nextWarehouses);
      const partRows = asRows(nextParts);
      setPurchases(asRows(nextPurchases));
      setSuppliers(supplierRows);
      setWarehouses(warehouseRows);
      setParts(partRows);
      setForm((current) => ({
        ...current,
        supplierId: current.supplierId || selectFirstId(supplierRows),
        warehouseId: current.warehouseId || selectFirstId(warehouseRows)
      }));
      setLine((current) => ({
        ...current,
        partId: current.partId || selectFirstId(partRows),
        unitCost: current.unitCost !== "0" ? current.unitCost : String(read(partRows[0], "costPrice") || 0)
      }));
      setStatus("Part purchases ready.");
    } catch (error) {
      setPurchases([]);
      setStatus(error.message || "Could not load part purchases.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  const addLine = useCallback(() => {
    const partId = toId(line.partId);
    const part = parts.find((item) => Number(read(item, "id")) === partId);
    if (!partId || !part) {
      setStatus("Choose a part before adding a line.");
      return;
    }

    const quantity = Math.max(1, toNumber(line.quantity));
    const unitCost = Math.max(0, toNumber(line.unitCost));
    const taxRate = Math.max(0, toNumber(line.taxRate));
    setItems((current) => [
      ...current,
      {
        key: `${partId}-${Date.now()}`,
        partId,
        partLabel: partName(part),
        quantity,
        unitCost,
        taxRate
      }
    ]);
    setLine((current) => ({ ...current, quantity: "1", unitCost: String(read(part, "costPrice") || current.unitCost || 0) }));
    setStatus("Line added to draft purchase.");
  }, [line, parts]);

  const createPurchase = useCallback(async () => {
    if (!toId(form.supplierId) || !toId(form.warehouseId)) {
      setStatus("Choose supplier and warehouse before saving.");
      return;
    }
    if (items.length === 0) {
      setStatus("Add at least one part line before saving.");
      return;
    }

    setIsLoading(true);
    setStatus("Saving purchase invoice...");
    try {
      const result = await api.post("/api/purchases", {
        purchaseDate: form.purchaseDate,
        supplierId: toId(form.supplierId),
        warehouseId: toId(form.warehouseId),
        paidAmount: Math.max(0, toNumber(form.paidAmount)),
        paymentStatus: form.paymentStatus,
        items: items.map((item) => ({
          partId: item.partId,
          quantity: Math.max(1, toNumber(item.quantity)),
          unitCost: Math.max(0, toNumber(item.unitCost)),
          taxRate: Math.max(0, toNumber(item.taxRate))
        }))
      });
      setItems([]);
      setStatus(`Purchase saved${read(result, "purchaseNumber") ? `: ${read(result, "purchaseNumber")}` : "."}`);
      await load();
    } catch (error) {
      setStatus(error.message || "Could not save purchase invoice.");
    } finally {
      setIsLoading(false);
    }
  }, [api, form, items, load]);

  useEffect(() => { load(); }, [load]);

  return h("section", { className: "screen operations-workspace" },
    h(PageHeader, {
      title: module.title,
      subtitle: "Create part purchase invoices and receive stock in one flow.",
      action: h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, t("common.refresh", "Refresh"))
    }),
    h(ModuleSummary, { module, t }),
    h(StatusLine, { status }),
    h("section", { className: "two-column" },
      h("article", { className: "invoice-create-panel" },
        h("div", { className: "invoice-create-header" },
          h("div", null,
            h("h3", null, "New part purchase"),
            h("span", null, `${items.length} line(s) - ${money(draftTotal, "USD")}`)
          ),
          h("button", { className: "primary-button", type: "button", onClick: createPurchase, disabled: isLoading }, "Save purchase")
        ),
        h("div", { className: "checkout-grid two" },
          h("label", null, "Supplier",
            h("select", { value: form.supplierId, onChange: (event) => setForm((current) => ({ ...current, supplierId: event.target.value })) },
              suppliers.map((supplier) => h("option", { key: read(supplier, "id"), value: read(supplier, "id") }, supplierName(supplier)))
            )
          ),
          h("label", null, "Warehouse",
            h("select", { value: form.warehouseId, onChange: (event) => setForm((current) => ({ ...current, warehouseId: event.target.value })) },
              warehouses.map((warehouse) => h("option", { key: read(warehouse, "id"), value: read(warehouse, "id") }, warehouseName(warehouse)))
            )
          ),
          h("label", null, "Purchase date",
            h("input", { type: "date", value: form.purchaseDate, onChange: (event) => setForm((current) => ({ ...current, purchaseDate: event.target.value })) })
          ),
          h("label", null, "Paid amount",
            h("input", { value: form.paidAmount, inputMode: "decimal", onChange: (event) => setForm((current) => ({ ...current, paidAmount: event.target.value })) })
          )
        ),
        h("div", { className: "invoice-line-builder" },
          h("label", null, "Find part",
            h("input", { value: search, onChange: (event) => setSearch(event.target.value), placeholder: "Code, OEM, barcode, name" })
          ),
          h("label", null, "Part",
            h("select", {
              value: line.partId,
              onChange: (event) => {
                const part = parts.find((item) => String(read(item, "id")) === event.target.value);
                setLine((current) => ({ ...current, partId: event.target.value, unitCost: String(read(part, "costPrice") || current.unitCost || 0) }));
              }
            },
              filteredParts.map((part) => h("option", { key: read(part, "id"), value: read(part, "id") }, partName(part)))
            )
          ),
          h("label", null, "Qty",
            h("input", { value: line.quantity, inputMode: "numeric", onChange: (event) => setLine((current) => ({ ...current, quantity: event.target.value })) })
          ),
          h("label", null, "Unit cost",
            h("input", { value: line.unitCost, inputMode: "decimal", onChange: (event) => setLine((current) => ({ ...current, unitCost: event.target.value })) })
          ),
          h("button", { className: "secondary-button", type: "button", onClick: addLine }, "Add line")
        ),
        h(DataTable, {
          columns: [
            { key: "part", label: "Part", render: (row) => row.partLabel },
            { key: "qty", label: "Qty", render: (row) => row.quantity },
            { key: "cost", label: "Unit", render: (row) => money(row.unitCost, "USD") },
            { key: "total", label: "Total", render: (row) => money(toNumber(row.quantity) * toNumber(row.unitCost), "USD") },
            { key: "remove", label: "", render: (row) => h("button", { className: "ghost-button", type: "button", onClick: () => setItems((current) => current.filter((item) => item.key !== row.key)) }, "Remove") }
          ],
          rows: items,
          getRowKey: (row) => row.key,
          emptyText: "No draft purchase lines yet."
        })
      ),
      h("article", { className: "table-panel" },
        h(DataTable, {
          columns: [
            { key: "number", label: "Purchase", render: (row) => read(row, "purchaseNumber") || `#${read(row, "purchaseId")}` },
            { key: "date", label: "Date", render: (row) => shortDate(read(row, "purchaseDate")) },
            { key: "supplier", label: "Supplier", render: (row) => read(row, "supplierName") || `#${read(row, "supplierId")}` },
            { key: "total", label: "Total", render: (row) => money(read(row, "totalAmount"), "USD") },
            { key: "paid", label: "Paid", render: (row) => money(read(row, "paidAmount"), "USD") }
          ],
          rows: purchases,
          getRowKey: (row, index) => `purchase-${read(row, "purchaseId") || index}`,
          emptyText: "No part purchases found."
        })
      )
    )
  );
}

function UsedCarPurchasesWorkspace({ api, module, t }) {
  const [purchases, setPurchases] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [usedCars, setUsedCars] = useState([]);
  const [accounts, setAccounts] = useState([]);
  const [form, setForm] = useState({
    usedCarId: "",
    supplierId: "",
    purchaseDate: todayInputValue(),
    paidAmount: "0",
    paidCounterAmount: "0",
    baseCurrencyCode: "USD",
    counterCurrencyCode: "USD",
    notes: ""
  });
  const [line, setLine] = useState({
    detailKey: "vehicle-cost",
    description: "Vehicle purchase cost",
    amount: "0",
    currencyCode: "USD",
    rateToBase: "1",
    counterAmount: "0",
    accountId: ""
  });
  const [status, setStatus] = useState("Create used-car purchases, then post the draft when ready.");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading used-car purchase workspace...");
    try {
      const [nextPurchases, nextSuppliers, nextUsedCars, nextAccounts] = await Promise.all([
        api.get("/api/purchases/used-cars"),
        api.get("/api/suppliers?pageSize=500"),
        api.get("/api/usedcars"),
        api.get("/api/accounting/accounts")
      ]);
      const supplierRows = asRows(nextSuppliers);
      const usedCarRows = asRows(nextUsedCars);
      const accountRows = asRows(nextAccounts);
      setPurchases(asRows(nextPurchases));
      setSuppliers(supplierRows);
      setUsedCars(usedCarRows);
      setAccounts(accountRows);
      setForm((current) => ({
        ...current,
        supplierId: current.supplierId || selectFirstId(supplierRows),
        usedCarId: current.usedCarId || selectFirstId(usedCarRows)
      }));
      setLine((current) => ({
        ...current,
        accountId: current.accountId || selectFirstId(accountRows)
      }));
      setStatus("Used-car purchases ready.");
    } catch (error) {
      setPurchases([]);
      setStatus(error.message || "Could not load used-car purchases.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  const createPurchase = useCallback(async () => {
    if (!toId(form.usedCarId) || !toId(form.supplierId) || !toId(line.accountId)) {
      setStatus("Choose used car, supplier, and account before saving.");
      return;
    }

    const amount = Math.max(0, toNumber(line.amount));
    if (amount <= 0) {
      setStatus("Enter a positive purchase amount.");
      return;
    }

    const rateToBase = Math.max(0, toNumber(line.rateToBase) || 1);
    const baseAmount = amount * rateToBase;
    const counterAmount = Math.max(0, toNumber(line.counterAmount) || baseAmount);

    setIsLoading(true);
    setStatus("Saving used-car purchase...");
    try {
      const result = await api.post("/api/purchases/used-cars", {
        usedCarId: toId(form.usedCarId),
        supplierId: toId(form.supplierId),
        purchaseDate: form.purchaseDate,
        paidAmount: Math.max(0, toNumber(form.paidAmount)),
        baseCurrencyCode: form.baseCurrencyCode || "USD",
        paidCounterAmount: Math.max(0, toNumber(form.paidCounterAmount)),
        counterCurrencyCode: form.counterCurrencyCode || "USD",
        notes: form.notes,
        lines: [{
          detailKey: line.detailKey || "vehicle-cost",
          description: line.description || "Vehicle purchase cost",
          amount,
          currencyCode: line.currencyCode || form.baseCurrencyCode || "USD",
          rateToBase,
          baseAmount,
          counterAmount,
          accountId: toId(line.accountId),
          sortOrder: 1
        }]
      });
      setStatus(`Used-car purchase saved${read(result, "purchaseNumber") ? `: ${read(result, "purchaseNumber")}` : "."}`);
      await load();
    } catch (error) {
      setStatus(error.message || "Could not save used-car purchase.");
    } finally {
      setIsLoading(false);
    }
  }, [api, form, line, load]);

  const postPurchase = useCallback(async (row) => {
    const id = read(row, "id");
    if (!id) return;
    setIsLoading(true);
    setStatus(`Posting ${read(row, "purchaseNumber") || `#${id}`}...`);
    try {
      await api.post(`/api/purchases/used-cars/${id}/post`, {});
      setStatus("Used-car purchase posted.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not post used-car purchase.");
    } finally {
      setIsLoading(false);
    }
  }, [api, load]);

  const deletePurchase = useCallback(async (row) => {
    const id = read(row, "id");
    if (!id) return;
    setIsLoading(true);
    setStatus(`Deleting ${read(row, "purchaseNumber") || `#${id}`}...`);
    try {
      await api.delete(`/api/purchases/used-cars/${id}`);
      setStatus("Used-car purchase deleted.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not delete used-car purchase.");
    } finally {
      setIsLoading(false);
    }
  }, [api, load]);

  useEffect(() => { load(); }, [load]);

  return h("section", { className: "screen operations-workspace" },
    h(PageHeader, {
      title: module.title,
      subtitle: "Create vehicle purchase drafts, assign cost accounts, and post to accounting.",
      action: h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, t("common.refresh", "Refresh"))
    }),
    h(ModuleSummary, { module, t }),
    h(StatusLine, { status }),
    h("section", { className: "two-column" },
      h("article", { className: "invoice-create-panel" },
        h("div", { className: "invoice-create-header" },
          h("div", null,
            h("h3", null, "New used-car purchase"),
            h("span", null, "One vehicle cost line is enough to create the draft.")
          ),
          h("button", { className: "primary-button", type: "button", onClick: createPurchase, disabled: isLoading }, "Save draft")
        ),
        h("div", { className: "checkout-grid two" },
          h("label", null, "Used car",
            h("select", { value: form.usedCarId, onChange: (event) => setForm((current) => ({ ...current, usedCarId: event.target.value })) },
              usedCars.map((car) => h("option", { key: read(car, "id"), value: read(car, "id") }, usedCarName(car)))
            )
          ),
          h("label", null, "Supplier",
            h("select", { value: form.supplierId, onChange: (event) => setForm((current) => ({ ...current, supplierId: event.target.value })) },
              suppliers.map((supplier) => h("option", { key: read(supplier, "id"), value: read(supplier, "id") }, supplierName(supplier)))
            )
          ),
          h("label", null, "Purchase date",
            h("input", { type: "date", value: form.purchaseDate, onChange: (event) => setForm((current) => ({ ...current, purchaseDate: event.target.value })) })
          ),
          h("label", null, "Paid amount",
            h("input", { value: form.paidAmount, inputMode: "decimal", onChange: (event) => setForm((current) => ({ ...current, paidAmount: event.target.value })) })
          ),
          h("label", null, "Base currency",
            h("input", { value: form.baseCurrencyCode, maxLength: 3, onChange: (event) => setForm((current) => ({ ...current, baseCurrencyCode: event.target.value.toUpperCase() })) })
          ),
          h("label", null, "Counter currency",
            h("input", { value: form.counterCurrencyCode, maxLength: 3, onChange: (event) => setForm((current) => ({ ...current, counterCurrencyCode: event.target.value.toUpperCase() })) })
          )
        ),
        h("div", { className: "invoice-line-builder" },
          h("label", null, "Cost detail",
            h("input", { value: line.description, onChange: (event) => setLine((current) => ({ ...current, description: event.target.value })) })
          ),
          h("label", null, "Amount",
            h("input", { value: line.amount, inputMode: "decimal", onChange: (event) => setLine((current) => ({ ...current, amount: event.target.value })) })
          ),
          h("label", null, "Rate",
            h("input", { value: line.rateToBase, inputMode: "decimal", onChange: (event) => setLine((current) => ({ ...current, rateToBase: event.target.value })) })
          ),
          h("label", null, "Account",
            h("select", { value: line.accountId, onChange: (event) => setLine((current) => ({ ...current, accountId: event.target.value })) },
              accounts.map((account) => h("option", { key: read(account, "id"), value: read(account, "id") }, accountName(account)))
            )
          )
        ),
        h("label", null, "Notes",
          h("textarea", { rows: 3, value: form.notes, onChange: (event) => setForm((current) => ({ ...current, notes: event.target.value })) })
        )
      ),
      h("article", { className: "table-panel" },
        h(DataTable, {
          columns: [
            { key: "number", label: "Purchase", render: (row) => read(row, "purchaseNumber") || `#${read(row, "id")}` },
            { key: "car", label: "Car", render: (row) => read(row, "usedCar") || `#${read(row, "usedCarId")}` },
            { key: "supplier", label: "Supplier", render: (row) => read(row, "supplierName") || `#${read(row, "supplierId")}` },
            { key: "total", label: "Total", render: (row) => money(read(row, "totalBaseAmount"), read(row, "baseCurrencyCode") || "USD") },
            { key: "status", label: "Status", render: (row) => read(row, "postingStatus") || read(row, "paymentStatus") || "-" },
            { key: "actions", label: "", render: (row) => h("div", { className: "row-actions" },
              h("button", { className: "secondary-button", type: "button", onClick: () => postPurchase(row), disabled: isLoading || read(row, "postingStatus") === "Posted" }, "Post"),
              h("button", { className: "ghost-button", type: "button", onClick: () => deletePurchase(row), disabled: isLoading || read(row, "postingStatus") === "Posted" }, "Delete")
            ) }
          ],
          rows: purchases,
          getRowKey: (row, index) => `used-car-purchase-${read(row, "id") || index}`,
          emptyText: "No used-car purchases found."
        })
      )
    )
  );
}

function StockWorkspace({ api, module, t }) {
  const [parts, setParts] = useState([]);
  const [partRequests, setPartRequests] = useState([]);
  const [usedCars, setUsedCars] = useState([]);
  const [query, setQuery] = useState("");
  const [usedCarFilter, setUsedCarFilter] = useState("");
  const [selectedPartId, setSelectedPartId] = useState("");
  const [assignUsedCarId, setAssignUsedCarId] = useState("");
  const [notesResult, setNotesResult] = useState(null);
  const [status, setStatus] = useState("Review live stock and assign parts to used cars.");
  const [isLoading, setIsLoading] = useState(false);

  const filteredParts = useMemo(
    () => parts.filter((part) => partMatches(part, query)),
    [parts, query]
  );

  const selectedPart = useMemo(
    () => parts.find((part) => String(read(part, "id")) === String(selectedPartId)) || filteredParts[0] || null,
    [filteredParts, parts, selectedPartId]
  );

  const lowStockCount = useMemo(
    () => parts.filter((part) => toNumber(read(part, "availableQuantity", "stockQuantity")) <= toNumber(read(part, "minStock"))).length,
    [parts]
  );

  const waitingByPart = useMemo(
    () => waitingCustomersByPart(partRequests),
    [partRequests]
  );

  const selectedCoach = useMemo(
    () => selectedPart ? smartPricingCoach(selectedPart, waitingByPart.get(String(read(selectedPart, "id"))) || 0) : null,
    [selectedPart, waitingByPart]
  );

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading stock...");
    try {
      const partPath = `/api/parts?pageSize=500${usedCarFilter ? `&usedCarId=${encodeURIComponent(usedCarFilter)}` : ""}`;
      const [nextParts, nextUsedCars, nextRequests] = await Promise.all([
        api.get(partPath),
        api.get("/api/usedcars"),
        api.get("/api/partrequests?status=Active").catch(() => [])
      ]);
      const partRows = asRows(nextParts);
      const usedCarRows = asRows(nextUsedCars);
      setParts(partRows);
      setUsedCars(usedCarRows);
      setPartRequests(asRows(nextRequests));
      setSelectedPartId((current) => current || selectFirstId(partRows));
      setAssignUsedCarId((current) => current || "");
      const nextLowStockCount = partRows.filter((part) => toNumber(read(part, "availableQuantity", "stockQuantity")) <= toNumber(read(part, "minStock"))).length;
      setStatus(`Stock loaded. ${partRows.length} part(s), ${nextLowStockCount} low-stock alert(s).`);
    } catch (error) {
      setParts([]);
      setPartRequests([]);
      setStatus(error.message || "Could not load stock.");
    } finally {
      setIsLoading(false);
    }
  }, [api, usedCarFilter]);

  const assignUsedCar = useCallback(async () => {
    const id = toId(selectedPartId || read(selectedPart, "id"));
    if (!id) {
      setStatus("Choose a part before assigning.");
      return;
    }

    setIsLoading(true);
    setStatus("Updating used-car assignment...");
    try {
      await api.put(`/api/parts/${id}/usedcar`, {
        usedCarId: assignUsedCarId ? toId(assignUsedCarId) : null
      });
      setStatus(assignUsedCarId ? "Part assigned to used car and stock ensured." : "Part unassigned from used car.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not update used-car assignment.");
    } finally {
      setIsLoading(false);
    }
  }, [api, assignUsedCarId, load, selectedPart, selectedPartId]);

  const generateNotes = useCallback(async () => {
    if (!selectedPart) {
      setStatus("Choose a part before generating notes.");
      return;
    }

    setIsLoading(true);
    setStatus("Generating part notes...");
    try {
      const result = await api.post("/api/parts/ai/notes", {
        internalCode: read(selectedPart, "internalCode"),
        name: read(selectedPart, "name"),
        oemNumber: read(selectedPart, "oemNumber") || null,
        categoryId: toId(read(selectedPart, "categoryId")),
        brandId: read(selectedPart, "brandId") ? toId(read(selectedPart, "brandId")) : null,
        costPrice: toNumber(read(selectedPart, "costPrice")),
        salePrice: toNumber(read(selectedPart, "salePrice")),
        currency: read(selectedPart, "currency") || "USD",
        minStock: toNumber(read(selectedPart, "minStock")),
        existingNotes: read(selectedPart, "notes") || null
      });
      setNotesResult(result);
      setStatus("Suggested notes generated.");
    } catch (error) {
      setNotesResult(null);
      setStatus(error.message || "Could not generate part notes.");
    } finally {
      setIsLoading(false);
    }
  }, [api, selectedPart]);

  useEffect(() => { load(); }, [load]);

  return h("section", { className: "screen operations-workspace" },
    h(PageHeader, {
      title: module.title,
      subtitle: "Live stock quantities, used-car assignment, and low-stock visibility.",
      action: h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, t("common.refresh", "Refresh"))
    }),
    h(ModuleSummary, { module, t }),
    h("section", { className: "module-summary" },
      h("div", null, h("span", null, "Parts"), h("strong", null, parts.length)),
      h("div", null, h("span", null, "Low stock"), h("strong", null, lowStockCount)),
      h("div", null, h("span", null, "Used-car linked"), h("strong", null, parts.filter((part) => read(part, "usedCarId")).length))
    ),
    h(StatusLine, { status }),
    h("section", { className: "invoice-create-panel stock-control-panel" },
      h("div", { className: "invoice-line-builder" },
        h("label", null, "Search stock",
          h("input", { value: query, onChange: (event) => setQuery(event.target.value), placeholder: "Code, barcode, OEM, name" })
        ),
        h("label", null, "Used-car filter",
          h("select", { value: usedCarFilter, onChange: (event) => setUsedCarFilter(event.target.value) },
            h("option", { value: "" }, "All stock"),
            usedCars.map((car) => h("option", { key: read(car, "id"), value: read(car, "id") }, usedCarName(car)))
          )
        ),
        h("label", null, "Assign selected to",
          h("select", { value: assignUsedCarId, onChange: (event) => setAssignUsedCarId(event.target.value) },
            h("option", { value: "" }, "No used car"),
            usedCars.map((car) => h("option", { key: read(car, "id"), value: read(car, "id") }, usedCarName(car)))
          )
        ),
        h("button", { className: "primary-button", type: "button", onClick: assignUsedCar, disabled: isLoading || !selectedPart }, "Assign"),
        h("button", { className: "secondary-button", type: "button", onClick: generateNotes, disabled: isLoading || !selectedPart }, "AI notes")
      ),
      selectedPart && h("div", { className: "stock-detail-strip" },
        h("strong", null, partName(selectedPart)),
        h("span", null, `Available ${read(selectedPart, "availableQuantity", "stockQuantity") || 0}`),
        h("span", null, `Reserved ${read(selectedPart, "reservedQuantity") || 0}`),
        h("span", null, `Min ${read(selectedPart, "minStock") || 0}`),
        h("span", null, read(selectedPart, "usedCarId") ? `Used car #${read(selectedPart, "usedCarId")}` : "Shelf stock")
      ),
      selectedCoach && h(PricingCoachCard, { h, coach: selectedCoach }),
      notesResult && h("article", { className: "selector-panel" },
        h("h3", null, "Suggested notes"),
        h("p", null, read(notesResult, "suggestedNotes") || "No notes returned."),
        read(notesResult, "suggestedAveragePrice") && h("strong", null, `Suggested average: ${money(read(notesResult, "suggestedAveragePrice"), read(selectedPart, "currency") || "USD")}`)
      )
    ),
    h("section", { className: "table-panel" },
      h(DataTable, {
        columns: [
          { key: "part", label: "Part", render: (row) => h("button", { className: "link-button", type: "button", onClick: () => { setSelectedPartId(String(read(row, "id"))); setAssignUsedCarId(read(row, "usedCarId") ? String(read(row, "usedCarId")) : ""); } }, partName(row)) },
          { key: "oem", label: "OEM", render: (row) => read(row, "oemNumber") || "-" },
          { key: "available", label: "Available", render: (row) => read(row, "availableQuantity", "stockQuantity") || 0 },
          { key: "reserved", label: "Reserved", render: (row) => read(row, "reservedQuantity") || 0 },
          { key: "min", label: "Min", render: (row) => read(row, "minStock") || 0 },
          { key: "price", label: "Sale", render: (row) => money(read(row, "salePrice"), read(row, "currency") || "USD") },
          {
            key: "coach",
            label: "Coach",
            render: (row) => h(PricingCoachSignal, {
              h,
              coach: smartPricingCoach(row, waitingByPart.get(String(read(row, "id"))) || 0)
            })
          },
          { key: "car", label: "Used car", render: (row) => read(row, "usedCarId") ? `#${read(row, "usedCarId")}` : "Shelf" }
        ],
        rows: filteredParts,
        getRowKey: (row, index) => `stock-${read(row, "id") || index}`,
        emptyText: "No parts match this stock filter."
      })
    )
  );
}

function ManualJournalWorkspace({ api, module, t }) {
  const [accounts, setAccounts] = useState([]);
  const [entries, setEntries] = useState([]);
  const [form, setForm] = useState({
    entryDate: todayInputValue(),
    description: "",
    referenceType: "Manual",
    referenceId: ""
  });
  const [lines, setLines] = useState([
    { key: "debit", accountId: "", debit: "0", credit: "0" },
    { key: "credit", accountId: "", debit: "0", credit: "0" }
  ]);
  const [status, setStatus] = useState("Create a balanced journal entry with debit and credit lines.");
  const [isLoading, setIsLoading] = useState(false);

  const totalDebit = useMemo(() => lines.reduce((sum, line) => sum + toNumber(line.debit), 0), [lines]);
  const totalCredit = useMemo(() => lines.reduce((sum, line) => sum + toNumber(line.credit), 0), [lines]);
  const difference = totalDebit - totalCredit;

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading manual journal...");
    try {
      const [nextEntries, nextAccounts] = await Promise.all([
        api.get("/api/accounting/journal-entries"),
        api.get("/api/accounting/accounts")
      ]);
      const accountRows = asRows(nextAccounts);
      setEntries(asRows(nextEntries));
      setAccounts(accountRows);
      setLines((current) => current.map((line, index) => ({
        ...line,
        accountId: line.accountId || String(read(accountRows[index], "id") || read(accountRows[0], "id") || "")
      })));
      setStatus("Manual journal ready.");
    } catch (error) {
      setEntries([]);
      setStatus(error.message || "Could not load manual journal.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  const updateLine = useCallback((key, patch) => {
    setLines((current) => current.map((line) => line.key === key ? { ...line, ...patch } : line));
  }, []);

  const addLine = useCallback((side) => {
    setLines((current) => [
      ...current,
      {
        key: `${side}-${Date.now()}`,
        accountId: selectFirstId(accounts),
        debit: side === "debit" ? "0" : "0",
        credit: side === "credit" ? "0" : "0"
      }
    ]);
  }, [accounts]);

  const createJournal = useCallback(async () => {
    const cleanLines = lines
      .map((line) => ({
        accountId: toId(line.accountId),
        debit: Math.max(0, toNumber(line.debit)),
        credit: Math.max(0, toNumber(line.credit))
      }))
      .filter((line) => line.accountId && (line.debit > 0 || line.credit > 0));

    if (!form.description.trim()) {
      setStatus("Enter a journal description.");
      return;
    }

    if (cleanLines.length < 2) {
      setStatus("Add at least two journal lines.");
      return;
    }

    const debit = cleanLines.reduce((sum, line) => sum + line.debit, 0);
    const credit = cleanLines.reduce((sum, line) => sum + line.credit, 0);
    if (debit <= 0 || Math.abs(debit - credit) > 0.005) {
      setStatus("Journal must be balanced before posting.");
      return;
    }

    setIsLoading(true);
    setStatus("Posting manual journal...");
    try {
      const id = await api.post("/api/accounting/journal-entries/manual", {
        entryDate: form.entryDate,
        description: form.description,
        referenceType: form.referenceType || "Manual",
        referenceId: form.referenceId ? toId(form.referenceId) : null,
        lines: cleanLines
      });
      setStatus(`Manual journal posted: #${id}.`);
      setForm((current) => ({ ...current, description: "", referenceId: "" }));
      setLines((current) => current.map((line) => ({ ...line, debit: "0", credit: "0" })));
      await load();
    } catch (error) {
      setStatus(error.message || "Could not post manual journal.");
    } finally {
      setIsLoading(false);
    }
  }, [api, form, lines, load]);

  useEffect(() => { load(); }, [load]);

  return h("section", { className: "screen operations-workspace" },
    h(PageHeader, {
      title: module.title,
      subtitle: "Post balanced accounting journals directly from the web admin.",
      action: h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, t("common.refresh", "Refresh"))
    }),
    h(ModuleSummary, { module, t }),
    h("section", { className: "module-summary" },
      h("div", null, h("span", null, "Debit"), h("strong", null, money(totalDebit, "USD"))),
      h("div", null, h("span", null, "Credit"), h("strong", null, money(totalCredit, "USD"))),
      h("div", null, h("span", null, "Difference"), h("strong", null, money(difference, "USD")))
    ),
    h(StatusLine, { status, tone: Math.abs(difference) > 0.005 ? "warning" : "muted" }),
    h("section", { className: "two-column" },
      h("article", { className: "invoice-create-panel" },
        h("div", { className: "invoice-create-header" },
          h("div", null,
            h("h3", null, "New manual journal"),
            h("span", null, Math.abs(difference) <= 0.005 ? "Balanced" : "Not balanced")
          ),
          h("button", { className: "primary-button", type: "button", onClick: createJournal, disabled: isLoading || Math.abs(difference) > 0.005 }, "Post journal")
        ),
        h("div", { className: "checkout-grid two" },
          h("label", null, "Entry date",
            h("input", { type: "date", value: form.entryDate, onChange: (event) => setForm((current) => ({ ...current, entryDate: event.target.value })) })
          ),
          h("label", null, "Reference type",
            h("input", { value: form.referenceType, onChange: (event) => setForm((current) => ({ ...current, referenceType: event.target.value })) })
          ),
          h("label", null, "Reference ID",
            h("input", { value: form.referenceId, inputMode: "numeric", onChange: (event) => setForm((current) => ({ ...current, referenceId: event.target.value })) })
          ),
          h("label", null, "Description",
            h("input", { value: form.description, onChange: (event) => setForm((current) => ({ ...current, description: event.target.value })), placeholder: "Why this journal is being posted" })
          )
        ),
        h("section", { className: "journal-lines" },
          lines.map((line) =>
            h("div", { className: "journal-line", key: line.key },
              h("label", null, "Account",
                h("select", { value: line.accountId, onChange: (event) => updateLine(line.key, { accountId: event.target.value }) },
                  accounts.map((account) => h("option", { key: read(account, "id"), value: read(account, "id") }, accountName(account)))
                )
              ),
              h("label", null, "Debit",
                h("input", { value: line.debit, inputMode: "decimal", onChange: (event) => updateLine(line.key, { debit: event.target.value }) })
              ),
              h("label", null, "Credit",
                h("input", { value: line.credit, inputMode: "decimal", onChange: (event) => updateLine(line.key, { credit: event.target.value }) })
              ),
              h("button", { className: "ghost-button", type: "button", onClick: () => setLines((current) => current.filter((item) => item.key !== line.key)), disabled: lines.length <= 2 }, "Remove")
            )
          )
        ),
        h("div", { className: "report-buttons" },
          h("button", { className: "secondary-button", type: "button", onClick: () => addLine("debit") }, "Add debit line"),
          h("button", { className: "secondary-button", type: "button", onClick: () => addLine("credit") }, "Add credit line")
        )
      ),
      h("article", { className: "table-panel" },
        h(DataTable, {
          columns: [
            { key: "id", label: "Entry", render: (row) => `#${read(row, "id")}` },
            { key: "date", label: "Date", render: (row) => shortDate(read(row, "entryDate")) },
            { key: "description", label: "Description", render: (row) => read(row, "description") || "-" },
            { key: "debit", label: "Debit", render: (row) => money(read(row, "totalDebit"), "USD") },
            { key: "credit", label: "Credit", render: (row) => money(read(row, "totalCredit"), "USD") },
            { key: "lines", label: "Lines", render: (row) => read(row, "lineCount") || 0 }
          ],
          rows: entries,
          getRowKey: (row, index) => `journal-${read(row, "id") || index}`,
          emptyText: "No journal entries found."
        })
      )
    )
  );
}

const assistantStarterActions = [
  {
    id: "create_report",
    label: "Create report",
    description: "Pick a quick operational report to run.",
    kind: "Report",
    tone: "Neutral",
    payload: {}
  },
  {
    id: "send_payment_reminder",
    label: "Send reminder",
    description: "Prepare payment reminder text from unpaid invoices.",
    kind: "Draft",
    target: "whatsapp",
    tone: "Good",
    payload: {}
  },
  {
    id: "open_customer",
    label: "Open customer",
    description: "Open the top customer summary from live balances.",
    kind: "Open",
    target: "contacts",
    tone: "Neutral",
    payload: {}
  },
  {
    id: "draft_purchase_order",
    label: "Draft purchase order",
    description: "Build a low-stock purchase shortlist.",
    kind: "Draft",
    target: "purchase-parts",
    tone: "Good",
    payload: {}
  },
  {
    id: "find_unpaid_customers",
    label: "Find unpaid customers",
    description: "List customer balances from unpaid sales.",
    kind: "Query",
    tone: "Warning",
    payload: {}
  },
  {
    id: "campaign_dead_stock_clearance",
    label: "Dead stock clearance",
    description: "Suggest a WhatsApp campaign for dormant on-hand parts.",
    kind: "Campaign",
    target: "whatsapp",
    tone: "Warning",
    payload: {}
  },
  {
    id: "campaign_back_in_stock",
    label: "Back in stock campaign",
    description: "Feature recently received available parts.",
    kind: "Campaign",
    target: "whatsapp",
    tone: "Good",
    payload: {}
  },
  {
    id: "campaign_unpaid_reminders",
    label: "Unpaid reminders",
    description: "Prepare a receivables reminder campaign.",
    kind: "Campaign",
    target: "whatsapp",
    tone: "Warning",
    payload: {}
  },
  {
    id: "campaign_seasonal_service_parts",
    label: "Seasonal service parts",
    description: "Promote maintenance parts customers may need now.",
    kind: "Campaign",
    target: "whatsapp",
    tone: "Good",
    payload: {}
  }
];

function actionValue(action, key, fallback = "") {
  const value = read(action, key);
  return value === "" ? fallback : value;
}

function BusinessAssistantWorkspace({ api, module, t, onView }) {
  const [question, setQuestion] = useState("");
  const [response, setResponse] = useState(null);
  const [status, setStatus] = useState(t("assistant.ready", "Ask a business question to inspect live operating data."));
  const [isLoading, setIsLoading] = useState(false);

  const runAction = useCallback(async (action) => {
    const actionId = actionValue(action, "id");
    const label = actionValue(action, "label", "action");
    const kind = actionValue(action, "kind");
    const target = actionValue(action, "target");

    if (String(kind).toLowerCase() === "navigate" && target && typeof onView === "function") {
      onView(target);
      return;
    }

    if (!actionId) {
      setStatus(t("assistant.actionRequired", "Choose an action first."));
      return;
    }

    setIsLoading(true);
    setStatus(t("assistant.runningAction", "Running {label}...", { label }));
    try {
      const result = await api.post("/api/business-assistant/actions/run", {
        actionId,
        payload: read(action, "payload") || {}
      });
      setResponse(result);
      setStatus(t("assistant.actionComplete", "Assistant action complete."));
    } catch (error) {
      setStatus(error.message || t("assistant.actionError", "Could not run assistant action."));
    } finally {
      setIsLoading(false);
    }
  }, [api, onView, t]);

  const ask = useCallback(async () => {
    const text = question.trim();
    if (!text) {
      setStatus(t("assistant.questionRequired", "Enter a question first."));
      return;
    }

    setIsLoading(true);
    setStatus(t("assistant.asking", "Asking the business assistant..."));
    try {
      const result = await api.post(module.endpoint, { question: text });
      setResponse(result);
      setStatus(t("assistant.answered", "Business assistant answered."));
    } catch (error) {
      setResponse(null);
      setStatus(error.message || t("assistant.error", "Could not ask the business assistant."));
    } finally {
      setIsLoading(false);
    }
  }, [api, module.endpoint, question, t]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: module.title,
      subtitle: t("assistant.subtitle", "Run operational actions or ask for a live business answer."),
      action: null
    }),
    h("section", { className: "invoice-create-panel" },
      h("div", { className: "invoice-create-header" },
        h("div", null,
          h("h3", null, t("assistant.question", "Question")),
          h("span", null, t("assistant.hint", "Examples: slow stock, unpaid customer debt, today's sales."))
        ),
        h("button", { className: "primary-button", type: "button", onClick: ask, disabled: isLoading },
          isLoading ? t("common.loading", "Loading") : t("assistant.ask", "Ask")
        )
      ),
      h("textarea", {
        value: question,
        onChange: (event) => setQuestion(event.target.value),
        onKeyDown: (event) => {
          if ((event.ctrlKey || event.metaKey) && event.key === "Enter") ask();
        },
        rows: 4,
        placeholder: t("assistant.placeholder", "What should I check?")
      })
    ),
    h("section", { className: "assistant-action-grid", "aria-label": t("assistant.actions", "Assistant actions") },
      ((response?.actions || []).length ? response.actions : assistantStarterActions).map((action) =>
        h("button", {
          className: `assistant-action-button tone-${String(actionValue(action, "tone", "neutral")).toLowerCase()}`,
          key: actionValue(action, "id") || actionValue(action, "label"),
          type: "button",
          onClick: () => runAction(action),
          disabled: isLoading
        },
          h("strong", null, actionValue(action, "label", "Run action")),
          h("span", null, actionValue(action, "description")),
          h("em", null, actionValue(action, "kind", "Action"))
        )
      )
    ),
    h(StatusLine, { status }),
    response && h("section", { className: "two-column" },
      h("article", { className: "panel" },
        h("h3", null, t("assistant.answer", "Answer")),
        h("p", null, response.answer || t("assistant.noAnswer", "No answer returned.")),
        response.suggestions?.length > 0 && h("div", { className: "dense-list" },
          response.suggestions.map((suggestion) =>
            h("button", {
              className: "list-row action-row",
              key: suggestion,
              type: "button",
              onClick: () => setQuestion(suggestion)
            },
              h("i", { className: "row-marker", "aria-hidden": "true" }),
              h("div", null, h("strong", null, suggestion), h("span", null, t("assistant.useSuggestion", "Use as next prompt")))
            )
          )
        )
      ),
      h("article", { className: "panel" },
        h("h3", null, t("assistant.insights", "Insights")),
        h("div", { className: "dense-list assistant-insight-list" },
          (response.insights || []).map((insight, index) =>
            h("div", { className: "list-row", key: `${insight.label}-${index}` },
              h("div", null,
                h("strong", null, insight.label || insight.severity || "Insight"),
                h("span", null, insight.detail || "")
              ),
              h("b", null, insight.value || "")
            )
          ),
          (!response.insights || response.insights.length === 0) && h("p", { className: "empty-state" }, t("assistant.noInsights", "No insights returned."))
        )
      )
    )
  );
}

function ScanLookupWorkspace({ api, module, t }) {
  const [code, setCode] = useState("");
  const [rows, setRows] = useState([]);
  const [status, setStatus] = useState(t("scans.ready", "Enter a barcode, invoice number, purchase number, or stock code."));
  const [isLoading, setIsLoading] = useState(false);

  const resolve = useCallback(async () => {
    const scanCode = code.trim();
    if (!scanCode) {
      setStatus(t("scans.codeRequired", "Enter a scan code first."));
      return;
    }

    setIsLoading(true);
    setStatus(t("scans.resolving", "Resolving scan code..."));
    try {
      const result = asRows(await api.get(`${module.endpoint}?code=${encodeURIComponent(scanCode)}`));
      setRows(result);
      setStatus(result.length
        ? t("scans.resolved", "{count} result(s) found.", { count: result.length })
        : t("scans.noResults", "No matching record was found."));
    } catch (error) {
      setRows([]);
      setStatus(error.message || t("scans.error", "Could not resolve the scan code."));
    } finally {
      setIsLoading(false);
    }
  }, [api, code, module.endpoint, t]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: module.title, action: null }),
    h(ModuleSummary, { module, t }),
    h("section", { className: "invoice-create-panel" },
      h("div", { className: "invoice-line-builder" },
        h("label", null,
          t("scans.code", "Scan code"),
          h("input", {
            value: code,
            onChange: (event) => setCode(event.target.value),
            onKeyDown: (event) => event.key === "Enter" && resolve(),
            placeholder: t("scans.placeholder", "Scan or type a code")
          })
        ),
        h("button", { className: "primary-button", type: "button", onClick: resolve, disabled: isLoading },
          isLoading ? t("common.loading", "Loading") : t("scans.resolve", "Resolve")
        )
      )
    ),
    h(StatusLine, { status }),
    h("section", { className: "table-panel" },
      h(DataTable, {
        columns: scanColumns,
        rows,
        getRowKey: (row, index) => `${row.targetType || "scan"}-${row.targetId || row.code || index}`,
        emptyText: t("scans.empty", "No scan results yet.")
      })
    )
  );
}

export function ModuleWorkspaceView({ api, module, t, onView }) {
  if (!module) {
    return h("section", { className: "screen" },
      h(PageHeader, { title: t("module.unavailable", "Workspace unavailable"), action: null }),
      h(StatusLine, { status: t("module.notMapped", "This workspace is not mapped in the web shell.") })
    );
  }

  if (module.key === "business-assistant") {
    return h(BusinessAssistantWorkspace, { api, module, t, onView });
  }

  if (module.key === "report-builder") {
    return h(ReportBuilderWorkspace, { api, module, t });
  }

  if (module.key === "ar") {
    return h(ScanLookupWorkspace, { api, module, t });
  }

  if (module.key === "purchase-parts") {
    return h(PartPurchasesWorkspace, { api, module, t });
  }

  if (module.key === "used-car-purchases") {
    return h(UsedCarPurchasesWorkspace, { api, module, t });
  }

  if (module.key === "stock") {
    return h(StockWorkspace, { api, module, t });
  }

  if (module.key === "manual-journal") {
    return h(ManualJournalWorkspace, { api, module, t });
  }

  const canPreview = module.endpoint && !module.endpoint.endsWith("/ask") && !module.endpoint.endsWith("/resolve");
  const [rows, setRows] = useState([]);
  const [status, setStatus] = useState(canPreview ? "" : t("module.mappedCommand", "This WPF workspace is mapped to a command endpoint."));
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    if (!canPreview) return;
    setIsLoading(true);
    setStatus(t("module.loading", `Loading ${module.title.toLowerCase()}...`, { title: module.title.toLowerCase() }));
    try {
      setRows(asRows(await api.get(module.endpoint)));
      setStatus(t("module.loaded", `${module.title} loaded.`, { title: module.title }));
    } catch (error) {
      setRows([]);
      setStatus(error.message || t("module.loadError", `Could not load ${module.title.toLowerCase()}.`, { title: module.title.toLowerCase() }));
    } finally {
      setIsLoading(false);
    }
  }, [api, canPreview, module]);

  useEffect(() => { load(); }, [load]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: module.title,
      action: canPreview
        ? h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, t("common.refresh", "Refresh"))
        : null
    }),
    h(ModuleSummary, { module, t }),
    h(StatusLine, { status }),
    canPreview && h("section", { className: "table-panel" },
      h(DataTable, {
        columns: genericColumns(t),
        rows,
        getRowKey: (row, index) => `${module.key}-${row.id || row.invoiceId || row.purchaseId || row.referenceNumber || index}`,
        emptyText: t("module.noRows", `No ${module.title.toLowerCase()} rows returned.`, { title: module.title.toLowerCase() })
      })
    )
  );
}

export function createModuleView(module) {
  return function ModuleView({ api, t, onView }) {
    return h(ModuleWorkspaceView, { api, module, t, onView });
  };
}
