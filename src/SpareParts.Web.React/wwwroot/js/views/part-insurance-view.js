import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function PartInsuranceView({ api }) {
  const [options, setOptions] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [partId, setPartId] = useState("");
  const [salesInvoiceId, setSalesInvoiceId] = useState("");
  const [insuranceOptionId, setInsuranceOptionId] = useState("");
  const [result, setResult] = useState(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = (await api.get("/api/part-insurance/options")) || [];
      setOptions(data);
      if (data.length && !insuranceOptionId) setInsuranceOptionId(String(data[0].id));
    } catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api, insuranceOptionId]);

  useEffect(() => { load(); }, []);

  const handleAdd = useCallback(async (e) => {
    e.preventDefault();
    if (!partId || !salesInvoiceId || !insuranceOptionId) { setStatus("All fields required."); return; }
    try {
      const data = await api.post("/api/part-insurance/addon", {
        partId: Number(partId), salesInvoiceId: Number(salesInvoiceId), insuranceOptionId: Number(insuranceOptionId)
      });
      setResult(data); setStatus("Add-on applied.");
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, partId, salesInvoiceId, insuranceOptionId]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "PartInsurance Add-On", subtitle: "Offer buyers optional protection coverage at checkout." }),
    h(StatusLine, { status }),
    h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Available Options")),
      isLoading ? h("p", { style: { padding: "12px" } }, "Loading...") :
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "Name"), h("th", null, "Description"), h("th", null, "Price %"), h("th", null, "Max Coverage"))),
        h("tbody", null, options.map(o =>
          h("tr", { key: o.id },
            h("td", null, o.name), h("td", null, o.description),
            h("td", null, `${o.pricePercent}%`), h("td", null, money(o.maxCoverageAmount, o.currency))
          )
        ))
      )
    ),
    h("form", { className: "admin-panel", onSubmit: handleAdd },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Add Insurance to Sale"), h("button", { type: "submit", className: "primary-button" }, "Apply")),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Part ID *"), h("input", { type: "number", value: partId, onChange: e => setPartId(e.target.value), required: true })),
        h("label", null, h("span", null, "Sales Invoice ID *"), h("input", { type: "number", value: salesInvoiceId, onChange: e => setSalesInvoiceId(e.target.value), required: true })),
        h("label", null, h("span", null, "Option"),
          h("select", { value: insuranceOptionId, onChange: e => setInsuranceOptionId(e.target.value) },
            options.map(o => h("option", { key: o.id, value: o.id }, o.name))
          )
        )
      )
    ),
    result && h("div", { className: "data-card", style: { marginTop: "16px" } },
      h("div", { className: "data-card-header" }, h("strong", null, result.optionName)),
      h("div", { className: "data-card-body" }, "Price: ", money(result.price, result.currency))
    )
  );
}
