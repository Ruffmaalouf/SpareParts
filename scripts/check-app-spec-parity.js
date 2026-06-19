const fs = require("fs");
const path = require("path");

const repoRoot = path.resolve(__dirname, "..");

function read(filePath) {
  return fs.readFileSync(path.join(repoRoot, filePath), "utf8");
}

function extractRegistryKeys(source) {
  return [...source.matchAll(/key:\s*"([^"]+)"/g)].map((match) => match[1]);
}

function extractRegistryEntries(source) {
  return [...source.matchAll(/\{\s*key:\s*"([^"]+)",\s*label:\s*"([^"]+)"/g)].map((match) => ({
    key: match[1],
    label: match[2]
  }));
}

function normalizeRegistryKeys(keys) {
  return keys.map((key) => key === "inventory" ? "parts" : key);
}

function normalizeRegistryEntries(entries) {
  return entries.map((entry) => ({
    ...entry,
    key: entry.key === "inventory" ? "parts" : entry.key
  }));
}

function extractNavigationGroupKeys(source) {
  return [...source.matchAll(/keys:\s*\[([^\]]*)\]/g)]
    .flatMap((match) => [...match[1].matchAll(/"([^"]+)"/g)].map((keyMatch) => keyMatch[1]))
    .map((key) => key === "inventory" ? "parts" : key);
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
const webRegistryEntries = normalizeRegistryEntries(extractRegistryEntries(read("src/SpareParts.Web.React/wwwroot/js/views/screen-registry.js")));
const mobileRegistryEntries = normalizeRegistryEntries(extractRegistryEntries(read("src/SpareParts.Mobile.ReactNative/src/screens/screen-registry.js")));
const mobileNavigationKeys = extractNavigationGroupKeys(read("src/SpareParts.Mobile.ReactNative/src/core/app-config.js"));
const wpfScreens = extractWpfScreens(read("src/SpareParts.Desktop.ViewModels/Navigation/AppScreen.cs"));

const webOnly = diff(webRegistry, mobileRegistry);
const mobileOnly = diff(mobileRegistry, webRegistry);
const mobileNavigationMissing = diff(mobileRegistry, mobileNavigationKeys);

const mobileLabelsByKey = new Map(mobileRegistryEntries.map((entry) => [entry.key, entry.label]));
const labelMismatches = webRegistryEntries
  .filter((entry) => mobileLabelsByKey.has(entry.key))
  .filter((entry) => mobileLabelsByKey.get(entry.key) !== entry.label)
  .map((entry) => `${entry.key} (web: "${entry.label}", mobile: "${mobileLabelsByKey.get(entry.key)}")`);

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

if (!webOnly.length && !mobileOnly.length && !labelMismatches.length && !mobileNavigationMissing.length && !missingWpf.length) {
  console.log("App spec parity check passed.");
  process.exit(0);
}

if (webOnly.length) {
  console.error(`Web-only screens: ${webOnly.join(", ")}`);
}

if (mobileOnly.length) {
  console.error(`Mobile-only screens: ${mobileOnly.join(", ")}`);
}

if (labelMismatches.length) {
  console.error(`Label mismatches: ${labelMismatches.join(", ")}`);
}

if (mobileNavigationMissing.length) {
  console.error(`Mobile screens missing navigation groups: ${mobileNavigationMissing.join(", ")}`);
}

if (missingWpf.length) {
  console.error(`Missing WPF coverage entries: ${missingWpf.join(", ")}`);
}

process.exit(1);
