import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function ApiPlatformView({ api }) {
  const [keys, setKeys] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [name, setName] = useState("");
  const [scopes, setScopes] = useState("");
  const [createdKey, setCreatedKey] = useState(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try { setKeys((await api.get("/api/api-platform/keys")) || []); }
    catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const handleCreate = useCallback(async (e) => {
    e.preventDefault();
    if (!name) { setStatus("Name required."); return; }
    try {
      const data = await api.post("/api/api-platform/keys", {
        name, scopes: scopes ? scopes.split(",").map(s => s.trim()).filter(Boolean) : []
      });
      setCreatedKey(data); setStatus("Key created — copy it now, it won't be shown again."); setName(""); setScopes("");
      load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, name, scopes, load]);

  const handleRevoke = useCallback(async (id) => {
    try { await api.delete(`/api/api-platform/keys/${id}`); setStatus("Key revoked."); load(); }
    catch (err) { setStatus(err.message || "Failed."); }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "API Platform Foundation", subtitle: "Manage API keys for third-party integrations." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleCreate, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Create API Key"), h("button", { type: "submit", className: "primary-button" }, "Create")),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Name *"), h("input", { value: name, onChange: e => setName(e.target.value), required: true })),
        h("label", null, h("span", null, "Scopes (comma-separated)"), h("input", { value: scopes, onChange: e => setScopes(e.target.value), placeholder: "parts.read, orders.read" }))
      )
    ),
    createdKey && h("div", { className: "data-card", style: { marginBottom: "16px", border: "2px solid #ff9800" } },
      h("div", { className: "data-card-header" }, h("strong", null, "New Key — copy now")),
      h("div", { className: "data-card-body", style: { wordBreak: "break-all", fontFamily: "monospace" } }, createdKey.plainKey)
    ),
    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, `Keys (${keys.length})`)),
      isLoading ? h("p", { style: { padding: "12px" } }, "Loading...") :
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "Name"), h("th", null, "Prefix"), h("th", null, "Scopes"), h("th", null, "Active"), h("th", null, "Action"))),
        h("tbody", null, keys.map(k =>
          h("tr", { key: k.id },
            h("td", null, k.name), h("td", null, h("code", null, k.keyPrefix)),
            h("td", null, (k.scopes || []).join(", ") || "—"),
            h("td", null, k.isActive ? "✅" : "—"),
            h("td", null, k.isActive && h("button", { className: "secondary-button", onClick: () => handleRevoke(k.id) }, "Revoke"))
          )
        ))
      )
    )
  );
}
