import { React, h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { asRows, dateTime } from "../core/formatters.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

const statusOptions = ["Active", "Open", "Contacted", "Reserved", "Fulfilled", "Cancelled"];
const reservationActionOptions = [
  { value: "AutoRelease", label: "Auto release" },
  { value: "StaffReminder", label: "Remind staff" }
];

function defaultReservationDeadline() {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  date.setHours(18, 0, 0, 0);
  return date;
}

function toLocalDateTimeInputValue(date) {
  const pad = (value) => String(value).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function createEmptyForm() {
  return {
    customerId: "",
    customerName: "",
    customerPhone: "",
    partId: "",
    requestedPartName: "",
    requestedOemNumber: "",
    vehicleDetails: "",
    quantity: "1",
    notes: "",
    reserveOnCreate: false,
    reservationExpiresAt: toLocalDateTimeInputValue(defaultReservationDeadline()),
    reservationExpirationAction: "AutoRelease"
  };
}

function readinessLabel(request) {
  if (request.isReservationReminderDue) return "Reminder due";
  if (request.isReserved) return "Reserved";
  if (request.isReadyToContact) return `${request.waitingCustomerCount || 1} waiting`;
  if (request.status === "Fulfilled" || request.status === "Cancelled") return request.status;
  return "Waiting";
}

function signalClassName(request) {
  if (request.isReservationReminderDue) return "request-signal overdue";
  if (request.isReserved) return "request-signal reserved";
  return request.isReadyToContact ? "request-signal ready" : "request-signal";
}

function reservationClockLabel(request) {
  if (!request.isReserved || !request.reservationExpiresAt) return "";
  const prefix = request.isReservationReminderDue
    ? "Reminder due"
    : request.isReservationOverdue
      ? "Overdue"
      : "Until";
  return `${prefix} ${dateTime(request.reservationExpiresAt)}`;
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
  const [form, setForm] = useState(() => createEmptyForm());
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
        api.list("/api/parts"),
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
  const reservedCount = useMemo(
    () => requests.filter((request) => request.isReserved).length,
    [requests]
  );
  const reminderCount = useMemo(
    () => requests.filter((request) => request.isReservationReminderDue).length,
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

  const buildReservationPayload = useCallback((quantity, expirationAction) => {
    let expiresAt = new Date(form.reservationExpiresAt || toLocalDateTimeInputValue(defaultReservationDeadline()));
    if (Number.isNaN(expiresAt.getTime()) || expiresAt <= new Date()) {
      expiresAt = defaultReservationDeadline();
    }

    return {
      quantity: Math.max(1, Number(quantity || 1)),
      expiresAt: expiresAt.toISOString(),
      expirationAction
    };
  }, [form.reservationExpiresAt]);

  const createRequest = useCallback(async () => {
    if (!form.customerName.trim() || !form.requestedPartName.trim()) {
      setStatus("Customer and requested part are required.");
      return;
    }

    if (form.reserveOnCreate && !form.partId) {
      setStatus("Match a catalog part before starting a reservation clock.");
      return;
    }

    setIsSaving(true);
    setStatus("Saving part request...");
    try {
      const requestId = await api.post("/api/partrequests", {
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
      if (form.reserveOnCreate) {
        await api.post(
          `/api/partrequests/${requestId}/reserve`,
          buildReservationPayload(form.quantity, form.reservationExpirationAction)
        );
      }

      setForm(createEmptyForm());
      setStatus(form.reserveOnCreate ? "Part request saved and reserved." : "Part request saved.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not save part request.");
    } finally {
      setIsSaving(false);
    }
  }, [api, buildReservationPayload, form, load]);

  const reserveRequest = useCallback(async (request, expirationAction) => {
    setIsSaving(true);
    setStatus("Starting reservation clock...");
    try {
      await api.post(
        `/api/partrequests/${request.id}/reserve`,
        buildReservationPayload(request.quantity, expirationAction)
      );
      setStatus(expirationAction === "StaffReminder"
        ? "Reserved until the deadline; staff will be reminded."
        : "Reserved until the deadline; stock will auto-release.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not reserve this request.");
    } finally {
      setIsSaving(false);
    }
  }, [api, buildReservationPayload, load]);

  const releaseReservation = useCallback(async (request) => {
    setIsSaving(true);
    setStatus("Releasing reservation...");
    try {
      await api.post(`/api/partrequests/${request.id}/release-reservation`, { reason: "Released by staff" });
      setStatus("Reservation released.");
      await load();
    } catch (error) {
      setStatus(error.message || "Could not release reservation.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

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
      h("div", null, h("span", null, "Active demand"), h("strong", null, activeCount)),
      h("div", null, h("span", null, "Reserved now"), h("strong", null, reservedCount)),
      h("div", null, h("span", null, "Staff reminders"), h("strong", null, reminderCount))
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
          ),
          h("label", { className: "checkbox-field" },
            h("input", {
              type: "checkbox",
              checked: form.reserveOnCreate,
              onChange: (event) => setField("reserveOnCreate", event.target.checked)
            }),
            h("span", null, "Start reservation clock")
          ),
          h("label", null,
            h("span", null, "Reserved until"),
            h("input", {
              type: "datetime-local",
              value: form.reservationExpiresAt,
              onChange: (event) => setField("reservationExpiresAt", event.target.value)
            })
          ),
          h("label", null,
            h("span", null, "Deadline action"),
            h("select", {
              value: form.reservationExpirationAction,
              onChange: (event) => setField("reservationExpirationAction", event.target.value)
            },
              reservationActionOptions.map((option) => h("option", { key: option.value, value: option.value }, option.label))
            )
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
            { key: "signal", label: "Signal", render: (request) => h("span", { className: signalClassName(request) }, readinessLabel(request)) },
            { key: "customer", label: "Customer", render: (request) => h("strong", null, request.customerName) },
            { key: "phone", label: "Phone", render: (request) => request.customerPhone || "" },
            { key: "part", label: "Requested", render: (request) => request.requestedPartName },
            { key: "code", label: "Matched", render: (request) => request.partInternalCode || request.matchedPartName || "No match" },
            { key: "available", label: "Available", render: (request) => request.availableQuantity },
            { key: "reserved", label: "Reserved", render: (request) => request.reservedQuantity || "" },
            { key: "clock", label: "Clock", render: (request) => reservationClockLabel(request) },
            { key: "status", label: "Status", render: (request) => request.status },
            { key: "created", label: "Created", render: (request) => dateTime(request.createdAt) },
            {
              key: "actions",
              label: "Actions",
              render: (request) => h("div", { className: "row-actions" },
                request.isReserved
                  ? h("button", { type: "button", onClick: () => releaseReservation(request), disabled: isSaving }, "Release")
                  : h(React.Fragment, null,
                    h("button", { type: "button", onClick: () => reserveRequest(request, "AutoRelease"), disabled: isSaving || !request.partId }, "Reserve"),
                    h("button", { type: "button", onClick: () => reserveRequest(request, "StaffReminder"), disabled: isSaving || !request.partId }, "Remind")
                  ),
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
