import { h, useMemo, useState } from "../../core/react-runtime.js";
import { EmptyState } from "./EmptyState.js";

// Rebuilt DataTable. Same base contract as the original (`columns`, `rows`,
// `getRowKey`, `emptyText`) so existing callers keep working unchanged.
// New opt-in behaviors:
//  - sticky header (always on, CSS-only, no prop needed)
//  - sortable columns: pass `sortable: true` on a column descriptor and
//    optionally `defaultSortKey`/`defaultSortDirection` on the table
//  - row hover actions: `renderRowActions(row)`
//  - expandable rows: `renderExpanded(row)` + chevron toggle
//  - pagination: `pageSize`
export function DataTable({
  columns,
  rows,
  getRowKey,
  emptyText,
  defaultSortKey,
  defaultSortDirection = "asc",
  renderRowActions,
  renderExpanded,
  pageSize
}) {
  const [sortKey, setSortKey] = useState(defaultSortKey || null);
  const [sortDirection, setSortDirection] = useState(defaultSortDirection);
  const [expandedKeys, setExpandedKeys] = useState({});
  const [page, setPage] = useState(0);

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

  const totalPages = pageSize ? Math.max(1, Math.ceil(sortedRows.length / pageSize)) : 1;
  const currentPage = Math.min(page, totalPages - 1);
  const pagedRows = pageSize ? sortedRows.slice(currentPage * pageSize, currentPage * pageSize + pageSize) : sortedRows;

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

  if (rows.length === 0) {
    return h(EmptyState, { title: emptyText || "No data found.", subtitle: null });
  }

  return h("div", { className: "data-table-v2-wrap" },
    h("table", { className: "data-table-v2" },
      h("thead", { className: "data-table-v2-head" }, h("tr", null,
        renderExpanded ? h("th", { className: "data-table-v2-expand-col" }) : null,
        columns.map((column) =>
          h("th", {
            key: column.key,
            className: column.sortable ? "is-sortable" : undefined,
            onClick: column.sortable ? () => toggleSort(column) : undefined
          },
            h("span", { className: "data-table-v2-th-label" }, column.label),
            column.sortable && h("span", { className: "data-table-v2-sort-indicator" },
              sortKey === column.key ? (sortDirection === "asc" ? "▲" : "▼") : "⇅"
            )
          )
        ),
        renderRowActions ? h("th", { className: "data-table-v2-actions-col" }) : null
      )),
      h("tbody", null,
        pagedRows.map((row, index) => {
          const key = getRowKey(row, index);
          const isExpanded = Boolean(expandedKeys[key]);
          return [
            h("tr", { key: `${key}-row`, className: isExpanded ? "is-expanded" : undefined },
              renderExpanded ? h("td", { className: "data-table-v2-expand-col" },
                h("button", {
                  type: "button",
                  className: "data-table-v2-expand-toggle",
                  onClick: () => toggleExpanded(key),
                  "aria-label": isExpanded ? "Collapse row" : "Expand row"
                }, isExpanded ? "▼" : "▶")
              ) : null,
              columns.map((column) =>
                h("td", { key: column.key }, column.render(row, index))
              ),
              renderRowActions ? h("td", { className: "data-table-v2-row-actions" },
                h("div", { className: "data-table-v2-row-actions-inner" }, renderRowActions(row))
              ) : null
            ),
            isExpanded && renderExpanded ? h("tr", { key: `${key}-expanded`, className: "data-table-v2-expanded-row" },
              h("td", { colSpan: columns.length + (renderRowActions ? 2 : 1) }, renderExpanded(row))
            ) : null
          ];
        })
      )
    ),
    pageSize && totalPages > 1 && h("div", { className: "data-table-v2-pagination" },
      h("button", {
        type: "button",
        disabled: currentPage === 0,
        onClick: () => setPage((value) => Math.max(0, value - 1))
      }, "Prev"),
      h("span", null, `Page ${currentPage + 1} of ${totalPages}`),
      h("button", {
        type: "button",
        disabled: currentPage >= totalPages - 1,
        onClick: () => setPage((value) => Math.min(totalPages - 1, value + 1))
      }, "Next")
    )
  );
}
