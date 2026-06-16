import { h, useCallback, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

function PriceBar({ minPrice, avgPrice, maxPrice }) {
  if (!minPrice && !maxPrice) return null;
  const range = maxPrice - minPrice;
  const safeRange = range > 0 ? range : 1;
  const avgPct = Math.round(((avgPrice - minPrice) / safeRange) * 100);

  return h("div", { style: { margin: "12px 0" } },
    h("div", { style: { position: "relative", height: "12px", borderRadius: "6px", background: "#e0e0e0" } },
      h("div", {
        style: {
          position: "absolute", left: 0, top: 0, bottom: 0,
          width: "100%", borderRadius: "6px",
          background: "linear-gradient(90deg, #4caf50, #ff9800, #f44336)"
        }
      }),
      h("div", {
        style: {
          position: "absolute", top: "-4px", bottom: "-4px", width: "3px",
          background: "#222", borderRadius: "2px", left: `calc(${avgPct}% - 1px)`
        }
      })
    ),
    h("div", { style: { display: "flex", justifyContent: "space-between", fontSize: "12px", marginTop: "4px" } },
      h("span", null, "Min"),
      h("span", null, "Avg"),
      h("span", null, "Max")
    )
  );
}

export function MarketPriceView({ api }) {
  const [partName, setPartName] = useState("");
  const [make, setMake] = useState("");
  const [model, setModel] = useState("");
  const [year, setYear] = useState("");
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [yourPrice, setYourPrice] = useState("");

  const handleSearch = useCallback(async (e) => {
    e.preventDefault();
    if (!partName.trim()) {
      setStatus("Please enter a part name.");
      return;
    }
    setIsLoading(true);
    setStatus("Checking market prices...");
    setResult(null);
    setYourPrice("");
    try {
      const params = new URLSearchParams({ partName: partName.trim() });
      if (make.trim()) params.set("make", make.trim());
      if (model.trim()) params.set("model", model.trim());
      if (year.trim()) params.set("year", year.trim());
      const data = await api.get(`/api/market-price?${params.toString()}`);
      if (!data || data.sampleCount === 0) {
        setResult(null);
        setStatus("No pricing data found for this part. Try a broader search term.");
      } else {
        setResult(data);
        setStatus("");
      }
    } catch (err) {
      setStatus(err.message || "Could not fetch market price data.");
    } finally {
      setIsLoading(false);
    }
  }, [api, partName, make, model, year]);

  const yourPriceNum = yourPrice ? Number(yourPrice) : null;
  let priceComparison = null;
  if (result && yourPriceNum && result.avgPrice) {
    const diff = ((yourPriceNum - result.avgPrice) / result.avgPrice) * 100;
    const abs = Math.abs(diff).toFixed(1);
    if (diff < 0) {
      priceComparison = { text: `${abs}% below market average`, tone: "positive" };
    } else if (diff > 0) {
      priceComparison = { text: `${abs}% above market average`, tone: "danger" };
    } else {
      priceComparison = { text: "Exactly at market average", tone: "neutral" };
    }
  }

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Market Price Index",
      subtitle: "Check fair market price ranges for parts based on active listings."
    }),
    h(StatusLine, { status }),

    h("form", { className: "admin-panel", onSubmit: handleSearch, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Search Part Price"),
        h("button", { type: "submit", className: "primary-button", disabled: isLoading },
          isLoading ? "Checking..." : "Check Price"
        )
      ),
      h("div", { className: "editor-grid" },
        h("label", null,
          h("span", null, "Part Name *"),
          h("input", {
            value: partName,
            onChange: e => setPartName(e.target.value),
            placeholder: "e.g. Alternator, CV Axle, Brake Disc",
            required: true
          })
        ),
        h("label", null,
          h("span", null, "Make (optional)"),
          h("input", {
            value: make,
            onChange: e => setMake(e.target.value),
            placeholder: "e.g. Toyota"
          })
        ),
        h("label", null,
          h("span", null, "Model (optional)"),
          h("input", {
            value: model,
            onChange: e => setModel(e.target.value),
            placeholder: "e.g. Camry"
          })
        ),
        h("label", null,
          h("span", null, "Year (optional)"),
          h("input", {
            type: "number",
            value: year,
            onChange: e => setYear(e.target.value),
            placeholder: "e.g. 2018"
          })
        )
      )
    ),

    result && h("div", { className: "data-card", style: { marginBottom: "16px" } },
      h("div", { className: "data-card-header" },
        h("strong", null, "Fair Price Range")
      ),
      h("div", { className: "data-card-body" },
        h("div", { style: { display: "flex", gap: "32px", alignItems: "center", flexWrap: "wrap", marginBottom: "8px" } },
          h("div", null,
            h("div", { style: { fontSize: "12px", color: "#888" } }, "Min"),
            h("div", { style: { fontSize: "20px", fontWeight: "bold" } }, money(result.minPrice, result.currency || "USD"))
          ),
          h("div", null,
            h("div", { style: { fontSize: "12px", color: "#888" } }, "Average"),
            h("div", { style: { fontSize: "28px", fontWeight: "bold", color: "#1976d2" } }, money(result.avgPrice, result.currency || "USD"))
          ),
          h("div", null,
            h("div", { style: { fontSize: "12px", color: "#888" } }, "Max"),
            h("div", { style: { fontSize: "20px", fontWeight: "bold" } }, money(result.maxPrice, result.currency || "USD"))
          )
        ),
        h(PriceBar, { minPrice: result.minPrice, avgPrice: result.avgPrice, maxPrice: result.maxPrice }),
        h("p", { style: { fontSize: "13px", color: "#888", marginTop: "8px" } },
          `Based on ${result.sampleCount} active listing${result.sampleCount === 1 ? "" : "s"}`
        )
      )
    ),

    result && h("div", { className: "data-card" },
      h("div", { className: "data-card-header" },
        h("strong", null, "Your Price Comparison")
      ),
      h("div", { className: "data-card-body" },
        h("div", { className: "form-row" },
          h("label", null,
            h("span", null, "Your Price (" + (result.currency || "USD") + ")"),
            h("input", {
              type: "number",
              value: yourPrice,
              onChange: e => setYourPrice(e.target.value),
              placeholder: "Enter your price",
              step: "0.01",
              style: { maxWidth: "200px" }
            })
          )
        ),
        priceComparison && h("p", {
          style: {
            fontSize: "18px",
            fontWeight: "bold",
            color: priceComparison.tone === "positive" ? "#388e3c"
              : priceComparison.tone === "danger" ? "#d32f2f" : "#555",
            marginTop: "8px"
          }
        }, priceComparison.text)
      )
    )
  );
}
