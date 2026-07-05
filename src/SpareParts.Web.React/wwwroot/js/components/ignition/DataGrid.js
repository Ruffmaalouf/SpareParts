import { h, useMemo, useState } from "../../core/react-runtime.js";

const VIRTUALIZE_THRESHOLD = 150;
const OVERSCAN = 4;
const VISIBLE_ROWS = 12;

// Ignition DataGrid (Report 03 §03) — backward-compatible superset of the
// existing ui/DataTable contract (columns / rows / getRowKey / emptyText /
// renderRowActions / renderExpanded). The sort logic is carried over from
// DataTable.js verbatim; DataGrid adds:
//  - virtualization: auto-enabled when rows.length > 150 (fixed rowHeight
//    required for windowing math; ~12 rows rendered + overscan 4)
//  - stickyColumns: freezes the first N columns horizontally for wide grids
// Virtualization is skipped when renderExpanded is supplied (variable row
// heights break the windowing math), matching current DataTable behavior.
export function DataGrid({
  columns,
  rows,
  getRowKey,
  emptyText,
  defaultSortKey,
  defaultSortDirection = "asc",
  renderRowActions,
  renderExpanded,
  virtualize,
  rowHeight = 44,
  stickyColumns = 0,
  loading = false,
  caption
}) {
  const [sortKey, setSortKey] = useState(defaultSortKey || null);
  const [sortDirection, setSortDirection] = useState(defaultSortDirection);
  const [expandedKeys, setExpandedKeys] = useState({});
  const [scrollTop, setScrollTop] = useState(0);

  // Sort logic ported from ui/DataTable.js.
  const sortedRows = useMemo(() => {
    if (!sortKey) return rows;
    const column = columns.find((candidate) => candidate.key === sortKey);
    if (!column) return rows;
    const copy = [...rows];
    copy.sort((left, right) => {
      const leftValue = column.sortValue ? column.sortValue(left) : column.render(left);
      const rightValue = column.sortValue ? column.sortValue(right) : column.render(right);
      if (leftValue == null && rightValue == null) return 0;
      if (leftValue == null) return -1;
      if (rightValue == null) return 1;
      if (typeof leftValue === "number" && typeof rightValue === "number") return leftValue - rightValue;
      return String(leftValue).localeCompare(String(rightValue));
    });
    if (sortDirection === "desc") copy.reverse();
    return copy;
  }, [columns, rows, sortDirection, sortKey]);

  const toggleSort = (column) => {
    if (!column.sortable) return;
    if (sortKey === column.key) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
    } else {
      setSortKey(column.key);
      setSortDirection("asc");
    }
  };

  const toggleExpanded = (key) => {
    setExpandedKeys((current) => ({ ...current, [key]: !current[key] }));
  };

  if (loading) {
    return h("div", { className: "ig-stack", "aria-hidden": "true" },
      [0, 1, 2, 3, 4].map((index) =>
        h("span", { key: index, className: "ig-skeleton", style: { height: "36px" } })));
  }

  if (!rows.length) {
    return h("div", { className: "ig-palette-empty" }, emptyText || "No data found.");
  }

  const shouldVirtualize = (virtualize ?? sortedRows.length > VIRTUALIZE_THRESHOLD) && !renderExpanded;
  let visibleRows = sortedRows;
  let topPad = 0;
  let bottomPad = 0;

  if (shouldVirtualize) {
    const start = Math.max(0, Math.floor(scrollTop / rowHeight) - OVERSCAN);
    const end = Math.min(sortedRows.length, start + VISIBLE_ROWS + OVERSCAN * 2);
    visibleRows = sortedRows.slice(start, end);
    topPad = start * rowHeight;
    bottomPad = (sortedRows.length - end) * rowHeight;
  }

  const stickyClass = (index) => (index < stickyColumns ? "is-sticky" : undefined);

  const renderRow = (row, index) => {
    const key = getRowKey(row, index);
    const isExpanded = Boolean(expandedKeys[key]);
    return [
      h("tr", { key: `${key}-row`, style: shouldVirtualize ? { height: `${rowHeight}px` } : undefined },
        renderExpanded ? h("td", null,
          h("button", {
            type: "button",
            onClick: () => toggleExpanded(key),
            "aria-label": isExpanded ? "Collapse row" : "Expand row"
          }, isExpanded ? "▼" : "▶")
        ) : null,
        columns.map((column, columnIndex) =>
          h("td", { key: column.key, className: stickyClass(columnIndex) }, column.render(row, index))),
        renderRowActions ? h("td", null, renderRowActions(row)) : null
      ),
      isExpanded && renderExpanded ? h("tr", { key: `${key}-expanded` },
        h("td", { colSpan: columns.length + (renderRowActions ? 2 : 1) }, renderExpanded(row))
      ) : null
    ];
  };

  return h("div", {
    className: "ig-datagrid-wrap",
    style: shouldVirtualize ? { maxHeight: `${rowHeight * VISIBLE_ROWS}px` } : undefined,
    onScroll: shouldVirtualize ? (event) => setScrollTop(event.currentTarget.scrollTop) : undefined
  },
    h("table", { className: "ig-datagrid" },
      caption && h("caption", { className: "ig-sr-only" }, caption),
      h("thead", null, h("tr", null,
        renderExpanded ? h("th", { style: { width: "28px" } }) : null,
        columns.map((column, columnIndex) =>
          h("th", {
            key: column.key,
            className: [column.sortable ? "is-sortable" : "", stickyClass(columnIndex) || ""].join(" ").trim() || undefined,
            "aria-sort": sortKey === column.key ? (sortDirection === "asc" ? "ascending" : "descending") : undefined,
            onClick: column.sortable ? () => toggleSort(column) : undefined
          },
            column.label,
            column.sortable && h("span", { "aria-hidden": "true" }, sortKey === column.key ? (sortDirection === "asc" ? " ▲" : " ▼") : " ⇅"))),
        renderRowActions ? h("th", null) : null
      )),
      h("tbody", null,
        topPad > 0 && h("tr", { "aria-hidden": "true" }, h("td", { colSpan: columns.length + 2, style: { height: `${topPad}px`, padding: 0, border: 0 } })),
        visibleRows.map(renderRow),
        bottomPad > 0 && h("tr", { "aria-hidden": "true" }, h("td", { colSpan: columns.length + 2, style: { height: `${bottomPad}px`, padding: 0, border: 0 } }))
      )
    )
  );
}
