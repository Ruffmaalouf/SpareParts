import { h, useCallback, useEffect, useMemo, useRef, useState } from "../core/react-runtime.js";
import { asRows, dateTime, money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";
import * as THREE from "https://unpkg.com/three@0.160.0/build/three.module.js";

const selectedCarStorageKey = "spareparts.car-twin.selected-car-id";
const eventTypes = ["Inspection", "MileageUpdate", "ConditionChange", "LocationMove", "Note"];
const eventTypeLabels = {
  Inspection: "Inspection",
  MileageUpdate: "Mileage update",
  ConditionChange: "Condition change",
  LocationMove: "Location move",
  Note: "Note"
};
const timelineDotByEventType = {
  Purchased: "info",
  Received: "info",
  PartListed: "muted",
  PartSold: "success",
  WholesaleSale: "success",
  Inspection: "warning",
  MileageUpdate: "muted",
  ConditionChange: "warning",
  LocationMove: "muted",
  Note: "muted"
};

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

function carTitle(car) {
  const title = read(car, "car") || `Used car #${read(car, "id")}`;
  const year = read(car, "modelYear");
  return year ? `${title} / ${year}` : title;
}

function DetailTile({ label, value }) {
  return h("div", { className: "passport-admin-detail" },
    h("span", null, label),
    h("strong", null, value || "Not recorded")
  );
}

function TimelineView({ events }) {
  if (!events.length) return h("p", { className: "empty-state" }, "No timeline events recorded yet.");
  return h("div", { className: "timeline" },
    events.map((event, index) => {
      const dotTone = timelineDotByEventType[read(event, "eventType")] || "muted";
      return h("div", { className: "timeline-item", key: `${read(event, "eventType")}-${index}` },
        h("div", { className: "timeline-node" },
          h("span", { className: `timeline-dot timeline-dot-${dotTone}` }),
          index < events.length - 1 && h("span", { className: "timeline-line" })
        ),
        h("div", { className: "timeline-content" },
          h("div", { className: "timeline-time" }, dateTime(read(event, "occurredAt"))),
          h("div", { className: "timeline-action" },
            read(event, "title"),
            read(event, "isManual") ? h("span", { className: "badge badge-neutral" }, " manual") : null
          ),
          read(event, "description") && h("div", { className: "timeline-detail" }, read(event, "description")),
          read(event, "amountBase") !== "" && h("div", { className: "timeline-detail" }, money(read(event, "amountBase")))
        )
      );
    })
  );
}

const pixelsPerSpinFrame = 18;

function imageSrc(image) {
  const data = read(image, "imageData");
  if (!data) return "";
  return `data:${read(image, "mimeType") || "image/jpeg"};base64,${data}`;
}

function SpinViewer({ images }) {
  const [frame, setFrame] = useState(0);
  const dragRef = useRef(null);

  useEffect(() => { setFrame(0); }, [images]);

  const stopDrag = useCallback(() => {
    dragRef.current = null;
    window.removeEventListener("mousemove", handleMoveRef.current);
    window.removeEventListener("mouseup", stopDragRef.current);
  }, []);
  const stopDragRef = useRef(stopDrag);
  stopDragRef.current = stopDrag;

  const handleMove = useCallback((event) => {
    if (!dragRef.current || images.length < 2) return;
    const deltaX = event.clientX - dragRef.current.startX;
    const frameDelta = Math.trunc(deltaX / pixelsPerSpinFrame);
    const next = ((dragRef.current.startFrame + frameDelta) % images.length + images.length) % images.length;
    setFrame(next);
  }, [images.length]);
  const handleMoveRef = useRef(handleMove);
  handleMoveRef.current = handleMove;

  const startDrag = useCallback((event) => {
    if (images.length < 2) return;
    dragRef.current = { startX: event.clientX, startFrame: frame };
    window.addEventListener("mousemove", handleMoveRef.current);
    window.addEventListener("mouseup", stopDragRef.current);
  }, [frame, images.length]);

  useEffect(() => () => {
    window.removeEventListener("mousemove", handleMoveRef.current);
    window.removeEventListener("mouseup", stopDragRef.current);
  }, []);

  if (images.length === 0) {
    return h("p", { className: "empty-state" }, "Upload photos for this vehicle to build a 360 degree walk-around.");
  }

  return h("div", { className: "spin-viewer" },
    h("img", {
      className: "spin-viewer-image",
      src: imageSrc(images[frame]),
      draggable: false,
      onMouseDown: startDrag,
      alt: "Vehicle walk-around"
    }),
    h("div", { className: "spin-viewer-bar" },
      h("span", null, images.length > 1 ? "Drag to rotate" : "Add more photos for a smoother spin"),
      h("b", null, `${frame + 1} / ${images.length}`)
    )
  );
}

function buildCarGroup() {
  const group = new THREE.Group();
  const bodyMaterial = new THREE.MeshStandardMaterial({ color: 0x3a4a63, metalness: 0.3, roughness: 0.6 });
  const glassMaterial = new THREE.MeshStandardMaterial({ color: 0x9fc6e0, metalness: 0.1, roughness: 0.2, transparent: true, opacity: 0.7 });
  const wheelMaterial = new THREE.MeshStandardMaterial({ color: 0x1a1a1a, roughness: 0.9 });

  const lowerBody = new THREE.Mesh(new THREE.BoxGeometry(4.4, 0.9, 1.8), bodyMaterial);
  lowerBody.position.set(0, 0.55, 0);
  group.add(lowerBody);

  const cabin = new THREE.Mesh(new THREE.BoxGeometry(2.4, 0.8, 1.6), glassMaterial);
  cabin.position.set(-0.2, 1.35, 0);
  group.add(cabin);

  const wheelGeometry = new THREE.CylinderGeometry(0.45, 0.45, 0.4, 20);
  const wheelOffsets = [
    [1.5, 0.45, 1],
    [1.5, 0.45, -1],
    [-1.5, 0.45, 1],
    [-1.5, 0.45, -1]
  ];
  wheelOffsets.forEach(([x, y, z]) => {
    const wheel = new THREE.Mesh(wheelGeometry, wheelMaterial);
    wheel.rotation.x = Math.PI / 2;
    wheel.position.set(x, y, z);
    group.add(wheel);
  });

  return group;
}

function partMarkerColor(part) {
  return read(part, "isSold") ? 0x35c46a : 0xe0a830;
}

function Model3DViewer({ parts }) {
  const containerRef = useRef(null);
  const dragRef = useRef(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const width = container.clientWidth || 320;
    const height = 260;
    const scene = new THREE.Scene();
    const camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 100);
    camera.position.set(4.5, 3, 5.5);
    camera.lookAt(0, 0.8, 0);

    const renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(width, height);
    container.appendChild(renderer.domElement);

    scene.add(new THREE.AmbientLight(0xffffff, 0.7));
    const directional = new THREE.DirectionalLight(0xffffff, 0.8);
    directional.position.set(3, 6, 4);
    scene.add(directional);

    const carGroup = buildCarGroup();
    scene.add(carGroup);

    const markerPositions = [
      [1.4, 1.7, 0.9], [1.4, 1.7, -0.9], [-1.4, 1.7, 0.9], [-1.4, 1.7, -0.9],
      [0.4, 1.9, 1.0], [0.4, 1.9, -1.0], [-0.6, 1.9, 1.0], [-0.6, 1.9, -1.0]
    ];
    const markerGeometry = new THREE.SphereGeometry(0.14, 12, 12);
    parts.slice(0, markerPositions.length).forEach((part, index) => {
      const marker = new THREE.Mesh(markerGeometry, new THREE.MeshStandardMaterial({ color: partMarkerColor(part) }));
      marker.position.set(...markerPositions[index]);
      carGroup.add(marker);
    });

    let autoRotate = true;
    let frameId = null;
    const animate = () => {
      if (autoRotate) carGroup.rotation.y += 0.006;
      renderer.render(scene, camera);
      frameId = requestAnimationFrame(animate);
    };
    animate();

    const onPointerDown = (event) => {
      autoRotate = false;
      dragRef.current = { startX: event.clientX, startRotation: carGroup.rotation.y };
    };
    const onPointerMove = (event) => {
      if (!dragRef.current) return;
      const deltaX = event.clientX - dragRef.current.startX;
      carGroup.rotation.y = dragRef.current.startRotation + deltaX * 0.01;
    };
    const onPointerUp = () => { dragRef.current = null; };

    renderer.domElement.addEventListener("pointerdown", onPointerDown);
    window.addEventListener("pointermove", onPointerMove);
    window.addEventListener("pointerup", onPointerUp);

    return () => {
      cancelAnimationFrame(frameId);
      renderer.domElement.removeEventListener("pointerdown", onPointerDown);
      window.removeEventListener("pointermove", onPointerMove);
      window.removeEventListener("pointerup", onPointerUp);
      renderer.dispose();
      container.removeChild(renderer.domElement);
    };
  }, [parts]);

  return h("div", { className: "model3d-viewer" },
    h("div", { className: "model3d-canvas", ref: containerRef }),
    h("div", { className: "spin-viewer-bar" },
      h("span", null, "Drag to rotate - generic body, markers show removed parts"),
      h("span", null,
        h("i", { className: "model3d-legend-dot model3d-legend-sold" }), " Sold  ",
        h("i", { className: "model3d-legend-dot model3d-legend-remaining" }), " Remaining"
      )
    )
  );
}

