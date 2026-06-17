const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "..");

function read(filePath) {
  return fs.readFileSync(path.join(repoRoot, filePath), "utf8");
}

function extractRegistryKeys(source) {
  return [...source.matchAll(/key:\s*"([^"]+)"/g)].map((match) => match[1]);
}

function normalizeRegistryKeys(keys) {
  return keys.map((key) => key === "inventory" ? "parts" : key);
}

function extractWpfScreens(source) {
  const enumBody = source.match(/public enum AppScreen\s*\{([\s\S]*?)\}/);
  if (!enumBody) {
    throw new Error("Could not find AppScreen enum.");
  }

  return enumBody[1]
    .split(",")
    .map((item) => item.trim())
    .filter(Boolean);
}

function diff(left, right) {
  const rightSet = new Set(right);
  return left.filter((item) => !rightSet.has(item));
}

const webRegistry = normalizeRegistryKeys(extractRegistryKeys(read("src/SpareParts.Web.React/wwwroot/js/views/screen-registry.js")));
const mobileRegistry = normalizeRegistryKeys(extractRegistryKeys(read("src/SpareParts.Mobile.ReactNative/src/screens/screen-registry.js")));
const wpfScreens = extractWpfScreens(read("src/SpareParts.Desktop.ViewModels/Navigation/AppScreen.cs"));

const webOnly = diff(webRegistry, mobileRegistry);
const mobileOnly = diff(mobileRegistry, webRegistry);

const expectedWpfCoverage = [
  "Accounting",
  "Quotes",
  "CustomerAging",
  "SupplierAging",
  "PartPurchases",
  "Purchases",
  "UsedCarWholesale",
  "StockArrivalTheater",
  "RepairPrepBoard",
  "StockManagement",
  "DeadStockResurrection",
  "GrowthLab",
  "PartPassport",
  "PartCompatibility",
  "BarcodeMode",
  "ReportBuilder",
  "WhatsAppInbox",
  "BusinessAssistant",
  "Reorder",
  "ExpiryAlerts",
  "Loyalty",
  "Warranty",
  "Shipments",
  "ActivityLog",
  "BillingSubscription"
];

const missingWpf = diff(expectedWpfCoverage, wpfScreens);

if (!webOnly.length && !mobileOnly.length && !missingWpf.length) {
  console.log("App spec parity check passed.");
  process.exit(0);
}

if (webOnly.length) {
  console.error(`Web-only screens: ${webOnly.join(", ")}`);
}

if (mobileOnly.length) {
  console.error(`Mobile-only screens: ${mobileOnly.join(", ")}`);
}

if (missingWpf.length) {
  console.error(`Missing WPF coverage entries: ${missingWpf.join(", ")}`);
}

process.exit(1);
