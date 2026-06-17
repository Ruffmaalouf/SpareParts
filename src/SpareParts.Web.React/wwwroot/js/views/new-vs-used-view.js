import { h, useCallback, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

export function NewVsUsedView({ api }) {
  const [partId, setPartId] = useState("");
  const [result, setResult] = useState(null);
  const [oemNewPrice, setOemNewPrice] = useState("");
  const [aftermarketNewPrice, setAftermarketNewPrice] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [status, setStatus] = useState("");

  const load = useCallback(async () => {
    if (!partId) return;
    setIsLoading(true); setStatus("Loading...");
    try {
      const data = await api.get(`/api/new-vs-used/${partId}`);
      setResult(data);
      setOemNewPrice(data.oemNewPrice ?? "");
      setAftermarketNewPrice(data.aftermarketNewPrice ?? "");
      setStatus("");
    } catch (err) { setStatus(err.message || "Not found."); }
    finally { setIsLoading(false); }
  }, [api, partId]);

  const handleUpdate = useCallback(async (e) => {
    e.preventDefault();
    try {
      await api.put("/api/new-vs-used/prices", {
        partId: Number(partId),
        oemNewPrice: oemNewPrice ? Number(oemNewPrice) : null,
        aftermarketNewPrice: aftermarketNewPrice ? Number(aftermarketNewPrice) : null
      });
      setStatus("Prices updated."); load();
    } catch (err) { setStatus(err.message || "Failed."); }
  }, [api, partId, oemNewPrice, aftermarketNewPrice, load]);

  return h("section", { className: "screen" },
    h(PageHeader, { title: "NewVsUsed Comparison", subtitle: "Compare used part savings against OEM and aftermarket new prices." }),
    h(StatusLine, { status }),
    h("div", { className: "admin-panel", style: { marginBottom: "16px" } },
      h("div", { className: "admin-panel-header" },
        h("h3", null, "Load Comparison"),
        h("button", { className: "primary-button", onClick: load, disabled: isLoading }, "Load")
      ),
      h("div", { className: "editor-grid" },
        h("label", null, h("span", null, "Part ID *"), h("input", { type: "number", value: partId, onChange: e => setPartId(e.target.value) }))
      )
    ),
    result && h("div", null,
      h("div", { className: "data-card", style: { marginBottom: "16px" } },
        h("div", { className: "data-card-header" }, h("strong", null, result.partName)),
        h("div", { className: "data-card-body", style: { display: "flex", gap: "32px", flexWrap: "wrap" } },
          h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "Used Price"), h("div", { style: { fontSize: "20px" } }, money(result.usedPrice, result.currency))),
          h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "OEM New"), h("div", { style: { fontSize: "20px" } }, result.oemNewPrice != null ? money(result.oemNewPrice, result.currency) : "—")),
          h("div", null, h("div", { style: { fontSize: "12px", color: "#888" } }, "Aftermarket New"), h("div", { style: { fontSize: "20px" } }, result.aftermarketNewPrice != null ? money(result.aftermarketNewPrice, result.currency) : "—")),
          h("div", null, h("div", { style: { fontSize: "12px", color: "#388e3c" } }, "Savings vs OEM"), h("div", { style: { fontSize: "20px", fontWeight: "bold", color: "#388e3c" } }, result.savingsVsOem != null ? `${money(result.savingsVsOem, result.currency)} (${result.savingsPercentVsOem}%)` : "—")),
          h("div", null, h("div", { style: { fontSize: "12px", color: "#388e3c" } }, "Savings vs Aftermarket"), h("div", { style: { fontSize: "20px", fontWeight: "bold", color: "#388e3c" } }, result.savingsVsAftermarket != null ? `${money(result.savingsVsAftermarket, result.currency)} (${result.savingsPercentVsAftermarket}%)` : "—"))
        )
      ),
      h("form", { className: "admin-panel", onSubmit: handleUpdate },
        h("div", { className: "admin-panel-header" }, h("h3", null, "Update New Prices"), h("button", { type: "submit", className: "primary-button" }, "Save")),
        h("div", { className: "editor-grid" },
          h("label", null, h("span", null, "OEM New Price"), h("input", { type: "number", step: "0.01", value: oemNewPrice, onChange: e => setOemNewPrice(e.target.value) })),
          h("label", null, h("span", null, "Aftermarket New Price"), h("input", { type: "number", step: "0.01", value: aftermarketNewPrice, onChange: e => setAftermarketNewPrice(e.target.value) }))
        )
      )
    )
  );
}
