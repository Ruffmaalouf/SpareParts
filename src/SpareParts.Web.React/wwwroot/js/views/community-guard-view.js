import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function CommunityGuardView({ api }) {
  const [reports, setReports] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [targetType, setTargetType] = useState("1");
  const [targetId, setTargetId] = useState("");
  const [category, setCategory] = useState("1");
  const [details, setDetails] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await api.get("/api/community-guard");
      setReports(data || []);
    } catch (err) { setStatus(err.message || "Failed to load."); }
    finally { setIsLoading(false); }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!targetId) { setStatus("Enter a target ID."); return; }
    try {
      await api.post("/api/community-guard", { targetType: Number(targetType), targetId: Number(targetId), category: Number(category), details: details || null });
      setStatus("Report submitted."); setTargetId(""); setDetails("");
      load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, targetType, targetId, category, details, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "CommunityGuard", subtitle: "Report suspicious listings, sellers, or parts." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleSubmit, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Submit Report"),
        h("button", { type: "submit", className: "primary-button" }, "Submit")
      ),
      h("div", { className: "editor-grid" },
        h("label", null,
          h("span", null, "Target Type"),
          h("select", { value: targetType, onChange: e => setTargetType(e.target.value) },
            h("option", { value: "1" }, "Listing"), h("option", { value: "2" }, "Seller"), h("option", { value: "3" }, "Part")
          )
        ),
        h("label", null,
          h("span", null, "Target ID *"),
          h("input", { type: "number", value: targetId, onChange: e => setTargetId(e.target.value), required: true })
        ),
        h("label", null,
          h("span", null, "Category"),
          h("select", { value: category, onChange: e => setCategory(e.target.value) },
            h("option", { value: "1" }, "Wrong Part Sent"),
            h("option", { value: "2" }, "Fake Photos"),
            h("option", { value: "3" }, "Worse Condition"),
            h("option", { value: "4" }, "Seller Disappeared"),
            h("option", { value: "5" }, "Duplicate Listing"),
            h("option", { value: "6" }, "Scam Suspicion")
          )
        ),
        h("label", { style: { gridColumn: "1 / -1" } },
          h("span", null, "Details"),
          h("textarea", { value: details, onChange: e => setDetails(e.target.value), rows: 3, placeholder: "Describe the issue..." })
        )
      )
    ),
    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, `Reports (${reports.length})`)),
      isLoading ? h("p", { style: { padding: "12px" } }, "Loading...") :
      reports.length === 0 ? h("p", { style: { padding: "12px", color: "#999" } }, "No reports yet.") :
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null,
          h("th", null, "ID"), h("th", null, "Target"), h("th", null, "Category"),
          h("th", null, "Status"), h("th", null, "Auto-Flagged")
        )),
        h("tbody", null, reports.map(r =>
          h("tr", { key: r.id },
            h("td", null, r.id),
            h("td", null, `${r.targetType === 1 ? "Listing" : r.targetType === 2 ? "Seller" : "Part"} #${r.targetId}`),
            h("td", null, r.categoryLabel),
            h("td", null, r.statusLabel),
            h("td", null, r.autoFlagged ? "🚩 Yes" : "No")
          )
        ))
      )
    )
  );
}
