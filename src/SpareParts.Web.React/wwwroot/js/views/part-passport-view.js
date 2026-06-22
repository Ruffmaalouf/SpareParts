import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { BrandMark } from "../components/layout.js";
import { Badge } from "../components/ui/Badge.js";
import { ImageGallery } from "../components/ui/ImageGallery.js";

const photoAsset = "/assets/passport-bmw-f30-headlight.png";
const defaultPart = {
  code: "BMW-F30-LED-LH",
  condition: "Tested / Grade A",
  donor: "BMW 320i / 2015",
  fitment: 96,
  inspectedAt: "29 MAY 2026",
  name: "BMW F30 LED Headlight",
  oem: "63117314531",
  shelf: "A-14 / Bin 06"
};
const inspectionPhotos = [
  { key: "front", label: "Front lens inspection", className: "photo-front" },
  { key: "housing", label: "Housing and mounts", className: "photo-housing" },
  { key: "lens", label: "Lens detail", className: "photo-lens" }
];
const conditionLabels = { 1: "New", 2: "Used", 3: "Rebuilt" };

const iconPaths = {
  calendar: "M7 3v3m10-3v3M4 9h16M5 5h14v15H5V5Zm3 8h3m2 0h3m-8 4h3m2 0h3",
  car: "M4 15h2l2-5h8l2 5h2v4h-2a2 2 0 0 1-4 0h-4a2 2 0 0 1-4 0H4v-4Zm4-3-1 3h10l-1-3H8Z",
  check: "m7 12 3 3 7-7",
  copy: "M9 8h10v11H9V8Zm-4 8V5h10",
  package: "m12 3 8 4-8 4-8-4 8-4Zm-8 4v10l8 4 8-4V7m-8 4v10",
  reserve: "M7 8V6a5 5 0 0 1 10 0v2m-12 0h14l1 13H4L5 8Z",
  shield: "M12 3 19 6v5c0 4.7-2.8 8.1-7 10-4.2-1.9-7-5.3-7-10V6l7-3Z",
  tag: "M4 5h7l9 9-6 6-9-9V5Zm4 3h.01",
  whatsapp: "M12 4a7 7 0 0 1 5.8 10.9L19 19l-4.2-1.2A7 7 0 1 1 12 4Zm-2 4.2c.3 2 1.7 3.5 3.8 4m-4.5-4.8.8-.4.9 1.6-.6.7m3.4 2.8.8-.6 1.5.9-.3.9"
};

function PassportIcon({ name, size = 20 }) {
  return h("svg", {
    className: "passport-icon",
    viewBox: "0 0 24 24",
    width: size,
    height: size,
    "aria-hidden": "true"
  }, h("path", { d: iconPaths[name] }));
}

function slugify(value) {
  return String(value || "part")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/(^-|-$)/g, "") || "part";
}

function passportCondition(value) {
  return conditionLabels[value] || String(value || "").trim() || "Catalog condition";
}

export function passportHref(part) {
  const params = new URLSearchParams();
  if (part?.name) params.set("name", part.name);
  if (part?.oemNumber) params.set("oem", part.oemNumber);
  if (part?.internalCode) params.set("code", part.internalCode);
  if (part?.condition) params.set("condition", `${passportCondition(part.condition)} / Bench checked`);
  if (part?.donorCar || part?.usedCarId) params.set("donor", part.donorCar || `Donor car #${part.usedCarId}`);
  else params.set("donor", "Warehouse stock");
  if (part?.storageLocation || part?.warehouseName) params.set("shelf", part.storageLocation || part.warehouseName);
  const query = params.toString();
  return `/passport/${slugify(part?.name)}${query ? `?${query}` : ""}`;
}

function MetaRow({ icon, label, value }) {
  return h("div", { className: "passport-meta-row" },
    h(PassportIcon, { name: icon }),
    h("span", null, label),
    h("strong", null, value)
  );
}

