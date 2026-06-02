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
    h(StatusLine, { message: status, isLoading }),
    h(DataTable, {
      rows: alerts,
      columns: [
        { key: "partCode", label: "Code" },
        { key: "partName", label: "Part" },
        { key: "expiryDate", label: "Expiry Date", render: (v) => v ? new Date(v).toLocaleDateString() : "" },
        { key: "daysUntilExpiry", label: "Days Left" },
        { key: "stockQuantity", label: "In Stock" },
        { key: "expiryStatus", label: "Status", render: (v) => h("span", { className: statusColor(v) }, v) }
      ]
    })
  );
}
