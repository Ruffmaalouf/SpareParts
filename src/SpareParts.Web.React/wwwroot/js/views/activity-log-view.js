import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

export function ActivityLogView({ api }) {
  const [logs, setLogs] = useState([]);
  const [entityType, setEntityType] = useState("");
  const [entityId, setEntityId] = useState("");
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading activity log...");
    try {
      const params = new URLSearchParams({ page, pageSize: 100 });
      if (entityType) params.set("entityType", entityType);
      if (entityId) params.set("entityId", entityId);
      setLogs(await api.get(`/api/activity-log?${params}`));
      setStatus("Log loaded.");
    } catch (e) {
      setStatus(e.message || "Failed to load.");
    } finally {
      setIsLoading(false);
    }
  }, [api, entityType, entityId, page]);

  useEffect(() => { load(); }, [load]);

  return h("div", { className: "view-container" },
    h(PageHeader, { title: "Activity Log" }),
    h("div", { className: "filter-bar" },
      h("input", {
        type: "text",
        placeholder: "Entity type (Part, Customer...)",
        value: entityType,
        onChange: (e) => { setEntityType(e.target.value); setPage(1); }
      }),
      h("input", {
        type: "number",
        placeholder: "Entity ID",
        value: entityId,
        onChange: (e) => { setEntityId(e.target.value); setPage(1); }
      }),
      h("button", { onClick: load }, "Search")
    ),
    h(StatusLine, { message: status, isLoading }),
    h(DataTable, {
      rows: logs,
      columns: [
        { key: "createdAt", label: "Time", render: (v) => new Date(v).toLocaleString() },
        { key: "userName", label: "User" },
        { key: "action", label: "Action" },
        { key: "entityType", label: "Entity" },
        { key: "entityId", label: "ID" },
        { key: "entityDescription", label: "Description" },
        { key: "ipAddress", label: "IP" }
      ]
    }),
    h("div", { className: "pagination-bar" },
      page > 1 && h("button", { onClick: () => setPage(page - 1) }, "← Prev"),
      h("span", null, ` Page ${page} `),
      logs.length === 100 && h("button", { onClick: () => setPage(page + 1) }, "Next →")
    )
  );
}
