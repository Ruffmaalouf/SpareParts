import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { initials, money } from "../core/formatters.js";
import { LoginPanel } from "../components/auth.js";
import { LanguagePicker, ThemePicker } from "../components/layout.js";
import { StatusLine } from "../components/shared.js";
import { passportHref } from "./part-passport-view.js";

const headlightAsset = "/assets/part-headlight.png";

const marqueItems = [
  { label: "All", value: "" },
  { label: "BMW", value: "BMW" },
  { label: "Mercedes-Benz", value: "Mercedes" },
  { label: "Audi", value: "Audi" },
  { label: "Porsche", value: "Porsche" },
  { label: "Volkswagen", value: "Volkswagen" }
];

const tickerWords = [
  "Genuine & OEM", "Bench-checked parts", "48h regional shipping",
  "Part passport on every listing", "Areeba & Whish checkout"
];

const paymentGatewayOptions = [
  { value: "Areeba Gateway", title: "Areeba", subtitle: "Secure card payment gateway" },
  { value: "Whish Gateway", title: "Whish", subtitle: "Wallet payment gateway" }
];

const navItems = [
  { key: "shop", label: "Shop", kind: "view" },
  { key: "request", label: "Find a Part", kind: "view" },
  { key: "passport", label: "Passport", kind: "section" },
  { key: "trust", label: "Why Maalouf", kind: "section" }
];

// ── data helpers ──────────────────────────────────────────────────────────
function partVisualLabel(part) {
  return String(part.internalCode || part.oemNumber || part.name || "DE").slice(0, 3).toUpperCase();
}

function partFitment(part, t) {
  return part.donorCar || part.oemNumber || part.barcode || part.internalCode || t("store.oemFitment", "OEM-grade fitment");
}

function partTag(part) {
  return part.condition || part.warehouseName || "Genuine";
}

function isBenchChecked(part) {
  return Boolean(part.usedCarId || (part.donorCar && String(part.donorCar).trim()));
}

function partAvailability(part) {
  const quantity = Number(part.availableQuantity ?? part.stockQuantity ?? 0);
  if (quantity > 12) return "In stock";
  if (quantity > 0) return `${quantity} left`;
  return "Check availability";
}

function looksLikeHeadlight(part) {
  const text = `${part?.name || ""} ${part?.oemNumber || ""} ${part?.internalCode || ""}`.toLowerCase();
  return text.includes("headlight") || text.includes("xenon") || text.includes("6311733");
}

function firstPartImage(part) {
  if (part?.imageUrl) return part.imageUrl;
  const raw = part?.imageUrls;
  if (!raw) return "";
  if (Array.isArray(raw)) return raw.find(Boolean) || "";
  try {
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed)) return parsed.find(Boolean) || "";
  } catch {
    // Older imports may store comma/newline separated URLs.
  }
  return String(raw).split(/[\n,]+/).map((url) => url.trim()).filter(Boolean)[0] || "";
}

function pickFeatured(parts) {
  return parts.find(looksLikeHeadlight) || parts[0] || null;
}

// ── inline icons ──────────────────────────────────────────────────────────
function svg(props, ...children) {
  return h("svg", {
    width: props.size || 18,
    height: props.size || 18,
    viewBox: "0 0 24 24",
    fill: props.fill || "none",
    stroke: props.stroke || "currentColor",
    strokeWidth: props.strokeWidth || 1.9,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    "aria-hidden": "true"
  }, ...children);
}
const CartIcon = (size) => svg({ size },
  h("circle", { cx: 9, cy: 20, r: 1.4 }),
  h("circle", { cx: 18, cy: 20, r: 1.4 }),
  h("path", { d: "M2 3h2.2l2.3 12.4a1.6 1.6 0 0 0 1.6 1.3h8.8a1.6 1.6 0 0 0 1.6-1.2L21 7H5.4" })
);
const UserIcon = (size) => svg({ size },
  h("circle", { cx: 12, cy: 8, r: 3.6 }),
  h("path", { d: "M4.5 20a7.5 7.5 0 0 1 15 0" })
);
const CloseIcon = () => svg({ size: 16, strokeWidth: 2 }, h("path", { d: "M5 5l14 14M19 5L5 19" }));
const trustIcons = {
  bench: svg({ size: 24, strokeWidth: 1.85 }, h("path", { d: "M12 3l7 3v5c0 4.4-3 8-7 10-4-2-7-5.6-7-10V6z" }), h("path", { d: "M9 12l2 2 4-4" })),
  oem: svg({ size: 24, strokeWidth: 1.85 }, h("path", { d: "M12 2l2.4 4.9 5.4.8-3.9 3.8.9 5.4L12 19.3 7.2 16.9l.9-5.4L4.2 7.7l5.4-.8z" })),
  ship: svg({ size: 24, strokeWidth: 1.85 }, h("path", { d: "M3 7h11v8H3z" }), h("path", { d: "M14 10h4l3 3v2h-7z" }), h("circle", { cx: 7, cy: 17, r: 1.6 }), h("circle", { cx: 17, cy: 17, r: 1.6 }))
};

function ApexLogo({ small }) {
  return h("span", { className: small ? "apx-logo apx-logo-sm" : "apx-logo", "aria-hidden": "true" }, "M");
}

function BrandLockup({ small, onClick }) {
  const inner = [
    h(ApexLogo, { small, key: "logo" }),
    h("span", { className: "apx-brand-text", key: "text" },
      h("strong", null, "MAALOUF"),
      h("span", null, "German Parts")
    )
  ];
  return onClick
    ? h("button", { className: "apx-brand", type: "button", onClick }, inner)
    : h("span", { className: "apx-brand" }, inner);
}

