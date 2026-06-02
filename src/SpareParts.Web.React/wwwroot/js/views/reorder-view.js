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
    h(StatusLine, { message: status, isLoading }),
    tab === "suggestions" && h(DataTable, {
      rows: suggestions,
      columns: [
        { key: "partCode", label: "Code" },
        { key: "partName", label: "Part" },
        { key: "currentStock", label: "Stock" },
        { key: "reorderPoint", label: "Reorder At" },
        { key: "suggestedOrderQuantity", label: "Order Qty" },
        { key: "salesLast30Days", label: "Sales/30d" },
        { key: "salesLast90Days", label: "Sales/90d" },
        { key: "preferredSupplierName", label: "Supplier" },
        { key: "lastPurchasePrice", label: "Last Price" }
      ]
    }),
    tab === "rules" && h(DataTable, {
      rows: rules,
      columns: [
        { key: "partCode", label: "Code" },
        { key: "partName", label: "Part" },
        { key: "reorderPoint", label: "Reorder Point" },
        { key: "reorderQuantity", label: "Order Qty" },
        { key: "preferredSupplierName", label: "Supplier" },
        { key: "isActive", label: "Active", render: (v) => v ? "Yes" : "No" }
      ],
      onDelete: (row) => deleteRule(row.partId)
    })
  );
}
