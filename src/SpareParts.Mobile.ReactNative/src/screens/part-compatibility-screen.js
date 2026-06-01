const React = require("react");
const { Pressable, ScrollView, Text, View } = require("react-native");
const { Circle, G, Path, Svg, Text: SvgText } = require("react-native-svg");
const { money } = require("../core/formatters");
const { Field, ListRow, Panel, ScreenHeader, ScreenScroll, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

function read(row, ...keys) {
  for (const key of keys) {
    const value = row && row[key];
    if (value !== undefined && value !== null && value !== "") return value;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row && row[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }
  return "";
}

function asRows(value) {
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

function splitLabel(value, max) {
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

  if (selectedId && selectedId === partId) reasons.push({ label: "Selected part", weight: 120 });
  if (selectedOem && selectedOem === partOem) reasons.push({ label: "OEM match", weight: 96 });

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
    return { matchedParts: [], vehicles: [], modelGroups: [], proofParts: [], years: [] };
  }

  const matchedParts = parts
    .map((part) => ({ part, reasons: matchPart(selectedPart, part) }))
    .filter((match) => match.reasons.length > 0)
    .map((match) => {
      const reason = bestReason(match.reasons);
      return { ...match, reason: reason.label, score: reason.weight };
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
      vehicleMap.set(key, { id: key, car, parts: [], reasons: new Set(), score: 0 });
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
    .slice(0, 12);
  const years = [...new Set(vehicles.map((vehicle) => vehicle.year).filter(Boolean))]
    .sort((a, b) => Number(b) - Number(a));

  return { matchedParts, vehicles, modelGroups, proofParts, years };
}

function graphPath(from, to) {
  const c1 = { x: from.x + (to.x > from.x ? 48 : -48), y: from.y };
  const c2 = { x: to.x + (to.x > from.x ? -48 : 48), y: to.y };
  return `M ${from.x} ${from.y} C ${c1.x} ${c1.y}, ${c2.x} ${c2.y}, ${to.x} ${to.y}`;
}

function buildGraphLayout(selectedPart, compatibility) {
  const center = {
    id: "selected",
    kind: "center",
    title: partTitle(selectedPart),
    subtitle: partSubtitle(selectedPart),
    x: 180,
    y: 130,
    r: 42
  };
  const vehicles = compatibility.vehicles.slice(0, 6);
  const proofParts = compatibility.proofParts.slice(0, 4);
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
      x: center.x + Math.cos(angle) * 112,
      y: center.y + Math.sin(angle) * 88,
      r: 34,
      vehicle
    };
    nodes.push(node);
    vehicleNodeById.set(vehicle.id, node);
    edges.push({ id: `selected-${vehicle.id}`, from: center, to: node, strength: vehicle.score >= 90 ? "strong" : "soft" });
  });

  proofParts.forEach((match, index) => {
    const carId = String(read(match.part, "usedCarId") || "");
    const vehicleNode = vehicleNodeById.get(carId);
    if (!vehicleNode) return;
    const angle = (-65 + (proofParts.length === 1 ? 0 : (130 / Math.max(proofParts.length - 1, 1)) * index)) * Math.PI / 180;
    const node = {
      id: `part-${itemId(match.part)}`,
      kind: "part",
      title: partTitle(match.part),
      subtitle: partSubtitle(match.part),
      x: center.x + Math.cos(angle) * 158,
      y: center.y + Math.sin(angle) * 106,
      r: 28,
      part: match.part
    };
    nodes.push(node);
    edges.push({ id: `vehicle-${carId}-part-${itemId(match.part)}`, from: vehicleNode, to: node, strength: "proof" });
  });

  return { nodes, edges };
}

function CompatibilityGraph({ selectedPart, compatibility, onSelectPart }) {
  const { palette, styles, t } = useTheme();
  if (!selectedPart) {
    return el(View, { style: styles.compatEmptyGraph },
      el(Text, { style: styles.emptyState }, t("compatibility.selectPart", "Select a part to see fitment."))
    );
  }

  const layout = buildGraphLayout(selectedPart, compatibility);

  return el(View, { style: styles.compatGraphFrame },
    el(Svg, { width: "100%", height: 260, viewBox: "0 0 360 260" },
      layout.edges.map((edge) =>
        el(Path, {
          key: edge.id,
          d: graphPath(edge.from, edge.to),
          fill: "none",
          stroke: edge.strength === "proof" ? palette.whatsapp || palette.accent : palette.accent,
          strokeDasharray: edge.strength === "proof" ? "7 7" : undefined,
          strokeOpacity: edge.strength === "strong" ? 0.92 : 0.58,
          strokeWidth: edge.strength === "strong" ? 3 : 2
        })
      ),
      layout.nodes.map((node) => {
        const selectable = node.kind === "part" && node.part;
        const titleLines = splitLabel(node.title, node.kind === "center" ? 15 : 12);
        const fill = node.kind === "center"
          ? palette.accent
          : node.kind === "vehicle"
            ? palette.surface
            : palette.surface2;
        return el(G, {
          key: node.id,
          onPress: selectable ? () => onSelectPart(String(itemId(node.part))) : undefined
        },
          el(Circle, {
            cx: node.x,
            cy: node.y,
            r: node.r,
            fill,
            stroke: node.kind === "part" ? palette.whatsapp || palette.accent : palette.line,
            strokeWidth: selectable ? 2 : 1
          }),
          el(SvgText, {
            x: node.x,
            y: node.y + (titleLines.length === 1 ? 2 : -4),
            fill: node.kind === "center" ? "#ffffff" : palette.text,
            fontSize: node.kind === "center" ? 10 : 8,
            fontWeight: "900",
            textAnchor: "middle"
          }, titleLines[0] || ""),
          titleLines[1] && el(SvgText, {
            x: node.x,
            y: node.y + 8,
            fill: node.kind === "center" ? "#ffffff" : palette.text,
            fontSize: node.kind === "center" ? 10 : 8,
            fontWeight: "900",
            textAnchor: "middle"
          }, titleLines[1])
        );
      })
    ),
    compatibility.vehicles.length === 0 && el(View, { style: styles.compatGraphOverlay },
      el(Text, { style: styles.compatGraphOverlayTitle }, t("compatibility.noLinks", "No fitment links yet")),
      el(Text, { style: styles.compatGraphOverlayText }, t("compatibility.linkHint", "Assign parts to used cars or match OEM numbers."))
    )
  );
}

function Metric({ label, value }) {
  const { styles } = useTheme();
  return el(View, { style: styles.compatMetric },
    el(Text, { style: styles.compatMetricLabel }, label),
    el(Text, { style: styles.compatMetricValue }, String(value))
  );
}

function PartCompatibilityScreen({ api }) {
  const { styles, t } = useTheme();
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
        read(part, "barcode"),
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
    setStatus(t("compatibility.loading", "Loading compatibility graph..."));
    try {
      const [nextParts, nextCars] = await Promise.all([
        api.list("/api/parts"),
        api.get("/api/usedcars")
      ]);
      const partRows = asRows(nextParts);
      const carRows = asRows(nextCars);
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
      setStatus(t("compatibility.loaded", "{parts} parts and {cars} vehicles loaded.", { parts: partRows.length, cars: carRows.length }));
    } catch (error) {
      setParts([]);
      setCars([]);
      setStatus(error.message || t("compatibility.loadError", "Could not load compatibility data."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => { load(); }, [load]);

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: t("compatibility.title", "Part Compatibility"),
      actionTitle: t("common.refresh", "Refresh"),
      onAction: load,
      loading: isLoading
    }),
    el(View, { style: styles.compatMetricGrid },
      el(Metric, { label: t("compatibility.models", "Models"), value: compatibility.modelGroups.length }),
      el(Metric, { label: t("compatibility.years", "Years"), value: compatibility.years.length ? compactYearList(compatibility.years) : "-" }),
      el(Metric, { label: t("compatibility.proofParts", "Proof parts"), value: compatibility.proofParts.length }),
      el(Metric, { label: t("compatibility.matches", "Matches"), value: compatibility.matchedParts.length })
    ),
    el(Field, {
      label: t("common.filter", "Filter"),
      value: search,
      onChangeText: setSearch,
      placeholder: t("compatibility.searchPlaceholder", "Code, part, OEM, vehicle")
    }),
    el(StatusText, { value: status }),
    el(Panel, { title: t("compatibility.graph", "Fitment graph") },
      el(CompatibilityGraph, { selectedPart, compatibility, onSelectPart: setSelectedPartId })
    ),
    el(Panel, { title: t("compatibility.parts", "Parts") },
      el(View, { style: styles.screenListFrameLarge },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          visibleParts.slice(0, 100).map((part) => {
            const active = String(itemId(part)) === String(selectedPartId);
            return el(Pressable, {
              key: String(itemId(part)),
              style: [styles.compatPartRow, active && styles.compatPartRowActive],
              onPress: () => setSelectedPartId(String(itemId(part)))
            },
              el(ListRow, {
                title: partTitle(part),
                subtitle: `${partSubtitle(part)} / ${read(part, "usedCarId") ? `Donor #${read(part, "usedCarId")}` : t("compatibility.noDonor", "No donor link")}`,
                value: money(read(part, "salePrice"), read(part, "currency") || "USD")
              })
            );
          }),
          visibleParts.length === 0 && el(Text, { style: styles.emptyState }, t("compatibility.noParts", "No parts match this search."))
        )
      )
    ),
    el(Panel, { title: t("compatibility.modelsAndYears", "Models and years") },
      compatibility.modelGroups.map((group) =>
        el(ListRow, {
          key: group.model,
          title: group.model,
          subtitle: `${group.vehicles} vehicle record(s) / ${group.parts.length} proof part(s)`,
          value: compactYearList(group.years)
        })
      ),
      compatibility.modelGroups.length === 0 && el(Text, { style: styles.emptyState }, t("compatibility.noModels", "No compatible model/year records yet."))
    ),
    el(Panel, { title: t("compatibility.evidence", "Evidence parts") },
      compatibility.proofParts.map((match) => {
        const car = carsById.get(String(read(match.part, "usedCarId") || ""));
        return el(Pressable, { key: String(itemId(match.part)), onPress: () => setSelectedPartId(String(itemId(match.part))) },
          el(ListRow, {
            title: partTitle(match.part),
            subtitle: `${partSubtitle(match.part)} / ${car ? carTitle(car) : t("compatibility.noVehicle", "No vehicle")}`,
            value: match.reason
          })
        );
      }),
      compatibility.proofParts.length === 0 && el(Text, { style: styles.emptyState }, t("compatibility.noEvidence", "No related parts with vehicle links yet."))
    )
  );
}

module.exports = { PartCompatibilityScreen };
