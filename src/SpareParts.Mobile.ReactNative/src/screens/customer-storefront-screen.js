const React = require("react");
const {
  ImageBackground,
  Pressable,
  ScrollView,
  Text,
  View
} = require("react-native");
const { StatusBar } = require("expo-status-bar");
const { useSafeAreaInsets } = require("react-native-safe-area-context");
const { Field } = require("../components/ui");
const { initials, money } = require("../core/formatters");
const { appLanguages, webAppRoleName, wpfThemes } = require("../core/app-config");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

const heroImage = { uri: "https://images.unsplash.com/photo-1486262715619-67b85e0b08d3?auto=format&fit=crop&w=1600&q=82" };
const partImage = { uri: "https://images.unsplash.com/photo-1493238792000-8113da705763?auto=format&fit=crop&w=900&q=76" };

const germanBrandFilters = ["BMW", "Mercedes", "Audi", "Porsche", "Volkswagen", "AMG"];
const paymentGatewayOptions = [
  { value: "Areeba Gateway", title: "Areeba", subtitle: "Secure card payment gateway" },
  { value: "Whish Gateway", title: "Whish", subtitle: "Wallet payment gateway" }
];

function partVisualLabel(part) {
  return String(part.internalCode || part.oemNumber || part.name || "DE").slice(0, 3).toUpperCase();
}

function partFitment(part, t) {
  return part.oemNumber || part.barcode || part.internalCode || t("store.oemFitment", "OEM-grade fitment");
}

function partAvailability(part, t) {
  const quantity = Number(part.availableQuantity ?? part.stockQuantity ?? 0);
  if (quantity > 12) return t("store.inStock", "In stock");
  if (quantity > 0) return t("store.left", "{count} left", { count: quantity });
  return t("store.checkAvailability", "Check availability");
}

function StoreButton({ title, onPress, variant, disabled, compact }) {
  const { styles } = useTheme();

  return el(Pressable, {
    style: [
      styles.storeButton,
      variant === "secondary" && styles.storeButtonSecondary,
      compact && styles.storeButtonCompact,
      disabled && styles.disabledButton
    ],
    onPress,
    disabled
  }, el(Text, { style: styles.storeButtonText }, title));
}

function BrandFilters({ onSelect }) {
  const { styles } = useTheme();

  return el(View, { style: styles.storeBrandRail },
    germanBrandFilters.map((brand) => el(Pressable, {
      key: brand,
      style: styles.storeBrandChip,
      onPress: () => onSelect(brand)
    }, el(Text, { style: styles.storeBrandChipText }, brand)))
  );
}

function StoreAction({ title, subtitle, value, onPress, variant }) {
  const { styles } = useTheme();

  return el(Pressable, {
    style: [styles.storeActionButton, variant === "primary" && styles.storeActionButtonPrimary],
    onPress
  },
    Boolean(value) && el(Text, { style: styles.storeActionValue }, value),
    el(Text, { style: styles.storeActionTitle }, title),
    Boolean(subtitle) && el(Text, { style: styles.storeActionSubtitle }, subtitle)
  );
}

function StoreSummaryBar({ count, total, onCart, onCheckout }) {
  const { styles, t } = useTheme();

  return el(View, { style: styles.storeSummaryBar },
    el(View, { style: styles.storeSummaryCopy },
      el(Text, { style: styles.storeSummaryLabel }, t("store.currentOrder", "Current order")),
      el(Text, { style: styles.storeSummaryValue }, `${t("store.itemCount", "{count} items", { count })} · ${money(total)}`)
    ),
    el(View, { style: styles.storeSummaryActions },
      el(StoreButton, { title: t("store.viewCart", "View cart"), onPress: onCart, variant: "secondary", compact: true }),
      el(StoreButton, { title: t("store.checkout", "Checkout"), onPress: onCheckout, disabled: count === 0, compact: true })
    )
  );
}

