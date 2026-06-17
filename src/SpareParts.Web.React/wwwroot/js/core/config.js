export const storageKeys = {
  apiBaseUrl: "spareparts.web.apiBaseUrl",
  token: "spareparts.web.token",
  user: "spareparts.web.user",
  theme: "spareparts.web.theme",
  language: "spareparts.web.language"
};

export const defaultApiBaseUrl = window.SparePartsWebConfig?.defaultApiBaseUrl || "http://localhost:5000";
export const googleClientId = window.SparePartsWebConfig?.googleClientId || "";
export const facebookAppId = window.SparePartsWebConfig?.facebookAppId || "";
export const defaultThemeKey = "aurora";
export const defaultLanguageKey = "en";

export const languageOptions = [
  { key: "en", name: "English" },
  { key: "ar", name: "Arabic" },
  { key: "fr", name: "French" }
];

export const wpfThemes = [
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
      accent2: "#25d366",
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
      accent2: "#25d366",
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
      accent2: "#25d366",
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
      accent2: "#25d366",
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
      accent2: "#25d366",
      danger: "#ff6b5f"
    }
  }
];

export const themeMap = new Map(wpfThemes.map((theme) => [theme.key, theme]));

export const featureModules = [
  {
    key: "dashboard",
    label: "Dashboard",
    title: "Owner Cockpit",
    source: "MainWindow dashboard",
    endpoint: "/api/owner-cockpit",
    capabilities: ["Sales, profit, cash, debts, and stock value", "Unpaid transactions", "Recent communications"]
  },
  {
    key: "invoices",
    label: "POS / Sales",
    title: "POS / Sales Invoices",
    source: "MainWindow POS and Search Invoices",
    endpoint: "/api/sales",
    capabilities: ["Create invoices", "Search invoices", "Send invoice by WhatsApp", "Send payment reminder"]
  },
  {
    key: "sales-returns",
    label: "Sales Returns",
    title: "Sales Returns",
    source: "Web sales return workspace",
    endpoint: "/api/sales-returns",
    capabilities: ["Browse return records", "Open return details", "Review credit and refund values"]
  },
  {
    key: "inventory",
    label: "Parts",
    title: "Parts Inventory",
    source: "MainWindow stock and parts workspace",
    endpoint: "/api/parts",
    capabilities: ["Browse parts", "Filter by code, name, and OEM", "Send part availability"]
  },
  {
    key: "part-passport",
    label: "Part Passport",
    title: "Part Passport",
    source: "Inventory passport workspace and public proof card",
    endpoint: "/api/parts + /api/usedcars",
    capabilities: ["Select inventory part", "Review public proof card", "Prepare WhatsApp-ready passport link"]
  },
  {
    key: "compatibility",
    label: "Compatibility",
    title: "Part Compatibility",
    source: "Parts and used-car fitment workspace",
    endpoint: "/api/parts + /api/usedcars",
    capabilities: ["Visual part-to-vehicle graph", "OEM and donor-car fitment evidence", "Model/year sales assist"]
  },
  {
    key: "part-requests",
    label: "Part Requests",
    title: "Parts Request Board",
    source: "ManagementWindow Part Requests tab",
    endpoint: "/api/partrequests",
    capabilities: ["Capture unavailable-part demand", "Match requests to stock", "Follow up when parts arrive"]
  },
  {
    key: "contacts",
    label: "Contacts",
    title: "Customers and Suppliers",
    source: "ManagementWindow contacts",
    endpoint: "/api/customers",
    capabilities: ["Customer list", "Supplier list", "Opening balances"]
  },
  {
    key: "management",
    label: "Management",
    title: "Management",
    source: "ManagementWindow tabs",
    endpoint: "/api/customers",
    capabilities: ["Customers, suppliers, brands, parts", "Car brands and models", "Users, roles, warehouses, and setup data"]
  },
  {
    key: "settings",
    label: "Settings",
    title: "Settings",
    source: "Web shell preferences",
    endpoint: "",
    capabilities: ["Theme selection", "Language selection", "Account sign out"]
  },
  {
    key: "purchase-parts",
    label: "Part Purchases",
    title: "Part Purchases",
    source: "MainWindow Part Purchases",
    endpoint: "/api/purchases",
    capabilities: ["Purchase invoice history", "Purchase invoice details", "Create and update purchase invoices"]
  },
  {
    key: "used-car-purchases",
    label: "Used Car Purchases",
    title: "Used Car Purchases",
    source: "MainWindow Used Car Purchases",
    endpoint: "/api/purchases/used-cars",
    capabilities: ["Used car purchase history", "Post purchased vehicles", "Delete draft purchases"]
  },
  {
    key: "used-car-wholesale",
    label: "Used Car Wholesale",
    title: "Used Car Wholesale",
    source: "MainWindow Used Car Wholesale",
    endpoint: "/api/usedcars + /api/usedcars/wholesale-sales + /api/customers",
    capabilities: ["Sell complete used cars as-is", "Capture buyer and payment details", "Track wholesale margin against loaded cost"]
  },
  {
    key: "stock-arrival",
    label: "Stock Arrival",
    title: "Stock Arrival Theater",
    source: "Purchase and used-car arrival workflow",
    endpoint: "/api/parts + /api/partrequests + /api/communications/campaign-assets",
    capabilities: ["New opportunity board", "Photo and pricing queues", "Waiting customer and campaign signals"]
  },
  {
    key: "used-cars",
    label: "Used Cars",
    title: "Used Cars and Galleries",
    source: "MainWindow used car parts and galleries",
    endpoint: "/api/usedcars",
    capabilities: ["Used car records", "Vehicle image galleries", "Vehicle-linked parts"]
  },
  {
    key: "repair-prep",
    label: "Repair / Prep",
    title: "Repair / Prep Board",
    source: "Used car repair and listing workflow",
    endpoint: "/api/usedcars",
    capabilities: ["Repair prep lanes", "Per-car task checklist", "Prep cost tracking"]
  },
  {
    key: "stock",
    label: "Stock",
    title: "Stock Management",
    source: "MainWindow Stock Management",
    endpoint: "/api/parts",
    capabilities: ["Stock list", "Used-car part assignment", "AI generated part notes"]
  },
  {
    key: "dead-stock",
    label: "Dead Stock",
    title: "Dead Stock Recovery",
    source: "MainWindow Dead Stock Resurrection",
    endpoint: "/api/parts/dead-stock",
    capabilities: ["Dormant stock candidates", "Recovery actions", "Shelf-value summary"]
  },
  {
    key: "growth-lab",
    label: "Money Finder",
    title: "Money Finder Lab",
    source: "Growth intelligence workspace",
    endpoint: "/api/growth/briefing",
    capabilities: ["Tonight's money queue", "Donor-car treasure map", "Auction simulator", "Teardown queue", "Duplicate detection", "Buying radar", "WhatsApp voice-to-quote"]
  },
  {
    key: "accounting",
    label: "Accounting",
    title: "Accounting Review",
    source: "MainWindow Accounting Review",
    endpoint: "/api/accounting/trial-balance",
    capabilities: ["Ledger", "Trial balance", "Statements of account"]
  },
  {
    key: "manual-journal",
    label: "Manual Journal",
    title: "Manual Journal",
    source: "MainWindow Manual Journal",
    endpoint: "/api/accounting/journal-entries",
    capabilities: ["Journal entry history", "Manual journal posting", "Account configuration"]
  },
  {
    key: "report-builder",
    label: "Report Builder",
    title: "Report Builder",
    source: "MainWindow Report Builder",
    endpoint: "/api/reportbuilder/saved-reports",
    capabilities: ["Schema explorer", "Saved reports", "Background runs"]
  },
  {
    key: "whatsapp",
    label: "WhatsApp",
    title: "WhatsApp Conversations",
    source: "MainWindow WhatsApp Conversations",
    endpoint: "/api/communications/conversations",
    capabilities: ["Conversation list", "Thread history", "Free-text outbound messages"]
  },
  {
    key: "business-assistant",
    label: "AI Assistant",
    title: "AI Business Assistant",
    source: "MainWindow AI Business Assistant",
    endpoint: "/api/business-assistant/ask",
    capabilities: ["Turn answers into actions", "Create reports and customer reminders", "Draft purchase orders and campaigns", "Build natural-language stock reports"]
  },
  {
    key: "ar",
    label: "AR Search",
    title: "AR Picture Search",
    source: "MainWindow AR Experience",
    endpoint: "/api/scans/resolve + /api/scans/visual-search",
    capabilities: ["Search parts by camera photo", "Overlay ranked matches on the captured image", "Generate printable labels and sell scanned parts"]
  },
  { key: "reorder", label: "Reorder Center", title: "Reorder Center", source: "MainWindow Reorder Center", endpoint: "/api/reorder/suggestions", capabilities: ["Parts below reorder point", "Suggested order quantities", "Preferred supplier details"] },
  { key: "expiry-alerts", label: "Expiry Alerts", title: "Expiry Alerts", source: "MainWindow Expiry Alerts", endpoint: "/api/parts/expiry/alerts", capabilities: ["Expired parts list", "Parts expiring within 30 days", "Parts expiring within 90 days"] },
  { key: "loyalty", label: "Loyalty", title: "Customer Loyalty", source: "MainWindow Loyalty", endpoint: "/api/loyalty/customers/top", capabilities: ["Top loyalty customers by points", "Points balance overview", "Redemption tracking"] },
  { key: "warranty", label: "Warranty & Returns", title: "Warranty Claims", source: "MainWindow Warranty", endpoint: "/api/warranty", capabilities: ["Active warranty claims", "Resolved claims history", "Create and track returns"] },
  { key: "shipments", label: "Shipments", title: "Shipments", source: "MainWindow Shipments", endpoint: "/api/shipments", capabilities: ["Pending shipments list", "Shipment status tracking", "Event history per shipment"] },
  { key: "activity-log", label: "Activity Log", title: "Activity Log", source: "MainWindow Activity Log", endpoint: "/api/activity-log", capabilities: ["Recent activity feed", "Filter by entity type", "Full audit trail"] },
  { key: "quotes", label: "Quotes / Estimates", title: "Quotes / Estimates", source: "Web quotes workspace", endpoint: "/api/quotes", capabilities: ["Draft and sent quotes", "Quote-to-sale conversion", "Expiry tracking"] },
  { key: "customer-aging", label: "Customer Aging", title: "Customer Aging", source: "Finance aging workspace", endpoint: "/api/customers/aging", capabilities: ["Outstanding balances by customer", "0/30/60/90+ day aging buckets", "Overdue receivables overview"] },
  { key: "supplier-aging", label: "Supplier Aging", title: "Supplier Aging", source: "Finance aging workspace", endpoint: "/api/suppliers/aging", capabilities: ["Outstanding balances by supplier", "0/30/60/90+ day aging buckets", "Overdue payables overview"] },
  { key: "my-garage", label: "My Garage", title: "My Garage", source: "Web customer garage workspace", endpoint: "/api/vehicle-profile", capabilities: ["Saved customer vehicles", "Default vehicle selection", "VIN-based garage profile"] },
  { key: "needboard", label: "NeedBoard", title: "NeedBoard", source: "Marketplace demand board", endpoint: "/api/needboard", capabilities: ["Browse buyer requests", "Post new demand", "Respond with seller offers"] },
  { key: "watchlist", label: "WatchPart", title: "WatchPart", source: "Wanted parts watchlist", endpoint: "/api/watchlist", capabilities: ["Track wanted parts", "View incoming match counts", "Remove fulfilled watch items"] },
  { key: "seller-reputation", label: "Shop Reputation", title: "Shop Reputation", source: "Seller performance workspace", endpoint: "/api/seller-reputation", capabilities: ["Seller reputation score", "Leaderboard comparison", "Fulfillment and dispute signals"] },
  { key: "seller-verification", label: "Seller Verification", title: "Seller Verification", source: "Seller onboarding workspace", endpoint: "/api/seller-verification", capabilities: ["Verification status", "Business document submission", "Admin review notes"] }
];

