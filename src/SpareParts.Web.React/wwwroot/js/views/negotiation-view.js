import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const statusColors = { Open: "#1976d2", CounterOffered: "#ff9800", Accepted: "#388e3c", Rejected: "#d32f2f", Expired: "#999" };

export function NegotiationView({ api }) {
  const [sessions, setSessions] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [partId, setPartId] = useState("");
  const [buyerName, setBuyerName] = useState("");
  const [buyerPhone, setBuyerPhone] = useState("");
  const [initialOffer, setInitialOffer] = useState("");
  const [offerAmounts, setOfferAmounts] = useState({});

  const load = useCallback(async () => {
    setIsLoading(true);
    try { setSessions((await api.get("/api/negotiations")) || []); }
    catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const handleStart = useCallback(async (e) => {
    e.preventDefault();
    if (!partId || !buyerName || !initialOffer) { setStatus("Part, buyer, and offer required."); return; }
    try {
      await api.post("/api/negotiations", { partId: Number(partId), buyerName, buyerPhone: buyerPhone || null, initialOffer: Number(initialOffer) });
      setStatus("Negotiation started."); setPartId(""); setBuyerName(""); setBuyerPhone(""); setInitialOffer("");
      load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, partId, buyerName, buyerPhone, initialOffer, load]);

  const handleOffer = useCallback(async (id) => {
    const amount = offerAmounts[id];
    if (!amount) return;
    try { await api.post(`/api/negotiations/${id}/offer`, { amount: Number(amount), offeredByRole: "Buyer" }); load(); }
    catch (err) { setStatus(err.message || "Failed."); }
  }, [api, offerAmounts, load]);

  const handleAccept = useCallback(async (id) => {
    try { await api.post(`/api/negotiations/${id}/accept`); load(); }
    catch (err) { setStatus(err.message || "Failed."); }
  }, [api, load]);

  const handleReject = useCallback(async (id) => {
    try { await api.post(`/api/negotiations/${id}/reject`); load(); }
    catch (err) { setStatus(err.message || "Failed."); }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "AI Negotiation Bot", subtitle: "Automated back-and-forth offers between buyers and sellers." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleStart, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Start Negotiation"), h("button", { type: "submit", className: "primary-button" }, "Start")),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Part ID *"), h("input", { type: "number", value: partId, onChange: e => setPartId(e.target.value), required: true })),
        h("label", null, h("span", null, "Buyer Name *"), h("input", { value: buyerName, onChange: e => setBuyerName(e.target.value), required: true })),
        h("label", null, h("span", null, "Buyer Phone"), h("input", { value: buyerPhone, onChange: e => setBuyerPhone(e.target.value) })),
        h("label", null, h("span", null, "Initial Offer *"), h("input", { type: "number", step: "0.01", value: initialOffer, onChange: e => setInitialOffer(e.target.value), required: true }))
      )
    ),
    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, `Sessions (${sessions.length})`)),
      isLoading ? h("p", { style: { padding: "12px" } }, "Loading...") :
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "Part"), h("th", null, "Buyer"), h("th", null, "Asking"), h("th", null, "Current Offer"), h("th", null, "Status"), h("th", null, "Counter"), h("th", null, "Actions"))),
        h("tbody", null, sessions.map(s =>
          h("tr", { key: s.id },
            h("td", null, s.partName), h("td", null, s.buyerName),
            h("td", null, money(s.askingPrice, s.currency)), h("td", null, money(s.currentOffer, s.currency)),
            h("td", null, h("span", { style: { color: statusColors[s.statusLabel] || "#333", fontWeight: "bold" } }, s.statusLabel)),
            h("td", null, h("input", { type: "number", style: { width: "90px" }, placeholder: "Amount", value: offerAmounts[s.id] || "", onChange: e => setOfferAmounts({ ...offerAmounts, [s.id]: e.target.value }) })),
            h("td", null,
              h("button", { className: "secondary-button", style: { marginRight: "4px" }, onClick: () => handleOffer(s.id) }, "Offer"),
              h("button", { className: "secondary-button", style: { marginRight: "4px" }, onClick: () => handleAccept(s.id) }, "Accept"),
              h("button", { className: "secondary-button", onClick: () => handleReject(s.id) }, "Reject")
            )
          )
        ))
      )
    )
  );
}
