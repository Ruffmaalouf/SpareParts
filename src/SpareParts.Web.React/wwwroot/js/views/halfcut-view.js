import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const TABS = ["Listings", "Add Listing"];

const emptyListingForm = {
  make: "",
  model: "",
  year: "",
  vin: "",
  color: "",
  originCountry: "",
  mileage: "",
  askingPrice: "",
  currency: "USD",
  notes: "",
  photoUrls: ""
};

const emptyClaimForm = {
  claimantName: "",
  claimantPhone: "",
  partName: "",
  depositAmount: "",
  notes: ""
};

function statusTone(status) {
  if (status === "Available") return "positive";
  if (status === "PartlyClaimed") return "warning";
  if (status === "FullyClaimed") return "info";
  if (status === "Sold") return "neutral";
  return "neutral";
}

function HalfCutCard({ listing, api, onAddClaim, onConfirmClaim, isSaving }) {
  const [expanded, setExpanded] = useState(false);
  const [claims, setClaims] = useState(null);
  const [showClaimForm, setShowClaimForm] = useState(false);
  const [claimForm, setClaimForm] = useState({ ...emptyClaimForm });
  const [claimStatus, setClaimStatus] = useState("");

  const loadClaims = useCallback(async () => {
    try {
      const data = await api.get(`/api/half-cuts/${listing.id}/claims`);
      setClaims(Array.isArray(data) ? data : []);
    } catch (e) {
      setClaimStatus(e.message || "Could not load claims.");
    }
  }, [api, listing.id]);

  const toggleExpanded = useCallback(() => {
    setExpanded(v => {
      const next = !v;
      if (next) loadClaims();
      return next;
    });
  }, [loadClaims]);

  const setClaimField = useCallback((key, value) => {
    setClaimForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleClaim = useCallback(async (e) => {
    e.preventDefault();
    if (!claimForm.claimantName.trim() || !claimForm.partName.trim()) {
      setClaimStatus("Name and part name are required.");
      return;
    }
    try {
      await onAddClaim(listing.id, {
        claimantName: claimForm.claimantName.trim(),
        claimantPhone: claimForm.claimantPhone.trim() || null,
        partName: claimForm.partName.trim(),
        depositAmount: claimForm.depositAmount ? Number(claimForm.depositAmount) : null,
        notes: claimForm.notes.trim() || null
      });
      setClaimForm({ ...emptyClaimForm });
      setShowClaimForm(false);
      setClaimStatus("");
      if (expanded) await loadClaims();
    } catch (e) {
      setClaimStatus(e.message || "Could not add claim.");
    }
  }, [listing.id, claimForm, onAddClaim, expanded, loadClaims]);

  const handleConfirmClaim = useCallback(async (claimId) => {
    await onConfirmClaim(listing.id, claimId);
    await loadClaims();
  }, [listing.id, onConfirmClaim, loadClaims]);

  return h("div", { className: "data-card" },
    h("div", { className: "data-card-header" },
      h("strong", null, `${listing.vehicleYear || ""} ${listing.vehicleMake || ""} ${listing.vehicleModel || ""}`.trim() || "Half-Cut Vehicle"),
      h("div", { className: "row-actions" },
        h(Badge, { tone: statusTone(listing.statusLabel) }, listing.statusLabel || "Available")
      )
    ),
    h("div", { className: "data-card-body" },
      listing.vinNumber && h("div", null, h("span", null, "VIN: "), h("strong", null, listing.vinNumber)),
      listing.color && h("div", null, h("span", null, "Color: "), h("strong", null, listing.color)),
      listing.originCountry && h("div", null, h("span", null, "Origin: "), h("strong", null, listing.originCountry)),
      listing.mileage && h("div", null, h("span", null, "Mileage: "), h("strong", null, Number(listing.mileage).toLocaleString())),
      listing.askingPrice && h("div", null, h("span", null, "Asking: "), h("strong", null, money(listing.askingPrice, listing.currency || "USD"))),
      listing.notes && h("p", null, listing.notes),
      h("div", null, h("small", null, `${listing.claimsCount || 0} claim(s)`))
    ),
    h("div", { className: "row-actions" },
      h("button", { type: "button", className: "secondary-button", onClick: toggleExpanded },
        expanded ? "Hide Claims" : "View Claims"),
      h("button", { type: "button", className: "primary-button", onClick: () => setShowClaimForm(v => !v) },
        showClaimForm ? "Cancel" : "Add Claim")
    ),
    showClaimForm && h("form", { className: "editor-grid", onSubmit: handleClaim, style: { marginTop: "8px" } },
      h("label", null,
        h("span", null, "Your Name *"),
        h("input", { value: claimForm.claimantName, onChange: e => setClaimField("claimantName", e.target.value), required: true })
      ),
      h("label", null,
        h("span", null, "Phone"),
        h("input", { value: claimForm.claimantPhone, onChange: e => setClaimField("claimantPhone", e.target.value) })
      ),
      h("label", null,
        h("span", null, "Part You Want *"),
        h("input", { value: claimForm.partName, onChange: e => setClaimField("partName", e.target.value), required: true, placeholder: "e.g. Engine, Door, Dashboard" })
      ),
      h("label", null,
        h("span", null, "Deposit Amount (USD)"),
        h("input", { type: "number", value: claimForm.depositAmount, onChange: e => setClaimField("depositAmount", e.target.value) })
      ),
      h("label", null,
        h("span", null, "Notes"),
        h("textarea", { value: claimForm.notes, onChange: e => setClaimField("notes", e.target.value), rows: 2 })
      ),
      claimStatus && h("p", { className: "status-line" }, claimStatus),
      h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Submit Claim")
    ),
    expanded && h("div", { style: { marginTop: "8px" } },
      claims === null
        ? h("p", null, "Loading claims...")
        : claims.length === 0
          ? h("p", { className: "empty-state" }, "No claims yet.")
          : claims.map((c) =>
            h("div", { key: c.id, style: { display: "flex", alignItems: "center", gap: "8px", marginBottom: "6px", flexWrap: "wrap" } },
              h("strong", null, c.claimantName),
              h("span", null, c.partName),
              c.depositAmount && h("span", null, money(c.depositAmount, "USD")),
              h(Badge, { tone: c.isConfirmed ? "positive" : "neutral" }, c.isConfirmed ? "Confirmed" : "Pending"),
              !c.isConfirmed && h("button", {
                type: "button",
                className: "secondary-button",
                onClick: () => handleConfirmClaim(c.id),
                disabled: isSaving
              }, "Confirm")
            )
          )
    )
  );
}

export function HalfCutView({ api }) {
  const [activeTab, setActiveTab] = useState("Listings");
  const [listings, setListings] = useState([]);
  const [form, setForm] = useState({ ...emptyListingForm });
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading half-cut listings...");
    try {
      const data = await api.get("/api/half-cuts");
      setListings(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load listings.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => {
    if (activeTab === "Listings") load();
  }, [activeTab, load]);

  const setField = useCallback((key, value) => {
    setForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!form.make.trim() || !form.model.trim() || !form.year) {
      setStatus("Make, model, and year are required.");
      return;
    }
    setIsSaving(true);
    setStatus("Creating listing...");
    try {
      const photoUrls = form.photoUrls.split("\n").map(u => u.trim()).filter(Boolean).join("\n");
      await api.post("/api/half-cuts", {
        vehicleMake: form.make.trim(),
        vehicleModel: form.model.trim(),
        vehicleYear: form.year ? Number(form.year) : null,
        vinNumber: form.vin.trim() || null,
        color: form.color.trim() || null,
        originCountry: form.originCountry.trim() || null,
        mileage: form.mileage ? Number(form.mileage) : null,
        askingPrice: form.askingPrice ? Number(form.askingPrice) : null,
        currency: form.currency,
        notes: form.notes.trim() || null,
        photoUrls: photoUrls || null
      });
      setForm({ ...emptyListingForm });
      setStatus("Listing created.");
      setActiveTab("Listings");
    } catch (e) {
      setStatus(e.message || "Could not create listing.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form]);

  const handleAddClaim = useCallback(async (listingId, claimPayload) => {
    await api.post(`/api/half-cuts/${listingId}/claims`, claimPayload);
    await load();
  }, [api, load]);

  const handleConfirmClaim = useCallback(async (listingId, claimId) => {
    setIsSaving(true);
    try {
      await api.put(`/api/half-cuts/${listingId}/claims/${claimId}/confirm`);
      await load();
    } catch (e) {
      setStatus(e.message || "Could not confirm claim.");
    } finally {
      setIsSaving(false);
    }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Half-Cut Showcase",
      subtitle: "List salvage vehicles and manage part claims.",
      action: h("button", { type: "button", className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h(StatusLine, { status }),

    h("div", { className: "filters-row" },
      TABS.map(tab =>
        h("button", {
          key: tab,
          type: "button",
          className: activeTab === tab ? "chip active" : "chip",
          onClick: () => setActiveTab(tab)
        }, tab)
      )
    ),

    activeTab === "Listings" && h("section", null,
      isLoading
        ? h("p", null, "Loading...")
        : listings.length === 0
          ? h("p", { className: "empty-state" }, "No half-cut listings. Add the first one.")
          : h("div", { className: "data-card-grid" },
            listings.map(l =>
              h(HalfCutCard, {
                key: l.id,
                listing: l,
                api,
                onAddClaim: handleAddClaim,
                onConfirmClaim: handleConfirmClaim,
                isSaving
              })
            )
          )
    ),

    activeTab === "Add Listing" && h("section", null,
      h("form", { className: "admin-panel", onSubmit: handleSubmit },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "Add Half-Cut Listing"),
          h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Publish Listing")
        ),
        h("div", { className: "editor-grid" },
          h("label", null, h("span", null, "Make *"), h("input", { value: form.make, onChange: e => setField("make", e.target.value), required: true, placeholder: "e.g. Toyota" })),
          h("label", null, h("span", null, "Model *"), h("input", { value: form.model, onChange: e => setField("model", e.target.value), required: true, placeholder: "e.g. Camry" })),
          h("label", null, h("span", null, "Year *"), h("input", { type: "number", value: form.year, onChange: e => setField("year", e.target.value), required: true, placeholder: "e.g. 2008" })),
          h("label", null, h("span", null, "VIN"), h("input", { value: form.vin, onChange: e => setField("vin", e.target.value), placeholder: "Optional" })),
          h("label", null, h("span", null, "Color"), h("input", { value: form.color, onChange: e => setField("color", e.target.value), placeholder: "e.g. Black" })),
          h("label", null, h("span", null, "Origin Country"), h("input", { value: form.originCountry, onChange: e => setField("originCountry", e.target.value), placeholder: "e.g. Japan" })),
          h("label", null, h("span", null, "Mileage (km)"), h("input", { type: "number", value: form.mileage, onChange: e => setField("mileage", e.target.value), placeholder: "e.g. 85000" })),
          h("label", null, h("span", null, "Asking Price"), h("input", { type: "number", value: form.askingPrice, onChange: e => setField("askingPrice", e.target.value), placeholder: "0.00" })),
          h("label", null,
            h("span", null, "Currency"),
            h("select", { value: form.currency, onChange: e => setField("currency", e.target.value) },
              h("option", { value: "USD" }, "USD"),
              h("option", { value: "LBP" }, "LBP")
            )
          ),
          h("label", null, h("span", null, "Notes"), h("textarea", { value: form.notes, onChange: e => setField("notes", e.target.value), rows: 3, placeholder: "Condition, available parts, etc." })),
          h("label", null,
            h("span", null, "Photo URLs (one per line)"),
            h("textarea", { value: form.photoUrls, onChange: e => setField("photoUrls", e.target.value), rows: 4, placeholder: "https://...\nhttps://..." })
          )
        )
      )
    )
  );
}
