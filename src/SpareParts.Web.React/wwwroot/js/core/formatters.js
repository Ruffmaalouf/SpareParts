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