// ── presentational pieces ─────────────────────────────────────────────────
function Ticker() {
  const group = (key) => h("span", { className: "apx-ticker-group", key },
    tickerWords.flatMap((word, index) => [
      h("span", { key: `${word}` }, word),
      h("span", { key: `dot-${index}` }, "·")
    ])
  );
  return h("div", { className: "apx-ticker", "aria-hidden": "true" },
    h("div", { className: "apx-ticker-track" }, group("a"), group("b"))
  );
}

function PartCard({ part, inCart, addToCart, t }) {
  const imageUrl = firstPartImage(part);
  const showImg = looksLikeHeadlight(part);
  return h("article", { className: "apx-card" },
    h("div", { className: "apx-card-media" },
      h("span", { className: "apx-card-tag" }, partTag(part)),
      isBenchChecked(part) && h("span", { className: "apx-card-bench" }, "Bench"),
      imageUrl || showImg
        ? h("img", { src: imageUrl || headlightAsset, alt: part.name })
        : h("span", { className: "apx-card-ph" }, partVisualLabel(part))
    ),
    h("div", { className: "apx-card-body" },
      h("span", { className: "apx-card-code" }, part.internalCode || part.oemNumber || "German Part"),
      h("h3", { className: "apx-card-title" }, part.name),
      h("p", { className: "apx-card-fit" }, partFitment(part, t)),
      h("div", { className: "apx-card-meta" },
        h("strong", { className: "apx-card-price" }, money(part.salePrice, part.currency)),
        h("span", { className: "apx-card-stock" }, partAvailability(part))
      )
    ),
    h("div", { className: "apx-card-actions" },
      h("a", { className: "apx-btn apx-btn-ghost", href: passportHref(part) }, "Passport"),
      h("button", { className: "apx-btn apx-btn-primary", type: "button", onClick: () => addToCart(part) },
        inCart > 0 ? t("store.addAnother", "Add another") : t("store.addToCart", "Add to cart")
      )
    )
  );
}

function Stepper({ partId, quantity, updateQuantity }) {
  return h("div", { className: "apx-stepper" },
    h("button", { type: "button", onClick: () => updateQuantity(partId, -1), "aria-label": "Decrease quantity" }, "-"),
    h("span", null, quantity),
    h("button", { type: "button", onClick: () => updateQuantity(partId, 1), "aria-label": "Increase quantity" }, "+")
  );
}

function CartRows({ cartRows, updateQuantity, t }) {
  if (cartRows.length === 0) {
    return h("p", { className: "apx-empty-note" }, t("store.emptyCart", "Your selected German parts will appear here."));
  }
  return h("div", { className: "apx-cart-rows" },
    cartRows.map((item) =>
      h("div", { className: "apx-cart-row", key: item.partId },
        h("div", { className: "apx-cart-row-copy" },
          h("strong", null, item.part?.name || "Part"),
          h("span", null, money(item.lineTotal, item.part?.currency || "USD"))
        ),
        h(Stepper, { partId: item.partId, quantity: item.quantity, updateQuantity })
      )
    )
  );
}

function OrderSummary({ cartRows, cartTotal, actions, t }) {
  const currency = cartRows[0]?.part?.currency || "USD";
  return h("aside", { className: "apx-panel apx-summary" },
    h("h2", null, t("store.currentOrder", "Current order")),
    cartRows.length === 0
      ? h("p", { className: "apx-empty-note" }, t("store.emptyCart", "Your selected German parts will appear here."))
      : h("div", { className: "apx-summary-lines" },
        cartRows.map((item) =>
          h("div", { className: "apx-summary-line", key: item.partId },
            h("span", null, `${item.quantity}× ${item.part?.name || "Part"}`),
            h("strong", null, money(item.lineTotal, item.part?.currency || currency))
          )
        )
      ),
    h("div", { className: "apx-summary-total" },
      h("span", null, t("store.total", "Total")),
      h("strong", null, money(cartTotal, currency))
    ),
    actions && actions.length > 0 && h("div", { className: "apx-summary-actions" },
      actions.map((action, index) =>
        h("button", {
          key: index,
          className: action.variant === "primary" ? "apx-btn apx-btn-primary" : "apx-btn apx-btn-ghost",
          type: "button",
          onClick: action.onClick,
          disabled: action.disabled,
          style: action.variant === "primary" ? { flex: 1 } : undefined
        }, action.label)
      )
    )
  );
}

