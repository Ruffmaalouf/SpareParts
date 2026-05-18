import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function ContactsView({ api }) {
  const [customers, setCustomers] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [status, setStatus] = useState("");

  const load = useCallback(async () => {
    setStatus("Loading contacts...");
    try {
      const [customerRows, supplierRows] = await Promise.all([
        api.get("/api/customers?page=1&pageSize=100"),
        api.get("/api/suppliers?page=1&pageSize=100")
      ]);
      setCustomers(customerRows);
      setSuppliers(supplierRows);
      setStatus("Contacts loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load contacts.");
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Contacts",
      action: h("button", { className: "secondary-button", onClick: load }, "Refresh")
    }),
    h(StatusLine, { status }),
    h("div", { className: "two-column" },
      h(ContactPanel, { title: "Customers", rows: customers }),
      h(ContactPanel, { title: "Suppliers", rows: suppliers })
    )
  );
}

function ContactPanel({ title, rows }) {
  return h("section", { className: "panel" },
    h("h3", null, title),
    h("div", { className: "dense-list contacts" },
      rows.map((row) =>
        h("div", { className: "list-row", key: `${title}-${row.id}` },
          h("div", null,
            h("strong", null, row.name),
            h("span", null, row.phone || row.email || "No contact detail")
          ),
          h("b", null, money(row.openingBalance || 0))
        )
      ),
      rows.length === 0 && h("p", { className: "empty-state" }, "No rows returned.")
    )
  );
}
