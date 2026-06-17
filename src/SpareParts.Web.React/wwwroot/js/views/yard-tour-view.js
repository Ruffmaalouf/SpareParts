import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const statusColors = { Scheduled: "#1976d2", Live: "#d32f2f", Completed: "#388e3c", Cancelled: "#999" };

export function YardTourView({ api }) {
  const [tours, setTours] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [scheduledAt, setScheduledAt] = useState("");
  const [streamUrl, setStreamUrl] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    try { setTours((await api.get("/api/yard-tours")) || []); }
    catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const handleCreate = useCallback(async (e) => {
    e.preventDefault();
    if (!title) { setStatus("Title required."); return; }
    try {
      await api.post("/api/yard-tours", { title, description: description || null, scheduledAt: scheduledAt || null, streamUrl: streamUrl || null });
      setStatus("Tour scheduled."); setTitle(""); setDescription(""); setScheduledAt(""); setStreamUrl("");
      load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, title, description, scheduledAt, streamUrl, load]);

  const handleStatus = useCallback(async (id, newStatus) => {
    try { await api.request(`/api/yard-tours/${id}/status`, { method: "PATCH", body: JSON.stringify(newStatus) }); load(); }
    catch (err) { setStatus(err.message || "Failed."); }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "Live Yard Tour", subtitle: "Schedule and broadcast live walkthroughs of your yard." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleCreate, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Schedule Tour"), h("button", { type: "submit", className: "primary-button" }, "Schedule")),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Title *"), h("input", { value: title, onChange: e => setTitle(e.target.value), required: true })),
        h("label", null, h("span", null, "Scheduled At"), h("input", { type: "datetime-local", value: scheduledAt, onChange: e => setScheduledAt(e.target.value) })),
        h("label", null, h("span", null, "Stream URL"), h("input", { value: streamUrl, onChange: e => setStreamUrl(e.target.value), placeholder: "https://..." })),
        h("label", { style: { gridColumn: "1 / -1" } }, h("span", null, "Description"), h("textarea", { value: description, onChange: e => setDescription(e.target.value), rows: 2 }))
      )
    ),
    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, `Tours (${tours.length})`)),
      isLoading ? h("p", { style: { padding: "12px" } }, "Loading...") :
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "Title"), h("th", null, "Seller"), h("th", null, "Status"), h("th", null, "Viewers"), h("th", null, "Interest"), h("th", null, "Actions"))),
        h("tbody", null, tours.map(t =>
          h("tr", { key: t.id },
            h("td", null, t.title), h("td", null, t.sellerName || "—"),
            h("td", null, h("span", { style: { color: statusColors[t.statusLabel] || "#333", fontWeight: "bold" } }, t.statusLabel)),
            h("td", null, t.viewerCount), h("td", null, t.interestCount),
            h("td", null,
              h("button", { className: "secondary-button", style: { marginRight: "4px" }, onClick: () => handleStatus(t.id, 2) }, "Go Live"),
              h("button", { className: "secondary-button", onClick: () => handleStatus(t.id, 3) }, "Complete")
            )
          )
        ))
      )
    )
  );
}
