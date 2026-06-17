import { h, useCallback, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const trendColors = { Rising: "#d32f2f", Falling: "#388e3c", Stable: "#888" };

export function PriceReportView({ api }) {
  const [partName, setPartName] = useState("");
  const [make, setMake] = useState("");
  const [model, setModel] = useState("");
  const [year, setYear] = useState("");
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");

  const handleGenerate = useCallback(async (e) => {
    e.preventDefault();
    setIsLoading(true); setStatus("Generating..."); setResult(null);
    try {
      const data = await api.post("/api/price-report", {
        partName: partName || null, make: make || null, model: model || null, year: year ? Number(year) : null
      });
      setResult(data); setStatus("");
    } catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api, partName, make, model, year]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "B2B PriceReport", subtitle: "Generate aggregate pricing reports for partners and buyers." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleGenerate, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Generate Report"),
        h("button", { type: "submit", className: "primary-button", disabled: isLoading }, isLoading ? "Generating..." : "Generate")
      ),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Part Name"), h("input", { value: partName, onChange: e => setPartName(e.target.value) })),
        h("label", null, h("span", null, "Make"), h("input", { value: make, onChange: e => setMake(e.target.value) })),
        h("label", null, h("span", null, "Model"), h("input", { value: model, onChange: e => setModel(e.target.value) })),
        h("label", null, h("span", null, "Year"), h("input", { type: "number", value: year, onChange: e => setYear(e.target.value) }))
      )
    ),
    result && h("div", { className: "data-card" },
      h("div", { className: "data-card-header" }, h("strong", null, result.partName || "Report"),
        h("span", { style: { marginLeft: "8px", color: trendColors[result.trendDirection] || "#333", fontWeight: "bold" } }, result.trendDirection)
      ),
      h("div", { className: "data-card-body", style: { display: "flex", gap: "32px", flexWrap: "wrap" } },
        h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "Min"), h("div", { style: { fontSize: "18px" } }, money(result.minPrice, result.currency))),
        h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "Avg"), h("div", { style: { fontSize: "18px" } }, money(result.avgPrice, result.currency))),
        h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "Max"), h("div", { style: { fontSize: "18px" } }, money(result.maxPrice, result.currency))),
        h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "Samples"), h("div", { style: { fontSize: "18px" } }, result.sampleCount))
      )
    )
  );
}
