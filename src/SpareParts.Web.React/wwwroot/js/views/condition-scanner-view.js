import { h, useCallback, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function ConditionScannerView({ api }) {
  const [partId, setPartId] = useState("");
  const [photos, setPhotos] = useState("");
  const [notes, setNotes] = useState("");
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!partId) { setStatus("Enter a part ID."); return; }
    const photoList = photos.split("\n").map(s => s.trim()).filter(Boolean);
    if (photoList.length === 0) { setStatus("Add at least one photo URL."); return; }
    setIsLoading(true); setStatus("Scanning..."); setResult(null);
    try {
      const data = await api.post("/api/condition-scanner/scan", { partId: Number(partId), photoUrls: photoList, notes: notes || null });
      setResult(data); setStatus("");
    } catch (err) { setStatus(err.message || "Scan failed."); }
    finally { setIsLoading(false); }
  }, [api, partId, photos, notes]);

  const gradeColor = { A: "#388e3c", B: "#1976d2", C: "#ff9800", D: "#d32f2f" };

  return h("section", { className: "screen" },
    h(PageHeader, { title: "PartDNA Condition Scanner", subtitle: "AI-simulated condition grading from photos." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleSubmit, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Scan Part Condition"),
        h("button", { type: "submit", className: "primary-button", disabled: isLoading }, isLoading ? "Scanning..." : "Scan & Grade")
      ),
      h("div", { className: "editor-grid" },
        h("label", null,
          h("span", null, "Part ID *"),
          h("input", { type: "number", value: partId, onChange: e => setPartId(e.target.value), placeholder: "e.g. 42", required: true })
        ),
        h("label", { style: { gridColumn: "1 / -1" } },
          h("span", null, "Photo URLs (one per line) *"),
          h("textarea", { value: photos, onChange: e => setPhotos(e.target.value), rows: 4, placeholder: "https://..." })
        ),
        h("label", { style: { gridColumn: "1 / -1" } },
          h("span", null, "Notes (optional)"),
          h("textarea", { value: notes, onChange: e => setNotes(e.target.value), rows: 2, placeholder: "e.g. slight scratches on surface" })
        )
      )
    ),
    result && h("div", { className: "data-card" },
      h("div", { className: "data-card-header" },
        h("strong", null, `Certificate #${result.certificateNumber}`),
        h("span", {
          style: { marginLeft: "8px", fontWeight: "bold", fontSize: "20px", color: gradeColor[result.gradeLabel] || "#333" }
        }, `Grade ${result.gradeLabel}`)
      ),
      h("div", { className: "data-card-body" },
        h("p", null, `Method: ${result.scanMethod}`),
        result.notes && h("p", null, `Notes: ${result.notes}`),
        result.isExpired && h("p", { style: { color: "#d32f2f" } }, "⚠ Certificate expired (>90 days)")
      )
    )
  );
}
