import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { CommunicationPayloadFactory } from "../services/communication-payload-factory.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

function todayInputValue() {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function toNumber(value) {
  const parsed = Number.parseFloat(String(value || "").replace(/,/g, ""));
  return Number.isFinite(parsed) ? parsed : 0;
}

function partLabel(part) {
  return [part.internalCode, part.name].filter(Boolean).join(" - ") || `Part #${part.id}`;
}

function profitClass(value) {
  const n = Number(value || 0);
  if (n > 0) return "success-text";
  if (n < 0) return "danger-text";
  return "";
}

export function InvoicesView({ api }) {
  const [search, setSearch] = useState("");
  const [invoices, setInvoices] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [parts, setParts] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [selectedInvoice, setSelectedInvoice] = useState(null);
  const [invoiceDate, setInvoiceDate] = useState(todayInputValue());
  const [customerId, setCustomerId] = useState("");
  const [warehouseId, setWarehouseId] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("Cash");
  const [paidAmount, setPaidAmount] = useState("");
  const [notes, setNotes] = useState("");
  const [selectedPartId, setSelectedPartId] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [unitPrice, setUnitPrice] = useState("");
  const [draftItems, setDraftItems] = useState([]);
  const [availableStock, setAvailableStock] = useState(null);
  const [profitHistory, setProfitHistory] = useState([]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading invoices...");
    try {
      const query = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : "";
      setInvoices(await api.get(`/api/sales${query}`));
      setStatus("Invoices loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load invoices.");
    } finally {
      setIsLoading(false);
    }
  }, [api, search]);

  useEffect(() => { load(); }, [load]);

  const loadLookups = useCallback(async () => {
    try {
      const [customerRows, warehouseRows, partRows] = await Promise.all([
        api.get("/api/customers?page=1&pageSize=100"),
        api.get("/api/warehouses"),
        api.list("/api/parts")
      ]);

      setCustomers(customerRows);
      setWarehouses(warehouseRows);
      setParts(partRows);
      setWarehouseId((current) => {
        if (current) return current;
        const defaultWarehouse = warehouseRows.find((warehouse) => warehouse.isMain) || warehouseRows[0];
        return defaultWarehouse ? String(defaultWarehouse.id) : "";
      });
    } catch (error) {
      setStatus(error.message || "Could not load invoice creation options.");
    }
  }, [api]);

  useEffect(() => { loadLookups(); }, [loadLookups]);

  useEffect(() => {
    api.get("/api/sales/profit-history?months=12")
      .then((rows) => setProfitHistory(Array.isArray(rows) ? rows : []))
      .catch(() => setProfitHistory([]));
  }, [api]);

  const loadInvoiceDetail = useCallback(async (invoiceId) => {
    try {
      const detail = await api.get(`/api/sales/${invoiceId}`);
      setSelectedInvoice(detail);
    } catch (error) {
      setStatus(error.message || "Could not load invoice detail.");
    }
  }, [api]);

  const sendInvoice = useCallback(async (invoiceId, templateKey) => {
    setStatus("Preparing WhatsApp message...");
    try {
      const payload = templateKey === "PaymentReminder"
        ? CommunicationPayloadFactory.paymentReminder(invoiceId)
        : CommunicationPayloadFactory.salesInvoice(invoiceId);
      await api.post("/api/communications/send", payload);
      setStatus(templateKey === "PaymentReminder" ? "Payment reminder sent." : "Invoice sent.");
    } catch (error) {
      setStatus(error.message || "Message failed.");
    }
  }, [api]);

  const selectedPart = useMemo(
    () => parts.find((part) => String(part.id) === selectedPartId),
    [parts, selectedPartId]
  );

  const draftTotal = useMemo(
    () => draftItems.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0),
    [draftItems]
  );

  const profitTrend = useMemo(() => {
    const byDate = new Map();
    (Array.isArray(invoices) ? invoices : []).forEach((inv) => {
      if (!inv.invoiceDate) return;
      const dateKey = new Date(inv.invoiceDate).toLocaleDateString();
      byDate.set(dateKey, (byDate.get(dateKey) || 0) + Number(inv.totalAmount || 0));
    });
    const sorted = Array.from(byDate.entries())
      .sort((a, b) => new Date(b[0]) - new Date(a[0]))
      .slice(0, 7)
      .reverse();
    const maxAmount = sorted.reduce((m, [, v]) => Math.max(m, v), 0);
    return sorted.map(([date, amount]) => ({ date, amount, width: maxAmount > 0 ? Math.round((amount / maxAmount) * 100) : 0 }));
  }, [invoices]);

  const onPartChange = useCallback(async (event) => {
    const nextPartId = event.target.value;
    const nextPart = parts.find((part) => String(part.id) === nextPartId);
    setSelectedPartId(nextPartId);
    setUnitPrice(nextPart ? String(nextPart.salePrice || 0) : "");
    setAvailableStock(null);
    if (nextPartId) {
      try {
        const stockData = await api.get(`/api/parts/${nextPartId}/stock`);
        setAvailableStock(typeof stockData === "number" ? stockData : Number(stockData?.availableQuantity ?? stockData?.available ?? stockData ?? 0));
      } catch {
        setAvailableStock(null);
      }
    }
  }, [api, parts]);

  const addLine = useCallback(() => {
    if (!selectedPart) {
      setStatus("Choose a part before adding a line.");
      return;
    }

    const nextQuantity = Math.trunc(toNumber(quantity));
    const nextUnitPrice = toNumber(unitPrice);
    if (nextQuantity <= 0 || nextUnitPrice < 0) {
      setStatus("Enter a valid quantity and unit price.");
      return;
    }

    setDraftItems((current) => {
      const existing = current.find((item) => item.partId === selectedPart.id);
      if (!existing) {
        return [
          ...current,
          {
            partId: selectedPart.id,
            description: partLabel(selectedPart),
            quantity: nextQuantity,
            unitPrice: nextUnitPrice
          }
        ];
      }

      return current.map((item) =>
        item.partId === selectedPart.id
          ? { ...item, quantity: item.quantity + nextQuantity, unitPrice: nextUnitPrice }
          : item
      );
    });
    setSelectedPartId("");
    setQuantity("1");
    setUnitPrice("");
    setStatus(`${partLabel(selectedPart)} added to the draft.`);
  }, [quantity, selectedPart, unitPrice]);

  const removeLine = useCallback((partId) => {
    setDraftItems((current) => current.filter((item) => item.partId !== partId));
  }, []);

  const createInvoice = useCallback(async () => {
    if (!warehouseId) {
      setStatus("Choose a warehouse before creating the invoice.");
      return;
    }
    if (draftItems.length === 0) {
      setStatus("Add at least one invoice line.");
      return;
    }

    setIsSaving(true);
    setStatus("Creating invoice...");
    try {
      const response = await api.post("/api/sales", {
        invoiceDate: invoiceDate ? `${invoiceDate}T00:00:00` : new Date().toISOString(),
        customerId: customerId ? Number(customerId) : null,
        warehouseId: Number(warehouseId),
        paymentMethod,
        paidAmount: toNumber(paidAmount),
        notes,
        items: draftItems.map((item) => ({
          partId: item.partId,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discountAmount: 0,
          taxRate: 0
        }))
      });

      setDraftItems([]);
      setPaidAmount("");
      setNotes("");
      await load();
      setStatus(`Invoice ${response.invoiceNumber} created. Total ${money(response.totalAmount, response.currencyCode || "USD")}.`);
    } catch (error) {
      setStatus(error.message || "Could not create invoice.");
    } finally {
      setIsSaving(false);
    }
  }, [api, customerId, draftItems, invoiceDate, load, notes, paidAmount, paymentMethod, warehouseId]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Invoices",
      action: h("div", { className: "toolbar" },
        h("input", {
          value: search,
          onChange: (event) => setSearch(event.target.value),
          onKeyDown: (event) => event.key === "Enter" && load(),
          placeholder: "Search invoices"
        }),
        h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, "Search")
      )
    }),
    h(StatusLine, { status }),
    h("section", { className: "invoice-create-panel" },
      h("div", { className: "invoice-create-header" },
        h("div", null,
          h("h3", null, "Create Invoice"),
          h("span", null, `${draftItems.length} line${draftItems.length === 1 ? "" : "s"} · ${money(draftTotal, "USD")}`)
        ),
        h("button", { className: "primary-button", onClick: createInvoice, disabled: isSaving }, isSaving ? "Creating..." : "Create Invoice")
      ),
      h("div", { className: "invoice-form-grid" },
        h("label", null,
          "Customer",
          h("select", { value: customerId, onChange: (event) => setCustomerId(event.target.value) },
            h("option", { value: "" }, "Walk-in customer"),
            customers.map((customer) =>
              h("option", { key: customer.id, value: customer.id }, customer.name)
            )
          )
        ),
        h("label", null,
          "Warehouse",
          h("select", { value: warehouseId, onChange: (event) => setWarehouseId(event.target.value) },
            h("option", { value: "" }, "Choose warehouse"),
            warehouses.map((warehouse) =>
              h("option", { key: warehouse.id, value: warehouse.id }, warehouse.name)
            )
          )
        ),
        h("label", null,
          "Invoice date",
          h("input", { type: "date", value: invoiceDate, onChange: (event) => setInvoiceDate(event.target.value) })
        ),
        h("label", null,
          "Payment method",
          h("input", { value: paymentMethod, onChange: (event) => setPaymentMethod(event.target.value), placeholder: "Cash" })
        ),
        h("label", null,
          "Paid amount",
          h("input", { inputMode: "decimal", value: paidAmount, onChange: (event) => setPaidAmount(event.target.value), placeholder: "0" })
        ),
        h("label", { className: "invoice-notes-field" },
          "Notes",
          h("textarea", { value: notes, onChange: (event) => setNotes(event.target.value), rows: 2, placeholder: "Optional invoice note" })
        )
      ),
      h("div", { className: "invoice-line-builder" },
        h("label", null,
          "Part",
          h("select", { value: selectedPartId, onChange: onPartChange },
            h("option", { value: "" }, "Choose part"),
            parts.map((part) =>
              h("option", { key: part.id, value: part.id }, `${partLabel(part)} · ${money(part.salePrice, part.currency || "USD")}`)
            )
          )
        ),
        h("label", null,
          availableStock !== null
            ? h("span", null,
                "Qty ",
                h("small", { style: { color: toNumber(quantity) > availableStock ? "red" : "inherit" } },
                  `(available: ${availableStock})`
                )
              )
            : "Qty",
          h("input", {
            inputMode: "numeric",
            value: quantity,
            onChange: (event) => setQuantity(event.target.value),
            style: availableStock !== null && toNumber(quantity) > availableStock ? { borderColor: "red" } : {}
          })
        ),
        h("label", null,
          "Unit price",
          h("input", { inputMode: "decimal", value: unitPrice, onChange: (event) => setUnitPrice(event.target.value) })
        ),
        h("button", { className: "secondary-button", onClick: addLine }, "Add Line")
      ),
      h("div", { className: "invoice-draft-lines" },
        draftItems.length === 0
          ? h("p", { className: "empty-state" }, "No draft lines yet.")
          : draftItems.map((item) =>
            h("div", { className: "invoice-draft-line", key: item.partId },
              h("div", null,
                h("strong", null, item.description),
                h("span", null, `${item.quantity} x ${money(item.unitPrice, "USD")}`)
              ),
              h("div", { className: "row-actions" },
                h("strong", null, money(item.quantity * item.unitPrice, "USD")),
                h("button", { onClick: () => removeLine(item.partId) }, "Remove")
              )
            )
          )
      )
    ),
    selectedInvoice && h("section", { className: "admin-panel", style: { marginBottom: "1rem" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, `Invoice ${selectedInvoice.invoiceNumber}`),
        h("button", { type: "button", onClick: () => setSelectedInvoice(null) }, "Close")
      ),
      h("div", { className: "invoice-draft-lines" },
        (selectedInvoice.items || []).length === 0
          ? h("p", { className: "empty-state" }, "No line items.")
          : (selectedInvoice.items || []).map((item, idx) => {
            const revenue = item.quantity * item.unitPrice;
            const cost = item.quantity * (item.costPrice || 0);
            const profit = revenue - cost;
            const margin = revenue > 0 ? ((profit / revenue) * 100).toFixed(1) : null;
            return h("div", { className: "invoice-draft-line", key: idx },
              h("div", null,
                h("strong", null, item.description),
                h("span", null, `${item.quantity} × ${money(item.unitPrice, selectedInvoice.counterCurrencyCode || "USD")}`)
              ),
              h("div", { className: "row-actions" },
                h("span", null, money(revenue, selectedInvoice.counterCurrencyCode || "USD")),
                item.costPrice > 0 && h("span", { className: profitClass(profit) },
                  `Profit: ${money(profit, selectedInvoice.counterCurrencyCode || "USD")}${margin != null ? ` (${margin}%)` : ""}`
                )
              )
            );
          }),
        (selectedInvoice.items || []).some((i) => i.costPrice > 0) && (() => {
          const totalRevenue = (selectedInvoice.items || []).reduce((s, i) => s + i.quantity * i.unitPrice, 0);
          const totalCost = (selectedInvoice.items || []).reduce((s, i) => s + i.quantity * (i.costPrice || 0), 0);
          const totalProfit = totalRevenue - totalCost;
          const totalMargin = totalRevenue > 0 ? ((totalProfit / totalRevenue) * 100).toFixed(1) : null;
          return h("div", { className: "invoice-draft-total" },
            h("span", null, `Revenue: ${money(totalRevenue, selectedInvoice.counterCurrencyCode || "USD")}`),
            h("span", { className: profitClass(totalProfit) },
              `Profit: ${money(totalProfit, selectedInvoice.counterCurrencyCode || "USD")}${totalMargin != null ? ` · Margin: ${totalMargin}%` : ""}`
            )
          );
        })()
      )
    ),
    profitTrend.length > 0 && h("section", { className: "panel", style: { marginBottom: "1rem", padding: "1rem" } },
      h("h4", { style: { margin: "0 0 0.75rem" } }, "Recent Profit Trend (last 7 days)"),
      profitTrend.map(({ date, amount, width }) =>
        h("div", { key: date, style: { display: "flex", alignItems: "center", gap: "0.5rem", marginBottom: "0.35rem", fontSize: "0.85rem" } },
          h("span", { style: { minWidth: "7rem", color: "#555" } }, date),
          h("div", { style: { flex: 1, background: "#eee", borderRadius: "3px", height: "16px", position: "relative" } },
            h("div", { style: { width: `${width}%`, height: "100%", background: "var(--accent-color, #4caf50)", borderRadius: "3px", transition: "width 0.3s" } })
          ),
          h("span", { style: { minWidth: "6rem", textAlign: "right", fontWeight: 600 } }, money(amount, "USD"))
        )
      )
    ),
    profitHistory.length > 0 && (() => {
      const maxProfit = profitHistory.reduce((m, p) => Math.max(m, Math.abs(p.profit || 0)), 0);
      return h("section", { className: "panel", style: { marginBottom: "1rem", padding: "1rem" } },
        h("h4", { style: { margin: "0 0 0.75rem" } }, "Profit History (last 12 months)"),
        profitHistory.map(({ period, profit, marginPercent, revenue, invoiceCount }) => {
          const width = maxProfit > 0 ? Math.round((Math.abs(profit) / maxProfit) * 100) : 0;
          return h("div", { key: period, style: { display: "flex", alignItems: "center", gap: "0.5rem", marginBottom: "0.35rem", fontSize: "0.85rem" } },
            h("span", { style: { minWidth: "5rem", color: "#555" } }, period),
            h("div", { style: { flex: 1, background: "#eee", borderRadius: "3px", height: "16px", position: "relative" } },
              h("div", {
                style: {
                  width: `${width}%`,
                  height: "100%",
                  background: profit >= 0 ? "var(--accent-color, #4caf50)" : "#e53935",
                  borderRadius: "3px",
                  transition: "width 0.3s"
                }
              })
            ),
            h("span", { className: profitClass(profit), style: { minWidth: "6rem", textAlign: "right", fontWeight: 600 } }, money(profit, "USD")),
            h("span", { style: { minWidth: "4rem", textAlign: "right", color: "#777" } }, revenue > 0 ? `${marginPercent}%` : "—"),
            h("span", { style: { minWidth: "5rem", textAlign: "right", color: "#999" } }, `${invoiceCount} sale${invoiceCount === 1 ? "" : "s"}`)
          );
        })
      );
    })(),
    h("section", { className: "table-panel" },
      h(DataTable, {
        columns: [
          { key: "invoice", label: "Invoice", render: (invoice) => invoice.invoiceNumber },
          { key: "customer", label: "Customer", render: (invoice) => invoice.customerName || "Walk-in" },
          { key: "date", label: "Date", render: (invoice) => invoice.invoiceDate ? new Date(invoice.invoiceDate).toLocaleDateString() : "" },
          { key: "total", label: "Total", render: (invoice) => money(invoice.totalAmount, invoice.currencyCode || "USD") },
          { key: "paid", label: "Paid", render: (invoice) => money(invoice.paidAmount, invoice.currencyCode || "USD") },
          {
            key: "actions",
            label: "Actions",
            render: (invoice) => h("div", { className: "row-actions" },
              h("button", { onClick: () => loadInvoiceDetail(invoice.invoiceId) }, "View Profit"),
              h("button", { onClick: () => sendInvoice(invoice.invoiceId, "SalesInvoice") }, "Send Invoice"),
              h("button", { onClick: () => sendInvoice(invoice.invoiceId, "PaymentReminder") }, "Reminder")
            )
          }
        ],
        rows: invoices,
        getRowKey: (invoice) => invoice.invoiceId,
        emptyText: "No invoices found."
      })
    )
  );
}
