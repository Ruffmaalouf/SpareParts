import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

export function ShipmentsView({ api }) {
  const [shipments, setShipments] = useState([]);
  const [statusFilter, setStatusFilter] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [selected, setSelected] = useState(null);
  const [detail, setDetail] = useState(null);
  const [newStatus, setNewStatus] = useState("InTransit");
  const [location, setLocation] = useState("");
  const [notes, setNotes] = useState("");
  const [showForm, setShowForm] = useState(false);
  const [invoiceId, setInvoiceId] = useState("");
  const [carrier, setCarrier] = useState("");
  const [tracking, setTracking] = useState("");
  const [address, setAddress] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const q = statusFilter ? `?status=${statusFilter}` : "";
      setShipments(await api.get(`/api/shipments${q}`));
      setStatus("Shipments loaded.");
    } catch (e) {
      setStatus(e.message || "Failed to load.");
    } finally {
      setIsLoading(false);
    }
  }, [api, statusFilter]);

  useEffect(() => { load(); }, [load]);

  const viewDetail = useCallback(async (row) => {
    setSelected(row);
    try {
      setDetail(await api.get(`/api/shipments/${row.id}`));
    } catch (e) {
      setStatus(e.message || "Failed to load detail.");
    }
  }, [api]);

  const updateStatus = useCallback(async () => {
    if (!selected) return;
    try {
      await api.put(`/api/shipments/${selected.id}/status`, {
        status: newStatus,
        location,
        notes,
        actualDelivery: newStatus === "Delivered" ? new Date().toISOString() : null
      });
      setStatus("Status updated.");
      setSelected(null); setDetail(null);
      load();
    } catch (e) {
      setStatus(e.message || "Failed to update.");
    }
  }, [api, selected, newStatus, location, notes, load]);

  const createShipment = useCallback(async () => {
    try {
      await api.post("/api/shipments", {
        salesInvoiceId: invoiceId ? Number(invoiceId) : null,
        carrierName: carrier,
        trackingNumber: tracking,
        deliveryAddress: address,
        notes
      });
      setStatus("Shipment created.");
      setShowForm(false);
      setInvoiceId(""); setCarrier(""); setTracking(""); setAddress(""); setNotes("");
      load();
    } catch (e) {
      setStatus(e.message || "Failed to create shipment.");
    }
  }, [api, invoiceId, carrier, tracking, address, notes, load]);

  const statusBadge = (s) => {
    const cls = { Pending: "info", Dispatched: "warning", InTransit: "primary", Delivered: "success", Returned: "danger" }[s] || "";
    return h("span", { className: `status-badge ${cls}` }, s);
  };

  return h("div", { className: "view-container" },
    h(PageHeader, { title: "Shipment Tracking" },
      h("button", { onClick: () => setShowForm(!showForm), className: "btn-primary" }, showForm ? "Cancel" : "+ New Shipment")
    ),
    h("div", { className: "filter-bar" },
      h("label", null, "Status: "),
      h("select", { value: statusFilter, onChange: (e) => setStatusFilter(e.target.value) },
        h("option", { value: "" }, "All"),
        ["Pending", "Dispatched", "InTransit", "Delivered", "Returned"].map((s) =>
          h("option", { key: s, value: s }, s)
        )
      )
    ),
    showForm && h("div", { className: "form-card" },
      h("h3", null, "Create Shipment"),
      h("div", { className: "form-row" },
        h("input", { type: "number", placeholder: "Invoice ID (optional)", value: invoiceId, onChange: (e) => setInvoiceId(e.target.value) }),
        h("input", { type: "text", placeholder: "Carrier", value: carrier, onChange: (e) => setCarrier(e.target.value) }),
        h("input", { type: "text", placeholder: "Tracking number", value: tracking, onChange: (e) => setTracking(e.target.value) })
      ),
      h("input", { type: "text", placeholder: "Delivery address", value: address, onChange: (e) => setAddress(e.target.value) }),
      h("button", { onClick: createShipment, className: "btn-primary" }, "Create")
    ),
    h(StatusLine, { message: status, isLoading }),
    h(DataTable, {
      rows: shipments,
      columns: [
        { key: "shipmentNumber", label: "Shipment #" },
        { key: "invoiceNumber", label: "Invoice" },
        { key: "customerName", label: "Customer" },
        { key: "status", label: "Status", render: statusBadge },
        { key: "carrierName", label: "Carrier" },
        { key: "trackingNumber", label: "Tracking" },
        { key: "estimatedDelivery", label: "ETA", render: (v) => v ? new Date(v).toLocaleDateString() : "" },
        { key: "createdAt", label: "Created", render: (v) => new Date(v).toLocaleDateString() }
      ],
      onSelect: viewDetail
    }),
    selected && h("div", { className: "form-card" },
      h("h3", null, `Update: ${selected.shipmentNumber}`),
      h("div", { className: "form-row" },
        h("select", { value: newStatus, onChange: (e) => setNewStatus(e.target.value) },
          ["Pending", "Dispatched", "InTransit", "Delivered", "Returned"].map((s) =>
            h("option", { key: s, value: s }, s)
          )
        ),
        h("input", { type: "text", placeholder: "Location", value: location, onChange: (e) => setLocation(e.target.value) }),
        h("input", { type: "text", placeholder: "Notes", value: notes, onChange: (e) => setNotes(e.target.value) })
      ),
      h("button", { onClick: updateStatus, className: "btn-primary" }, "Update Status"),
      detail && detail.events && h("div", null,
        h("h4", null, "Event History"),
        detail.events.map((ev, i) =>
          h("div", { key: i, className: "event-row" },
            h("span", { className: "event-status" }, ev.status),
            ev.location && h("span", null, ` @ ${ev.location}`),
            h("span", { className: "event-time" }, ` — ${new Date(ev.eventAt).toLocaleString()}`),
            ev.notes && h("span", { className: "event-notes" }, ` — ${ev.notes}`)
          )
        )
      )
    )
  );
}
