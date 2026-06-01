import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

function read(row, ...keys) {
  for (const key of keys) {
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") return value;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row?.[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }
  return "";
}

function toArray(value) {
  return Array.isArray(value) ? value : value ? [value] : [];
}

function itemId(row) {
  return read(row, "id");
}

function normalizeCode(value) {
  return String(value || "").toUpperCase().replace(/[^A-Z0-9]/g, "");
}

function normalizeText(value) {
  return String(value || "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, " ")
    .trim()
    .replace(/\s+/g, " ");
}

function partTitle(part) {
  return read(part, "name") || read(part, "internalCode") || `Part #${itemId(part)}`;
}

function partSubtitle(part) {
  const code = read(part, "internalCode") || `#${itemId(part)}`;
  const oem = read(part, "oemNumber");
  return oem ? `${code} / OEM ${oem}` : code;
}

function carTitle(car) {
  const year = read(car, "modelYear");
  const name = read(car, "car") || `Vehicle #${itemId(car)}`;
  return `${year || ""} ${name}`.trim();
}

function carModelTitle(car) {
  return read(car, "car") || `Vehicle #${itemId(car)}`;
}

function compactYearList(years) {
  const sorted = [...years].filter(Boolean).sort((a, b) => Number(b) - Number(a));
  if (sorted.length <= 4) return sorted.join(", ");
  return `${sorted.slice(0, 4).join(", ")} +${sorted.length - 4}`;
}

function splitLabel(value, max = 18) {
  const words = String(value || "").split(/\s+/).filter(Boolean);
  const lines = [""];

  words.forEach((word) => {
    const current = lines[lines.length - 1];
    if (!current) {
      lines[lines.length - 1] = word;
      return;
    }

    if (`${current} ${word}`.length <= max) {
      lines[lines.length - 1] = `${current} ${word}`;
      return;
    }

    if (lines.length < 2) {
      lines.push(word);
    } else {
      lines[1] = `${lines[1]} ${word}`.trim();
    }
  });

  return lines.slice(0, 2).map((line) => line.length > max ? `${line.slice(0, max - 1)}...` : line);
}

function matchPart(selectedPart, part) {
  const reasons = [];
  const selectedId = String(itemId(selectedPart) || "");
  const partId = String(itemId(part) || "");
  const selectedOem = normalizeCode(read(selectedPart, "oemNumber"));
  const partOem = normalizeCode(read(part, "oemNumber"));
  const selectedName = normalizeText(read(selectedPart, "name"));
  const partName = normalizeText(read(part, "name"));
  const selectedCategory = String(read(selectedPart, "categoryId") || "");
  const partCategory = String(read(part, "categoryId") || "");

  if (selectedId && selectedId === partId) {
    reasons.push({ label: "Selected part", weight: 120 });
  }

  if (selectedOem && selectedOem === partOem) {
    reasons.push({ label: "OEM match", weight: 96 });
  }

  if (selectedName && selectedName === partName && selectedCategory && selectedCategory === partCategory) {
    reasons.push({ label: "Name + category", weight: 76 });
  } else if (selectedName && selectedName.length >= 6 && selectedName === partName) {
    reasons.push({ label: "Name match", weight: 56 });
  }

  return reasons;
}

function bestReason(reasons) {
  return [...reasons].sort((a, b) => b.weight - a.weight)[0] || { label: "Related", weight: 0 };
}

function buildCompatibility(selectedPart, parts, carsById) {
  if (!selectedPart) {
    return {
      matchedParts: [],
      vehicles: [],
      modelGroups: [],
      proofParts: [],
      years: []
    };
  }

  const matchedParts = parts
    .map((part) => ({ part, reasons: matchPart(selectedPart, part) }))
    .filter((match) => match.reasons.length > 0)
    .map((match) => {
      const reason = bestReason(match.reasons);
      return {
        ...match,
        reason: reason.label,
        score: reason.weight
      };
    })
    .sort((a, b) => b.score - a.score || String(partTitle(a.part)).localeCompare(String(partTitle(b.part))));

  const vehicleMap = new Map();
  matchedParts.forEach((match) => {
    const usedCarId = read(match.part, "usedCarId");
    if (!usedCarId) return;

    const car = carsById.get(String(usedCarId));
    if (!car) return;

    const key = String(usedCarId);
    if (!vehicleMap.has(key)) {
      vehicleMap.set(key, {
        id: key,
        car,
        parts: [],
        reasons: new Set(),
        score: 0
      });
    }

    const vehicle = vehicleMap.get(key);
    vehicle.parts.push(match.part);
    vehicle.reasons.add(match.reason);
    vehicle.score = Math.max(vehicle.score, match.score);
  });

  const vehicles = [...vehicleMap.values()]
    .map((vehicle) => ({
      ...vehicle,
      reasons: [...vehicle.reasons],
      title: carTitle(vehicle.car),
      model: carModelTitle(vehicle.car),
      year: read(vehicle.car, "modelYear")
    }))
    .sort((a, b) => b.score - a.score || Number(b.year || 0) - Number(a.year || 0) || a.title.localeCompare(b.title));

  const modelMap = new Map();
  vehicles.forEach((vehicle) => {
    const key = vehicle.model;
    if (!modelMap.has(key)) {
      modelMap.set(key, {
        model: key,
        years: new Set(),
        vehicleIds: new Set(),
        parts: new Map(),
        reasons: new Set(),
        score: 0
      });
    }

    const group = modelMap.get(key);
    if (vehicle.year) group.years.add(vehicle.year);
    group.vehicleIds.add(vehicle.id);
    vehicle.parts.forEach((part) => group.parts.set(String(itemId(part)), part));
    vehicle.reasons.forEach((reason) => group.reasons.add(reason));
    group.score = Math.max(group.score, vehicle.score);
  });

  const modelGroups = [...modelMap.values()]
    .map((group) => ({
      ...group,
      years: [...group.years].sort((a, b) => Number(b) - Number(a)),
      parts: [...group.parts.values()],
      reasons: [...group.reasons],
      vehicles: group.vehicleIds.size
    }))
    .sort((a, b) => b.score - a.score || a.model.localeCompare(b.model));

  const selectedId = String(itemId(selectedPart) || "");
  const proofParts = matchedParts
    .filter((match) => String(itemId(match.part) || "") !== selectedId)
    .filter((match) => read(match.part, "usedCarId"))
    .slice(0, 18);

  const years = [...new Set(vehicles.map((vehicle) => vehicle.year).filter(Boolean))]
    .sort((a, b) => Number(b) - Number(a));

  return {
    matchedParts,
    vehicles,
    modelGroups,
    proofParts,
    years
  };
}

function graphPath(from, to) {
  const dx = Math.abs(to.x - from.x);
  const dy = Math.abs(to.y - from.y);
  const bend = Math.max(60, Math.min(150, (dx + dy) / 2));
  const c1 = { x: from.x + (to.x > from.x ? bend : -bend), y: from.y };
  const c2 = { x: to.x + (to.x > from.x ? -bend : bend), y: to.y };
  return `M ${from.x} ${from.y} C ${c1.x} ${c1.y}, ${c2.x} ${c2.y}, ${to.x} ${to.y}`;
}

function buildGraphLayout(selectedPart, compatibility) {
  const center = {
    id: "selected",
    kind: "center",
    title: partTitle(selectedPart),
    subtitle: partSubtitle(selectedPart),
    x: 460,
    y: 280,
    r: 74
  };
  const vehicles = compatibility.vehicles.slice(0, 10);
  const proofParts = compatibility.proofParts.slice(0, 8);
  const nodes = [center];
  const edges = [];
  const vehicleNodeById = new Map();

  vehicles.forEach((vehicle, index) => {
    const angle = (-90 + (vehicles.length === 1 ? 0 : (360 / vehicles.length) * index)) * Math.PI / 180;
    const node = {
      id: `vehicle-${vehicle.id}`,
      kind: "vehicle",
      title: vehicle.title,
      subtitle: vehicle.reasons.join(" / "),
      x: center.x + Math.cos(angle) * 220,
      y: center.y + Math.sin(angle) * 178,
      r: 58,
      vehicle
    };
    nodes.push(node);
    vehicleNodeById.set(vehicle.id, node);
    edges.push({
      id: `selected-${vehicle.id}`,
      from: center,
      to: node,
      strength: vehicle.score >= 90 ? "strong" : "soft"
    });
  });

  proofParts.forEach((match, index) => {
    const carId = String(read(match.part, "usedCarId") || "");
    const vehicleNode = vehicleNodeById.get(carId);
    if (!vehicleNode) return;

    const angle = (-72 + (proofParts.length === 1 ? 0 : (144 / Math.max(proofParts.length - 1, 1)) * index)) * Math.PI / 180;
    const node = {
      id: `part-${itemId(match.part)}`,
      kind: "part",
      title: partTitle(match.part),
      subtitle: partSubtitle(match.part),
      x: center.x + Math.cos(angle) * 350,
      y: center.y + Math.sin(angle) * 238,
      r: 45,
      part: match.part,
      reason: match.reason
    };
    nodes.push(node);
    edges.push({
      id: `vehicle-${carId}-part-${itemId(match.part)}`,
      from: vehicleNode,
      to: node,
      strength: "proof"
    });
  });

  return { nodes, edges };
}

function GraphNode({ node, onSelectPart }) {
  const lines = splitLabel(node.title, node.kind === "center" ? 22 : 17);
  const selectable = node.kind === "part" && node.part;
  const attrs = {
    className: `compat-node ${node.kind}${selectable ? " selectable" : ""}`,
    transform: `translate(${node.x} ${node.y})`
  };

  if (selectable) {
    attrs.onClick = () => onSelectPart(String(itemId(node.part)));
    attrs.role = "button";
    attrs.tabIndex = 0;
  }

  return h("g", attrs,
    h("circle", { r: node.r }),
    h("text", { className: "compat-node-title", y: lines.length === 1 ? 2 : -5 }, lines[0] || ""),
    lines[1] && h("text", { className: "compat-node-title", y: 12 }, lines[1]),
    h("text", { className: "compat-node-subtitle", y: node.kind === "center" ? 34 : 28 }, splitLabel(node.subtitle, 21)[0] || ""),
    h("title", null, `${node.title}${node.subtitle ? ` / ${node.subtitle}` : ""}`)
  );
}

function CompatibilityGraph({ selectedPart, compatibility, onSelectPart }) {
  if (!selectedPart) {
    return h("div", { className: "compat-empty-graph" }, "Select a part to see fitment.");
  }

  const layout = buildGraphLayout(selectedPart, compatibility);
  const hasVehicles = compatibility.vehicles.length > 0;

  return h("div", { className: "compat-graph-frame" },
    h("svg", { className: "compat-graph", viewBox: "0 0 920 560", role: "img", "aria-label": "Part compatibility graph" },
      h("defs", null,
        h("radialGradient", { id: "compat-core", cx: "45%", cy: "35%" },
          h("stop", { offset: "0%", stopColor: "color-mix(in srgb, var(--accent) 74%, white)" }),
          h("stop", { offset: "100%", stopColor: "var(--accent)" })
        )
      ),
      layout.edges.map((edge) =>
        h("path", {
          key: edge.id,
          className: `compat-edge ${edge.strength}`,
          d: graphPath(edge.from, edge.to)
        })
      ),
      layout.nodes.map((node) =>
        h(GraphNode, {
          key: node.id,
          node,
          onSelectPart
        })
      )
    ),
    !hasVehicles && h("div", { className: "compat-graph-empty-overlay" },
      h("strong", null, "No fitment links yet"),
      h("span", null, "Assign parts to used cars or add matching OEM numbers to build the network.")
    )
  );
}

function MetricTile({ label, value }) {
  return h("div", { className: "compat-metric" },
    h("span", null, label),
    h("strong", null, value)
  );
}

function PartPicker({ parts, selectedPartId, onSelect }) {
  return h("section", { className: "compat-part-picker" },
    parts.map((part) => {
      const active = String(itemId(part)) === String(selectedPartId);
      return h("button", {
        key: itemId(part),
        className: active ? "compat-part-row active" : "compat-part-row",
        type: "button",
        onClick: () => onSelect(String(itemId(part)))
      },
        h("strong", null, partTitle(part)),
        h("span", null, partSubtitle(part)),
        h("small", null, read(part, "usedCarId") ? `Donor car #${read(part, "usedCarId")}` : "No donor link")
      );
    }),
    parts.length === 0 && h("p", { className: "empty-state" }, "No parts match this search.")
  );
}

function FitmentInspector({ selectedPart, compatibility }) {
  if (!selectedPart) {
    return h("aside", { className: "compat-inspector panel" },
      h("h3", null, "Fitment")
    );
  }

  return h("aside", { className: "compat-inspector panel" },
    h("h3", null, "Fitment"),
    h("div", { className: "compat-selected-part" },
      h("span", null, "Selected part"),
      h("strong", null, partTitle(selectedPart)),
      h("small", null, `${partSubtitle(selectedPart)} / ${money(read(selectedPart, "salePrice"), read(selectedPart, "currency") || "USD")}`)
    ),
    h("div", { className: "compat-chip-row" },
      read(selectedPart, "oemNumber") && h("span", null, `OEM ${read(selectedPart, "oemNumber")}`),
      read(selectedPart, "availableQuantity") !== "" && h("span", null, `${read(selectedPart, "availableQuantity")} available`),
      read(selectedPart, "isActive") === false ? h("span", null, "Inactive") : h("span", null, "Active")
    ),
    h("div", { className: "compat-model-list" },
      h("h4", null, "Models and years"),
      compatibility.modelGroups.map((group) =>
        h("article", { className: "compat-model-row", key: group.model },
          h("div", null,
            h("strong", null, group.model),
            h("span", null, `${group.vehicles} vehicle record(s) / ${group.parts.length} proof part(s)`)
          ),
          h("b", null, compactYearList(group.years)),
          h("small", null, group.reasons.join(" / "))
        )
      ),
      compatibility.modelGroups.length === 0 && h("p", { className: "empty-state" }, "No compatible model/year records yet.")
    )
  );
}

function ProofParts({ compatibility, carsById, onSelectPart }) {
  return h("section", { className: "panel compat-proof-panel" },
    h("div", { className: "admin-panel-header" },
      h("h3", null, "Evidence parts"),
      h("span", null, `${compatibility.proofParts.length}`)
    ),
    h("div", { className: "compat-proof-list" },
      compatibility.proofParts.map((match) => {
        const car = carsById.get(String(read(match.part, "usedCarId") || ""));
        return h("button", {
          key: itemId(match.part),
          className: "compat-proof-row",
          type: "button",
          onClick: () => onSelectPart(String(itemId(match.part)))
        },
          h("div", null,
            h("strong", null, partTitle(match.part)),
            h("span", null, `${partSubtitle(match.part)} / ${car ? carTitle(car) : "No vehicle"}`)
          ),
          h("b", null, match.reason)
        );
      }),
      compatibility.proofParts.length === 0 && h("p", { className: "empty-state" }, "No related parts with vehicle links yet.")
    )
  );
}

export function PartCompatibilityView({ api }) {
  const [parts, setParts] = useState([]);
  const [cars, setCars] = useState([]);
  const [search, setSearch] = useState("");
  const [selectedPartId, setSelectedPartId] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const carsById = useMemo(() => {
    const map = new Map();
    cars.forEach((car) => map.set(String(itemId(car)), car));
    return map;
  }, [cars]);

  const selectedPart = useMemo(
    () => parts.find((part) => String(itemId(part)) === String(selectedPartId)) || null,
    [parts, selectedPartId]
  );

  const compatibility = useMemo(
    () => buildCompatibility(selectedPart, parts, carsById),
    [carsById, parts, selectedPart]
  );

  const visibleParts = useMemo(() => {
    const query = normalizeText(search);
    if (!query) return parts;

    return parts.filter((part) => {
      const car = carsById.get(String(read(part, "usedCarId") || ""));
      const haystack = normalizeText([
        read(part, "internalCode"),
        read(part, "name"),
        read(part, "oemNumber"),
        read(part, "notes"),
        car ? carTitle(car) : ""
      ].join(" "));
      return haystack.includes(query);
    });
  }, [carsById, parts, search]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading compatibility graph...");
    try {
      const [nextParts, nextCars] = await Promise.all([
        api.list("/api/parts"),
        api.get("/api/usedcars")
      ]);
      const partRows = toArray(nextParts);
      const carRows = toArray(nextCars);
      setParts(partRows);
      setCars(carRows);
      setSelectedPartId((current) => {
        if (current && partRows.some((part) => String(itemId(part)) === String(current))) return current;
        const bestPart = partRows.find((part) => read(part, "usedCarId") && read(part, "oemNumber"))
          || partRows.find((part) => read(part, "usedCarId"))
          || partRows.find((part) => read(part, "oemNumber"))
          || partRows[0];
        return bestPart ? String(itemId(bestPart)) : "";
      });
      setStatus(`${partRows.length} parts and ${carRows.length} vehicles loaded.`);
    } catch (error) {
      setParts([]);
      setCars([]);
      setStatus(error.message || "Could not load compatibility data.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => {
    load();
  }, [load]);

  return h("section", { className: "screen compatibility-screen" },
    h(PageHeader, {
      kicker: "Sales assist",
      title: "Part Compatibility",
      subtitle: "Visual fitment by donor vehicle, OEM number, model, and year.",
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: load,
        disabled: isLoading
      }, isLoading ? "Loading" : "Refresh")
    }),
    h("section", { className: "compat-summary-grid" },
      h(MetricTile, { label: "Compatible models", value: compatibility.modelGroups.length }),
      h(MetricTile, { label: "Vehicle years", value: compatibility.years.length ? compactYearList(compatibility.years) : "-" }),
      h(MetricTile, { label: "Proof parts", value: compatibility.proofParts.length }),
      h(MetricTile, { label: "Catalog matches", value: compatibility.matchedParts.length })
    ),
    h("div", { className: "filters-row compatibility-filters" },
      h("input", {
        value: search,
        onChange: (event) => setSearch(event.target.value),
        placeholder: "Search code, part, OEM, vehicle"
      })
    ),
    h(StatusLine, { status }),
    h("section", { className: "compat-workspace" },
      h("aside", { className: "panel compat-selector-panel" },
        h("div", { className: "admin-panel-header" },
          h("h3", null, "Parts"),
          h("span", null, `${visibleParts.length}`)
        ),
        h(PartPicker, {
          parts: visibleParts.slice(0, 80),
          selectedPartId,
          onSelect: setSelectedPartId
        })
      ),
      h("main", { className: "compat-main" },
        h("section", { className: "panel compat-graph-panel" },
          h("div", { className: "admin-panel-header" },
            h("h3", null, selectedPart ? partTitle(selectedPart) : "Graph"),
            selectedPart && h("span", null, partSubtitle(selectedPart))
          ),
          h(CompatibilityGraph, {
            selectedPart,
            compatibility,
            onSelectPart: setSelectedPartId
          })
        ),
        h(ProofParts, {
          compatibility,
          carsById,
          onSelectPart: setSelectedPartId
        })
      ),
      h(FitmentInspector, {
        selectedPart,
        compatibility
      })
    )
  );
}