export function PartPassportView() {
  const params = useMemo(() => new URLSearchParams(window.location.search), []);
  const isCatalogPassport = useMemo(() => Array.from(params.keys()).length > 0, [params]);
  const [activePhoto, setActivePhoto] = useState(0);
  const [copyStatus, setCopyStatus] = useState("");
  const [reserved, setReserved] = useState(false);
  const part = useMemo(() => ({
    ...defaultPart,
    code: params.get("code") || defaultPart.code,
    condition: params.get("condition") || (isCatalogPassport ? "Catalog condition / Verify on dispatch" : defaultPart.condition),
    donor: params.get("donor") || (isCatalogPassport ? "Provenance available on request" : defaultPart.donor),
    fitment: params.has("fitment") ? Number(params.get("fitment")) : (isCatalogPassport ? null : defaultPart.fitment),
    inspectedAt: isCatalogPassport ? "INSPECTION REQUIRED" : defaultPart.inspectedAt,
    name: params.get("name") || defaultPart.name,
    oem: params.get("oem") || (isCatalogPassport ? "Not recorded" : defaultPart.oem),
    shelf: params.get("shelf") || (isCatalogPassport ? "Ask staff for shelf location" : defaultPart.shelf),
    verified: !isCatalogPassport
  }), [isCatalogPassport, params]);
  const whatsappHref = useMemo(() => {
    const text = `Part Passport: ${part.name} - OEM ${part.oem}\n${window.location.href}`;
    return `https://wa.me/?text=${encodeURIComponent(text)}`;
  }, [part.name, part.oem]);

  useEffect(() => {
    const previousTitle = document.title;
    document.title = `${part.name} | Part Passport`;
    return () => { document.title = previousTitle; };
  }, [part.name]);

  const copyPassportLink = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(window.location.href);
      setCopyStatus("Passport link copied. Ready for WhatsApp.");
    } catch {
      setCopyStatus("Passport link ready to share.");
    }
  }, []);

  const reserve = useCallback(() => {
    setReserved(true);
    setCopyStatus("Reservation requested. We will confirm availability on WhatsApp.");
  }, []);
  const fitmentRows = part.fitment === null
    ? [
      ["SOURCE", part.donor],
      ["LOCATION", part.shelf],
      ["CONDITION", part.condition],
      ["OEM", part.oem]
    ]
    : [
      ["MAKE", "BMW"],
      ["MODEL", "3 Series"],
      ["CHASSIS", "F30, F31"],
      ["YEARS", "2012 - 2018"]
    ];

  return h("main", { className: "part-passport-page" },
    h("header", { className: "passport-header" },
      h("a", { className: "passport-brand", href: "/", "aria-label": "Maalouf German Parts home" },
        h(BrandMark, { size: "small", variant: "german", label: "Maalouf German Parts" }),
        h("strong", null, "MAALOUF GERMAN PARTS")
      ),
      h("div", { className: "passport-trust" },
        h(PassportIcon, { name: "shield", size: 26 }),
        h("div", null,
          h("strong", null, "TRUSTED GENUINE PARTS"),
          h("span", null, "TESTED. VERIFIED. READY.")
        )
      )
    ),
    h("div", { className: "passport-redline" }),
    h("div", { className: "passport-content" },
      h("section", { className: "passport-hero" },
        h("section", { className: "passport-media-column", "aria-label": "Inspection photography" },
          isCatalogPassport
            ? h("div", { className: "passport-main-photo passport-main-photo-placeholder" },
              h(PassportIcon, { name: "package", size: 42 }),
              h("strong", null, part.code),
              h("span", null, "INSPECTION MEDIA PENDING")
            )
            : h("figure", { className: "passport-main-photo" },
              h("img", {
                className: inspectionPhotos[activePhoto].className,
                src: photoAsset,
                alt: `${part.name} inspection view`
              }),
              h("figcaption", null, `${activePhoto + 1} / ${inspectionPhotos.length}`)
            ),
          h("div", { className: "passport-photo-heading" },
            h("strong", null, "INSPECTION PHOTOS"),
            h("span", null, isCatalogPassport ? "Attach before sharing" : "Select a verified view")
          ),
          isCatalogPassport
            ? h("div", { className: "passport-photo-rail" },
              inspectionPhotos.map((photo) =>
                h("div", { className: "passport-thumb passport-thumb-placeholder", key: photo.key },
                  h(PassportIcon, { name: "package", size: 22 })
                )
              )
            )
            : h(ImageGallery, {
              images: inspectionPhotos.map((photo) => ({ id: photo.key, url: photoAsset, alt: photo.label })),
              fallbackLabel: part.code
            })
        ),
        h("section", { className: "passport-detail-column" },
          h("span", { className: "passport-section-label" }, "PART PASSPORT"),
          h("h1", null, part.name),
          h("div", { className: "passport-meta" },
            h(MetaRow, { icon: "tag", label: "OEM", value: part.oem }),
            h(MetaRow, { icon: "car", label: "Donor car", value: part.donor }),
            h(MetaRow, { icon: "shield", label: "Condition", value: part.condition }),
            h(MetaRow, { icon: "package", label: "Shelf", value: part.shelf })
          ),
          h("section", { className: "passport-confidence", "aria-label": part.fitment === null ? "Fitment review required" : `${part.fitment}% fitment confidence` },
            h("div", { className: "passport-confidence-ring", style: { "--confidence": `${(part.fitment || 0) * 3.6}deg` } },
              h("strong", null, part.fitment === null ? "--" : `${part.fitment}%`)
            ),
            h("div", null,
              h("strong", null, part.fitment === null ? "FITMENT REVIEW REQUIRED" : "FITMENT CONFIDENCE"),
              h("span", null, part.fitment === null ? "Confirm chassis details on WhatsApp." : "High compatibility with your vehicle.")
            )
          ),
          h("div", { className: "passport-inspection-stamp" },
            h(PassportIcon, { name: "check", size: 26 }),
            h("div", null,
              h("strong", null, part.verified ? "INSPECTED" : "DRAFT PASSPORT"),
              h("span", null, part.inspectedAt)
            ),
            h(Badge, { tone: part.verified ? "success" : "warning", size: "sm" }, part.verified ? "Verified" : "Pending")
          ),
          h("div", { className: "passport-actions" },
            h("button", {
              className: reserved ? "passport-primary reserved" : "passport-primary",
              type: "button",
              onClick: reserve,
              disabled: reserved
            },
              h(PassportIcon, { name: reserved ? "check" : "reserve" }),
              reserved ? "RESERVATION REQUESTED" : "RESERVE THIS PART"
            ),
            h("a", { className: "passport-secondary", href: whatsappHref, target: "_blank", rel: "noreferrer" },
              h(PassportIcon, { name: "whatsapp" }),
              "ASK ON WHATSAPP"
            ),
            h("button", { className: "passport-link-button", type: "button", onClick: copyPassportLink },
              h(PassportIcon, { name: "copy", size: 18 }),
              "COPY PASSPORT LINK"
            )
          ),
          h("p", { className: copyStatus ? "passport-share-note active" : "passport-share-note", role: "status" },
            copyStatus || "Share this public trust card with anyone."
          )
        )
      ),
      h("section", { className: "passport-fitment" },
        h("div", { className: "passport-fitment-copy" },
          h("span", { className: "passport-section-label" }, "FITMENT CHECK"),
          h("h2", null, part.fitment === null ? "Confirm fitment before checkout." : "Built for the right chassis."),
          h("p", null, part.fitment === null
            ? "Send the chassis code or VIN on WhatsApp and staff will confirm the exact match before reserving."
            : "This inspected part is matched against OEM references and donor-car details before it reaches your shelf."),
          h("div", { className: "passport-fitment-grid" },
            fitmentRows.map(([label, value]) =>
              h("div", { key: label }, h("span", null, label), h("strong", null, value))
            )
          ),
          h("p", { className: "passport-fitment-note" }, part.fitment === null
            ? "Passport draft: complete the inspection photos and fitment confidence before sending this link."
            : "For vehicles with LED headlights (SA 5A2). Verify left or right fitment before ordering.")
        ),
        h("div", { className: "passport-fitment-art", "aria-hidden": "true" },
          h("span", null, part.fitment === null ? "OEM" : "F30"),
          h("strong", null, part.fitment === null ? "CHECK" : "LED"),
          h("small", null, part.fitment === null ? "VERIFY FIRST" : "OEM MATCH")
        )
      )
    )
  );
}
