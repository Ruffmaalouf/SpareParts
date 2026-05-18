import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { asRows, dateTime } from "../core/formatters.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

const statusOptions = ["Active", "Open", "Contacted", "Fulfilled", "Cancelled"];

const emptyForm = {
  customerId: "",
  customerName: "",
  customerPhone: "",
  partId: "",
  requestedPartName: "",
  requestedOemNumber: "",
  vehicleDetails: "",
  quantity: "1",
  notes: ""
};

function readinessLabel(request) {
  if (request.isReadyToContact) return `${request.waitingCustomerCount || 1} waiting`;
  if (request.status === "Fulfilled" || request.status === "Cancelled") return request.status;
  return "Waiting";
}

function matchesRequest(request, query) {
  if (!query) return true;
  return [
    request.customerName,
    request.customerPhone,
    request.requestedPartName,
    request.requestedOemNumber,
    request.vehicleDetails,
    request.partInternalCode,
    request.matchedPartName,
    request.notes
  ].some((value) => String(value || "").toLowerCase().includes(query));
}

export function PartRequestsView({ api }) {
  const [requests, setRequests] = useState([]);
  const [parts, setParts] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [form, setForm] = useState(emptyForm);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("Active");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading part requests...");
    try {
      const statusQuery = statusFilter ? `?status=${encodeURIComponent(statusFilter)}` : "";
      const [nextRequests, nextParts, nextCustomers] = await Promise.all([
        api.get(`/api/partrequests${statusQuery}`),
        api.get("/api/parts?page=1&pageSize=5000"),
        api.get("/api/customers?page=1&pageSize=5000")
      ]);
      setRequests(asRows(nextRequests));
      setParts(asRows(nextParts));
      setCustomers(asRows(nextCustomers));
      setStatus("Part requests loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load part requests.");
    } finally {
      setIsLoading(false);
    }
  }, [api, statusFilter]);

  useEffect(() => { load(); }, [load]);

  const visibleRequests = useMemo(() => {
    const query = search.trim().toLowerCase();
    return requests.filter((request) => matchesRequest(request, query));
  }, [requests, search]);

  const readyCount = useMemo(
    () => requests.filter((request) => request.isReadyToContact).length,
    [requests]
  );
  const activeCount = useMemo(
    () => requests.filter((request) => request.status === "Open" || request.status === "Contacted").length,
    [requests]
  );

  const setField = useCallback((key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  }, []);

  const chooseCustomer = useCallback((customerId) => {
    const customer = customers.find((item) => String(item.id) === String(customerId));
    setForm((current) => ({
      ...current,
      customerId,
      customerName: customer?.name || current.customerName,
      customerPhone: customer?.phone || current.customerPhone
    }));
  }, [customers]);

  const choosePart = useCallback((partId) => {
    const part = parts.find((item) => String(item.id) === String(partId));
    setForm((current) => ({
      ...current,
      partId,
      requestedPartName: part?.name || current.requestedPartName,
      requestedOemNumber: part?.oemNumber || current.requestedOemNumber
    }));
  }, [parts]);

  const createRequest = useCallback(async () => {
    if (!form.customerName.trim() || !form.requestedPartName.trim()) {
      setStatus("Customer and requested part are required.");
      return;
    }

    setIsSaving(true);
    setStatus("Saving part request...");
    try {
      await api.post("/api/partrequests", {
        partId: form.partId ? Number(form.partId) : null,
        customerId: form.customerId ? Number(form.customerId) : null,
        customerName: form.customerName.trim(),
        customerPhone: form.customerPhone.trim() || null,
        requestedPartName: form.requestedPartName.trim(),
        requestedOemNumber: form.requestedOemNumber.trim() || null,
        vehicleDetails: form.vehicleDetails.trim() || null,
        quantity: Math.max(1, Number(form.quantity || 1)),
        notes: form.notes.trim() || null
      });
      setForm(emptyForm);
      setStatus("Part request saved.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not save part request.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, load]);

  const updateStatus = useCallback(async (request, nextStatus) => {
    setIsSaving(true);
    setStatus(`Marking request ${nextStatus.toLowerCase()}...`);
    try {
      await api.put(`/api/partrequests/${request.id}/status`, { status: nextStatus });
      setStatus("Part request updated.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not update part request.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const remove = useCallback(async (request) => {
    if (!window.confirm(`Delete request for ${request.requestedPartName}?`)) return;
    setIsSaving(true);
    try {
      await api.delete(`/api/partrequests/${request.id}`);
      setStatus("Part request deleted.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not delete part request.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Part Requests",
      subtitle: readyCount
        ? `${readyCount} customer(s) are waiting for parts now in stock.`
        : "Capture unavailable-part demand and follow up when stock arrives.",
      action: h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h("section", { className: "module-summary" },
      h("div", null, h("span", null, "Ready to contact"), h("strong", null, readyCount)),
      h("div", null, h("span", null, "Active demand"), h("strong", null, activeCount))
    ),
    h("section", { className: "part-request-layout" },
      h("form", {
        className: "admin-panel request-intake-panel",
        onSubmit: (event) => {
          event.preventDefault();
          createRequest();
        }
      },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "New Request"),
          h("button", { className: "primary-button", type: "submit", disabled: isSaving }, "Save")
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "Known customer"),
            h("select", { value: form.customerId, onChange: (event) => chooseCustomer(event.target.value) },
              h("option", { value: "" }, "Walk-in / new customer"),
              customers.map((customer) => h("option", { key: customer.id, value: customer.id }, customer.name))
            )
          ),
          h("label", null,
            h("span", null, "Customer name"),
            h("input", { value: form.customerName, onChange: (event) => setField("customerName", event.target.value), required: true })
          ),
          h("label", null,
            h("span", null, "Phone"),
            h("input", { value: form.customerPhone, onChange: (event) => setField("customerPhone", event.target.value) })
          ),
          h("label", null,
            h("span", null, "Matched catalog part"),
            h("select", { value: form.partId, onChange: (event) => choosePart(event.target.value) },
              h("option", { value: "" }, "No exact match yet"),
              parts.map((part) => h("option", { key: part.id, value: part.id }, `${part.internalCode} - ${part.name}`))
            )
          ),
          h("label", null,
            h("span", null, "Requested part"),
            h("input", { value: form.requestedPartName, onChange: (event) => setField("requestedPartName", event.target.value), required: true })
          ),
          h("label", null,
            h("span", null, "OEM / reference"),
            h("input", { value: form.requestedOemNumber, onChange: (event) => setField("requestedOemNumber", event.target.value) })
          ),
          h("label", null,
            h("span", null, "Vehicle"),
            h("input", { value: form.vehicleDetails, onChange: (event) => setField("vehicleDetails", event.target.value), placeholder: "BMW E90 2011, left side..." })
          ),
          h("label", null,
            h("span", null, "Quantity"),
            h("input", { type: "number", min: "1", value: form.quantity, onChange: (event) => setField("quantity", event.target.value) })
          ),
          h("label", null,
            h("span", null, "Notes"),
            h("textarea", { value: form.notes, onChange: (event) => setField("notes", event.target.value), rows: 3 })
          )
        )
      ),
      h("section", { className: "table-panel part-request-board" },
        h("div", { className: "filters-row" },
          h("input", { value: search, onChange: (event) => setSearch(event.target.value), placeholder: "Search customer, phone, part, OEM, vehicle" }),
          h("select", { value: statusFilter, onChange: (event) => setStatusFilter(event.target.value) },
            statusOptions.map((option) => h("option", { key: option, value: option }, option))
          )
        ),
        h(StatusLine, { status }),
        h(DataTable, {
          columns: [
            { key: "signal", label: "Signal", render: (request) => h("span", { className: request.isReadyToContact ? "request-signal ready" : "request-signal" }, readinessLabel(request)) },
            { key: "customer", label: "Customer", render: (request) => h("strong", null, request.customerName) },
            { key: "phone", label: "Phone", render: (request) => request.customerPhone || "" },
            { key: "part", label: "Requested", render: (request) => request.requestedPartName },
            { key: "code", label: "Matched", render: (request) => request.partInternalCode || request.matchedPartName || "No match" },
            { key: "available", label: "Available", render: (request) => request.availableQuantity },
            { key: "status", label: "Status", render: (request) => request.status },
            { key: "created", label: "Created", render: (request) => dateTime(request.createdAt) },
            {
              key: "actions",
              label: "Actions",
              render: (request) => h("div", { className: "row-actions" },
                h("button", { type: "button", onClick: () => updateStatus(request, "Contacted"), disabled: isSaving }, "Contacted"),
                h("button", { type: "button", onClick: () => updateStatus(request, "Fulfilled"), disabled: isSaving }, "Fulfilled"),
                h("button", { type: "button", onClick: () => updateStatus(request, "Open"), disabled: isSaving }, "Reopen"),
                h("button", { className: "danger-button", type: "button", onClick: () => updateStatus(request, "Cancelled"), disabled: isSaving }, "Cancel"),
                h("button", { className: "danger-button", type: "button", onClick: () => remove(request), disabled: isSaving }, "Delete")
              )
            }
          ],
          rows: visibleRequests,
          getRowKey: (request) => request.id,
          emptyText: "No part requests match this view."
        })
      )
    )
  );
}
