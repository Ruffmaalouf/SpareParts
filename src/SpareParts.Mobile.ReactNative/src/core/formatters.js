const { defaultApiBaseUrl, defaultLanguageKey, defaultThemeKey, languageMap, themeMap } = require("./app-config");

function normalizeBaseUrl(value) {
  const url = String(value || defaultApiBaseUrl).trim();
  return url.endsWith("/") ? url.slice(0, -1) : url;
}

function normalizeThemeKey(value) {
  return themeMap.has(value) ? value : defaultThemeKey;
}

function normalizeLanguageKey(value) {
  return languageMap.has(value) ? value : defaultLanguageKey;
}

function pickFirst(row, keys) {
  for (const key of keys) {
    const value = row && row[key];
    if (value !== undefined && value !== null && value !== "") {
      return value;
    }
  }
  return "";
}

function readField(row, ...keys) {
  for (const key of keys) {
    const value = row && row[key];
    if (value !== undefined && value !== null && value !== "") return value;

    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row && row[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }

  return "";
}

function positiveNumber(value) {
  const parsed = Number(String(value || "").replace(/,/g, ".").trim());
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
}

function normalizeCurrencyCode(value, fallback = "") {
  const normalized = String(value || "").trim().toUpperCase();
  return normalized.length === 3 ? normalized : fallback;
}

function appConstantValue(constants, key) {
  const target = String(key || "").trim().toLowerCase();
  const rows = Array.isArray(constants) ? constants : [];
  const match = rows.find((row) => String(readField(row, "key")).trim().toLowerCase() === target);
  return match ? readField(match, "value") : "";
}

function resolveRateToBaseCurrency({ rates = [], baseCurrencyCode = "USD", counterCurrencyCode = "USD", defaultCounterRate = 1 } = {}, currencyCode) {
  const normalized = normalizeCurrencyCode(currencyCode);
  const base = normalizeCurrencyCode(baseCurrencyCode, "USD");
  const counter = normalizeCurrencyCode(counterCurrencyCode, base);
  if (!normalized || normalized === base) return 1;

  const rate = (Array.isArray(rates) ? rates : []).find((item) =>
    normalizeCurrencyCode(readField(item, "code")) === normalized);
  if (rate) {
    const rateBase = normalizeCurrencyCode(readField(rate, "baseCode"), base);
    const rateToUsd = positiveNumber(readField(rate, "rateToUsd"));
    if (normalized === rateBase) return 1;
    if (rateToUsd > 0) return 1 / rateToUsd;
  }

  if (normalized === counter) {
    const fallbackRate = positiveNumber(defaultCounterRate);
    return fallbackRate > 0 ? fallbackRate : 1;
  }

  return 1;
}

function displayCurrencyContext({ constants = [], rates = [], baseCurrencyCode, counterCurrencyCode } = {}) {
  const base = normalizeCurrencyCode(
    baseCurrencyCode,
    normalizeCurrencyCode(appConstantValue(constants, "BaseCurrencyCode") || appConstantValue(constants, "DefaultCurrencyCode"), "USD")
  );
  const counter = normalizeCurrencyCode(counterCurrencyCode, normalizeCurrencyCode(appConstantValue(constants, "CounterCurrencyCode"), base));
  const display = normalizeCurrencyCode(appConstantValue(constants, "DisplayCurrencyCode"), counter || base || "USD");
  const defaultCounterRate = positiveNumber(appConstantValue(constants, "DefaultCounterRate")) || 1;
  const rateToBase = resolveRateToBaseCurrency({
    rates,
    baseCurrencyCode: base,
    counterCurrencyCode: counter,
    defaultCounterRate
  }, display);
  const counterRateToBase = resolveRateToBaseCurrency({
    rates,
    baseCurrencyCode: base,
    counterCurrencyCode: counter,
    defaultCounterRate
  }, counter);

  return {
    code: display,
    baseCurrencyCode: base,
    counterCurrencyCode: counter,
    counterRateToBase: counterRateToBase > 0 ? counterRateToBase : 1,
    rateToBase: rateToBase > 0 ? rateToBase : 1
  };
}

function convertBaseToDisplay(value, context) {
  const number = Number(value || 0);
  const rateToBase = positiveNumber(context && context.rateToBase) || 1;
  return number / rateToBase;
}

function displayMoneyFromBase(value, context) {
  return money(convertBaseToDisplay(value, context), context && context.code || "USD");
}

function convertCounterToDisplay(value, context) {
  const number = Number(value || 0);
  const counterRateToBase = positiveNumber(context && context.counterRateToBase) || 1;
  const displayRateToBase = positiveNumber(context && context.rateToBase) || 1;
  return (number * counterRateToBase) / displayRateToBase;
}

function displayMoneyFromCounter(value, context) {
  return money(convertCounterToDisplay(value, context), context && context.code || "USD");
}

function rowTitle(row) {
  return pickFirst(row, ["name", "requestedPartName", "fullName", "username", "invoiceNumber", "purchaseNumber", "referenceNumber", "internalCode", "code", "modelName", "brandName", "title"]) || `#${row?.id || row?.invoiceId || row?.purchaseId || ""}`.trim();
}

function rowSubtitle(row) {
  const customer = pickFirst(row, ["customerName", "customerPhone"]);
  const status = pickFirst(row, ["status"]);
  if (customer && status) return `${customer} - ${status}`;
  return pickFirst(row, ["phone", "email", "oemNumber", "requestedOemNumber", "description", "notes", "role", "currencyCode", "status", "plateNumber", "vin"]);
}

function rowAmount(row) {
  if (row?.isReservationReminderDue) return "Reminder due";
  if (row?.isReserved) return row.reservationExpiresAt ? `Until ${shortDateTime(row.reservationExpiresAt)}` : "Reserved";
  if (row?.isReadyToContact) return `${row.waitingCustomerCount || 1} waiting`;
  if (pickFirst(row, ["requestedPartName"])) {
    const quantity = pickFirst(row, ["quantity"]);
    return quantity === "" ? "" : `Qty ${quantity}`;
  }

  const value = pickFirst(row, ["balance", "openingBalance", "totalAmount", "amount", "salePrice", "costPrice", "stockQuantity"]);
  if (value === "") return "";
  const currency = pickFirst(row, ["currencyCode", "currency"]) || "USD";
  return typeof value === "number" ? money(value, currency) : String(value);
}

function money(value, currency) {
  const number = Number(value || 0);
  return `${currency || "USD"} ${number.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  })}`;
}

function shortDate(value) {
  if (!value) return "";
  return new Date(value).toLocaleDateString();
}

function shortDateTime(value) {
  if (!value) return "";
  return new Date(value).toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function initials(value) {
  const words = String(value || "MA").trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "MA";
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return `${words[0][0]}${words[1][0]}`.toUpperCase();
}

function asRows(result) {
  return Array.isArray(result) ? result : result ? [result] : [];
}

module.exports = {
  asRows,
  appConstantValue,
  convertBaseToDisplay,
  convertCounterToDisplay,
  displayCurrencyContext,
  displayMoneyFromBase,
  displayMoneyFromCounter,
  initials,
  money,
  normalizeBaseUrl,
  normalizeCurrencyCode,
  normalizeLanguageKey,
  normalizeThemeKey,
  pickFirst,
  resolveRateToBaseCurrency,
  rowAmount,
  rowSubtitle,
  rowTitle,
  shortDate,
  shortDateTime
};
