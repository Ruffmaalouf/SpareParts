import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const emptyLinkForm = {
  partName: "",
  price: "",
  currency: "USD",
  location: "",
  listingUrl: "",
  vehicleMake: "",
  vehicleModel: "",
  vehicleYear: "",
  sellerName: "",
  sellerPhone: ""
};

function buildWhatsAppMessage({ partName, price, currency, location, listingUrl, vehicleMake, vehicleModel, vehicleYear, sellerName, sellerPhone }) {
  const lines = [];
  lines.push(`*${partName || "Part"}*`);
  if (price) lines.push(`Price: ${currency || "USD"} ${price}`);
  const vehicle = [vehicleYear, vehicleMake, vehicleModel].filter(Boolean).join(" ");
  if (vehicle) lines.push(`Fits: ${vehicle}`);
  if (location) lines.push(`Location: ${location}`);
  if (sellerName) lines.push(`Seller: ${sellerName}`);
  if (sellerPhone) lines.push(`Contact: ${sellerPhone}`);
  if (listingUrl) lines.push(`View listing: ${listingUrl}`);
  lines.push("\nSent via SpareParts App");
  return lines.join("\n");
}

export function WhatsAppSellingView({ api }) {
  const [sellerPhone, setSellerPhone] = useState("");
  const [savedPhone, setSavedPhone] = useState("");
  const [linkForm, setLinkForm] = useState({ ...emptyLinkForm });
  const [generatedLink, setGeneratedLink] = useState("");
  const [status, setStatus] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    api.get("/api/app-constants").then(data => {
      const arr = Array.isArray(data) ? data : [];
      const row = arr.find(r => (r.key || r.Key || "").toLowerCase() === "whatsappnumber");
      const val = row ? (row.value || row.Value || "") : "";
      if (val) { setSavedPhone(val); setSellerPhone(val); }
    }).catch(() => {});
  }, [api]);

  const setLinkField = useCallback((key, value) => {
    setLinkForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleSavePhone = useCallback(async (e) => {
    e.preventDefault();
    if (!sellerPhone.trim()) {
      setStatus("Please enter a WhatsApp number.");
      return;
    }
    setIsSaving(true);
    setStatus("Saving...");
    try {
      await api.post("/api/app-constants", { key: "WhatsAppNumber", value: sellerPhone.trim() });
      setSavedPhone(sellerPhone.trim());
      setStatus("WhatsApp number saved.");
    } catch (e) {
      setStatus(e.message || "Could not save number.");
    } finally {
      setIsSaving(false);
    }
  }, [api, sellerPhone]);

  const handleGenerate = useCallback((e) => {
    e.preventDefault();
    const phone = (savedPhone || "").replace(/\D/g, "");
    if (!phone) {
      setStatus("Save your WhatsApp number first.");
      return;
    }
    const text = buildWhatsAppMessage({
      ...linkForm,
      sellerPhone: linkForm.sellerPhone || savedPhone
    });
    const url = `https://wa.me/${phone}?text=${encodeURIComponent(text)}`;
    setGeneratedLink(url);
    setStatus("");
  }, [savedPhone, linkForm]);

  const handleCopy = useCallback(() => {
    if (!generatedLink) return;
    navigator.clipboard.writeText(generatedLink).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    });
  }, [generatedLink]);

  const handleExport = useCallback(async () => {
    setIsExporting(true);
    setStatus("Exporting catalog...");
    try {
      const token = localStorage.getItem("token");
      const res = await fetch("/api/marketplace/catalog-export", {
        headers: token ? { Authorization: `Bearer ${token}` } : {}
      });
      if (!res.ok) throw new Error(`Export failed: ${res.status}`);
      const blob = await res.blob();
      const contentType = res.headers.get("content-type") || "";
      const ext = contentType.includes("csv") ? "csv" : "json";
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `parts-catalog.${ext}`;
      a.click();
      URL.revokeObjectURL(url);
      setStatus("Catalog exported.");
    } catch (e) {
      setStatus(e.message || "Could not export catalog.");
    } finally {
      setIsExporting(false);
    }
  }, []);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "WhatsApp Selling",
      subtitle: "Generate shareable WhatsApp links and export your parts catalog."
    }),
    h(StatusLine, { status }),

    h("section", { className: "part-request-layout" },
      h("div", { className: "admin-panel request-intake-panel" },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "Your WhatsApp Number")
        ),
        h("form", { onSubmit: handleSavePhone },
          h("div", { className: "editor-grid" },
            h("label", null,
              h("span", null, "WhatsApp Number"),
              h("input", {
                value: sellerPhone,
                onChange: e => setSellerPhone(e.target.value),
                placeholder: "+961XXXXXXXX",
                type: "tel"
              })
            )
          ),
          h("div", { className: "row-actions", style: { marginTop: "8px" } },
            h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Save Number")
          )
        ),

        h("div", { style: { marginTop: "24px" } },
          h("div", { className: "admin-panel-header" },
            h("h3", null, "Generate WhatsApp Link")
          ),
          h("form", { onSubmit: handleGenerate },
            h("div", { className: "editor-grid" },
              h("label", null,
                h("span", null, "Part Name *"),
                h("input", { value: linkForm.partName, onChange: e => setLinkField("partName", e.target.value), placeholder: "e.g. Front Bumper BMW E46", required: true })
              ),
              h("label", null,
                h("span", null, "Price"),
                h("input", { type: "number", value: linkForm.price, onChange: e => setLinkField("price", e.target.value), placeholder: "0.00" })
              ),
              h("label", null,
                h("span", null, "Currency"),
                h("select", { value: linkForm.currency, onChange: e => setLinkField("currency", e.target.value) },
                  h("option", { value: "USD" }, "USD"),
                  h("option", { value: "LBP" }, "LBP")
                )
              ),
              h("label", null,
                h("span", null, "Vehicle Make"),
                h("input", { value: linkForm.vehicleMake, onChange: e => setLinkField("vehicleMake", e.target.value), placeholder: "e.g. BMW" })
              ),
              h("label", null,
                h("span", null, "Vehicle Model"),
                h("input", { value: linkForm.vehicleModel, onChange: e => setLinkField("vehicleModel", e.target.value), placeholder: "e.g. E46" })
              ),
              h("label", null,
                h("span", null, "Vehicle Year"),
                h("input", { type: "number", value: linkForm.vehicleYear, onChange: e => setLinkField("vehicleYear", e.target.value), placeholder: "e.g. 2002" })
              ),
              h("label", null,
                h("span", null, "Location"),
                h("input", { value: linkForm.location, onChange: e => setLinkField("location", e.target.value), placeholder: "e.g. Beirut, Lebanon" })
              ),
              h("label", null,
                h("span", null, "Listing URL (optional)"),
                h("input", { value: linkForm.listingUrl, onChange: e => setLinkField("listingUrl", e.target.value), placeholder: "https://..." })
              ),
              h("label", null,
                h("span", null, "Your Name (for message)"),
                h("input", { value: linkForm.sellerName, onChange: e => setLinkField("sellerName", e.target.value), placeholder: "Seller name" })
              )
            ),
            h("div", { className: "row-actions", style: { marginTop: "8px" } },
              h("button", { type: "submit", className: "primary-button" }, "Generate Link")
            )
          )
        )
      ),

      h("section", { className: "table-panel" },
        generatedLink && h("div", { className: "data-card", style: { marginBottom: "16px" } },
          h("div", { className: "data-card-header" },
            h("strong", null, "Your WhatsApp Link")
          ),
          h("div", { className: "data-card-body" },
            h("p", { style: { wordBreak: "break-all", fontSize: "0.85em" } }, generatedLink)
          ),
          h("div", { className: "row-actions" },
            h("button", { type: "button", className: "secondary-button", onClick: handleCopy },
              copied ? "Copied!" : "Copy Link"),
            h("a", {
              href: generatedLink,
              target: "_blank",
              rel: "noopener noreferrer",
              className: "primary-button"
            }, "Test on WhatsApp")
          )
        ),

        h("div", { className: "data-card" },
          h("div", { className: "data-card-header" },
            h("strong", null, "Catalog Export")
          ),
          h("div", { className: "data-card-body" },
            h("p", null, "Export your full parts catalog as a file you can share via WhatsApp.")
          ),
          h("div", { className: "row-actions" },
            h("button", {
              type: "button",
              className: "primary-button",
              onClick: handleExport,
              disabled: isExporting
            }, isExporting ? "Exporting..." : "Export Parts Catalog for WhatsApp")
          )
        )
      )
    )
  );
}
