const fs = require("fs");
const path = require("path");
const { pathToFileURL } = require("url");
const vm = require("vm");

const repoRoot = path.resolve(__dirname, "..");
const outputPath = path.join(repoRoot, "outputs", "app-spec-parity-report.json");

function read(filePath) {
  return fs.readFileSync(path.join(repoRoot, filePath), "utf8");
}

function normalizeSpecKey(key) {
  return key === "inventory" ? "parts" : key;
}

function normalizeScreenEntries(entries) {
  return entries.map((entry) => ({
    ...entry,
    key: normalizeSpecKey(entry.key)
  }));
}

function normalizeFeatureModules(modules) {
  return modules.map((module) => ({
    key: normalizeSpecKey(module.key),
    label: module.label,
    title: module.title,
    endpoint: module.endpoint || "",
    capabilities: [...(module.capabilities || [])],
    commandOnly: Boolean(module.commandOnly)
  }));
}

function normalizeManagementSections(sections) {
  return sections.map((section) => ({
    key: section.key,
    label: section.label,
    endpoint: section.endpoint || ""
  }));
}

function normalizeNavigationGroups(groups) {
  return groups.map((group) => ({
    title: group.title,
    keys: (group.keys || []).map(normalizeSpecKey)
  }));
}

function extractNavGroupsFromLayout(source) {
  return [...source.matchAll(/\{\s*key:\s*"([^"]+)",\s*label:\s*"([^"]+)",\s*items:\s*\[([^\]]*)\]/g)].map((match) => ({
    title: match[2],
    keys: [...match[3].matchAll(/"([^"]+)"/g)].map((itemMatch) => normalizeSpecKey(itemMatch[1]))
  }));
}

