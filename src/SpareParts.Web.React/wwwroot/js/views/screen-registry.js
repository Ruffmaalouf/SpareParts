import { featureModules } from "../core/config.js";
import { ScreenRegistry } from "../core/screen-registry.js";
import { ActivityLogView } from "./activity-log-view.js";
import { AdminBillingView } from "./admin-billing-view.js";
import { BillingView } from "./billing-view.js";
import { CarTwinWorkspaceView } from "./car-twin-workspace-view.js";
import { ContactsView } from "./contacts-view.js";
import { CustomerAgingView } from "./customer-aging-view.js";
import { SupplierAgingView } from "./supplier-aging-view.js";
import { DashboardView } from "./dashboard-view.js";
import { QuotesView } from "./quotes-view.js";
import { DeadStockView } from "./dead-stock-view.js";
import { ExpiryAlertsView } from "./expiry-alerts-view.js";
import { GrowthLabView } from "./growth-lab-view.js";
import { InventoryView } from "./inventory-view.js";
import { InvoicesView } from "./invoices-view.js";
import { SalesReturnsView } from "./sales-returns-view.js";
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
import { MyGarageView } from "./my-garage-view.js";
import { NeedBoardView } from "./needboard-view.js";
import { WatchlistView } from "./watchlist-view.js";
import { SellerReputationView } from "./seller-reputation-view.js";
import { SellerVerificationView } from "./seller-verification-view.js";
import { SymptomSearchView } from "./symptom-search-view.js";
import { MechanicDeskView } from "./mechanic-desk-view.js";
import { GarageStockView } from "./garage-stock-view.js";
import { PartReserveView } from "./part-reserve-view.js";
import { PartReelView } from "./part-reel-view.js";
import { WhatsAppSellingView } from "./whatsapp-selling-view.js";
import { HalfCutView } from "./halfcut-view.js";
import { CarCrushView } from "./car-crush-view.js";
import { EscrowView } from "./escrow-view.js";
import { MarketPriceView } from "./market-price-view.js";
import { ListingBoostView } from "./listing-boost-view.js";
import { ReferralView } from "./referral-view.js";
import { VoiceSearchView } from "./voice-search-view.js";
import { DoesItFitView } from "./does-it-fit-view.js";
import { PriceGeniusView } from "./price-genius-view.js";
import { ConditionScannerView } from "./condition-scanner-view.js";
import { CommunityGuardView } from "./community-guard-view.js";
import { LiveInspectionView } from "./live-inspection-view.js";
import { QrTagView } from "./qr-tag-view.js";
import { PartGenealogyView } from "./part-genealogy-view.js";
import { DismantlerForecastView } from "./dismantler-forecast-view.js";
import { RegionalDemandView } from "./regional-demand-view.js";
import { MechanicTrustView } from "./mechanic-trust-view.js";
import { NewVsUsedView } from "./new-vs-used-view.js";
import { YardTourView } from "./yard-tour-view.js";
import { NegotiationView } from "./negotiation-view.js";
import { InstantOfferView } from "./instant-offer-view.js";
import { PartInsuranceView } from "./part-insurance-view.js";
import { KareemView } from "./kareem-view.js";
import { ArFinderView } from "./ar-finder-view.js";
import { PriceReportView } from "./price-report-view.js";
import { ApiPlatformView } from "./api-platform-view.js";

function moduleByKey(key) {
  return featureModules.find((module) => module.key === key);
}

