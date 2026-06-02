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

const defaultApiBaseUrl =
  process.env.EXPO_PUBLIC_API_BASE_URL ||
  (Platform.OS === "android" ? "http://10.0.2.2:5000" : "http://localhost:5000");
const defaultThemeKey = "aurora";
const defaultLanguageKey = "en";
const googleClientId = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID || "";
const googleAndroidClientId = process.env.EXPO_PUBLIC_GOOGLE_ANDROID_CLIENT_ID || "";
const googleIosClientId = process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID || "";
const googleWebClientId = process.env.EXPO_PUBLIC_GOOGLE_WEB_CLIENT_ID || googleClientId;
const facebookAppId = process.env.EXPO_PUBLIC_FACEBOOK_APP_ID || "";
const webAppRoleId = 4;

const wpfThemes = [
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
      whatsapp: "#25d366",
      danger: "#ff6b6b"
    }
  },
  {
    key: "default",
    name: "Default",
    colors: {
      bg: "#07080b",
      surface: "#0c0f14",
      surface2: "#111620",
      sidebar: "#08090d",
      input: "#060709",
      line: "#1c2230",
      text: "#edf1f9",
      muted: "#7585a0",
      soft: "#404c60",
      accent: "#e85012",
      whatsapp: "#22c55e",
      danger: "#f43f5e"
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
  { key: "part-passport", label: "Part Passport", title: "Part Passport", endpoint: "/api/parts + /api/usedcars", capabilities: ["Select inventory part", "Review public proof card", "Prepare WhatsApp-ready passport link"] },
  { key: "compatibility", label: "Compatibility", title: "Part Compatibility", endpoint: "/api/parts + /api/usedcars", capabilities: ["Visual part-to-vehicle graph", "OEM and donor-car fitment evidence", "Model/year sales assist"] },
  { key: "part-requests", label: "Part Requests", title: "Parts Request Board", endpoint: "/api/partrequests", capabilities: ["Unavailable-part demand", "Ready-to-contact signals", "Customer follow-up list"] },
  { key: "purchase-parts", label: "Part Purchases", title: "Part Purchases", endpoint: "/api/purchases", capabilities: ["Purchase invoice history", "Purchase invoice details", "Create and update purchase invoices"] },
  { key: "used-car-purchases", label: "Used Car Purchases", title: "Used Car Purchases", endpoint: "/api/purchases/used-cars", capabilities: ["Used car purchase history", "Post purchased vehicles", "Delete draft purchases"] },
  { key: "used-car-wholesale", label: "Used Car Wholesale", title: "Used Car Wholesale", endpoint: "/api/usedcars + /api/usedcars/wholesale-sales + /api/customers", capabilities: ["Sell complete used cars as-is", "Capture buyer and payment details", "Track wholesale margin against loaded cost"] },
  { key: "stock-arrival", label: "Stock Arrival", title: "Stock Arrival Theater", endpoint: "/api/parts + /api/partrequests + /api/communications/campaign-assets", capabilities: ["New opportunity board", "Photo and pricing queues", "Waiting customer and campaign signals"] },
  { key: "used-cars", label: "Used Cars", title: "Used Cars", endpoint: "/api/usedcars", capabilities: ["Used car records", "Vehicle image galleries", "Vehicle-linked parts"] },
  { key: "repair-prep", label: "Repair / Prep", title: "Repair / Prep Board", endpoint: "/api/usedcars", capabilities: ["Repair prep lanes", "Per-car task checklist", "Prep cost tracking"] },
  { key: "stock", label: "Stock", title: "Stock Management", endpoint: "/api/parts?page=1&pageSize=100", capabilities: ["Stock list", "Used-car part assignment", "AI generated part notes"] },
  { key: "dead-stock", label: "Dead Stock", title: "Dead Stock Recovery", endpoint: "/api/parts/dead-stock", capabilities: ["Dormant stock candidates", "Recovery actions", "Shelf-value summary"] },
  { key: "growth-lab", label: "Money Finder", title: "Money Finder Lab", endpoint: "/api/growth/briefing", capabilities: ["Tonight's money queue", "Donor-car treasure map", "Auction simulator", "Teardown queue", "Duplicate detection", "Buying radar", "WhatsApp voice-to-quote"] },
  { key: "accounting", label: "Accounting", title: "Accounting Review", endpoint: "/api/accounting/trial-balance", capabilities: ["Ledger", "Trial balance", "Statements of account"] },
  { key: "manual-journal", label: "Manual Journal", title: "Manual Journal", endpoint: "/api/accounting/journal-entries", capabilities: ["Journal entry history", "Manual journal posting", "Account configuration"] },
  { key: "report-builder", label: "Report Builder", title: "Report Builder", endpoint: "/api/reportbuilder/saved-reports", capabilities: ["Schema explorer", "Saved reports", "Background runs"] },
  { key: "business-assistant", label: "AI Assistant", title: "AI Business Assistant", endpoint: "/api/business-assistant/ask", capabilities: ["Turn answers into actions", "Create reports and customer reminders", "Draft purchase orders and campaigns", "Build natural-language stock reports"] },
  { key: "ar", label: "AR Search", title: "AR Picture Search", endpoint: "/api/scans/resolve + /api/scans/visual-search", capabilities: ["Search parts by camera photo", "Overlay ranked matches on the captured image", "Generate printable labels and sell scanned parts"] },
  { key: "reorder", label: "Reorder Center", title: "Reorder Center", endpoint: "/api/reorder/suggestions", capabilities: ["Parts below reorder point", "Suggested order quantities", "Preferred supplier details"] },
  { key: "expiry-alerts", label: "Expiry Alerts", title: "Expiry Alerts", endpoint: "/api/parts/expiry/alerts", capabilities: ["Expired parts list", "Parts expiring within 30 days", "Parts expiring within 90 days"] },
  { key: "loyalty", label: "Loyalty", title: "Customer Loyalty", endpoint: "/api/loyalty/customers/top", capabilities: ["Top loyalty customers by points", "Points balance overview", "Redemption tracking"] },
  { key: "warranty", label: "Warranty & Returns", title: "Warranty Claims", endpoint: "/api/warranty", capabilities: ["Active warranty claims", "Resolved claims history", "Create and track returns"] },
  { key: "shipments", label: "Shipments", title: "Shipments", endpoint: "/api/shipments", capabilities: ["Pending shipments list", "Shipment status tracking", "Event history per shipment"] },
  { key: "activity-log", label: "Activity Log", title: "Activity Log", endpoint: "/api/activity-log", capabilities: ["Recent activity feed", "Filter by entity type", "Full audit trail"] }
];

const navigationGroups = [
  { title: "Core", keys: ["dashboard", "invoices", "parts", "part-passport", "compatibility", "contacts", "management", "settings"] },
  { title: "Operations", keys: ["growth-lab", "part-requests", "purchase-parts", "used-car-purchases", "used-car-wholesale", "stock-arrival", "used-cars", "repair-prep", "stock", "dead-stock", "reorder", "expiry-alerts", "loyalty", "warranty", "shipments"] },
  { title: "Finance", keys: ["accounting", "manual-journal", "report-builder"] },
  { title: "Tools", keys: ["whatsapp", "business-assistant", "ar", "activity-log"] }
];

module.exports = {
  CommunicationChannel,
  CommunicationRecipientKind,
  CommunicationTemplateKey,
  appLanguages,
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
  storageKeys,
  languageMap,
  themeMap,
  webAppRoleId,
  wpfThemes
};
