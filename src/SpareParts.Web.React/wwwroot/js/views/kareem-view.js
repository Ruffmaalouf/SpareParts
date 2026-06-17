import { h, useCallback, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function KareemView({ api }) {
  const [history, setHistory] = useState([]);
  const [message, setMessage] = useState("");
  const [language, setLanguage] = useState("en");
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");

  const handleSend = useCallback(async (e) => {
    e.preventDefault();
    if (!message.trim()) return;
    const userMsg = { role: "user", text: message };
    setHistory(h => [...h, userMsg]);
    setIsLoading(true); setMessage("");
    try {
      const data = await api.post("/api/kareem/chat", { message: userMsg.text, language, history: history.map(m => ({ role: m.role === "user" ? 1 : 2, content: m.text })) });
      setHistory(h => [...h, { role: "assistant", text: data.reply, suggestedActions: data.suggestedActions }]);
      setStatus("");
    } catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api, message, language, history]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "AutoChat Kareem", subtitle: "Your bilingual AI assistant for parts questions." }),
    h(StatusLine, { status }),
    h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Chat"),
        h("select", { value: language, onChange: e => setLanguage(e.target.value) },
          h("option", { value: "en" }, "English"), h("option", { value: "ar" }, "العربية")
        )
      ),
      h("div", { style: { padding: "12px", maxHeight: "400px", overflowY: "auto" } },
        history.map((m, i) =>
          h("div", { key: i, style: { marginBottom: "10px", textAlign: m.role === "user" ? "right" : "left" } },
            h("div", { style: { display: "inline-block", padding: "8px 12px", borderRadius: "12px", background: m.role === "user" ? "#1976d2" : "#eee", color: m.role === "user" ? "#fff" : "#333", maxWidth: "70%" } }, m.text)
          )
        ),
        isLoading && h("p", { style: { color: "#999" } }, "Kareem is typing...")
      ),
      h("form", { onSubmit: handleSend, style: { display: "flex", gap: "8px", padding: "12px", borderTop: "1px solid #eee" } },
        h("input", { value: message, onChange: e => setMessage(e.target.value), placeholder: "Ask Kareem about a part...", style: { flex: 1 } }),
        h("button", { type: "submit", className: "primary-button", disabled: isLoading }, "Send")
      )
    )
  );
}
