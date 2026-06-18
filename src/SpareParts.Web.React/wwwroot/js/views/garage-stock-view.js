import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const FILTER_OPTIONS = ["All", "Low Stock"];

const emptyForm = {
  name: "",
  category: "",
  barcode: "",
  oemNumber: "",
  quantity: "",
  minimumQuantity: "",
  notes: ""
};

function isLow(item) {
  return Number(item.minimumQuantity) > 0 && Number(item.quantity) <= Number(item.minimumQuantity);
}

function StockCard({ item, onEditQty, onEdit, onDelete, isSaving }) {
  const [newQty, setNewQty] = useState(String(item.quantity));
  const [editing, setEditing] = useState(false);
  const low = isLow(item);

  return h("div", { className: "data-card" },
    h("div", { className: "data-card-header" },
      h("strong", null, item.itemName),
      h("div", { className: "row-actions" },
        low && h(Badge, { tone: "danger" }, "Low Stock"),
        item.category && h(Badge, { tone: "neutral" }, item.category)
      )
    ),
    h("div", { className: "data-card-body" },
      h("div", null, h("span", null, "Qty: "), h("strong", null, item.quantity)),
      item.minimumQuantity > 0 && h("div", null, h("span", null, "Min Qty: "), h("strong", null, item.minimumQuantity)),
      item.oemNumber && h("div", null, h("span", null, "OEM: "), h("strong", null, item.oemNumber)),
      item.barcode && h("div", null, h("span", null, "Barcode: "), h("strong", null, item.barcode)),
      item.notes && h("p", null, item.notes),
      editing && h("div", { className: "form-row", style: { marginTop: "6px" } },
        h("input", {
          type: "number",
          value: newQty,
          onChange: e => setNewQty(e.target.value),
          style: { width: "80px" }
        }),
        h("button", {
          type: "button",
          className: "primary-button",
          onClick: () => {
            onEditQty(item.id, Number(newQty));
            setEditing(false);
          },
          disabled: isSaving
        }, "Save"),
        h("button", { type: "button", className: "secondary-button", onClick: () => setEditing(false) }, "Cancel")
      )
    ),
    h("div", { className: "row-actions" },
      !editing && h("button", {
        type: "button",
        className: "secondary-button",
        onClick: () => { setNewQty(String(item.quantity)); setEditing(true); }
      }, "Update Qty"),
      h("button", {
        type: "button",
        className: "secondary-button",
        onClick: () => onEdit(item)
      }, "Edit"),
      h("button", {
        type: "button",
        className: "danger-button",
        onClick: () => onDelete(item.id),
        disabled: isSaving
      }, "Delete")
    )
  );
}

