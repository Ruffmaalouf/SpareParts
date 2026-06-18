import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const TABS = ["Browse Requests", "Post a Request", "My Offers"];
const STATUS_OPTIONS = ["", "Open", "Contacted", "Fulfilled", "Cancelled"];
const URGENCY_OPTIONS = ["High", "Normal", "Low"];
const CONDITION_OPTIONS = ["New", "Used", "Refurbished"];

const emptyPostForm = {
  buyerName: "",
  buyerPhone: "",
  partName: "",
  vehicleMake: "",
  vehicleModel: "",
  vehicleYear: "",
  budget: "",
  notes: "",
  locationCity: "",
  urgency: "Normal",
  expiresInDays: "7"
};

const emptyOfferForm = {
  sellerName: "",
  sellerPhone: "",
  offeredPrice: "",
  currency: "USD",
  condition: "New",
  notes: ""
};

function urgencyBadgeTone(urgency) {
  if (urgency === "High") return "danger";
  if (urgency === "Low") return "info";
  return "neutral";
}

function statusBadgeTone(status) {
  if (status === "Open") return "positive";
  if (status === "Contacted") return "warning";
  if (status === "Fulfilled") return "neutral";
  if (status === "Expired") return "muted";
  return "neutral";
}

function timeSince(isoDate) {
  if (!isoDate) return "";
  const ms = Date.now() - new Date(isoDate).getTime();
  const minutes = Math.floor(ms / 60000);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

function AdCard({ ad, onMakeOffer }) {
  const [showOfferForm, setShowOfferForm] = useState(false);
  const [offerForm, setOfferForm] = useState({ ...emptyOfferForm });
  const [offerStatus, setOfferStatus] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const setOfferField = useCallback((key, value) => {
    setOfferForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const submitOffer = useCallback(async (e) => {
    e.preventDefault();
    if (!offerForm.sellerName.trim() || !offerForm.offeredPrice) {
      setOfferStatus("Seller name and price are required.");
      return;
    }
    setIsSubmitting(true);
    setOfferStatus("Submitting offer...");
    try {
      await onMakeOffer(ad.id, {
        sellerName: offerForm.sellerName.trim(),
        sellerPhone: offerForm.sellerPhone.trim() || null,
        offeredPrice: Number(offerForm.offeredPrice),
        currency: offerForm.currency,
        condition: offerForm.condition,
        notes: offerForm.notes.trim() || null
      });
      setOfferStatus("Offer submitted!");
      setShowOfferForm(false);
      setOfferForm({ ...emptyOfferForm });
    } catch (err) {
      setOfferStatus(err.message || "Could not submit offer.");
    } finally {
      setIsSubmitting(false);
    }
  }, [ad.id, offerForm, onMakeOffer]);

  return h("div", { className: "data-card" },
    h("div", { className: "data-card-header" },
      h("strong", null, ad.partName),
      h("div", { className: "row-actions" },
        h(Badge, { tone: urgencyBadgeTone(ad.urgency) }, ad.urgency),
        h(Badge, { tone: statusBadgeTone(ad.statusLabel) }, ad.statusLabel)
      )
    ),
    h("div", { className: "data-card-body" },
      (ad.vehicleMake || ad.vehicleModel || ad.vehicleYear) && h("div", null,
        h("span", null, "Vehicle: "),
        h("strong", null, [ad.vehicleYear, ad.vehicleMake, ad.vehicleModel].filter(Boolean).join(" "))
      ),
      ad.budget && h("div", null,
        h("span", null, "Budget: "),
        h("strong", null, money(ad.budget, "USD"))
      ),
      ad.locationCity && h("div", null,
        h("span", null, "Location: "),
        h("strong", null, ad.locationCity)
      ),
      h("div", null,
        h("span", null, "Offers: "),
        h("strong", null, ad.offersCount || 0)
      ),
      h("div", null,
        h("small", null, timeSince(ad.createdAt))
      ),
      ad.notes && h("p", null, ad.notes)
    ),
    ad.statusLabel === "Open" && h("div", null,
      h("button", {
        type: "button",
        className: "secondary-button",
        onClick: () => setShowOfferForm(v => !v)
      }, showOfferForm ? "Cancel" : "Make Offer"),
      showOfferForm && h("form", { className: "editor-grid", onSubmit: submitOffer, style: { marginTop: "8px" } },
        h("label", null,
          h("span", null, "Your Name *"),
          h("input", {
            value: offerForm.sellerName,
            onChange: e => setOfferField("sellerName", e.target.value),
            required: true
          })
        ),
        h("label", null,
          h("span", null, "Phone"),
          h("input", {
            value: offerForm.sellerPhone,
            onChange: e => setOfferField("sellerPhone", e.target.value)
          })
        ),
        h("label", null,
          h("span", null, "Offered Price *"),
          h("input", {
            type: "number",
            value: offerForm.offeredPrice,
            onChange: e => setOfferField("offeredPrice", e.target.value),
            required: true
          })
        ),
        h("label", null,
          h("span", null, "Currency"),
          h("select", { value: offerForm.currency, onChange: e => setOfferField("currency", e.target.value) },
            h("option", { value: "USD" }, "USD"),
            h("option", { value: "LBP" }, "LBP")
          )
        ),
        h("label", null,
          h("span", null, "Condition"),
          h("select", { value: offerForm.condition, onChange: e => setOfferField("condition", e.target.value) },
            CONDITION_OPTIONS.map(c => h("option", { key: c, value: c }, c))
          )
        ),
        h("label", null,
          h("span", null, "Notes"),
          h("textarea", {
            value: offerForm.notes,
            onChange: e => setOfferField("notes", e.target.value),
            rows: 2
          })
        ),
        offerStatus && h("p", { className: "status-line" }, offerStatus),
        h("button", { type: "submit", className: "primary-button", disabled: isSubmitting }, "Submit Offer")
      )
    )
  );
}

export function NeedBoardView({ api }) {
  const [activeTab, setActiveTab] = useState("Browse Requests");
  const [ads, setAds] = useState([]);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState("Open");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [postForm, setPostForm] = useState({ ...emptyPostForm });

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading board...");
    try {
      const params = new URLSearchParams();
      if (statusFilter) params.set("status", statusFilter);
      if (search.trim()) params.set("search", search.trim());
      const data = await api.get(`/api/needboard?${params.toString()}`);
      setAds(Array.isArray(data) ? data : []);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load board.");
    } finally {
      setIsLoading(false);
    }
  }, [api, statusFilter, search]);

  useEffect(() => {
    if (activeTab === "Browse Requests") {
      load();
    }
  }, [activeTab, load]);

  const setPostField = useCallback((key, value) => {
    setPostForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handlePost = useCallback(async (e) => {
    e.preventDefault();
    if (!postForm.partName.trim()) {
      setStatus("Part name is required.");
      return;
    }
    setIsSaving(true);
    setStatus("Posting request...");
    try {
      await api.post("/api/needboard", {
        buyerName: postForm.buyerName.trim() || null,
        buyerPhone: postForm.buyerPhone.trim() || null,
        partName: postForm.partName.trim(),
        vehicleMake: postForm.vehicleMake.trim() || null,
        vehicleModel: postForm.vehicleModel.trim() || null,
        vehicleYear: postForm.vehicleYear ? Number(postForm.vehicleYear) : null,
        budget: postForm.budget ? Number(postForm.budget) : null,
        notes: postForm.notes.trim() || null,
        locationCity: postForm.locationCity.trim() || null,
        urgency: postForm.urgency,
        expiresInDays: postForm.expiresInDays ? Number(postForm.expiresInDays) : null
      });
      setPostForm({ ...emptyPostForm });
      setStatus("Request posted successfully.");
      setActiveTab("Browse Requests");
    } catch (e) {
      setStatus(e.message || "Could not post request.");
    } finally {
      setIsSaving(false);
    }
  }, [api, postForm]);

  const handleMakeOffer = useCallback(async (adId, offerPayload) => {
    await api.post(`/api/needboard/${adId}/offers`, offerPayload);
    await load();
  }, [api, load]);

  const handleCancel = useCallback(async (adId) => {
    if (!window.confirm("Cancel this request?")) return;
    try {
      await api.put(`/api/needboard/${adId}/cancel`);
      setStatus("Request cancelled.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not cancel request.");
    }
  }, [api, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "NeedBoard", subtitle: "Buyers post what they need. Sellers respond with offers." }),
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

    activeTab === "Browse Requests" && h("section", null,
      h("div", { className: "filters-row" },
        h("input", {
          value: search,
          onChange: e => setSearch(e.target.value),
          placeholder: "Search by part name or car make/model",
          style: { flex: 1 }
        }),
        h("button", { type: "button", className: "secondary-button", onClick: load }, "Search"),
        h("select", { value: statusFilter, onChange: e => setStatusFilter(e.target.value) },
          STATUS_OPTIONS.map(s => h("option", { key: s, value: s }, s || "All Statuses"))
        )
      ),
      isLoading
        ? h("p", null, "Loading...")
        : ads.length === 0
          ? h("p", { className: "empty-state" }, "No requests found.")
          : h("div", { className: "data-card-grid" },
            ads.map(ad =>
              h(AdCard, {
                key: ad.id,
                ad,
                onMakeOffer: handleMakeOffer
              })
            )
          )
    ),

    activeTab === "Post a Request" && h("section", null,
      h("form", { className: "admin-panel", onSubmit: handlePost },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "Post a Part Request"),
          h("button", { type: "submit", className: "primary-button", disabled: isSaving }, "Post Request")
        ),
        h("div", { className: "editor-grid" },
          h("label", null,
            h("span", null, "Your Name"),
            h("input", { value: postForm.buyerName, onChange: e => setPostField("buyerName", e.target.value), placeholder: "Your name" })
          ),
          h("label", null,
            h("span", null, "Phone"),
            h("input", { value: postForm.buyerPhone, onChange: e => setPostField("buyerPhone", e.target.value), placeholder: "Contact phone" })
          ),
          h("label", null,
            h("span", null, "Part Name *"),
            h("input", { value: postForm.partName, onChange: e => setPostField("partName", e.target.value), required: true, placeholder: "e.g. Front left CV axle" })
          ),
          h("label", null,
            h("span", null, "Vehicle Make"),
            h("input", { value: postForm.vehicleMake, onChange: e => setPostField("vehicleMake", e.target.value), placeholder: "e.g. Toyota" })
          ),
          h("label", null,
            h("span", null, "Vehicle Model"),
            h("input", { value: postForm.vehicleModel, onChange: e => setPostField("vehicleModel", e.target.value), placeholder: "e.g. Camry" })
          ),
          h("label", null,
            h("span", null, "Vehicle Year"),
            h("input", { type: "number", value: postForm.vehicleYear, onChange: e => setPostField("vehicleYear", e.target.value), placeholder: "e.g. 2018" })
          ),
          h("label", null,
            h("span", null, "Budget (USD)"),
            h("input", { type: "number", value: postForm.budget, onChange: e => setPostField("budget", e.target.value), placeholder: "Max budget" })
          ),
          h("label", null,
            h("span", null, "Location / City"),
            h("input", { value: postForm.locationCity, onChange: e => setPostField("locationCity", e.target.value), placeholder: "e.g. Beirut" })
          ),
          h("label", null,
            h("span", null, "Urgency"),
            h("select", { value: postForm.urgency, onChange: e => setPostField("urgency", e.target.value) },
              URGENCY_OPTIONS.map(u => h("option", { key: u, value: u }, u))
            )
          ),
          h("label", null,
            h("span", null, "Expires In (Days)"),
            h("input", { type: "number", value: postForm.expiresInDays, onChange: e => setPostField("expiresInDays", e.target.value), placeholder: "7" })
          ),
          h("label", null,
            h("span", null, "Notes"),
            h("textarea", { value: postForm.notes, onChange: e => setPostField("notes", e.target.value), rows: 3, placeholder: "Any additional details..." })
          )
        )
      )
    ),

    activeTab === "My Offers" && h("section", null,
      h("p", { className: "empty-state" }, "Offer tracking coming soon. Use the Browse Requests tab to submit offers on open requests.")
    )
  );
}
