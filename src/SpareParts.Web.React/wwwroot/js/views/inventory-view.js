import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { CommunicationPayloadFactory } from "../services/communication-payload-factory.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

export function InventoryView({ api }) {
  const [parts, setParts] = useState([]);
  const [filter, setFilter] = useState("");
  const [phone, setPhone] = useState("");
  const [name, setName] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading inventory...");
    try {
      setParts(await api.get("/api/parts?page=1&pageSize=120"));
      setStatus("Inventory loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load inventory.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const visibleParts = useMemo(() => {
    const query = filter.trim().toLowerCase();
    if (!query) return parts;
    return parts.filter((part) =>
      [part.internalCode, part.name, part.oemNumber, part.currency].some((value) =>
        String(value || "").toLowerCase().includes(query)
      )
    );
  }, [parts, filter]);

  const sendAvailability = useCallback(async (part) => {
    if (!phone.trim()) {
      setStatus("Enter a WhatsApp phone number first.");
      return;
    }

    setStatus("Sending part availability...");
    try {
      await api.post("/api/communications/send", CommunicationPayloadFactory.partAvailability({
        recipientName: name || "Customer",
        recipientPhone: phone,
        part
      }));
      setStatus("Part availability sent.");
    } catch (error) {
      setStatus(error.message || "Could not send availability.");
    }
  }, [api, name, phone]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: "Inventory",
      action: h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h("div", { className: "filters-row" },
      h("input", { value: filter, onChange: (event) => setFilter(event.target.value), placeholder: "Filter by code, name, OEM" }),
      h("input", { value: name, onChange: (event) => setName(event.target.value), placeholder: "Recipient name" }),
      h("input", { value: phone, onChange: (event) => setPhone(event.target.value), placeholder: "WhatsApp phone" })
    ),
    h(StatusLine, { status }),
    h("section", { className: "table-panel" },
      h(DataTable, {
        columns: [
          { key: "code", label: "Code", render: (part) => part.internalCode },
          { key: "part", label: "Part", render: (part) => part.name },
          { key: "oem", label: "OEM", render: (part) => part.oemNumber || "" },
          { key: "cost", label: "Cost", render: (part) => money(part.costPrice, part.currency) },
          { key: "sale", label: "Sale", render: (part) => money(part.salePrice, part.currency) },
          { key: "status", label: "Status", render: (part) => part.isActive ? "Active" : "Inactive" },
          { key: "action", label: "Action", render: (part) => h("button", { onClick: () => sendAvailability(part) }, "Send Availability") }
        ],
        rows: visibleParts,
        getRowKey: (part) => part.id,
        emptyText: "No parts found."
      })
    )
  );
}
