import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const TABS = ["Repair Orders", "New Repair Order"];
const CONDITION_OPTIONS = ["New", "Used", "Any"];
const STATUS_OPTIONS = ["Draft", "Sent", "InProgress", "Completed", "Cancelled"];

const emptyOrderForm = {
  customerName: "",
  customerPhone: "",
  vehicleMake: "",
  vehicleModel: "",
  vehicleYear: "",
  vehicleVin: "",
  notes: "",
  deadline: ""
};

const emptyPartForm = {
  partName: "",
  oemNumber: "",
  quantity: "1",
  conditionPreference: "Any",
  maxBudget: ""
};

function statusTone(status) {
  if (status === "Draft") return "neutral";
  if (status === "Sent") return "info";
  if (status === "InProgress") return "warning";
  if (status === "Completed") return "positive";
  if (status === "Cancelled") return "danger";
  return "neutral";
}

function quoteTone(status) {
  if (status === "Accepted") return "positive";
  if (status === "Rejected") return "danger";
  return "neutral";
}

function RepairOrderCard({ order, onSendToSellers, onAcceptQuote, isSaving }) {
  const [expanded, setExpanded] = useState(false);

  return h("div", { className: "data-card" },
    h("div", { className: "data-card-header" },
      h("strong", null, order.customerName || "Unnamed Customer"),
      h("div", { className: "row-actions" },
        h(Badge, { tone: statusTone(order.status) }, order.status)
      )
    ),
    h("div", { className: "data-card-body" },
      h("div", null, h("span", null, "Vehicle: "), h("strong", null, [order.vehicleYear, order.vehicleMake, order.vehicleModel].filter(Boolean).join(" ") || "—")),
      order.vehicleVin && h("div", null, h("span", null, "VIN: "), h("strong", null, order.vehicleVin)),
      order.customerPhone && h("div", null, h("span", null, "Phone: "), h("strong", null, order.customerPhone)),
      order.deadline && h("div", null, h("span", null, "Deadline: "), h("strong", null, shortDate(order.deadline))),
      order.notes && h("p", null, order.notes),
      order.items && order.items.length > 0 && h("div", null,
        h("small", null, `${order.items.length} part(s) needed`)
      )
    ),
    h("div", { className: "row-actions" },
      order.status === "Draft" && h("button", {
        type: "button",
        className: "primary-button",
        onClick: () => onSendToSellers(order.id),
        disabled: isSaving
      }, "Send to Sellers"),
      h("button", {
        type: "button",
        className: "secondary-button",
        onClick: () => setExpanded(v => !v)
      }, expanded ? "Hide Details" : "View Parts & Quotes")
    ),
    expanded && h("div", { style: { marginTop: "8px" } },
      (order.items || []).map((item, i) =>
        h("div", { key: i, className: "data-card", style: { marginBottom: "6px" } },
          h("div", { className: "data-card-header" },
            h("strong", null, item.partName),
            h(Badge, { tone: item.isFulfilled ? "positive" : "neutral" }, item.isFulfilled ? "Fulfilled" : "Pending")
          ),
          h("div", { className: "data-card-body" },
            item.oemNumber && h("div", null, h("span", null, "OEM: "), h("strong", null, item.oemNumber)),
            h("div", null, h("span", null, "Qty: "), h("strong", null, item.quantity)),
            h("div", null, h("span", null, "Condition: "), h("strong", null, item.conditionPreference)),
            item.maxBudget && h("div", null, h("span", null, "Max Budget: "), h("strong", null, money(item.maxBudget, "USD")))
          ),
          (item.quotes || []).length > 0 && h("div", null,
            h("small", { style: { fontWeight: "bold" } }, "Quotes:"),
            (item.quotes || []).map((q, qi) =>
              h("div", { key: qi, style: { display: "flex", alignItems: "center", gap: "8px", marginTop: "4px" } },
                h("span", null, q.sellerName),
                h("strong", null, money(q.price, q.currency || "USD")),
                h(Badge, { tone: quoteTone(q.status) }, q.status || "Pending"),
                q.status !== "Accepted" && !item.isFulfilled && h("button", {
                  type: "button",
                  className: "primary-button",
                  onClick: () => onAcceptQuote(order.id, item.id, q.id),
                  disabled: isSaving
                }, "Accept")
              )
            )
          )
        )
      )
    )
  );
}

