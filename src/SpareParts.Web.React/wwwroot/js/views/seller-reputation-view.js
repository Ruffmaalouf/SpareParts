import { h, useCallback, useEffect, useState } from "../core/react-runtime.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

function badgeTone(badgeName) {
  if (badgeName === "Verified") return "info";
  if (badgeName === "Top Seller") return "warning";
  if (badgeName === "Fast Responder") return "positive";
  return "neutral";
}

function ScoreCircle({ score }) {
  const clamped = Math.max(0, Math.min(100, score || 0));
  const tone = clamped >= 80 ? "positive" : clamped >= 50 ? "warning" : "danger";
  return h("div", { className: `score-circle score-circle-${tone}` },
    h("span", { className: "score-value" }, clamped),
    h("small", null, "/ 100")
  );
}

function MetricBar({ label, value, tone }) {
  const pct = Math.max(0, Math.min(100, Math.round((value || 0) * 100)));
  const barTone = tone || (pct >= 80 ? "positive" : pct >= 50 ? "warning" : "danger");
  return h("div", { className: "metric-bar-row" },
    h("div", { className: "metric-bar-label" },
      h("span", null, label),
      h("strong", null, `${pct}%`)
    ),
    h("div", { className: "metric-bar-track" },
      h("div", { className: `metric-bar-fill metric-bar-${barTone}`, style: { width: `${pct}%` } })
    )
  );
}

export function SellerReputationView({ api }) {
  const [reputation, setReputation] = useState(null);
  const [leaderboard, setLeaderboard] = useState([]);
  const [showLeaderboard, setShowLeaderboard] = useState(false);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isLoadingLeaderboard, setIsLoadingLeaderboard] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading reputation...");
    try {
      const data = await api.get("/api/seller-reputation");
      setReputation(data);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load reputation.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const loadLeaderboard = useCallback(async () => {
    if (leaderboard.length > 0) {
      setShowLeaderboard(v => !v);
      return;
    }
    setIsLoadingLeaderboard(true);
    setStatus("Loading leaderboard...");
    try {
      const data = await api.get("/api/seller-reputation/all");
      setLeaderboard(Array.isArray(data) ? data : []);
      setShowLeaderboard(true);
      setStatus("");
    } catch (e) {
      setStatus(e.message || "Could not load leaderboard.");
    } finally {
      setIsLoadingLeaderboard(false);
    }
  }, [api, leaderboard.length]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Shop Reputation",
      subtitle: "Your reputation score based on fulfillment, disputes, and returns.",
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
      : !reputation
        ? h("p", { className: "empty-state" }, "No reputation data found.")
        : h("div", { className: "workspace" },

          h("section", { className: "module-summary" },
            h("div", { style: { display: "flex", alignItems: "center", gap: "24px", flexWrap: "wrap" } },
              h(ScoreCircle, { score: reputation.reputationScore }),
              h("div", null,
                h("h3", null, reputation.shopName || "Your Shop"),
                h("p", null, `Reputation Score: ${reputation.reputationScore || 0} / 100`)
              )
            )
          ),

          reputation.badges && reputation.badges.length > 0 && h("section", { className: "admin-panel" },
            h("h3", null, "Badges Earned"),
            h("div", { className: "row-actions", style: { flexWrap: "wrap" } },
              reputation.badges.map((badge, i) =>
                h("span", {
                  key: i,
                  className: `badge badge-${badgeTone(badge)}`
                }, badge)
              )
            )
          ),

          h("section", { className: "admin-panel" },
            h("h3", null, "Performance Metrics"),
            h("div", { className: "module-summary" },
              reputation.salesCount !== undefined && h("div", null,
                h("span", null, "Total Sales"),
                h("strong", null, reputation.salesCount || 0)
              ),
              reputation.avgResponseTime !== undefined && h("div", null,
                h("span", null, "Avg Response"),
                h("strong", null, reputation.avgResponseTime || "—")
              ),
              reputation.fulfillmentRate !== undefined && h("div", null,
                h("span", null, "Fulfillment Rate"),
                h("strong", null, `${Math.round((reputation.fulfillmentRate || 0) * 100)}%`)
              ),
              reputation.returnRate !== undefined && h("div", null,
                h("span", null, "Return Rate"),
                h("strong", null, `${Math.round((reputation.returnRate || 0) * 100)}%`)
              )
            ),

            h("div", { style: { marginTop: "16px" } },
              reputation.fulfillmentRate !== undefined && h(MetricBar, {
                label: "Fulfillment Rate",
                value: reputation.fulfillmentRate,
                tone: "positive"
              }),
              reputation.disputeRate !== undefined && h(MetricBar, {
                label: "Dispute Rate",
                value: reputation.disputeRate,
                tone: reputation.disputeRate < 0.1 ? "positive" : "danger"
              }),
              reputation.returnRate !== undefined && h(MetricBar, {
                label: "Return Rate",
                value: reputation.returnRate,
                tone: reputation.returnRate < 0.1 ? "positive" : "warning"
              })
            )
          ),

          h("section", { className: "admin-panel" },
            h("div", { className: "admin-panel-header" },
              h("h3", null, "All Shops Leaderboard"),
              h("button", {
                type: "button",
                className: "secondary-button",
                onClick: loadLeaderboard,
                disabled: isLoadingLeaderboard
              }, isLoadingLeaderboard ? "Loading..." : showLeaderboard ? "Collapse" : "Show Leaderboard")
            ),
            showLeaderboard && h(DataTable, {
              columns: [
                {
                  key: "rank",
                  label: "#",
                  render: (row, index) => h("strong", null, index + 1)
                },
                { key: "shopName", label: "Shop", render: row => row.shopName || "—" },
                {
                  key: "reputationScore",
                  label: "Score",
                  render: row => h("strong", null, row.reputationScore || 0)
                },
                {
                  key: "fulfillmentRate",
                  label: "Fulfillment",
                  render: row => `${Math.round((row.fulfillmentRate || 0) * 100)}%`
                },
                {
                  key: "salesCount",
                  label: "Sales",
                  render: row => row.salesCount || 0
                },
                {
                  key: "badges",
                  label: "Badges",
                  render: row => row.badges && row.badges.length > 0
                    ? h("div", { className: "row-actions" },
                      row.badges.map((b, i) =>
                        h("span", { key: i, className: `badge badge-${badgeTone(b)}` }, b)
                      )
                    )
                    : "—"
                }
              ],
              rows: leaderboard,
              getRowKey: (row, i) => row.tenantId || row.shopName || i,
              emptyText: "No leaderboard data available."
            })
          )
        )
  );
}