function StoreStepRail({ active }) {
  const { styles, t } = useTheme();
  const steps = [
    { key: "cart", label: t("store.cart", "Cart") },
    { key: "contact", label: t("store.contact", "Contact") },
    { key: "payment", label: t("store.payment", "Payment") }
  ];
  const activeIndex = steps.findIndex((step) => step.key === active);

  return el(View, { style: styles.storeStepRail },
    steps.map((step, index) => {
      const isDone = index < activeIndex;
      const isActive = index === activeIndex;
      return el(View, { key: step.key, style: [styles.storeStep, (isActive || isDone) && styles.storeStepActive] },
        el(Text, { style: [styles.storeStepNumber, (isActive || isDone) && styles.storeStepNumberActive] }, isDone ? "✓" : String(index + 1)),
        el(Text, { style: [styles.storeStepLabel, (isActive || isDone) && styles.storeStepLabelActive] }, step.label)
      );
    })
  );
}

function PartCard({ part, onAdd, quantity }) {
  const { styles, t } = useTheme();

  return el(View, { style: styles.storePartCard },
    el(ImageBackground, { source: partImage, style: styles.storePartImage, imageStyle: styles.storePartImageInner },
      el(View, { style: styles.storePartImageOverlay },
        el(Text, { style: styles.storePartBadge }, partVisualLabel(part))
      )
    ),
    el(View, { style: styles.storePartCopy },
      el(Text, { style: styles.storePartCode }, part.internalCode || t("store.germanPart", "German Part")),
      el(Text, { style: styles.storePartTitle }, part.name),
      el(Text, { style: styles.storePartFitment }, partFitment(part, t))
    ),
    el(View, { style: styles.storePartMeta },
      el(Text, { style: styles.storePartPrice }, money(part.salePrice, part.currency)),
      el(Text, { style: styles.storePartAvailability }, partAvailability(part, t))
    ),
    quantity > 0 && el(View, { style: styles.storePartInCart },
      el(Text, { style: styles.storePartInCartText }, t("store.inCartCount", "{count} in cart", { count: quantity }))
    ),
    el(StoreButton, { title: quantity > 0 ? t("store.addAnother", "Add another") : t("store.addToCart", "Add to Cart"), onPress: () => onAdd(part) })
  );
}

function CartItemRow({ item, onQuantity }) {
  const { styles, t } = useTheme();

  return el(View, { style: styles.storeCartRow },
    el(View, { style: styles.storeCartCopy },
      el(Text, { style: styles.storeCartTitle }, item.part?.name || t("store.part", "Part")),
      el(Text, { style: styles.storeCartSubtitle }, money(item.lineTotal, item.part?.currency || "USD"))
    ),
    el(View, { style: styles.storeStepper },
      el(Pressable, { style: styles.storeStepperButton, onPress: () => onQuantity(item.partId, -1) },
        el(Text, { style: styles.storeStepperText }, "-")
      ),
      el(Text, { style: styles.storeStepperValue }, String(item.quantity)),
      el(Pressable, { style: styles.storeStepperButton, onPress: () => onQuantity(item.partId, 1) },
        el(Text, { style: styles.storeStepperText }, "+")
      )
    )
  );
}

function PaymentOption({ option, selected, onPress }) {
  const { styles, t } = useTheme();

  return el(Pressable, {
    style: [styles.storePaymentOption, selected && styles.storePaymentOptionActive],
    onPress
  },
    el(Text, { style: styles.storePaymentTitle }, option.title),
    el(Text, { style: styles.storePaymentSubtitle }, t(`store.paymentDescriptions.${option.value}`, option.subtitle))
  );
}

function CheckoutSection({ title, children }) {
  const { styles } = useTheme();

  return el(View, { style: styles.storeCheckoutSection },
    el(Text, { style: styles.storeCheckoutSectionTitle }, title),
    children
  );
}

