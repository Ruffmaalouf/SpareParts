import { h, useState, useEffect, useCallback } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";

const MODE_LIST = "list";
const MODE_CREATE = "create";
const MODE_DETAIL = "detail";

export function SalesReturnsView({ api }) {
  const [mode, setMode] = useState(MODE_LIST);
  const [returns, setReturns] = useState([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [selected, setSelected] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const fetchReturns = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await api.get(`/api/sales-returns?search=${encodeURIComponent(searchQuery)}`);
      setReturns(data || []);
    } catch (e) {
      setError("Failed to load sales returns.");
    } finally {
      setLoading(false);
    }
  }, [searchQuery]);

  useEffect(() => {
    fetchReturns();
  }, [fetchReturns]);

  const openDetail = async (returnId) => {
    setLoading(true);
    setError(null);
    try {
      const data = await api.get(`/api/sales-returns/${returnId}`);
      setSelected(data);
      setMode(MODE_DETAIL);
    } catch (e) {
      setError("Failed to load return details.");
    } finally {
      setLoading(false);
    }
  };

  const backToList = () => {
    setMode(MODE_LIST);
    setSelected(null);
    fetchReturns();
  };

  if (mode === MODE_CREATE) {
    return h(CreateReturnForm, { api, onCreated: backToList, onCancel: () => setMode(MODE_LIST) });
  }

  if (mode === MODE_DETAIL && selected) {
    return h(ReturnDetailPanel, { data: selected, onBack: backToList });
  }

  return h("div", { className: "view-container" },
    h("div", { className: "view-header" },
      h("h2", null, "Sales Returns"),
      h("button", { className: "btn btn-primary", onClick: () => setMode(MODE_CREATE) }, "+ New Return")
    ),
    h("div", { className: "search-bar" },
      h("input", {
        type: "text",
        placeholder: "Search by return # or invoice #…",
        value: searchQuery,
        onChange: (e) => setSearchQuery(e.target.value),
        onKeyDown: (e) => e.key === "Enter" && fetchReturns()
      }),
      h("button", { className: "btn", onClick: fetchReturns }, "Search")
    ),
    error && h("div", { className: "error-banner" }, error),
    loading
      ? h("div", { className: "loading" }, "Loading…")
      : h("table", { className: "data-table" },
          h("thead", null,
            h("tr", null,
              h("th", null, "Return #"),
              h("th", null, "Original Invoice"),
              h("th", null, "Date"),
              h("th", null, "Credit Amount"),
              h("th", null, "Refund Amount")
            )
          ),
          h("tbody", null,
            returns.length === 0
              ? h("tr", null, h("td", { colSpan: 5, style: { textAlign: "center", color: "#888" } }, "No returns found."))
              : returns.map((r) =>
                  h("tr", { key: r.returnId, style: { cursor: "pointer" }, onClick: () => openDetail(r.returnId) },
                    h("td", null, h("strong", null, r.returnNumber)),
                    h("td", null, r.originalInvoiceNumber || `#${r.originalInvoiceId}`),
                    h("td", null, shortDate(r.returnDate)),
                    h("td", null, money(r.totalAmount)),
                    h("td", null, money(r.refundAmount))
                  )
                )
          )
        )
  );
}

function ReturnDetailPanel({ data, onBack }) {
  return h("div", { className: "view-container" },
    h("div", { className: "view-header" },
      h("button", { className: "btn", onClick: onBack }, "← Back"),
      h("h2", null, `Return ${data.returnNumber}`)
    ),
    h("div", { className: "detail-grid" },
      h("div", null,
        h("label", null, "Original Invoice"),
        h("span", null, data.originalInvoiceNumber || `#${data.originalInvoiceId}`)
      ),
      h("div", null,
        h("label", null, "Return Date"),
        h("span", null, shortDate(data.returnDate))
      ),
      h("div", null,
        h("label", null, "Credit Amount"),
        h("span", null, money(data.totalAmount))
      ),
      h("div", null,
        h("label", null, "Refund Amount"),
        h("span", null, money(data.refundAmount))
      ),
      data.reason && h("div", { style: { gridColumn: "1 / -1" } },
        h("label", null, "Reason"),
        h("span", null, data.reason)
      )
    ),
    h("h3", null, "Returned Items"),
    h("table", { className: "data-table" },
      h("thead", null,
        h("tr", null,
          h("th", null, "Part"),
          h("th", null, "Qty"),
          h("th", null, "Unit Price"),
          h("th", null, "Tax Rate"),
          h("th", null, "Line Total")
        )
      ),
      h("tbody", null,
        (data.items || []).map((item, i) =>
          h("tr", { key: i },
            h("td", null, item.description || `Part #${item.partId}`),
            h("td", null, item.quantity),
            h("td", null, money(item.unitPrice)),
            h("td", null, `${item.taxRate}%`),
            h("td", null, money(item.lineTotal))
          )
        )
      )
    )
  );
}

