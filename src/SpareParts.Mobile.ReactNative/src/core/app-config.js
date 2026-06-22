const { Platform } = require("react-native");

const storageKeys = {
  apiBaseUrl: "spareparts.mobile.apiBaseUrl",
  token: "spareparts.mobile.token",
  user: "spareparts.mobile.user",
  theme: "spareparts.mobile.theme",
  language: "spareparts.mobile.language"
};

const CommunicationChannel = {
  WhatsApp: 0,
  Sms: 1
};

const CommunicationRecipientKind = {
  Customer: 0,
  Supplier: 1,
  Manual: 2
};

const CommunicationTemplateKey = {
  SalesInvoice: 0,
  PaymentReminder: 2,
  PartAvailability: 4,
  FreeText: 6
};

const configuredApiBaseUrl = String(process.env.EXPO_PUBLIC_API_BASE_URL || "").trim();
const defaultApiBaseUrl =
  configuredApiBaseUrl ||
  (Platform.OS === "android" ? "http://10.0.2.2:5000" : "http://localhost:5000");
const defaultThemeKey = "apex";
const defaultLanguageKey = "en";
const googleClientId = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID || "";
const googleAndroidClientId = process.env.EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID || "";
const googleIosClientId = process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID || "";
const googleWebClientId = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID || googleClientId;
const facebookAppId = process.env.EXPO_PUBLIC_FACEBOOK_APP_ID || "";
const webAppRoleId = 4;

