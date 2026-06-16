import { h, useCallback, useState } from "../core/react-runtime.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const emptyVehicleForm = {
  vin: "",
  make: "",
  model: "",
  year: "",
  trim: ""
};

export function CarCrushView({ api }) {
  const [vehicleForm, setVehicleForm] = useState({ ...emptyVehicleForm });
  const [isDecoding, setIsDecoding] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [isCreating, setIsCreating] = useState(false);
  const [vehicleDescription, setVehicleDescription] = useState("");
  const [checklist, setChecklist] = useState([]);
  const [status, setStatus] = useState("");
  const [createdCount, setCreatedCount] = useState(null);

  const setField = useCallback((key, value) => {
    setVehicleForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const togglePart = useCallback((index) => {
    setChecklist(prev => prev.map((p, i) =>
      i === index ? { ...p, selected: !p.selected } : p
    ));
  }, []);

  const decodeVin = useCallback(async () => {
    if (!vehicleForm.vin.trim()) {
      setStatus("Enter a VIN to decode.");
      return;
    }
    setIsDecoding(true);
    setStatus("Decoding VIN...");
    try {
      const result = await api.get(`/api/vehicle-profile/vin-decode?vin=${encodeURIComponent(vehicleForm.vin.trim())}`);
      if (result.success) {
        setVehicleForm(prev => ({
          ...prev,
          make: result.make || prev.make,
          model: result.model || prev.model,
          year: result.year ? String(result.year) : prev.year,
          trim: result.trim || prev.trim
        }));
        setStatus("VIN decoded.");
      } else {
        setStatus(result.errorMessage || "VIN decode failed.");
      }
    } catch (e) {
      setStatus(e.message || "Could not decode VIN.");
    } finally {
      setIsDecoding(false);
    }
  }, [api, vehicleForm.vin]);

  const handleGenerate = useCallback(async (e) => {
    e.preventDefault();
    if (!vehicleForm.make.trim() || !vehicleForm.model.trim()) {
      setStatus("Make and model are required.");
      return;
    }
    setIsGenerating(true);
    setStatus("Generating parts checklist...");
    setChecklist([]);
    setCreatedCount(null);
    try {
      const payload = {
        vin: vehicleForm.vin.trim() || null,
        make: vehicleForm.make.trim(),
        model: vehicleForm.model.trim(),
        year: vehicleForm.year ? Number(vehicleForm.year) : null,
        trim: vehicleForm.trim.trim() || null
      };
      const result = await api.post("/api/car-crush/checklist", payload);
      const parts = Array.isArray(result?.parts) ? result.parts : Array.isArray(result) ? result : [];
      setVehicleDescription(result?.vehicleDescription || [vehicleForm.year, vehicleForm.make, vehicleForm.model, vehicleForm.trim].filter(Boolean).join(" "));
      setChecklist(parts.map(p => ({
        ...p,
        selected: p.isHighValue || p.IsFastMoving || true
      })));
      setStatus(parts.length === 0 ? "No parts generated. Try again." : "");
    } catch (e) {
      setStatus(e.message || "Could not generate checklist.");
    } finally {
      setIsGenerating(false);
    }
  }, [api, vehicleForm]);

  const handleCreateListings = useCallback(async () => {
    const selected = checklist.filter(p => p.selected);
    if (selected.length === 0) {
      setStatus("Select at least one part to create listings.");
      return;
    }
    setIsCreating(true);
    setStatus("Creating draft listings...");
    try {
      const result = await api.post("/api/car-crush/create-listings", {
        vin: vehicleForm.vin.trim() || null,
        make: vehicleForm.make.trim(),
        model: vehicleForm.model.trim(),
        year: vehicleForm.year ? Number(vehicleForm.year) : null,
        trim: vehicleForm.trim.trim() || null,
        parts: selected.map(p => ({
          partName: p.partName || p.PartName,
          category: p.category || p.Category,
          oemHint: p.oemHint || p.OemHint || null
        }))
      });
      const count = result?.createdCount || selected.length;
      setCreatedCount(count);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not create listings.");
    } finally {
      setIsCreating(false);
    }
  }, [api, vehicleForm, checklist]);

  const selectedCount = checklist.filter(p => p.selected).length;

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "CarCrush",
      subtitle: "Auto-generate a parts checklist from a vehicle and create draft listings instantly."
    }),
    h(StatusLine, { status }),

    h("section", { className: "part-request-layout" },
      h("form", { className: "admin-panel request-intake-panel", onSubmit: handleGenerate },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "Vehicle Input"),
          h("button", {
            type: "submit",
            className: "primary-button",
            disabled: isGenerating
          }, isGenerating ? "Generating..." : "Generate Parts Checklist")
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "VIN (optional)"),
            h("div", { className: "form-row" },
              h("input", {
                value: vehicleForm.vin,
                onChange: e => setField("vin", e.target.value),
                placeholder: "Enter VIN for auto-fill"
              }),
              h("button", {
                type: "button",
                className: "secondary-button",
                onClick: decodeVin,
                disabled: isDecoding || !vehicleForm.vin.trim()
              }, isDecoding ? "Decoding..." : "Decode")
            )
          ),
          h("label", null,
            h("span", null, "Make *"),
            h("input", { value: vehicleForm.make, onChange: e => setField("make", e.target.value), required: true, placeholder: "e.g. BMW" })
          ),
          h("label", null,
            h("span", null, "Model *"),
            h("input", { value: vehicleForm.model, onChange: e => setField("model", e.target.value), required: true, placeholder: "e.g. E46" })
          ),
          h("label", null,
            h("span", null, "Year"),
            h("input", { type: "number", value: vehicleForm.year, onChange: e => setField("year", e.target.value), placeholder: "e.g. 2002" })
          ),
          h("label", null,
            h("span", null, "Trim"),
            h("input", { value: vehicleForm.trim, onChange: e => setField("trim", e.target.value), placeholder: "e.g. 320i" })
          )
        )
      ),

      h("section", { className: "table-panel" },
        createdCount !== null && h("div", { className: "data-card", style: { marginBottom: "12px" } },
          h("div", { className: "data-card-body" },
            h("strong", null, `${createdCount} draft listing${createdCount !== 1 ? "s" : ""} created.`),
            h("p", null, "Go to Parts to review and publish.")
          )
        ),

        checklist.length > 0 && h("div", null,
          vehicleDescription && h("div", { className: "module-summary" },
            h("div", null,
              h("span", null, "Vehicle"),
              h("strong", null, vehicleDescription)
            )
          ),
          h("div", { style: { display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "8px" } },
            h("span", null, `${selectedCount} of ${checklist.length} parts selected`),
            h("button", {
              type: "button",
              className: "primary-button",
              onClick: handleCreateListings,
              disabled: isCreating || selectedCount === 0
            }, isCreating ? "Creating..." : `Create Draft Listings for Selected Parts`)
          ),
          checklist.map((part, i) =>
            h("div", { key: i, className: "data-card", style: { marginBottom: "6px" } },
              h("div", { className: "data-card-header" },
                h("label", { style: { display: "flex", alignItems: "center", gap: "8px", cursor: "pointer" } },
                  h("input", {
                    type: "checkbox",
                    checked: part.selected,
                    onChange: () => togglePart(i)
                  }),
                  h("strong", null, part.partName || part.PartName)
                ),
                h("div", { className: "row-actions" },
                  (part.isHighValue || part.IsHighValue) && h(Badge, { tone: "warning" }, "High Value"),
                  (part.isFastMoving || part.IsFastMoving) && h(Badge, { tone: "info" }, "Fast Moving")
                )
              ),
              h("div", { className: "data-card-body" },
                (part.category || part.Category) && h("div", null, h("span", null, "Category: "), h("strong", null, part.category || part.Category)),
                (part.oemHint || part.OemHint) && h("div", null, h("span", null, "OEM Hint: "), h("strong", null, part.oemHint || part.OemHint))
              )
            )
          )
        ),

        checklist.length === 0 && !isGenerating && h("p", { className: "empty-state" }, "Enter vehicle details and click Generate Parts Checklist.")
      )
    )
  );
}
