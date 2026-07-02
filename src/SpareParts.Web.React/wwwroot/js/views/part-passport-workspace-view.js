import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { asRows } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";
import { passportHref } from "./part-passport-view.js";
import { read } from "../admin/resource-utils.js";

const selectedPartStorageKey = "spareparts.part-passport.selected-part-id";
const conditionLabels = { 1: "New", 2: "Used", 3: "Rebuilt" };

function partTitle(part) {
  return read(part, "name") || read(part, "internalCode") || `Part #${read(part, "id")}`;
}

function partCondition(part) {
  const value = read(part, "condition");
  return conditionLabels[value] || String(value || "Not recorded");
}

function usedCarTitle(car) {
  const title = read(car, "car") || `Used car #${read(car, "id")}`;
  const year = read(car, "modelYear");
  return year ? `${title} / ${year}` : title;
}

function decoratePart(part, carsById) {
  const usedCarId = read(part, "usedCarId");
  const car = usedCarId ? carsById.get(String(usedCarId)) : null;
  return {
    ...part,
    condition: partCondition(part),
    donorCar: car ? usedCarTitle(car) : "Warehouse stock",
    storageLocation: read(car, "location") || "Shelf stock"
  };
}

export function selectPartPassport(part, onView) {
  const id = read(part, "id");
  if (id) window.sessionStorage.setItem(selectedPartStorageKey, String(id));
  if (typeof onView === "function") onView("part-passport");
}

function DetailTile({ label, value }) {
  return h("div", { className: "passport-admin-detail" },
    h("span", null, label),
    h("strong", null, value || "Not recorded")
  );
}

