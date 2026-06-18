import { h, useCallback, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function ArFinderView({ api }) {
  const [photoUrl, setPhotoUrl] = useState("");
  const [make, setMake] = useState("");
  const [model, setModel] = useState("");
  const [year, setYear] = useState("");
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");

  const handleScan = useCallback(async (e) => {
    e.preventDefault();
    if (!photoUrl) { setStatus("Photo URL required."); return; }
    setIsLoading(true); setStatus("Scanning..."); setResult(null);
    try {
      const data = await api.post("/api/ar-finder/scan", { photoUrl, make: make || null, model: model || null, year: year ? Number(year) : null });
      setResult(data);
      setStatus(data.success ? "" : (data.disclaimer || "No matches found."));
    } catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api, photoUrl, make, model, year]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "AR Parts Finder", subtitle: "Point your camera at a part to find matching inventory." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleScan, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Scan a Part"),
        h("button", { type: "submit", className: "primary-button", disabled: isLoading }, isLoading ? "Scanning..." : "Scan")
      ),
      h("div", { className: "editor-grid" },
        h("label", { style: { gridColumn: "1 / -1" } }, h("span", null, "Photo URL *"), h("input", { value: photoUrl, onChange: e => setPhotoUrl(e.target.value), required: true, placeholder: "https://..." })),
        h("label", null, h("span", null, "Make"), h("input", { value: make, onChange: e => setMake(e.target.value) })),
        h("label", null, h("span", null, "Model"), h("input", { value: model, onChange: e => setModel(e.target.value) })),
        h("label", null, h("span", null, "Year"), h("input", { type: "number", value: year, onChange: e => setYear(e.target.value) }))
      )
    ),
    result && result.success && h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, `Matches (${result.matchedParts.length}) — Confidence: ${result.confidence}`)),
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "Part"), h("th", null, "Internal Code"))),
        h("tbody", null, result.matchedParts.map((p, i) =>
          h("tr", { key: i }, h("td", null, p.partName), h("td", null, p.internalCode || "—"))
        ))
      ),
      h("p", { style: { fontSize: "12px", color: "#999", marginTop: "8px" } }, result.disclaimer)
    )
  );
}
