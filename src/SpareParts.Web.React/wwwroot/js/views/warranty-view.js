import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

export function WarrantyView({ api }) {
  const [claims, setClaims] = useState([]);
  const [statusFilter, setStatusFilter] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [showForm, setShowForm] = useState(false);
  const [partId, setPartId] = useState("");
  const [customerId, setCustomerId] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [claimType, setClaimType] = useState("Return");
  const [description, setDescription] = useState("");
  const [selected, setSelected] = useState(null);
  const [resolution, setResolution] = useState("");
  const [resolveStatus, setResolveStatus] = useState("Resolved");
  const [refundAmount, setRefundAmount] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading claims...");
    try {
      const q = statusFilter ? `?status=${statusFilter}` : "";
      setClaims(await api.get(`/api/warranty${q}`));
      setStatus("Claims loaded.");
    } catch (e) {
      setStatus(e.message || "Failed to load.");
    } finally {
      setIsLoading(false);
    }
  }, [api, statusFilter]);

  useEffect(() => { load(); }, [load]);

  const createClaim = useCallback(async () => {
    try {
      await api.post("/api/warranty", {
        partId: Number(partId),
        customerId: customerId ? Number(customerId) : null,
        quantity: Number(quantity),
        claimType,
        description
      });
      setStatus("Claim created.");
      setShowForm(false);
      setPartId(""); setCustomerId(""); setQuantity("1"); setDescription("");
      load();
    } catch (e) {
      setStatus(e.message || "Failed to create claim.");
    }
  }, [api, partId, customerId, quantity, claimType, description, load]);

  const resolveClaim = useCallback(async () => {
    if (!selected) return;
    try {
      await api.put(`/api/warranty/${selected.id}/resolve`, {
        status: resolveStatus,
        resolution,
        refundAmount: refundAmount ? Number(refundAmount) : null
      });
      setStatus("Claim resolved.");
      setSelected(null);
      load();
    } catch (e) {
      setStatus(e.message || "Failed to resolve.");
    }
  }, [api, selected, resolveStatus, resolution, refundAmount, load]);

  const statusBadge = (s) => h("span", { className: `status-badge ${s === "Open" ? "warning" : s === "Resolved" ? "success" : "info"}` }, s);

  return h("div", { className: "view-container" },
    h(PageHeader, { title: "Warranty & Returns" },
      h("button", { onClick: () => setShowForm(!showForm), className: "btn-primary" }, showForm ? "Cancel" : "+ New Claim")
    ),
    h("div", { className: "filter-bar" },
      h("label", null, "Filter: "),
      h("select", { value: statusFilter, onChange: (e) => setStatusFilter(e.target.value) },
        h("option", { value: "" }, "All"),
        h("option", { value: "Open" }, "Open"),
        h("option", { value: "Approved" }, "Approved"),
        h("option", { value: "Rejected" }, "Rejected"),
        h("option", { value: "Resolved" }, "Resolved")
      )
    ),
    showForm && h("div", { className: "form-card" },
      h("h3", null, "New Warranty / Return Claim"),
      h("div", { className: "form-row" },
        h("input", { type: "number", placeholder: "Part ID*", value: partId, onChange: (e) => setPartId(e.target.value) }),
        h("input", { type: "number", placeholder: "Customer ID (optional)", value: customerId, onChange: (e) => setCustomerId(e.target.value) }),
        h("input", { type: "number", placeholder: "Quantity", value: quantity, onChange: (e) => setQuantity(e.target.value) }),
        h("select", { value: claimType, onChange: (e) => setClaimType(e.target.value) },
          h("option", { value: "Return" }, "Return"),
          h("option", { value: "Warranty" }, "Warranty"),
          h("option", { value: "Defective" }, "Defective")
        )
      ),
      h("textarea", { placeholder: "Description", value: description, onChange: (e) => setDescription(e.target.value) }),
      h("button", { onClick: createClaim, className: "btn-primary" }, "Submit Claim")
    ),
    h(StatusLine, { message: status, isLoading }),
    h(DataTable, {
      rows: claims,
      columns: [
        { key: "claimNumber", label: "Claim #" },
        { key: "customerName", label: "Customer" },
        { key: "partName", label: "Part" },
        { key: "claimType", label: "Type" },
        { key: "quantity", label: "Qty" },
        { key: "status", label: "Status", render: statusBadge },
        { key: "refundAmount", label: "Refund" },
        { key: "createdAt", label: "Created", render: (v) => new Date(v).toLocaleDateString() }
      ],
      onSelect: setSelected
    }),
    selected && h("div", { className: "form-card" },
      h("h3", null, `Resolve: ${selected.claimNumber}`),
      h("div", { className: "form-row" },
        h("select", { value: resolveStatus, onChange: (e) => setResolveStatus(e.target.value) },
          h("option", { value: "Approved" }, "Approved"),
          h("option", { value: "Rejected" }, "Rejected"),
          h("option", { value: "Resolved" }, "Resolved")
        ),
        h("input", { type: "number", placeholder: "Refund amount", value: refundAmount, onChange: (e) => setRefundAmount(e.target.value) })
      ),
      h("textarea", { placeholder: "Resolution notes", value: resolution, onChange: (e) => setResolution(e.target.value) }),
      h("button", { onClick: resolveClaim, className: "btn-primary" }, "Save Resolution")
    )
  );
}
