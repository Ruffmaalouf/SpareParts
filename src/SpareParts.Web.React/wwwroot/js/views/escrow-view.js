import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const STATUS_LABELS = {
  1: "Pending Payment",
  2: "Held",
  3: "Shipped",
  4: "Delivered",
  5: "Buyer Accepted",
  6: "Released",
  7: "Disputed",
  8: "Refunded",
  9: "Cancelled"
};

const STATUS_TONES = {
  1: "warning",
  2: "info",
  3: "info",
  4: "positive",
  5: "positive",
  6: "positive",
  7: "danger",
  8: "neutral",
  9: "muted"
};

const STEP_ORDER = [1, 2, 3, 4, 5, 6];

const emptyForm = {
  buyerName: "",
  buyerPhone: "",
  sellerName: "",
  sellerPhone: "",
  amount: "",
  currency: "USD",
  notes: "",
  partId: ""
};

function StatusSteps({ currentStatus }) {
  return h("div", { className: "filters-row", style: { flexWrap: "wrap", gap: "4px" } },
    STEP_ORDER.map((step, idx) => {
      const isActive = currentStatus === step;
      const isPast = currentStatus > step && currentStatus <= 6;
      const tone = isActive ? STATUS_TONES[step] : (isPast ? "neutral" : "muted");
      return h("span", { key: step, style: { display: "flex", alignItems: "center", gap: "4px" } },
        idx > 0 && h("span", { style: { color: "#aaa" } }, "→"),
        h(Badge, { tone }, STATUS_LABELS[step])
      );
    })
  );
}

function EscrowCard({ tx, onAction }) {
  const [trackingInput, setTrackingInput] = useState("");
  const [showTrackingInput, setShowTrackingInput] = useState(false);
  const [disputeReason, setDisputeReason] = useState("");
  const [showDisputeForm, setShowDisputeForm] = useState(false);
  const [isBusy, setIsBusy] = useState(false);
  const [localStatus, setLocalStatus] = useState("");

  const act = useCallback(async (newStatus, extra) => {
    setIsBusy(true);
    setLocalStatus("Updating...");
    try {
      await onAction(tx.id, { newStatus, ...extra });
      setLocalStatus("Done.");
      setShowTrackingInput(false);
      setShowDisputeForm(false);
    } catch (err) {
      setLocalStatus(err.message || "Action failed.");
    } finally {
      setIsBusy(false);
    }
  }, [tx.id, onAction]);

  return h("div", { className: "data-card" },
    h("div", { className: "data-card-header" },
      h("strong", null, `#${tx.id} — ${tx.buyerName} → ${tx.sellerName}`),
      h("div", { className: "row-actions" },
        h(Badge, { tone: STATUS_TONES[tx.status] || "neutral" }, STATUS_LABELS[tx.status] || `Status ${tx.status}`)
      )
    ),
    h("div", { className: "data-card-body" },
      h(StatusSteps, { currentStatus: tx.status }),
      h("div", { style: { marginTop: "8px" } },
        h("span", null, "Amount: "),
        h("strong", null, money(tx.amount, tx.currency || "USD"))
      ),
      tx.trackingNumber && h("div", null,
        h("span", null, "Tracking: "),
        h("strong", null, tx.trackingNumber)
      ),
      tx.createdAt && h("div", null,
        h("small", null, "Created: " + shortDate(tx.createdAt))
      ),
      tx.notes && h("p", null, tx.notes),

      localStatus && h("p", { className: "status-line" }, localStatus),

      tx.status === 2 && h("div", { style: { marginTop: "8px" } },
        !showTrackingInput
          ? h("button", {
            type: "button", className: "primary-button",
            onClick: () => setShowTrackingInput(true), disabled: isBusy
          }, "Mark Shipped")
          : h("div", { style: { display: "flex", gap: "8px", alignItems: "center", flexWrap: "wrap" } },
            h("input", {
              value: trackingInput,
              onChange: e => setTrackingInput(e.target.value),
              placeholder: "Tracking number",
              style: { flex: 1 }
            }),
            h("button", {
              type: "button", className: "primary-button",
              onClick: () => act(3, { trackingNumber: trackingInput || null }), disabled: isBusy
            }, "Confirm Ship"),
            h("button", {
              type: "button", className: "secondary-button",
              onClick: () => setShowTrackingInput(false), disabled: isBusy
            }, "Cancel")
          )
      ),

      tx.status === 3 && h("button", {
        type: "button", className: "primary-button",
        onClick: () => act(4), disabled: isBusy,
        style: { marginTop: "8px" }
      }, "Mark Delivered"),

      tx.status === 4 && h("div", { className: "row-actions", style: { marginTop: "8px", flexWrap: "wrap" } },
        h("button", {
          type: "button", className: "primary-button",
          onClick: () => act(6), disabled: isBusy
        }, "Accept & Release"),
        !showDisputeForm
          ? h("button", {
            type: "button", className: "danger-button",
            onClick: () => setShowDisputeForm(true), disabled: isBusy
          }, "Raise Dispute")
          : h("div", { style: { display: "flex", gap: "8px", alignItems: "center", flexWrap: "wrap", width: "100%" } },
            h("input", {
              value: disputeReason,
              onChange: e => setDisputeReason(e.target.value),
              placeholder: "Reason for dispute",
              style: { flex: 1 }
            }),
            h("button", {
              type: "button", className: "danger-button",
              onClick: () => act(7, { disputeReason: disputeReason || null }), disabled: isBusy
            }, "Submit Dispute"),
            h("button", {
              type: "button", className: "secondary-button",
              onClick: () => setShowDisputeForm(false), disabled: isBusy
            }, "Cancel")
          )
      ),

      tx.status === 7 && h("div", { className: "row-actions", style: { marginTop: "8px" } },
        h("button", {
          type: "button", className: "primary-button",
          onClick: () => act(6), disabled: isBusy
        }, "Release to Seller"),
        h("button", {
          type: "button", className: "danger-button",
          onClick: () => act(8), disabled: isBusy
        }, "Refund Buyer")
      )
    )
  );
}