export function CarTwinWorkspaceView({ api }) {
  const [usedCars, setUsedCars] = useState([]);
  const [filter, setFilter] = useState("");
  const [selectedCarId, setSelectedCarId] = useState(() => window.sessionStorage.getItem(selectedCarStorageKey) || "");
  const [twin, setTwin] = useState(null);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isTwinLoading, setIsTwinLoading] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [eventForm, setEventForm] = useState({ eventType: "Note", mileage: "", condition: "", location: "", note: "" });
  const [spinImages, setSpinImages] = useState([]);

  const loadCars = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading vehicle digital twin workspace...");
    try {
      const nextCars = asRows(await api.list("/api/usedcars"));
      setUsedCars(nextCars);
      setSelectedCarId((current) => {
        if (nextCars.some((car) => String(read(car, "id")) === String(current))) return String(current);
        const bestId = nextCars[0] ? String(read(nextCars[0], "id")) : "";
        if (bestId) window.sessionStorage.setItem(selectedCarStorageKey, bestId);
        return bestId;
      });
      setStatus("Choose a vehicle to inspect its digital twin.");
    } catch (error) {
      setUsedCars([]);
      setStatus(error.message || "Could not load the vehicle list.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => { loadCars(); }, [loadCars]);

  const loadTwin = useCallback(async () => {
    if (!selectedCarId) {
      setTwin(null);
      return;
    }
    setIsTwinLoading(true);
    try {
      setTwin(await api.get(`/api/usedcars/${selectedCarId}/twin`));
    } catch (error) {
      setTwin(null);
      setStatus(error.message || "Could not load this vehicle's digital twin.");
    } finally {
      setIsTwinLoading(false);
    }
  }, [api, selectedCarId]);

  useEffect(() => { loadTwin(); }, [loadTwin]);

  useEffect(() => {
    if (!selectedCarId) {
      setSpinImages([]);
      return;
    }
    let cancelled = false;
    api.get(`/api/usedcars/${selectedCarId}/images`)
      .then((result) => {
        if (cancelled) return;
        const rows = asRows(result);
        const uploadOrder = [...rows].sort((a, b) => new Date(read(a, "createdAt")) - new Date(read(b, "createdAt")));
        setSpinImages(uploadOrder);
      })
      .catch(() => { if (!cancelled) setSpinImages([]); });
    return () => { cancelled = true; };
  }, [api, selectedCarId]);

  const visibleCars = useMemo(() => {
    const query = filter.trim().toLowerCase();
    if (!query) return usedCars;
    return usedCars.filter((car) =>
      [read(car, "car"), read(car, "barcode"), read(car, "location")]
        .some((value) => String(value || "").toLowerCase().includes(query))
    );
  }, [filter, usedCars]);
  const displayedCars = useMemo(() => visibleCars.slice(0, 120), [visibleCars]);

  const chooseCar = useCallback((car) => {
    const id = String(read(car, "id"));
    setSelectedCarId(id);
    window.sessionStorage.setItem(selectedCarStorageKey, id);
    setStatus(`${carTitle(car)} selected.`);
  }, []);

  const updateEventField = useCallback((field, value) => {
    setEventForm((current) => ({ ...current, [field]: value }));
  }, []);

  const submitEvent = useCallback(async (e) => {
    e.preventDefault();
    if (!selectedCarId) return;
    setIsSubmitting(true);
    setStatus("Recording state event...");
    try {
      await api.post(`/api/usedcars/${selectedCarId}/state-events`, {
        eventType: eventForm.eventType,
        mileage: eventForm.mileage === "" ? null : Number(eventForm.mileage),
        condition: eventForm.condition || null,
        location: eventForm.location || null,
        note: eventForm.note || null
      });
      setEventForm({ eventType: "Note", mileage: "", condition: "", location: "", note: "" });
      setStatus("State event recorded.");
      await loadTwin();
    } catch (error) {
      setStatus(error.message || "Could not record the state event.");
    } finally {
      setIsSubmitting(false);
    }
  }, [api, eventForm, loadTwin, selectedCarId]);

  const car = twin?.car || null;
  const parts = asRows(twin?.parts);
  const timeline = asRows(twin?.timeline);
  const wholesaleSale = twin?.wholesaleSale || null;

  return h("section", { className: "screen passport-admin-screen" },
    h(PageHeader, {
      kicker: "Vehicle digital twin",
      title: "Digital Twin",
      subtitle: "Track a vehicle's real-world condition, installed parts, and history as one living record.",
      action: h("button", { className: "secondary-button", type: "button", onClick: loadCars, disabled: isLoading },
        isLoading ? "Loading" : "Refresh"
      )
    }),
    h(StatusLine, { status }),
    h("section", { className: "passport-admin-layout" },
      h("aside", { className: "passport-admin-list-panel" },
        h("div", { className: "passport-admin-panel-heading" },
          h("div", null,
            h("span", null, "Inventory"),
            h("h3", null, "Choose a vehicle")
          ),
          h("b", null, visibleCars.length)
        ),
        h("input", {
          value: filter,
          onChange: (event) => setFilter(event.target.value),
          placeholder: "Search vehicle, barcode, location"
        }),
        h("div", { className: "passport-admin-part-list" },
          displayedCars.map((carRow) => {
            const id = String(read(carRow, "id"));
            const isActive = id === String(selectedCarId);
            return h("button", {
              className: isActive ? "passport-admin-part active" : "passport-admin-part",
              key: id,
              type: "button",
              onClick: () => chooseCar(carRow)
            },
              h("span", null, read(carRow, "barcode") || `#${id}`),
              h("strong", null, carTitle(carRow)),
              h("small", null, read(carRow, "isWholesaleSold") ? "Sold wholesale" : (read(carRow, "location") || "Location not recorded"))
            );
          }),
          visibleCars.length > displayedCars.length && h("p", { className: "passport-admin-list-note" },
            `Showing the first ${displayedCars.length} vehicles. Refine your search to find a specific twin.`
          ),
          visibleCars.length === 0 && h("p", { className: "empty-state" }, "No vehicles match this search.")
        )
      ),
      h("section", { className: "passport-admin-preview-panel" },
        isTwinLoading
          ? h("p", { className: "empty-state" }, "Loading digital twin...")
          : car
            ? h("div", { className: "passport-admin-preview" },
              h("header", { className: "passport-admin-preview-heading" },
                h("div", null,
                  h("span", null, "Digital twin"),
                  h("h3", null, carTitle(car))
                ),
                h("b", null, wholesaleSale ? "Sold" : (car.isReceived ? "In stock" : "In transit"))
              ),
              h(SpinViewer, { images: spinImages }),
              h(Model3DViewer, { parts }),
              h("div", { className: "passport-admin-proof-grid" },
                h(DetailTile, { label: "Purchase price", value: money(car.priceBase) }),
                h(DetailTile, { label: "Location", value: car.location }),
                h(DetailTile, { label: "Images on file", value: String(twin.imageCount ?? 0) }),
                h(DetailTile, { label: "Parts removed", value: String(parts.length) })
              ),
              wholesaleSale && h("div", { className: "passport-admin-proof-grid" },
                h(DetailTile, { label: "Wholesale buyer", value: wholesaleSale.buyerName }),
                h(DetailTile, { label: "Sale date", value: dateTime(wholesaleSale.saleDate) }),
                h(DetailTile, { label: "Sale price", value: money(wholesaleSale.salePriceBase) })
              ),
              h("section", { className: "passport-admin-readiness" },
                h("strong", null, "Parts on this vehicle"),
                parts.length === 0
                  ? h("span", null, "No parts have been listed from this vehicle yet.")
                  : h("div", { className: "passport-admin-part-list" },
                    parts.map((part) => h("div", { className: "passport-admin-part", key: part.partId },
                      h("span", null, part.internalCode),
                      h("strong", null, part.name),
                      h("small", null, part.isSold ? `Sold for ${money(part.soldAmountBase)}` : `${part.remainingQuantity} remaining - ${part.condition}`)
                    ))
                  )
              ),
              h("section", { className: "passport-admin-readiness" },
                h("strong", null, "Timeline"),
                h(TimelineView, { events: timeline })
              ),
              h("form", { className: "passport-admin-readiness", onSubmit: submitEvent },
                h("strong", null, "Record a state event"),
                h("label", { className: "passport-admin-link-field" },
                  "Event type",
                  h("select", {
                    value: eventForm.eventType,
                    onChange: (e) => updateEventField("eventType", e.target.value)
                  }, eventTypes.map((type) => h("option", { key: type, value: type }, eventTypeLabels[type])))
                ),
                h("label", { className: "passport-admin-link-field" },
                  "Mileage (km)",
                  h("input", {
                    type: "number",
                    min: "0",
                    value: eventForm.mileage,
                    onChange: (e) => updateEventField("mileage", e.target.value)
                  })
                ),
                h("label", { className: "passport-admin-link-field" },
                  "Condition",
                  h("input", {
                    value: eventForm.condition,
                    onChange: (e) => updateEventField("condition", e.target.value)
                  })
                ),
                h("label", { className: "passport-admin-link-field" },
                  "Location",
                  h("input", {
                    value: eventForm.location,
                    onChange: (e) => updateEventField("location", e.target.value)
                  })
                ),
                h("label", { className: "passport-admin-link-field" },
                  "Note",
                  h("input", {
                    value: eventForm.note,
                    onChange: (e) => updateEventField("note", e.target.value)
                  })
                ),
                h("div", { className: "passport-admin-actions" },
                  h("button", { className: "primary-button", type: "submit", disabled: isSubmitting },
                    isSubmitting ? "Saving..." : "Add event"
                  )
                )
              )
            )
            : h("p", { className: "empty-state" }, "Choose a vehicle to view its digital twin.")
      )
    )
  );
}
