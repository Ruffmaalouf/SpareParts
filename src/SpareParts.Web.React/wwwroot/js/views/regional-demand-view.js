import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function RegionalDemandView({ api }) {
  const [demand, setDemand] = useState([]);
  const [topParts, setTopParts] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [partName, setPartName] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true); setStatus("Loading...");
    try {
      const params = partName.trim() ? `?partName=${encodeURIComponent(partName.trim())}` : "";
      const [regionData, topData] = await Promise.all([
        api.get(`/api/regional-demand${params}`),
        api.get("/api/regional-demand/top")
      ]);
      setDemand(regionData || []); setTopParts(topData || []); setStatus("");
    } catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api, partName]);

  useEffect(() => { load(); }, []);

  const insightColor = { "High Demand": "#d32f2f", "Hot": "#d32f2f", "Moderate Demand": "#ff9800", "Warm": "#ff9800", "Balanced": "#388e3c", "Cool": "#888" };

  return h("section", { className: "screen" },
    h(PageHeader, { title: "RegionalDemandMap", subtitle: "See where part demand is highest across regions." }),
    h(StatusLine, { status }),
    h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Filter"),
        h("button", { className: "primary-button", onClick: load, disabled: isLoading }, "Refresh")
      ),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Part Name"), h("input", { value: partName, onChange: e => setPartName(e.target.value), placeholder: "Optional filter" }))
      )
    ),
    h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Top Demanded Parts")),
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "Part"), h("th", null, "Total Demand"), h("th", null, "Active Listings"), h("th", null, "Gap"), h("th", null, "Insight"))),
        h("tbody", null, topParts.map((p, i) =>
          h("tr", { key: i },
            h("td", null, p.partName), h("td", null, p.totalDemand), h("td", null, p.activeListings),
            h("td", null, p.gap), h("td", null, h("span", { style: { color: insightColor[p.insightLabel] || "#333" } }, p.insightLabel))
          )
        ))
      )
    ),
    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Demand by Region")),
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "Region"), h("th", null, "City"), h("th", null, "Part"), h("th", null, "Demand"), h("th", null, "Supply"), h("th", null, "Insight"))),
        h("tbody", null, demand.map((d, i) =>
          h("tr", { key: i },
            h("td", null, d.region || "—"), h("td", null, d.city || "—"), h("td", null, d.partName),
            h("td", null, d.demandCount), h("td", null, d.supplyCount),
            h("td", null, h("span", { style: { color: insightColor[d.insightLabel] || "#333" } }, d.insightLabel))
          )
        ))
      )
    )
  );
}
