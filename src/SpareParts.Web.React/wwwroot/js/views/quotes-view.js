import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { money, dateTime } from "../core/formatters.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

const statusOptions = ["Draft", "Sent", "Accepted", "Declined", "Expired", "Converted", "All"];

function todayInputValue() {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function defaultExpiryValue() {
  const date = new Date();
  date.setDate(date.getDate() + 7);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function toNumber(value) {
  const parsed = Number.parseFloat(String(value || "").replace(/,/g, ""));
  return Number.isFinite(parsed) ? parsed : 0;
}

function partLabel(part) {
  return [part.internalCode, part.name].filter(Boolean).join(" - ") || `Part #${part.id}`;
}

function statusBadgeClass(status) {
  switch (String(status || "").toLowerCase()) {
    case "accepted": return "status-badge badge-green";
    case "converted": return "status-badge badge-green";
    case "declined": return "status-badge badge-red";
    case "expired": return "status-badge badge-red";
    case "sent": return "status-badge badge-blue";
    default: return "status-badge badge-neutral";
  }
}

function createEmptyForm() {
  return {
    customerId: "",
    customerName: "",
    customerPhone: "",
    warehouseId: "",
    quoteDate: todayInputValue(),
    expiryDate: defaultExpiryValue(),
    notes: ""
  };
}

export function QuotesView({ api }) {
  const [quotes, setQuotes] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [warehouses, setWarehouses] = useState([]);
  const [parts, setParts] = useState([]);
  const [form, setForm] = useState(() => createEmptyForm());
  const [draftItems, setDraftItems] = useState([]);
  const [selectedPartId, setSelectedPartId] = useState("");
  const [lineQty, setLineQty] = useState("1");
  const [linePrice, setLinePrice] = useState("");
  const [statusFilter, setStatusFilter] = useState("Draft");
  const [search, setSearch] = useState("");
  const [selectedQuote, setSelectedQuote] = useState(null);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading quotes...");
    try {
      const statusQuery = statusFilter === "All" ? "" : `?status=${encodeURIComponent(statusFilter)}`;
      const [nextQuotes, nextCustomers, nextWarehouses, nextParts] = await Promise.all([
        api.get(`/api/quotes${statusQuery}`),
        api.get("/api/customers?page=1&pageSize=5000"),
        api.get("/api/warehouses"),
        api.list("/api/parts")
      ]);
      setQuotes(Array.isArray(nextQuotes) ? nextQuotes : []);
      setCustomers(Array.isArray(nextCustomers) ? nextCustomers : []);
      setWarehouses(Array.isArray(nextWarehouses) ? nextWarehouses : []);
      setParts(Array.isArray(nextParts) ? nextParts : []);
      if (!form.warehouseId) {
        const main = (Array.isArray(nextWarehouses) ? nextWarehouses : []).find((w) => w.isMain) || nextWarehouses[0];
        if (main) setForm((current) => ({ ...current, warehouseId: String(main.id) }));
      }
      setStatus("Quotes loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load quotes.");
    } finally {
      setIsLoading(false);
    }
  }, [api, statusFilter, form.warehouseId]);

  useEffect(() => { load(); }, [load]);

  const setField = useCallback((key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  }, []);

  const chooseCustomer = useCallback((customerId) => {
    const customer = customers.find((c) => String(c.id) === String(customerId));
    setForm((current) => ({
      ...current,
      customerId,
      customerName: customer?.name || current.customerName,
      customerPhone: customer?.phone || current.customerPhone
    }));
  }, [customers]);

  const choosePart = useCallback((partId) => {
    const part = parts.find((p) => String(p.id) === String(partId));
    setSelectedPartId(partId);
    setLinePrice(part ? String(part.salePrice || 0) : "");
  }, [parts]);

  const addLine = useCallback(() => {
    const part = parts.find((p) => String(p.id) === String(selectedPartId));
    if (!part) {
      setStatus("Choose a part to add.");
      return;
    }
    const qty = Math.max(1, Math.trunc(toNumber(lineQty)));
    const price = toNumber(linePrice);
    if (price < 0) {
      setStatus("Unit price cannot be negative.");
      return;
    }
    setDraftItems((current) => {
      const existing = current.find((item) => item.partId === part.id);
      if (existing) {
        return current.map((item) =>
          item.partId === part.id ? { ...item, quantity: item.quantity + qty, unitPrice: price } : item
        );
      }
      return [...current, { partId: part.id, description: partLabel(part), quantity: qty, unitPrice: price }];
    });
    setSelectedPartId("");
    setLineQty("1");
    setLinePrice("");
    setStatus(`${partLabel(part)} added.`);
  }, [linePrice, lineQty, parts, selectedPartId]);

  const removeLine = useCallback((partId) => {
    setDraftItems((current) => current.filter((item) => item.partId !== partId));
  }, []);

  const createQuote = useCallback(async () => {
    if (!form.customerName.trim()) {
      setStatus("Customer name is required.");
      return;
    }
    if (draftItems.length === 0) {
      setStatus("Add at least one line item.");
      return;
    }
    setIsSaving(true);
    setStatus("Saving quote...");
    try {
      const id = await api.post("/api/quotes", {
        quoteDate: form.quoteDate ? `${form.quoteDate}T00:00:00` : new Date().toISOString(),
        expiryDate: form.expiryDate ? `${form.expiryDate}T23:59:59` : null,
        customerId: form.customerId ? Number(form.customerId) : null,
        customerName: form.customerName.trim(),
        customerPhone: form.customerPhone.trim() || null,
        warehouseId: form.warehouseId ? Number(form.warehouseId) : null,
        notes: form.notes.trim() || null,
        items: draftItems.map((item) => ({
          partId: item.partId,
          description: item.description,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discountAmount: 0
        }))
      });
      setForm(createEmptyForm());
      setDraftItems([]);
      setStatus(`Quote #${id} created.`);
      await load();
    } catch (error) {
      setStatus(error.message || "Could not save quote.");
    } finally {
      setIsSaving(false);
    }
  }, [api, draftItems, form, load]);

  const updateQuoteStatus = useCallback(async (quote, nextStatus) => {
    setIsSaving(true);
    try {
      await api.put(`/api/quotes/${quote.id}/status`, { status: nextStatus });
      setStatus(`Quote marked ${nextStatus}.`);
      await load();
    } catch (error) {
      setStatus(error.message || "Could not update quote.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const convertToInvoice = useCallback(async (quote) => {
    if (!window.confirm(`Convert ${quote.quoteNumber} to a sales invoice?`)) return;
    setIsSaving(true);
    setStatus("Converting to invoice...");
    try {
      const result = await api.post(`/api/quotes/${quote.id}/convert`, {});
      setStatus(`Invoice ${result.invoiceNumber} created from quote ${quote.quoteNumber}.`);
      await load();
    } catch (error) {
      setStatus(error.message || "Could not convert quote.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const deleteQuote = useCallback(async (quote) => {
    if (!window.confirm(`Delete quote ${quote.quoteNumber}?`)) return;
    setIsSaving(true);
    try {
      await api.delete(`/api/quotes/${quote.id}`);
      setStatus("Quote deleted.");
      setSelectedQuote(null);
      await load();
    } catch (error) {
      setStatus(error.message || "Could not delete quote.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const loadQuoteDetail = useCallback(async (quote) => {
    try {
      const detail = await api.get(`/api/quotes/${quote.id}`);
      setSelectedQuote(detail);
    } catch (error) {
      setStatus(error.message || "Could not load quote detail.");
    }
  }, [api]);

  const draftTotal = useMemo(
    () => draftItems.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0),
    [draftItems]
  );

  const visibleQuotes = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return quotes;
    return quotes.filter((quote) =>
      [quote.quoteNumber, quote.customerName, quote.customerPhone].some(
        (val) => String(val || "").toLowerCase().includes(q)
      )
    );
  }, [quotes, search]);

  const draftCount = useMemo(() => quotes.filter((q) => q.status === "Draft").length, [quotes]);
  const sentCount = useMemo(() => quotes.filter((q) => q.status === "Sent").length, [quotes]);
  const acceptedCount = useMemo(() => quotes.filter((q) => q.status === "Accepted").length, [quotes]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Quotes / Estimates",
      subtitle: "Create price estimates for customers, then convert them to invoices when accepted.",
      action: h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h("section", { className: "module-summary" },
      h("div", null, h("span", null, "Drafts"), h("strong", null, draftCount)),
      h("div", null, h("span", null, "Sent"), h("strong", null, sentCount)),
      h("div", null, h("span", null, "Accepted"), h("strong", null, acceptedCount))
    ),
    h(StatusLine, { status }),
    h("section", { className: "part-request-layout" },
      h("form", {
        className: "admin-panel request-intake-panel",
        onSubmit: (event) => { event.preventDefault(); createQuote(); }
      },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "New Quote"),
          h("button", { className: "primary-button", type: "submit", disabled: isSaving },
            isSaving ? "Saving..." : "Create Quote"
          )
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "Known customer"),
            h("select", { value: form.customerId, onChange: (e) => chooseCustomer(e.target.value) },
              h("option", { value: "" }, "Walk-in / new customer"),
              customers.map((c) => h("option", { key: c.id, value: c.id }, c.name))
            )
          ),
          h("label", null,
            h("span", null, "Customer name *"),
            h("input", { value: form.customerName, onChange: (e) => setField("customerName", e.target.value), required: true })
          ),
          h("label", null,
            h("span", null, "Phone"),
            h("input", { value: form.customerPhone, onChange: (e) => setField("customerPhone", e.target.value) })
          ),
          h("label", null,
            h("span", null, "Warehouse"),
            h("select", { value: form.warehouseId, onChange: (e) => setField("warehouseId", e.target.value) },
              h("option", { value: "" }, "Choose warehouse"),
              warehouses.map((w) => h("option", { key: w.id, value: w.id }, w.name))
            )
          ),
          h("label", null,
            h("span", null, "Quote date"),
            h("input", { type: "date", value: form.quoteDate, onChange: (e) => setField("quoteDate", e.target.value) })
          ),
          h("label", null,
            h("span", null, "Expires"),
            h("input", { type: "date", value: form.expiryDate, onChange: (e) => setField("expiryDate", e.target.value) })
          ),
          h("label", { className: "invoice-notes-field" },
            h("span", null, "Notes"),
            h("textarea", { value: form.notes, onChange: (e) => setField("notes", e.target.value), rows: 2 })
          )
        ),
        h("div", { className: "invoice-line-builder" },
          h("label", null,
            "Part",
            h("select", { value: selectedPartId, onChange: (e) => choosePart(e.target.value) },
              h("option", { value: "" }, "Choose part"),
              parts.map((part) => h("option", { key: part.id, value: part.id }, partLabel(part)))
            )
          ),
          h("label", null,
            "Qty",
            h("input", { inputMode: "numeric", value: lineQty, onChange: (e) => setLineQty(e.target.value) })
          ),
          h("label", null,
            "Unit price",
            h("input", { inputMode: "decimal", value: linePrice, onChange: (e) => setLinePrice(e.target.value) })
          ),
          h("button", { className: "secondary-button", type: "button", onClick: addLine }, "Add Line")
        ),
        h("div", { className: "invoice-draft-lines" },
          draftItems.length === 0
            ? h("p", { className: "empty-state" }, "No lines yet.")
            : draftItems.map((item) =>
              h("div", { className: "invoice-draft-line", key: item.partId },
                h("div", null,
                  h("strong", null, item.description),
                  h("span", null, `${item.quantity} x ${money(item.unitPrice, "USD")}`)
                ),
                h("div", { className: "row-actions" },
                  h("strong", null, money(item.quantity * item.unitPrice, "USD")),
                  h("button", { type: "button", onClick: () => removeLine(item.partId) }, "Remove")
                )
              )
            ),
          draftItems.length > 0 && h("div", { className: "invoice-draft-total" },
            h("strong", null, `Total: ${money(draftTotal, "USD")}`)
          )
        )
      ),
      h("section", { className: "table-panel part-request-board" },
        h("div", { className: "filters-row" },
          h("input", {
            value: search,
            onChange: (e) => setSearch(e.target.value),
            placeholder: "Search quotes"
          }),
          h("select", { value: statusFilter, onChange: (e) => setStatusFilter(e.target.value) },
            statusOptions.map((s) => h("option", { key: s, value: s }, s))
          )
        ),
        selectedQuote && h("div", { className: "admin-panel", style: { marginBottom: "1rem" } },
          h("div", { className: "admin-panel-header" },
            h("h3", null, `${selectedQuote.quoteNumber} · ${selectedQuote.customerName}`),
            h("button", { type: "button", onClick: () => setSelectedQuote(null) }, "Close")
          ),
          h("div", { className: "invoice-draft-lines" },
            (selectedQuote.items || []).map((item, idx) =>
              h("div", { className: "invoice-draft-line", key: idx },
                h("div", null,
                  h("strong", null, item.description),
                  h("span", null, `${item.quantity} x ${money(item.unitPrice, "USD")}`)
                ),
                h("strong", null, money(item.lineTotal, "USD"))
              )
            ),
            h("div", { className: "invoice-draft-total" },
              h("strong", null, `Total: ${money(selectedQuote.totalAmount, "USD")}`)
            )
          ),
          selectedQuote.notes && h("p", { style: { margin: "0.5rem 0" } }, selectedQuote.notes),
          h("div", { className: "row-actions" },
            selectedQuote.status === "Draft" && h("button", { type: "button", onClick: () => updateQuoteStatus(selectedQuote, "Sent"), disabled: isSaving }, "Mark Sent"),
            selectedQuote.status === "Sent" && h("button", { type: "button", onClick: () => updateQuoteStatus(selectedQuote, "Accepted"), disabled: isSaving }, "Mark Accepted"),
            selectedQuote.status === "Sent" && h("button", { type: "button", onClick: () => updateQuoteStatus(selectedQuote, "Declined"), disabled: isSaving }, "Mark Declined"),
            (selectedQuote.status === "Draft" || selectedQuote.status === "Accepted") &&
              h("button", { className: "primary-button", type: "button", onClick: () => convertToInvoice(selectedQuote), disabled: isSaving || !selectedQuote.warehouseId }, "Convert to Invoice"),
            h("button", { className: "danger-button", type: "button", onClick: () => deleteQuote(selectedQuote), disabled: isSaving }, "Delete")
          )
        ),
        h(DataTable, {
          columns: [
            { key: "number", label: "Quote #", render: (q) => h("strong", null, q.quoteNumber) },
            { key: "customer", label: "Customer", render: (q) => q.customerName },
            { key: "phone", label: "Phone", render: (q) => q.customerPhone || "" },
            { key: "date", label: "Date", render: (q) => q.quoteDate ? new Date(q.quoteDate).toLocaleDateString() : "" },
            { key: "expiry", label: "Expires", render: (q) => q.expiryDate ? new Date(q.expiryDate).toLocaleDateString() : "—" },
            { key: "total", label: "Total", render: (q) => money(q.totalAmount, "USD") },
            { key: "status", label: "Status", render: (q) => h("span", { className: statusBadgeClass(q.status) }, q.status) },
            {
              key: "actions",
              label: "Actions",
              render: (q) => h("div", { className: "row-actions" },
                h("button", { type: "button", onClick: () => loadQuoteDetail(q) }, "View"),
                q.status === "Draft" && h("button", { type: "button", onClick: () => updateQuoteStatus(q, "Sent"), disabled: isSaving }, "Send"),
                (q.status === "Draft" || q.status === "Accepted") &&
                  h("button", { className: "primary-button", type: "button", onClick: () => convertToInvoice(q), disabled: isSaving }, "→ Invoice")
              )
            }
          ],
          rows: visibleQuotes,
          getRowKey: (q) => q.id,
          emptyText: "No quotes found."
        })
      )
    )
  );
}
