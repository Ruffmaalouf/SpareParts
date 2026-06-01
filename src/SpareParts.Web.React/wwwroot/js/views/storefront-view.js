import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { initials, money } from "../core/formatters.js";
import { LoginPanel } from "../components/auth.js";
import { BrandMark, LanguagePicker, ThemePicker } from "../components/layout.js";
import { StatusLine } from "../components/shared.js";
import { passportHref } from "./part-passport-view.js";

const germanBrandFilters = ["BMW", "Mercedes", "Audi", "Porsche", "Volkswagen", "AMG"];
const paymentGatewayOptions = [
  { value: "Areeba Gateway", title: "Areeba", subtitle: "Secure card payment gateway" },
  { value: "Whish Gateway", title: "Whish", subtitle: "Wallet payment gateway" }
];
const storeNavItems = [
  { key: "shop", label: "Shop" },
  { key: "request", label: "Find a Part" },
  { key: "cart", label: "Cart" },
  { key: "checkout", label: "Checkout" },
  { key: "account", label: "Account" }
];
const petrolheadFilters = [
  { label: "Boost", query: "turbo", detail: "Turbo, intake, sensors" },
  { label: "Braking", query: "brake", detail: "Pads, discs, hydraulics" },
  { label: "Service", query: "filter", detail: "Oil, cabin, DSG kits" },
  { label: "Ignition", query: "coil", detail: "Coils, plugs, modules" }
];
const heroSpecs = ["OEM numbers", "German fitment", "Fast checkout"];

function partVisualLabel(part) {
  return String(part.internalCode || part.oemNumber || part.name || "DE").slice(0, 3).toUpperCase();
}

function partFitment(part, t) {
  return part.oemNumber || part.barcode || part.internalCode || t("store.oemFitment", "OEM-grade fitment");
}

function partAvailability(part) {
  const quantity = Number(part.availableQuantity ?? part.stockQuantity ?? 0);
  if (quantity > 12) return "In stock";
  if (quantity > 0) return `${quantity} left`;
  return "Check availability";
}

function CartRows({ cartRows, updateQuantity, t }) {
  return h("div", { className: "store-cart-list" },
    cartRows.map((item) =>
      h("div", { className: "store-cart-row", key: item.partId },
        h("div", { className: "store-cart-row-copy" },
          h("strong", null, item.part?.name || "Part"),
          h("span", null, money(item.lineTotal, item.part?.currency || "USD"))
        ),
        h("div", { className: "quantity-stepper store-quantity-stepper" },
          h("button", { type: "button", onClick: () => updateQuantity(item.partId, -1), "aria-label": "Decrease quantity" }, "-"),
          h("span", null, item.quantity),
          h("button", { type: "button", onClick: () => updateQuantity(item.partId, 1), "aria-label": "Increase quantity" }, "+")
        )
      )
    ),
    cartRows.length === 0 && h("p", { className: "store-empty-note" }, t("store.emptyCart", "Your selected German parts will appear here."))
  );
}

function CartPanel({ cartRows, cartItemCount, cartTotal, updateQuantity, onCheckout, onShop, checkoutLabel, t }) {
  const currency = cartRows[0]?.part?.currency || "USD";
  return h("aside", { className: "store-cart-rail" },
    h("div", { className: "store-cart-heading" },
      h("span", null, t("store.currentOrder", "Current order")),
      h("strong", null, t("store.itemCount", "{count} items", { count: cartItemCount }))
    ),
    h(CartRows, { cartRows, updateQuantity, t }),
    h("div", { className: "store-cart-total" },
      h("span", null, t("store.total", "Total")),
      h("strong", null, money(cartTotal, currency))
    ),
    h("div", { className: "store-cart-actions" },
      h("button", { className: "secondary-button", type: "button", onClick: onShop }, t("store.continueShopping", "Continue shopping")),
      h("button", { className: "primary-button", type: "button", onClick: onCheckout, disabled: cartRows.length === 0 }, checkoutLabel || t("store.checkout", "Checkout"))
    )
  );
}