function CustomerStorefrontScreen({ activeSection, api, embedded, languageKey, onLanguage, onLogout, onNavigate, onTheme, themeKey, user }) {
  const { styles, t } = useTheme();
  const insets = useSafeAreaInsets();
  const sectionKey = activeSection || "store-home";
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

  const load = useCallback(async (nextSearch) => {
    const queryText = typeof nextSearch === "string" ? nextSearch : search;
    setIsLoading(true);
    setStatus(t("store.loadingParts", "Loading available parts..."));
    try {
      const query = queryText.trim() ? `&search=${encodeURIComponent(queryText.trim())}` : "";
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

  const cartTotal = useMemo(
    () => cartRows.reduce((sum, item) => sum + item.lineTotal, 0),
    [cartRows]
  );
  const availableCount = parts.reduce((count, part) => count + Number(part.availableQuantity > 0 ? 1 : 0), 0);
  const cartItemCount = cartRows.reduce((sum, item) => sum + item.quantity, 0);
  const cartQuantityByPartId = useMemo(() =>
    cartRows.reduce((map, item) => ({ ...map, [item.partId]: item.quantity }), {}),
  [cartRows]);

  const navigateStore = useCallback((key) => {
    if (onNavigate) onNavigate(key);
  }, [onNavigate]);

  const selectBrand = useCallback((brand) => {
    setSearch(brand);
    load(brand);
  }, [load]);

  const addToCart = useCallback((part) => {
    setCart((current) => {
      const existing = current.find((item) => item.partId === part.id);
      if (existing) {
        return current.map((item) =>
          item.partId === part.id
            ? { ...item, quantity: Math.min(item.quantity + 1, part.availableQuantity || 99), part }
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
      const available = item.part?.availableQuantity || 99;
      const quantity = Math.min(Math.max(item.quantity + delta, 0), available);
      return quantity === 0 ? [] : [{ ...item, quantity }];
    }));
  }, []);

  const checkout = useCallback(async () => {
    if (cart.length === 0) {
      setStatus(t("store.addPartsFirst", "Add parts to the cart first."));
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

  const headerNode = !embedded && el(View, { key: "store-header", style: styles.storeHeader },
        el(View, { style: styles.storeBrand },
          el(View, { style: styles.storeBrandMark }, el(Text, { style: styles.storeBrandMarkText }, "DE")),
          el(View, { style: styles.storeBrandCopy },
            el(Text, { style: styles.storeBrandTitle }, t("store.brandTitle", "Maalouf German Parts")),
            el(Text, { style: styles.storeBrandSubtitle }, t("store.brandSubtitle", "OEM-grade German car parts"))
          )
        ),
        el(Pressable, { style: styles.storeLogoutButton, onPress: onLogout },
          el(Text, { style: styles.storeLogoutText }, t("common.signOut", "Sign out"))
        )
      );
  const statusNode = Boolean(status) && el(Text, { key: "store-status", style: styles.storeStatus }, status);
  const heroNode = el(ImageBackground, { key: "store-hero", source: heroImage, style: styles.storeHero, imageStyle: styles.storeHeroImage },
        el(View, { style: styles.storeHeroOverlay },
          el(Text, { style: styles.storeEyebrow }, "BMW · Mercedes-Benz · Audi · Porsche · Volkswagen"),
          el(Text, { style: styles.storeHeroTitle }, t("store.heroTitle", "German Performance Parts")),
          el(Text, { style: styles.storeHeroBody }, t("store.heroBody", "Find verified replacement parts, fast-moving service items, and OEM-grade fitment for German vehicles.")),
          el(View, { style: styles.storeHeroStats },
            el(View, { style: styles.storeHeroStat },
              el(Text, { style: styles.storeHeroStatValue }, String(availableCount || parts.length)),
              el(Text, { style: styles.storeHeroStatLabel }, t("store.partsReady", "parts ready"))
            ),
            el(View, { style: styles.storeHeroStat },
              el(Text, { style: styles.storeHeroStatValue }, "OEM"),
              el(Text, { style: styles.storeHeroStatLabel }, t("store.fitmentChecks", "fitment checks"))
            ),
            el(View, { style: styles.storeHeroStat },
              el(Text, { style: styles.storeHeroStatValue }, "24h"),
              el(Text, { style: styles.storeHeroStatLabel }, t("store.orderResponse", "order response"))
            )
          ),
          el(View, { style: styles.storeHeroActions },
            el(StoreButton, { title: t("store.browseParts", "Browse parts"), onPress: () => navigateStore("store-parts"), compact: true }),
            el(StoreButton, { title: t("store.myCart", "My cart"), onPress: () => navigateStore("store-cart"), variant: "secondary", compact: true })
          )
        )
      );
  const quickActionsNode = el(View, { key: "store-actions", style: styles.storeActionGrid },
        el(StoreAction, {
          title: t("store.findFast", "Find fast"),
          subtitle: t("store.findFastSubtitle", "Search OEM, barcode, internal code, or brand."),
          value: `${availableCount || parts.length}`,
          onPress: () => navigateStore("store-parts"),
          variant: "primary"
        }),
        el(StoreAction, {
          title: t("store.reviewCart", "Review cart"),
          subtitle: t("store.reviewCartSubtitle", "Adjust quantities before checkout."),
          value: String(cartItemCount),
          onPress: () => navigateStore("store-cart")
        }),
        el(StoreAction, {
          title: t("store.finishOrder", "Finish order"),
          subtitle: t("store.finishOrderSubtitle", "Confirm delivery and payment gateway."),
          value: money(cartTotal),
          onPress: () => navigateStore("store-checkout")
        })
      );
  const fitmentNode = el(View, { key: "store-fitment", style: styles.storeFitment },
        el(Text, { style: styles.storeFitmentLabel }, t("store.fitmentLabel", "Find by code, OEM, barcode, or part name")),
        el(Text, { style: styles.storeFitmentTitle }, t("store.fitmentTitle", "Built for precise German fitment")),
        el(BrandFilters, { onSelect: selectBrand })
      );
  const partsNode = [
    el(View, { key: "parts-header", style: styles.storeCatalogHeader },
        el(View, null,
          el(Text, { style: styles.storeEyebrow }, t("store.liveCatalog", "Live Catalog")),
          el(Text, { style: styles.storeSectionTitle }, t("store.catalogTitle", "Parts ready to quote and checkout"))
        ),
        el(View, { style: styles.storeSearchRow },
          el(Field, {
            label: t("common.search", "Search"),
            value: search,
            onChangeText: setSearch,
            returnKeyType: "search",
            onSubmitEditing: () => load(),
            placeholder: t("store.searchPlaceholder", "BMW sensor, Audi filter, OEM code...")
          }),
          el(StoreButton, { title: isLoading ? t("store.searching", "Searching") : t("common.search", "Search"), onPress: () => load(), variant: "secondary", disabled: isLoading })
        )
      ),
    statusNode,
    cartItemCount > 0 && el(StoreSummaryBar, {
      key: "parts-summary",
      count: cartItemCount,
      total: cartTotal,
      onCart: () => navigateStore("store-cart"),
      onCheckout: () => navigateStore("store-checkout")
    }),
    el(View, { key: "parts-grid", style: styles.storePartGrid },
        parts.map((part) => el(PartCard, {
          key: String(part.id),
          part,
          quantity: cartQuantityByPartId[part.id] || 0,
          onAdd: addToCart
        })),
        parts.length === 0 && el(View, { style: styles.storeEmpty },
          el(Text, { style: styles.storeEmptyTitle }, t("store.noPartsMatch", "No parts matched that search.")),
          el(Text, { style: styles.storeEmptyText }, t("store.trySearch", "Try a German brand, OEM number, internal code, or part name.")),
          el(StoreButton, { title: t("store.clearSearch", "Clear search"), onPress: () => { setSearch(""); load(""); }, variant: "secondary" })
        )
      )
  ];
  const cartNode = el(View, { key: "store-cart", style: styles.storeCheckout },
        el(StoreStepRail, { active: "cart" }),
        el(View, { style: styles.storeCartHeading },
          el(View, null,
            el(Text, { style: styles.storeEyebrow }, t("store.checkoutEyebrow", "Checkout")),
            el(Text, { style: styles.storeCartTitleLarge }, t("store.cart", "Cart"))
          ),
          el(Text, { style: styles.storeCartCount }, t("store.itemCount", "{count} items", { count: cartItemCount }))
        ),
        el(View, { style: styles.storeCartList },
          cartRows.map((item) => el(CartItemRow, {
            key: String(item.partId),
            item,
            onQuantity: updateQuantity
          })),
          cartRows.length === 0 && el(Text, { style: styles.storeEmptyText }, t("store.emptyCart", "Your selected German parts will appear here."))
        ),
        el(View, { style: styles.storeTotalRow },
          el(Text, { style: styles.storeTotalLabel }, t("store.total", "Total")),
          el(Text, { style: styles.storeTotalValue }, money(cartTotal, cartRows[0]?.part?.currency || "USD"))
        ),
        el(View, { style: styles.storeCartActions },
          el(StoreButton, { title: t("store.continueShopping", "Continue shopping"), onPress: () => navigateStore("store-parts"), variant: "secondary", compact: true }),
          el(StoreButton, { title: t("store.checkout", "Checkout"), onPress: () => navigateStore("store-checkout"), disabled: cartRows.length === 0, compact: true })
        )
      );
  const checkoutNode = el(View, { key: "store-checkout", style: styles.storeCheckout },
        el(StoreStepRail, { active: "payment" }),
        el(View, { style: styles.storeCartHeading },
          el(View, null,
            el(Text, { style: styles.storeEyebrow }, t("store.payment", "Payment")),
            el(Text, { style: styles.storeCartTitleLarge }, t("store.checkout", "Checkout"))
          ),
          el(Text, { style: styles.storeCartCount }, t("store.itemCount", "{count} items", { count: cartItemCount }))
        ),
        cartRows.length === 0 && el(Text, { style: styles.storeEmptyText }, t("store.addBeforeCheckout", "Add parts to the cart before checkout.")),
        cartRows.length > 0 && el(CheckoutSection, { title: t("store.orderSummary", "Order summary") },
          cartRows.slice(0, 4).map((item) =>
            el(View, { key: `summary-${item.partId}`, style: styles.storeOrderSummaryRow },
              el(Text, { style: styles.storeOrderSummaryName, numberOfLines: 1 }, item.part?.name || t("store.part", "Part")),
              el(Text, { style: styles.storeOrderSummaryValue }, `${item.quantity} · ${money(item.lineTotal, item.part?.currency || "USD")}`)
            )
          ),
          cartRows.length > 4 && el(Text, { style: styles.storeEmptyText }, t("store.moreItems", "+ {count} more", { count: cartRows.length - 4 }))
        ),
        el(CheckoutSection, { title: t("store.contact", "Contact") },
          el(Field, { label: t("fields.name", "Name"), value: customerName, onChangeText: setCustomerName }),
          el(Field, { label: t("fields.phone", "Phone"), value: customerPhone, onChangeText: setCustomerPhone, keyboardType: "phone-pad" }),
          el(Field, { label: t("fields.email", "Email"), value: customerEmail, onChangeText: setCustomerEmail, keyboardType: "email-address", autoCapitalize: "none" })
        ),
        el(CheckoutSection, { title: t("store.shippingAddress", "Shipping Address") },
          el(Field, { label: t("store.addressLine1", "Address line 1"), value: shippingAddressLine1, onChangeText: setShippingAddressLine1, placeholder: t("store.addressLine1Placeholder", "Street, building, floor") }),
          el(Field, { label: t("store.addressLine2", "Address line 2"), value: shippingAddressLine2, onChangeText: setShippingAddressLine2, placeholder: t("store.addressLine2Placeholder", "Apartment, landmark, industrial zone") }),
          el(Field, { label: t("store.city", "City"), value: shippingCity, onChangeText: setShippingCity }),
          el(Field, { label: t("store.region", "Region"), value: shippingRegion, onChangeText: setShippingRegion }),
          el(Field, { label: t("store.postalCode", "Postal code"), value: shippingPostalCode, onChangeText: setShippingPostalCode }),
          el(Field, { label: t("fields.country", "Country"), value: shippingCountry, onChangeText: setShippingCountry }),
          el(Field, {
            label: t("store.deliveryInstructions", "Delivery instructions"),
            value: deliveryInstructions,
            onChangeText: setDeliveryInstructions,
            placeholder: t("store.deliveryPlaceholder", "Preferred delivery time, receiver name..."),
            multiline: true,
            numberOfLines: 3
          })
        ),
        el(CheckoutSection, { title: t("store.paymentGateway", "Payment Gateway") },
          el(View, { style: styles.storePaymentGrid },
            paymentGatewayOptions.map((option) => el(PaymentOption, {
              key: option.value,
              option,
              selected: option.value === paymentMethod,
              onPress: () => setPaymentMethod(option.value)
            }))
          ),
          el(Field, {
            label: paymentMethod === "Whish Gateway" ? t("store.whishReference", "Whish phone or transfer reference") : t("store.areebaReference", "Areeba order reference"),
            value: paymentReference,
            onChangeText: setPaymentReference,
            placeholder: paymentMethod === "Whish Gateway" ? t("store.whishPlaceholder", "Optional wallet phone/reference") : t("store.areebaPlaceholder", "Optional gateway reference")
          }),
          el(Text, { style: styles.storePaymentNote }, t("store.paymentNote", "Payment is confirmed through the selected gateway before fulfillment."))
        ),
        el(View, { style: styles.storeTotalRow },
          el(Text, { style: styles.storeTotalLabel }, t("store.total", "Total")),
          el(Text, { style: styles.storeTotalValue }, money(cartTotal, cartRows[0]?.part?.currency || "USD"))
        ),
        el(StoreButton, { title: t("store.payWith", "Pay with {method}", { method: paymentMethod.replace(" Gateway", "") }), onPress: checkout, disabled: isLoading || cart.length === 0 })
      );
  const accountNode = el(View, { key: "store-account", style: styles.storeCheckout },
      el(View, { style: styles.storeUserFooter },
        el(View, { style: styles.storeAvatar }, el(Text, { style: styles.storeAvatarText }, initials(user.fullName))),
        el(View, { style: styles.storeUserCopy },
          el(Text, { style: styles.storeUserName }, user.fullName),
          el(Text, { style: styles.storeUserRole }, webAppRoleName)
        )
      ),
      el(CheckoutSection, { title: t("settings.language", "Language") },
        el(View, { style: styles.sideThemeGrid },
          appLanguages.map((language) =>
            el(Pressable, {
              key: language.key,
              style: [styles.settingsLanguageButton, language.key === languageKey && styles.settingsLanguageButtonActive],
              onPress: () => onLanguage && onLanguage(language.key)
            },
              el(View, { style: [styles.settingsLanguageMark, language.key === languageKey && styles.settingsLanguageMarkActive] },
                el(Text, { style: [styles.settingsLanguageMarkText, language.key === languageKey && styles.settingsLanguageMarkTextActive] }, language.shortName)
              ),
              el(Text, { style: [styles.sideThemeText, language.key === languageKey && styles.sideThemeTextActive], numberOfLines: 1 }, t(`languages.${language.key}`, language.name))
            )
          )
        )
      ),
      el(CheckoutSection, { title: t("settings.themes", "Themes") },
        el(View, { style: styles.sideThemeGrid },
          wpfThemes.map((theme) =>
            el(Pressable, {
              key: theme.key,
              style: [styles.sideThemeButton, theme.key === themeKey && styles.sideThemeButtonActive],
              onPress: () => onTheme && onTheme(theme.key)
            },
              el(View, { style: [styles.themeDot, { backgroundColor: theme.colors.accent, borderColor: theme.colors.line }] }),
              el(Text, { style: [styles.sideThemeText, theme.key === themeKey && styles.sideThemeTextActive], numberOfLines: 1 }, theme.name)
            )
          )
        )
      ),
      el(StoreButton, { title: t("common.signOut", "Sign out"), onPress: onLogout, variant: "secondary" })
    );
  const contentBySection = {
    "store-home": [heroNode, quickActionsNode, fitmentNode, statusNode],
    "store-parts": partsNode,
    "store-cart": [statusNode, cartNode],
    "store-checkout": [statusNode, checkoutNode],
    "store-account": [accountNode]
  };
  const sectionChildren = [headerNode, ...(contentBySection[sectionKey] || contentBySection["store-home"])]
    .filter(Boolean);
  const scrollNode = el(ScrollView, {
    style: styles.storeScroll,
    contentContainerStyle: styles.storeScrollContent,
    contentInsetAdjustmentBehavior: "automatic"
  }, ...sectionChildren);

  if (embedded) {
    return scrollNode;
  }

  return el(View, { style: [styles.storeRoot, { paddingTop: insets.top, paddingBottom: insets.bottom }] },
    el(StatusBar, { style: "light" }),
    scrollNode
  );
}

module.exports = { CustomerStorefrontScreen };
