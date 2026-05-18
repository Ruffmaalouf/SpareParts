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

export function InvoicesView({ api }) {
  const [search, setSearch] = useState("");
  const [invoices, setInvoices] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [parts, setParts] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
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
        api.get("/api/parts?page=1&pageSize=200")
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

  const onPartChange = useCallback((event) => {
    const nextPartId = event.target.value;
    const nextPart = parts.find((part) => String(part.id) === nextPartId);
    setSelectedPartId(nextPartId);
    setUnitPrice(nextPart ? String(nextPart.salePrice || 0) : "");
  }, [parts]);

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
          "Qty",
          h("input", { inputMode: "numeric", value: quantity, onChange: (event) => setQuantity(event.target.value) })
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
