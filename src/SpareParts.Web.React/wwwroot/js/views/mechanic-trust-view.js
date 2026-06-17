import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const badgeColors = { None: "#999", Emerging: "#1976d2", Verified: "#388e3c", Elite: "#ff9800" };

export function MechanicTrustView({ api }) {
  const [profiles, setProfiles] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");
  const [userId, setUserId] = useState("");
  const [specialtyBrands, setSpecialtyBrands] = useState("");
  const [specialtyTypes, setSpecialtyTypes] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    try { setProfiles((await api.get("/api/mechanic-trust")) || []); }
    catch (err) { setStatus(err.message || "Failed."); }
    finally { setIsLoading(false); }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const handleCreate = useCallback(async (e) => {
    e.preventDefault();
    if (!userId) { setStatus("User ID required."); return; }
    try {
      await api.post("/api/mechanic-trust", { userId: Number(userId), specialtyBrands: specialtyBrands || null, specialtyTypes: specialtyTypes || null });
      setStatus("Profile saved."); setUserId(""); setSpecialtyBrands(""); setSpecialtyTypes("");
      load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, userId, specialtyBrands, specialtyTypes, load]);

  const handleVerify = useCallback(async (uid) => {
    try { await api.post(`/api/mechanic-trust/user/${uid}/verify`); setStatus("Verified."); load(); }
    catch (err) { setStatus(err.message || "Failed."); }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "MechanicTrust Network", subtitle: "Badges and verification for mechanics and buyers." }),
    h(StatusLine, { status }),
    h("form", { className: "admin-panel", onSubmit: handleCreate, style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" }, h("h3", null, "Create / Update Profile"), h("button", { type: "submit", className: "primary-button" }, "Save")),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "User ID *"), h("input", { type: "number", value: userId, onChange: e => setUserId(e.target.value), required: true })),
        h("label", null, h("span", null, "Specialty Brands"), h("input", { value: specialtyBrands, onChange: e => setSpecialtyBrands(e.target.value), placeholder: "Toyota, Honda" })),
        h("label", null, h("span", null, "Specialty Types"), h("input", { value: specialtyTypes, onChange: e => setSpecialtyTypes(e.target.value), placeholder: "Engine, Transmission" }))
      )
    ),
    h("div", { className: "admin-panel" },
      h("div", { className: "admin-panel-header" }, h("h3", null, `Profiles (${profiles.length})`)),
      isLoading ? h("p", { style: { padding: "12px" } }, "Loading...") :
      h("table", { className: "data-table" },
        h("thead", null, h("tr", null, h("th", null, "User"), h("th", null, "Badge"), h("th", null, "Purchases"), h("th", null, "Repairs"), h("th", null, "Verified"), h("th", null, "Action"))),
        h("tbody", null, profiles.map(p =>
          h("tr", { key: p.id },
            h("td", null, p.userName || `#${p.userId}`),
            h("td", null, h("span", { style: { color: badgeColors[p.badgeLevelLabel] || "#333", fontWeight: "bold" } }, p.badgeLevelLabel)),
            h("td", null, p.purchaseCount), h("td", null, p.repairOrderCount),
            h("td", null, p.isVerified ? "✅" : "—"),
            h("td", null, !p.isVerified && h("button", { className: "secondary-button", onClick: () => handleVerify(p.userId) }, "Verify"))
          )
        ))
      )
    )
  );
}
