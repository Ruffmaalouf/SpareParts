import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

export function ReorderView({ api }) {
  const [tab, setTab] = useState("suggestions");
  const [suggestions, setSuggestions] = useState([]);
  const [rules, setRules] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const loadSuggestions = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading reorder suggestions...");
    try {
      setSuggestions(await api.get("/api/reorder/suggestions"));
      setStatus("Suggestions loaded.");
    } catch (e) {
      setStatus(e.message || "Failed to load suggestions.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  const loadRules = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading reorder rules...");
    try {
      setRules(await api.get("/api/reorder/rules"));
      setStatus("Rules loaded.");
    } catch (e) {
      setStatus(e.message || "Failed to load rules.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => {
    if (tab === "suggestions") loadSuggestions();
    else loadRules();
  }, [tab, loadSuggestions, loadRules]);

  const deleteRule = useCallback(async (partId) => {
    if (!confirm("Remove this reorder rule?")) return;
    try {
      await api.delete(`/api/reorder/rules/${partId}`);
      setStatus("Rule removed.");
      loadRules();
    } catch (e) {
      setStatus(e.message || "Failed to remove rule.");
    }
  }, [api, loadRules]);

  return h("div", { className: "view-container" },
    h(PageHeader, { title: "Reorder Center" }),
    h("div", { className: "tab-bar" },
      h("button", { className: `tab-btn${tab === "suggestions" ? " active" : ""}`, onClick: () => setTab("suggestions") }, "Reorder Suggestions"),
      h("button", { className: `tab-btn${tab === "rules" ? " active" : ""}`, onClick: () => setTab("rules") }, "Reorder Rules")
    ),
    h(StatusLine, { status }),
    tab === "suggestions" && h(DataTable, {
      rows: suggestions,
      getRowKey: (row) => row.partId,
      emptyText: "No reorder suggestions found.",
      columns: [
        { key: "partCode", label: "Code", render: (row) => row.partCode },
        { key: "partName", label: "Part", render: (row) => row.partName },
        { key: "currentStock", label: "Stock", render: (row) => row.currentStock },
        { key: "reorderPoint", label: "Reorder At", render: (row) => row.reorderPoint },
        { key: "suggestedOrderQuantity", label: "Order Qty", render: (row) => row.suggestedOrderQuantity },
        { key: "salesLast30Days", label: "Sales/30d", render: (row) => row.salesLast30Days },
        { key: "salesLast90Days", label: "Sales/90d", render: (row) => row.salesLast90Days },
        { key: "preferredSupplierName", label: "Supplier", render: (row) => row.preferredSupplierName || "" },
        { key: "lastPurchasePrice", label: "Last Price", render: (row) => row.lastPurchasePrice != null ? row.lastPurchasePrice : "" }
      ]
    }),
    tab === "rules" && h(DataTable, {
      rows: rules,
      getRowKey: (row) => row.partId,
      emptyText: "No reorder rules found.",
      columns: [
        { key: "partCode", label: "Code", render: (row) => row.partCode },
        { key: "partName", label: "Part", render: (row) => row.partName },
        { key: "reorderPoint", label: "Reorder Point", render: (row) => row.reorderPoint },
        { key: "reorderQuantity", label: "Order Qty", render: (row) => row.reorderQuantity },
        { key: "preferredSupplierName", label: "Supplier", render: (row) => row.preferredSupplierName || "" },
        { key: "isActive", label: "Active", render: (row) => row.isActive ? "Yes" : "No" },
        {
          key: "actions",
          label: "Actions",
          render: (row) => h("button", { type: "button", className: "danger-button", onClick: () => deleteRule(row.partId) }, "Remove")
        }
      ]
    })
  );
}