function CreateReturnForm({ api, onCreated, onCancel }) {
  const [step, setStep] = useState(1);
  const [invoiceId, setInvoiceId] = useState("");
  const [invoiceSearch, setInvoiceSearch] = useState("");
  const [invoiceResults, setInvoiceResults] = useState([]);
  const [returnableLines, setReturnableLines] = useState([]);
  const [quantities, setQuantities] = useState({});
  const [returnDate, setReturnDate] = useState(new Date().toISOString().slice(0, 10));
  const [warehouseId, setWarehouseId] = useState("");
  const [reason, setReason] = useState("");
  const [refundAmount, setRefundAmount] = useState("0");
  const [warehouses, setWarehouses] = useState([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    api.get("/api/warehouses").then((data) => setWarehouses(data || [])).catch(() => {});
  }, []);

  const searchInvoices = async () => {
    if (!invoiceSearch.trim()) return;
    try {
      const data = await api.get(`/api/sales?search=${encodeURIComponent(invoiceSearch)}`);
      setInvoiceResults(data || []);
    } catch {
      setError("Failed to search invoices.");
    }
  };

  const selectInvoice = async (id) => {
    setInvoiceId(id);
    setError(null);
    try {
      const lines = await api.get(`/api/sales-returns/returnable/${id}`);
      setReturnableLines(lines || []);
      const initial = {};
      (lines || []).forEach((l) => { initial[l.partId] = 0; });
      setQuantities(initial);
      setStep(2);
    } catch {
      setError("Failed to load returnable lines.");
    }
  };

  const computeCredit = () => {
    return returnableLines.reduce((sum, line) => {
      const qty = parseInt(quantities[line.partId] || 0, 10);
      if (qty <= 0) return sum;
      const net = qty * line.unitPrice;
      const tax = net * (line.taxRate / 100);
      return sum + net + tax;
    }, 0);
  };

  const handleSubmit = async () => {
    setError(null);
    const items = returnableLines
      .filter((l) => parseInt(quantities[l.partId] || 0, 10) > 0)
      .map((l) => ({
        partId: l.partId,
        quantity: parseInt(quantities[l.partId], 10),
        unitPrice: l.unitPrice,
        taxRate: l.taxRate
      }));

    if (items.length === 0) {
      setError("Select at least one item to return.");
      return;
    }

    if (!warehouseId) {
      setError("Please select a warehouse.");
      return;
    }

    const creditAmount = computeCredit();
    const refund = parseFloat(refundAmount) || 0;
    if (refund > creditAmount) {
      setError(`Refund amount cannot exceed credit amount (${money(creditAmount)}).`);
      return;
    }

    setSubmitting(true);
    try {
      await api.post("/api/sales-returns", {
        originalInvoiceId: parseInt(invoiceId, 10),
        returnDate: new Date(returnDate).toISOString(),
        warehouseId: parseInt(warehouseId, 10),
        reason: reason.trim() || null,
        refundAmount: refund,
        items
      });
      onCreated();
    } catch (e) {
      setError(e?.message || "Failed to create return.");
    } finally {
      setSubmitting(false);
    }
  };

  if (step === 1) {
    return h("div", { className: "view-container" },
      h("div", { className: "view-header" },
        h("button", { className: "btn", onClick: onCancel }, "← Cancel"),
        h("h2", null, "New Sales Return — Select Invoice")
      ),
      error && h("div", { className: "error-banner" }, error),
      h("div", { className: "search-bar" },
        h("input", {
          type: "text",
          placeholder: "Search invoice # or scan code…",
          value: invoiceSearch,
          onChange: (e) => setInvoiceSearch(e.target.value),
          onKeyDown: (e) => e.key === "Enter" && searchInvoices()
        }),
        h("button", { className: "btn", onClick: searchInvoices }, "Search")
      ),
      invoiceResults.length > 0 && h("table", { className: "data-table" },
        h("thead", null,
          h("tr", null,
            h("th", null, "Invoice #"),
            h("th", null, "Date"),
            h("th", null, "Total"),
            h("th", null, "")
          )
        ),
        h("tbody", null,
          invoiceResults.map((inv) =>
            h("tr", { key: inv.invoiceId },
              h("td", null, inv.invoiceNumber),
              h("td", null, shortDate(inv.invoiceDate)),
              h("td", null, money(inv.totalAmount)),
              h("td", null,
                h("button", { className: "btn btn-sm", onClick: () => selectInvoice(inv.invoiceId) }, "Select")
              )
            )
          )
        )
      )
    );
  }

  const creditAmount = computeCredit();

  return h("div", { className: "view-container" },
    h("div", { className: "view-header" },
      h("button", { className: "btn", onClick: () => setStep(1) }, "← Back"),
      h("h2", null, "New Sales Return — Select Items")
    ),
    error && h("div", { className: "error-banner" }, error),
    h("div", { className: "form-row" },
      h("label", null, "Return Date"),
      h("input", {
        type: "date",
        value: returnDate,
        onChange: (e) => setReturnDate(e.target.value)
      })
    ),
    h("div", { className: "form-row" },
      h("label", null, "Warehouse"),
      h("select", {
        value: warehouseId,
        onChange: (e) => setWarehouseId(e.target.value)
      },
        h("option", { value: "" }, "-- Select --"),
        warehouses.map((w) => h("option", { key: w.id, value: w.id }, w.name))
      )
    ),
    h("div", { className: "form-row" },
      h("label", null, "Reason (optional)"),
      h("input", {
        type: "text",
        value: reason,
        maxLength: 500,
        onChange: (e) => setReason(e.target.value)
      })
    ),
    h("h3", null, "Returnable Items"),
    h("table", { className: "data-table" },
      h("thead", null,
        h("tr", null,
          h("th", null, "Part"),
          h("th", null, "Unit Price"),
          h("th", null, "Returnable Qty"),
          h("th", null, "Return Qty")
        )
      ),
      h("tbody", null,
        returnableLines.map((line) =>
          h("tr", { key: line.partId },
            h("td", null, line.description || `Part #${line.partId}`),
            h("td", null, money(line.unitPrice)),
            h("td", null, line.returnableQuantity),
            h("td", null,
              h("input", {
                type: "number",
                min: 0,
                max: line.returnableQuantity,
                value: quantities[line.partId] ?? 0,
                style: { width: "70px" },
                onChange: (e) => {
                  const v = Math.min(Math.max(0, parseInt(e.target.value, 10) || 0), line.returnableQuantity);
                  setQuantities({ ...quantities, [line.partId]: v });
                }
              })
            )
          )
        )
      )
    ),
    h("div", { className: "form-row" },
      h("label", null, `Credit Amount: ${money(creditAmount)}`)
    ),
    h("div", { className: "form-row" },
      h("label", null, "Refund Amount (optional)"),
      h("input", {
        type: "number",
        min: 0,
        max: creditAmount,
        step: "0.01",
        value: refundAmount,
        style: { width: "150px" },
        onChange: (e) => setRefundAmount(e.target.value)
      })
    ),
    h("div", { className: "form-actions" },
      h("button", { className: "btn btn-primary", onClick: handleSubmit, disabled: submitting },
        submitting ? "Saving…" : "Create Return"
      ),
      h("button", { className: "btn", onClick: onCancel }, "Cancel")
    )
  );
}
