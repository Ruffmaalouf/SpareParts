import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const emptyForm = {
  vin: "",
  make: "",
  model: "",
  year: "",
  trim: "",
  mileage: "",
  nickname: "",
  setAsDefault: false
};

function maskVin(vin) {
  if (!vin || vin.length < 4) return vin || "";
  return "*".repeat(vin.length - 4) + vin.slice(-4);
}

function VehicleCard({ vehicle, onSetDefault, onDelete, isSaving }) {
  return h("div", { className: "data-card" },
    h("div", { className: "data-card-header" },
      h("strong", null, vehicle.nickname || `${vehicle.year} ${vehicle.make} ${vehicle.model}`),
      vehicle.isDefault && h(Badge, { tone: "positive" }, "Default")
    ),
    h("div", { className: "data-card-body" },
      h("div", null,
        h("span", null, "Make: "),
        h("strong", null, vehicle.make)
      ),
      h("div", null,
        h("span", null, "Model: "),
        h("strong", null, vehicle.model)
      ),
      h("div", null,
        h("span", null, "Year: "),
        h("strong", null, vehicle.year)
      ),
      vehicle.trim && h("div", null,
        h("span", null, "Trim: "),
        h("strong", null, vehicle.trim)
      ),
      vehicle.mileage && h("div", null,
        h("span", null, "Mileage: "),
        h("strong", null, Number(vehicle.mileage).toLocaleString())
      ),
      vehicle.vinNumber && h("div", null,
        h("span", null, "VIN: "),
        h("strong", null, maskVin(vehicle.vinNumber))
      )
    ),
    h("div", { className: "row-actions" },
      !vehicle.isDefault && h("button", {
        type: "button",
        className: "secondary-button",
        onClick: () => onSetDefault(vehicle.id),
        disabled: isSaving
      }, "Set as Default"),
      h("button", {
        type: "button",
        className: "danger-button",
        onClick: () => onDelete(vehicle.id),
        disabled: isSaving
      }, "Delete")
    )
  );
}

