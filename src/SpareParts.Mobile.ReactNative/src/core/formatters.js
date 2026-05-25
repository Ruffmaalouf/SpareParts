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
  initials,
  money,
  normalizeBaseUrl,
  normalizeLanguageKey,
  normalizeThemeKey,
  pickFirst,
  rowAmount,
  rowSubtitle,
  rowTitle,
  shortDate,
  shortDateTime
};