export function GarageStockView({ api }) {
  const [items, setItems] = useState([]);
  const [filter, setFilter] = useState("All");
  const [form, setForm] = useState({ ...emptyForm });
  const [editingId, setEditingId] = useState(null);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading stock...");
    try {
      const data = await api.get("/api/garage-stock");
      setItems(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load stock.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const setField = useCallback((key, value) => {
    setForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleEdit = useCallback((item) => {
    setEditingId(item.id);
    setForm({
      name: item.itemName || "",
      category: item.category || "",
      barcode: item.barcode || "",
      oemNumber: item.oemNumber || "",
      quantity: String(item.quantity ?? ""),
      minimumQuantity: String(item.minimumQuantity ?? ""),
      notes: item.notes || ""
    });
  }, []);

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!form.name.trim()) {
      setStatus("Item name is required.");
      return;
    }
    setIsSaving(true);
    setStatus(editingId ? "Updating item..." : "Adding item...");
    try {
      const payload = {
        itemName: form.name.trim(),
        category: form.category.trim() || null,
        barcode: form.barcode.trim() || null,
        oemNumber: form.oemNumber.trim() || null,
        quantity: form.quantity !== "" ? Number(form.quantity) : 0,
        minimumQuantity: form.minimumQuantity !== "" ? Number(form.minimumQuantity) : 0,
        notes: form.notes.trim() || null
      };
      if (editingId) {
        await api.put(`/api/garage-stock/${editingId}`, payload);
      } else {
        await api.post("/api/garage-stock", payload);
      }
      setForm({ ...emptyForm });
      setEditingId(null);
      setStatus(editingId ? "Item updated." : "Item added.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not save item.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, editingId, load]);

  const handleUpdateQty = useCallback(async (id, qty) => {
    setIsSaving(true);
    setStatus("Updating quantity...");
    try {
      await api.put(`/api/garage-stock/${id}/quantity`, { quantity: qty });
      setStatus("Quantity updated.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not update quantity.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const handleDelete = useCallback(async (id) => {
    if (!window.confirm("Delete this stock item?")) return;
    setIsSaving(true);
    setStatus("Deleting item...");
    try {
      await api.delete(`/api/garage-stock/${id}`);
      setStatus("Item deleted.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not delete item.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const lowCount = items.filter(isLow).length;
  const displayed = filter === "Low Stock" ? items.filter(isLow) : items;

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Garage Stock",
      subtitle: "Track your internal parts inventory.",
      action: h("button", { type: "button", className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh")
    }),

    lowCount > 0 && h("div", { className: "module-summary" },
      h("div", null,
        h("span", null, "Low Stock Alert"),
        h("strong", null, `${lowCount} item${lowCount !== 1 ? "s" : ""} below minimum`)
      )
    ),

    h(StatusLine, { status }),

    h("div", { className: "filters-row" },
      FILTER_OPTIONS.map(f =>
        h("button", {
          key: f,
          type: "button",
          className: filter === f ? "chip active" : "chip",
          onClick: () => setFilter(f)
        }, f)
      )
    ),

    h("section", { className: "part-request-layout" },
      h("form", { className: "admin-panel request-intake-panel", onSubmit: handleSubmit },
        h("div", { className: "admin-panel-header" },
          h("h3", null, editingId ? "Edit Item" : "Add Stock Item"),
          h("div", { className: "row-actions" },
            editingId && h("button", {
              type: "button",
              className: "secondary-button",
              onClick: () => { setEditingId(null); setForm({ ...emptyForm }); }
            }, "Cancel"),
            h("button", { type: "submit", className: "primary-button", disabled: isSaving },
              editingId ? "Update" : "Add Item")
          )
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "Name *"),
            h("input", { value: form.name, onChange: e => setField("name", e.target.value), required: true, placeholder: "e.g. Engine Oil Filter" })
          ),
          h("label", null,
            h("span", null, "Category"),
            h("input", { value: form.category, onChange: e => setField("category", e.target.value), placeholder: "e.g. Filters" })
          ),
          h("label", null,
            h("span", null, "Barcode"),
            h("input", { value: form.barcode, onChange: e => setField("barcode", e.target.value), placeholder: "Optional" })
          ),
          h("label", null,
            h("span", null, "OEM Number"),
            h("input", { value: form.oemNumber, onChange: e => setField("oemNumber", e.target.value), placeholder: "Optional" })
          ),
          h("label", null,
            h("span", null, "Quantity"),
            h("input", { type: "number", value: form.quantity, onChange: e => setField("quantity", e.target.value), min: "0", placeholder: "0" })
          ),
          h("label", null,
            h("span", null, "Minimum Quantity"),
            h("input", { type: "number", value: form.minimumQuantity, onChange: e => setField("minimumQuantity", e.target.value), min: "0", placeholder: "0" })
          ),
          h("label", null,
            h("span", null, "Notes"),
            h("textarea", { value: form.notes, onChange: e => setField("notes", e.target.value), rows: 2, placeholder: "Optional notes" })
          )
        )
      ),

      h("section", { className: "table-panel" },
        isLoading
          ? h("p", null, "Loading...")
          : displayed.length === 0
            ? h("p", { className: "empty-state" }, filter === "Low Stock" ? "No low-stock items." : "No stock items yet.")
            : h("div", { className: "data-card-grid" },
              displayed.map(item =>
                h(StockCard, {
                  key: item.id,
                  item,
                  onEditQty: handleUpdateQty,
                  onEdit: handleEdit,
                  onDelete: handleDelete,
                  isSaving
                })
              )
            )
      )
    )
  );
}