function extractRegistryEntries(source) {
  return [...source.matchAll(/\{\s*key:\s*"([^"]+)",\s*label:\s*"([^"]+)"/g)].map((match) => ({
    key: match[1],
    label: match[2]
  }));
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

function extractXamlScreenParameters(source) {
  return [...source.matchAll(/ConverterParameter=([A-Za-z]+)/g)].map((match) => match[1]);
}

function compareKeySets(name, leftEntries, rightEntries) {
  const leftKeys = leftEntries.map((entry) => entry.key);
  const rightKeys = rightEntries.map((entry) => entry.key);
  const rightSet = new Set(rightKeys);
  const leftSet = new Set(leftKeys);

  return {
    name,
    leftOnly: leftKeys.filter((key) => !rightSet.has(key)),
    rightOnly: rightKeys.filter((key) => !leftSet.has(key))
  };
}

function compareFieldMaps(name, leftEntries, rightEntries, fields) {
  const rightByKey = new Map(rightEntries.map((entry) => [entry.key, entry]));

  return leftEntries
    .filter((entry) => rightByKey.has(entry.key))
    .flatMap((entry) => {
      const right = rightByKey.get(entry.key);
      return fields
        .filter((field) => JSON.stringify(entry[field]) !== JSON.stringify(right[field]))
        .map((field) => ({
          name,
          key: entry.key,
          field,
          left: entry[field],
          right: right[field]
        }));
    });
}

function ensureOutputDir() {
  fs.mkdirSync(path.dirname(outputPath), { recursive: true });
}

function writeReport(report) {
  ensureOutputDir();
  fs.writeFileSync(outputPath, JSON.stringify(report, null, 2));
}

function hasSymbol(source, symbol) {
  return source.includes(symbol);
}

function evaluateWpfCoverage(context) {
  const directMappings = {
    dashboard: ["HomePage"],
    invoices: ["Pos"],
    "sales-returns": ["SalesReturns"],
    parts: ["StockManagement"],
    "part-passport": ["PartPassport"],
    compatibility: ["PartCompatibility"],
    "purchase-parts": ["PartPurchases"],
    "used-car-purchases": ["Purchases"],
    "used-car-wholesale": ["UsedCarWholesale"],
    "stock-arrival": ["StockArrivalTheater"],
    "used-cars": ["PurchaseHistory"],
    "car-twin": ["CarTwin"],
    "repair-prep": ["RepairPrepBoard"],
    stock: ["StockManagement"],
    "dead-stock": ["DeadStockResurrection"],
    "growth-lab": ["GrowthLab"],
    accounting: ["Accounting"],
    "manual-journal": ["ManualJournal"],
    "report-builder": ["ReportBuilder"],
    whatsapp: ["WhatsAppInbox"],
    "business-assistant": ["BusinessAssistant"],
    ar: ["BarcodeMode"],
    reorder: ["Reorder"],
    "expiry-alerts": ["ExpiryAlerts"],
    loyalty: ["Loyalty"],
    warranty: ["Warranty"],
    shipments: ["Shipments"],
    "activity-log": ["ActivityLog"],
    quotes: ["Quotes"],
    "customer-aging": ["CustomerAging"],
    "supplier-aging": ["SupplierAging"],
    billing: ["BillingSubscription"],
    "admin-billing": ["AdminBilling"],
    needboard: ["Needboard"],
    watchlist: ["Watchlist"],
    "seller-reputation": ["SellerReputation"],
    "seller-verification": ["SellerVerification"],
    "symptom-search": ["SymptomSearch"],
    "mechanic-desk": ["MechanicDesk"],
    "garage-stock": ["GarageStock"],
    "part-reserve": ["PartReserve"],
    "part-reel": ["PartReel"],
    "whatsapp-selling": ["WhatsAppSelling"],
    halfcut: ["Halfcut"],
    "car-crush": ["CarCrush"],
    escrow: ["Escrow"],
    "market-price": ["MarketPrice"],
    "listing-boost": ["ListingBoost"],
    referral: ["Referral"],
    "voice-search": ["VoiceSearch"],
    "my-garage": ["MyGarage"],
    "does-it-fit": ["DoesItFit"],
    "price-genius": ["PriceGenius"],
    "condition-scanner": ["ConditionScanner"],
    "community-guard": ["CommunityGuard"],
    "live-inspection": ["LiveInspection"],
    "qr-tag": ["QrTag"],
    "part-genealogy": ["PartGenealogy"],
    "dismantler-forecast": ["DismantlerForecast"],
    "regional-demand": ["RegionalDemand"],
    "mechanic-trust": ["MechanicTrust"],
    "new-vs-used": ["NewVsUsed"],
    negotiation: ["Negotiation"],
    "yard-tour": ["YardTour"],
    "instant-offer": ["InstantOffer"],
    "part-insurance": ["PartInsurance"],
    kareem: ["Kareem"],
    "ar-finder": ["ArFinder"],
    "price-report": ["PriceReport"],
    "api-platform": ["ApiPlatform"]
  };

  const embeddedCoverage = {
    "part-requests": [
      {
        file: "src/SpareParts.Desktop.ViewModels/Management/PartRequestsManagementViewModel.cs",
        token: "class PartRequestsManagementViewModel"
      }
    ],
    contacts: [
      {
        file: "src/SpareParts.Desktop.ViewModels/Management/CustomerManagementViewModel.cs",
        token: "class CustomerManagementViewModel"
      },
      {
        file: "src/SpareParts.Desktop.ViewModels/Management/SupplierManagementViewModel.cs",
        token: "class SupplierManagementViewModel"
      }
    ],
    management: [
      {
        file: "src/SpareParts.Desktop.ViewModels/ViewModels/InvoiceTabsViewModel.cs",
        token: "ManagementVm"
      },
      {
        file: "src/SpareParts.Desktop.Wpf/Windows/MainWindow.xaml",
        token: "Management"
      }
    ],
    settings: [
      {
        file: "src/SpareParts.Desktop.Wpf/Windows/MainWindow.xaml.cs",
        token: "ThemeMenuItem_Click"
      }
    ]
  };

  const missingDirect = [];
  const missingEmbedded = [];
  const coveredByDirect = [];
  const coveredByEmbedded = [];

  for (const module of context.sharedModules) {
    const directScreens = directMappings[module.key];
    if (directScreens) {
      const missingScreens = directScreens.filter((screen) => !context.wpfEnum.has(screen) || !context.wpfXamlScreens.has(screen));
      if (missingScreens.length) {
        missingDirect.push({ key: module.key, expectedScreens: directScreens, missingScreens });
      } else {
        coveredByDirect.push({ key: module.key, screens: directScreens });
      }
      continue;
    }

    const embeddedRules = embeddedCoverage[module.key] || [];
    const missingRules = embeddedRules.filter((rule) => !hasSymbol(context.fileContents[rule.file] || "", rule.token));
    if (!embeddedRules.length || missingRules.length) {
      missingEmbedded.push({
        key: module.key,
        expectedEvidence: embeddedRules.map((rule) => ({ file: rule.file, token: rule.token })),
        missingEvidence: missingRules.map((rule) => ({ file: rule.file, token: rule.token }))
      });
    } else {
      coveredByEmbedded.push({
        key: module.key,
        evidence: embeddedRules.map((rule) => ({ file: rule.file, token: rule.token }))
      });
    }
  }

  return {
    coveredByDirect,
    coveredByEmbedded,
    missingDirect,
    missingEmbedded
  };
}

async function loadConfigs() {
  global.window = global.window || {};
  const webConfigModule = await import(pathToFileURL(path.join(repoRoot, "src/SpareParts.Web.React/wwwroot/js/core/config.js")).href);
  const mobileConfigPath = path.join(repoRoot, "src/SpareParts.Mobile.ReactNative/src/core/app-config.js");
  const mobileConfigSource = fs.readFileSync(mobileConfigPath, "utf8");
  const mobileModule = { exports: {} };
  const mobileSandbox = {
    module: mobileModule,
    exports: mobileModule.exports,
    process,
    console,
    __filename: mobileConfigPath,
    __dirname: path.dirname(mobileConfigPath),
    require(requested) {
      if (requested === "react-native") {
        return { Platform: { OS: "android" } };
      }

      return require(requested);
    }
  };
  const wrappedMobileSource = `(function (require, module, exports, process, __filename, __dirname) { ${mobileConfigSource}\n})`;
  const mobileFactory = vm.runInNewContext(wrappedMobileSource, mobileSandbox, { filename: mobileConfigPath });
  mobileFactory(
    mobileSandbox.require,
    mobileModule,
    mobileModule.exports,
    process,
    mobileConfigPath,
    path.dirname(mobileConfigPath)
  );
  return {
    web: webConfigModule,
    mobile: mobileModule.exports
  };
}

async function main() {
  const configs = await loadConfigs();
  const webFeatureModules = normalizeFeatureModules(configs.web.featureModules);
  const mobileFeatureModules = normalizeFeatureModules(configs.mobile.featureModules);
  const webManagementSections = normalizeManagementSections(configs.web.managementSections);
  const mobileManagementSections = normalizeManagementSections(configs.mobile.managementSections);
  const webNavigationGroups = normalizeNavigationGroups(extractNavGroupsFromLayout(read("src/SpareParts.Web.React/wwwroot/js/components/layout.js")));
  const mobileNavigationGroups = normalizeNavigationGroups(configs.mobile.navigationGroups);

  const webScreenRegistry = normalizeScreenEntries(extractRegistryEntries(read("src/SpareParts.Web.React/wwwroot/js/views/screen-registry.js")));
  const mobileScreenRegistry = normalizeScreenEntries(extractRegistryEntries(read("src/SpareParts.Mobile.ReactNative/src/screens/screen-registry.js")));

  const wpfEnumSource = read("src/SpareParts.Desktop.ViewModels/Navigation/AppScreen.cs");
  const wpfXamlSource = read("src/SpareParts.Desktop.Wpf/Windows/MainWindow.xaml");
  const wpfEnum = new Set(extractWpfScreens(wpfEnumSource));
  const wpfXamlScreens = new Set(extractXamlScreenParameters(wpfXamlSource));

  const sharedModules = webFeatureModules.filter((module) => mobileFeatureModules.some((mobileModule) => mobileModule.key === module.key));
  const wpfContext = {
    sharedModules,
    wpfEnum,
    wpfXamlScreens,
    fileContents: {
      "src/SpareParts.Desktop.ViewModels/ViewModels/InvoiceTabsViewModel.cs": read("src/SpareParts.Desktop.ViewModels/ViewModels/InvoiceTabsViewModel.cs"),
      "src/SpareParts.Desktop.ViewModels/Management/PartRequestsManagementViewModel.cs": read("src/SpareParts.Desktop.ViewModels/Management/PartRequestsManagementViewModel.cs"),
      "src/SpareParts.Desktop.ViewModels/Management/CustomerManagementViewModel.cs": read("src/SpareParts.Desktop.ViewModels/Management/CustomerManagementViewModel.cs"),
      "src/SpareParts.Desktop.ViewModels/Management/SupplierManagementViewModel.cs": read("src/SpareParts.Desktop.ViewModels/Management/SupplierManagementViewModel.cs"),
      "src/SpareParts.Desktop.Wpf/Windows/MainWindow.xaml": wpfXamlSource,
      "src/SpareParts.Desktop.Wpf/Windows/MainWindow.xaml.cs": read("src/SpareParts.Desktop.Wpf/Windows/MainWindow.xaml.cs"),
      "src/SpareParts.Desktop.Wpf/Windows/LoginWindow.xaml.cs": read("src/SpareParts.Desktop.Wpf/Windows/LoginWindow.xaml.cs")
    }
  };

  const comparisons = {
    featureModuleKeys: compareKeySets("featureModules", webFeatureModules, mobileFeatureModules),
    featureModuleFields: compareFieldMaps("featureModules", webFeatureModules, mobileFeatureModules, ["label", "title", "endpoint", "capabilities", "commandOnly"]),
    managementSectionKeys: compareKeySets("managementSections", webManagementSections, mobileManagementSections),
    managementSectionFields: compareFieldMaps("managementSections", webManagementSections, mobileManagementSections, ["label", "endpoint"]),
    navigationGroupKeys: compareKeySets("navigationGroups", webNavigationGroups.map((group) => ({ key: group.title })), mobileNavigationGroups.map((group) => ({ key: group.title }))),
    navigationGroupFields: compareFieldMaps("navigationGroups", webNavigationGroups.map((group) => ({ key: group.title, keys: group.keys })), mobileNavigationGroups.map((group) => ({ key: group.title, keys: group.keys })), ["keys"]),
    screenRegistryKeys: compareKeySets("screenRegistry", webScreenRegistry, mobileScreenRegistry),
    screenRegistryFields: compareFieldMaps("screenRegistry", webScreenRegistry, mobileScreenRegistry, ["label"])
  };

  const mobileNavigationCoverage = {
    missingFeatureModules: mobileFeatureModules
      .map((module) => module.key)
      .filter((key) => !new Set(mobileNavigationGroups.flatMap((group) => group.keys)).has(key))
  };

  const wpfCoverage = evaluateWpfCoverage(wpfContext);

  const webThemes = (configs.web.wpfThemes || []).map((theme) => theme.key);
  const mobileThemes = (configs.mobile.wpfThemes || []).map((theme) => theme.key);
  const webLanguages = (configs.web.languageOptions || []).map((language) => language.key);
  const mobileLanguages = (configs.mobile.appLanguages || []).map((language) => language.key);

  const report = {
    generatedAtUtc: new Date().toISOString(),
    summary: {
      featureModuleCount: webFeatureModules.length,
      managementSectionCount: webManagementSections.length,
      navigationGroupCount: webNavigationGroups.length,
      screenRegistryCount: webScreenRegistry.length,
      wpfEnumCount: wpfEnum.size,
      wpfXamlScreenCount: wpfXamlScreens.size
    },
    comparisons,
    mobileNavigationCoverage,
    themes: {
      web: webThemes,
      mobile: mobileThemes,
      mismatches: JSON.stringify(webThemes) === JSON.stringify(mobileThemes) ? [] : [{ web: webThemes, mobile: mobileThemes }]
    },
    languages: {
      web: webLanguages,
      mobile: mobileLanguages,
      mismatches: JSON.stringify(webLanguages) === JSON.stringify(mobileLanguages) ? [] : [{ web: webLanguages, mobile: mobileLanguages }]
    },
    wpfCoverage
  };

  writeReport(report);

  const failures = [
    comparisons.featureModuleKeys.leftOnly,
    comparisons.featureModuleKeys.rightOnly,
    comparisons.featureModuleFields,
    comparisons.managementSectionKeys.leftOnly,
    comparisons.managementSectionKeys.rightOnly,
    comparisons.managementSectionFields,
    comparisons.navigationGroupKeys.leftOnly,
    comparisons.navigationGroupKeys.rightOnly,
    comparisons.navigationGroupFields,
    comparisons.screenRegistryKeys.leftOnly,
    comparisons.screenRegistryKeys.rightOnly,
    comparisons.screenRegistryFields,
    mobileNavigationCoverage.missingFeatureModules,
    report.themes.mismatches,
    report.languages.mismatches,
    wpfCoverage.missingDirect,
    wpfCoverage.missingEmbedded
  ];

  const failed = failures.some((entry) => entry.length > 0);

  if (!failed) {
    console.log(`App spec parity check passed. Report: ${path.relative(repoRoot, outputPath)}`);
    return;
  }

  console.error("App spec parity check failed. See outputs/app-spec-parity-report.json for details.");
  process.exit(1);
}

main().catch((error) => {
  console.error(error.stack || error.message || String(error));
  process.exit(1);
});
