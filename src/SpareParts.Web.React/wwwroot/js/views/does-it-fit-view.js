import { h, useCallback, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const VERDICT_CONFIG = {
  YES: { icon: "✓", color: "#2e7d32", background: "#e8f5e9", label: "YES — Compatible" },
  PROBABLY: { icon: "~", color: "#e65100", background: "#fff3e0", label: "PROBABLY Compatible" },
  NO: { icon: "✗", color: "#c62828", background: "#ffebee", label: "NO — Not Compatible" },
  UNKNOWN: { icon: "?", color: "#555", background: "#f5f5f5", label: "UNKNOWN" }
};

const emptyCompatForm = {
  partId: "",
  compatibleMakes: "",
  compatibleModels: "",
  yearFrom: "",
  yearTo: "",
  oemNumbers: "",
  alternativeNumbers: ""
};

export function DoesItFitView({ api }) {
  const [checkPartId, setCheckPartId] = useState("");
  const [checkMake, setCheckMake] = useState("");
  const [checkModel, setCheckModel] = useState("");
  const [checkYear, setCheckYear] = useState("");
  const [verdict, setVerdict] = useState(null);
  const [isChecking, setIsChecking] = useState(false);
  const [checkStatus, setCheckStatus] = useState("");

  const [managePartId, setManagePartId] = useState("");
  const [currentData, setCurrentData] = useState(null);
  const [compatForm, setCompatForm] = useState({ ...emptyCompatForm });
  const [isLoadingData, setIsLoadingData] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [manageStatus, setManageStatus] = useState("");

  const handleCheck = useCallback(async (e) => {
    e.preventDefault();
    if (!checkPartId || !checkMake.trim() || !checkModel.trim() || !checkYear) {
      setCheckStatus("Please fill in all fields.");
      return;
    }
    setIsChecking(true);
    setCheckStatus("Checking compatibility...");
    setVerdict(null);
    try {
      const params = new URLSearchParams({
        partId: checkPartId,
        make: checkMake.trim(),
        model: checkModel.trim(),
        year: checkYear
      });
      const data = await api.get(`/api/part-compatibility/check?${params.toString()}`);
      setVerdict(data);
      setCheckStatus("");
    } catch (err) {
      setCheckStatus(err.message || "Could not check compatibility.");
    } finally {
      setIsChecking(false);
    }
  }, [api, checkPartId, checkMake, checkModel, checkYear]);

  const handleLoadData = useCallback(async () => {
    if (!managePartId) {
      setManageStatus("Enter a Part ID first.");
      return;
    }
    setIsLoadingData(true);
    setManageStatus("Loading compatibility data...");
    setCurrentData(null);
    try {
      const data = await api.get(`/api/part-compatibility/${managePartId}`);
      setCurrentData(data);
      if (data) {
        setCompatForm({
          partId: managePartId,
          compatibleMakes: Array.isArray(data.compatibleMakes) ? data.compatibleMakes.join(", ") : (data.compatibleMakes || ""),
          compatibleModels: Array.isArray(data.compatibleModels) ? data.compatibleModels.join(", ") : (data.compatibleModels || ""),
          yearFrom: data.yearFrom != null ? String(data.yearFrom) : "",
          yearTo: data.yearTo != null ? String(data.yearTo) : "",
          oemNumbers: Array.isArray(data.oemNumbers) ? data.oemNumbers.join(", ") : (data.oemNumbers || ""),
          alternativeNumbers: Array.isArray(data.alternativeNumbers) ? data.alternativeNumbers.join(", ") : (data.alternativeNumbers || "")
        });
      } else {
        setCompatForm({ ...emptyCompatForm, partId: managePartId });
      }
      setManageStatus("");
    } catch (err) {
      setManageStatus(err.message || "Could not load compatibility data.");
    } finally {
      setIsLoadingData(false);
    }
  }, [api, managePartId]);

  const setCompatField = useCallback((key, value) => {
    setCompatForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const splitCsv = (str) => str.split(",").map(s => s.trim()).filter(Boolean);

  const handleSaveCompat = useCallback(async (e) => {
    e.preventDefault();
    if (!compatForm.partId) {
      setManageStatus("Part ID is required.");
      return;
    }
    setIsSaving(true);
    setManageStatus("Saving compatibility data...");
    try {
      await api.post("/api/part-compatibility", {
        partId: Number(compatForm.partId),
        compatibleMakes: splitCsv(compatForm.compatibleMakes),
        compatibleModels: splitCsv(compatForm.compatibleModels),
        yearFrom: compatForm.yearFrom ? Number(compatForm.yearFrom) : null,
        yearTo: compatForm.yearTo ? Number(compatForm.yearTo) : null,
        oemNumbers: splitCsv(compatForm.oemNumbers),
        alternativeNumbers: splitCsv(compatForm.alternativeNumbers)
      });
      setManageStatus("Compatibility data saved.");
      await handleLoadData();
    } catch (err) {
      setManageStatus(err.message || "Could not save compatibility data.");
    } finally {
      setIsSaving(false);
    }
  }, [api, compatForm, handleLoadData]);

  const verdictConfig = verdict && verdict.verdict ? VERDICT_CONFIG[verdict.verdict] || VERDICT_CONFIG.UNKNOWN : null;

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Does It Fit?",
      subtitle: "Check if a part is compatible with a specific vehicle."
    }),

    h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Check Compatibility")
      ),
      h("form", { className: "editor-grid", onSubmit: handleCheck },
        h("label", null,
          h("span", null, "Part ID *"),
          h("input", {
            type: "number",
            value: checkPartId,
            onChange: e => setCheckPartId(e.target.value),
            required: true,
            placeholder: "Enter part ID"
          })
        ),
        h("label", null,
          h("span", null, "Make *"),
          h("input", {
            value: checkMake,
            onChange: e => setCheckMake(e.target.value),
            required: true,
            placeholder: "e.g. Toyota"
          })
        ),
        h("label", null,
          h("span", null, "Model *"),
          h("input", {
            value: checkModel,
            onChange: e => setCheckModel(e.target.value),
            required: true,
            placeholder: "e.g. Corolla"
          })
        ),
        h("label", null,
          h("span", null, "Year *"),
          h("input", {
            type: "number",
            value: checkYear,
            onChange: e => setCheckYear(e.target.value),
            required: true,
            placeholder: "e.g. 2018"
          })
        ),
        h("div", { style: { gridColumn: "1 / -1" } },
          h("button", { type: "submit", className: "primary-button", disabled: isChecking },
            isChecking ? "Checking..." : "Check Compatibility"
          )
        )
      ),
      checkStatus && h(StatusLine, { status: checkStatus })
    ),

    verdictConfig && verdict && h("div", {
      style: {
        background: verdictConfig.background,
        borderRadius: "12px",
        padding: "24px",
        marginBottom: "16px",
        textAlign: "center"
      }
    },
      h("div", {
        style: {
          fontSize: "64px", fontWeight: "bold",
          color: verdictConfig.color, lineHeight: 1
        }
      }, verdictConfig.icon),
      h("div", { style: { fontSize: "24px", fontWeight: "bold", color: verdictConfig.color, marginTop: "8px" } },
        verdictConfig.label
      ),
      verdict.reason && h("p", { style: { color: "#555", marginTop: "8px" } }, verdict.reason),

      (verdict.compatibleMakes || verdict.compatibleModels || verdict.oemNumbers) && h("div", {
        style: { marginTop: "16px", textAlign: "left", maxWidth: "480px", margin: "16px auto 0" }
      },
        verdict.compatibleMakes && verdict.compatibleMakes.length > 0 && h("div", null,
          h("strong", null, "Compatible Makes: "),
          h("span", null, verdict.compatibleMakes.join(", "))
        ),
        verdict.compatibleModels && verdict.compatibleModels.length > 0 && h("div", null,
          h("strong", null, "Compatible Models: "),
          h("span", null, verdict.compatibleModels.join(", "))
        ),
        (verdict.yearFrom || verdict.yearTo) && h("div", null,
          h("strong", null, "Year Range: "),
          h("span", null, `${verdict.yearFrom || "?"} – ${verdict.yearTo || "?"}`)
        ),
        verdict.oemNumbers && verdict.oemNumbers.length > 0 && h("div", null,
          h("strong", null, "OEM Numbers: "),
          h("span", null, verdict.oemNumbers.join(", "))
        )
      )
    ),

    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Manage Compatibility Data")
      ),
      h("div", { className: "editor-grid", style: { marginBottom: "12px" } },
        h("label", null,
          h("span", null, "Part ID"),
          h("input", {
            type: "number",
            value: managePartId,
            onChange: e => setManagePartId(e.target.value),
            placeholder: "Enter part ID to manage"
          })
        ),
        h("div", { style: { display: "flex", alignItems: "flex-end" } },
          h("button", {
            type: "button", className: "secondary-button",
            onClick: handleLoadData, disabled: isLoadingData || !managePartId
          }, isLoadingData ? "Loading..." : "Load Data")
        )
      ),

      manageStatus && h(StatusLine, { status: manageStatus }),

      (currentData !== null || managePartId) && h("form", { className: "editor-grid", onSubmit: handleSaveCompat },
        h("label", null,
          h("span", null, "Compatible Makes (comma-separated)"),
          h("input", {
            value: compatForm.compatibleMakes,
            onChange: e => setCompatField("compatibleMakes", e.target.value),
            placeholder: "Toyota, Honda, Nissan"
          })
        ),
        h("label", null,
          h("span", null, "Compatible Models (comma-separated)"),
          h("input", {
            value: compatForm.compatibleModels,
            onChange: e => setCompatField("compatibleModels", e.target.value),
            placeholder: "Corolla, Civic, Sentra"
          })
        ),
        h("label", null,
          h("span", null, "Year From"),
          h("input", {
            type: "number",
            value: compatForm.yearFrom,
            onChange: e => setCompatField("yearFrom", e.target.value),
            placeholder: "e.g. 2010"
          })
        ),
        h("label", null,
          h("span", null, "Year To"),
          h("input", {
            type: "number",
            value: compatForm.yearTo,
            onChange: e => setCompatField("yearTo", e.target.value),
            placeholder: "e.g. 2020"
          })
        ),
        h("label", null,
          h("span", null, "OEM Numbers (comma-separated)"),
          h("input", {
            value: compatForm.oemNumbers,
            onChange: e => setCompatField("oemNumbers", e.target.value),
            placeholder: "12345-67890, OEM-ABC"
          })
        ),
        h("label", null,
          h("span", null, "Alternative Numbers (comma-separated)"),
          h("input", {
            value: compatForm.alternativeNumbers,
            onChange: e => setCompatField("alternativeNumbers", e.target.value),
            placeholder: "ALT-001, PART-XYZ"
          })
        ),
        h("div", { style: { gridColumn: "1 / -1" } },
          h("button", { type: "submit", className: "primary-button", disabled: isSaving },
            isSaving ? "Saving..." : "Save Compatibility Data"
          )
        )
      )
    )
  );
}
