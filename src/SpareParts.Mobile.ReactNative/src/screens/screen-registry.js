const { featureModules } = require("../core/app-config");
const { ScreenRegistry } = require("../core/screen-registry");
const { AccountingScreen } = require("./accounting-screen");
const { ContactsScreen } = require("./contacts-screen");
const { DashboardScreen } = require("./dashboard-screen");
const { DeadStockScreen } = require("./dead-stock-screen");
const { InvoicesScreen } = require("./invoices-screen");
const { ManagementScreen } = require("./management-screen");
const { PartCompatibilityScreen } = require("./part-compatibility-screen");
const { PartsScreen } = require("./parts-screen");
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
  { key: "parts", label: "Parts", component: PartsScreen },
  { key: "compatibility", label: "Compatibility", component: PartCompatibilityScreen },
  { key: "contacts", label: "Contacts", component: ContactsScreen },
  { key: "management", label: "Management", component: ManagementScreen },
  { key: "settings", label: "Settings", component: SettingsScreen },
  { key: "part-requests", label: "Part Requests", component: createModuleScreen(moduleByKey("part-requests")) },
  { key: "purchase-parts", label: "Part Purchases", component: createModuleScreen(moduleByKey("purchase-parts")) },
  { key: "used-car-purchases", label: "Used Car Purchases", component: createModuleScreen(moduleByKey("used-car-purchases")) },
  { key: "used-cars", label: "Used Cars", component: UsedCarsScreen },
  { key: "repair-prep", label: "Repair / Prep", component: RepairPrepScreen },
  { key: "stock", label: "Stock", component: createModuleScreen(moduleByKey("stock")) },
  { key: "dead-stock", label: "Dead Stock", component: DeadStockScreen },
  { key: "accounting", label: "Accounting", component: AccountingScreen },
  { key: "manual-journal", label: "Manual Journal", component: createModuleScreen(moduleByKey("manual-journal")) },
  { key: "report-builder", label: "Report Builder", component: createModuleScreen(moduleByKey("report-builder")) },
  { key: "whatsapp", label: "WhatsApp", component: WhatsAppScreen },
  { key: "business-assistant", label: "AI Assistant", component: createModuleScreen(moduleByKey("business-assistant")) },
  { key: "ar", label: "Barcode / QR", component: createModuleScreen(moduleByKey("ar")) }
]);

module.exports = { screenRegistry };
