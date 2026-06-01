function read(row, ...keys) {
  for (const key of keys) {
    const value = row && row[key];
    if (value !== undefined && value !== null && value !== "") return value;

    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row && row[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }

  return "";
}

function rowId(row, config) {
  const keys = (config && config.idKeys) || ["id", "userId", "locationId"];
  return read(row, ...keys);
}

function emptyForm(config) {
  return Object.fromEntries((config.fields || []).map((field) => [
    field.key,
    field.type === "bool" ? Boolean(field.defaultValue) : String(field.defaultValue ?? "")
  ]));
}

function formFromRow(row, config) {
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

function buildPayload(config, form, isUpdate) {
  const payload = {};

  for (const field of config.fields) {
    if (isUpdate && field.update === false) continue;
    if (!isUpdate && field.create === false) continue;
    if (field.readOnly) continue;
    if (isUpdate && field.optionalUpdate && !String(form[field.key] || "").trim()) continue;

    const targetKey = isUpdate ? field.updateKey || field.key : field.createKey || field.key;
    payload[targetKey] = castValue(field, form[field.key], isUpdate);
  }

  return payload;
}

function matchesRow(row, term) {
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

function launchMeta(screenKey) {
  if (screenKey === "part-requests") return "Demand";
  if (screenKey === "used-cars") return "Gallery";
  if (screenKey === "whatsapp") return "Messages";
  if (screenKey === "report-builder") return "Reports";
  if (screenKey === "business-assistant") return "Ask";
  return "Open";
}

module.exports = {
  buildPayload,
  emptyForm,
  formFromRow,
  launchMeta,
  matchesRow,
  read,
  rowId
};