export const managementSections = [
  { key: "customers", label: "Customers", endpoint: "/api/customers?page=1&pageSize=100", source: "ManagementWindow Customers tab" },
  { key: "suppliers", label: "Suppliers", endpoint: "/api/suppliers?page=1&pageSize=100", source: "ManagementWindow Suppliers tab" },
  { key: "brands", label: "Brands", endpoint: "/api/brands?page=1&pageSize=100", source: "ManagementWindow Brands tab" },
  { key: "parts", label: "Parts", endpoint: "/api/parts?page=1&pageSize=100", source: "ManagementWindow Parts tab" },
  { key: "part-requests", label: "Part Requests", endpoint: "/api/partrequests", source: "ManagementWindow Part Requests tab" },
  { key: "car-brands", label: "Car Brands", endpoint: "/api/carbrands?page=1&pageSize=100", source: "ManagementWindow Car Brands tab" },
  { key: "car-models", label: "Car Models", endpoint: "/api/carmodels?page=1&pageSize=100", source: "ManagementWindow Car Models tab" },
  { key: "users", label: "Users", endpoint: "/api/users", source: "ManagementWindow Users tab" },
  { key: "warehouses", label: "Warehouses", endpoint: "/api/warehouses", source: "Management view model" },
  { key: "locations", label: "Locations", endpoint: "/api/locations", source: "Management view model" },
  { key: "currencies", label: "Currencies", endpoint: "/api/currencies", source: "Management view model" },
  { key: "roles", label: "Roles", endpoint: "/api/roles", source: "Security view model" },
  { key: "transaction-types", label: "Transaction Types", endpoint: "/api/transactiontypes", source: "Accounting setup view model" },
  { key: "categories", label: "Categories", endpoint: "/api/categories", source: "Inventory view model" }
];
