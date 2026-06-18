import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

export function LoyaltyView({ api }) {
  const [topCustomers, setTopCustomers] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState(null);
  const [transactions, setTransactions] = useState([]);
  const [addPoints, setAddPoints] = useState("");
  const [redeemPoints, setRedeemPoints] = useState("");
  const [notes, setNotes] = useState("");

  const loadTop = useCallback(async () => {
    setIsLoading(true);
    try {
      setTopCustomers(await api.get("/api/loyalty/customers/top?top=50"));
      setStatus("Loyalty leaders loaded.");
    } catch (e) {
      setStatus(e.message || "Failed to load.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { loadTop(); }, [loadTop]);

  const selectCustomer = useCallback(async (customer) => {
    setSelectedCustomer(customer);
    try {
      setTransactions(await api.get(`/api/loyalty/customers/${customer.customerId}/transactions`));
    } catch (e) {
      setStatus(e.message || "Failed to load transactions.");
    }
  }, [api]);

  const handleAdd = useCallback(async () => {
    if (!selectedCustomer || !addPoints) return;
    try {
      await api.post("/api/loyalty/points", {
        customerId: selectedCustomer.customerId,
        points: Number(addPoints),
        notes
      });
      setStatus(`Added ${addPoints} points.`);
      setAddPoints("");
      setNotes("");
      loadTop();
      selectCustomer(selectedCustomer);
    } catch (e) {
      setStatus(e.message || "Failed to add points.");
    }
  }, [api, selectedCustomer, addPoints, notes, loadTop, selectCustomer]);

  const handleRedeem = useCallback(async () => {
    if (!selectedCustomer || !redeemPoints) return;
    try {
      await api.post("/api/loyalty/redeem", {
        customerId: selectedCustomer.customerId,
        points: Number(redeemPoints),
        notes
      });
      setStatus(`Redeemed ${redeemPoints} points.`);
      setRedeemPoints("");
      loadTop();
      selectCustomer(selectedCustomer);
    } catch (e) {
      setStatus(e.message || "Failed to redeem: " + e.message);
    }
  }, [api, selectedCustomer, redeemPoints, notes, loadTop, selectCustomer]);

  return h("div", { className: "view-container" },
    h(PageHeader, { title: "Customer Loyalty" }),
    h(StatusLine, { status }),
    h("div", { className: "split-layout" },
      h("div", { className: "split-left" },
        h("h3", null, "Top Customers by Points"),
        h(DataTable, {
          rows: topCustomers,
          columns: [
            {
              key: "customerName",
              label: "Customer",
              render: (row) => h("button", { type: "button", className: "link-button", onClick: () => selectCustomer(row) }, row.customerName)
            },
            { key: "totalPoints", label: "Points", render: (row) => row.totalPoints },
            { key: "lifetimePointsEarned", label: "Earned", render: (row) => row.lifetimePointsEarned },
            { key: "lifetimePointsRedeemed", label: "Redeemed", render: (row) => row.lifetimePointsRedeemed }
          ],
          getRowKey: (row, index) => row.customerId ?? index,
          emptyText: "No loyalty customers found."
        })
      ),
      selectedCustomer && h("div", { className: "split-right" },
        h("h3", null, `${selectedCustomer.customerName} — ${selectedCustomer.totalPoints} pts`),
        h("div", { className: "form-row" },
          h("input", { type: "number", placeholder: "Points to add", value: addPoints, onChange: (e) => setAddPoints(e.target.value) }),
          h("button", { onClick: handleAdd }, "Add Points"),
          h("input", { type: "number", placeholder: "Points to redeem", value: redeemPoints, onChange: (e) => setRedeemPoints(e.target.value) }),
          h("button", { onClick: handleRedeem }, "Redeem")
        ),
        h("input", { type: "text", placeholder: "Notes", value: notes, onChange: (e) => setNotes(e.target.value) }),
        h("h4", null, "Transaction History"),
        h(DataTable, {
          rows: transactions,
          columns: [
            { key: "createdAt", label: "Date", render: (row) => new Date(row.createdAt).toLocaleDateString() },
            { key: "transactionType", label: "Type", render: (row) => row.transactionType },
            { key: "points", label: "Points", render: (row) => row.points },
            { key: "notes", label: "Notes", render: (row) => row.notes }
          ],
          getRowKey: (row, index) => row.id ?? index,
          emptyText: "No transactions yet."
        })
      )
    )
  );
}