export const screenRegistry = new ScreenRegistry([
  { key: "dashboard", label: "Dashboard", component: DashboardView },
  { key: "invoices", label: "POS / Sales", component: InvoicesView },
  { key: "sales-returns", label: "Sales Returns", component: SalesReturnsView },
  { key: "inventory", label: "Parts", component: InventoryView },
  { key: "part-passport", label: "Part Passport", component: PartPassportWorkspaceView },
  { key: "compatibility", label: "Compatibility", component: PartCompatibilityView },
  { key: "part-requests", label: "Part Requests", component: PartRequestsView },
  { key: "contacts", label: "Contacts", component: ContactsView },
  { key: "management", label: "Management", component: ManagementWorkspaceView },
  { key: "billing", label: "Billing & Subscription", component: BillingView },
  { key: "settings", label: "Settings", component: SettingsView },
  { key: "purchase-parts", label: "Part Purchases", component: createModuleView(moduleByKey("purchase-parts")) },
  { key: "used-car-purchases", label: "Used Car Purchases", component: createModuleView(moduleByKey("used-car-purchases")) },
  { key: "used-car-wholesale", label: "Used Car Wholesale", component: createModuleView(moduleByKey("used-car-wholesale")) },
  { key: "stock-arrival", label: "Stock Arrival", component: StockArrivalTheaterView },
  { key: "used-cars", label: "Used Cars", component: UsedCarsView },
  { key: "car-twin", label: "Vehicle Digital Twin", component: CarTwinWorkspaceView },
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
  { key: "customer-aging", label: "Customer Aging", component: CustomerAgingView },
  { key: "supplier-aging", label: "Supplier Aging", component: SupplierAgingView },
  { key: "admin-billing", label: "Pricing & Subscriptions", component: AdminBillingView },
  { key: "my-garage", label: "My Garage", component: MyGarageView },
  { key: "needboard", label: "NeedBoard", component: NeedBoardView },
  { key: "watchlist", label: "WatchPart", component: WatchlistView },
  { key: "seller-reputation", label: "Shop Reputation", component: SellerReputationView },
  { key: "seller-verification", label: "Seller Verification", component: SellerVerificationView },
  { key: "symptom-search", label: "Symptom Finder", component: SymptomSearchView },
  { key: "mechanic-desk", label: "Mechanic Desk", component: MechanicDeskView },
  { key: "garage-stock", label: "Garage Stock", component: GarageStockView },
  { key: "part-reserve", label: "Part Reservations", component: PartReserveView },
  { key: "part-reel", label: "Part Reels", component: PartReelView },
  { key: "whatsapp-selling", label: "WhatsApp Selling", component: WhatsAppSellingView },
  { key: "halfcut", label: "Half-Cut Showcase", component: HalfCutView },
  { key: "car-crush", label: "CarCrush", component: CarCrushView },
  { key: "escrow", label: "Escrow / Protection", component: EscrowView },
  { key: "market-price", label: "Market Price Index", component: MarketPriceView },
  { key: "listing-boost", label: "Boost Listings", component: ListingBoostView },
  { key: "referral", label: "Referral Program", component: ReferralView },
  { key: "voice-search", label: "Voice Search", component: VoiceSearchView },
  { key: "does-it-fit", label: "Does It Fit?", component: DoesItFitView },
  { key: "price-genius", label: "PriceGenius AI", component: PriceGeniusView },
  { key: "condition-scanner", label: "Condition Scanner", component: ConditionScannerView },
  { key: "community-guard", label: "CommunityGuard", component: CommunityGuardView },
  { key: "live-inspection", label: "Live Inspection", component: LiveInspectionView },
  { key: "qr-tag", label: "QR Tag System", component: QrTagView },
  { key: "part-genealogy", label: "Part Genealogy", component: PartGenealogyView },
  { key: "dismantler-forecast", label: "Dismantler Forecast", component: DismantlerForecastView },
  { key: "regional-demand", label: "Regional Demand Map", component: RegionalDemandView },
  { key: "mechanic-trust", label: "MechanicTrust Network", component: MechanicTrustView },
  { key: "new-vs-used", label: "New vs Used", component: NewVsUsedView },
  { key: "yard-tour", label: "Live Yard Tour", component: YardTourView },
  { key: "negotiation", label: "AI Negotiation Bot", component: NegotiationView },
  { key: "instant-offer", label: "InstantOffer", component: InstantOfferView },
  { key: "part-insurance", label: "Part Insurance Add-On", component: PartInsuranceView },
  { key: "kareem", label: "AutoChat Kareem", component: KareemView },
  { key: "ar-finder", label: "AR Parts Finder", component: ArFinderView },
  { key: "price-report", label: "B2B Price Report", component: PriceReportView },
  { key: "api-platform", label: "API Platform", component: ApiPlatformView }
]);
