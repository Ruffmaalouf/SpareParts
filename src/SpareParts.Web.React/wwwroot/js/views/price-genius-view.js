import { h, useCallback, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function PriceGeniusView({ api }) {
  const [partId, setPartId] = useState("");
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!partId) { setStatus("Enter a part ID."); return; }
    setIsLoading(true); setStatus("Analyzing market data..."); setResult(null);
    try {
      const data = await api.post("/api/price-genius/suggest", { partId: Number(partId) });
      setResult(data); setStatus("");
    } catch (err) { setStatus(err.message || "Failed to fetch suggestion."); }
    finally { setIsLoading(false); }
  }, [api, partId]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "PriceGenius AI", subtitle: "AI-powered pricing suggestions based on market data." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleSubmit, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Get Price Suggestion"),
        h("button", { type: "submit", className: "primary-button", disabled: isLoading },
          isLoading ? "Analyzing..." : "Suggest Price"
        )
      ),
      h("div", { className: "editor-grid" },
        h("label", null,
          h("span", null, "Part ID *"),
          h("input", { type: "number", value: partId, onChange: e => setPartId(e.target.value), placeholder: "e.g. 42", required: true })
        )
      )
    ),
    result && h("div", { className: "data-card" },
      h("div", { className: "data-card-header" },
        h("strong", null, result.partName || `Part #${result.partId}`),
        result.isLocked && h("span", { style: { color: "#ff9800", marginLeft: "8px" } }, "🔒 PRO")
      ),
      h("div", { className: "data-card-body" },
        h("div", { style: { display: "flex", gap: "24px", flexWrap: "wrap", marginBottom: "12px" } },
          result.suggestedRegularPrice && h("div", null,
            h("div", { style: { fontSize: "12px", color: "#888" } }, "Regular"),
            h("div", { style: { fontSize: "22px", fontWeight: "bold" } }, money(result.suggestedRegularPrice, result.currency))
          ),
          result.suggestedFastSalePrice && h("div", null,
            h("div", { style: { fontSize: "12px", color: "#4caf50" } }, "Fast Sale"),
            h("div", { style: { fontSize: "22px", fontWeight: "bold", color: "#4caf50" } }, money(result.suggestedFastSalePrice, result.currency))
          ),
          result.suggestedMaxProfitPrice && h("div", null,
            h("div", { style: { fontSize: "12px", color: "#ff9800" } }, "Max Profit"),
            h("div", { style: { fontSize: "22px", fontWeight: "bold", color: "#ff9800" } }, money(result.suggestedMaxProfitPrice, result.currency))
          )
        ),
        result.reasoning && h("p", { style: { fontSize: "13px", color: "#666", marginTop: "8px" } }, result.reasoning),
        result.sampleCount > 0 && h("p", { style: { fontSize: "12px", color: "#999" } }, `Based on ${result.sampleCount} listings`)
      )
    )
  );
}
