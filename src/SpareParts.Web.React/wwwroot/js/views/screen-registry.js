import { featureModules } from "../core/config.js";
import { ScreenRegistry } from "../core/screen-registry.js";
import { ContactsView } from "./contacts-view.js";
import { DashboardView } from "./dashboard-view.js";
import { DeadStockView } from "./dead-stock-view.js";
import { InventoryView } from "./inventory-view.js";
import { InvoicesView } from "./invoices-view.js";
import { ManagementWorkspaceView } from "./management-view.js";
import { PartCompatibilityView } from "./part-compatibility-view.js";
import { PartRequestsView } from "./part-requests-view.js";
import { RepairPrepBoardView } from "./repair-prep-board-view.js";
import { AccountingView } from "./accounting-view.js";
import { BarcodeModeView } from "./barcode-mode-view.js";
import { SettingsView } from "./settings-view.js";
import { UsedCarsView } from "./used-cars-view.js";
import { WhatsAppView } from "./whatsapp-view.js";
import { createModuleView } from "./module-workspace-view.js";

function moduleByKey(key) {
  return featureModules.find((module) => module.key === key);
}

export const screenRegistry = new ScreenRegistry([
  { key: "dashboard", label: "Dashboard", component: DashboardView },
  { key: "invoices", label: "POS / Sales", component: InvoicesView },
  { key: "inventory", label: "Parts", component: InventoryView },
  { key: "compatibility", label: "Compatibility", component: PartCompatibilityView },
  { key: "part-requests", label: "Part Requests", component: PartRequestsView },
  { key: "contacts", label: "Contacts", component: ContactsView },
  { key: "management", label: "Management", component: ManagementWorkspaceView },
  { key: "settings", label: "Settings", component: SettingsView },
  { key: "purchase-parts", label: "Part Purchases", component: createModuleView(moduleByKey("purchase-parts")) },
  { key: "used-car-purchases", label: "Used Car Purchases", component: createModuleView(moduleByKey("used-car-purchases")) },
  { key: "used-cars", label: "Used Cars", component: UsedCarsView },
  { key: "repair-prep", label: "Repair / Prep", component: RepairPrepBoardView },
  { key: "stock", label: "Stock", component: createModuleView(moduleByKey("stock")) },
  { key: "dead-stock", label: "Dead Stock", component: DeadStockView },
  { key: "accounting", label: "Accounting", component: AccountingView },
  { key: "manual-journal", label: "Manual Journal", component: createModuleView(moduleByKey("manual-journal")) },
  { key: "report-builder", label: "Report Builder", component: createModuleView(moduleByKey("report-builder")) },
  { key: "whatsapp", label: "WhatsApp", component: WhatsAppView },
  { key: "business-assistant", label: "AI Assistant", component: createModuleView(moduleByKey("business-assistant")) },
  { key: "ar", label: "Barcode / QR", component: BarcodeModeView }
]);