export function PartPassportWorkspaceView({ api }) {
  const [parts, setParts] = useState([]);
  const [usedCars, setUsedCars] = useState([]);
  const [filter, setFilter] = useState("");
  const [selectedPartId, setSelectedPartId] = useState(() => window.sessionStorage.getItem(selectedPartStorageKey) || "");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading passport workspace...");
    try {
      const [nextParts, nextCars] = await Promise.all([
        api.list("/api/parts"),
        api.get("/api/usedcars").catch(() => [])
      ]);
      const partRows = asRows(nextParts);
      setParts(partRows);
      setUsedCars(asRows(nextCars));
      setSelectedPartId((current) => {
        if (partRows.some((part) => String(read(part, "id")) === String(current))) return String(current);
        const best = partRows.find((part) => read(part, "usedCarId") && read(part, "oemNumber")) || partRows[0];
        const bestId = best ? String(read(best, "id")) : "";
        if (bestId) window.sessionStorage.setItem(selectedPartStorageKey, bestId);
        return bestId;
      });
      setStatus("Choose a part, review its passport draft, then copy or open the public link.");
    } catch (error) {
      setParts([]);
      setUsedCars([]);
      setStatus(error.message || "Could not load passport workspace.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { load(); }, [load]);

  const carsById = useMemo(
    () => new Map(usedCars.map((car) => [String(read(car, "id")), car])),
    [usedCars]
  );
  const visibleParts = useMemo(() => {
    const query = filter.trim().toLowerCase();
    if (!query) return parts;
    return parts.filter((part) =>
      [read(part, "internalCode"), read(part, "name"), read(part, "oemNumber"), read(part, "barcode")]
        .some((value) => String(value || "").toLowerCase().includes(query))
    );
  }, [filter, parts]);
  const displayedParts = useMemo(() => visibleParts.slice(0, 120), [visibleParts]);
  const selectedPart = useMemo(
    () => parts.find((part) => String(read(part, "id")) === String(selectedPartId)) || null,
    [parts, selectedPartId]
  );
  const passportPart = useMemo(
    () => selectedPart ? decoratePart(selectedPart, carsById) : null,
    [carsById, selectedPart]
  );
  const publicPath = passportPart ? passportHref(passportPart) : "";
  const publicUrl = publicPath ? new URL(publicPath, window.location.origin).href : "";
  const whatsappHref = publicUrl
    ? `https://wa.me/?text=${encodeURIComponent(`Part Passport: ${partTitle(passportPart)}\n${publicUrl}`)}`
    : "";

  const choosePart = useCallback((part) => {
    const id = String(read(part, "id"));
    setSelectedPartId(id);
    window.sessionStorage.setItem(selectedPartStorageKey, id);
    setStatus(`${partTitle(part)} selected. Review the draft before sending it.`);
  }, []);

  const copyLink = useCallback(async () => {
    if (!publicUrl) return;
    try {
      await navigator.clipboard.writeText(publicUrl);
      setStatus("Passport link copied. Paste it into WhatsApp when the draft is ready.");
    } catch {
      setStatus("Could not copy automatically. Open the public card and copy the browser URL.");
    }
  }, [publicUrl]);

  return h("section", { className: "screen passport-admin-screen" },
    h(PageHeader, {
      kicker: "Customer trust card",
      title: "Part Passport",
      subtitle: "Select a part, review its public proof card, and send the WhatsApp-ready link in seconds.",
      action: h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading },
        isLoading ? "Loading" : "Refresh"
      )
    }),
    h(StatusLine, { status }),
    h("section", { className: "passport-admin-layout" },
      h("aside", { className: "passport-admin-list-panel" },
        h("div", { className: "passport-admin-panel-heading" },
          h("div", null,
            h("span", null, "Inventory"),
            h("h3", null, "Choose a part")
          ),
          h("b", null, visibleParts.length)
        ),
        h("input", {
          value: filter,
          onChange: (event) => setFilter(event.target.value),
          placeholder: "Search code, part name, OEM, barcode"
        }),
        h("div", { className: "passport-admin-part-list" },
          displayedParts.map((part) => {
            const id = String(read(part, "id"));
            const isActive = id === String(selectedPartId);
            return h("button", {
              className: isActive ? "passport-admin-part active" : "passport-admin-part",
              key: id,
              type: "button",
              onClick: () => choosePart(part)
            },
              h("span", null, read(part, "internalCode") || `#${id}`),
              h("strong", null, partTitle(part)),
              h("small", null, read(part, "oemNumber") ? `OEM ${read(part, "oemNumber")}` : "OEM not recorded")
            );
          }),
          visibleParts.length > displayedParts.length && h("p", { className: "passport-admin-list-note" },
            `Showing the first ${displayedParts.length} parts. Refine your search to find a specific passport.`
          ),
          visibleParts.length === 0 && h("p", { className: "empty-state" }, "No parts match this search.")
        )
      ),
      h("section", { className: "passport-admin-preview-panel" },
        passportPart
          ? h("div", { className: "passport-admin-preview" },
            h("header", { className: "passport-admin-preview-heading" },
              h("div", null,
                h("span", null, "Public card draft"),
                h("h3", null, partTitle(passportPart))
              ),
              h("b", null, "Review required")
            ),
            h("div", { className: "passport-admin-proof-grid" },
              h(DetailTile, { label: "OEM number", value: read(passportPart, "oemNumber") }),
              h(DetailTile, { label: "Donor source", value: passportPart.donorCar }),
              h(DetailTile, { label: "Condition", value: passportPart.condition }),
              h(DetailTile, { label: "Shelf location", value: passportPart.storageLocation })
            ),
            h("section", { className: "passport-admin-readiness" },
              h("div", { className: "passport-admin-readiness-ring" }, "--"),
              h("div", null,
                h("strong", null, "Fitment confidence pending"),
                h("span", null, "Confirm chassis fitment and attach inspection photos before sending the link.")
              )
            ),
            h("section", { className: "passport-admin-photo-strip", "aria-label": "Inspection photo placeholders" },
              h("div", null, "Front"),
              h("div", null, "Housing"),
              h("div", null, "Detail")
            ),
            h("label", { className: "passport-admin-link-field" },
              "Public passport URL",
              h("input", { readOnly: true, value: publicUrl })
            ),
            h("div", { className: "passport-admin-actions" },
              h("button", { className: "primary-button", type: "button", onClick: copyLink }, "Copy passport link"),
              h("a", { className: "secondary-button", href: publicPath, target: "_blank", rel: "noreferrer" }, "Open public card"),
              h("a", { className: "secondary-button passport-admin-whatsapp", href: whatsappHref, target: "_blank", rel: "noreferrer" }, "Prepare WhatsApp")
            ),
            h("p", { className: "passport-admin-note" },
              "Draft links intentionally show missing inspection photos and fitment confidence as pending. Complete those checks before sharing with a customer."
            )
          )
          : h("p", { className: "empty-state" }, "Choose an inventory part to prepare its passport.")
      )
    )
  );
}
