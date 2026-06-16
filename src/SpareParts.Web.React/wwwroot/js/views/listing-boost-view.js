import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const DURATION_OPTIONS = [
  { value: "7", label: "7 days" },
  { value: "14", label: "14 days" },
  { value: "30", label: "30 days" }
];

const emptyForm = {
  partId: "",
  durationDays: "7",
  fee: "",
  paymentReference: ""
};

function boostStatusTone(status) {
  const s = (status || "").toLowerCase();
  if (s === "active") return "positive";
  if (s === "expired") return "muted";
  if (s === "cancelled") return "danger";
  return "neutral";
}

function daysRemaining(expiresAt) {
  if (!expiresAt) return null;
  const ms = new Date(expiresAt).getTime() - Date.now();
  const days = Math.ceil(ms / 86400000);
  return days > 0 ? days : 0;
}

export function ListingBoostView({ api }) {
  const [boosts, setBoosts] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isCancelling, setIsCancelling] = useState(null);
  const [status, setStatus] = useState("");
  const [form, setForm] = useState({ ...emptyForm });
  const [showForm, setShowForm] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading boosts...");
    try {
      const data = await api.get("/api/listing-boosts");
      setBoosts(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (err) {
      setStatus(err.message || "Could not load listing boosts.");
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
    if (!form.partId) {
      setStatus("Part ID is required.");
      return;
    }
    setIsSaving(true);
    setStatus("Creating boost...");
    try {
      await api.post("/api/listing-boosts", {
        partId: Number(form.partId),
        durationDays: Number(form.durationDays),
        fee: form.fee ? Number(form.fee) : null,
        paymentReference: form.paymentReference.trim() || null
      });
      setForm({ ...emptyForm });
      setShowForm(false);
      setStatus("Boost created successfully.");
      await load();
    } catch (err) {
      setStatus(err.message || "Could not create boost.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, load]);

  const handleCancel = useCallback(async (id) => {
    if (!window.confirm("Cancel this boost?")) return;
    setIsCancelling(id);
    try {
      await api.put(`/api/listing-boosts/${id}/cancel`);
      setStatus("Boost cancelled.");
      await load();
    } catch (err) {
      setStatus(err.message || "Could not cancel boost.");
    } finally {
      setIsCancelling(null);
    }
  }, [api, load]);

  const activeCount = boosts.filter(b => (b.status || "").toLowerCase() === "active").length;

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Boost Listings",
      subtitle: "Promote parts to the top of search results for increased visibility.",
      action: h("button", {
        type: "button",
        className: "primary-button",
        onClick: () => setShowForm(v => !v)
      }, showForm ? "Cancel" : "New Boost")
    }),
    h(StatusLine, { status }),

    h("div", { className: "data-card", style: { marginBottom: "16px" } },
      h("div", { className: "data-card-body" },
        h("strong", null, `Active Boosts: ${activeCount}`)
      )
    ),

    showForm && h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Create New Boost")
      ),
      h("form", { className: "editor-grid", onSubmit: handleCreate },
        h("label", null,
          h("span", null, "Part ID *"),
          h("input", {
            type: "number",
            value: form.partId,
            onChange: e => setField("partId", e.target.value),
            required: true,
            placeholder: "Enter part ID to boost"
          })
        ),
        h("label", null,
          h("span", null, "Duration"),
          h("select", { value: form.durationDays, onChange: e => setField("durationDays", e.target.value) },
            DURATION_OPTIONS.map(d => h("option", { key: d.value, value: d.value }, d.label))
          )
        ),
        h("label", null,
          h("span", null, "Fee (optional)"),
          h("input", {
            type: "number",
            value: form.fee,
            onChange: e => setField("fee", e.target.value),
            placeholder: "0.00",
            step: "0.01"
          })
        ),
        h("label", null,
          h("span", null, "Payment Reference (optional)"),
          h("input", {
            value: form.paymentReference,
            onChange: e => setField("paymentReference", e.target.value),
            placeholder: "Receipt or transaction ref"
          })
        ),
        h("div", { style: { gridColumn: "1 / -1" } },
          h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Activate Boost")
        )
      )
    ),

    isLoading
      ? h("p", null, "Loading...")
      : boosts.length === 0
        ? h("p", { className: "empty-state" }, "No listing boosts yet. Create one to promote a part.")
        : h("div", { className: "data-card-grid" },
          boosts.map(boost =>
            h("div", { key: boost.id, className: "data-card" },
              h("div", { className: "data-card-header" },
                h("strong", null, boost.partName || boost.partCode || `Part #${boost.partId}`),
                h("div", { className: "row-actions" },
                  h(Badge, { tone: boostStatusTone(boost.status) }, boost.status || "Unknown")
                )
              ),
              h("div", { className: "data-card-body" },
                boost.startsAt && h("div", null,
                  h("span", null, "Starts: "),
                  h("strong", null, shortDate(boost.startsAt))
                ),
                boost.expiresAt && h("div", null,
                  h("span", null, "Expires: "),
                  h("strong", null, shortDate(boost.expiresAt))
                ),
                (boost.status || "").toLowerCase() === "active" && boost.expiresAt && h("div", null,
                  h("span", null, "Days Remaining: "),
                  h("strong", null, daysRemaining(boost.expiresAt))
                ),
                boost.fee != null && h("div", null,
                  h("span", null, "Fee: "),
                  h("strong", null, money(boost.fee, "USD"))
                ),
                (boost.status || "").toLowerCase() === "active" && h("div", { className: "row-actions", style: { marginTop: "8px" } },
                  h("button", {
                    type: "button",
                    className: "danger-button",
                    onClick: () => handleCancel(boost.id),
                    disabled: isCancelling === boost.id
                  }, "Cancel Boost")
                )
              )
            )
          )
        )
  );
}
