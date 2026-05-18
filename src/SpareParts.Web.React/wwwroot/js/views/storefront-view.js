import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { webAppRoleName } from "../core/config.js";
import { initials, money } from "../core/formatters.js";
import { BrandMark, LanguagePicker, ThemePicker } from "../components/layout.js";
import { StatusLine } from "../components/shared.js";

const germanBrandFilters = ["BMW", "Mercedes", "Audi", "Porsche", "Volkswagen", "AMG"];
const paymentGatewayOptions = [
  { value: "Areeba Gateway", title: "Areeba", subtitle: "Secure card payment gateway" },
  { value: "Whish Gateway", title: "Whish", subtitle: "Wallet payment gateway" }
];
const storeTabs = [
  { key: "garage", labelKey: "store.garage", fallback: "Garage" },
  { key: "parts", labelKey: "store.parts", fallback: "Parts" },
  { key: "cart", labelKey: "store.cart", fallback: "Cart" },
  { key: "checkout", labelKey: "store.checkout", fallback: "Pay" },
  { key: "account", labelKey: "store.account", fallback: "Driver" }
];

const storeIconPaths = {
  garage: "M4 12h16l-2-5H6l-2 5Zm1 0v7h14v-7M8 16h2M14 16h2",
  parts: "M7 7h10v10H7V7Zm-3 3h3M17 10h3M10 4v3M14 4v3M10 17v3M14 17v3",
  cart: "M5 5h2l2 10h8l2-7H8M10 20a1 1 0 1 0 0-2 1 1 0 0 0 0 2Zm7 0a1 1 0 1 0 0-2 1 1 0 0 0 0 2Z",
  checkout: "M4 7h16v10H4V7Zm3 4h5M15 14h2",
  account: "M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm-7 8a7 7 0 0 1 14 0"
};

function StoreIcon({ name }) {
  return h("svg", { className: "store-tab-icon", viewBox: "0 0 24 24", "aria-hidden": "true" },
    h("path", { d: storeIconPaths[name] })
  );
}

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

function QuickAction({ title, body, onClick }) {
  return h("button", { className: "quick-action", type: "button", onClick },
    h("strong", null, title),
    h("span", null, body)
  );
}

function CartRows({ cartRows, updateQuantity, t }) {
  return h("div", { className: "cart-list" },
    cartRows.map((item) =>
      h("div", { className: "cart-row", key: item.partId },
        h("div", null,
          h("strong", null, item.part?.name || "Part"),
          h("span", null, money(item.lineTotal, item.part?.currency || "USD"))
        ),
        h("div", { className: "quantity-stepper" },
          h("button", { type: "button", onClick: () => updateQuantity(item.partId, -1) }, "-"),
          h("span", null, item.quantity),
          h("button", { type: "button", onClick: () => updateQuantity(item.partId, 1) }, "+")
        )
      )
    ),
    cartRows.length === 0 && h("p", { className: "empty-state" }, t("store.emptyCart", "Your selected German parts will appear here."))
  );
}

