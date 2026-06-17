import { h, useCallback, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function QrTagView({ api }) {
  const [partId, setPartId] = useState("");
  const [result, setResult] = useState(null);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");

  const handleGenerate = useCallback(async (e) => {
    e.preventDefault();
    if (!partId) { setStatus("Enter a part ID."); return; }
    setIsLoading(true); setStatus("Generating..."); setResult(null);
    try {
      const data = await api.get(`/api/qr-tag/${partId}`);
      setResult(data); setStatus("");
    } catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api, partId]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "QRTag System", subtitle: "Generate QR codes for physical part tags." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleGenerate, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Generate QR Code"),
        h("button", { type: "submit", className: "primary-button", disabled: isLoading }, isLoading ? "Generating..." : "Generate")
      ),
      h("div", { className: "editor-grid" },
        h("label", null,
          h("span", null, "Part ID *"),
          h("input", { type: "number", value: partId, onChange: e => setPartId(e.target.value), required: true, placeholder: "e.g. 42" })
        )
      )
    ),
    result && h("div", { className: "data-card" },
      h("div", { className: "data-card-header" },
        h("strong", null, result.partName),
        result.internalCode && h("span", { style: { color: "#888", marginLeft: "8px" } }, result.internalCode)
      ),
      h("div", { className: "data-card-body", style: { display: "flex", gap: "24px", flexWrap: "wrap" } },
        h("img", { src: result.qrCodeDataUrl, alt: "QR Code", style: { width: "150px", height: "150px", border: "1px solid #ddd", borderRadius: "4px" } }),
        h("div", null,
          h("p", { style: { fontWeight: "bold", marginBottom: "8px" } }, "Public URL:"),
          h("a", { href: result.publicUrl, target: "_blank", style: { color: "#1976d2" } }, result.publicUrl),
          h("p", { style: { fontSize: "12px", color: "#888", marginTop: "12px", wordBreak: "break-all" } }, result.qrPayload)
        )
      )
    )
  );
}
