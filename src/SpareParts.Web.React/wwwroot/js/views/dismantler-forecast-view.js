import { h, useCallback, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function DismantlerForecastView({ api }) {
  const [make, setMake] = useState("");
  const [model, setModel] = useState("");
  const [year, setYear] = useState("");
  const [purchasePrice, setPurchasePrice] = useState("");
  const [currency, setCurrency] = useState("USD");
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!make || !purchasePrice) { setStatus("Make and purchase price required."); return; }
    setIsLoading(true); setStatus("Forecasting..."); setResult(null);
    try {
      const data = await api.post("/api/dismantler-forecast", {
        vehicleMake: make, vehicleModel: model || null,
        vehicleYear: year ? Number(year) : null,
        purchasePrice: Number(purchasePrice), currency
      });
      setResult(data); setStatus("");
    } catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api, make, model, year, purchasePrice, currency]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "DismantlerForecast", subtitle: "Estimate part-out value before buying a vehicle." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleSubmit, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Vehicle Forecast"),
        h("button", { type: "submit", className: "primary-button", disabled: isLoading }, isLoading ? "Forecasting..." : "Forecast")
      ),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Make *"), h("input", { value: make, onChange: e => setMake(e.target.value), placeholder: "e.g. Toyota", required: true })),
        h("label", null, h("span", null, "Model"), h("input", { value: model, onChange: e => setModel(e.target.value), placeholder: "e.g. Camry" })),
        h("label", null, h("span", null, "Year"), h("input", { type: "number", value: year, onChange: e => setYear(e.target.value), placeholder: "e.g. 2015" })),
        h("label", null, h("span", null, "Purchase Price *"), h("input", { type: "number", value: purchasePrice, onChange: e => setPurchasePrice(e.target.value), required: true, step: "0.01" })),
        h("label", null, h("span", null, "Currency"),
          h("select", { value: currency, onChange: e => setCurrency(e.target.value) },
            h("option", { value: "USD" }, "USD"), h("option", { value: "EUR" }, "EUR"), h("option", { value: "LBP" }, "LBP")
          )
        )
      )
    ),
    result && h("div", null,
      h("div", { className: "data-card", style: { marginBottom: "16px" } },
        h("div", { className: "data-card-header" }, h("strong", null, result.vehicleDescription || "Forecast")),
        h("div", { className: "data-card-body" },
          h("div", { style: { display: "flex", gap: "32px", flexWrap: "wrap", marginBottom: "12px" } },
            h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "Purchase"), h("div", { style: { fontSize: "20px" } }, money(result.purchasePrice, result.currency))),
            h("div", null, h("div", { style: { fontSize: "12px", color: "#388e3c" } }, "Part-Out Value"), h("div", { style: { fontSize: "24px", fontWeight: "bold", color: "#388e3c" } }, money(result.estimatedPartOutValue, result.currency))),
            h("div", null, h("div", { style: { fontSize: "12px", color: result.estimatedProfit >= 0 ? "#388e3c" : "#d32f2f" } }, "Profit"), h("div", { style: { fontSize: "24px", fontWeight: "bold", color: result.estimatedProfit >= 0 ? "#388e3c" : "#d32f2f" } }, money(result.estimatedProfit, result.currency))),
            h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "ROI"), h("div", { style: { fontSize: "20px", fontWeight: "bold" } }, `${result.estimatedRoiPercent}%`))
          )
        )
      ),
      h("div", { className: "admin-panel" },
        h("div", { className: "admin-panel-header" }, h("h3", null, "Top Parts")),
        h("table", { className: "data-table" },
          h("thead", null, h("tr", null,
            h("th", null, "Part"), h("th", null, "Category"), h("th", null, "Est. Price"), h("th", null, "High Value"), h("th", null, "Fast Moving"), h("th", null, "Confidence")
          )),
          h("tbody", null, result.topParts.map((p, i) =>
            h("tr", { key: i },
              h("td", null, p.partName),
              h("td", null, p.category),
              h("td", null, money(p.estimatedPrice, result.currency)),
              h("td", null, p.isHighValue ? "⭐" : ""),
              h("td", null, p.isFastMoving ? "🔥" : ""),
              h("td", null, p.confidenceLevel)
            )
          ))
        )
      ),
      h("p", { style: { fontSize: "12px", color: "#999", marginTop: "8px" } }, result.disclaimer)
    )
  );
}
