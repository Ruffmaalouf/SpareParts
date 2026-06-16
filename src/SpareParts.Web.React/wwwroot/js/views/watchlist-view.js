import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, DataTable, PageHeader, StatusLine } from "../components/shared.js";

const emptyForm = {
  partName: "",
  vehicleMake: "",
  vehicleModel: "",
  vehicleYear: "",
  maxBudget: "",
  currency: "USD",
  notes: ""
};

export function WatchlistView({ api }) {
  const [items, setItems] = useState([]);
  const [matchCounts, setMatchCounts] = useState({});
  const [form, setForm] = useState({ ...emptyForm });
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading watchlist...");
    try {
      const data = await api.get("/api/watchlist");
      const rows = Array.isArray(data) ? data : [];
      setItems(rows);
      setStatus("");

      const counts = {};
      await Promise.all(
        rows.map(async (item) => {
          try {
            const count = await api.get(`/api/watchlist/${item.id}/matches`);
            counts[item.id] = typeof count === "number" ? count : 0;
          } catch {
            counts[item.id] = 0;
          }
        })
      );
      setMatchCounts({ ...counts });
    } catch (e) {
      setStatus(e.message || "Could not load watchlist.");
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
    if (!form.partName.trim()) {
      setStatus("Part name is required.");
      return;
    }
    setIsSaving(true);
    setStatus("Saving watchlist item...");
    try {
      await api.post("/api/watchlist", {
        partName: form.partName.trim(),
        vehicleMake: form.vehicleMake.trim() || null,
        vehicleModel: form.vehicleModel.trim() || null,
        vehicleYear: form.vehicleYear ? Number(form.vehicleYear) : null,
        maxBudget: form.maxBudget ? Number(form.maxBudget) : null,
        currency: form.currency || "USD",
        notes: form.notes.trim() || null
      });
      setForm({ ...emptyForm });
      setStatus("Item added to watchlist.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not save watchlist item.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, load]);

  const handleDelete = useCallback(async (id) => {
    if (!window.confirm("Remove this item from your watchlist?")) return;
    setIsSaving(true);
    setStatus("Removing item...");
    try {
      await api.delete(`/api/watchlist/${id}`);
      setStatus("Item removed.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not remove item.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "WatchPart",
      subtitle: "Track parts you want to buy. We alert you when matches arrive.",
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: load,
        disabled: isLoading
      }, "Refresh")
    }),
    h(StatusLine, { status }),

    h("section", { className: "part-request-layout" },
      h("form", { className: "admin-panel request-intake-panel", onSubmit: handleSubmit },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "Watch a Part"),
          h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Add")
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "Part Name *"),
            h("input", {
              value: form.partName,
              onChange: e => setField("partName", e.target.value),
              required: true,
              placeholder: "e.g. Alternator"
            })
          ),
          h("label", null,
            h("span", null, "Vehicle Make"),
            h("input", {
              value: form.vehicleMake,
              onChange: e => setField("vehicleMake", e.target.value),
              placeholder: "e.g. Honda"
            })
          ),
          h("label", null,
            h("span", null, "Vehicle Model"),
            h("input", {
              value: form.vehicleModel,
              onChange: e => setField("vehicleModel", e.target.value),
              placeholder: "e.g. Civic"
            })
          ),
          h("label", null,
            h("span", null, "Vehicle Year"),
            h("input", {
              type: "number",
              value: form.vehicleYear,
              onChange: e => setField("vehicleYear", e.target.value),
              placeholder: "e.g. 2015"
            })
          ),
          h("label", null,
            h("span", null, "Max Budget"),
            h("input", {
              type: "number",
              value: form.maxBudget,
              onChange: e => setField("maxBudget", e.target.value),
              placeholder: "e.g. 150"
            })
          ),
          h("label", null,
            h("span", null, "Currency"),
            h("select", { value: form.currency, onChange: e => setField("currency", e.target.value) },
              h("option", { value: "USD" }, "USD"),
              h("option", { value: "LBP" }, "LBP")
            )
          ),
          h("label", null,
            h("span", null, "Notes"),
            h("textarea", {
              value: form.notes,
              onChange: e => setField("notes", e.target.value),
              rows: 2,
              placeholder: "Any specific requirements..."
            })
          )
        )
      ),

      h("section", { className: "table-panel" },
        isLoading
          ? h("p", null, "Loading...")
          : h(DataTable, {
            columns: [
              { key: "partName", label: "Part", render: row => h("strong", null, row.partName) },
              {
                key: "vehicle",
                label: "Vehicle",
                render: row => [row.vehicleYear, row.vehicleMake, row.vehicleModel].filter(Boolean).join(" ") || "—"
              },
              {
                key: "maxBudget",
                label: "Max Budget",
                render: row => row.maxBudget ? money(row.maxBudget, row.currency || "USD") : "—"
              },
              {
                key: "matches",
                label: "Matches",
                render: row => {
                  const count = matchCounts[row.id];
                  if (count === undefined) return h("span", null, "...");
                  return h(Badge, { tone: count > 0 ? "positive" : "neutral" },
                    count > 0 ? `${count} match${count === 1 ? "" : "es"}` : "0 matches"
                  );
                }
              },
              {
                key: "dateAdded",
                label: "Added",
                render: row => row.createdAt ? shortDate(row.createdAt) : "—"
              },
              {
                key: "actions",
                label: "",
                render: row => h("button", {
                  type: "button",
                  className: "danger-button",
                  onClick: () => handleDelete(row.id),
                  disabled: isSaving
                }, "Remove")
              }
            ],
            rows: items,
            getRowKey: row => row.id,
            emptyText: "Your watchlist is empty. Add parts you are looking for."
          })
      )
    )
  );
}
