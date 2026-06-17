import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const statusColors = { Pending: "#1976d2", Accepted: "#388e3c", Rejected: "#d32f2f", Contacted: "#ff9800", Expired: "#999" };

export function InstantOfferView({ api }) {
  const [offers, setOffers] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [partName, setPartName] = useState("");
  const [partDescription, setPartDescription] = useState("");
  const [conditionGrade, setConditionGrade] = useState("B");
  const [contactName, setContactName] = useState("");
  const [contactPhone, setContactPhone] = useState("");
  const [result, setResult] = useState(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try { setOffers((await api.get("/api/instant-offers")) || []); }
    catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!partName || !contactName) { setStatus("Part name and contact required."); return; }
    try {
      const data = await api.post("/api/instant-offers", {
        partName, partDescription: partDescription || null, conditionGrade,
        contactName, contactPhone: contactPhone || null
      });
      setResult(data); setStatus("Offer generated.");
      setPartName(""); setPartDescription(""); setContactName(""); setContactPhone("");
      load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, partName, partDescription, conditionGrade, contactName, contactPhone, load]);

  const handleStatus = useCallback(async (id, newStatus) => {
    try { await api.request(`/api/instant-offers/${id}/status`, { method: "PATCH", body: JSON.stringify({ newStatus }) }); load(); }
    catch (err) { setStatus(err.message || "Failed."); }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "InstantOffer", subtitle: "Get an instant cash offer for a used part." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleSubmit, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Submit Part for Offer"), h("button", { type: "submit", className: "primary-button" }, "Get Offer")),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Part Name *"), h("input", { value: partName, onChange: e => setPartName(e.target.value), required: true })),
        h("label", null, h("span", null, "Condition Grade"),
          h("select", { value: conditionGrade, onChange: e => setConditionGrade(e.target.value) },
            h("option", { value: "A" }, "A - Excellent"), h("option", { value: "B" }, "B - Good"),
            h("option", { value: "C" }, "C - Fair"), h("option", { value: "D" }, "D - Poor")
          )
        ),
        h("label", null, h("span", null, "Contact Name *"), h("input", { value: contactName, onChange: e => setContactName(e.target.value), required: true })),
        h("label", null, h("span", null, "Contact Phone"), h("input", { value: contactPhone, onChange: e => setContactPhone(e.target.value) })),
        h("label", { style: { gridColumn: "1 / -1" } }, h("span", null, "Description"), h("textarea", { value: partDescription, onChange: e => setPartDescription(e.target.value), rows: 2 }))
      )
    ),
    result && h("div", { className: "data-card", style: { marginBottom: "16px" } },
      h("div", { className: "data-card-header" }, h("strong", null, "Offer")),
      h("div", { className: "data-card-body" },
        h("div", null, "Estimated Value: ", money(result.estimatedValue, result.currency)),
        h("div", { style: { fontWeight: "bold", fontSize: "20px", color: "#388e3c" } }, "Offered Price: ", money(result.offeredPrice, result.currency))
      )
    ),
    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, `Offers (${offers.length})`)),
      isLoading ? h("p", { style: { padding: "12px" } }, "Loading...") :
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "Part"), h("th", null, "Grade"), h("th", null, "Offered"), h("th", null, "Contact"), h("th", null, "Status"), h("th", null, "Actions"))),
        h("tbody", null, offers.map(o =>
          h("tr", { key: o.id },
            h("td", null, o.partName), h("td", null, o.conditionGrade),
            h("td", null, money(o.offeredPrice, o.currency)), h("td", null, o.contactName),
            h("td", null, h("span", { style: { color: statusColors[o.statusLabel] || "#333", fontWeight: "bold" } }, o.statusLabel)),
            h("td", null,
              h("button", { className: "secondary-button", style: { marginRight: "4px" }, onClick: () => handleStatus(o.id, 2) }, "Accept"),
              h("button", { className: "secondary-button", onClick: () => handleStatus(o.id, 3) }, "Reject")
            )
          )
        ))
      )
    )
  );
}
