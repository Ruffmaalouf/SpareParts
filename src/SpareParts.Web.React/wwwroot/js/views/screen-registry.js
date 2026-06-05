import { featureModules } from "../core/config.js";
import { ScreenRegistry } from "../core/screen-registry.js";
import { ActivityLogView } from "./activity-log-view.js";
import { ContactsView } from "./contacts-view.js";
import { CustomerAgingView } from "./customer-aging-view.js";
import { DashboardView } from "./dashboard-view.js";
import { QuotesView } from "./quotes-view.js";
import { DeadStockView } from "./dead-stock-view.js";
import { ExpiryAlertsView } from "./expiry-alerts-view.js";
import { GrowthLabView } from "./growth-lab-view.js";
import { InventoryView } from "./inventory-view.js";
import { InvoicesView } from "./invoices-view.js";
import { LoyaltyView } from "./loyalty-view.js";
import { ManagementWorkspaceView } from "./management-view.js";
import { PartCompatibilityView } from "./part-compatibility-view.js";
import { PartPassportWorkspaceView } from "./part-passport-workspace-view.js";
import { PartRequestsView } from "./part-requests-view.js";
import { ReorderView } from "./reorder-view.js";
import { RepairPrepBoardView } from "./repair-prep-board-view.js";
import { ShipmentsView } from "./shipments-view.js";
import { AccountingView } from "./accounting-view.js";
import { BarcodeModeView } from "./barcode-mode-view.js";
import { SettingsView } from "./settings-view.js";
import { StockArrivalTheaterView } from "./stock-arrival-theater-view.js";
import { UsedCarsView } from "./used-cars-view.js";
import { WarrantyView } from "./warranty-view.js";
import { WhatsAppView } from "./whatsapp-view.js";
import { createModuleView } from "./module-workspace-view.js";

function moduleByKey(key) {
  return featureModules.find((module) => module.key === key);
}

export const screenRegistry = new ScreenRegistry([
  { key: "dashboard", label: "Dashboard", component: DashboardView },
  { key: "invoices", label: "POS / Sales", component: InvoicesView },
  { key: "inventory", label: "Parts", component: InventoryView },
  { key: "part-passport", label: "Part Passport", component: PartPassportWorkspaceView },
  { key: "compatibility", label: "Compatibility", component: PartCompatibilityView },
  { key: "part-requests", label: "Part Requests", component: PartRequestsView },
  { key: "contacts", label: "Contacts", component: ContactsView },
  { key: "management", label: "Management", component: ManagementWorkspaceView },
  { key: "settings", label: "Settings", component: SettingsView },
  { key: "purchase-parts", label: "Part Purchases", component: createModuleView(moduleByKey("purchase-parts")) },
  { key: "used-car-purchases", label: "Used Car Purchases", component: createModuleView(moduleByKey("used-car-purchases")) },
  { key: "used-car-wholesale", label: "Used Car Wholesale", component: createModuleView(moduleByKey("used-car-wholesale")) },
  { key: "stock-arrival", label: "Stock Arrival", component: StockArrivalTheaterView },
  { key: "used-cars", label: "Used Cars", component: UsedCarsView },
  { key: "repair-prep", label: "Repair / Prep", component: RepairPrepBoardView },
  { key: "stock", label: "Stock", component: createModuleView(moduleByKey("stock")) },
  { key: "dead-stock", label: "Dead Stock", component: DeadStockView },
  { key: "growth-lab", label: "Money Finder", component: GrowthLabView },
  { key: "accounting", label: "Accounting", component: AccountingView },
  { key: "manual-journal", label: "Manual Journal", component: createModuleView(moduleByKey("manual-journal")) },
  { key: "report-builder", label: "Report Builder", component: createModuleView(moduleByKey("report-builder")) },
  { key: "whatsapp", label: "WhatsApp", component: WhatsAppView },
  { key: "business-assistant", label: "AI Assistant", component: createModuleView(moduleByKey("business-assistant")) },
  { key: "ar", label: "AR Search", component: BarcodeModeView },
  { key: "reorder", label: "Reorder Center", component: ReorderView },
  { key: "expiry-alerts", label: "Expiry Alerts", component: ExpiryAlertsView },
  { key: "loyalty", label: "Loyalty", component: LoyaltyView },
  { key: "warranty", label: "Warranty & Returns", component: WarrantyView },
  { key: "shipments", label: "Shipments", component: ShipmentsView },
  { key: "activity-log", label: "Activity Log", component: ActivityLogView },
  { key: "quotes", label: "Quotes / Estimates", component: QuotesView },
  { key: "customer-aging", label: "Customer Aging", component: CustomerAgingView }
]);