export function EscrowView({ api }) {
  const [transactions, setTransactions] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [status, setStatus] = useState("");
  const [form, setForm] = useState({ ...emptyForm });
  const [showForm, setShowForm] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading escrow transactions...");
    try {
      const data = await api.get("/api/escrow");
      setTransactions(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (err) {
      setStatus(err.message || "Could not load transactions.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const setField = useCallback((key, value) => {
    setForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleCreate = useCallback(async (e) => {
    e.preventDefault();
    if (!form.buyerName.trim() || !form.sellerName.trim() || !form.amount) {
      setStatus("Buyer name, seller name, and amount are required.");
      return;
    }
    setIsSaving(true);
    setStatus("Creating escrow transaction...");
    try {
      await api.post("/api/escrow", {
        buyerName: form.buyerName.trim(),
        buyerPhone: form.buyerPhone.trim() || null,
        sellerName: form.sellerName.trim(),
        sellerPhone: form.sellerPhone.trim() || null,
        amount: Number(form.amount),
        currency: form.currency || "USD",
        notes: form.notes.trim() || null,
        partId: form.partId ? Number(form.partId) : null
      });
      setForm({ ...emptyForm });
      setShowForm(false);
      setStatus("Escrow transaction created.");
      await load();
    } catch (err) {
      setStatus(err.message || "Could not create transaction.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, load]);

  const handleAction = useCallback(async (id, payload) => {
    await api.put(`/api/escrow/${id}/status`, payload);
    await load();
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Escrow / Protection",
      subtitle: "Secure buyer-seller transactions held until delivery is confirmed.",
      action: h("button", {
        type: "button",
        className: "primary-button",
        onClick: () => setShowForm(v => !v)
      }, showForm ? "Cancel" : "New Escrow")
    }),
    h(StatusLine, { status }),

    showForm && h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Create Escrow Transaction")
      ),
      h("form", { className: "editor-grid", onSubmit: handleCreate },
        h("label", null,
          h("span", null, "Buyer Name *"),
          h("input", { value: form.buyerName, onChange: e => setField("buyerName", e.target.value), required: true, placeholder: "Buyer name" })
        ),
        h("label", null,
          h("span", null, "Buyer Phone"),
          h("input", { value: form.buyerPhone, onChange: e => setField("buyerPhone", e.target.value), placeholder: "Buyer contact" })
        ),
        h("label", null,
          h("span", null, "Seller Name *"),
          h("input", { value: form.sellerName, onChange: e => setField("sellerName", e.target.value), required: true, placeholder: "Seller name" })
        ),
        h("label", null,
          h("span", null, "Seller Phone"),
          h("input", { value: form.sellerPhone, onChange: e => setField("sellerPhone", e.target.value), placeholder: "Seller contact" })
        ),
        h("label", null,
          h("span", null, "Amount *"),
          h("input", { type: "number", value: form.amount, onChange: e => setField("amount", e.target.value), required: true, placeholder: "0.00", step: "0.01" })
        ),
        h("label", null,
          h("span", null, "Currency"),
          h("select", { value: form.currency, onChange: e => setField("currency", e.target.value) },
            h("option", { value: "USD" }, "USD"),
            h("option", { value: "LBP" }, "LBP"),
            h("option", { value: "EUR" }, "EUR")
          )
        ),
        h("label", null,
          h("span", null, "Part ID (optional)"),
          h("input", { type: "number", value: form.partId, onChange: e => setField("partId", e.target.value), placeholder: "Link to part" })
        ),
        h("label", null,
          h("span", null, "Notes"),
          h("textarea", { value: form.notes, onChange: e => setField("notes", e.target.value), rows: 2, placeholder: "Additional details..." })
        ),
        h("div", { style: { gridColumn: "1 / -1" } },
          h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Create Transaction")
        )
      )
    ),

    isLoading
      ? h("p", null, "Loading...")
      : transactions.length === 0
        ? h("p", { className: "empty-state" }, "No escrow transactions found. Create one to get started.")
        : h("div", { className: "data-card-grid" },
          transactions.map(tx =>
            h(EscrowCard, { key: tx.id, tx, onAction: handleAction })
          )
        )
  );
}
