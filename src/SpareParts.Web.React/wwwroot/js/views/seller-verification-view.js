import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const emptyForm = {
  businessName: "",
  registrationNumber: "",
  physicalAddress: "",
  idDocumentUrl: "",
  businessDocumentUrl: ""
};

function verificationStatusTone(verificationStatus) {
  if (verificationStatus === "Verified") return "positive";
  if (verificationStatus === "PendingReview") return "warning";
  if (verificationStatus === "Rejected") return "danger";
  if (verificationStatus === "Suspended") return "danger";
  return "neutral";
}

function verificationStatusLabel(verificationStatus) {
  if (verificationStatus === "Verified") return "Verified Seller";
  if (verificationStatus === "PendingReview") return "Under Review";
  if (verificationStatus === "Rejected") return "Rejected";
  if (verificationStatus === "Suspended") return "Suspended";
  return "Not Verified";
}

function verificationStatusMessage(verificationStatus) {
  if (verificationStatus === "Verified") return "Your shop is verified. Buyers can see your verified status.";
  if (verificationStatus === "PendingReview") return "Your documents are under review. We will notify you once the review is complete.";
  if (verificationStatus === "Rejected") return "Your verification was rejected. Please review the admin notes below and resubmit.";
  if (verificationStatus === "Suspended") return "Your seller account has been suspended. Please contact support.";
  return "Get verified to build buyer trust and unlock more features.";
}

function showForm(verificationStatus) {
  return !verificationStatus || verificationStatus === "Unverified" || verificationStatus === "Rejected";
}

export function SellerVerificationView({ api }) {
  const [verification, setVerification] = useState(null);
  const [form, setForm] = useState({ ...emptyForm });
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading verification status...");
    try {
      const data = await api.get("/api/seller-verification");
      setVerification(data);
      if (data) {
        setForm({
          businessName: data.businessName || "",
          registrationNumber: data.registrationNumber || "",
          physicalAddress: data.physicalAddress || "",
          idDocumentUrl: data.idDocumentUrl || "",
          businessDocumentUrl: data.businessDocumentUrl || ""
        });
      }
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load verification status.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const setField = useCallback((key, value) => {
    setForm(prev => ({ ...prev, [key]: value }));
  }, []);

  const handleSubmit = useCallback(async (e) => {
    e.preventDefault();
    if (!form.businessName.trim()) {
      setStatus("Business name is required.");
      return;
    }
    setIsSaving(true);
    setStatus("Submitting verification request...");
    try {
      await api.post("/api/seller-verification/submit", {
        businessName: form.businessName.trim(),
        registrationNumber: form.registrationNumber.trim() || null,
        physicalAddress: form.physicalAddress.trim() || null,
        idDocumentUrl: form.idDocumentUrl.trim() || null,
        businessDocumentUrl: form.businessDocumentUrl.trim() || null
      });
      setStatus("Verification request submitted. We will review your documents shortly.");
      await load();
    } catch (e) {
      setStatus(e.message || "Could not submit verification.");
    } finally {
      setIsSaving(false);
    }
  }, [api, form, load]);

  const currentStatus = verification ? (verification.verificationStatus || "Unverified") : null;

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Seller Verification",
      subtitle: "Verify your shop to build trust with buyers.",
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: load,
        disabled: isLoading
      }, "Refresh")
    }),
    h(StatusLine, { status }),

    isLoading
      ? h("p", null, "Loading...")
      : h("div", { className: "workspace" },

        h("section", { className: "module-summary" },
          h("div", { style: { display: "flex", alignItems: "center", gap: "16px", flexWrap: "wrap" } },
            h(Badge, { tone: verificationStatusTone(currentStatus) },
              verificationStatusLabel(currentStatus)
            ),
            h("p", null, verificationStatusMessage(currentStatus))
          )
        ),

        currentStatus === "PendingReview" && verification && verification.submittedAt && h("section", { className: "admin-panel" },
          h("p", null,
            h("span", null, "Submitted on: "),
            h("strong", null, shortDate(verification.submittedAt))
          )
        ),

        currentStatus === "Verified" && verification && h("section", { className: "admin-panel" },
          h("h3", null, "Verification Details"),
          h("div", { className: "detail-grid" },
            verification.verifiedAt && h("div", { className: "detail-tile" },
              h("span", null, "Verified On"),
              h("strong", null, shortDate(verification.verifiedAt))
            ),
            verification.businessName && h("div", { className: "detail-tile" },
              h("span", null, "Business Name"),
              h("strong", null, verification.businessName)
            ),
            verification.registrationNumber && h("div", { className: "detail-tile" },
              h("span", null, "Registration #"),
              h("strong", null, verification.registrationNumber)
            ),
            verification.physicalAddress && h("div", { className: "detail-tile" },
              h("span", null, "Address"),
              h("strong", null, verification.physicalAddress)
            )
          )
        ),

        currentStatus === "Rejected" && verification && verification.adminNotes && h("section", { className: "admin-panel" },
          h("h3", null, "Rejection Notes"),
          h("p", { style: { color: "var(--danger)" } }, verification.adminNotes)
        ),

        currentStatus === "Suspended" && h("section", { className: "admin-panel" },
          h("p", null, "Please contact support to resolve your account suspension.")
        ),

        showForm(currentStatus) && h("section", { className: "admin-panel" },
          h("form", { onSubmit: handleSubmit },
            h("div", { className: "admin-panel-header" },
              h("h3", null, currentStatus === "Rejected" ? "Resubmit Verification" : "Submit for Verification"),
              h("button", {
                type: "submit",
                className: "primary-button",
                disabled: isSaving
              }, isSaving ? "Submitting..." : "Submit")
            ),
            h("p", { className: "status-line" },
              "In a future update, you will be able to upload files directly. For now, paste a link to your document (Google Drive, Dropbox, etc.)."
            ),
            h("div", { className: "editor-grid" },
              h("label", null,
                h("span", null, "Business Name *"),
                h("input", {
                  value: form.businessName,
                  onChange: e => setField("businessName", e.target.value),
                  required: true,
                  placeholder: "Your registered business name"
                })
              ),
              h("label", null,
                h("span", null, "Registration Number"),
                h("input", {
                  value: form.registrationNumber,
                  onChange: e => setField("registrationNumber", e.target.value),
                  placeholder: "Commercial registration number"
                })
              ),
              h("label", null,
                h("span", null, "Physical Address"),
                h("input", {
                  value: form.physicalAddress,
                  onChange: e => setField("physicalAddress", e.target.value),
                  placeholder: "Shop or business address"
                })
              ),
              h("label", null,
                h("span", null, "ID Document URL"),
                h("input", {
                  type: "url",
                  value: form.idDocumentUrl,
                  onChange: e => setField("idDocumentUrl", e.target.value),
                  placeholder: "https://drive.google.com/..."
                })
              ),
              h("label", null,
                h("span", null, "Business Document URL"),
                h("input", {
                  type: "url",
                  value: form.businessDocumentUrl,
                  onChange: e => setField("businessDocumentUrl", e.target.value),
                  placeholder: "https://drive.google.com/..."
                })
              )
            )
          )
        )
      )
  );
}
