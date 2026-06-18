import { h, useCallback, useState } from "../core/react-runtime.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const LANGUAGE_OPTIONS = ["English", "Arabic", "French"];

const emptyForm = {
  symptoms: "",
  language: "English",
  make: "",
  model: ""
};

function ConfidenceBar({ percent }) {
  const pct = Math.max(0, Math.min(100, Number(percent) || 0));
  const color = pct >= 75 ? "#22c55e" : pct >= 45 ? "#f59e0b" : "#ef4444";
  return h("div", { style: { marginTop: "4px" } },
    h("div", { style: { background: "#e5e7eb", borderRadius: "4px", height: "8px", overflow: "hidden" } },
      h("div", { style: { width: `${pct}%`, background: color, height: "100%", borderRadius: "4px", transition: "width 0.3s" } })
    ),
    h("small", null, `${pct}% confidence`)
  );
}

function ResultCard({ result, onFindParts }) {
  return h("div", { className: "data-card" },
    h("div", { className: "data-card-header" },
      h("strong", null, result.suggestedPartName || result.SuggestedPartName || "Unknown Part"),
      result.suggestedPartNameAr || result.SuggestedPartNameAr
        ? h("span", null, result.suggestedPartNameAr || result.SuggestedPartNameAr)
        : null
    ),
    h("div", { className: "data-card-body" },
      h(ConfidenceBar, { percent: result.confidencePercent || result.ConfidencePercent }),
      (result.reasoning || result.Reasoning) && h("p", null, result.reasoning || result.Reasoning),
      h("div", { style: { display: "flex", flexWrap: "wrap", gap: "6px", marginTop: "6px" } },
        (result.searchTerms || result.SearchTerms || []).map((term, i) =>
          h("button", {
            key: i,
            type: "button",
            className: "chip",
            onClick: () => onFindParts(term)
          }, term)
        )
      )
    ),
    h("div", { className: "row-actions" },
      h("button", {
        type: "button",
        className: "primary-button",
        onClick: () => onFindParts(result.suggestedPartName || result.SuggestedPartName || "")
      }, "Find Parts")
    )
  );
}

export function SymptomSearchView({ api, navigate }) {
  const [form, setForm] = useState({ ...emptyForm });
  const [results, setResults] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const setField = useCallback((key, value) => {
    setForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleDiagnose = useCallback(async (e) => {
    e.preventDefault();
    if (!form.symptoms.trim()) {
      setStatus("Please describe your symptoms.");
      return;
    }
    setIsLoading(true);
    setStatus("Analyzing symptoms...");
    setResults([]);
    try {
      const payload = {
        symptoms: form.symptoms.trim(),
        language: form.language,
        vehicleMake: form.make.trim() || null,
        vehicleModel: form.model.trim() || null
      };
      const data = await api.post("/api/symptom-search", payload);
      const arr = Array.isArray(data) ? data : data?.results || [];
      setResults(arr);
      setStatus(arr.length === 0 ? "No suggestions found. Try describing the symptom differently." : "");
    } catch (e) {
      setStatus(e.message || "Could not run diagnosis.");
    } finally {
      setIsLoading(false);
    }
  }, [api, form]);

  const handleFindParts = useCallback((term) => {
    if (navigate) {
      navigate("inventory", { search: term });
    } else {
      window.location.hash = `#inventory?search=${encodeURIComponent(term)}`;
    }
  }, [navigate]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Symptom Finder",
      subtitle: "Describe what your car is doing — we'll suggest the likely parts."
    }),
    h(StatusLine, { status }),

    h("section", { className: "part-request-layout" },
      h("form", { className: "admin-panel request-intake-panel", onSubmit: handleDiagnose },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "Describe the Problem"),
          h("button", {
            type: "submit",
            className: "primary-button",
            disabled: isLoading
          }, isLoading ? "Diagnosing..." : "Diagnose")
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "Symptoms *"),
            h("textarea", {
              value: form.symptoms,
              onChange: e => setField("symptoms", e.target.value),
              rows: 4,
              placeholder: "e.g. clicking when turning left, car shakes when braking at high speed",
              required: true
            })
          ),
          h("label", null,
            h("span", null, "Language"),
            h("select", { value: form.language, onChange: e => setField("language", e.target.value) },
              LANGUAGE_OPTIONS.map(l => h("option", { key: l, value: l }, l))
            )
          ),
          h("label", null,
            h("span", null, "Vehicle Make (optional)"),
            h("input", {
              value: form.make,
              onChange: e => setField("make", e.target.value),
              placeholder: "e.g. Toyota"
            })
          ),
          h("label", null,
            h("span", null, "Vehicle Model (optional)"),
            h("input", {
              value: form.model,
              onChange: e => setField("model", e.target.value),
              placeholder: "e.g. Camry"
            })
          )
        )
      ),

      h("section", { className: "table-panel" },
        results.length === 0 && !isLoading
          ? h("p", { className: "empty-state" }, "Enter symptoms above and click Diagnose to see suggested parts.")
          : h("div", { className: "data-card-grid" },
            results.map((r, i) =>
              h(ResultCard, { key: i, result: r, onFindParts: handleFindParts })
            )
          )
      )
    )
  );
}
