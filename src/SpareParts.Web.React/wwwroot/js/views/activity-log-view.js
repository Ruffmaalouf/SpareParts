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
    h(StatusLine, { status }),
    h(DataTable, {
      rows: logs,
      getRowKey: (row, index) => row?.id ?? index,
      columns: [
        { key: "createdAt", label: "Time", render: (row) => row.createdAt ? new Date(row.createdAt).toLocaleString() : "-" },
        { key: "userName", label: "User", render: (row) => row.userName || "-" },
        { key: "action", label: "Action", render: (row) => row.action || "-" },
        { key: "entityType", label: "Entity", render: (row) => row.entityType || "-" },
        { key: "entityId", label: "ID", render: (row) => row.entityId ?? "-" },
        { key: "entityDescription", label: "Description", render: (row) => row.entityDescription || "-" },
        { key: "ipAddress", label: "IP", render: (row) => row.ipAddress || "-" }
      ],
      emptyText: "No activity log entries found."
    }),
    h("div", { className: "pagination-bar" },
      page > 1 && h("button", { onClick: () => setPage(page - 1) }, "← Prev"),
      h("span", null, ` Page ${page} `),
      logs.length === 100 && h("button", { onClick: () => setPage(page + 1) }, "Next →")
    )
  );
}
