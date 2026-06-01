export function read(row, ...keys) {
  for (const key of keys) {
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") return value;

    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row?.[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }

  return "";
}

export function rowId(row, config) {
  const keys = config?.idKeys || ["id", "userId", "locationId"];
  return read(row, ...keys);
}

export function emptyForm(config) {
  return Object.fromEntries((config.fields || []).map((field) => [
    field.key,
    field.type === "bool" ? Boolean(field.defaultValue) : String(field.defaultValue ?? "")
  ]));
}

export function formFromRow(row, config) {
  if (!row) return emptyForm(config);

  return Object.fromEntries((config.fields || []).map((field) => {
    const value = read(row, field.key);
    if (field.type === "bool") return [field.key, Boolean(value)];
    return [field.key, value === "" ? String(field.defaultValue ?? "") : String(value)];
  }));
}

function castValue(field, value, isUpdate) {
  if (field.type === "bool") return Boolean(value);

  if (field.type === "number") {
    const text = String(value ?? "").replace(/,/g, ".").trim();
    if (!text && field.optional) return null;
    const number = Number(text || 0);
    return Number.isFinite(number) ? number : 0;
  }

  const text = String(value ?? "").trim();
  if (!text && (field.optional || (isUpdate && field.optionalUpdate))) return null;
  return text;
}

export function buildPayload(config, form, isUpdate) {
  const payload = {};

  for (const field of config.fields || []) {
    if (isUpdate && field.update === false) continue;
    if (!isUpdate && field.create === false) continue;
    if (field.readOnly) continue;
    if (isUpdate && field.optionalUpdate && !String(form[field.key] || "").trim()) continue;

    const targetKey = isUpdate ? field.updateKey || field.key : field.createKey || field.key;
    payload[targetKey] = castValue(field, form[field.key], isUpdate);
  }

  return payload;
}

export function matchesRow(row, term) {
  const needle = term.trim().toLowerCase();
  if (!needle) return true;

  return [
    "name",
    "fullName",
    "username",
    "phone",
    "email",
    "code",
    "barcode",
    "description",
    "country",
    "role",
    "customerName",
    "customerPhone",
    "requestedPartName",
    "requestedOemNumber",
    "vehicleDetails",
    "partInternalCode",
    "matchedPartName",
    "status",
    "notes"
  ].some((key) => String(read(row, key) || "").toLowerCase().includes(needle));
}

export function rowTitle(row) {
  return read(row, "name", "requestedPartName", "fullName", "username", "invoiceNumber", "purchaseNumber", "referenceNumber", "internalCode", "code", "modelName", "brandName", "title") || `#${read(row, "id", "userId", "locationId")}`.trim();
}

export function rowSubtitle(row) {
  const customer = read(row, "customerName", "customerPhone");
  const status = read(row, "status");
  if (customer && status) return `${customer} - ${status}`;
  return read(row, "phone", "email", "oemNumber", "requestedOemNumber", "description", "notes", "role", "currencyCode", "status", "plateNumber", "vin");
}
