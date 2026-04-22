using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelImportCoordinator
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
        private readonly ICrudApiClient _crudApi;
        private readonly IReadOnlyDictionary<string, ExcelImportTargetDefinition> _targets;

        public ExcelImportCoordinator(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            _targets = BuildTargets().ToDictionary(target => target.Key, StringComparer.OrdinalIgnoreCase);
        }

        public IReadOnlyList<ExcelImportTargetOption> GetTargets()
            => _targets.Values
                .OrderBy(target => target.Name, StringComparer.OrdinalIgnoreCase)
                .Select(target => new ExcelImportTargetOption
                {
                    Key = target.Key,
                    Name = target.Name,
                    TableName = target.TableName,
                    Description = target.Description,
                    ColumnCount = target.Fields.Count
                })
                .ToList();

        public IReadOnlyList<ExcelImportFieldDefinition> GetFields(string targetKey)
            => GetTarget(targetKey).Fields;

        public ExcelWorkbookSheet ReadWorkbook(string filePath)
            => new ExcelWorkbookReader().Read(filePath);

        public ExcelImportProfile GetProfile(IEnumerable<AppConstantDto>? appConstants, string targetKey)
        {
            if (appConstants == null)
            {
                return new ExcelImportProfile { TargetKey = targetKey };
            }

            var profileValue = appConstants
                .FirstOrDefault(item => string.Equals(item.Key, BuildProfileKey(targetKey), StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (string.IsNullOrWhiteSpace(profileValue))
            {
                return new ExcelImportProfile { TargetKey = targetKey };
            }

            try
            {
                var profile = JsonSerializer.Deserialize<ExcelImportProfile>(profileValue, JsonOptions);
                if (profile == null)
                {
                    return new ExcelImportProfile { TargetKey = targetKey };
                }

                profile.TargetKey = string.IsNullOrWhiteSpace(profile.TargetKey) ? targetKey : profile.TargetKey;
                profile.Mappings ??= new Dictionary<string, string>();
                return profile;
            }
            catch
            {
                return new ExcelImportProfile { TargetKey = targetKey };
            }
        }

        public IReadOnlyDictionary<string, string?> ResolveMappings(
            string targetKey,
            IReadOnlyList<string> workbookHeaders,
            IEnumerable<AppConstantDto>? appConstants,
            IReadOnlyDictionary<string, string?>? overrides = null)
        {
            var target = GetTarget(targetKey);
            var savedProfile = GetProfile(appConstants, targetKey);
            var headers = workbookHeaders
                .Where(header => !string.IsNullOrWhiteSpace(header))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var resolved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in target.Fields)
            {
                var selectedHeader = ResolveHeaderFromMappings(overrides, field.Key, headers)
                    ?? ResolveHeaderFromProfile(savedProfile, field.Key, headers)
                    ?? FindMatchingHeader(field, headers);

                resolved[field.Key] = selectedHeader;
            }

            return resolved;
        }

        public async Task SaveProfileAsync(string targetKey, IReadOnlyDictionary<string, string?> mappings)
        {
            var target = GetTarget(targetKey);
            var profile = new ExcelImportProfile
            {
                TargetKey = target.Key,
                Mappings = mappings
                    .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                    .ToDictionary(item => item.Key, item => item.Value!.Trim(), StringComparer.OrdinalIgnoreCase)
            };

            await _crudApi.PutAsync(
                $"api/appconstants/{Uri.EscapeDataString(BuildProfileKey(targetKey))}",
                new UpsertAppConstantRequest
                {
                    Value = JsonSerializer.Serialize(profile, JsonOptions),
                    Description = $"Excel import profile for {target.TableName}."
                });
        }

        public async Task<ExcelImportResult> ImportAsync(
            string targetKey,
            string filePath,
            ExcelImportExecutionContext context,
            IReadOnlyDictionary<string, string?>? mappings = null,
            IEnumerable<AppConstantDto>? appConstants = null)
        {
            var target = GetTarget(targetKey);
            var workbook = ReadWorkbook(filePath);
            var resolvedMappings = ResolveMappings(target.Key, workbook.Headers, appConstants, mappings);
            var result = new ExcelImportResult { TargetName = target.TableName };

            if (workbook.RowCount == 0)
            {
                result.AddError("The workbook does not contain any data rows to import.");
                return result;
            }

            var missingRequiredMappings = target.Fields
                .Where(field => field.IsRequired
                    && (!resolvedMappings.TryGetValue(field.Key, out var selectedHeader)
                        || string.IsNullOrWhiteSpace(selectedHeader)))
                .Select(field => field.ColumnName)
                .ToList();

            if (missingRequiredMappings.Count > 0)
            {
                result.AddError($"Map the required columns first: {string.Join(", ", missingRequiredMappings)}.");
                return result;
            }

            foreach (var row in workbook.Rows)
            {
                var values = BuildRowValues(target, row, resolvedMappings);
                if (values.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                try
                {
                    var payload = target.BuildPayload(values, context, row.RowNumber);
                    await _crudApi.PostAsync(target.Endpoint, payload);
                    result.AddImported();
                }
                catch (Exception ex)
                {
                    result.AddError($"Row {row.RowNumber}: {MapImportError(ex)}");
                }
            }

            if (!result.HasImportedRows && !result.HasErrors)
            {
                result.AddError("No mapped rows were found to import.");
            }

            return result;
        }

        private static IReadOnlyDictionary<string, string?> BuildRowValues(
            ExcelImportTargetDefinition target,
            ExcelWorkbookRow row,
            IReadOnlyDictionary<string, string?> resolvedMappings)
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in target.Fields)
            {
                string? value = null;
                if (resolvedMappings.TryGetValue(field.Key, out var selectedHeader)
                    && !string.IsNullOrWhiteSpace(selectedHeader)
                    && row.Cells.TryGetValue(selectedHeader, out var rawValue))
                {
                    value = string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
                }

                values[field.Key] = value;
            }

            return values;
        }

        private static string? ResolveHeaderFromMappings(
            IReadOnlyDictionary<string, string?>? mappings,
            string fieldKey,
            IReadOnlyList<string> workbookHeaders)
        {
            if (mappings == null
                || !mappings.TryGetValue(fieldKey, out var selectedHeader)
                || string.IsNullOrWhiteSpace(selectedHeader))
            {
                return null;
            }

            return workbookHeaders.FirstOrDefault(header =>
                string.Equals(header, selectedHeader, StringComparison.OrdinalIgnoreCase));
        }

        private static string? ResolveHeaderFromProfile(
            ExcelImportProfile profile,
            string fieldKey,
            IReadOnlyList<string> workbookHeaders)
        {
            if (profile.Mappings == null
                || !profile.Mappings.TryGetValue(fieldKey, out var selectedHeader)
                || string.IsNullOrWhiteSpace(selectedHeader))
            {
                return null;
            }

            return workbookHeaders.FirstOrDefault(header =>
                string.Equals(header, selectedHeader, StringComparison.OrdinalIgnoreCase));
        }

        private static string? FindMatchingHeader(ExcelImportFieldDefinition field, IReadOnlyList<string> workbookHeaders)
        {
            var aliases = new List<string> { field.Key, field.ColumnName };
            aliases.AddRange(field.Aliases);

            var normalizedAliases = aliases
                .Select(NormalizeLookupKey)
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return workbookHeaders.FirstOrDefault(header =>
                normalizedAliases.Contains(NormalizeLookupKey(header), StringComparer.OrdinalIgnoreCase));
        }

        private ExcelImportTargetDefinition GetTarget(string targetKey)
        {
            if (_targets.TryGetValue(targetKey, out var target))
            {
                return target;
            }

            throw new DomainValidationException($"Unknown Excel import target '{targetKey}'.", "excel_import_target_missing");
        }

        private static IReadOnlyList<ExcelImportTargetDefinition> BuildTargets()
            => new List<ExcelImportTargetDefinition>
            {
                BuildCustomersTarget(),
                BuildSuppliersTarget(),
                BuildBrandsTarget(),
                BuildCategoriesTarget(),
                BuildPartsTarget(),
                BuildCarBrandsTarget(),
                BuildCarModelsTarget(),
                BuildLocationsTarget(),
                BuildWarehousesTarget(),
                BuildTransactionTypesTarget(),
                BuildUsedCarsTarget(),
                BuildAccountsTarget(),
                BuildAccountTypesTarget(),
                BuildPostingRolesTarget(),
                BuildRolesTarget(),
                BuildUsersTarget()
            };

        private static ExcelImportTargetDefinition BuildCustomersTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Customers,
                Name = "Customers",
                TableName = "Customers",
                Description = "Import customer records into the customer management screen.",
                Endpoint = "api/customers",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Customer name.", "customer", "customername"),
                    Field("phone", "Phone", ExcelImportDataType.Text, false, "Phone number.", "mobile", "telephone", "cell"),
                    Field("email", "Email", ExcelImportDataType.Text, false, "Email address.", "mail"),
                    Field("address", "Address", ExcelImportDataType.Text, false, "Postal address.", "location"),
                    Field("tax_number", "Tax Number", ExcelImportDataType.Text, false, "Tax or VAT number.", "taxnumber", "taxno", "vat", "tin"),
                    Field("opening_balance", "Opening Balance", ExcelImportDataType.Decimal, false, "Opening balance amount.", "balance", "openingbalance", "initialbalance")
                },
                BuildPayload = (values, _, rowNumber) => new CreateCustomerRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    Phone = NormalizeOptional(GetValue(values, "phone")),
                    Email = NormalizeOptional(GetValue(values, "email")),
                    Address = NormalizeOptional(GetValue(values, "address")),
                    TaxNumber = NormalizeOptional(GetValue(values, "tax_number")),
                    OpeningBalance = ParseDecimal(values, "opening_balance", "Opening Balance", rowNumber, 0m)
                }
            };

        private static ExcelImportTargetDefinition BuildSuppliersTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Suppliers,
                Name = "Suppliers",
                TableName = "Suppliers",
                Description = "Import supplier records into the supplier management screen.",
                Endpoint = "api/suppliers",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Supplier name.", "supplier", "suppliername"),
                    Field("phone", "Phone", ExcelImportDataType.Text, false, "Phone number.", "mobile", "telephone", "cell"),
                    Field("email", "Email", ExcelImportDataType.Text, false, "Email address.", "mail"),
                    Field("address", "Address", ExcelImportDataType.Text, false, "Postal address.", "location"),
                    Field("tax_number", "Tax Number", ExcelImportDataType.Text, false, "Tax or VAT number.", "taxnumber", "taxno", "vat", "tin"),
                    Field("opening_balance", "Opening Balance", ExcelImportDataType.Decimal, false, "Opening balance amount.", "balance", "openingbalance", "initialbalance")
                },
                BuildPayload = (values, _, rowNumber) => new CreateSupplierRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    Phone = NormalizeOptional(GetValue(values, "phone")),
                    Email = NormalizeOptional(GetValue(values, "email")),
                    Address = NormalizeOptional(GetValue(values, "address")),
                    TaxNumber = NormalizeOptional(GetValue(values, "tax_number")),
                    OpeningBalance = ParseDecimal(values, "opening_balance", "Opening Balance", rowNumber, 0m)
                }
            };

        private static ExcelImportTargetDefinition BuildBrandsTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Brands,
                Name = "Part Brands",
                TableName = "Brands",
                Description = "Import inventory brand rows into the brands screen.",
                Endpoint = "api/brands",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Brand name.", "brand", "brandname"),
                    Field("is_active", "Is Active", ExcelImportDataType.Boolean, false, "True or false.", "active", "enabled", "isactive")
                },
                BuildPayload = (values, _, rowNumber) => new CreateBrandRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    IsActive = ParseBoolean(values, "is_active", rowNumber, true)
                }
            };

        private static ExcelImportTargetDefinition BuildCategoriesTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Categories,
                Name = "Categories",
                TableName = "Categories",
                Description = "Import inventory categories and optional parent links.",
                Endpoint = "api/categories",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Category name.", "category", "categoryname"),
                    Field("parent", "Parent Category", ExcelImportDataType.Lookup, false, "Existing parent category id or name.", "parent", "parentcategory", "parentid")
                },
                BuildPayload = (values, context, rowNumber) => new CreateCategoryRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    ParentId = ResolveOptionalLookupId(GetValue(values, "parent"), "Parent Category", rowNumber, context.Categories, category => category.Id, category => category.Name)
                }
            };

        private static ExcelImportTargetDefinition BuildPartsTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Parts,
                Name = "Parts",
                TableName = "Parts",
                Description = "Import parts with smart category and brand lookup.",
                Endpoint = "api/parts",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("internal_code", "Internal Code", ExcelImportDataType.Text, true, "Internal part code.", "code", "partcode", "itemcode", "internalcode"),
                    Field("barcode", "Barcode", ExcelImportDataType.Text, false, "Barcode or scan code.", "barcodeno"),
                    Field("name", "Name", ExcelImportDataType.Text, true, "Part name or description.", "partname", "description"),
                    Field("oem_number", "OEM Number", ExcelImportDataType.Text, false, "OEM number.", "oem", "oemno", "oemnumber", "oemcode"),
                    Field("condition", "Condition", ExcelImportDataType.Enum, false, "New, Used, or Rebuilt.", "partcondition"),
                    Field("category", "Category", ExcelImportDataType.Lookup, false, "Category id or name. If empty, the first category is used.", "categoryid", "categoryname"),
                    Field("brand", "Brand", ExcelImportDataType.Lookup, false, "Brand id or name.", "brandid", "brandname"),
                    Field("cost_price", "Cost Price", ExcelImportDataType.Decimal, false, "Cost amount.", "cost", "purchaseprice", "buyprice"),
                    Field("sale_price", "Sale Price", ExcelImportDataType.Decimal, false, "Sale amount.", "sale", "sellingprice", "retailprice"),
                    Field("average_price", "Average Price", ExcelImportDataType.Decimal, false, "Average price amount.", "average", "avgprice"),
                    Field("currency", "Currency", ExcelImportDataType.Currency, false, "Three-letter currency code.", "currencycode"),
                    Field("min_stock", "Min Stock", ExcelImportDataType.Integer, false, "Minimum stock quantity.", "minimumstock", "minimumqty", "stockminimum"),
                    Field("notes", "Notes", ExcelImportDataType.Text, false, "Free-text notes.", "note", "remarks"),
                    Field("used_car", "Used Car", ExcelImportDataType.Lookup, false, "Linked used car id or display name.", "usedcarid", "car")
                },
                BuildPayload = (values, context, rowNumber) => new CreatePartRequest
                {
                    InternalCode = RequireValue(values, "internal_code", "Internal Code", rowNumber).Trim(),
                    Barcode = NormalizeOptional(GetValue(values, "barcode")),
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    OEMNumber = NormalizeOptional(GetValue(values, "oem_number")),
                    Condition = ParsePartCondition(GetValue(values, "condition"), rowNumber),
                    CategoryId = ResolvePartCategoryId(GetValue(values, "category"), context, rowNumber),
                    BrandId = ResolveOptionalLookupId(GetValue(values, "brand"), "Brand", rowNumber, context.Brands, brand => brand.Id, brand => brand.Name),
                    CostPrice = ParseDecimal(values, "cost_price", "Cost Price", rowNumber, 0m),
                    SalePrice = ParseDecimal(values, "sale_price", "Sale Price", rowNumber, 0m),
                    AveragePrice = ParseOptionalDecimal(values, "average_price", "Average Price", rowNumber),
                    Currency = ParseCurrencyCode(GetValue(values, "currency"), context.BaseCurrencyCode),
                    MinStock = ParseInteger(values, "min_stock", "Min Stock", rowNumber, 0),
                    Notes = NormalizeOptional(GetValue(values, "notes")),
                    UsedCarId = ResolveOptionalLookupId(
                        GetValue(values, "used_car"),
                        "Used Car",
                        rowNumber,
                        context.UsedCars,
                        usedCar => usedCar.Id,
                        usedCar => usedCar.Car,
                        usedCar => $"{usedCar.Id}",
                        usedCar => $"{usedCar.Car} {usedCar.ModelYear}")
                }
            };

        private static ExcelImportTargetDefinition BuildCarBrandsTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.CarBrands,
                Name = "Car Brands",
                TableName = "CarBrands",
                Description = "Import car brand rows for the car catalog.",
                Endpoint = "api/carbrands",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Brand name.", "brand", "brandname"),
                    Field("country", "Country", ExcelImportDataType.Text, false, "Country label.", "origincountry"),
                    Field("region_group", "Region Group", ExcelImportDataType.Text, false, "Region grouping label.", "region", "regiongroup"),
                    Field("sort_order", "Sort Order", ExcelImportDataType.Integer, false, "Display sort order.", "sort", "sortorder", "displayorder")
                },
                BuildPayload = (values, _, rowNumber) => new CreateCarBrandRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    Country = NormalizeOptional(GetValue(values, "country")) ?? string.Empty,
                    RegionGroup = NormalizeOptional(GetValue(values, "region_group")) ?? string.Empty,
                    SortOrder = ParseInteger(values, "sort_order", "Sort Order", rowNumber, 0)
                }
            };

        private static ExcelImportTargetDefinition BuildCarModelsTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.CarModels,
                Name = "Car Models",
                TableName = "CarModels",
                Description = "Import car model rows and resolve the parent car brand automatically.",
                Endpoint = "api/carmodels",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("car_brand", "Car Brand", ExcelImportDataType.Lookup, true, "Existing car brand id or name.", "brand", "carbrandid", "carbrandname"),
                    Field("name", "Name", ExcelImportDataType.Text, true, "Car model name.", "model", "modelname"),
                    Field("body_type", "Body Type", ExcelImportDataType.Text, false, "Body type label.", "body", "bodytype")
                },
                BuildPayload = (values, context, rowNumber) => new CreateCarModelRequest
                {
                    CarBrandId = ResolveRequiredLookupId(GetValue(values, "car_brand"), "Car Brand", rowNumber, context.CarBrands, brand => brand.Id, brand => brand.Name),
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    BodyType = NormalizeOptional(GetValue(values, "body_type")) ?? string.Empty
                }
            };

        private static ExcelImportTargetDefinition BuildLocationsTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Locations,
                Name = "Locations",
                TableName = "Locations",
                Description = "Import location rows with shipping fees and currency.",
                Endpoint = "api/locations",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Location name.", "location", "locationname"),
                    Field("shipping_fees", "Shipping Fees", ExcelImportDataType.Decimal, false, "Shipping fees amount.", "shipping", "shippingfee", "shippingcost"),
                    Field("shipping_currency", "Shipping Currency", ExcelImportDataType.Currency, false, "Three-letter currency code.", "shippingcurrency", "shippingfeescurrencycode", "currency")
                },
                BuildPayload = (values, context, rowNumber) => new CreateLocationRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    ShippingFees = ParseDecimal(values, "shipping_fees", "Shipping Fees", rowNumber, 0m),
                    ShippingFeesCurrencyCode = ParseCurrencyCode(GetValue(values, "shipping_currency"), context.CounterCurrencyCode)
                }
            };

        private static ExcelImportTargetDefinition BuildWarehousesTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Warehouses,
                Name = "Warehouses",
                TableName = "Warehouses",
                Description = "Import warehouse rows into the warehouse management screen.",
                Endpoint = "api/warehouses",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Warehouse name.", "warehouse", "warehousename"),
                    Field("address", "Address", ExcelImportDataType.Text, false, "Warehouse address.", "location", "warehouseaddress"),
                    Field("is_main", "Is Main", ExcelImportDataType.Boolean, false, "True or false.", "main", "ismain", "primary")
                },
                BuildPayload = (values, _, rowNumber) => new CreateWarehouseRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    Address = NormalizeOptional(GetValue(values, "address")),
                    IsMain = ParseBoolean(values, "is_main", rowNumber, false)
                }
            };

        private static ExcelImportTargetDefinition BuildTransactionTypesTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.TransactionTypes,
                Name = "Transaction Types",
                TableName = "TransactionTypes",
                Description = "Import transaction type rows including numbering settings.",
                Endpoint = "api/transactiontypes",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Transaction type name.", "transactiontype", "transactiontypename"),
                    Field("currency", "Currency", ExcelImportDataType.Currency, false, "Three-letter currency code.", "currencycode"),
                    Field("counter_rate", "Counter Rate", ExcelImportDataType.Decimal, false, "Rate to base currency.", "rate", "counterrate"),
                    Field("serial_number_format", "Serial Number Format", ExcelImportDataType.Text, false, "Serial number format string.", "serialformat", "serialnumberformat"),
                    Field("start_number", "Start Number", ExcelImportDataType.Integer, false, "Start number for numbering.", "startno", "startnumber"),
                    Field("current_number", "Current Number", ExcelImportDataType.Integer, false, "Current used number.", "currentno", "currentnumber"),
                    Field("is_active", "Is Active", ExcelImportDataType.Boolean, false, "True or false.", "active", "enabled", "isactive")
                },
                BuildPayload = (values, context, rowNumber) => new CreateTransactionTypeRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    CurrencyCode = ParseCurrencyCode(GetValue(values, "currency"), context.CounterCurrencyCode),
                    CounterRate = ParseDecimal(values, "counter_rate", "Counter Rate", rowNumber, context.DefaultCounterRate > 0 ? context.DefaultCounterRate : 1m),
                    SerialNumberFormat = NormalizeOptional(GetValue(values, "serial_number_format")) ?? "TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}",
                    StartNumber = ParseLong(values, "start_number", "Start Number", rowNumber, 1L),
                    CurrentNumber = ParseLong(values, "current_number", "Current Number", rowNumber, 0L),
                    IsActive = ParseBoolean(values, "is_active", rowNumber, true)
                }
            };

        private static ExcelImportTargetDefinition BuildUsedCarsTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.UsedCars,
                Name = "Used Cars",
                TableName = "UsedCars",
                Description = "Import used cars and resolve supplier, model, and location lookups.",
                Endpoint = "api/usedcars",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("supplier", "Supplier", ExcelImportDataType.Lookup, true, "Existing supplier id or name.", "supplierid", "suppliername"),
                    Field("car_model", "Car Model", ExcelImportDataType.Lookup, true, "Existing car model id or display name.", "carmodelid", "model", "modelname"),
                    Field("model_year", "Model Year", ExcelImportDataType.Integer, true, "Car model year.", "year"),
                    Field("price_currency", "Price Currency", ExcelImportDataType.Currency, false, "Three-letter currency code.", "currency", "currencycode"),
                    Field("price", "Price", ExcelImportDataType.Decimal, true, "Purchase price amount.", "amount"),
                    Field("location", "Location", ExcelImportDataType.Lookup, true, "Existing location id or name.", "locationid", "locationname"),
                    Field("is_received", "Is Received", ExcelImportDataType.Boolean, false, "True or false.", "received"),
                    Field("is_shipped", "Is Shipped", ExcelImportDataType.Boolean, false, "True or false.", "shipped"),
                    Field("part_out", "Part-Out", ExcelImportDataType.Decimal, false, "Part-out amount.", "partout"),
                    Field("shipping", "Shipping", ExcelImportDataType.Decimal, false, "Shipping amount.", "shippingfees"),
                    Field("customs", "Customs", ExcelImportDataType.Decimal, false, "Customs amount.", "custom")
                },
                BuildPayload = (values, context, rowNumber) => new CreateUsedCarRequest
                {
                    SupplierId = ResolveRequiredLookupId(GetValue(values, "supplier"), "Supplier", rowNumber, context.Suppliers, supplier => supplier.Id, supplier => supplier.Name),
                    CarModelId = ResolveRequiredLookupId(
                        GetValue(values, "car_model"),
                        "Car Model",
                        rowNumber,
                        context.CarModels,
                        model => model.Id,
                        model => model.Name,
                        model => model.CarBrandName,
                        model => $"{model.CarBrandName} {model.Name}",
                        model => $"{model.CarBrandName} {model.Name} {model.BodyType}"),
                    ModelYear = ParseInteger(values, "model_year", "Model Year", rowNumber),
                    PriceCurrency = ParseCurrencyCode(GetValue(values, "price_currency"), context.BaseCurrencyCode),
                    Price = ParseDecimal(values, "price", "Price", rowNumber),
                    LocationId = ResolveRequiredLookupId(GetValue(values, "location"), "Location", rowNumber, context.Locations, location => location.LocationId, location => location.Name),
                    IsReceived = ParseBoolean(values, "is_received", rowNumber, false),
                    IsShipped = ParseBoolean(values, "is_shipped", rowNumber, false),
                    PartOut = ParseDecimal(values, "part_out", "Part-Out", rowNumber, 0m),
                    Shipping = ParseDecimal(values, "shipping", "Shipping", rowNumber, 0m),
                    Customs = ParseDecimal(values, "customs", "Customs", rowNumber, 0m)
                }
            };

        private static ExcelImportTargetDefinition BuildAccountsTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Accounts,
                Name = "Accounts",
                TableName = "Accounts",
                Description = "Import chart of accounts rows with dynamic account type lookup.",
                Endpoint = "api/accounts",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("code", "Code", ExcelImportDataType.Text, true, "Account code.", "accountcode"),
                    Field("name", "Name", ExcelImportDataType.Text, true, "Account name.", "accountname"),
                    Field("account_type", "Account Type", ExcelImportDataType.Lookup, false, "Account type key or label.", "accounttype", "accounttypekey"),
                    Field("parent", "Parent Account", ExcelImportDataType.Lookup, false, "Parent account id, code, or name.", "parentaccount", "parentid", "parentcode")
                },
                BuildPayload = (values, context, rowNumber) => new CreateAccountRequest
                {
                    Code = RequireValue(values, "code", "Code", rowNumber).Trim(),
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    AccountTypeKey = ResolveAccountTypeKey(GetValue(values, "account_type"), context),
                    ParentId = ResolveOptionalLookupId(GetValue(values, "parent"), "Parent Account", rowNumber, context.Accounts, account => account.Id, account => account.Code, account => account.Name, account => account.DisplayName)
                }
            };

        private static ExcelImportTargetDefinition BuildAccountTypesTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.AccountTypes,
                Name = "Account Types",
                TableName = "AccountTypes",
                Description = "Import dynamic accounting account type definitions.",
                Endpoint = "api/accounting/account-types",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("type_key", "Type Key", ExcelImportDataType.Text, true, "Unique account type key.", "key", "accounttypekey"),
                    Field("label", "Label", ExcelImportDataType.Text, true, "Display label.", "name"),
                    Field("description", "Description", ExcelImportDataType.Text, false, "Description text.", "details"),
                    Field("sort_order", "Sort Order", ExcelImportDataType.Integer, false, "Display sort order.", "sort", "sortorder")
                },
                BuildPayload = (values, _, rowNumber) => new SaveAccountTypeRequest
                {
                    TypeKey = RequireValue(values, "type_key", "Type Key", rowNumber).Trim(),
                    Label = RequireValue(values, "label", "Label", rowNumber).Trim(),
                    Description = NormalizeOptional(GetValue(values, "description")) ?? string.Empty,
                    SortOrder = ParseInteger(values, "sort_order", "Sort Order", rowNumber, 10)
                }
            };

        private static ExcelImportTargetDefinition BuildPostingRolesTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.PostingRoles,
                Name = "Posting Roles",
                TableName = "PostingRoles",
                Description = "Import dynamic accounting posting role definitions.",
                Endpoint = "api/accounting/posting-roles",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("role_key", "Role Key", ExcelImportDataType.Text, true, "Unique posting role key.", "key", "postingrolekey"),
                    Field("label", "Label", ExcelImportDataType.Text, true, "Display label.", "name"),
                    Field("description", "Description", ExcelImportDataType.Text, false, "Description text.", "details"),
                    Field("sort_order", "Sort Order", ExcelImportDataType.Integer, false, "Display sort order.", "sort", "sortorder")
                },
                BuildPayload = (values, _, rowNumber) => new SavePostingRoleRequest
                {
                    RoleKey = RequireValue(values, "role_key", "Role Key", rowNumber).Trim(),
                    Label = RequireValue(values, "label", "Label", rowNumber).Trim(),
                    Description = NormalizeOptional(GetValue(values, "description")) ?? string.Empty,
                    SortOrder = ParseInteger(values, "sort_order", "Sort Order", rowNumber, 10)
                }
            };

        private static ExcelImportTargetDefinition BuildRolesTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Roles,
                Name = "Roles",
                TableName = "Roles",
                Description = "Import application role definitions.",
                Endpoint = "api/roles",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("name", "Name", ExcelImportDataType.Text, true, "Role name.", "rolename"),
                    Field("description", "Description", ExcelImportDataType.Text, false, "Role description.", "details"),
                    Field("badge_color", "Badge Color", ExcelImportDataType.Text, false, "Badge background color.", "badgecolor", "color"),
                    Field("badge_text_color", "Badge Text Color", ExcelImportDataType.Text, false, "Badge text color.", "badgetextcolor", "textcolor")
                },
                BuildPayload = (values, _, rowNumber) => new CreateRoleRequest
                {
                    Name = RequireValue(values, "name", "Name", rowNumber).Trim(),
                    Description = NormalizeOptional(GetValue(values, "description")),
                    BadgeColor = NormalizeOptional(GetValue(values, "badge_color")) ?? "#22FFFFFF",
                    BadgeTextColor = NormalizeOptional(GetValue(values, "badge_text_color")) ?? "#FFFFFF"
                }
            };

        private static ExcelImportTargetDefinition BuildUsersTarget()
            => new()
            {
                Key = ExcelImportTargetKeys.Users,
                Name = "Users",
                TableName = "Users",
                Description = "Import application users and assign them to existing roles.",
                Endpoint = "api/users",
                Fields = new List<ExcelImportFieldDefinition>
                {
                    Field("username", "Username", ExcelImportDataType.Text, true, "Login username.", "user", "login"),
                    Field("full_name", "Full Name", ExcelImportDataType.Text, true, "User display name.", "fullname", "name"),
                    Field("email", "Email", ExcelImportDataType.Text, false, "Email address.", "mail"),
                    Field("password", "Password", ExcelImportDataType.Text, true, "Initial password.", "pass"),
                    Field("role", "Role", ExcelImportDataType.Lookup, false, "Existing role name.", "rolename")
                },
                BuildPayload = (values, context, rowNumber) => new CreateUserRequest
                {
                    Username = RequireValue(values, "username", "Username", rowNumber).Trim(),
                    FullName = RequireValue(values, "full_name", "Full Name", rowNumber).Trim(),
                    Email = NormalizeOptional(GetValue(values, "email")),
                    Password = RequireValue(values, "password", "Password", rowNumber),
                    Role = ResolveRoleName(GetValue(values, "role"), context)
                }
            };

        private static ExcelImportFieldDefinition Field(string key, string columnName, ExcelImportDataType dataType, bool isRequired, string description, params string[] aliases)
            => new()
            {
                Key = key,
                ColumnName = columnName,
                DataType = dataType,
                IsRequired = isRequired,
                Description = description,
                Aliases = aliases
            };

        private static string? GetValue(IReadOnlyDictionary<string, string?> values, string key)
            => values.TryGetValue(key, out var value) ? value : null;

        private static string RequireValue(IReadOnlyDictionary<string, string?> values, string key, string label, int rowNumber)
        {
            var value = GetValue(values, key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            throw new DomainValidationException($"{label} is required.", $"excel_import_{NormalizeLookupKey(label)}_required_row_{rowNumber}");
        }

        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static decimal ParseDecimal(IReadOnlyDictionary<string, string?> values, string key, string label, int rowNumber, decimal? defaultValue = null)
            => ParseDecimal(GetValue(values, key), label, rowNumber, defaultValue);

        private static decimal ParseDecimal(string? rawValue, string label, int rowNumber, decimal? defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue ?? throw new DomainValidationException($"{label} is required.", $"excel_import_{NormalizeLookupKey(label)}_required_row_{rowNumber}");
            }

            if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
                || decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            throw new DomainValidationException($"{label} '{rawValue}' is not a valid number.", $"excel_import_{NormalizeLookupKey(label)}_invalid_row_{rowNumber}");
        }

        private static decimal? ParseOptionalDecimal(IReadOnlyDictionary<string, string?> values, string key, string label, int rowNumber)
        {
            var rawValue = GetValue(values, key);
            return string.IsNullOrWhiteSpace(rawValue) ? null : ParseDecimal(rawValue, label, rowNumber);
        }

        private static int ParseInteger(IReadOnlyDictionary<string, string?> values, string key, string label, int rowNumber, int? defaultValue = null)
            => ParseInteger(GetValue(values, key), label, rowNumber, defaultValue);

        private static int ParseInteger(string? rawValue, string label, int rowNumber, int? defaultValue = null)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue ?? throw new DomainValidationException($"{label} is required.", $"excel_import_{NormalizeLookupKey(label)}_required_row_{rowNumber}");
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed)
                || int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            throw new DomainValidationException($"{label} '{rawValue}' is not a valid whole number.", $"excel_import_{NormalizeLookupKey(label)}_invalid_row_{rowNumber}");
        }

        private static long ParseLong(IReadOnlyDictionary<string, string?> values, string key, string label, int rowNumber, long? defaultValue = null)
        {
            var rawValue = GetValue(values, key);
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue ?? throw new DomainValidationException($"{label} is required.", $"excel_import_{NormalizeLookupKey(label)}_required_row_{rowNumber}");
            }

            if (long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out var parsed)
                || long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            throw new DomainValidationException($"{label} '{rawValue}' is not a valid whole number.", $"excel_import_{NormalizeLookupKey(label)}_invalid_row_{rowNumber}");
        }

        private static bool ParseBoolean(IReadOnlyDictionary<string, string?> values, string key, int rowNumber, bool defaultValue = false)
            => ParseBoolean(GetValue(values, key), rowNumber, defaultValue);

        private static bool ParseBoolean(string? rawValue, int rowNumber, bool defaultValue = false)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            return NormalizeLookupKey(rawValue) switch
            {
                "1" or "true" or "yes" or "y" or "on" => true,
                "0" or "false" or "no" or "n" or "off" => false,
                _ => throw new DomainValidationException(
                    $"Boolean value '{rawValue}' is invalid. Use True/False, Yes/No, or 1/0.",
                    $"excel_import_boolean_invalid_row_{rowNumber}")
            };
        }

        private static string ParseCurrencyCode(string? rawValue, string fallbackCurrencyCode)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return NormalizeCurrencyCode(fallbackCurrencyCode) ?? "USD";
            }

            var normalized = NormalizeCurrencyCode(rawValue);
            if (normalized != null)
            {
                return normalized;
            }

            throw new DomainValidationException($"Currency '{rawValue}' must be a 3-letter code.", "excel_import_currency_invalid");
        }

        private static PartCondition ParsePartCondition(string? rawValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return PartCondition.New;
            }

            return NormalizeLookupKey(rawValue) switch
            {
                "1" or "new" => PartCondition.New,
                "2" or "used" => PartCondition.Used,
                "3" or "rebuilt" => PartCondition.Rebuilt,
                _ => throw new DomainValidationException(
                    $"Condition '{rawValue}' is invalid. Use New, Used, or Rebuilt.",
                    $"excel_import_condition_invalid_row_{rowNumber}")
            };
        }

        private static int ResolvePartCategoryId(string? rawValue, ExcelImportExecutionContext context, int rowNumber)
        {
            if (context.Categories.Count == 0)
            {
                throw new DomainValidationException("Create at least one category before importing parts.", "excel_import_part_category_missing");
            }

            return ResolveOptionalLookupId(rawValue, "Category", rowNumber, context.Categories, category => category.Id, category => category.Name)
                ?? context.Categories.OrderBy(category => category.Id).Select(category => category.Id).First();
        }

        private static string ResolveAccountTypeKey(string? rawValue, ExcelImportExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return string.Empty;
            }

            var normalized = NormalizeLookupKey(rawValue);
            var match = context.AccountTypes.FirstOrDefault(type =>
                string.Equals(type.TypeKey, rawValue.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeLookupKey(type.Label), normalized, StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeLookupKey(type.TypeKey), normalized, StringComparison.OrdinalIgnoreCase));

            return match?.TypeKey ?? rawValue.Trim();
        }

        private static string ResolveRoleName(string? rawValue, ExcelImportExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return context.RoleNames.FirstOrDefault() ?? "Cashier";
            }

            var normalized = NormalizeLookupKey(rawValue);
            var match = context.RoleNames.FirstOrDefault(role =>
                string.Equals(role, rawValue.Trim(), StringComparison.OrdinalIgnoreCase)
                || string.Equals(NormalizeLookupKey(role), normalized, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(match))
            {
                return match;
            }

            if (context.RoleNames.Count == 0)
            {
                return rawValue.Trim();
            }

            throw new DomainValidationException($"Role '{rawValue}' was not found.", "excel_import_role_missing");
        }

        private static int ResolveRequiredLookupId<T>(string? rawValue, string label, int rowNumber, IEnumerable<T> items, Func<T, int> idSelector, params Func<T, string?>[] nameSelectors)
            => ResolveOptionalLookupId(rawValue, label, rowNumber, items, idSelector, nameSelectors)
                ?? throw new DomainValidationException($"{label} is required.", $"excel_import_{NormalizeLookupKey(label)}_required_row_{rowNumber}");

        private static int? ResolveOptionalLookupId<T>(string? rawValue, string label, int rowNumber, IEnumerable<T> items, Func<T, int> idSelector, params Func<T, string?>[] nameSelectors)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            var materialized = items.ToList();
            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var explicitId))
            {
                var matchById = materialized.FirstOrDefault(item => idSelector(item) == explicitId);
                if (matchById != null)
                {
                    return idSelector(matchById);
                }
            }

            var normalized = NormalizeLookupKey(rawValue);
            var match = materialized.FirstOrDefault(item =>
                nameSelectors.Any(selector =>
                    string.Equals(NormalizeLookupKey(selector(item)), normalized, StringComparison.OrdinalIgnoreCase)));

            if (match != null)
            {
                return idSelector(match);
            }

            throw new DomainValidationException($"{label} '{rawValue}' was not found.", $"excel_import_{NormalizeLookupKey(label)}_missing_row_{rowNumber}");
        }

        private static string NormalizeLookupKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
        }

        private static string? NormalizeCurrencyCode(string? currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                return null;
            }

            var normalized = currencyCode.Trim().ToUpperInvariant();
            return normalized.Length == 3 ? normalized : null;
        }

        private static string BuildProfileKey(string targetKey)
            => $"ExcelImportProfile.{targetKey.Trim().ToLowerInvariant()}";

        private static string MapImportError(Exception exception)
            => exception switch
            {
                DomainValidationException validation => validation.Message,
                ApiClientException apiException => $"API error ({apiException.Code}): {apiException.Message}",
                _ when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
                _ => "Unexpected error while importing Excel rows."
            };
    }
}
