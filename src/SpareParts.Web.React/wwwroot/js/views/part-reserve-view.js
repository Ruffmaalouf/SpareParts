import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const emptyForm = {
  partId: "",
  partName: "",
  reservedByName: "",
  reservedByPhone: "",
  holdHours: "24",
  notes: ""
};

function statusTone(status) {
  if (status === "Active") return "positive";
  if (status === "Expired") return "neutral";
  if (status === "Released") return "warning";
  if (status === "Fulfilled") return "info";
  return "neutral";
}

function ReservationCard({ reservation, onRelease, isSaving }) {
  return h("div", { className: "data-card" },
    h("div", { className: "data-card-header" },
      h("strong", null, reservation.partName || reservation.partCode || `Part #${reservation.partId}`),
      h(Badge, { tone: statusTone(reservation.status) }, reservation.status)
    ),
    h("div", { className: "data-card-body" },
      h("div", null, h("span", null, "Reserved By: "), h("strong", null, reservation.reservedByName || "—")),
      reservation.reservedByPhone && h("div", null, h("span", null, "Phone: "), h("strong", null, reservation.reservedByPhone)),
      h("div", null, h("span", null, "Quantity: "), h("strong", null, reservation.quantity || 1)),
      reservation.expiresAt && h("div", null, h("span", null, "Expires: "), h("strong", null, shortDate(reservation.expiresAt))),
      reservation.notes && h("p", null, reservation.notes)
    ),
    reservation.status === "Active" && h("div", { className: "row-actions" },
      h("button", {
        type: "button",
        className: "danger-button",
        onClick: () => onRelease(reservation.id),
        disabled: isSaving
      }, "Release")
    )
  );
}

export function PartReserveView({ api }) {
  const [reservations, setReservations] = useState([]);
  const [form, setForm] = useState({ ...emptyForm });
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [showForm, setShowForm] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading reservations...");
    try {
      const data = await api.get("/api/part-reservations");
      setReservations(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load reservations.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const setField = useCallback((key, value) => {
    setForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!form.partId.trim() && !form.partName.trim()) {
      setStatus("Part ID or Part Name is required.");
      return;
    }
    if (!form.reservedByName.trim()) {
      setStatus("Reserved by name is required.");
      return;
    }
    setIsSaving(true);
    setStatus("Creating reservation...");
    try {
      await api.post("/api/part-reservations", {
        partId: form.partId.trim() || null,
        partName: form.partName.trim() || null,
        reservedByName: form.reservedByName.trim(),
        reservedByPhone: form.reservedByPhone.trim() || null,
        holdHours: Number(form.holdHours) || 24,
        notes: form.notes.trim() || null
      });
      setForm({ ...emptyForm });
      setShowForm(false);
      setStatus("Reservation created.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not create reservation.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, load]);

  const handleRelease = useCallback(async (id) => {
    if (!window.confirm("Release this reservation?")) return;
    setIsSaving(true);
    setStatus("Releasing reservation...");
    try {
      await api.put(`/api/part-reservations/${id}/release`);
      setStatus("Reservation released.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not release reservation.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const activeCount = reservations.filter(r => r.status === "Active").length;

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Part Reservations",
      subtitle: "Hold parts for customers with a time-limited reservation.",
      action: h("div", { className: "row-actions" },
        h("button", { type: "button", className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh"),
        h("button", {
          type: "button",
          className: "primary-button",
          onClick: () => setShowForm(v => !v)
        }, showForm ? "Cancel" : "Reserve a Part")
      )
    }),

    activeCount > 0 && h("div", { className: "module-summary" },
      h("div", null,
        h("span", null, "Active Holds"),
        h("strong", null, `${activeCount} active reservation${activeCount !== 1 ? "s" : ""}`)
      )
    ),

    h(StatusLine, { status }),

    showForm && h("form", { className: "admin-panel", onSubmit: handleSubmit, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Reserve a Part"),
        h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Create Reservation")
      ),
      h("div", { className: "editor-grid" },
        h("label", null,
          h("span", null, "Part ID"),
          h("input", { value: form.partId, onChange: e => setField("partId", e.target.value), placeholder: "Internal part ID" })
        ),
        h("label", null,
          h("span", null, "Part Name"),
          h("input", { value: form.partName, onChange: e => setField("partName", e.target.value), placeholder: "e.g. Front brake caliper" })
        ),
        h("label", null,
          h("span", null, "Reserved By (Name) *"),
          h("input", { value: form.reservedByName, onChange: e => setField("reservedByName", e.target.value), required: true, placeholder: "Customer name" })
        ),
        h("label", null,
          h("span", null, "Phone"),
          h("input", { value: form.reservedByPhone, onChange: e => setField("reservedByPhone", e.target.value), placeholder: "Customer phone" })
        ),
        h("label", null,
          h("span", null, "Hold Duration (Hours)"),
          h("input", { type: "number", value: form.holdHours, onChange: e => setField("holdHours", e.target.value), min: "1", placeholder: "24" })
        ),
        h("label", null,
          h("span", null, "Notes"),
          h("textarea", { value: form.notes, onChange: e => setField("notes", e.target.value), rows: 2, placeholder: "Optional notes" })
        )
      )
    ),

    isLoading
      ? h("p", null, "Loading...")
      : reservations.length === 0
        ? h("p", { className: "empty-state" }, "No reservations found.")
        : h("div", { className: "data-card-grid" },
          reservations.map(r =>
            h(ReservationCard, {
              key: r.id,
              reservation: r,
              onRelease: handleRelease,
              isSaving
            })
          )
        )
  );
}
