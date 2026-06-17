import { h, useCallback, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const eventColors = { 1: "#795548", 2: "#1976d2", 3: "#388e3c", 4: "#4caf50", 5: "#9c27b0", 6: "#ff9800", 7: "#d32f2f", 8: "#607d8b" };

export function PartGenealogyView({ api }) {
  const [partId, setPartId] = useState("");
  const [genealogy, setGenealogy] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [eventType, setEventType] = useState("2");
  const [description, setDescription] = useState("");
  const [actorName, setActorName] = useState("");

  const load = useCallback(async () => {
    if (!partId) return;
    setIsLoading(true); setStatus("Loading...");
    try { setGenealogy(await api.get(`/api/part-genealogy/${partId}`)); setStatus(""); }
    catch (err) { setStatus(err.message || "Not found."); }
    finally { setIsLoading(false); }
  }, [api, partId]);

  const addEvent = useCallback(async (e) => {
    e.preventDefault();
    try {
      await api.post("/api/part-genealogy/event", { partId: Number(partId), eventType: Number(eventType), description: description || null, actorName: actorName || null });
      setDescription(""); setStatus("Event added."); load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, partId, eventType, description, actorName, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "PartGenealogy", subtitle: "Complete life history of a part from dismantling to sale." }),
    h(StatusLine, { status }),
    h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Load Part History"),
        h("button", { className: "primary-button", onClick: load, disabled: isLoading }, "Load")
      ),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Part ID *"), h("input", { type: "number", value: partId, onChange: e => setPartId(e.target.value) }))
      )
    ),
    genealogy && h("div", null,
      h("h3", { style: { marginBottom: "8px" } }, genealogy.partName),
      h("div", { style: { position: "relative", paddingLeft: "24px" } },
        genealogy.events.map((ev, i) =>
          h("div", { key: i, style: { display: "flex", gap: "12px", marginBottom: "16px" } },
            h("div", { style: { width: "12px", height: "12px", borderRadius: "50%", background: eventColors[ev.eventType] || "#666", marginTop: "4px", flexShrink: 0 } }),
            h("div", null,
              h("div", { style: { fontWeight: "bold", color: eventColors[ev.eventType] || "#333" } }, ev.eventTypeLabel),
              ev.description && h("p", { style: { margin: "2px 0", fontSize: "14px" } }, ev.description),
              ev.actorName && h("p", { style: { fontSize: "12px", color: "#888" } }, `By: ${ev.actorName}`),
              h("p", { style: { fontSize: "12px", color: "#aaa" } }, new Date(ev.occurredAt || ev.createdAt).toLocaleDateString())
            )
          )
        )
      ),
      h("form", { className: "admin-panel", onSubmit: addEvent, style: { marginTop: "16px" } },
        h("div", { className: "admin-panel-header" }, h("h3", null, "Add Event"), h("button", { type: "submit", className: "primary-button" }, "Add")),
        h("div", { className: "editor-grid" },
          h("label", null, h("span", null, "Event Type"),
            h("select", { value: eventType, onChange: e => setEventType(e.target.value) },
              h("option", { value: "1" }, "Dismantled"), h("option", { value: "2" }, "Listed"),
              h("option", { value: "3" }, "Sold"), h("option", { value: "4" }, "Resold"),
              h("option", { value: "5" }, "Inspected"), h("option", { value: "6" }, "Certified"),
              h("option", { value: "7" }, "Disputed"), h("option", { value: "8" }, "Repaired")
            )
          ),
          h("label", null, h("span", null, "Actor Name"), h("input", { value: actorName, onChange: e => setActorName(e.target.value) })),
          h("label", { style: { gridColumn: "1 / -1" } }, h("span", null, "Description"),
            h("textarea", { value: description, onChange: e => setDescription(e.target.value), rows: 2 })
          )
        )
      )
    )
  );
}
