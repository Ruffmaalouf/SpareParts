import { h, useMemo, useState } from "../../core/react-runtime.js";
import { initials } from "../../core/formatters.js";
import { MoneyText } from "./MoneyText.js";

const FILTERS = [
  { key: "all", label: "All" },
  { key: "customer", label: "Customers" },
  { key: "supplier", label: "Suppliers" }
];

// Ignition ClientRail (Report 03 §04) — searchable merged customer+supplier
// list replacing the two-column split in contacts-view.js. Local filter runs
// first (instant); `onSearch` lets the view fall back to the
// /api/clients/search typeahead for full-catalog search past the loaded
// rows. role="listbox" with aria-selected options and an sr-only search
// label per Report 03 §11.
// `clients` = Array<{id,name,balance,riskTone,lastActivity,kind}>.
export function ClientRail({ clients = [], selectedId, onSelect, onSearch, filterKind = "all", onFilterKind, loading, error, onRetry, formatMoney }) {
  const [query, setQuery] = useState("");

  const visible = useMemo(() => {
    const normalized = query.trim().toLowerCase();
    let rows = clients;
    if (filterKind !== "all") rows = rows.filter((client) => (client.kind || "customer") === filterKind);
    if (!normalized) return rows;
    return rows.filter((client) =>
      [client.name, client.phone].some((value) => String(value || "").toLowerCase().includes(normalized)));
  }, [clients, filterKind, query]);

  const handleSearch = (value) => {
    setQuery(value);
    if (typeof onSearch === "function") onSearch(value);
  };

  const handleKeyDown = (event) => {
    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    const index = visible.findIndex((client) => String(client.id) === String(selectedId));
    const nextIndex = event.key === "ArrowDown" ? Math.min(visible.length - 1, index + 1) : Math.max(0, index - 1);
    if (visible[nextIndex] && onSelect) {
      event.preventDefault();
      onSelect(visible[nextIndex].id);
    }
  };

  let body;
  if (loading) {
    body = h("div", { className: "ig-stack", "aria-hidden": "true" },
      [0, 1, 2, 3, 4, 5, 6, 7].map((index) =>
        h("span", { key: index, className: "ig-skeleton", style: { height: "44px" } })));
  } else if (error) {
    body = h("div", { className: "ig-error-banner", role: "alert" },
      h("span", null, error),
      typeof onRetry === "function" && h("button", { type: "button", className: "ig-btn", onClick: onRetry }, "Retry"));
  } else if (!visible.length) {
    body = h("div", { className: "ig-palette-empty" },
      query ? `No clients match “${query}”. ` : "No clients yet.",
      query && h("button", { type: "button", className: "ig-btn", onClick: () => handleSearch("") }, "Clear filter"));
  } else {
    body = visible.map((client) => {
      const isSelected = String(client.id) === String(selectedId);
      const balance = Number(client.balance || 0);
      const isOverdue = client.riskTone === "danger" || client.riskTone === "warning";
      return h("button", {
        key: client.id,
        type: "button",
        role: "option",
        "aria-selected": isSelected ? "true" : "false",
        className: isSelected ? "ig-client-row is-selected" : "ig-client-row",
        onClick: () => onSelect && onSelect(client.id)
      },
        h("span", { className: "ig-client-avatar", "aria-hidden": "true" }, initials(client.name)),
        h("span", { className: "ig-client-row-main" },
          h("span", { className: "ig-client-row-name", style: { display: "block" } }, client.name),
          client.lastActivity && h("span", { className: "ig-client-row-sub" }, client.lastActivity)),
        isOverdue && h("span", { className: "ig-overdue-dot", title: "Overdue balance", "aria-hidden": "true" }),
        h(MoneyText, {
          value: balance,
          negativeTone: true,
          format: formatMoney ? () => formatMoney(balance) : undefined
        }));
    });
  }

  return h("div", { className: "ig-client-rail" },
    h("label", { className: "ig-sr-only", htmlFor: "ig-client-rail-search" }, "Search clients"),
    h("input", {
      id: "ig-client-rail-search",
      type: "search",
      placeholder: "Search clients... ( / )",
      value: query,
      onChange: (event) => handleSearch(event.target.value)
    }),
    h("div", { className: "ig-client-rail-filter", role: "group", "aria-label": "Filter by kind" },
      FILTERS.map((filter) =>
        h("button", {
          key: filter.key,
          type: "button",
          className: filterKind === filter.key ? "is-active" : undefined,
          onClick: () => onFilterKind && onFilterKind(filter.key)
        }, filter.label))),
    h("div", { className: "ig-client-rail-list", role: "listbox", "aria-label": "Clients", onKeyDown: handleKeyDown },
      body)
  );
}