function PartCard({ part, inCart, addToCart, t }) {
  return h("article", { className: "store-product-card" },
    h("div", { className: "store-product-media", "aria-hidden": "true" },
      h("span", null, partVisualLabel(part)),
      h("b", null, "Bench checked")
    ),
    h("div", { className: "store-product-copy" },
      h("span", { className: "store-product-code" }, part.internalCode || part.oemNumber || "German Part"),
      h("h3", null, part.name),
      h("p", null, partFitment(part, t))
    ),
    h("div", { className: "store-product-meta" },
      h("strong", null, money(part.salePrice, part.currency)),
      h("span", null, partAvailability(part))
    ),
    h("div", { className: "store-product-actions" },
      h("a", { className: "secondary-button store-passport-link", href: passportHref(part) }, "Passport"),
      h("button", { className: "primary-button", type: "button", onClick: () => addToCart(part) },
        inCart > 0 ? t("store.addAnother", "Add another") : t("store.addToCart", "Add to cart")
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
  const availableCount = parts.reduce((count, part) => count + (Number(part.availableQuantity ?? part.stockQuantity ?? 0) > 0 ? 1 : 0), 0);
  const featuredParts = parts.slice(0, 8);

  useEffect(() => {
    window.scrollTo({ top: 0, behavior: "smooth" });
  }, [activeView]);

  const goShop = useCallback(() => setActiveView("shop"), []);
  const promptSignInForCheckout = useCallback(() => {
    setStatus(t("store.signInToCheckout", "Sign in to checkout."));
    setActiveView("account");
  }, [t]);
  const goCheckout = useCallback(() => {
    if (cartRows.length === 0) {
      setActiveView("shop");
      return;
    }

    if (!isSignedIn) {
      promptSignInForCheckout();
      return;
    }

    setActiveView("checkout");
  }, [cartRows.length, isSignedIn, promptSignInForCheckout]);
  const goStoreView = useCallback((nextView) => {
    if (nextView === "checkout") {
      goCheckout();
      return;
    }

    setActiveView(nextView);
  }, [goCheckout]);

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
      promptSignInForCheckout();
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
    api,
    cart,
    customerEmail,
    customerName,
    customerPhone,
    deliveryInstructions,
    isSignedIn,
    load,
    paymentMethod,
    paymentReference,
    promptSignInForCheckout,
    shippingAddressLine1,
    shippingAddressLine2,
    shippingCity,
    shippingCountry,
    shippingPostalCode,
    shippingRegion,
    t
  ]);

  const runSearch = useCallback(() => {
    setActiveView("shop");
    load();
  }, [load]);

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
      setActiveView("account");
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
    api,
    customerName,
    customerPhone,
    isSignedIn,
    requestImageName,
    requestNotes,
    requestOemNumber,
    requestPartId,
    requestPartName,
    requestQuantity,
    requestVehicleDetails,
    t
  ]);

  const renderShop = () => h("section", { className: "store-shop-view" },
    h("section", { className: "store-hero-redesign" },
      h("div", { className: "store-hero-copy" },
        h("h1", null, "Maalouf German Parts for petrolheads"),
        h("p", null, "OEM-grade German parts for the driver who knows the sound, the chassis code, and the part number before the dashboard light does."),
        h("div", { className: "store-hero-search" },
          h("input", {
            value: search,
            onChange: (event) => setSearch(event.target.value),
            onKeyDown: (event) => event.key === "Enter" && runSearch(),
            placeholder: "Search N54 coil, W205 pads, DSG kit, OEM code..."
          }),
          h("button", { className: "primary-button", type: "button", onClick: runSearch, disabled: isLoading },
            isLoading ? t("store.searching", "Searching") : t("common.search", "Search")
          )
        ),
        h("div", { className: "store-hero-specs", "aria-label": "Store capabilities" },
          heroSpecs.map((spec) => h("span", { key: spec }, spec))
        ),
        h("button", { className: "secondary-button store-find-part-cta", type: "button", onClick: () => setActiveView("request") },
          "Can't find it? Send a photo"
        ),
        h("div", { className: "store-brand-filters" },
          germanBrandFilters.map((brand) =>
            h("button", {
              key: brand,
              type: "button",
              onClick: () => {
                setSearch(brand);
                setActiveView("shop");
              }
            }, brand)
          )
        )
      ),
      h("div", { className: "store-hero-visual", "aria-hidden": "true" },
        h("div", { className: "store-hero-photo" }),
        h("div", { className: "store-hero-proof" },
          h("span", null, "Garage stock"),
          h("strong", null, availableCount || parts.length || "--"),
          h("small", null, "Matched by OEM, chassis clue, barcode, or part name")
        )
      )
    ),
    h("section", { className: "store-petrolhead-strip", "aria-label": "Performance part categories" },
      petrolheadFilters.map((filter) =>
        h("button", {
          key: filter.label,
          type: "button",
          onClick: () => {
            setSearch(filter.query);
            setActiveView("shop");
          }
        },
          h("strong", null, filter.label),
          h("span", null, filter.detail)
        )
      )
    ),
    h("section", { className: "store-commerce-grid" },
      h("div", { className: "store-catalog-section" },
        h("div", { className: "store-section-heading" },
          h("div", null,
            h("span", null, t("store.liveCatalog", "Live catalog")),
            h("h2", null, "Parts with garage credibility")
          ),
          h("button", { className: "secondary-button", type: "button", onClick: () => { setSearch(""); load(); } }, t("store.clearSearch", "Clear search"))
        ),
        h(StatusLine, { status }),
        h("div", { className: "store-product-grid" },
          featuredParts.map((part) => {
            const inCart = cartRows.find((item) => item.partId === part.id)?.quantity || 0;
            return h(PartCard, { key: part.id, part, inCart, addToCart, t });
          }),
          featuredParts.length === 0 && h("div", { className: "store-empty-panel" },
            h("strong", null, t("store.noPartsMatch", "No parts matched that search.")),
            h("span", null, t("store.trySearch", "Try a German brand, OEM number, internal code, or part name."))
          )
        )
      ),
      h(CartPanel, {
        cartRows,
        cartItemCount,
        cartTotal,
        updateQuantity,
        onCheckout: goCheckout,
        onShop: goShop,
        checkoutLabel: isSignedIn ? undefined : t("store.signInToOrder", "Sign in to checkout"),
        t
      })
    )
  );

  const renderCart = () => h("section", { className: "store-simple-view" },
    h("div", { className: "store-page-title" },
      h("span", null, t("store.currentOrder", "Current order")),
      h("h1", null, t("store.cart", "Cart"))
    ),
    h(CartPanel, {
      cartRows,
      cartItemCount,
      cartTotal,
      updateQuantity,
      onCheckout: goCheckout,
      onShop: goShop,
      checkoutLabel: isSignedIn ? undefined : t("store.signInToOrder", "Sign in to checkout"),
      t
    })
  );

  const renderPartRequest = () => h("section", { className: "store-request-view" },
    h("div", { className: "store-page-title" },
      h("span", null, "Reverse storefront"),
      h("h1", null, "Show us the part. We will find it.")
    ),
    h("div", { className: "store-request-layout" },
      h("article", { className: "store-form-section store-request-form" },
        h("h2", null, "Part request"),
        h("p", null, "Upload a counter photo, OEM marking, or vehicle clue. The workshop receives a structured request and can reply on WhatsApp."),
        h("label", { className: "store-photo-drop" },
          h("strong", null, requestImageName || "Upload a part photo"),
          h("span", null, "JPG, PNG, WebP, or HEIC. Add a clue first for better matching."),
          h("input", { type: "file", accept: "image/*", onChange: findRequestMatches })
        ),
        h("div", { className: "store-field-grid two" },
          h("label", null, "Requested part", h("input", { value: requestPartName, onChange: (event) => setRequestPartName(event.target.value), placeholder: "Left headlight, turbo hose..." })),
          h("label", null, "OEM number", h("input", { value: requestOemNumber, onChange: (event) => setRequestOemNumber(event.target.value), placeholder: "Optional marking or OEM code" }))
        ),
        h("label", null, "Vehicle details", h("input", { value: requestVehicleDetails, onChange: (event) => setRequestVehicleDetails(event.target.value), placeholder: "2015 Mercedes W205 C200" })),
        h("div", { className: "store-field-grid two" },
          h("label", null, "Your name", h("input", { value: customerName, onChange: (event) => setCustomerName(event.target.value) })),
          h("label", null, "WhatsApp phone", h("input", { value: customerPhone, onChange: (event) => setCustomerPhone(event.target.value) }))
        ),
        h("label", null, "Quantity", h("input", { type: "number", min: "1", value: requestQuantity, onChange: (event) => setRequestQuantity(event.target.value) })),
        h("label", null, "Notes", h("textarea", { value: requestNotes, onChange: (event) => setRequestNotes(event.target.value), placeholder: "Side, color, engine code, or anything the workshop should know." })),
        h(StatusLine, { status }),
        h("button", { className: "primary-button", type: "button", disabled: isLoading, onClick: submitPartRequest },
          isSignedIn ? "Send workshop request" : "Sign in to send request"
        )
      ),
      h("aside", { className: "store-request-results" },
        h("div", null,
          h("span", null, "Picture search"),
          h("h2", null, requestMatches.length ? "Possible matches" : "Your shortlist appears here"),
          h("p", null, "Choosing a match helps the workshop reserve the exact catalog item. Leave it unselected when you are unsure.")
        ),
        requestMatches.map((match) =>
          h("button", {
            className: String(match.partId) === requestPartId ? "store-request-match active" : "store-request-match",
            key: match.partId,
            type: "button",
            onClick: () => chooseRequestMatch(match)
          },
            h("strong", null, `${match.internalCode} / ${match.partName}`),
            h("span", null, `${match.availableQuantity} available / ${money(match.salePrice, match.currency)}`),
            h("small", null, match.matchReason || "Picture search match")
          )
        )
      )
    )
  );

  const renderCheckout = () => h("section", { className: "store-checkout-view" },
    h(CartPanel, {
      cartRows,
      cartItemCount,
      cartTotal,
      updateQuantity,
      onCheckout: checkout,
      onShop: goShop,
      checkoutLabel: t("store.checkout", "Checkout"),
      t
    }),
    h("div", { className: "store-checkout-form" },
      h("div", { className: "store-page-title" },
        h("span", null, t("store.checkout", "Checkout")),
        h("h1", null, t("store.finishOrder", "Finish order"))
      ),
      h("section", { className: "store-form-section" },
        h("h2", null, t("store.contact", "Contact")),
        h("div", { className: "store-field-grid two" },
          h("label", null, "Name", h("input", { value: customerName, onChange: (event) => setCustomerName(event.target.value), autoComplete: "name" })),
          h("label", null, "Phone", h("input", { value: customerPhone, onChange: (event) => setCustomerPhone(event.target.value), autoComplete: "tel" }))
        ),
        h("label", null, "Email", h("input", { value: customerEmail, onChange: (event) => setCustomerEmail(event.target.value), autoComplete: "email" }))
      ),
      h("section", { className: "store-form-section" },
        h("h2", null, t("store.shippingAddress", "Shipping address")),
        h("label", null, "Address line 1", h("input", { value: shippingAddressLine1, onChange: (event) => setShippingAddressLine1(event.target.value), autoComplete: "shipping address-line1" })),
        h("label", null, "Address line 2", h("input", { value: shippingAddressLine2, onChange: (event) => setShippingAddressLine2(event.target.value), autoComplete: "shipping address-line2" })),
        h("div", { className: "store-field-grid two" },
          h("label", null, "City", h("input", { value: shippingCity, onChange: (event) => setShippingCity(event.target.value), autoComplete: "shipping address-level2" })),
          h("label", null, "Region", h("input", { value: shippingRegion, onChange: (event) => setShippingRegion(event.target.value), autoComplete: "shipping address-level1" }))
        ),
        h("div", { className: "store-field-grid two" },
          h("label", null, "Postal code", h("input", { value: shippingPostalCode, onChange: (event) => setShippingPostalCode(event.target.value), autoComplete: "shipping postal-code" })),
          h("label", null, "Country", h("input", { value: shippingCountry, onChange: (event) => setShippingCountry(event.target.value), autoComplete: "shipping country-name" }))
        ),
        h("label", null, "Delivery instructions", h("textarea", { value: deliveryInstructions, onChange: (event) => setDeliveryInstructions(event.target.value) }))
      ),
      h("section", { className: "store-form-section" },
        h("h2", null, t("store.paymentGateway", "Payment gateway")),
        h("div", { className: "store-payment-options" },
          paymentGatewayOptions.map((option) =>
            h("button", {
              key: option.value,
              className: option.value === paymentMethod ? "store-payment-option active" : "store-payment-option",
              type: "button",
              onClick: () => setPaymentMethod(option.value)
            },
              h("strong", null, option.title),
              h("span", null, option.subtitle)
            )
          )
        ),
        h("label", null, paymentMethod === "Whish Gateway" ? "Whish reference" : "Areeba reference",
          h("input", { value: paymentReference, onChange: (event) => setPaymentReference(event.target.value) })
        ),
        h("p", { className: "store-payment-note" }, t("store.paymentNote", "Payment is confirmed through the selected gateway before fulfillment."))
      ),
      h(StatusLine, { status }),
      h("button", { className: "primary-button store-place-order", onClick: checkout, disabled: isLoading || cart.length === 0 || !isSignedIn },
        t("store.payWith", "Pay with {method}", { method: paymentMethod.replace(" Gateway", "") })
      )
    )
  );

  const renderAccount = () => h("section", { className: "store-account-view" },
    h("div", { className: "store-page-title" },
      h("span", null, isSignedIn ? t("store.account", "Account") : t("store.guestDriver", "Guest driver")),
      h("h1", null, isSignedIn ? (user.fullName || t("store.driver", "Driver")) : t("store.signIn", "Sign in"))
    ),
    isSignedIn
      ? h("div", { className: "store-account-grid" },
        h("article", { className: "store-account-panel" },
          h("div", { className: "store-account-card" },
            h("span", { className: "avatar" }, initials(user.fullName)),
            h("div", null,
              h("span", null, `Role ID ${user.roleId ?? user.RoleId ?? 4}`),
              h("strong", null, user.fullName),
              h("small", null, user.email || user.username || "")
            )
          ),
          h("button", { className: "primary-button danger-button", type: "button", onClick: onLogout }, t("common.signOut", "Sign out"))
        ),
        h("article", { className: "store-account-panel" }, h(ThemePicker, { value: themeKey, onChange: onTheme, t })),
        h("article", { className: "store-account-panel" }, h(LanguagePicker, { value: languageKey, onChange: onLanguage, t }))
      )
      : h("div", { className: "store-account-grid store-account-grid-auth" },
        h("article", { className: "store-account-panel store-login-panel" },
          h(LoginPanel, {
            initialApiBaseUrl,
            onLogin,
            t
          })
        ),
        h("article", { className: "store-account-panel" }, h(ThemePicker, { value: themeKey, onChange: onTheme, t })),
        h("article", { className: "store-account-panel" }, h(LanguagePicker, { value: languageKey, onChange: onLanguage, t }))
      )
  );

  const activePanel = {
    account: renderAccount,
    cart: renderCart,
    checkout: renderCheckout,
    request: renderPartRequest,
    shop: renderShop
  }[activeView] || renderShop;

  return h("main", { className: "storefront-shell german-parts-site store-redesign" },
    h("header", { className: "storefront-header store-header-redesign" },
      h("button", { className: "storefront-brand store-brand-button", type: "button", onClick: goShop },
        h(BrandMark, { size: "small", variant: "german", label: t("store.brandTitle", "Maalouf German Parts") }),
        h("div", null,
          h("strong", null, t("store.brandTitle", "Maalouf German Parts")),
          h("span", null, t("store.brandSubtitle", "German car parts store"))
        )
      ),
      h("nav", { className: "store-header-nav", "aria-label": "Store navigation" },
        storeNavItems.map((item) =>
          h("button", {
            key: item.key,
            className: activeView === item.key ? "active" : "",
            type: "button",
            onClick: () => goStoreView(item.key)
          }, item.key === "account" && !isSignedIn ? t("store.signIn", "Sign in") : item.label)
        )
      ),
      h("button", { className: "store-header-cart", type: "button", onClick: () => setActiveView(cartItemCount ? "cart" : "shop") },
        h("span", null, t("store.cart", "Cart")),
        h("strong", null, cartItemCount)
      ),
      h("button", {
        className: isSignedIn ? "store-header-user" : "store-header-user store-header-sign-in",
        type: "button",
        onClick: () => setActiveView("account"),
        "aria-label": isSignedIn ? t("store.account", "Account") : t("store.signIn", "Sign in")
      },
        isSignedIn ? initials(user.fullName) : t("store.signIn", "Sign in")
      )
    ),
    h("section", { className: "storefront-content store-content-redesign" }, activePanel())
  );
}
