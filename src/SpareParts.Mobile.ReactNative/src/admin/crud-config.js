const commonPartnerFields = [
  { key: "name", label: "Name", required: true },
  { key: "phone", label: "Phone", optional: true, keyboardType: "phone-pad" },
  { key: "email", label: "Email", optional: true, keyboardType: "email-address" },
  { key: "address", label: "Address", optional: true },
  { key: "taxNumber", label: "Tax Number", optional: true },
  { key: "openingBalance", label: "Opening Balance", type: "number" }
];

const crudConfigs = {
  customers: { basePath: "/api/customers", fields: commonPartnerFields },
  suppliers: { basePath: "/api/suppliers", fields: commonPartnerFields },
  brands: {
    basePath: "/api/brands",
    fields: [
      { key: "name", label: "Name", required: true },
      { key: "isActive", label: "Active", type: "bool", defaultValue: true }
    ]
  },
  parts: {
    basePath: "/api/parts",
    fields: [
      { key: "internalCode", label: "Internal Code", required: true },
      { key: "barcode", label: "Barcode", optional: true },
      { key: "name", label: "Name", required: true },
      { key: "oemNumber", label: "OEM Number", optional: true },
      { key: "condition", label: "Condition", type: "number", defaultValue: "1" },
      { key: "categoryId", label: "Category Id", type: "number", required: true },
      { key: "brandId", label: "Brand Id", type: "number", optional: true },
      { key: "costPrice", label: "Cost Price", type: "number" },
      { key: "salePrice", label: "Sale Price", type: "number" },
      { key: "averagePrice", label: "Average Price", type: "number", optional: true },
      { key: "currency", label: "Currency", defaultValue: "USD" },
      { key: "minStock", label: "Min Stock", type: "number" },
      { key: "usedCarId", label: "Used Car Id", type: "number", optional: true },
      { key: "notes", label: "Notes", optional: true }
    ]
  },
  "part-requests": {
    basePath: "/api/partrequests",
    canUpdate: false,
    fields: [
      { key: "partId", label: "Matched Part Id", type: "number", optional: true },
      { key: "customerId", label: "Customer Id", type: "number", optional: true },
      { key: "customerName", label: "Customer Name", required: true },
      { key: "customerPhone", label: "Customer Phone", optional: true, keyboardType: "phone-pad" },
      { key: "requestedPartName", label: "Requested Part", required: true },
      { key: "requestedOemNumber", label: "OEM / Reference", optional: true },
      { key: "vehicleDetails", label: "Vehicle", optional: true },
      { key: "quantity", label: "Quantity", type: "number", defaultValue: "1" },
      { key: "notes", label: "Notes", optional: true }
    ]
  },
  "car-brands": {
    basePath: "/api/carbrands",
    fields: [
      { key: "name", label: "Name", required: true },
      { key: "country", label: "Country" },
      { key: "regionGroup", label: "Region Group" },
      { key: "sortOrder", label: "Sort Order", type: "number" }
    ]
  },
  "car-models": {
    basePath: "/api/carmodels",
    fields: [
      { key: "carBrandId", label: "Car Brand Id", type: "number", required: true },
      { key: "name", label: "Model", required: true },
      { key: "bodyType", label: "Body Type" }
    ]
  },
  users: {
    basePath: "/api/users",
    fields: [
      { key: "username", label: "Username", required: true, update: false },
      { key: "fullName", label: "Full Name", required: true },
      { key: "email", label: "Email", optional: true, keyboardType: "email-address" },
      { key: "roleId", label: "Role ID", type: "number", required: true, defaultValue: 3 },
      { key: "password", label: "Password", createKey: "password", updateKey: "newPassword", secure: true, optionalUpdate: true },
      { key: "isActive", label: "Active", type: "bool", defaultValue: true, create: false }
    ]
  },
  warehouses: {
    basePath: "/api/warehouses",
    fields: [
      { key: "name", label: "Name", required: true },
      { key: "barcode", label: "Barcode", optional: true },
      { key: "address", label: "Address", optional: true },
      { key: "isMain", label: "Main Warehouse", type: "bool" }
    ]
  },
  locations: {
    basePath: "/api/locations",
    idKeys: ["locationId", "id"],
    fields: [
      { key: "name", label: "Name", required: true },
      { key: "shippingFees", label: "Shipping Fees", type: "number" },
      { key: "shippingFeesCurrencyCode", label: "Shipping Currency", defaultValue: "USD" }
    ]
  },
  roles: {
    basePath: "/api/roles",
    fields: [
      { key: "name", label: "Name", required: true, update: false },
      { key: "description", label: "Description", optional: true },
      { key: "badgeColor", label: "Badge Color", defaultValue: "#22FFFFFF" },
      { key: "badgeTextColor", label: "Badge Text Color", defaultValue: "#FFFFFF" },
      { key: "isActive", label: "Active", type: "bool", defaultValue: true, create: false }
    ]
  },
  "transaction-types": {
    basePath: "/api/transactiontypes",
    fields: [
      { key: "name", label: "Name", required: true },
      { key: "currencyCode", label: "Currency", defaultValue: "USD" },
      { key: "counterRate", label: "Counter Rate", type: "number", defaultValue: "1" },
      { key: "serialNumberFormat", label: "Serial Format" },
      { key: "startNumber", label: "Start Number", type: "number", defaultValue: "1" },
      { key: "currentNumber", label: "Current Number", type: "number" },
      { key: "isActive", label: "Active", type: "bool", defaultValue: true }
    ]
  },
  categories: {
    basePath: "/api/categories",
    canUpdate: false,
    canDelete: false,
    fields: [
      { key: "name", label: "Name", required: true },
      { key: "parentId", label: "Parent Id", type: "number", optional: true }
    ]
  }
};

module.exports = {
  crudConfigs
};
