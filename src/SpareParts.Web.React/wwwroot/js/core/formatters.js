import { defaultApiBaseUrl, defaultThemeKey, themeMap } from "./config.js";

export function normalizeBaseUrl(value) {
  const url = (value || defaultApiBaseUrl).trim();
  return url.endsWith("/") ? url.slice(0, -1) : url;
}

export function normalizeThemeKey(value) {
  return themeMap.has(value) ? value : defaultThemeKey;
}

export function applyWebTheme(themeKey) {
  const theme = themeMap.get(normalizeThemeKey(themeKey)) || themeMap.get(defaultThemeKey);
  const root = document.documentElement;
  Object.entries(theme.colors).forEach(([key, value]) => {
    const cssKey = key.replace(/[A-Z]/g, (match) => `-${match.toLowerCase()}`);
    root.style.setProperty(`--${cssKey}`, value);
  });
  root.style.setProperty("--surface-2", theme.colors.surface2);
  root.style.setProperty("--accent-2", theme.colors.accent2);
}

export function pickFirst(row, keys) {
  for (const key of keys) {
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") {
      return value;
    }
  }
  return "";
}

function readField(row, ...keys) {
  for (const key of keys) {
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") return value;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row?.[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }
  return "";
}

function positiveNumber(value) {
  const parsed = Number(String(value || "").replace(/,/g, ".").trim());
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 0;
}

export function normalizeCurrencyCode(value, fallback = "") {
  const normalized = String(value || "").trim().toUpperCase();
  return normalized.length === 3 ? normalized : fallback;
}

export function appConstantValue(constants, key) {
  const target = String(key || "").trim().toLowerCase();
  const rows = Array.isArray(constants) ? constants : [];
  const match = rows.find((row) => String(readField(row, "key")).trim().toLowerCase() === target);
  return match ? readField(match, "value") : "";
}

export function resolveRateToBaseCurrency({ rates = [], baseCurrencyCode = "USD", counterCurrencyCode = "USD", defaultCounterRate = 1 } = {}, currencyCode) {
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

export function displayCurrencyContext({ constants = [], rates = [], baseCurrencyCode, counterCurrencyCode } = {}) {
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

export function convertBaseToDisplay(value, context) {
  const number = Number(value || 0);
  const rateToBase = positiveNumber(context?.rateToBase) || 1;
  return number / rateToBase;
}

export function displayMoneyFromBase(value, context) {
  return money(convertBaseToDisplay(value, context), context?.code || "USD");
}

export function convertCounterToDisplay(value, context) {
  const number = Number(value || 0);
  const counterRateToBase = positiveNumber(context?.counterRateToBase) || 1;
  const displayRateToBase = positiveNumber(context?.rateToBase) || 1;
  return (number * counterRateToBase) / displayRateToBase;
}

export function displayMoneyFromCounter(value, context) {
  return money(convertCounterToDisplay(value, context), context?.code || "USD");
}

export function rowTitle(row) {
  return pickFirst(row, ["name", "requestedPartName", "fullName", "username", "invoiceNumber", "purchaseNumber", "referenceNumber", "internalCode", "code", "modelName", "brandName", "title"]) || `#${row?.id || row?.invoiceId || row?.purchaseId || ""}`.trim();
}

export function rowSubtitle(row) {
  const customer = pickFirst(row, ["customerName", "customerPhone"]);
  const status = pickFirst(row, ["status"]);
  if (customer && status) return `${customer} - ${status}`;
  return pickFirst(row, ["phone", "email", "oemNumber", "requestedOemNumber", "description", "notes", "role", "currencyCode", "status", "plateNumber", "vin"]);
}

export function rowAmount(row) {
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

export function money(value, currency = "USD") {
  const number = Number(value || 0);
  return `${currency || "USD"} ${number.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function dateTime(value) {
  if (!value) return "";
  return new Date(value).toLocaleString(undefined, { month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
}

export function shortDate(value) {
  if (!value) return "";
  return new Date(value).toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
}

export function initials(value) {
  const words = String(value || "WA").trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return "WA";
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return `${words[0][0]}${words[1][0]}`.toUpperCase();
}

export function asRows(result) {
  return Array.isArray(result) ? result : result ? [result] : [];
}

export function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}
