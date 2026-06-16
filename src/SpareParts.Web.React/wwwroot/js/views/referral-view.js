import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { money, shortDate } from "../core/formatters.js";
import { Badge, PageHeader, StatusLine } from "../components/shared.js";

const BASE_SIGNUP_URL = "https://sparepartsapp.com/signup";

function CopyBox({ label, value }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = useCallback(() => {
    navigator.clipboard.writeText(value).then(() => {
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }).catch(() => {});
  }, [value]);

  return h("div", { style: { marginBottom: "16px" } },
    label && h("div", { style: { fontSize: "12px", color: "#888", marginBottom: "4px" } }, label),
    h("div", { style: { display: "flex", gap: "8px", alignItems: "center" } },
      h("div", {
        style: {
          flex: 1, padding: "10px 14px", background: "#f5f5f5",
          borderRadius: "6px", fontFamily: "monospace", fontSize: "16px",
          fontWeight: "bold", wordBreak: "break-all"
        }
      }, value),
      h("button", {
        type: "button", className: "secondary-button",
        onClick: handleCopy
      }, copied ? "Copied!" : "Copy")
    )
  );
}

export function ReferralView({ api }) {
  const [myCode, setMyCode] = useState(null);
  const [referrals, setReferrals] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [status, setStatus] = useState("");

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading referral info...");
    try {
      const codeData = await api.get("/api/referral/my-code");
      setMyCode(codeData || null);

      if (codeData && codeData.code) {
        const refs = await api.get("/api/referral/my-referrals");
        setReferrals(Array.isArray(refs) ? refs : []);
      }
      setStatus("");
    } catch (err) {
      setStatus(err.message || "Could not load referral data.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const handleGenerate = useCallback(async () => {
    setIsGenerating(true);
    setStatus("Generating your referral code...");
    try {
      const data = await api.post("/api/referral/generate", {});
      setMyCode(data || null);
      setStatus("Referral code generated!");
      await load();
    } catch (err) {
      setStatus(err.message || "Could not generate referral code.");
    } finally {
      setIsGenerating(false);
    }
  }, [api, load]);

  const referralLink = myCode && myCode.code
    ? `${BASE_SIGNUP_URL}?ref=${encodeURIComponent(myCode.code)}`
    : null;

  const whatsappLink = referralLink
    ? `https://wa.me/?text=${encodeURIComponent("Join SpareParts using my referral link: " + referralLink)}`
    : null;

  const totalReferrals = referrals.length;
  const totalCredits = referrals.reduce((sum, r) => sum + (r.creditAmount || 0), 0);
  const creditCurrency = referrals.length > 0 && referrals[0].creditCurrency
    ? referrals[0].creditCurrency : "USD";

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Referral Program",
      subtitle: "Share your referral code and earn credits when friends join SpareParts."
    }),
    h(StatusLine, { status }),

    isLoading
      ? h("p", null, "Loading...")
      : !myCode || !myCode.code
        ? h("div", { className: "data-card" },
          h("div", { className: "data-card-body", style: { textAlign: "center", padding: "32px" } },
            h("p", { style: { fontSize: "16px", marginBottom: "16px" } },
              "You don't have a referral code yet."
            ),
            h("button", {
              type: "button",
              className: "primary-button",
              onClick: handleGenerate,
              disabled: isGenerating
            }, isGenerating ? "Generating..." : "Generate My Referral Code")
          )
        )
        : h("div", null,
          h("div", { className: "data-card", style: { marginBottom: "16px" } },
            h("div", { className: "data-card-header" },
              h("strong", null, "Your Referral Code")
            ),
            h("div", { className: "data-card-body" },
              h(CopyBox, { label: "Referral Code", value: myCode.code }),
              referralLink && h(CopyBox, { label: "Referral Link", value: referralLink }),
              whatsappLink && h("a", {
                href: whatsappLink,
                target: "_blank",
                rel: "noopener noreferrer",
                className: "primary-button",
                style: { display: "inline-block", textDecoration: "none", marginTop: "4px" }
              }, "📱 Share via WhatsApp")
            )
          ),

          h("div", { className: "data-card", style: { marginBottom: "16px" } },
            h("div", { className: "data-card-header" },
              h("strong", null, "Your Stats")
            ),
            h("div", { className: "data-card-body" },
              h("div", { style: { display: "flex", gap: "32px", flexWrap: "wrap" } },
                h("div", null,
                  h("div", { style: { fontSize: "12px", color: "#888" } }, "Total Referrals"),
                  h("div", { style: { fontSize: "28px", fontWeight: "bold" } }, totalReferrals)
                ),
                h("div", null,
                  h("div", { style: { fontSize: "12px", color: "#888" } }, "Total Credits Earned"),
                  h("div", { style: { fontSize: "28px", fontWeight: "bold", color: "#1976d2" } },
                    money(totalCredits, creditCurrency)
                  )
                )
              )
            )
          ),

          referrals.length > 0 && h("div", { className: "data-card" },
            h("div", { className: "data-card-header" },
              h("strong", null, "Referred Users")
            ),
            h("div", { className: "data-card-body" },
              h("table", { style: { width: "100%", borderCollapse: "collapse" } },
                h("thead", null,
                  h("tr", null,
                    h("th", { style: { textAlign: "left", padding: "6px 8px", borderBottom: "1px solid #eee" } }, "Email"),
                    h("th", { style: { textAlign: "left", padding: "6px 8px", borderBottom: "1px solid #eee" } }, "Joined"),
                    h("th", { style: { textAlign: "right", padding: "6px 8px", borderBottom: "1px solid #eee" } }, "Credit"),
                    h("th", { style: { textAlign: "center", padding: "6px 8px", borderBottom: "1px solid #eee" } }, "Paid")
                  )
                ),
                h("tbody", null,
                  referrals.map((ref, idx) =>
                    h("tr", { key: ref.id || idx },
                      h("td", { style: { padding: "6px 8px" } }, ref.email || "—"),
                      h("td", { style: { padding: "6px 8px" } }, ref.joinedAt ? shortDate(ref.joinedAt) : "—"),
                      h("td", { style: { textAlign: "right", padding: "6px 8px" } },
                        ref.creditAmount != null ? money(ref.creditAmount, ref.creditCurrency || "USD") : "—"
                      ),
                      h("td", { style: { textAlign: "center", padding: "6px 8px" } },
                        h(Badge, { tone: ref.isPaid ? "positive" : "warning" }, ref.isPaid ? "Paid" : "Pending")
                      )
                    )
                  )
                )
              )
            )
          )
        )
  );
}