export function CustomerStorefrontView({
  api,
  user,
  themeKey,
  languageKey,
  onTheme,
  onLanguage,
  onLogout,
  t
}) {
  const [activeTab, setActiveTab] = useState("garage");
  const [parts, setParts] = useState([]);
  const [cart, setCart] = useState([]);
  const [search, setSearch] = useState("");
  const [customerName, setCustomerName] = useState(user.fullName || "");
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
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

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
  const availableCount = parts.reduce((count, part) => count + Number(part.availableQuantity > 0 ? 1 : 0), 0);

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
    if (cart.length === 0) {
      setStatus(t("store.addPartsFirst", "Add parts to the cart first."));
      setActiveTab("parts");
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
      setActiveTab("garage");
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
    load,
    paymentMethod,
    paymentReference,
    shippingAddressLine1,
    shippingAddressLine2,
    shippingCity,
    shippingCountry,
    shippingPostalCode,
    shippingRegion,
    t
  ]);

  const renderParts = () => h("section", { className: "store-tab-panel catalog-pane" },
    h("header", { className: "catalog-header" },
      h("div", null,
        h("span", { className: "eyebrow" }, t("store.liveCatalog", "Live Catalog")),
        h("h2", null, t("store.catalogTitle", "Parts ready to quote and checkout"))
      ),
      h("div", { className: "toolbar catalog-search" },
        h("input", {
          value: search,
          onChange: (event) => setSearch(event.target.value),
          onKeyDown: (event) => event.key === "Enter" && load(),
          placeholder: t("store.searchPlaceholder", "Search BMW sensor, Audi filter, OEM code...")
        }),
        h("button", { className: "secondary-button", onClick: load, disabled: isLoading }, isLoading ? t("store.searching", "Searching") : t("common.search", "Search"))
      )
    ),
    h(StatusLine, { status }),
    cartItemCount > 0 && h("button", { className: "cart-summary-bar", type: "button", onClick: () => setActiveTab("cart") },
      h("strong", null, t("store.currentOrder", "Current order")),
      h("span", null, `${t("store.itemCount", "{count} items", { count: cartItemCount })} / ${money(cartTotal, cartRows[0]?.part?.currency || "USD")}`)
    ),
    h("div", { className: "catalog-grid" },
      parts.map((part) => {
        const inCart = cartRows.find((item) => item.partId === part.id)?.quantity || 0;
        return h("article", { className: "part-card", key: part.id },
          h("div", { className: "part-visual", "aria-hidden": "true" }, h("span", null, partVisualLabel(part))),
          h("div", { className: "part-card-copy" },
            h("span", { className: "part-code" }, part.internalCode || "German Part"),
            h("h3", null, part.name),
            h("p", null, partFitment(part, t))
          ),
          h("div", { className: "part-meta" },
            h("strong", null, money(part.salePrice, part.currency)),
            h("span", null, partAvailability(part))
          ),
          inCart > 0 && h("span", { className: "in-cart-pill" }, t("store.inCartCount", "{count} in cart", { count: inCart })),
          h("button", { className: "primary-button", onClick: () => addToCart(part) }, inCart > 0 ? t("store.addAnother", "Add another") : t("store.addToCart", "Add to Cart"))
        );
      }),
      parts.length === 0 && h("div", { className: "catalog-empty" },
        h("strong", null, t("store.noPartsMatch", "No parts matched that search.")),
        h("span", null, t("store.trySearch", "Try a German brand, OEM number, internal code, or part name.")),
        h("button", { className: "secondary-button", type: "button", onClick: () => { setSearch(""); load(); } }, t("store.clearSearch", "Clear search"))
      )
    )
  );

  const renderCart = () => h("section", { className: "store-tab-panel cart-tab-panel" },
    h("div", { className: "cart-heading" },
      h("span", { className: "eyebrow" }, t("store.orderSummary", "Order summary")),
      h("h2", null, t("store.cart", "Cart")),
      h("strong", null, t("store.itemCount", "{count} items", { count: cartItemCount }))
    ),
    h(CartRows, { cartRows, updateQuantity, t }),
    h("div", { className: "cart-total" }, h("span", null, t("store.total", "Total")), h("strong", null, money(cartTotal, cartRows[0]?.part?.currency || "USD"))),
    h("div", { className: "hero-actions" },
      h("button", { className: "secondary-button", type: "button", onClick: () => setActiveTab("parts") }, t("store.continueShopping", "Continue shopping")),
      h("button", { className: "primary-button", type: "button", onClick: () => setActiveTab(cartRows.length ? "checkout" : "parts") }, cartRows.length ? t("store.checkout", "Pay") : t("store.addBeforeCheckout", "Add parts to the cart before checkout."))
    )
  );

  const renderCheckout = () => h("section", { className: "store-tab-panel checkout-layout" },
    h("aside", { className: "cart-pane inline-cart" },
      h("div", { className: "cart-heading" },
        h("span", { className: "eyebrow" }, t("store.orderSummary", "Order summary")),
        h("h2", null, t("store.cart", "Cart")),
        h("strong", null, t("store.itemCount", "{count} items", { count: cartItemCount }))
      ),
      h(CartRows, { cartRows, updateQuantity, t }),
      h("div", { className: "cart-total" }, h("span", null, t("store.total", "Total")), h("strong", null, money(cartTotal, cartRows[0]?.part?.currency || "USD")))
    ),
    h("div", { className: "checkout-form" },
      h("section", { className: "checkout-section" },
        h("h3", null, t("store.contact", "Contact")),
        h("div", { className: "checkout-grid two" },
          h("label", null, "Name", h("input", { value: customerName, onChange: (event) => setCustomerName(event.target.value), autoComplete: "name" })),
          h("label", null, "Phone", h("input", { value: customerPhone, onChange: (event) => setCustomerPhone(event.target.value), autoComplete: "tel" }))
        ),
        h("label", null, "Email", h("input", { value: customerEmail, onChange: (event) => setCustomerEmail(event.target.value), autoComplete: "email" }))
      ),
      h("section", { className: "checkout-section" },
        h("h3", null, t("store.shippingAddress", "Shipping Address")),
        h("label", null, "Address line 1", h("input", { value: shippingAddressLine1, onChange: (event) => setShippingAddressLine1(event.target.value), autoComplete: "shipping address-line1" })),
        h("label", null, "Address line 2", h("input", { value: shippingAddressLine2, onChange: (event) => setShippingAddressLine2(event.target.value), autoComplete: "shipping address-line2" })),
        h("div", { className: "checkout-grid two" },
          h("label", null, "City", h("input", { value: shippingCity, onChange: (event) => setShippingCity(event.target.value), autoComplete: "shipping address-level2" })),
          h("label", null, "Region", h("input", { value: shippingRegion, onChange: (event) => setShippingRegion(event.target.value), autoComplete: "shipping address-level1" }))
        ),
        h("div", { className: "checkout-grid two" },
          h("label", null, "Postal code", h("input", { value: shippingPostalCode, onChange: (event) => setShippingPostalCode(event.target.value), autoComplete: "shipping postal-code" })),
          h("label", null, "Country", h("input", { value: shippingCountry, onChange: (event) => setShippingCountry(event.target.value), autoComplete: "shipping country-name" }))
        ),
        h("label", null, "Delivery instructions", h("textarea", { value: deliveryInstructions, onChange: (event) => setDeliveryInstructions(event.target.value) }))
      ),
      h("section", { className: "checkout-section" },
        h("h3", null, t("store.paymentGateway", "Payment Gateway")),
        h("div", { className: "payment-options" },
          paymentGatewayOptions.map((option) =>
            h("button", {
              key: option.value,
              className: option.value === paymentMethod ? "payment-option active" : "payment-option",
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
        h("p", { className: "payment-note" }, t("store.paymentNote", "Payment is confirmed through the selected gateway before fulfillment."))
      ),
      h("button", { className: "primary-button checkout-button", onClick: checkout, disabled: isLoading || cart.length === 0 },
        t("store.payWith", "Pay with {method}", { method: paymentMethod.replace(" Gateway", "") })
      )
    )
  );

  const renderGarage = () => h("section", { className: "store-tab-panel" },
    h("section", { className: "storefront-hero" },
      h("div", { className: "hero-copy" },
        h("span", { className: "eyebrow" }, "BMW · Mercedes-Benz · Audi · Porsche · Volkswagen"),
        h("h1", null, t("store.heroTitle", "German Performance Parts")),
        h("p", null, t("store.heroBody", "Find verified replacement parts, fast-moving service items, and OEM-grade fitment for German vehicles.")),
        h("div", { className: "hero-actions" },
          h("button", { className: "primary-button", type: "button", onClick: () => setActiveTab("parts") }, t("store.browseParts", "Browse Parts")),
          h("button", { className: "secondary-button", type: "button", onClick: () => setActiveTab("cart") }, t("store.myCart", "My Cart"))
        )
      ),
      h("div", { className: "hero-stats" },
        h("div", null, h("strong", null, availableCount || parts.length), h("span", null, "parts ready")),
        h("div", null, h("strong", null, "OEM"), h("span", null, "fitment checks")),
        h("div", null, h("strong", null, "24h"), h("span", null, "order response"))
      )
    ),
    h("section", { className: "quick-actions" },
      h(QuickAction, { title: t("store.findFast", "Find fast"), body: t("store.findFastSubtitle", "Search by OEM, barcode, internal code, or brand."), onClick: () => setActiveTab("parts") }),
      h(QuickAction, { title: t("store.reviewCart", "Review cart"), body: t("store.reviewCartSubtitle", "Tune quantities before checkout."), onClick: () => setActiveTab("cart") }),
      h(QuickAction, { title: t("store.finishOrder", "Finish order"), body: t("store.finishOrderSubtitle", "Confirm delivery and payment gateway."), onClick: () => setActiveTab("checkout") })
    ),
    h("section", { className: "fitment-strip" },
      h("div", null,
        h("span", null, "Find by code, OEM, barcode, or part name"),
        h("strong", null, "Built for precise German fitment")
      ),
      h("div", { className: "brand-filter-row" },
        germanBrandFilters.map((brand) => h("button", { key: brand, className: "brand-filter", onClick: () => { setSearch(brand); setActiveTab("parts"); } }, brand))
      )
    )
  );

  const renderAccount = () => h("section", { className: "store-tab-panel account-tab-panel" },
    h("article", { className: "settings-panel account-settings-panel" },
      h("div", { className: "account-card" },
        h("span", { className: "avatar" }, initials(user.fullName)),
        h("div", null,
          h("span", null, webAppRoleName),
          h("strong", null, user.fullName),
          h("small", null, user.role || "")
        )
      ),
      h("button", { className: "primary-button danger-button", type: "button", onClick: onLogout }, t("common.signOut", "Sign out"))
    ),
    h("article", { className: "settings-panel" }, h(ThemePicker, { value: themeKey, onChange: onTheme, t })),
    h("article", { className: "settings-panel" }, h(LanguagePicker, { value: languageKey, onChange: onLanguage, t }))
  );

  const activePanel = {
    account: renderAccount,
    cart: renderCart,
    checkout: renderCheckout,
    garage: renderGarage,
    parts: renderParts
  }[activeTab] || renderGarage;

  return h("main", { className: "storefront-shell german-parts-site" },
    h("header", { className: "storefront-header store-header-clean" },
      h("div", { className: "storefront-brand" },
        h(BrandMark, { size: "small", variant: "german", label: t("store.brandTitle", "Maalouf German Parts") }),
        h("div", null,
          h("strong", null, t("store.brandTitle", "Maalouf German Parts")),
          h("span", null, t("store.brandSubtitle", "OEM-grade German car parts"))
        )
      )
    ),
    h("section", { className: "storefront-content" }, activePanel()),
    h("nav", { className: "store-bottom-tabs", "aria-label": "Store tabs" },
      storeTabs.map((tab) =>
        h("button", {
          key: tab.key,
          className: tab.key === activeTab ? "store-tab active" : "store-tab",
          type: "button",
          onClick: () => setActiveTab(tab.key)
        },
          h(StoreIcon, { name: tab.key }),
          h("span", null, t(tab.labelKey, tab.fallback)),
          tab.key === "cart" && cartItemCount > 0 && h("b", null, cartItemCount)
        )
      )
    )
  );
}