export function MyGarageView({ api }) {
  const [vehicles, setVehicles] = useState([]);
  const [form, setForm] = useState({ ...emptyForm });
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isDecoding, setIsDecoding] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading vehicles...");
    try {
      const data = await api.get("/api/vehicle-profile");
      setVehicles(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load vehicles.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const setField = useCallback((key, value) => {
    setForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const decodeVin = useCallback(async () => {
    if (!form.vin.trim()) {
      setStatus("Enter a VIN to decode.");
      return;
    }
    setIsDecoding(true);
    setStatus("Decoding VIN...");
    try {
      const result = await api.get(`/api/vehicle-profile/vin-decode?vin=${encodeURIComponent(form.vin.trim())}`);
      if (result.success) {
        setForm(prev => ({
          ...prev,
          make: result.make || prev.make,
          model: result.model || prev.model,
          year: result.year ? String(result.year) : prev.year,
          trim: result.trim || prev.trim
        }));
        setStatus("VIN decoded successfully.");
      } else {
        setStatus(result.errorMessage || "VIN decode failed.");
      }
    } catch (e) {
      setStatus(e.message || "Could not decode VIN.");
    } finally {
      setIsDecoding(false);
    }
  }, [api, form.vin]);

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!form.make.trim() || !form.model.trim() || !form.year.trim()) {
      setStatus("Make, model, and year are required.");
      return;
    }
    setIsSaving(true);
    setStatus("Saving vehicle...");
    try {
      await api.post("/api/vehicle-profile", {
        make: form.make.trim(),
        model: form.model.trim(),
        year: Number(form.year),
        trim: form.trim.trim() || null,
        vinNumber: form.vin.trim() || null,
        mileage: form.mileage ? Number(form.mileage) : null,
        nickname: form.nickname.trim() || null,
        setAsDefault: form.setAsDefault
      });
      setForm({ ...emptyForm });
      setStatus("Vehicle saved.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not save vehicle.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, load]);

  const handleSetDefault = useCallback(async (id) => {
    setIsSaving(true);
    setStatus("Setting default vehicle...");
    try {
      await api.put(`/api/vehicle-profile/${id}/set-default`);
      setStatus("Default vehicle updated.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not set default vehicle.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const handleDelete = useCallback(async (id) => {
    if (!window.confirm("Delete this vehicle?")) return;
    setIsSaving(true);
    setStatus("Deleting vehicle...");
    try {
      await api.delete(`/api/vehicle-profile/${id}`);
      setStatus("Vehicle deleted.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not delete vehicle.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  const defaultVehicle = vehicles.find(v => v.isDefault);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "My Garage",
      subtitle: "Manage your saved vehicles.",
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: load,
        disabled: isLoading
      }, "Refresh")
    }),

    defaultVehicle && h("div", { className: "module-summary" },
      h("div", null,
        h("span", null, "Active Car"),
        h("strong", null, `${defaultVehicle.year} ${defaultVehicle.make} ${defaultVehicle.model}${defaultVehicle.trim ? " " + defaultVehicle.trim : ""}`)
      )
    ),

    h(StatusLine, { status }),

    h("section", { className: "part-request-layout" },
      h("form", { className: "admin-panel request-intake-panel", onSubmit: handleSubmit },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "Add Vehicle"),
          h("button", { className: "primary-button", type: "submit", disabled: isSaving }, "Save")
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "VIN (optional)"),
            h("div", { className: "form-row" },
              h("input", {
                value: form.vin,
                onChange: e => setField("vin", e.target.value),
                placeholder: "Enter VIN"
              }),
              h("button", {
                type: "button",
                className: "secondary-button",
                onClick: decodeVin,
                disabled: isDecoding || !form.vin.trim()
              }, isDecoding ? "Decoding..." : "Decode")
            )
          ),
          h("label", null,
            h("span", null, "Make *"),
            h("input", {
              value: form.make,
              onChange: e => setField("make", e.target.value),
              required: true,
              placeholder: "e.g. Toyota"
            })
          ),
          h("label", null,
            h("span", null, "Model *"),
            h("input", {
              value: form.model,
              onChange: e => setField("model", e.target.value),
              required: true,
              placeholder: "e.g. Corolla"
            })
          ),
          h("label", null,
            h("span", null, "Year *"),
            h("input", {
              type: "number",
              value: form.year,
              onChange: e => setField("year", e.target.value),
              required: true,
              placeholder: "e.g. 2019",
              min: "1900",
              max: "2100"
            })
          ),
          h("label", null,
            h("span", null, "Trim"),
            h("input", {
              value: form.trim,
              onChange: e => setField("trim", e.target.value),
              placeholder: "e.g. XLE"
            })
          ),
          h("label", null,
            h("span", null, "Mileage"),
            h("input", {
              type: "number",
              value: form.mileage,
              onChange: e => setField("mileage", e.target.value),
              placeholder: "km"
            })
          ),
          h("label", null,
            h("span", null, "Nickname"),
            h("input", {
              value: form.nickname,
              onChange: e => setField("nickname", e.target.value),
              placeholder: "e.g. My Daily Driver"
            })
          ),
          h("label", { className: "checkbox-field" },
            h("input", {
              type: "checkbox",
              checked: form.setAsDefault,
              onChange: e => setField("setAsDefault", e.target.checked)
            }),
            h("span", null, "Set as default vehicle")
          )
        )
      ),

      h("section", { className: "table-panel" },
        isLoading
          ? h("p", null, "Loading...")
          : vehicles.length === 0
            ? h("p", { className: "empty-state" }, "No vehicles saved yet. Add your first vehicle.")
            : h("div", { className: "data-card-grid" },
              vehicles.map(v =>
                h(VehicleCard, {
                  key: v.id,
                  vehicle: v,
                  onSetDefault: handleSetDefault,
                  onDelete: handleDelete,
                  isSaving
                })
              )
            )
      )
    )
  );
}
