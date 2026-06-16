import { h, useCallback, useEffect, useRef, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const LANGUAGES = [
  { value: "en-US", label: "English (US)" },
  { value: "ar-LB", label: "Arabic (Lebanese)" },
  { value: "fr-FR", label: "French" }
];

export function VoiceSearchView({ api }) {
  const [language, setLanguage] = useState("en-US");
  const [transcript, setTranscript] = useState("");
  const [isListening, setIsListening] = useState(false);
  const [speechError, setSpeechError] = useState("");
  const [status, setStatus] = useState("");
  const [results, setResults] = useState([]);
  const [isSearching, setIsSearching] = useState(false);
  const [hintMake, setHintMake] = useState("");
  const [hintModel, setHintModel] = useState("");
  const [hintYear, setHintYear] = useState("");

  const recognitionRef = useRef(null);

  const SpeechRecognition = typeof window !== "undefined"
    ? (window.SpeechRecognition || window.webkitSpeechRecognition)
    : null;

  const speechSupported = !!SpeechRecognition;

  const stopListening = useCallback(() => {
    if (recognitionRef.current) {
      try { recognitionRef.current.stop(); } catch (_) {}
      recognitionRef.current = null;
    }
    setIsListening(false);
  }, []);

  useEffect(() => {
    return () => { stopListening(); };
  }, [stopListening]);

  const startListening = useCallback(() => {
    if (!SpeechRecognition) return;
    setSpeechError("");
    setTranscript("");

    const recognition = new SpeechRecognition();
    recognition.lang = language;
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;

    recognition.onstart = () => setIsListening(true);

    recognition.onresult = (event) => {
      const text = event.results[0][0].transcript;
      setTranscript(text);
      setIsListening(false);
    };

    recognition.onerror = (event) => {
      setSpeechError("Speech recognition error: " + (event.error || "unknown error") + ". Please type your search below.");
      setIsListening(false);
    };

    recognition.onend = () => {
      setIsListening(false);
    };

    recognitionRef.current = recognition;
    recognition.start();
  }, [SpeechRecognition, language]);

  const handleMicClick = useCallback(() => {
    if (isListening) {
      stopListening();
    } else {
      startListening();
    }
  }, [isListening, startListening, stopListening]);

  const handleSearch = useCallback(async (e) => {
    e.preventDefault();
    if (!transcript.trim()) {
      setStatus("Please say or type something to search.");
      return;
    }
    setIsSearching(true);
    setStatus("Searching...");
    setResults([]);
    try {
      const data = await api.post("/api/voice-search", {
        transcript: transcript.trim(),
        language,
        hintVehicleMake: hintMake.trim() || null,
        hintVehicleModel: hintModel.trim() || null,
        hintVehicleYear: hintYear ? Number(hintYear) : null
      });
      const rows = Array.isArray(data) ? data : (data && data.results ? data.results : []);
      setResults(rows);
      setStatus(rows.length === 0 ? "No results found." : "");
    } catch (err) {
      setStatus(err.message || "Search failed.");
    } finally {
      setIsSearching(false);
    }
  }, [api, transcript, language, hintMake, hintModel, hintYear]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Voice Search",
      subtitle: "Speak the part name and let us find it for you."
    }),
    h(StatusLine, { status }),

    h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Voice Input")
      ),
      h("div", { className: "editor-grid" },
        h("label", null,
          h("span", null, "Language"),
          h("select", {
            value: language,
            onChange: e => setLanguage(e.target.value),
            disabled: isListening
          },
            LANGUAGES.map(l => h("option", { key: l.value, value: l.value }, l.label))
          )
        )
      ),

      !speechSupported && h("div", {
        style: {
          background: "#fff3cd", border: "1px solid #ffc107",
          borderRadius: "6px", padding: "12px", margin: "12px 0"
        }
      }, "Voice search is not supported in this browser. Please type your search below."),

      speechSupported && h("div", { style: { textAlign: "center", margin: "24px 0" } },
        h("button", {
          type: "button",
          onClick: handleMicClick,
          style: {
            fontSize: "48px", background: "none", border: "none",
            cursor: "pointer", padding: "16px",
            borderRadius: "50%",
            background: isListening ? "#ffebee" : "#e3f2fd",
            lineHeight: 1
          },
          title: isListening ? "Stop listening" : "Start listening"
        }, "🎤"),
        h("div", { style: { marginTop: "8px", fontWeight: "bold", color: isListening ? "#d32f2f" : "#555" } },
          isListening ? "Listening..." : "Tap to speak"
        )
      ),

      speechError && h("p", { style: { color: "#d32f2f", marginBottom: "8px" } }, speechError),

      h("form", { onSubmit: handleSearch },
        h("label", null,
          h("span", null, "Transcript (editable)"),
          h("textarea", {
            value: transcript,
            onChange: e => setTranscript(e.target.value),
            rows: 3,
            placeholder: "Transcript will appear here, or type your search...",
            style: { width: "100%" }
          })
        ),

        h("div", { className: "editor-grid", style: { marginTop: "12px" } },
          h("label", null,
            h("span", null, "Vehicle Make (hint)"),
            h("input", {
              value: hintMake,
              onChange: e => setHintMake(e.target.value),
              placeholder: "e.g. Toyota"
            })
          ),
          h("label", null,
            h("span", null, "Vehicle Model (hint)"),
            h("input", {
              value: hintModel,
              onChange: e => setHintModel(e.target.value),
              placeholder: "e.g. Corolla"
            })
          ),
          h("label", null,
            h("span", null, "Vehicle Year (hint)"),
            h("input", {
              type: "number",
              value: hintYear,
              onChange: e => setHintYear(e.target.value),
              placeholder: "e.g. 2017"
            })
          )
        ),

        h("div", { style: { marginTop: "12px" } },
          h("button", {
            type: "submit",
            className: "primary-button",
            disabled: isSearching || !transcript.trim()
          }, isSearching ? "Searching..." : "Search")
        )
      )
    ),

    results.length > 0 && h("div", null,
      h("h3", { style: { marginBottom: "12px" } }, `Results (${results.length})`),
      h("div", { className: "data-card-grid" },
        results.map((item, idx) =>
          h("div", { key: item.id || idx, className: "data-card" },
            h("div", { className: "data-card-header" },
              h("strong", null, item.name || item.partName || "Part"),
              item.code && h(Badge, { tone: "neutral" }, item.code)
            ),
            h("div", { className: "data-card-body" },
              item.price != null && h("div", null,
                h("span", null, "Price: "),
                h("strong", null, money(item.price, item.currency || "USD"))
              ),
              item.id && h("div", null,
                h("small", null, `ID: ${item.id}`)
              )
            )
          )
        )
      )
    )
  );
}