function isLocalDevelopmentApiBaseUrl(value) {
  const match = String(value || "").trim().match(/^[a-z][a-z0-9+.-]*:\/\/([^/:?#]+)/i);
  const hostname = match ? match[1].toLowerCase() : "";
  return ["localhost", "127.0.0.1", "10.0.2.2"].includes(hostname);
}

function shouldUsePackagedApiBaseUrl(storedApiBaseUrl) {
  return Boolean(configuredApiBaseUrl) && isLocalDevelopmentApiBaseUrl(storedApiBaseUrl);
}

const wpfThemes = [
  {
    key: "apex",
    name: "Apex",
    colors: {
      bg: "#090909",
      surface: "#131313",
      surface2: "#1c1c1c",
      sidebar: "#050505",
      input: "#0e0e0e",
      line: "#2a2a2a",
      text: "#f2f1ee",
      muted: "#9b9b9b",
      soft: "#6b6b6b",
      accent: "#e2231a",
      accentViolet: "#ffb400",
      accent2: "#2bd97e",
      whatsapp: "#25d366",
      danger: "#ff3b30"
    }
  },
  {
    key: "aurora",
    name: "Aurora",
    colors: {
      bg: "#080c14",
      surface: "#0e1420",
      surface2: "#141c2e",
      sidebar: "#060a12",
      input: "#0e1420",
      line: "#1a2840",
      text: "#e8f0ff",
      muted: "#7a8faf",
      soft: "#4a5a78",
      accent: "#00c9a7",
      accentViolet: "#7c5cfc",
      accent2: "#25d366",
      whatsapp: "#25d366",
      danger: "#ff6b6b"
    }
  },
  {
    key: "carbon",
    name: "Carbon",
    colors: {
      bg: "#101114",
      surface: "#17191f",
      surface2: "#20232b",
      sidebar: "#121318",
      input: "#0f1014",
      line: "#313642",
      text: "#f4f5f7",
      muted: "#a9afbd",
      soft: "#737b8c",
      accent: "#ff5722",
      accent2: "#25d366",
      whatsapp: "#25d366",
      danger: "#ff6b5f"
    }
  },
  {
    key: "amg",
    name: "AMG",
    colors: {
      bg: "#111114",
      surface: "#1A1A1E",
      surface2: "#242428",
      sidebar: "#151518",
      input: "#101014",
      line: "#3f3f48",
      text: "#F0F0F2",
      muted: "#8888A0",
      soft: "#6f6f82",
      accent: "#C8C8D0",
      whatsapp: "#25d366",
      danger: "#ff6b5f"
    }
  },
  {
    key: "bmw-m",
    name: "BMW M",
    colors: {
      bg: "#0D0D12",
      surface: "#14141C",
      surface2: "#1E1E2C",
      sidebar: "#101018",
      input: "#0a0a10",
      line: "#283d66",
      text: "#E8EAED",
      muted: "#8899BB",
      soft: "#66779a",
      accent: "#1C69D4",
      whatsapp: "#25d366",
      danger: "#ff6b5f"
    }
  },
  {
    key: "lambo",
    name: "Lambo",
    colors: {
      bg: "#090909",
      surface: "#131313",
      surface2: "#1E1E1E",
      sidebar: "#0f0f0f",
      input: "#0a0a0a",
      line: "#3f3a18",
      text: "#F5F5F0",
      muted: "#888878",
      soft: "#6e6e5e",
      accent: "#FFD600",
      whatsapp: "#25d366",
      danger: "#ff6b5f"
    }
  },
  {
    key: "neon-glow",
    name: "Neon Glow",
    colors: {
      bg: "#080810",
      surface: "#0D0D1A",
      surface2: "#141428",
      sidebar: "#0a0a16",
      input: "#070712",
      line: "#114759",
      text: "#E0F7FA",
      muted: "#4DD0E1",
      soft: "#3497a8",
      accent: "#00E5FF",
      whatsapp: "#25d366",
      danger: "#ff6b8a"
    }
  },
  {
    key: "porsche-rs",
    name: "Porsche RS",
    colors: {
      bg: "#0C0C0E",
      surface: "#181818",
      surface2: "#222222",
      sidebar: "#111111",
      input: "#0b0b0c",
      line: "#4d1b20",
      text: "#F2F2F2",
      muted: "#888888",
      soft: "#707070",
      accent: "#E30613",
      whatsapp: "#25d366",
      danger: "#ff6b5f"
    }
  }
];

const themeMap = new Map(wpfThemes.map((theme) => [theme.key, theme]));

const appLanguages = [
  { key: "en", name: "English", shortName: "EN" },
  { key: "ar", name: "Arabic", shortName: "AR" },
  { key: "fr", name: "French", shortName: "FR" }
];

const languageMap = new Map(appLanguages.map((language) => [language.key, language]));

const managementSections = [
  { key: "customers", label: "Customers", endpoint: "/api/customers?page=1&pageSize=100" },
  { key: "suppliers", label: "Suppliers", endpoint: "/api/suppliers?page=1&pageSize=100" },
  { key: "brands", label: "Brands", endpoint: "/api/brands?page=1&pageSize=100" },
  { key: "parts", label: "Parts", endpoint: "/api/parts?page=1&pageSize=100" },
  { key: "part-requests", label: "Part Requests", endpoint: "/api/partrequests" },
  { key: "car-brands", label: "Car Brands", endpoint: "/api/carbrands?page=1&pageSize=100" },
  { key: "car-models", label: "Car Models", endpoint: "/api/carmodels?page=1&pageSize=100" },
  { key: "users", label: "Users", endpoint: "/api/users" },
  { key: "warehouses", label: "Warehouses", endpoint: "/api/warehouses" },
  { key: "locations", label: "Locations", endpoint: "/api/locations" },
  { key: "currencies", label: "Currencies", endpoint: "/api/currencies" },
  { key: "roles", label: "Roles", endpoint: "/api/roles" },
  { key: "transaction-types", label: "Transaction Types", endpoint: "/api/transactiontypes" },
  { key: "categories", label: "Categories", endpoint: "/api/categories" }
];

const featureModules = [
  { key: "dashboard", label: "Dashboard", title: "Owner Cockpit", endpoint: "/api/owner-cockpit", capabilities: ["Sales, profit, cash, debts, and stock value", "Unpaid transactions", "Recent communications"] },
  { key: "invoices", label: "POS / Sales", title: "POS / Sales Invoices", endpoint: "/api/sales", capabilities: ["Create invoices", "Search invoices", "Send invoice by WhatsApp", "Send payment reminder"] },
  { key: "sales-returns", label: "Sales Returns", title: "Sales Returns", endpoint: "/api/sales-returns", capabilities: ["Browse return records", "Open return details", "Review credit and refund values"] },
  { key: "parts", label: "Parts", title: "Parts Inventory", endpoint: "/api/parts", capabilities: ["Browse parts", "Filter by code, name, and OEM", "Send part availability"] },
  { key: "part-passport", label: "Part Passport", title: "Part Passport", endpoint: "/api/parts + /api/usedcars", capabilities: ["Select inventory part", "Review public proof card", "Prepare WhatsApp-ready passport link"] },
  { key: "compatibility", label: "Compatibility", title: "Part Compatibility", endpoint: "/api/parts + /api/usedcars", capabilities: ["Visual part-to-vehicle graph", "OEM and donor-car fitment evidence", "Model/year sales assist"] },
  { key: "part-requests", label: "Part Requests", title: "Parts Request Board", endpoint: "/api/partrequests", capabilities: ["Unavailable-part demand", "Ready-to-contact signals", "Customer follow-up list"] },
  { key: "contacts", label: "Contacts", title: "Customers and Suppliers", endpoint: "/api/customers", capabilities: ["Customer list", "Supplier list", "Opening balances"] },
  { key: "management", label: "Management", title: "Management", endpoint: "/api/customers", capabilities: ["Customers, suppliers, brands, parts", "Car brands and models", "Users, roles, warehouses, and setup data"] },
  { key: "settings", label: "Settings", title: "Settings", endpoint: "", capabilities: ["Theme selection", "Language selection", "Account sign out"] },
  { key: "purchase-parts", label: "Part Purchases", title: "Part Purchases", endpoint: "/api/purchases", capabilities: ["Purchase invoice history", "Purchase invoice details", "Create and update purchase invoices"] },
  { key: "used-car-purchases", label: "Used Car Purchases", title: "Used Car Purchases", endpoint: "/api/purchases/used-cars", capabilities: ["Used car purchase history", "Post purchased vehicles", "Delete draft purchases"] },
  { key: "used-car-wholesale", label: "Used Car Wholesale", title: "Used Car Wholesale", endpoint: "/api/usedcars + /api/usedcars/wholesale-sales + /api/customers", capabilities: ["Sell complete used cars as-is", "Capture buyer and payment details", "Track wholesale margin against loaded cost"] },
  { key: "stock-arrival", label: "Stock Arrival", title: "Stock Arrival Theater", endpoint: "/api/parts + /api/partrequests + /api/communications/campaign-assets", capabilities: ["New opportunity board", "Photo and pricing queues", "Waiting customer and campaign signals"] },
  { key: "used-cars", label: "Used Cars", title: "Used Cars and Galleries", endpoint: "/api/usedcars", capabilities: ["Used car records", "Vehicle image galleries", "Vehicle-linked parts"] },
  { key: "car-twin", label: "Vehicle Digital Twin", title: "Vehicle Digital Twin", endpoint: "/api/usedcars + /api/usedcars/{id}/twin + /api/usedcars/{id}/state-events", capabilities: ["Track condition changes", "Review twin timeline and removed parts", "Record new state events"] },
  { key: "repair-prep", label: "Repair / Prep", title: "Repair / Prep Board", endpoint: "/api/usedcars", capabilities: ["Repair prep lanes", "Per-car task checklist", "Prep cost tracking"] },
  { key: "stock", label: "Stock", title: "Stock Management", endpoint: "/api/parts?page=1&pageSize=100", capabilities: ["Stock list", "Used-car part assignment", "AI generated part notes"] },
  { key: "dead-stock", label: "Dead Stock", title: "Dead Stock Recovery", endpoint: "/api/parts/dead-stock", capabilities: ["Dormant stock candidates", "Recovery actions", "Shelf-value summary"] },
  { key: "growth-lab", label: "Money Finder", title: "Money Finder Lab", endpoint: "/api/growth/briefing", capabilities: ["Tonight's money queue", "Donor-car treasure map", "Auction simulator", "Teardown queue", "Duplicate detection", "Buying radar", "WhatsApp voice-to-quote"] },
  { key: "accounting", label: "Accounting", title: "Accounting Review", endpoint: "/api/accounting/trial-balance", capabilities: ["Ledger", "Trial balance", "Statements of account"] },
  { key: "manual-journal", label: "Manual Journal", title: "Manual Journal", endpoint: "/api/accounting/journal-entries", capabilities: ["Journal entry history", "Manual journal posting", "Account configuration"] },
  { key: "report-builder", label: "Report Builder", title: "Report Builder", endpoint: "/api/reportbuilder/saved-reports", capabilities: ["Schema explorer", "Saved reports", "Background runs"] },
  { key: "whatsapp", label: "WhatsApp", title: "WhatsApp Conversations", endpoint: "/api/communications/conversations", capabilities: ["Conversation list", "Thread history", "Free-text outbound messages"] },
  { key: "business-assistant", label: "AI Assistant", title: "AI Business Assistant", endpoint: "/api/business-assistant/ask", capabilities: ["Turn answers into actions", "Create reports and customer reminders", "Draft purchase orders and campaigns", "Build natural-language stock reports"] },
  { key: "ar", label: "AR Search", title: "AR Picture Search", endpoint: "/api/scans/resolve + /api/scans/visual-search", capabilities: ["Search parts by camera photo", "Overlay ranked matches on the captured image", "Generate printable labels and sell scanned parts"] },
  { key: "reorder", label: "Reorder Center", title: "Reorder Center", endpoint: "/api/reorder/suggestions", capabilities: ["Parts below reorder point", "Suggested order quantities", "Preferred supplier details"] },
  { key: "expiry-alerts", label: "Expiry Alerts", title: "Expiry Alerts", endpoint: "/api/parts/expiry/alerts", capabilities: ["Expired parts list", "Parts expiring within 30 days", "Parts expiring within 90 days"] },
  { key: "loyalty", label: "Loyalty", title: "Customer Loyalty", endpoint: "/api/loyalty/customers/top", capabilities: ["Top loyalty customers by points", "Points balance overview", "Redemption tracking"] },
  { key: "warranty", label: "Warranty & Returns", title: "Warranty Claims", endpoint: "/api/warranty", capabilities: ["Active warranty claims", "Resolved claims history", "Create and track returns"] },
  { key: "shipments", label: "Shipments", title: "Shipments", endpoint: "/api/shipments", capabilities: ["Pending shipments list", "Shipment status tracking", "Event history per shipment"] },
  { key: "activity-log", label: "Activity Log", title: "Activity Log", endpoint: "/api/activity-log", capabilities: ["Recent activity feed", "Filter by entity type", "Full audit trail"] },
  { key: "quotes", label: "Quotes / Estimates", title: "Quotes / Estimates", endpoint: "/api/quotes", capabilities: ["Draft and sent quotes", "Quote-to-sale conversion", "Expiry tracking"] },
  { key: "customer-aging", label: "Customer Aging", title: "Customer Aging", endpoint: "/api/customers/aging", capabilities: ["Outstanding balances by customer", "0/30/60/90+ day aging buckets", "Overdue receivables overview"] },
  { key: "supplier-aging", label: "Supplier Aging", title: "Supplier Aging", endpoint: "/api/suppliers/aging", capabilities: ["Outstanding balances by supplier", "0/30/60/90+ day aging buckets", "Overdue payables overview"] },
  { key: "my-garage", label: "My Garage", title: "My Garage", endpoint: "/api/vehicle-profile", capabilities: ["Saved customer vehicles", "Default vehicle selection", "VIN-based garage profile"] },
  { key: "needboard", label: "NeedBoard", title: "NeedBoard", endpoint: "/api/needboard", capabilities: ["Browse buyer requests", "Post new demand", "Respond with seller offers"] },
  { key: "watchlist", label: "WatchPart", title: "WatchPart", endpoint: "/api/watchlist", capabilities: ["Track wanted parts", "View incoming match counts", "Remove fulfilled watch items"] },
  { key: "seller-reputation", label: "Shop Reputation", title: "Shop Reputation", endpoint: "/api/seller-reputation", capabilities: ["Seller reputation score", "Leaderboard comparison", "Fulfillment and dispute signals"] },
  { key: "seller-verification", label: "Seller Verification", title: "Seller Verification", endpoint: "/api/seller-verification", capabilities: ["Verification status", "Business document submission", "Admin review notes"] },
  { key: "symptom-search", label: "Symptom Finder", title: "Symptom Finder", endpoint: "/api/symptom-search", commandOnly: true, capabilities: ["Translate reported symptoms into likely parts", "Prioritize repair leads", "Guide staff toward fitment checks"] },
  { key: "mechanic-desk", label: "Mechanic Desk", title: "Mechanic Desk", endpoint: "/api/mechanic-desk", capabilities: ["Track mechanic orders", "Send quote-ready requests", "Accept winning mechanic quotes"] },
  { key: "garage-stock", label: "Garage Stock", title: "Garage Stock", endpoint: "/api/garage-stock", capabilities: ["Maintain garage-held stock", "Adjust reserved quantities", "Remove stale stock records"] },
  { key: "part-reserve", label: "Part Reservations", title: "Part Reservations", endpoint: "/api/part-reservations", capabilities: ["Create customer reservations", "Review held inventory", "Release expired holds"] },
  { key: "part-reel", label: "Part Reels", title: "Part Reels", endpoint: "/api/part-reels", capabilities: ["Publish short-form part promos", "Track likes and traction", "Archive spent reels"] },
  { key: "whatsapp-selling", label: "WhatsApp Selling", title: "WhatsApp Selling", endpoint: "/api/marketplace/catalog-export", commandOnly: true, capabilities: ["Set seller WhatsApp contact", "Generate catalog export payloads", "Prepare marketplace-ready selling assets"] },
  { key: "halfcut", label: "Half-Cut Showcase", title: "Half-Cut Showcase", endpoint: "/api/half-cut", capabilities: ["List half-cut inventory", "Post new half-cut offers", "Manage buyer claims and confirmations"] },
  { key: "car-crush", label: "CarCrush", title: "CarCrush", endpoint: "/api/car-crush/checklist", commandOnly: true, capabilities: ["Decode donor vehicles", "Generate dismantling checklists", "Create salvage listing packs"] },
  { key: "escrow", label: "Escrow / Protection", title: "Escrow / Protection", endpoint: "/api/escrow", capabilities: ["Open protected transactions", "Track escrow status changes", "Release or cancel protected deals"] },
  { key: "market-price", label: "Market Price Index", title: "Market Price Index", endpoint: "/api/market-price", capabilities: ["Review recent market pricing", "Filter by fitment signals", "Benchmark pricing decisions"] },
  { key: "listing-boost", label: "Boost Listings", title: "Boost Listings", endpoint: "/api/listing-boosts", capabilities: ["Create paid listing boosts", "Monitor campaign windows", "Cancel weak-performing boosts"] },
  { key: "referral", label: "Referral Program", title: "Referral Program", endpoint: "/api/referral/my-code + /api/referral/my-referrals", capabilities: ["Generate referral codes", "Review invited accounts", "Track referral outcomes"] },
  { key: "voice-search", label: "Voice Search", title: "Voice Search", endpoint: "/api/voice-search", commandOnly: true, capabilities: ["Search inventory by spoken request", "Convert audio into part intent", "Reduce typing during counter sales"] },
  { key: "does-it-fit", label: "Does It Fit?", title: "Does It Fit?", endpoint: "/api/part-compatibility/check", commandOnly: true, capabilities: ["Check fitment for a chosen part", "Compare vehicle compatibility", "Create compatibility evidence"] },
  { key: "price-genius", label: "PriceGenius AI", title: "PriceGenius AI", endpoint: "/api/price-genius/suggest", commandOnly: true, capabilities: ["Suggest selling prices", "Blend stock and market signals", "Support fast counter pricing"] },
  { key: "condition-scanner", label: "Condition Scanner", title: "Condition Scanner", endpoint: "/api/condition-scanner/scan", commandOnly: true, capabilities: ["Assess condition from photos", "Summarize detected wear", "Support pricing and disclosure"] },
  { key: "community-guard", label: "CommunityGuard", title: "CommunityGuard", endpoint: "/api/community-guard", capabilities: ["Review community reports", "File abuse or trust incidents", "Monitor marketplace safety signals"] },
  { key: "live-inspection", label: "Live Inspection", title: "Live Inspection", endpoint: "/api/live-inspection", capabilities: ["Queue live buyer inspections", "Track buyer contact details", "Manage inspection notes"] },
  { key: "qr-tag", label: "QR Tag System", title: "QR Tag System", endpoint: "/api/qr-tag", commandOnly: true, capabilities: ["Generate QR labels for parts", "Fetch printable QR data", "Support scan-driven retrieval"] },
  { key: "part-genealogy", label: "Part Genealogy", title: "Part Genealogy", endpoint: "/api/part-genealogy", commandOnly: true, capabilities: ["Trace part lifecycle events", "Record chain-of-custody notes", "Explain donor and movement history"] },
  { key: "dismantler-forecast", label: "Dismantler Forecast", title: "Dismantler Forecast", endpoint: "/api/dismantler-forecast", commandOnly: true, capabilities: ["Forecast teardown demand", "Prioritize profitable dismantles", "Support used-car buying decisions"] },
  { key: "regional-demand", label: "Regional Demand Map", title: "Regional Demand Map", endpoint: "/api/regional-demand/top", capabilities: ["Review region demand hotspots", "Compare top-demand parts", "Guide stocking by geography"] },
  { key: "mechanic-trust", label: "MechanicTrust Network", title: "MechanicTrust Network", endpoint: "/api/mechanic-trust", capabilities: ["Manage mechanic trust profiles", "Verify network members", "Track specialties by brand and job type"] },
  { key: "new-vs-used", label: "New vs Used", title: "New vs Used", endpoint: "/api/new-vs-used", commandOnly: true, capabilities: ["Compare new and used pricing", "Override indexed prices", "Support customer trade-off conversations"] },
  { key: "yard-tour", label: "Live Yard Tour", title: "Live Yard Tour", endpoint: "/api/yard-tours", capabilities: ["Schedule yard livestreams", "Publish tour links", "Update tour status"] },
  { key: "negotiation", label: "AI Negotiation Bot", title: "AI Negotiation Bot", endpoint: "/api/negotiations", capabilities: ["Track offer sessions", "Post counter-offers", "Accept or reject deals"] },
  { key: "instant-offer", label: "InstantOffer", title: "InstantOffer", endpoint: "/api/instant-offers", capabilities: ["Create instant offers", "Monitor offer queue", "Patch offer status"] },
  { key: "part-insurance", label: "Part Insurance Add-On", title: "Part Insurance Add-On", endpoint: "/api/part-insurance/options", capabilities: ["Review add-on options", "Attach protection to a sale", "Explain covered parts policies"] },
  { key: "kareem", label: "AutoChat Kareem", title: "AutoChat Kareem", endpoint: "/api/kareem/chat", commandOnly: true, capabilities: ["Chat with customers in natural language", "Maintain multilingual conversation history", "Support sales and support replies"] },
  { key: "ar-finder", label: "AR Parts Finder", title: "AR Parts Finder", endpoint: "/api/ar-finder/scan", commandOnly: true, capabilities: ["Scan a scene for likely parts", "Filter by make and model", "Support visual search workflows"] },
  { key: "price-report", label: "B2B Price Report", title: "B2B Price Report", endpoint: "/api/price-report", commandOnly: true, capabilities: ["Build wholesale price reports", "Summarize market recommendations", "Prepare buyer-facing pricing output"] },
  { key: "api-platform", label: "API Platform", title: "API Platform", endpoint: "/api/api-platform/keys", capabilities: ["Issue API keys", "Review integration access", "Revoke compromised credentials"] }
];

const navigationGroups = [
  { title: "Core", keys: ["dashboard", "invoices", "sales-returns", "parts", "part-passport", "compatibility", "contacts", "management", "billing", "settings"] },
  { title: "Operations", keys: ["growth-lab", "part-requests", "purchase-parts", "used-car-purchases", "used-car-wholesale", "stock-arrival", "used-cars", "car-twin", "repair-prep", "stock", "dead-stock", "reorder", "expiry-alerts", "loyalty", "warranty", "shipments", "mechanic-desk", "garage-stock", "part-reserve", "part-reel", "halfcut", "escrow", "listing-boost", "live-inspection", "part-genealogy", "yard-tour", "instant-offer", "part-insurance"] },
  { title: "Finance", keys: ["accounting", "manual-journal", "report-builder", "quotes", "customer-aging", "supplier-aging", "market-price", "price-genius", "price-report", "new-vs-used"] },
  { title: "Tools", keys: ["whatsapp", "whatsapp-selling", "business-assistant", "ar", "activity-log", "voice-search", "symptom-search", "does-it-fit", "condition-scanner", "qr-tag", "ar-finder", "api-platform", "kareem"] },
  { title: "Marketplace", keys: ["my-garage", "needboard", "watchlist", "seller-reputation", "seller-verification", "community-guard", "referral", "regional-demand", "mechanic-trust"] },
  { title: "Intelligence", keys: ["car-crush", "dismantler-forecast", "negotiation"] },
  { title: "Platform Admin", keys: ["admin-billing"] }
];

module.exports = {
  CommunicationChannel,
  CommunicationRecipientKind,
  CommunicationTemplateKey,
  appLanguages,
  configuredApiBaseUrl,
  defaultApiBaseUrl,
  defaultLanguageKey,
  defaultThemeKey,
  facebookAppId,
  featureModules,
  googleAndroidClientId,
  googleClientId,
  googleIosClientId,
  googleWebClientId,
  managementSections,
  navigationGroups,
  shouldUsePackagedApiBaseUrl,
  storageKeys,
  languageMap,
  themeMap,
  webAppRoleId,
  wpfThemes
};
