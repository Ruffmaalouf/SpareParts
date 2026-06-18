import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

export function ExpiryAlertsView({ api }) {
  const [alerts, setAlerts] = useState([]);
  const [daysAhead, setDaysAhead] = useState(90);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading expiry alerts...");
    try {
      setAlerts(await api.get(`/api/parts/expiry/alerts?daysAhead=${daysAhead}`));
      setStatus("Alerts loaded.");
    } catch (e) {
      setStatus(e.message || "Failed to load alerts.");
    } finally {
      setIsLoading(false);
    }
  }, [api, daysAhead]);

  useEffect(() => { load(); }, [load]);

  const statusColor = (s) => ({
    Expired: "status-badge danger",
    Critical: "status-badge warning",
    Warning: "status-badge info"
  }[s] || "status-badge");

  return h("div", { className: "view-container" },
    h(PageHeader, { title: "Expiry Alerts" }),
    h("div", { className: "filter-bar" },
      h("label", null, "Show parts expiring within "),
      h("select", {
        value: daysAhead,
        onChange: (e) => setDaysAhead(Number(e.target.value))
      },
        h("option", { value: 30 }, "30 days"),
        h("option", { value: 60 }, "60 days"),
        h("option", { value: 90 }, "90 days"),
        h("option", { value: 180 }, "180 days"),
        h("option", { value: 0 }, "Already expired")
      )
    ),
    h(StatusLine, { status }),
    h(DataTable, {
      rows: alerts,
      getRowKey: (row) => row.partId,
      emptyText: "No expiring parts found.",
      columns: [
        { key: "partCode", label: "Code", render: (row) => row.partCode },
        { key: "partName", label: "Part", render: (row) => row.partName },
        { key: "expiryDate", label: "Expiry Date", render: (row) => row.expiryDate ? new Date(row.expiryDate).toLocaleDateString() : "" },
        { key: "daysUntilExpiry", label: "Days Left", render: (row) => row.daysUntilExpiry },
        { key: "stockQuantity", label: "In Stock", render: (row) => row.stockQuantity },
        { key: "expiryStatus", label: "Status", render: (row) => h("span", { className: statusColor(row.expiryStatus) }, row.expiryStatus) }
      ]
    })
  );
}
