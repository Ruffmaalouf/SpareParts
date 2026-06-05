import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

function agingClass(amount) {
  if (!amount || amount <= 0) return "";
  return "danger-text";
}

function percent(value, total) {
  if (!total || total <= 0) return "";
  return ` (${((value / total) * 100).toFixed(0)}%)`;
}

export function CustomerAgingView({ api }) {
  const [rows, setRows] = useState([]);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading customer aging...");
    try {
      const data = await api.get("/api/customers/aging");
      setRows(Array.isArray(data) ? data : []);
      setStatus("Aging report loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load customer aging.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const visibleRows = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return rows;
    return rows.filter((row) =>
      [row.customerName, row.phone].some((val) => String(val || "").toLowerCase().includes(q))
    );
  }, [rows, search]);

  const totals = useMemo(() => {
    return visibleRows.reduce(
      (acc, row) => ({
        current: acc.current + Number(row.current || 0),
        days1To30: acc.days1To30 + Number(row.days1To30 || 0),
        days31To60: acc.days31To60 + Number(row.days31To60 || 0),
        days61To90: acc.days61To90 + Number(row.days61To90 || 0),
        over90Days: acc.over90Days + Number(row.over90Days || 0),
        totalBalance: acc.totalBalance + Number(row.totalBalance || 0)
      }),
      { current: 0, days1To30: 0, days31To60: 0, days61To90: 0, over90Days: 0, totalBalance: 0 }
    );
  }, [visibleRows]);

  const over30 = useMemo(
    () => visibleRows.filter((r) => Number(r.days1To30 || 0) + Number(r.days31To60 || 0) + Number(r.days61To90 || 0) + Number(r.over90Days || 0) > 0).length,
    [visibleRows]
  );

  const over90 = useMemo(
    () => visibleRows.filter((r) => Number(r.over90Days || 0) > 0).length,
    [visibleRows]
  );

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Customer Aging",
      subtitle: "Outstanding balances by age bucket — follow up on overdue receivables.",
      action: h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h("section", { className: "module-summary" },
      h("div", null, h("span", null, "Customers with balance"), h("strong", null, visibleRows.length)),
      h("div", null, h("span", null, "Overdue 30+ days"), h("strong", null, over30)),
      h("div", null, h("span", null, "Overdue 90+ days"), h("strong", null, over90)),
      h("div", null, h("span", null, "Total outstanding"), h("strong", null, money(totals.totalBalance, "USD")))
    ),
    h(StatusLine, { status }),
    h("div", { className: "filters-row" },
      h("input", {
        value: search,
        onChange: (e) => setSearch(e.target.value),
        placeholder: "Search customer or phone"
      })
    ),
    visibleRows.length > 0 && h("div", { className: "panel", style: { marginBottom: "1rem", padding: "1rem" } },
      h("h4", { style: { margin: "0 0 0.5rem" } }, "Totals"),
      h("div", { className: "aging-totals-row" },
        h("span", null, h("small", null, "Current"), h("strong", null, money(totals.current, "USD"))),
        h("span", null, h("small", null, "1-30 days"), h("strong", { className: totals.days1To30 > 0 ? "danger-text" : "" }, money(totals.days1To30, "USD"))),
        h("span", null, h("small", null, "31-60 days"), h("strong", { className: totals.days31To60 > 0 ? "danger-text" : "" }, money(totals.days31To60, "USD"))),
        h("span", null, h("small", null, "61-90 days"), h("strong", { className: totals.days61To90 > 0 ? "danger-text" : "" }, money(totals.days61To90, "USD"))),
        h("span", null, h("small", null, "90+ days"), h("strong", { className: totals.over90Days > 0 ? "danger-text" : "" }, money(totals.over90Days, "USD"))),
        h("span", null, h("small", null, "Total"), h("strong", null, money(totals.totalBalance, "USD")))
      )
    ),
    h("section", { className: "table-panel" },
      h(DataTable, {
        columns: [
          { key: "customer", label: "Customer", render: (row) => h("strong", null, row.customerName) },
          { key: "phone", label: "Phone", render: (row) => row.phone || "—" },
          { key: "current", label: "Current", render: (row) => money(row.current, "USD") },
          {
            key: "days30",
            label: "1–30 days",
            render: (row) => h("span", { className: agingClass(row.days1To30) }, money(row.days1To30, "USD"))
          },
          {
            key: "days60",
            label: "31–60 days",
            render: (row) => h("span", { className: agingClass(row.days31To60) }, money(row.days31To60, "USD"))
          },
          {
            key: "days90",
            label: "61–90 days",
            render: (row) => h("span", { className: agingClass(row.days61To90) }, money(row.days61To90, "USD"))
          },
          {
            key: "over90",
            label: "90+ days",
            render: (row) => h("span", { className: agingClass(row.over90Days) }, money(row.over90Days, "USD"))
          },
          {
            key: "total",
            label: "Total",
            render: (row) => h("strong", null, money(row.totalBalance, "USD"))
          }
        ],
        rows: visibleRows,
        getRowKey: (row) => row.customerId,
        emptyText: "No outstanding customer balances."
      })
    )
  );
}