export function MechanicDeskView({ api }) {
  const [activeTab, setActiveTab] = useState("Repair Orders");
  const [orders, setOrders] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [orderForm, setOrderForm] = useState({ ...emptyOrderForm });
  const [parts, setParts] = useState([]);
  const [partForm, setPartForm] = useState({ ...emptyPartForm });

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading repair orders...");
    try {
      const data = await api.get("/api/mechanic-desk");
      setOrders(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load repair orders.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => {
    if (activeTab === "Repair Orders") load();
  }, [activeTab, load]);

  const setOrderField = useCallback((key, value) => {
    setOrderForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const setPartField = useCallback((key, value) => {
    setPartForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const addPart = useCallback(() => {
    if (!partForm.partName.trim()) {
      setStatus("Part name is required.");
      return;
    }
    setParts(prev => [...prev, {
      partName: partForm.partName.trim(),
      oemNumber: partForm.oemNumber.trim() || null,
      quantity: Number(partForm.quantity) || 1,
      conditionPreference: partForm.conditionPreference,
      maxBudget: partForm.maxBudget ? Number(partForm.maxBudget) : null
    }]);
    setPartForm({ ...emptyPartForm });
    setStatus("");
  }, [partForm]);

  const removePart = useCallback((index) => {
    setParts(prev => prev.filter((_, i) => i !== index));
  }, []);

  const handleCreateOrder = useCallback(async (e) => {
    e.preventDefault();
    if (!orderForm.customerName.trim()) {
      setStatus("Customer name is required.");
      return;
    }
    setIsSaving(true);
    setStatus("Creating repair order...");
    try {
      await api.post("/api/mechanic-desk", {
        customerName: orderForm.customerName.trim(),
        customerPhone: orderForm.customerPhone.trim() || null,
        vehicleMake: orderForm.vehicleMake.trim() || null,
        vehicleModel: orderForm.vehicleModel.trim() || null,
        vehicleYear: orderForm.vehicleYear ? Number(orderForm.vehicleYear) : null,
        vehicleVin: orderForm.vehicleVin.trim() || null,
        notes: orderForm.notes.trim() || null,
        deadline: orderForm.deadline || null,
        items: parts
      });
      setOrderForm({ ...emptyOrderForm });
      setParts([]);
      setStatus("Repair order created.");
      setActiveTab("Repair Orders");
    } catch (e) {
      setStatus(e.message || "Could not create repair order.");
    } finally {
      setIsSaving(false);
    }
  }, [api, orderForm, parts]);

  const handleSendToSellers = useCallback(async (id) => {
    setIsSaving(true);
    setStatus("Sending to sellers...");
    try {
      await api.put(`/api/mechanic-desk/${id}/send`);
      setStatus("Sent to sellers.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not send.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const handleAcceptQuote = useCallback(async (orderId, itemId, quoteId) => {
    setIsSaving(true);
    setStatus("Accepting quote...");
    try {
      await api.put(`/api/mechanic-desk/${orderId}/items/${itemId}/quotes/${quoteId}/accept`);
      setStatus("Quote accepted.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not accept quote.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Mechanic Desk",
      subtitle: "Manage repair orders, needed parts, and seller quotes.",
      action: h("button", {
        type: "button",
        className: "secondary-button",
        onClick: load,
        disabled: isLoading
      }, "Refresh")
    }),
    h(StatusLine, { status }),

    h("div", { className: "filters-row" },
      TABS.map(tab =>
        h("button", {
          key: tab,
          type: "button",
          className: activeTab === tab ? "chip active" : "chip",
          onClick: () => setActiveTab(tab)
        }, tab)
      )
    ),

    activeTab === "Repair Orders" && h("section", null,
      isLoading
        ? h("p", null, "Loading...")
        : orders.length === 0
          ? h("p", { className: "empty-state" }, "No repair orders yet. Create your first one.")
          : h("div", { className: "data-card-grid" },
            orders.map(o =>
              h(RepairOrderCard, {
                key: o.id,
                order: o,
                onSendToSellers: handleSendToSellers,
                onAcceptQuote: handleAcceptQuote,
                isSaving
              })
            )
          )
    ),

    activeTab === "New Repair Order" && h("section", null,
      h("form", { className: "admin-panel", onSubmit: handleCreateOrder },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "New Repair Order"),
          h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Create Order")
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "Customer Name *"),
            h("input", { value: orderForm.customerName, onChange: e => setOrderField("customerName", e.target.value), required: true, placeholder: "Full name" })
          ),
          h("label", null,
            h("span", null, "Phone"),
            h("input", { value: orderForm.customerPhone, onChange: e => setOrderField("customerPhone", e.target.value), placeholder: "Contact number" })
          ),
          h("label", null,
            h("span", null, "Vehicle Make"),
            h("input", { value: orderForm.vehicleMake, onChange: e => setOrderField("vehicleMake", e.target.value), placeholder: "e.g. Toyota" })
          ),
          h("label", null,
            h("span", null, "Vehicle Model"),
            h("input", { value: orderForm.vehicleModel, onChange: e => setOrderField("vehicleModel", e.target.value), placeholder: "e.g. Camry" })
          ),
          h("label", null,
            h("span", null, "Year"),
            h("input", { type: "number", value: orderForm.vehicleYear, onChange: e => setOrderField("vehicleYear", e.target.value), placeholder: "e.g. 2019" })
          ),
          h("label", null,
            h("span", null, "VIN"),
            h("input", { value: orderForm.vehicleVin, onChange: e => setOrderField("vehicleVin", e.target.value), placeholder: "Optional" })
          ),
          h("label", null,
            h("span", null, "Deadline"),
            h("input", { type: "date", value: orderForm.deadline, onChange: e => setOrderField("deadline", e.target.value) })
          ),
          h("label", null,
            h("span", null, "Notes"),
            h("textarea", { value: orderForm.notes, onChange: e => setOrderField("notes", e.target.value), rows: 3, placeholder: "Work description or special instructions" })
          )
        ),

        h("div", { style: { marginTop: "16px" } },
          h("h4", null, "Parts Needed"),
          parts.length > 0 && h("div", { style: { marginBottom: "8px" } },
            parts.map((p, i) =>
              h("div", { key: i, style: { display: "flex", alignItems: "center", gap: "8px", marginBottom: "4px" } },
                h("span", null, p.partName),
                h("span", null, `Qty: ${p.quantity}`),
                h("span", null, p.conditionPreference),
                p.maxBudget && h("span", null, money(p.maxBudget, "USD")),
                h("button", { type: "button", className: "danger-button", onClick: () => removePart(i) }, "Remove")
              )
            )
          ),
          h("div", { className: "editor-grid" },
            h("label", null,
              h("span", null, "Part Name *"),
              h("input", { value: partForm.partName, onChange: e => setPartField("partName", e.target.value), placeholder: "e.g. Front brake pads" })
            ),
            h("label", null,
              h("span", null, "OEM Number"),
              h("input", { value: partForm.oemNumber, onChange: e => setPartField("oemNumber", e.target.value), placeholder: "Optional" })
            ),
            h("label", null,
              h("span", null, "Quantity"),
              h("input", { type: "number", value: partForm.quantity, onChange: e => setPartField("quantity", e.target.value), min: "1" })
            ),
            h("label", null,
              h("span", null, "Condition Preference"),
              h("select", { value: partForm.conditionPreference, onChange: e => setPartField("conditionPreference", e.target.value) },
                CONDITION_OPTIONS.map(c => h("option", { key: c, value: c }, c))
              )
            ),
            h("label", null,
              h("span", null, "Max Budget (USD)"),
              h("input", { type: "number", value: partForm.maxBudget, onChange: e => setPartField("maxBudget", e.target.value), placeholder: "Optional" })
            )
          ),
          h("button", { type: "button", className: "secondary-button", onClick: addPart }, "Add Part")
        )
      )
    )
  );
}
