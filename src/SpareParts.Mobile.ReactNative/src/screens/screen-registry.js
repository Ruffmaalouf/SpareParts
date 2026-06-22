const { featureModules } = require("../core/app-config");
const { ScreenRegistry } = require("../core/screen-registry");
const { AccountingScreen } = require("./accounting-screen");
const { AdminBillingScreen } = require("./admin-billing-screen");
const { BillingScreen } = require("./billing-screen");
const { ContactsScreen } = require("./contacts-screen");
const { DashboardScreen } = require("./dashboard-screen");
const { DeadStockScreen } = require("./dead-stock-screen");
const { InvoicesScreen } = require("./invoices-screen");
const { ManagementScreen } = require("./management-screen");
const { MechanicModeScreen } = require("./mechanic-mode-screen");
const { PartCompatibilityScreen } = require("./part-compatibility-screen");
const { RepairPrepScreen } = require("./repair-prep-screen");
const { SettingsScreen } = require("./settings-screen");
const { UsedCarsScreen } = require("./used-cars-screen");
const { WhatsAppScreen } = require("./whatsapp-screen");
const { createModuleScreen } = require("./module-screen");

function moduleByKey(key) {
  return featureModules.find((module) => module.key === key);
}

const screenRegistry = new ScreenRegistry([
  { key: "dashboard", label: "Dashboard", component: DashboardScreen },
  { key: "invoices", label: "POS / Sales", component: InvoicesScreen },
  { key: "sales-returns", label: "Sales Returns", component: createModuleScreen(moduleByKey("sales-returns")) },
  { key: "parts", label: "Parts", component: MechanicModeScreen },
  { key: "part-passport", label: "Part Passport", component: createModuleScreen(moduleByKey("part-passport")) },
  { key: "compatibility", label: "Compatibility", component: PartCompatibilityScreen },
  { key: "contacts", label: "Contacts", component: ContactsScreen },
  { key: "management", label: "Management", component: ManagementScreen },
  { key: "billing", label: "Billing & Subscription", component: BillingScreen },
  { key: "settings", label: "Settings", component: SettingsScreen },
  { key: "part-requests", label: "Part Requests", component: createModuleScreen(moduleByKey("part-requests")) },
  { key: "purchase-parts", label: "Part Purchases", component: createModuleScreen(moduleByKey("purchase-parts")) },
  { key: "used-car-purchases", label: "Used Car Purchases", component: createModuleScreen(moduleByKey("used-car-purchases")) },
  { key: "used-car-wholesale", label: "Used Car Wholesale", component: createModuleScreen(moduleByKey("used-car-wholesale")) },
  { key: "stock-arrival", label: "Stock Arrival", component: createModuleScreen(moduleByKey("stock-arrival")) },
  { key: "used-cars", label: "Used Cars", component: UsedCarsScreen },
  { key: "car-twin", label: "Vehicle Digital Twin", component: createModuleScreen(moduleByKey("car-twin")) },
  { key: "repair-prep", label: "Repair / Prep", component: RepairPrepScreen },
  { key: "stock", label: "Stock", component: createModuleScreen(moduleByKey("stock")) },
  { key: "dead-stock", label: "Dead Stock", component: DeadStockScreen },
  { key: "growth-lab", label: "Money Finder", component: createModuleScreen(moduleByKey("growth-lab")) },
  { key: "accounting", label: "Accounting", component: AccountingScreen },
  { key: "manual-journal", label: "Manual Journal", component: createModuleScreen(moduleByKey("manual-journal")) },
  { key: "report-builder", label: "Report Builder", component: createModuleScreen(moduleByKey("report-builder")) },
  { key: "whatsapp", label: "WhatsApp", component: WhatsAppScreen },
  { key: "business-assistant", label: "AI Assistant", component: createModuleScreen(moduleByKey("business-assistant")) },
  { key: "ar", label: "AR Search", component: createModuleScreen(moduleByKey("ar")) },
  { key: "reorder", label: "Reorder Center", component: createModuleScreen(moduleByKey("reorder")) },
  { key: "expiry-alerts", label: "Expiry Alerts", component: createModuleScreen(moduleByKey("expiry-alerts")) },
  { key: "loyalty", label: "Loyalty", component: createModuleScreen(moduleByKey("loyalty")) },
  { key: "warranty", label: "Warranty & Returns", component: createModuleScreen(moduleByKey("warranty")) },
  { key: "shipments", label: "Shipments", component: createModuleScreen(moduleByKey("shipments")) },
  { key: "activity-log", label: "Activity Log", component: createModuleScreen(moduleByKey("activity-log")) },
  { key: "quotes", label: "Quotes / Estimates", component: createModuleScreen(moduleByKey("quotes")) },
  { key: "customer-aging", label: "Customer Aging", component: createModuleScreen(moduleByKey("customer-aging")) },
  { key: "supplier-aging", label: "Supplier Aging", component: createModuleScreen(moduleByKey("supplier-aging")) },
  { key: "admin-billing", label: "Pricing & Subscriptions", component: AdminBillingScreen },
  { key: "my-garage", label: "My Garage", component: createModuleScreen(moduleByKey("my-garage")) },
  { key: "needboard", label: "NeedBoard", component: createModuleScreen(moduleByKey("needboard")) },
  { key: "watchlist", label: "WatchPart", component: createModuleScreen(moduleByKey("watchlist")) },
  { key: "seller-reputation", label: "Shop Reputation", component: createModuleScreen(moduleByKey("seller-reputation")) },
  { key: "seller-verification", label: "Seller Verification", component: createModuleScreen(moduleByKey("seller-verification")) },
  { key: "symptom-search", label: "Symptom Finder", component: createModuleScreen(moduleByKey("symptom-search")) },
  { key: "mechanic-desk", label: "Mechanic Desk", component: createModuleScreen(moduleByKey("mechanic-desk")) },
  { key: "garage-stock", label: "Garage Stock", component: createModuleScreen(moduleByKey("garage-stock")) },
  { key: "part-reserve", label: "Part Reservations", component: createModuleScreen(moduleByKey("part-reserve")) },
  { key: "part-reel", label: "Part Reels", component: createModuleScreen(moduleByKey("part-reel")) },
  { key: "whatsapp-selling", label: "WhatsApp Selling", component: createModuleScreen(moduleByKey("whatsapp-selling")) },
  { key: "halfcut", label: "Half-Cut Showcase", component: createModuleScreen(moduleByKey("halfcut")) },
  { key: "car-crush", label: "CarCrush", component: createModuleScreen(moduleByKey("car-crush")) },
  { key: "escrow", label: "Escrow / Protection", component: createModuleScreen(moduleByKey("escrow")) },
  { key: "market-price", label: "Market Price Index", component: createModuleScreen(moduleByKey("market-price")) },
  { key: "listing-boost", label: "Boost Listings", component: createModuleScreen(moduleByKey("listing-boost")) },
  { key: "referral", label: "Referral Program", component: createModuleScreen(moduleByKey("referral")) },
  { key: "voice-search", label: "Voice Search", component: createModuleScreen(moduleByKey("voice-search")) },
  { key: "does-it-fit", label: "Does It Fit?", component: createModuleScreen(moduleByKey("does-it-fit")) },
  { key: "price-genius", label: "PriceGenius AI", component: createModuleScreen(moduleByKey("price-genius")) },
  { key: "condition-scanner", label: "Condition Scanner", component: createModuleScreen(moduleByKey("condition-scanner")) },
  { key: "community-guard", label: "CommunityGuard", component: createModuleScreen(moduleByKey("community-guard")) },
  { key: "live-inspection", label: "Live Inspection", component: createModuleScreen(moduleByKey("live-inspection")) },
  { key: "qr-tag", label: "QR Tag System", component: createModuleScreen(moduleByKey("qr-tag")) },
  { key: "part-genealogy", label: "Part Genealogy", component: createModuleScreen(moduleByKey("part-genealogy")) },
  { key: "dismantler-forecast", label: "Dismantler Forecast", component: createModuleScreen(moduleByKey("dismantler-forecast")) },
  { key: "regional-demand", label: "Regional Demand Map", component: createModuleScreen(moduleByKey("regional-demand")) },
  { key: "mechanic-trust", label: "MechanicTrust Network", component: createModuleScreen(moduleByKey("mechanic-trust")) },
  { key: "new-vs-used", label: "New vs Used", component: createModuleScreen(moduleByKey("new-vs-used")) },
  { key: "yard-tour", label: "Live Yard Tour", component: createModuleScreen(moduleByKey("yard-tour")) },
  { key: "negotiation", label: "AI Negotiation Bot", component: createModuleScreen(moduleByKey("negotiation")) },
  { key: "instant-offer", label: "InstantOffer", component: createModuleScreen(moduleByKey("instant-offer")) },
  { key: "part-insurance", label: "Part Insurance Add-On", component: createModuleScreen(moduleByKey("part-insurance")) },
  { key: "kareem", label: "AutoChat Kareem", component: createModuleScreen(moduleByKey("kareem")) },
  { key: "ar-finder", label: "AR Parts Finder", component: createModuleScreen(moduleByKey("ar-finder")) },
  { key: "price-report", label: "B2B Price Report", component: createModuleScreen(moduleByKey("price-report")) },
  { key: "api-platform", label: "API Platform", component: createModuleScreen(moduleByKey("api-platform")) }
]);

module.exports = { screenRegistry };