export function CustomerStorefrontView({
  api,
  user,
  initialApiBaseUrl,
  themeKey,
  languageKey,
  onTheme,
  onLanguage,
  onLogin,
  onLogout,
  t
}) {
  const [activeView, setActiveView] = useState("shop");
  const [parts, setParts] = useState([]);
  const [cart, setCart] = useState([]);
  const [search, setSearch] = useState("");
  const [pendingScroll, setPendingScroll] = useState("");
  const [loginOpen, setLoginOpen] = useState(false);
  const [customerName, setCustomerName] = useState(user?.fullName || "");
  const [customerPhone, setCustomerPhone] = useState("");
  const [customerEmail, setCustomerEmail] = useState("");
  const [shippingAddressLine1, setShippingAddressLine1] = useState("");
  const [shippingAddressLine2, setShippingAddressLine2] = useState("");
  const [shippingCity, setShippingCity] = useState("");
  const [shippingRegion, setShippingRegion] = useState("");
  const [shippingPostalCode, setShippingPostalCode] = useState("");
  const [shippingCountry, setShippingCountry] = useState("Lebanon");
  const [deliveryInstructions, setDeliveryInstructions] = useState("");
  const [paymentMethod, setPaymentMethod] = useState(paymentGatewayOptions[0].value);
  const [paymentReference, setPaymentReference] = useState("");
  const [requestPartId, setRequestPartId] = useState("");
  const [requestPartName, setRequestPartName] = useState("");
  const [requestOemNumber, setRequestOemNumber] = useState("");
  const [requestVehicleDetails, setRequestVehicleDetails] = useState("");
  const [requestQuantity, setRequestQuantity] = useState("1");
  const [requestNotes, setRequestNotes] = useState("");
  const [requestMatches, setRequestMatches] = useState([]);
  const [requestImageName, setRequestImageName] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const isSignedIn = Boolean(user);

  useEffect(() => {
    if (user?.fullName && !customerName.trim()) {
      setCustomerName(user.fullName);
    }
  }, [customerName, user]);

  useEffect(() => {
    if (user) setLoginOpen(false);
  }, [user]);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("store.loadingParts", "Loading available parts..."));
    try {
      const query = search.trim() ? `&search=${encodeURIComponent(search.trim())}` : "";
      setParts(await api.get(`/api/web-catalog/parts?page=1&pageSize=160${query}`));
      setStatus(t("store.partsLoaded", "Available parts loaded."));
    } catch (error) {
      setStatus(error.message || t("store.partsLoadError", "Could not load parts."));
    } finally {
      setIsLoading(false);
    }
  }, [api, search, t]);

  useEffect(() => { load(); }, [load]);

  const cartRows = useMemo(() => cart.map((item) => {
    const part = parts.find((row) => row.id === item.partId) || item.part;
    return {
      ...item,
      part,
      lineTotal: (part?.salePrice || 0) * item.quantity
    };
  }), [cart, parts]);
  const cartTotal = useMemo(() => cartRows.reduce((sum, item) => sum + item.lineTotal, 0), [cartRows]);
  const cartItemCount = cartRows.reduce((sum, item) => sum + item.quantity, 0);
  const cartCurrency = cartRows[0]?.part?.currency || "USD";
  const availableCount = parts.reduce((count, part) => count + (Number(part.availableQuantity ?? part.stockQuantity ?? 0) > 0 ? 1 : 0), 0);
  const featuredPart = useMemo(() => pickFeatured(parts), [parts]);

  // scroll to top on view change (unless we are aiming at a section)
  useEffect(() => {
    if (pendingScroll) return;
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [activeView]); // eslint-disable-line react-hooks/exhaustive-deps

  // scroll to a section once the shop view has rendered
  useEffect(() => {
    if (!pendingScroll) return;
    const element = document.getElementById(pendingScroll);
    if (element) element.scrollIntoView({ behavior: "smooth", block: "start" });
    setPendingScroll("");
  }, [pendingScroll, parts]);

  const goShop = useCallback(() => setActiveView("shop"), []);
  const openLogin = useCallback(() => setLoginOpen(true), []);
  const closeLogin = useCallback(() => setLoginOpen(false), []);

  const goSection = useCallback((id) => {
    setActiveView("shop");
    setPendingScroll(id);
  }, []);

  const goCheckout = useCallback(() => {
    if (cartRows.length === 0) {
      setActiveView("shop");
      return;
    }
    if (!isSignedIn) {
      setStatus(t("store.signInToCheckout", "Sign in to checkout."));
      setLoginOpen(true);
      return;
    }
    setActiveView("checkout");
  }, [cartRows.length, isSignedIn, t]);

  const handleNav = useCallback((item) => {
    if (item.kind === "section") {
      goSection(`apx-${item.key}`);
      return;
    }
    setActiveView(item.key);
  }, [goSection]);

  const addToCart = useCallback((part) => {
    setCart((current) => {
      const existing = current.find((item) => item.partId === part.id);
      if (existing) {
        return current.map((item) =>
          item.partId === part.id
            ? { ...item, quantity: Math.min(item.quantity + 1, part.availableQuantity || item.quantity + 1), part }
            : item
        );
      }
      return [...current, { partId: part.id, quantity: 1, part }];
    });
    setStatus(t("store.addedToCart", "{name} added to cart.", { name: part.name }));
  }, [t]);

  const updateQuantity = useCallback((partId, delta) => {
    setCart((current) => current.flatMap((item) => {
      if (item.partId !== partId) return [item];
      const available = item.part?.availableQuantity || item.quantity + 1;
      const quantity = Math.min(Math.max(item.quantity + delta, 0), available);
      return quantity === 0 ? [] : [{ ...item, quantity }];
    }));
  }, []);

  const checkout = useCallback(async () => {
    if (!isSignedIn) {
      setStatus(t("store.signInToCheckout", "Sign in to checkout."));
      setLoginOpen(true);
      return;
    }
    if (cart.length === 0) {
      setStatus(t("store.addPartsFirst", "Add parts to the cart first."));
      setActiveView("shop");
      return;
    }
    if (!customerName.trim() || !customerPhone.trim()) {
      setStatus(t("store.enterNamePhone", "Enter your name and phone number."));
      return;
    }
    if (!shippingAddressLine1.trim() || !shippingCity.trim() || !shippingCountry.trim()) {
      setStatus(t("store.enterShipping", "Enter the shipping street, city, and country."));
      return;
    }

    setIsLoading(true);
    setStatus(t("store.preparingPayment", "Preparing secure payment order..."));
    try {
      const response = await api.post("/api/web-catalog/checkout", {
        customerName,
        customerPhone,
        customerEmail,
        shippingAddressLine1,
        shippingAddressLine2,
        shippingCity,
        shippingRegion,
        shippingPostalCode,
        shippingCountry,
        deliveryInstructions,
        paymentMethod,
        paymentReference,
        items: cart.map((item) => ({ partId: item.partId, quantity: item.quantity }))
      });
      setCart([]);
      await load();
      setStatus(t("store.orderCreated", "Order {invoice} created for {method}. Total {total}.", {
        invoice: response.invoiceNumber,
        method: paymentMethod,
        total: money(response.totalAmount)
      }));
      setActiveView("shop");
    } catch (error) {
      setStatus(error.message || t("store.checkoutFailed", "Checkout failed."));
    } finally {
      setIsLoading(false);
    }
  }, [
    api, cart, customerEmail, customerName, customerPhone, deliveryInstructions,
    isSignedIn, load, paymentMethod, paymentReference, shippingAddressLine1,
    shippingAddressLine2, shippingCity, shippingCountry, shippingPostalCode, shippingRegion, t
  ]);

  const runSearch = useCallback((value) => {
    if (value !== undefined) setSearch(value);
    setActiveView("shop");
  }, []);

  const selectMarque = useCallback((value) => {
    setSearch(value);
    setActiveView("shop");
  }, []);

  const findRequestMatches = useCallback(async (event) => {
    const image = event.target.files?.[0];
    if (!image) return;

    setRequestImageName(image.name);
    setIsLoading(true);
    setStatus(t("store.identifyingPart", "Reading the part photo..."));
    try {
      const formData = new FormData();
      formData.append("image", image);
      formData.append("hint", [requestPartName, requestOemNumber, requestVehicleDetails].filter(Boolean).join(" "));
      formData.append("limit", "8");
      const response = await api.postForm("/api/web-catalog/visual-search", formData);
      setRequestMatches(response.matches || []);
      setStatus(response.message || t("store.photoMatchesReady", "Photo matches are ready."));
    } catch (error) {
      setRequestMatches([]);
      setStatus(error.message || t("store.photoMatchFailed", "Could not read this photo."));
    } finally {
      setIsLoading(false);
    }
  }, [api, requestOemNumber, requestPartName, requestVehicleDetails, t]);

  const chooseRequestMatch = useCallback((match) => {
    setRequestPartId(String(match.partId || ""));
    setRequestPartName(match.partName || "");
    setRequestOemNumber(match.oemNumber || "");
    setStatus(t("store.partMatchSelected", "Catalog match selected. Add your contact details and send the request."));
  }, [t]);

  const submitPartRequest = useCallback(async () => {
    if (!isSignedIn) {
      setStatus(t("store.signInToRequest", "Sign in so we can track your part request."));
      setLoginOpen(true);
      return;
    }
    if (!customerName.trim() || !requestPartName.trim()) {
      setStatus(t("store.requestNamePartRequired", "Enter your name and the part you need."));
      return;
    }

    setIsLoading(true);
    setStatus(t("store.sendingPartRequest", "Sending your part request..."));
    try {
      const requestId = await api.post("/api/web-catalog/part-requests", {
        partId: requestPartId ? Number(requestPartId) : null,
        customerId: null,
        customerName: customerName.trim(),
        customerPhone: customerPhone.trim() || null,
        requestedPartName: requestPartName.trim(),
        requestedOemNumber: requestOemNumber.trim() || null,
        vehicleDetails: requestVehicleDetails.trim() || null,
        quantity: Math.max(1, Number(requestQuantity || 1)),
        notes: [
          "Reverse storefront request",
          requestImageName ? `Customer photo: ${requestImageName}` : "",
          requestNotes.trim()
        ].filter(Boolean).join("\n")
      });
      setRequestPartId("");
      setRequestPartName("");
      setRequestOemNumber("");
      setRequestVehicleDetails("");
      setRequestQuantity("1");
      setRequestNotes("");
      setRequestMatches([]);
      setRequestImageName("");
      setStatus(t("store.partRequestSent", "Request #{id} is in the workshop queue. We will follow up on WhatsApp.", { id: requestId }));
    } catch (error) {
      setStatus(error.message || t("store.partRequestFailed", "Could not send your part request."));
    } finally {
      setIsLoading(false);
    }
  }, [
    api, customerName, customerPhone, isSignedIn, requestImageName, requestNotes,
    requestOemNumber, requestPartId, requestPartName, requestQuantity, requestVehicleDetails, t
  ]);

  // ── header ────────────────────────────────────────────────────────────
  const renderHeader = () => h("header", { className: "apx-header" },
    h(BrandLockup, { onClick: goShop }),
    h("nav", { className: "apx-nav", "aria-label": "Store navigation" },
      navItems.map((item) =>
        h("button", {
          key: item.key,
          className: activeView === item.key ? "apx-nav-link active" : "apx-nav-link",
          type: "button",
          onClick: () => handleNav(item)
        }, item.label)
      )
    ),
    h("div", { className: "apx-header-actions" },
      isSignedIn
        ? h("button", { className: "apx-btn apx-btn-ghost apx-btn-login", type: "button", onClick: () => setActiveView("account"), "aria-label": t("store.account", "Account") },
          UserIcon(17), h("span", null, t("store.account", "Account")))
        : h("button", { className: "apx-btn apx-btn-ghost apx-btn-login", type: "button", onClick: openLogin },
          UserIcon(17), h("span", null, t("store.signIn", "Log in"))),
      h("button", { className: "apx-btn apx-btn-outline apx-cart-pill", type: "button", onClick: () => setActiveView("cart") },
        CartIcon(17),
        h("span", null, t("store.cart", "Cart")),
        h("b", null, String(cartItemCount)),
        h("span", { className: "apx-dot" }, "·"),
        h("span", { className: "apx-mono" }, money(cartTotal, cartCurrency))
      )
    )
  );

  // ── shop view ───────────────────────────────────────────────────────────
  const renderHero = () => h("section", { id: "apx-top", className: "apx-hero" },
    h("div", { className: "apx-hero-accent", "aria-hidden": "true" }),
    h("div", { className: "apx-hero-sweep", "aria-hidden": "true" }),
    h("div", { className: "apx-hero-copy" },
      h("span", { className: "apx-eyebrow" }, "Genuine & OEM performance parts"),
      h("h1", null, "Keep your ", h("span", { className: "apx-grad-text" }, "German build"), " in motion"),
      h("p", { className: "apx-hero-lead" }, "Search by OEM number or your vehicle. Every listing carries a part passport — fitment, condition and provenance you can verify before you buy."),
      h("div", { className: "apx-hero-ctas" },
        h("button", { className: "apx-btn apx-btn-primary apx-btn-lg", type: "button", onClick: () => goSection("apx-catalog") }, "Shop the catalog"),
        h("button", { className: "apx-btn apx-btn-outline apx-btn-lg", type: "button", onClick: () => setActiveView("request") }, "Find by vehicle")
      ),
      h("div", { className: "apx-hero-stats" },
        h("div", { className: "apx-stat" }, h("strong", null, availableCount ? `${availableCount.toLocaleString()}+` : "12,400+"), h("span", null, "Parts in stock")),
        h("div", { className: "apx-stat-div", "aria-hidden": "true" }),
        h("div", { className: "apx-stat accent" }, h("strong", null, "OEM"), h("span", null, "Verified numbers")),
        h("div", { className: "apx-stat-div", "aria-hidden": "true" }),
        h("div", { className: "apx-stat" }, h("strong", null, "48h"), h("span", null, "Regional shipping"))
      )
    ),
    featuredPart && h("div", { className: "apx-hero-card" },
      isBenchChecked(featuredPart) && h("div", { className: "apx-featured-badge" }, "Bench-checked"),
      h("div", { className: "apx-featured-media" },
        looksLikeHeadlight(featuredPart)
          ? h("img", { src: headlightAsset, alt: featuredPart.name })
          : h("span", { className: "apx-ph" }, partVisualLabel(featuredPart))
      ),
      h("div", { className: "apx-featured-body" },
        h("span", { className: "apx-featured-code" }, featuredPart.internalCode || featuredPart.oemNumber || "German Part"),
        h("h3", null, featuredPart.name),
        h("p", null, partFitment(featuredPart, t)),
        h("div", { className: "apx-featured-row" },
          h("strong", null, money(featuredPart.salePrice, featuredPart.currency)),
          h("button", { className: "apx-btn apx-btn-primary", type: "button", onClick: () => addToCart(featuredPart) }, "Add to cart")
        )
      )
    )
  );

  const renderMarques = () => h("section", { className: "apx-marques" },
    h("div", { className: "apx-marques-head" },
      h("span", null, "Shop by marque"),
      h("strong", null, "Filter to parts that fit")
    ),
    h("div", { className: "apx-marque-list" },
      marqueItems.map((marque) => {
        const on = marque.value
          ? marque.value.toLowerCase() === search.trim().toLowerCase()
          : search.trim() === "";
        return h("button", {
          key: marque.label,
          className: on ? "apx-marque on" : "apx-marque",
          type: "button",
          onClick: () => selectMarque(marque.value)
        }, marque.label);
      })
    )
  );

  const renderCatalog = () => h("section", { id: "apx-catalog", className: "apx-catalog" },
    h("div", { className: "apx-section-head" },
      h("div", null,
        h("span", { className: "apx-section-kicker" }, `${parts.length} parts in view`),
        h("h2", { className: "apx-section-title" }, search.trim() ? `${search.trim()} parts` : "All German parts"),
        h("div", { className: "apx-search" },
          h("input", {
            value: search,
            onChange: (event) => setSearch(event.target.value),
            onKeyDown: (event) => event.key === "Enter" && runSearch(),
            placeholder: "Search OEM code, marque, chassis, part name...",
            "aria-label": t("common.search", "Search")
          }),
          h("button", { className: "apx-btn apx-btn-primary", type: "button", onClick: () => runSearch(), disabled: isLoading },
            isLoading ? t("store.searching", "Searching") : t("common.search", "Search")
          )
        )
      ),
      h("span", { className: "apx-catalog-note" }, "Every listing carries a part passport — fitment, condition & provenance.")
    ),
    h(StatusLine, { status }),
    h("div", { className: "apx-grid" },
      parts.map((part) => {
        const inCart = cartRows.find((item) => item.partId === part.id)?.quantity || 0;
        return h(PartCard, { key: part.id, part, inCart, addToCart, t });
      }),
      parts.length === 0 && !isLoading && h("div", { className: "apx-empty" },
        h("strong", null, t("store.noPartsMatch", "No parts matched that search.")),
        h("span", null, t("store.trySearch", "Try a German marque, OEM number, internal code, or part name."))
      )
    )
  );

  const renderPassportShowcase = () => h("section", { id: "apx-passport", className: "apx-passport" },
    h("div", { className: "apx-passport-visual" },
      h("div", { className: "apx-passport-badge" }, "Part Passport"),
      h("img", { src: headlightAsset, alt: "Bi-Xenon headlight, bench-checked" })
    ),
    h("div", { className: "apx-passport-body" },
      h("span", { className: "apx-section-kicker" }, "Verify before you buy"),
      h("h2", { className: "apx-section-title" }, "Every part carries a passport"),
      h("p", null, "No guesswork. Fitment, condition and provenance are documented and bench-checked before a listing goes live."),
      h("dl", { className: "apx-passport-table" },
        h("div", { className: "apx-passport-row" }, h("dt", null, "OEM number"), h("dd", { className: "apx-mono" }, "63117338701")),
        h("div", { className: "apx-passport-row" }, h("dt", null, "Fitment"), h("dd", null, "BMW F30 Pre-LCI · Right · 2012–2015")),
        h("div", { className: "apx-passport-row" }, h("dt", null, "Condition"), h("dd", null, "Used · bench-tested · no lens fade")),
        h("div", { className: "apx-passport-row" }, h("dt", null, "Provenance"), h("dd", null, "Donor: F30 320i · half-cut #HC-2231")),
        h("div", { className: "apx-passport-row" }, h("dt", null, "Warranty"), h("dd", null, "14-day fitment guarantee"))
      )
    )
  );

  const renderTrust = () => h("section", { id: "apx-trust", className: "apx-trust" },
    h("div", { className: "apx-trust-grid" },
      [
        { icon: "bench", title: "Bench-checked", body: "Used parts are tested on the bench and graded for condition before they ever reach a listing." },
        { icon: "oem", title: "OEM verified", body: "Search by the exact OEM number. Cross-references and fitment ranges are confirmed against the catalog." },
        { icon: "ship", title: "48h shipping", body: "Regional dispatch within 48 hours, with Areeba card and Whish wallet checkout at the counter or online." }
      ].map((card) =>
        h("div", { className: "apx-trust-card", key: card.title },
          h("div", { className: "apx-trust-icon" }, trustIcons[card.icon]),
          h("h3", null, card.title),
          h("p", null, card.body)
        )
      )
    )
  );

  const renderShop = () => h("div", null,
    renderHero(),
    renderMarques(),
    renderCatalog(),
    renderPassportShowcase(),
    renderTrust()
  );

  // ── cart view ─────────────────────────────────────────────────────────
  const renderCart = () => h("section", { className: "apx-page" },
    h("div", { className: "apx-page-head" },
      h("span", { className: "apx-section-kicker" }, t("store.currentOrder", "Current order")),
      h("h1", null, t("store.cart", "Cart"))
    ),
    h("div", { className: "apx-checkout-layout" },
      h("div", { className: "apx-panel" },
        h("h2", null, t("store.itemCount", "{count} items", { count: cartItemCount })),
        h(CartRows, { cartRows, updateQuantity, t })
      ),
      h(OrderSummary, {
        cartRows, cartTotal,
        actions: [
          { label: t("store.continueShopping", "Continue shopping"), onClick: goShop },
          {
            label: isSignedIn ? t("store.checkout", "Checkout") : t("store.signInToOrder", "Sign in to checkout"),
            onClick: goCheckout,
            variant: "primary",
            disabled: cartRows.length === 0
          }
        ],
        t
      })
    )
  );

  // ── checkout view ───────────────────────────────────────────────────────
  const field = (label, input) => h("label", { className: "apx-field" }, h("span", null, label), input);
  const renderCheckout = () => h("section", { className: "apx-page" },
    h("div", { className: "apx-page-head" },
      h("span", { className: "apx-section-kicker" }, t("store.checkout", "Checkout")),
      h("h1", null, t("store.finishOrder", "Finish order"))
    ),
    h("div", { className: "apx-checkout-layout" },
      h("div", null,
        h("section", { className: "apx-panel" },
          h("h2", null, t("store.contact", "Contact")),
          h("div", { className: "apx-form-grid two" },
            field("Name", h("input", { value: customerName, onChange: (event) => setCustomerName(event.target.value), autoComplete: "name" })),
            field("Phone", h("input", { value: customerPhone, onChange: (event) => setCustomerPhone(event.target.value), autoComplete: "tel" }))
          ),
          h("div", { className: "apx-form-grid", style: { marginTop: "14px" } },
            field("Email", h("input", { value: customerEmail, onChange: (event) => setCustomerEmail(event.target.value), autoComplete: "email" }))
          )
        ),
        h("section", { className: "apx-panel" },
          h("h2", null, t("store.shippingAddress", "Shipping address")),
          h("div", { className: "apx-form-grid" },
            field("Address line 1", h("input", { value: shippingAddressLine1, onChange: (event) => setShippingAddressLine1(event.target.value), autoComplete: "shipping address-line1" })),
            field("Address line 2", h("input", { value: shippingAddressLine2, onChange: (event) => setShippingAddressLine2(event.target.value), autoComplete: "shipping address-line2" }))
          ),
          h("div", { className: "apx-form-grid two", style: { marginTop: "14px" } },
            field("City", h("input", { value: shippingCity, onChange: (event) => setShippingCity(event.target.value), autoComplete: "shipping address-level2" })),
            field("Region", h("input", { value: shippingRegion, onChange: (event) => setShippingRegion(event.target.value), autoComplete: "shipping address-level1" }))
          ),
          h("div", { className: "apx-form-grid two", style: { marginTop: "14px" } },
            field("Postal code", h("input", { value: shippingPostalCode, onChange: (event) => setShippingPostalCode(event.target.value), autoComplete: "shipping postal-code" })),
            field("Country", h("input", { value: shippingCountry, onChange: (event) => setShippingCountry(event.target.value), autoComplete: "shipping country-name" }))
          ),
          h("div", { className: "apx-form-grid", style: { marginTop: "14px" } },
            field("Delivery instructions", h("textarea", { value: deliveryInstructions, onChange: (event) => setDeliveryInstructions(event.target.value) }))
          )
        ),
        h("section", { className: "apx-panel" },
          h("h2", null, t("store.paymentGateway", "Payment gateway")),
          h("div", { className: "apx-pay-options" },
            paymentGatewayOptions.map((option) =>
              h("button", {
                key: option.value,
                className: option.value === paymentMethod ? "apx-pay-option active" : "apx-pay-option",
                type: "button",
                onClick: () => setPaymentMethod(option.value)
              },
                h("strong", null, option.title),
                h("span", null, option.subtitle)
              )
            )
          ),
          field(paymentMethod === "Whish Gateway" ? "Whish reference" : "Areeba reference",
            h("input", { value: paymentReference, onChange: (event) => setPaymentReference(event.target.value) })
          ),
          h("p", { className: "apx-pay-note" }, t("store.paymentNote", "Payment is confirmed through the selected gateway before fulfillment.")),
          h(StatusLine, { status }),
          h("button", {
            className: "apx-btn apx-btn-primary apx-btn-lg apx-btn-block",
            style: { marginTop: "14px" },
            type: "button",
            onClick: checkout,
            disabled: isLoading || cart.length === 0 || !isSignedIn
          }, t("store.payWith", "Pay with {method}", { method: paymentMethod.replace(" Gateway", "") }))
        )
      ),
      h(OrderSummary, {
        cartRows, cartTotal,
        actions: [{ label: t("store.continueShopping", "Continue shopping"), onClick: goShop }],
        t
      })
    )
  );

  // ── find a part (reverse storefront) ────────────────────────────────────
  const renderRequest = () => h("section", { className: "apx-page" },
    h("div", { className: "apx-page-head" },
      h("span", { className: "apx-section-kicker" }, "Reverse storefront"),
      h("h1", null, "Show us the part. We'll find it.")
    ),
    h("div", { className: "apx-request-layout" },
      h("article", { className: "apx-panel" },
        h("h2", null, "Part request"),
        h("p", { style: { margin: "0 0 16px", color: "var(--apx-muted)", fontSize: "14px", lineHeight: 1.55 } },
          "Upload a counter photo, OEM marking, or vehicle clue. The workshop receives a structured request and can reply on WhatsApp."),
        h("label", { className: "apx-photo-drop" },
          h("strong", null, requestImageName || "Upload a part photo"),
          h("span", null, "JPG, PNG, WebP, or HEIC. Add a clue first for better matching."),
          h("input", { type: "file", accept: "image/*", onChange: findRequestMatches })
        ),
        h("div", { className: "apx-form-grid two", style: { marginTop: "14px" } },
          field("Requested part", h("input", { value: requestPartName, onChange: (event) => setRequestPartName(event.target.value), placeholder: "Left headlight, turbo hose..." })),
          field("OEM number", h("input", { value: requestOemNumber, onChange: (event) => setRequestOemNumber(event.target.value), placeholder: "Optional marking or OEM code" }))
        ),
        h("div", { className: "apx-form-grid", style: { marginTop: "14px" } },
          field("Vehicle details", h("input", { value: requestVehicleDetails, onChange: (event) => setRequestVehicleDetails(event.target.value), placeholder: "2015 Mercedes W205 C200" }))
        ),
        h("div", { className: "apx-form-grid two", style: { marginTop: "14px" } },
          field("Your name", h("input", { value: customerName, onChange: (event) => setCustomerName(event.target.value) })),
          field("WhatsApp phone", h("input", { value: customerPhone, onChange: (event) => setCustomerPhone(event.target.value) }))
        ),
        h("div", { className: "apx-form-grid two", style: { marginTop: "14px" } },
          field("Quantity", h("input", { type: "number", min: "1", value: requestQuantity, onChange: (event) => setRequestQuantity(event.target.value) })),
          field("Notes", h("textarea", { value: requestNotes, onChange: (event) => setRequestNotes(event.target.value), placeholder: "Side, color, engine code..." }))
        ),
        h(StatusLine, { status }),
        h("button", { className: "apx-btn apx-btn-primary apx-btn-lg apx-btn-block", style: { marginTop: "14px" }, type: "button", disabled: isLoading, onClick: submitPartRequest },
          isSignedIn ? "Send workshop request" : "Sign in to send request"
        )
      ),
      h("aside", { className: "apx-panel" },
        h("span", { className: "apx-section-kicker" }, "Picture search"),
        h("h2", { style: { marginTop: "10px" } }, requestMatches.length ? "Possible matches" : "Your shortlist appears here"),
        h("p", { style: { margin: "0 0 16px", color: "var(--apx-muted)", fontSize: "14px", lineHeight: 1.55 } },
          "Choosing a match helps the workshop reserve the exact catalog item. Leave it unselected when you are unsure."),
        requestMatches.map((match) =>
          h("button", {
            className: String(match.partId) === requestPartId ? "apx-match active" : "apx-match",
            key: match.partId,
            type: "button",
            onClick: () => chooseRequestMatch(match)
          },
            h("strong", null, `${match.internalCode} / ${match.partName}`),
            h("span", null, `${match.availableQuantity} available · ${money(match.salePrice, match.currency)}`),
            h("small", null, match.matchReason || "Picture search match")
          )
        )
      )
    )
  );

  // ── account view ────────────────────────────────────────────────────────
  const renderAccount = () => h("section", { className: "apx-page" },
    h("div", { className: "apx-page-head" },
      h("span", { className: "apx-section-kicker" }, isSignedIn ? t("store.account", "Account") : t("store.guestDriver", "Guest driver")),
      h("h1", null, isSignedIn ? (user.fullName || t("store.driver", "Driver")) : t("store.signIn", "Sign in"))
    ),
    isSignedIn
      ? h("div", { className: "apx-account-grid" },
        h("article", { className: "apx-panel" },
          h("div", { className: "apx-account-card" },
            h("span", { className: "apx-avatar" }, initials(user.fullName)),
            h("div", null,
              h("small", null, `Role ID ${user.roleId ?? user.RoleId ?? 4}`),
              h("strong", null, user.fullName),
              h("span", null, user.email || user.username || "")
            )
          ),
          h("button", { className: "apx-btn apx-btn-danger apx-btn-block", type: "button", onClick: onLogout }, t("common.signOut", "Sign out"))
        ),
        h("article", { className: "apx-panel" }, h(ThemePicker, { value: themeKey, onChange: onTheme, t })),
        h("article", { className: "apx-panel" }, h(LanguagePicker, { value: languageKey, onChange: onLanguage, t }))
      )
      : h("div", { className: "apx-account-grid" },
        h("article", { className: "apx-panel" },
          h("p", { style: { margin: "0 0 16px", color: "var(--apx-muted)" } }, t("store.signInToCheckout", "Sign in to checkout.")),
          h("button", { className: "apx-btn apx-btn-primary apx-btn-lg apx-btn-block", type: "button", onClick: openLogin }, t("store.signIn", "Log in"))
        ),
        h("article", { className: "apx-panel" }, h(ThemePicker, { value: themeKey, onChange: onTheme, t })),
        h("article", { className: "apx-panel" }, h(LanguagePicker, { value: languageKey, onChange: onLanguage, t }))
      )
  );

  // ── login modal ─────────────────────────────────────────────────────────
  const renderLoginModal = () => h("div", { className: "apx-modal-overlay", onClick: closeLogin },
    h("div", { className: "apx-modal", onClick: (event) => event.stopPropagation() },
      h("div", { className: "apx-modal-visual" },
        h("div", { className: "apx-hero-accent", "aria-hidden": "true" }),
        h(BrandLockup, { small: true }),
        h("div", null,
          h("span", { className: "apx-eyebrow" }, "Trade & retail accounts"),
          h("h2", null, "Welcome back"),
          h("p", null, "Track orders, save your garage and check out faster with Areeba & Whish.")
        )
      ),
      h("div", { className: "apx-modal-form" },
        h("button", { className: "apx-modal-close", type: "button", onClick: closeLogin, "aria-label": "Close" }, CloseIcon()),
        h("h3", null, t("store.signIn", "Sign in")),
        h(LoginPanel, { initialApiBaseUrl, onLogin, t })
      )
    )
  );

  const panels = {
    shop: renderShop,
    cart: renderCart,
    checkout: renderCheckout,
    request: renderRequest,
    account: renderAccount
  };
  const renderActive = panels[activeView] || renderShop;
  const showCartBar = cartItemCount > 0 && activeView !== "cart" && activeView !== "checkout";

  return h("main", { className: "apex-store" },
    h(Ticker),
    renderHeader(),
    h("div", { className: "apx-content" }, renderActive()),
    h("footer", { className: "apx-footer" },
      h("div", { className: "apx-footer-grid" },
        h("div", { className: "apx-footer-brand" },
          h("div", { className: "apx-footer-brand-row" }, h(ApexLogo, { small: true }),
            h("div", { className: "apx-brand-text" }, h("strong", null, "MAALOUF"), h("span", null, "German Parts"))),
          h("p", null, "The right part, bench-checked & ready to ship. Genuine and OEM spares for German marques.")
        ),
        h("div", { className: "apx-footer-col" },
          h("strong", null, "Shop"),
          h("button", { className: "apx-footer-link", type: "button", onClick: goShop }, "By marque"),
          h("button", { className: "apx-footer-link", type: "button", onClick: () => goSection("apx-catalog") }, "By OEM number"),
          h("button", { className: "apx-footer-link", type: "button", onClick: () => setActiveView("request") }, "By vehicle")
        ),
        h("div", { className: "apx-footer-col" },
          h("strong", null, "Support"),
          h("button", { className: "apx-footer-link", type: "button", onClick: () => goSection("apx-passport") }, "Part passport"),
          h("button", { className: "apx-footer-link", type: "button", onClick: () => goSection("apx-trust") }, "Fitment guarantee"),
          h("button", { className: "apx-footer-link", type: "button", onClick: () => goSection("apx-trust") }, "Shipping")
        ),
        h("div", { className: "apx-footer-col" },
          h("strong", null, "Checkout"),
          h("div", { className: "apx-footer-pay" },
            h("div", null, h("b", null, "Areeba"), h("span", null, "Secure card")),
            h("div", null, h("b", null, "Whish"), h("span", null, "Wallet pay"))
          )
        )
      ),
      h("div", { className: "apx-footer-bottom" },
        h("span", null, "© 2026 Maalouf Auto Parts"),
        h("span", { className: "apx-mono" }, "Apex Storefront · German Parts")
      )
    ),
    showCartBar && h("div", { className: "apx-cartbar" },
      h("div", { className: "apx-cartbar-info" },
        h("span", { className: "apx-count" }, CartIcon(18), t("store.itemCount", "{count} items", { count: cartItemCount })),
        h("small", null, "Areeba & Whish at checkout")
      ),
      h("div", { className: "apx-cartbar-actions" },
        h("strong", null, money(cartTotal, cartCurrency)),
        h("button", { className: "apx-btn apx-btn-primary apx-btn-lg", type: "button", onClick: goCheckout }, `Checkout · ${money(cartTotal, cartCurrency)}`)
      )
    ),
    loginOpen && renderLoginModal()
  );
}
