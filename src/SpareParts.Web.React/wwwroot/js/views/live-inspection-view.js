import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function LiveInspectionView({ api }) {
  const [items, setItems] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [partId, setPartId] = useState("");
  const [buyerName, setBuyerName] = useState("");
  const [buyerPhone, setBuyerPhone] = useState("");
  const [notes, setNotes] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    try { setItems((await api.get("/api/live-inspection")) || []); }
    catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const handleCreate = useCallback(async (e) => {
    e.preventDefault();
    if (!partId || !buyerName) { setStatus("Part ID and buyer name required."); return; }
    try {
      await api.post("/api/live-inspection", { partId: Number(partId), buyerName, buyerPhone: buyerPhone || null, notes: notes || null });
      setStatus("Inspection request created."); setPartId(""); setBuyerName(""); setBuyerPhone(""); setNotes("");
      load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, partId, buyerName, buyerPhone, notes, load]);

  const statusColors = { 1: "#ff9800", 2: "#1976d2", 3: "#d32f2f", 4: "#388e3c", 5: "#999" };

  return h("section", { className: "screen" },
    h(PageHeader, { title: "LiveInspection", subtitle: "Schedule and manage remote live video inspections." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleCreate, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "New Inspection Request"),
        h("button", { type: "submit", className: "primary-button" }, "Request")
      ),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Part ID *"), h("input", { type: "number", value: partId, onChange: e => setPartId(e.target.value), required: true })),
        h("label", null, h("span", null, "Buyer Name *"), h("input", { value: buyerName, onChange: e => setBuyerName(e.target.value), required: true })),
        h("label", null, h("span", null, "Buyer Phone"), h("input", { value: buyerPhone, onChange: e => setBuyerPhone(e.target.value) })),
        h("label", { style: { gridColumn: "1 / -1" } }, h("span", null, "Notes"), h("textarea", { value: notes, onChange: e => setNotes(e.target.value), rows: 2 }))
      )
    ),
    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, `Inspections (${items.length})`)),
      isLoading ? h("p", { style: { padding: "12px" } }, "Loading...") :
      items.length === 0 ? h("p", { style: { padding: "12px", color: "#999" } }, "No inspections yet.") :
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null,
          h("th", null, "ID"), h("th", null, "Part"), h("th", null, "Buyer"), h("th", null, "Status"), h("th", null, "Date")
        )),
        h("tbody", null, items.map(r =>
          h("tr", { key: r.id },
            h("td", null, r.id),
            h("td", null, r.partName || `#${r.partId}`),
            h("td", null, r.buyerName),
            h("td", null, h("span", { style: { color: statusColors[r.status] || "#333" } }, r.statusLabel)),
            h("td", null, new Date(r.createdAt).toLocaleDateString())
          )
        ))
      )
    )
  );
}
