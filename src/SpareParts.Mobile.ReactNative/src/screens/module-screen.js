const React = require("react");
const { Image, Pressable, ScrollView, Text, View } = require("react-native");
const ImagePicker = require("expo-image-picker");
const { asRows, money, rowAmount, rowSubtitle, rowTitle, shortDateTime } = require("../core/formatters");
const { EmptyState, Field, ListRow, Panel, PrimaryButton, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

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

function reportCell(value) {
  if (value === null || value === undefined || value === "") return "-";
  if (typeof value === "boolean") return value ? "Yes" : "No";
  if (typeof value === "number") return Number.isInteger(value) ? String(value) : value.toLocaleString(undefined, { maximumFractionDigits: 2 });
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}T/.test(value)) {
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? value : date.toLocaleString();
  }
  return String(value);
}

function numericValue(value, fallback = 0) {
  const number = Number(value);
  return Number.isFinite(number) ? number : fallback;
}

function todayInputValue() {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

function tableKey(table) {
  return String(read(table, "key") || "");
}

function tableTitle(table) {
  return read(table, "displayName") || tableKey(table) || "Table";
}

function columnKey(column) {
  return String(read(column, "key") || read(column, "qualifiedColumnName") || "");
}

function columnTitle(column) {
  return read(column, "qualifiedDisplayName", "displayName", "columnName") || "Column";
}

function reportRowText(row, columns, fallback) {
  const visibleColumns = columns.slice(0, 4);
  const parts = visibleColumns.map((column) => {
    const name = read(column, "displayName", "key");
    const key = read(column, "columnName", "key");
    return `${name}: ${reportCell(row[key])}`;
  });
  return parts.length ? parts.join(" / ") : fallback;
}

function splitEndpoints(endpoint) {
  return String(endpoint || "")
    .split("+")
    .map((item) => item.trim())
    .filter(Boolean);
}

function loadModuleRows(api, endpoint) {
  const normalized = String(endpoint || "").split("?")[0].replace(/\/+$/, "");
  return /^\/?api\/parts$/i.test(normalized) ? api.getAllPages(endpoint) : api.get(endpoint);
}

function visualMatchTitle(match) {
  return [read(match, "internalCode"), read(match, "partName")].filter(Boolean).join(" - ") || `Part #${read(match, "partId")}`;
}

function ModuleSummary({ module, moduleTitle, t }) {
  return el(Panel, { title: t("module.wpfWorkspace", "WPF workspace") },
    el(ListRow, { title: t("module.api", "API"), subtitle: module.endpoint }),
    module.capabilities.map((capability) =>
      el(ListRow, { key: capability, title: capability })
    )
  );
}

function defaultReservationExpiresAt() {
  const date = new Date();
  date.setDate(date.getDate() + 1);
  date.setHours(18, 0, 0, 0);
  return date;
}

function requestSignal(row) {
  if (read(row, "isReservationReminderDue")) return "Reminder due";
  if (read(row, "isReserved")) return "Reserved";
  if (read(row, "isReadyToContact")) return `${read(row, "waitingCustomerCount") || 1} waiting`;
  return read(row, "status") || "Waiting";
}

function requestClockText(row) {
  const expiresAt = read(row, "reservationExpiresAt");
  if (!read(row, "isReserved") || !expiresAt) return "";
  const prefix = read(row, "isReservationReminderDue")
    ? "Reminder due"
    : read(row, "isReservationOverdue")
      ? "Overdue"
      : "Until";
  return `${prefix} ${shortDateTime(expiresAt)}`;
}

function PartRequestsModuleScreen({ api, module }) {
  const { styles, t } = useTheme();
  const moduleTitle = t(`screens.${module.key}`, module.title);
  const [rows, setRows] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isWorking, setIsWorking] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("module.loading", "Loading {title}...", { title: moduleTitle.toLowerCase() }));
    try {
      setRows(asRows(await api.get("/api/partrequests?status=Active")));
      setStatus(t("module.loaded", "{title} loaded.", { title: moduleTitle }));
    } catch (error) {
      setRows([]);
      setStatus(error.message || t("module.loadError", "Could not load {title}.", { title: moduleTitle.toLowerCase() }));
    } finally {
      setIsLoading(false);
    }
  }, [api, moduleTitle, t]);

  useEffect(() => { load(); }, [load]);

  const reserve = useCallback(async (row, expirationAction) => {
    if (!read(row, "partId")) {
      setStatus(t("partRequests.matchRequired", "Match a catalog part before starting a reservation clock."));
      return;
    }

    setIsWorking(true);
    setStatus(t("partRequests.reserving", "Starting reservation clock..."));
    try {
      await api.post(`/api/partrequests/${read(row, "id")}/reserve`, {
        quantity: Math.max(1, Number(read(row, "quantity") || 1)),
        expiresAt: defaultReservationExpiresAt().toISOString(),
        expirationAction
      });
      setStatus(expirationAction === "StaffReminder"
        ? t("partRequests.reminderSet", "Reserved until tomorrow 6 PM; staff will be reminded.")
        : t("partRequests.reserved", "Reserved until tomorrow 6 PM; stock will auto-release."));
      await load();
    } catch (error) {
      setStatus(error.message || t("partRequests.reserveError", "Could not reserve this request."));
    } finally {
      setIsWorking(false);
    }
  }, [api, load, t]);

  const release = useCallback(async (row) => {
    setIsWorking(true);
    setStatus(t("partRequests.releasing", "Releasing reservation..."));
    try {
      await api.post(`/api/partrequests/${read(row, "id")}/release-reservation`, {
        reason: "Released from mobile app"
      });
      setStatus(t("partRequests.released", "Reservation released."));
      await load();
    } catch (error) {
      setStatus(error.message || t("partRequests.releaseError", "Could not release this reservation."));
    } finally {
      setIsWorking(false);
    }
  }, [api, load, t]);

  const updateStatus = useCallback(async (row, nextStatus) => {
    setIsWorking(true);
    setStatus(t("partRequests.updating", "Updating request..."));
    try {
      await api.put(`/api/partrequests/${read(row, "id")}/status`, { status: nextStatus });
      setStatus(t("partRequests.updated", "Part request updated."));
      await load();
    } catch (error) {
      setStatus(error.message || t("partRequests.updateError", "Could not update this request."));
    } finally {
      setIsWorking(false);
    }
  }, [api, load, t]);

  const reservedCount = rows.filter((row) => read(row, "isReserved")).length;
  const reminderCount = rows.filter((row) => read(row, "isReservationReminderDue")).length;

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: moduleTitle,
      actionTitle: t("common.refresh", "Refresh"),
      onAction: load,
      loading: isLoading
    }),
    el(ModuleSummary, { module, moduleTitle, t }),
    el(Panel, { title: t("partRequests.clock", "Reservation clock") },
      el(ListRow, {
        title: t("partRequests.defaultDeadline", "Default deadline"),
        subtitle: t("partRequests.tomorrowSix", "Tomorrow at 6 PM"),
        value: `${reservedCount} reserved`
      }),
      reminderCount > 0 && el(ListRow, {
        title: t("partRequests.staffReminders", "Staff reminders"),
        subtitle: t("partRequests.needsAttention", "Reservations need attention"),
        value: String(reminderCount)
      })
    ),
    el(StatusText, { value: status }),
    el(Panel, { title: t("module.preview", "Preview") },
      el(View, { style: styles.screenListFrameLarge || styles.screenListFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          rows.map((row, index) => {
            const id = read(row, "id") || index;
            const clock = requestClockText(row);
            return el(View, { key: `part-request-${id}`, style: styles.screenListItem },
              el(ListRow, {
                title: rowTitle(row),
                subtitle: [
                  read(row, "customerName"),
                  read(row, "customerPhone"),
                  requestSignal(row),
                  clock
                ].filter(Boolean).join(" / "),
                value: read(row, "reservedQuantity") ? `${read(row, "reservedQuantity")} held` : rowAmount(row)
              }),
              read(row, "isReserved")
                ? el(View, { style: styles.inlineButtons },
                  el(PrimaryButton, {
                    title: t("partRequests.release", "Release"),
                    onPress: () => release(row),
                    disabled: isWorking,
                    compact: true
                  }),
                  el(PrimaryButton, {
                    title: t("partRequests.fulfilled", "Fulfilled"),
                    onPress: () => updateStatus(row, "Fulfilled"),
                    disabled: isWorking,
                    compact: true
                  })
                )
                : el(View, { style: styles.inlineButtons },
                  el(PrimaryButton, {
                    title: t("partRequests.reserve", "Reserve"),
                    onPress: () => reserve(row, "AutoRelease"),
                    disabled: isWorking || !read(row, "partId"),
                    compact: true
                  }),
                  el(PrimaryButton, {
                    title: t("partRequests.remindStaff", "Remind Staff"),
                    onPress: () => reserve(row, "StaffReminder"),
                    disabled: isWorking || !read(row, "partId"),
                    compact: true
                  }),
                  el(SecondaryButton, {
                    title: t("partRequests.contacted", "Contacted"),
                    onPress: () => updateStatus(row, "Contacted")
                  })
                )
            );
          }),
          rows.length === 0 && el(EmptyState, { text: t("module.noRows", "No {title} rows returned.", { title: moduleTitle.toLowerCase() }) })
        )
      )
    )
  );
}

function UsedCarWholesaleModuleScreen({ api, module }) {
  const { styles, t } = useTheme();
  const moduleTitle = t(`screens.${module.key}`, module.title);
  const [usedCars, setUsedCars] = useState([]);
  const [customers, setCustomers] = useState([]);
  const [sales, setSales] = useState([]);
  const [form, setForm] = useState({
    usedCarId: "",
    customerId: "",
    saleDate: todayInputValue(),
    currencyCode: "USD",
    salePrice: "0",
    paidAmount: "0",
    buyerName: "",
    buyerPhone: "",
    paymentMethod: "",
    notes: "",
    isForParts: false,
    repairDescription: "",
    repairPrice: "0",
    repairItems: [],
    soldAsIsAcknowledged: false
  });
  const [status, setStatus] = useState(t("wholesale.ready", "Choose an unsold used car and record the as-is sale."));
  const [isLoading, setIsLoading] = useState(false);

  const availableCars = useMemo(
    () => usedCars.filter((car) => !read(car, "isWholesaleSold")),
    [usedCars]
  );
  const selectedCar = useMemo(
    () => usedCars.find((car) => String(read(car, "id")) === String(form.usedCarId)) || null,
    [form.usedCarId, usedCars]
  );
  const repairTotal = form.isForParts
    ? 0
    : form.repairItems.reduce((sum, item) => sum + numericValue(item.price), 0);
  const projectedResult = selectedCar
    ? numericValue(form.salePrice) - numericValue(read(selectedCar, "fullCostBase")) - repairTotal
    : 0;

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("module.loading", "Loading {title}...", { title: moduleTitle.toLowerCase() }));
    try {
      const [nextCars, nextCustomers, nextSales] = await Promise.all([
        api.get("/api/usedcars"),
        api.get("/api/customers?pageSize=500"),
        api.get("/api/usedcars/wholesale-sales")
      ]);
      const carRows = asRows(nextCars);
      const customerRows = asRows(nextCustomers);
      setUsedCars(carRows);
      setCustomers(customerRows);
      setSales(asRows(nextSales));
      setForm((current) => {
        const retainedCar = carRows.find((car) => String(read(car, "id")) === String(current.usedCarId));
        const firstAvailableCar = carRows.find((car) => !read(car, "isWholesaleSold"));
        const nextCar = retainedCar || firstAvailableCar;
        return {
          ...current,
          usedCarId: nextCar ? String(read(nextCar, "id")) : "",
          currencyCode: read(nextCar, "baseCurrencyCode") || current.currencyCode || "USD"
        };
      });
      setStatus(t("module.loaded", "{title} loaded.", { title: moduleTitle }));
    } catch (error) {
      setUsedCars([]);
      setSales([]);
      setStatus(error.message || t("module.loadError", "Could not load {title}.", { title: moduleTitle.toLowerCase() }));
    } finally {
      setIsLoading(false);
    }
  }, [api, moduleTitle, t]);

  const chooseCar = useCallback((car) => {
    setForm((current) => ({
      ...current,
      usedCarId: String(read(car, "id")),
      currencyCode: read(car, "baseCurrencyCode") || current.currencyCode || "USD"
    }));
  }, []);

  const chooseCustomer = useCallback((customer) => {
    setForm((current) => ({
      ...current,
      customerId: String(read(customer, "id")),
      buyerName: read(customer, "name") || current.buyerName,
      buyerPhone: read(customer, "phone") || current.buyerPhone
    }));
  }, []);

  const addRepairItem = useCallback(() => {
    if (form.isForParts) {
      setStatus(t("wholesale.repairForPartsSkip", "Repair items are not needed when the car is sold for parts."));
      return;
    }
    const description = form.repairDescription.trim();
    const price = numericValue(form.repairPrice);
    if (!description) {
      setStatus(t("wholesale.repairDescriptionRequired", "Enter a repair item description."));
      return;
    }
    if (price < 0) {
      setStatus(t("wholesale.repairPriceInvalid", "Repair item price cannot be negative."));
      return;
    }

    setForm((current) => ({
      ...current,
      repairDescription: "",
      repairPrice: "0",
      repairItems: [
        ...current.repairItems,
        {
          key: `${Date.now()}-${current.repairItems.length}`,
          description,
          price
        }
      ]
    }));
  }, [form.isForParts, form.repairDescription, form.repairPrice, t]);

  const removeRepairItem = useCallback((key) => {
    setForm((current) => ({
      ...current,
      repairItems: current.repairItems.filter((item) => item.key !== key)
    }));
  }, []);

  const createSale = useCallback(async () => {
    const usedCarId = numericValue(form.usedCarId);
    if (!usedCarId) {
      setStatus(t("wholesale.carRequired", "Choose a used car before recording the sale."));
      return;
    }
    if (selectedCar && read(selectedCar, "isWholesaleSold")) {
      setStatus(t("wholesale.alreadySold", "This used car already has a wholesale sale."));
      return;
    }
    if (numericValue(form.salePrice) <= 0) {
      setStatus(t("wholesale.priceRequired", "Enter a sale price greater than zero."));
      return;
    }
    if (!form.buyerName.trim() && !numericValue(form.customerId)) {
      setStatus(t("wholesale.buyerRequired", "Choose a customer or type the buyer name."));
      return;
    }
    if (!form.soldAsIsAcknowledged) {
      setStatus(t("wholesale.asIsRequired", "Confirm the as-is acknowledgement before recording the sale."));
      return;
    }

    setIsLoading(true);
    setStatus(t("wholesale.saving", "Recording wholesale sale..."));
    try {
      const result = await api.post(`/api/usedcars/${usedCarId}/wholesale-sales`, {
        customerId: numericValue(form.customerId) || null,
        buyerName: form.buyerName,
        buyerPhone: form.buyerPhone,
        saleDate: form.saleDate,
        currencyCode: form.currencyCode || "USD",
        salePrice: Math.max(0, numericValue(form.salePrice)),
        paidAmount: Math.max(0, numericValue(form.paidAmount)),
        isForParts: Boolean(form.isForParts),
        repairItems: form.isForParts
          ? []
          : form.repairItems.map((item) => ({
            description: item.description,
            price: Math.max(0, numericValue(item.price))
          })),
        paymentMethod: form.paymentMethod,
        notes: form.notes,
        soldAsIsAcknowledged: form.soldAsIsAcknowledged
      });
      setStatus(t("wholesale.saved", "Wholesale sale recorded: {number}", { number: read(result, "saleNumber") || "" }));
      setForm((current) => ({
        ...current,
        usedCarId: "",
        customerId: "",
        salePrice: "0",
        paidAmount: "0",
        buyerName: "",
        buyerPhone: "",
        paymentMethod: "",
        notes: "",
        isForParts: false,
        repairDescription: "",
        repairPrice: "0",
        repairItems: [],
        soldAsIsAcknowledged: false
      }));
      await load();
    } catch (error) {
      setStatus(error.message || t("wholesale.saveError", "Could not record wholesale sale."));
    } finally {
      setIsLoading(false);
    }
  }, [api, form, load, selectedCar, t]);

  useEffect(() => { load(); }, [load]);

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: moduleTitle,
      actionTitle: t("common.refresh", "Refresh"),
      onAction: load,
      loading: isLoading
    }),
    el(ModuleSummary, { module, moduleTitle, t }),
    el(Panel, { title: t("wholesale.summary", "Wholesale summary") },
      el(View, { style: styles.metricGrid },
        el(View, { style: styles.metricTile },
          el(Text, { style: styles.metricLabel }, t("wholesale.availableCars", "Available cars")),
          el(Text, { style: styles.metricValue }, String(availableCars.length)),
          el(Text, { style: styles.metricAction }, t("wholesale.availableCarsDetail", "Not yet sold wholesale"))
        ),
        el(View, { style: styles.metricTile },
          el(Text, { style: styles.metricLabel }, t("wholesale.loadedCost", "Loaded cost")),
          el(Text, { style: styles.metricValue }, selectedCar ? money(read(selectedCar, "fullCostBase"), read(selectedCar, "baseCurrencyCode") || "USD") : "-"),
          el(Text, { style: styles.metricAction }, selectedCar ? rowTitle(selectedCar) : t("wholesale.noCarSelected", "No car selected"))
        ),
        el(View, { style: styles.metricTile },
          el(Text, { style: styles.metricLabel }, t("wholesale.repairTotal", "Repair total")),
          el(Text, { style: styles.metricValue }, form.isForParts ? "-" : money(repairTotal, form.currencyCode || "USD")),
          el(Text, { style: styles.metricAction }, form.isForParts ? t("wholesale.forPartsOnly", "For parts only") : t("wholesale.repairTotalDetail", "Added repair items"))
        ),
        el(View, { style: styles.metricTile },
          el(Text, { style: styles.metricLabel }, t("wholesale.projectedResult", "Projected result")),
          el(Text, { style: styles.metricValue }, selectedCar ? money(projectedResult, read(selectedCar, "baseCurrencyCode") || "USD") : "-"),
          el(Text, { style: styles.metricAction }, t("wholesale.projectedResultDetail", "Sale price minus loaded cost and repairs"))
        )
      )
    ),
    el(StatusText, { value: status }),
    el(Panel, { title: t("wholesale.vehicle", "Vehicle") },
      el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.segmentRail
      },
        availableCars.map((car) => {
          const active = String(read(car, "id")) === String(form.usedCarId);
          return el(Pressable, {
            key: `wholesale-car-${read(car, "id")}`,
            style: [styles.segmentButton, active && styles.segmentButtonActive],
            onPress: () => chooseCar(car)
          },
            el(Text, { style: [styles.segmentText, active && styles.segmentTextActive] }, read(car, "car") || rowTitle(car)),
            el(Text, { style: [styles.segmentText, active && styles.segmentTextActive] }, String(read(car, "modelYear") || ""))
          );
        }),
        availableCars.length === 0 && el(EmptyState, { text: t("wholesale.noAvailableCars", "No unsold used cars found.") })
      )
    ),
    el(Panel, { title: t("wholesale.customer", "Customer") },
      el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.segmentRail
      },
        customers.slice(0, 40).map((customer) => {
          const active = String(read(customer, "id")) === String(form.customerId);
          return el(Pressable, {
            key: `wholesale-customer-${read(customer, "id")}`,
            style: [styles.segmentButton, active && styles.segmentButtonActive],
            onPress: () => chooseCustomer(customer)
          },
            el(Text, { style: [styles.segmentText, active && styles.segmentTextActive] }, read(customer, "name") || rowTitle(customer))
          );
        })
      )
    ),
    el(Panel, { title: t("wholesale.saleForm", "Wholesale deal") },
      el(Field, {
        label: t("wholesale.buyerName", "Buyer name"),
        value: form.buyerName,
        onChangeText: (value) => setForm((current) => ({ ...current, buyerName: value }))
      }),
      el(Field, {
        label: t("wholesale.buyerPhone", "Buyer phone"),
        value: form.buyerPhone,
        onChangeText: (value) => setForm((current) => ({ ...current, buyerPhone: value })),
        keyboardType: "phone-pad"
      }),
      el(Field, {
        label: t("wholesale.saleDate", "Sale date"),
        value: form.saleDate,
        onChangeText: (value) => setForm((current) => ({ ...current, saleDate: value })),
        placeholder: "YYYY-MM-DD"
      }),
      el(Field, {
        label: t("wholesale.currency", "Currency"),
        value: form.currencyCode,
        onChangeText: (value) => setForm((current) => ({ ...current, currencyCode: value.toUpperCase() })),
        autoCapitalize: "characters"
      }),
      el(Field, {
        label: t("wholesale.salePrice", "Sale price"),
        value: form.salePrice,
        onChangeText: (value) => setForm((current) => ({ ...current, salePrice: value })),
        keyboardType: "decimal-pad"
      }),
      el(Field, {
        label: t("wholesale.paidAmount", "Paid amount"),
        value: form.paidAmount,
        onChangeText: (value) => setForm((current) => ({ ...current, paidAmount: value })),
        keyboardType: "decimal-pad"
      }),
      el(View, { style: styles.inlineButtons },
        el(Pressable, {
          style: [styles.segmentButton, form.isForParts && styles.segmentButtonActive],
          onPress: () => setForm((current) => ({ ...current, isForParts: !current.isForParts }))
        },
          el(Text, {
            style: [styles.segmentText, form.isForParts && styles.segmentTextActive]
          }, t("wholesale.forPartsOnly", "For parts only"))
        ),
        el(Pressable, {
          style: [styles.segmentButton, form.soldAsIsAcknowledged && styles.segmentButtonActive],
          onPress: () => setForm((current) => ({ ...current, soldAsIsAcknowledged: !current.soldAsIsAcknowledged }))
        },
          el(Text, {
            style: [styles.segmentText, form.soldAsIsAcknowledged && styles.segmentTextActive]
          }, t("wholesale.asIsShort", "As-is confirmed"))
        )
      ),
      !form.isForParts && el(View, { style: styles.screenListItem },
        el(Field, {
          label: t("wholesale.repairDescription", "Repair item"),
          value: form.repairDescription,
          onChangeText: (value) => setForm((current) => ({ ...current, repairDescription: value }))
        }),
        el(Field, {
          label: t("wholesale.repairPrice", "Repair price"),
          value: form.repairPrice,
          onChangeText: (value) => setForm((current) => ({ ...current, repairPrice: value })),
          keyboardType: "decimal-pad"
        }),
        el(SecondaryButton, {
          title: t("wholesale.addRepair", "Add repair"),
          onPress: addRepairItem
        }),
        el(ListRow, {
          title: t("wholesale.repairTotal", "Repair total"),
          value: money(repairTotal, form.currencyCode || "USD")
        }),
        form.repairItems.map((item) =>
          el(View, { key: item.key, style: styles.listRow },
            el(View, { style: styles.listRowCopy },
              el(Text, { style: styles.listRowTitle }, item.description),
              el(Text, { style: styles.listRowSubtitle }, money(item.price, form.currencyCode || "USD"))
            ),
            el(Pressable, {
              style: styles.secondaryButton,
              onPress: () => removeRepairItem(item.key)
            },
              el(Text, { style: styles.secondaryButtonText }, t("common.remove", "Remove"))
            )
          )
        )
      ),
      el(Field, {
        label: t("wholesale.paymentMethod", "Payment method"),
        value: form.paymentMethod,
        onChangeText: (value) => setForm((current) => ({ ...current, paymentMethod: value }))
      }),
      el(Field, {
        label: t("wholesale.notes", "Notes"),
        value: form.notes,
        onChangeText: (value) => setForm((current) => ({ ...current, notes: value })),
        multiline: true,
        numberOfLines: 3
      }),
      el(PrimaryButton, {
        title: isLoading ? t("common.loading", "Loading") : t("wholesale.recordSale", "Record sale"),
        onPress: createSale,
        disabled: isLoading
      })
    ),
    el(Panel, { title: t("wholesale.recentSales", "Recent wholesale sales") },
      el(View, { style: styles.screenListFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          sales.map((row, index) =>
            el(ListRow, {
              key: `wholesale-sale-${read(row, "id") || index}`,
              title: read(row, "saleNumber") || `#${read(row, "id")}`,
              subtitle: [
                read(row, "usedCar"),
                read(row, "buyerName"),
                read(row, "isForParts") ? t("wholesale.forPartsOnly", "For parts only") : t("wholesale.repairsAmount", "Repairs {amount}", { amount: money(read(row, "repairTotalAmount"), read(row, "currencyCode") || "USD") }),
                shortDateTime(read(row, "saleDate"))
              ].filter(Boolean).join(" / "),
              value: money(read(row, "salePrice"), read(row, "currencyCode") || "USD")
            })
          ),
          sales.length === 0 && el(EmptyState, { text: t("wholesale.noSales", "No wholesale sales recorded yet.") })
        )
      )
    )
  );
}

function ReportBuilderModuleScreen({ api, module }) {
  const { styles, t } = useTheme();
  const moduleTitle = t(`screens.${module.key}`, module.title);
  const [tables, setTables] = useState([]);
  const [columns, setColumns] = useState([]);
  const [savedReports, setSavedReports] = useState([]);
  const [backgroundRuns, setBackgroundRuns] = useState([]);
  const [selectedTableKey, setSelectedTableKey] = useState("");
  const [selectedColumnKeys, setSelectedColumnKeys] = useState([]);
  const [maxRows, setMaxRows] = useState("100");
  const [result, setResult] = useState(null);
  const [status, setStatus] = useState(t("reports.ready", "Choose a table, select columns, then run a report."));
  const [isLoading, setIsLoading] = useState(false);

  const selectedTable = useMemo(
    () => tables.find((table) => tableKey(table) === selectedTableKey) || null,
    [selectedTableKey, tables]
  );
  const resultColumns = useMemo(() => asRows(read(result, "columns")), [result]);
  const resultRows = useMemo(() => asRows(read(result, "rows")), [result]);

  const loadOverview = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("reports.loading", "Loading report builder..."));
    try {
      const [nextTables, nextSavedReports, nextRuns] = await Promise.all([
        api.get("/api/reportbuilder/tables"),
        api.get("/api/reportbuilder/saved-reports"),
        api.get("/api/reportbuilder/background-runs")
      ]);
      const tableRows = asRows(nextTables);
      setTables(tableRows);
      setSavedReports(asRows(nextSavedReports));
      setBackgroundRuns(asRows(nextRuns));
      setSelectedTableKey((current) => current || tableKey(tableRows[0]) || "");
      setStatus(t("reports.loaded", "Report builder loaded."));
    } catch (error) {
      setTables([]);
      setColumns([]);
      setSavedReports([]);
      setBackgroundRuns([]);
      setStatus(error.message || t("reports.loadError", "Could not load report builder."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  const loadColumns = useCallback(async () => {
    if (!selectedTableKey) {
      setColumns([]);
      setSelectedColumnKeys([]);
      return;
    }

    try {
      const nextColumns = asRows(await api.get(`/api/reportbuilder/columns?tableKey=${encodeURIComponent(selectedTableKey)}`));
      setColumns(nextColumns);
      setSelectedColumnKeys((current) => {
        const available = new Set(nextColumns.map(columnKey));
        const retained = current.filter((key) => available.has(key));
        return retained.length ? retained : nextColumns.slice(0, 6).map(columnKey).filter(Boolean);
      });
    } catch (error) {
      setColumns([]);
      setSelectedColumnKeys([]);
      setStatus(error.message || t("reports.columnsError", "Could not load report columns."));
    }
  }, [api, selectedTableKey, t]);

  const runReport = useCallback(async (layout) => {
    const request = layout || {
      tableKey: selectedTableKey,
      columns: selectedColumnKeys,
      joins: [],
      groupByColumns: [],
      calculatedColumns: [],
      aggregates: [],
      filters: [],
      maxRows: Math.max(1, Math.min(5000, Number(maxRows) || 100)),
      includeSqlPreview: false,
      currencySettings: {}
    };

    if (!request.tableKey) {
      setStatus(t("reports.tableRequired", "Choose a table before running the report."));
      return;
    }

    setIsLoading(true);
    setStatus(t("reports.running", "Running report..."));
    try {
      const nextResult = await api.post("/api/reportbuilder/run", request);
      setResult(nextResult);
      setStatus(t("reports.ran", "{count} row(s) returned.", { count: read(nextResult, "rowCount") || asRows(read(nextResult, "rows")).length }));
    } catch (error) {
      setResult(null);
      setStatus(error.message || t("reports.runError", "Could not run report."));
    } finally {
      setIsLoading(false);
    }
  }, [api, maxRows, selectedColumnKeys, selectedTableKey, t]);

  const runSavedReport = useCallback(async (report) => {
    setIsLoading(true);
    setStatus(t("reports.loadingSaved", "Loading saved report..."));
    try {
      const detail = await api.get(`/api/reportbuilder/saved-reports/${read(report, "id")}`);
      const layout = read(detail, "layout") || {};
      setSelectedTableKey(read(layout, "tableKey") || selectedTableKey);
      setSelectedColumnKeys(asRows(read(layout, "columns")).map(String));
      await runReport({
        ...layout,
        tableKey: read(layout, "tableKey") || selectedTableKey,
        maxRows: read(layout, "maxRows") || 500,
        includeSqlPreview: false
      });
    } catch (error) {
      setStatus(error.message || t("reports.savedError", "Could not open saved report."));
    } finally {
      setIsLoading(false);
    }
  }, [api, runReport, selectedTableKey, t]);

  const toggleColumn = useCallback((key) => {
    setSelectedColumnKeys((current) =>
      current.includes(key)
        ? current.filter((item) => item !== key)
        : [...current, key]
    );
  }, []);

  useEffect(() => { loadOverview(); }, [loadOverview]);
  useEffect(() => { loadColumns(); }, [loadColumns]);

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: moduleTitle,
      actionTitle: t("common.refresh", "Refresh"),
      onAction: loadOverview,
      loading: isLoading
    }),
    el(ModuleSummary, { module, moduleTitle, t }),
    el(Panel, { title: t("reports.controls", "Report controls") },
      el(Field, {
        label: t("reports.maxRows", "Max rows"),
        value: maxRows,
        onChangeText: setMaxRows,
        keyboardType: "number-pad"
      }),
      el(View, { style: styles.inlineButtons },
        el(PrimaryButton, {
          title: t("common.load", "Load"),
          onPress: loadColumns,
          disabled: isLoading || !selectedTableKey,
          compact: true
        }),
        el(PrimaryButton, {
          title: t("reports.run", "Run"),
          onPress: () => runReport(),
          disabled: isLoading || !selectedTableKey,
          compact: true
        })
      )
    ),
    el(Panel, { title: t("reports.schema", "Schema") },
      el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.segmentRail
      },
        tables.map((table) => {
          const key = tableKey(table);
          return el(Pressable, {
            key,
            style: [styles.segmentButton, key === selectedTableKey && styles.segmentButtonActive],
            onPress: () => setSelectedTableKey(key)
          },
            el(Text, { style: [styles.segmentText, key === selectedTableKey && styles.segmentTextActive] }, tableTitle(table))
          );
        }),
        tables.length === 0 && el(EmptyState, { text: t("reports.noTables", "No reportable tables found.") })
      )
    ),
    el(Panel, { title: selectedTable ? tableTitle(selectedTable) : t("reports.columns", "Columns") },
      el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.segmentRail
      },
        columns.map((column) => {
          const key = columnKey(column);
          const active = selectedColumnKeys.includes(key);
          return el(Pressable, {
            key,
            style: [styles.segmentButton, active && styles.segmentButtonActive],
            onPress: () => toggleColumn(key)
          },
            el(Text, { style: [styles.segmentText, active && styles.segmentTextActive] }, columnTitle(column))
          );
        }),
        columns.length === 0 && el(EmptyState, { text: t("reports.noColumns", "No columns loaded yet.") })
      )
    ),
    el(StatusText, { value: status }),
    el(Panel, { title: t("reports.savedReports", "Saved reports") },
      el(View, { style: styles.screenListFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          savedReports.map((report, index) =>
            el(Pressable, { key: `saved-${read(report, "id") || index}`, onPress: () => runSavedReport(report) },
              el(ListRow, {
                title: read(report, "name") || `#${read(report, "id")}`,
                subtitle: read(report, "tableDisplayName", "tableKey") || t("reports.savedReport", "Saved report"),
                value: read(report, "isFavorite") ? "*" : ""
              })
            )
          ),
          savedReports.length === 0 && el(EmptyState, { text: t("reports.noSavedReports", "No saved reports yet.") })
        )
      )
    ),
    el(Panel, { title: t("reports.backgroundRuns", "Background runs") },
      el(View, { style: styles.screenListFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          backgroundRuns.map((run, index) =>
            el(ListRow, {
              key: `run-${read(run, "id") || index}`,
              title: read(run, "reportName") || `#${read(run, "id")}`,
              subtitle: read(run, "status") || "-",
              value: reportCell(read(run, "rowCount"))
            })
          ),
          backgroundRuns.length === 0 && el(EmptyState, { text: t("reports.noRuns", "No background runs yet.") })
        )
      )
    ),
    el(Panel, { title: t("reports.results", "Results") },
      el(View, { style: styles.screenListFrameLarge || styles.screenListFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          resultRows.map((row, index) =>
            el(ListRow, {
              key: `result-${index}`,
              title: reportCell(row[read(resultColumns[0], "columnName", "key")]),
              subtitle: reportRowText(row, resultColumns.slice(1), t("reports.resultRow", "Report row")),
              value: String(index + 1)
            })
          ),
          resultRows.length === 0 && el(EmptyState, { text: t("reports.noResultRows", "Run a report to see results.") })
        )
      )
    )
  );
}

const assistantStarterActions = [
  {
    id: "create_report",
    label: "Create report",
    description: "Pick a quick operational report to run.",
    kind: "Report",
    tone: "Neutral",
    payload: {}
  },
  {
    id: "send_payment_reminder",
    label: "Send reminder",
    description: "Prepare payment reminder text from unpaid invoices.",
    kind: "Draft",
    target: "whatsapp",
    tone: "Good",
    payload: {}
  },
  {
    id: "open_customer",
    label: "Open customer",
    description: "Open the top customer summary from live balances.",
    kind: "Open",
    target: "contacts",
    tone: "Neutral",
    payload: {}
  },
  {
    id: "draft_purchase_order",
    label: "Draft purchase order",
    description: "Build a low-stock purchase shortlist.",
    kind: "Draft",
    target: "purchase-parts",
    tone: "Good",
    payload: {}
  },
  {
    id: "find_unpaid_customers",
    label: "Find unpaid customers",
    description: "List customer balances from unpaid sales.",
    kind: "Query",
    tone: "Warning",
    payload: {}
  },
  {
    id: "campaign_dead_stock_clearance",
    label: "Dead stock clearance",
    description: "Suggest a WhatsApp campaign for dormant on-hand parts.",
    kind: "Campaign",
    target: "whatsapp",
    tone: "Warning",
    payload: {}
  },
  {
    id: "campaign_back_in_stock",
    label: "Back in stock campaign",
    description: "Feature recently received available parts.",
    kind: "Campaign",
    target: "whatsapp",
    tone: "Good",
    payload: {}
  },
  {
    id: "campaign_unpaid_reminders",
    label: "Unpaid reminders",
    description: "Prepare a receivables reminder campaign.",
    kind: "Campaign",
    target: "whatsapp",
    tone: "Warning",
    payload: {}
  },
  {
    id: "campaign_seasonal_service_parts",
    label: "Seasonal service parts",
    description: "Promote maintenance parts customers may need now.",
    kind: "Campaign",
    target: "whatsapp",
    tone: "Good",
    payload: {}
  }
];

function actionValue(action, key, fallback = "") {
  const value = read(action, key);
  return value === "" ? fallback : value;
}

function assistantActionToneStyle(styles, tone) {
  const normalized = String(tone || "neutral").toLowerCase();
  if (normalized === "warning") return styles.assistantActionButtonWarning;
  if (normalized === "good") return styles.assistantActionButtonGood;
  return null;
}

function BusinessAssistantModuleScreen({ api, module, onNavigate }) {
  const { styles, t } = useTheme();
  const moduleTitle = t(`screens.${module.key}`, module.title);
  const [question, setQuestion] = useState("");
  const [response, setResponse] = useState(null);
  const [status, setStatus] = useState(t("assistant.ready", "Ask a business question to inspect live operating data."));
  const [isLoading, setIsLoading] = useState(false);

  const runAction = useCallback(async (action) => {
    const actionId = actionValue(action, "id");
    const label = actionValue(action, "label", "action");
    const kind = actionValue(action, "kind");
    const target = actionValue(action, "target");

    if (String(kind).toLowerCase() === "navigate" && target && typeof onNavigate === "function") {
      onNavigate(target);
      return;
    }

    if (!actionId) {
      setStatus(t("assistant.actionRequired", "Choose an action first."));
      return;
    }

    setIsLoading(true);
    setStatus(t("assistant.runningAction", "Running {label}...", { label }));
    try {
      const result = await api.post("/api/business-assistant/actions/run", {
        actionId,
        payload: read(action, "payload") || {}
      });
      setResponse(result);
      setStatus(t("assistant.actionComplete", "Assistant action complete."));
    } catch (error) {
      setStatus(error.message || t("assistant.actionError", "Could not run assistant action."));
    } finally {
      setIsLoading(false);
    }
  }, [api, onNavigate, t]);

  const ask = useCallback(async () => {
    const text = question.trim();
    if (!text) {
      setStatus(t("assistant.questionRequired", "Enter a question first."));
      return;
    }

    setIsLoading(true);
    setStatus(t("assistant.asking", "Asking the business assistant..."));
    try {
      const result = await api.post(module.endpoint, { question: text });
      setResponse(result);
      setStatus(t("assistant.answered", "Business assistant answered."));
    } catch (error) {
      setResponse(null);
      setStatus(error.message || t("assistant.error", "Could not ask the business assistant."));
    } finally {
      setIsLoading(false);
    }
  }, [api, module.endpoint, question, t]);

  return el(ScreenScroll, null,
    el(ScreenHeader, { title: moduleTitle }),
    el(ModuleSummary, { module, moduleTitle, t }),
    el(Panel, { title: t("assistant.question", "Question") },
      el(Field, {
        label: t("assistant.prompt", "Ask"),
        value: question,
        onChangeText: setQuestion,
        placeholder: t("assistant.placeholder", "What should I check?"),
        multiline: true,
        numberOfLines: 4
      }),
      el(PrimaryButton, {
        title: isLoading ? t("common.loading", "Loading") : t("assistant.ask", "Ask"),
        onPress: ask,
        disabled: isLoading
      })
    ),
    el(Panel, { title: t("assistant.actions", "Assistant actions") },
      el(View, { style: styles.assistantActionGrid },
        (((response && response.actions) || []).length ? response.actions : assistantStarterActions).map((action) =>
          el(Pressable, {
            key: actionValue(action, "id") || actionValue(action, "label"),
            style: [styles.assistantActionButton, assistantActionToneStyle(styles, actionValue(action, "tone"))],
            onPress: () => runAction(action),
            disabled: isLoading
          },
            el(Text, { style: styles.assistantActionTitle }, actionValue(action, "label", "Run action")),
            el(Text, { style: styles.assistantActionDescription }, actionValue(action, "description")),
            el(Text, { style: styles.assistantActionKind }, actionValue(action, "kind", "Action"))
          )
        )
      )
    ),
    el(StatusText, { value: status }),
    response && el(React.Fragment, null,
      el(Panel, { title: t("assistant.answer", "Answer") },
        el(ListRow, {
          title: response.answer || t("assistant.noAnswer", "No answer returned."),
          subtitle: response.isSupported === false ? t("assistant.unsupported", "Unsupported question") : response.intent
        }),
        (response.suggestions || []).map((suggestion) =>
          el(Pressable, { key: suggestion, onPress: () => setQuestion(suggestion) },
            el(ListRow, {
              title: suggestion,
              subtitle: t("assistant.useSuggestion", "Use as next prompt")
            })
          )
        )
      ),
      el(Panel, { title: t("assistant.insights", "Insights") },
        el(View, { style: styles.screenListFrame },
          el(ScrollView, {
            nestedScrollEnabled: true,
            showsVerticalScrollIndicator: true,
            contentContainerStyle: styles.screenListContent
          },
            (response.insights || []).map((insight, index) =>
              el(ListRow, {
                key: `${insight.label || "insight"}-${index}`,
                title: insight.label || insight.severity || "Insight",
                subtitle: insight.detail,
                value: insight.value
              })
            ),
            (!response.insights || response.insights.length === 0) && el(EmptyState, { text: t("assistant.noInsights", "No insights returned.") })
          )
        )
      )
    )
  );
}

function ScanLookupModuleScreen({ api, module }) {
  const { styles, t } = useTheme();
  const moduleTitle = t(`screens.${module.key}`, module.title);
  const scanEndpoint = useMemo(() => splitEndpoints(module.endpoint)[0] || "/api/scans/resolve", [module.endpoint]);
  const [code, setCode] = useState("");
  const [rows, setRows] = useState([]);
  const [visualHint, setVisualHint] = useState("");
  const [visualAsset, setVisualAsset] = useState(null);
  const [visualResponse, setVisualResponse] = useState(null);
  const [visualRows, setVisualRows] = useState([]);
  const [status, setStatus] = useState(t("scans.ready", "Enter a barcode, invoice number, purchase number, or stock code."));
  const [isLoading, setIsLoading] = useState(false);
  const [isVisualSearching, setIsVisualSearching] = useState(false);

  const resolve = useCallback(async () => {
    const scanCode = code.trim();
    if (!scanCode) {
      setStatus(t("scans.codeRequired", "Enter a scan code first."));
      return;
    }

    setIsLoading(true);
    setStatus(t("scans.resolving", "Resolving scan code..."));
    try {
      const result = asRows(await api.get(`${scanEndpoint}?code=${encodeURIComponent(scanCode)}`));
      setRows(result);
      setStatus(result.length
        ? t("scans.resolved", "{count} result(s) found.", { count: result.length })
        : t("scans.noResults", "No matching record was found."));
    } catch (error) {
      setRows([]);
      setStatus(error.message || t("scans.error", "Could not resolve the scan code."));
    } finally {
      setIsLoading(false);
    }
  }, [api, code, scanEndpoint, t]);

  const choosePicture = useCallback(async (source) => {
    try {
      const permission = source === "camera"
        ? await ImagePicker.requestCameraPermissionsAsync()
        : await ImagePicker.requestMediaLibraryPermissionsAsync();

      if (!permission.granted) {
        setStatus(t("scans.photoPermission", "Camera or photo permission is required for picture search."));
        return;
      }

      const result = source === "camera"
        ? await ImagePicker.launchCameraAsync({ mediaTypes: ["images"], quality: 0.82 })
        : await ImagePicker.launchImageLibraryAsync({ mediaTypes: ["images"], quality: 0.82, selectionLimit: 1 });

      if (result.canceled || !result.assets || result.assets.length === 0) return;

      setVisualAsset(result.assets[0]);
      setVisualResponse(null);
      setVisualRows([]);
      setStatus(t("scans.pictureReady", "Picture ready. Add a hint if needed, then search."));
    } catch (error) {
      setStatus(error.message || t("scans.pictureError", "Could not open the camera or photo picker."));
    }
  }, [t]);

  const useVisualMatch = useCallback((match) => {
    const nextCode = read(match, "internalCode") || read(match, "barcode") || read(match, "partId");
    const selectedRow = {
      targetType: "part",
      targetId: read(match, "partId"),
      code: nextCode,
      displayText: visualMatchTitle(match),
      secondaryText: read(match, "matchReason")
    };

    setCode(nextCode ? `part:${nextCode}` : "");
    setRows([selectedRow]);
    setStatus(t("scans.pictureSelected", "{part} selected from picture search.", { part: visualMatchTitle(match) }));
  }, [t]);

  const searchPicture = useCallback(async () => {
    if (!visualAsset) {
      setStatus(t("scans.pictureRequired", "Take or choose a part picture first."));
      return;
    }

    setIsVisualSearching(true);
    setStatus(t("scans.pictureSearching", "Searching catalog from picture..."));
    try {
      const formData = new FormData();
      const name = visualAsset.fileName || `part-picture-${Date.now()}.jpg`;
      const type = visualAsset.mimeType || "image/jpeg";
      formData.append("image", { uri: visualAsset.uri, name, type });
      formData.append("hint", visualHint.trim());
      formData.append("limit", "10");

      const response = await api.postForm("/api/scans/visual-search", formData);
      const matches = asRows(read(response, "matches"));
      setVisualResponse(response);
      setVisualRows(matches);

      if (matches[0]) {
        useVisualMatch(matches[0]);
      }

      setStatus(matches.length
        ? t("scans.pictureResolved", "{count} picture match(es) found.", { count: matches.length })
        : read(response, "message") || t("scans.pictureNoResults", "No part matched that picture."));
    } catch (error) {
      setVisualResponse(null);
      setVisualRows([]);
      setStatus(error.message || t("scans.pictureSearchError", "Could not search by picture."));
    } finally {
      setIsVisualSearching(false);
    }
  }, [api, t, useVisualMatch, visualAsset, visualHint]);

  return el(ScreenScroll, null,
    el(ScreenHeader, { title: moduleTitle }),
    el(ModuleSummary, { module, moduleTitle, t }),
    el(Panel, { title: t("scans.lookup", "Scan lookup") },
      el(Field, {
        label: t("scans.code", "Scan code"),
        value: code,
        onChangeText: setCode,
        onSubmitEditing: resolve,
        placeholder: t("scans.placeholder", "Scan or type a code"),
        returnKeyType: "search",
        autoCapitalize: "characters"
      }),
      el(PrimaryButton, {
        title: isLoading ? t("common.loading", "Loading") : t("scans.resolve", "Resolve"),
        onPress: resolve,
        disabled: isLoading
      })
    ),
    el(Panel, { title: t("scans.pictureSearch", "AR picture search") },
      el(View, { style: styles.visualActionRow },
        el(PrimaryButton, {
          title: t("scans.takePhoto", "Take photo"),
          onPress: () => choosePicture("camera"),
          disabled: isVisualSearching,
          compact: true
        }),
        el(PrimaryButton, {
          title: t("scans.choosePhoto", "Choose photo"),
          onPress: () => choosePicture("library"),
          disabled: isVisualSearching,
          compact: true
        }),
        el(PrimaryButton, {
          title: isVisualSearching ? t("common.loading", "Loading") : t("scans.searchPicture", "Search picture"),
          onPress: searchPicture,
          disabled: isVisualSearching || !visualAsset,
          compact: true
        })
      ),
      el(Field, {
        label: t("scans.pictureHint", "Hint"),
        value: visualHint,
        onChangeText: setVisualHint,
        placeholder: t("scans.pictureHintPlaceholder", "Optional: BMW headlight, brake sensor, oil filter")
      }),
      el(View, { style: styles.visualPreviewFrame },
        visualAsset
          ? el(Image, { source: { uri: visualAsset.uri }, style: styles.visualPreviewImage })
          : el(View, { style: styles.visualPreviewEmpty },
            el(Text, { style: styles.visualPreviewEmptyTitle }, t("scans.noPicture", "No picture selected")),
            el(Text, { style: styles.visualPreviewEmptyText }, t("scans.noPictureDetail", "Use the camera or choose a saved part photo."))
          ),
        visualAsset && visualRows.slice(0, 5).map((match, index) =>
          el(Pressable, {
            key: `visual-pin-${read(match, "partId") || index}`,
            style: [
              styles.visualPin,
              {
                left: `${Math.round(numericValue(read(match, "anchorX"), 0.5) * 100)}%`,
                top: `${Math.round(numericValue(read(match, "anchorY"), 0.5) * 100)}%`
              }
            ],
            onPress: () => useVisualMatch(match)
          },
            el(Text, { style: styles.visualPinText }, `#${index + 1}`)
          )
        )
      ),
      el(Text, { style: styles.statusText }, read(visualResponse, "searchText") || read(visualResponse, "message") || "")
    ),
    el(StatusText, { value: status }),
    el(Panel, { title: t("scans.pictureResults", "Picture matches") },
      el(View, { style: styles.screenListFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          visualRows.map((row, index) =>
            el(Pressable, { key: `visual-${read(row, "partId") || index}`, onPress: () => useVisualMatch(row) },
              el(ListRow, {
                title: `#${index + 1} ${visualMatchTitle(row)}`,
                subtitle: [read(row, "matchReason"), read(row, "oemNumber") ? `OEM ${read(row, "oemNumber")}` : ""].filter(Boolean).join(" / "),
                value: `${read(row, "availableQuantity") || 0} avail`
              })
            )
          ),
          visualRows.length === 0 && el(EmptyState, { text: t("scans.pictureEmpty", "Picture matches will appear here.") })
        )
      )
    ),
    el(Panel, { title: t("scans.results", "Results") },
      el(View, { style: styles.screenListFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          rows.map((row, index) => el(ListRow, {
            key: `${row.targetType || "scan"}-${row.targetId || row.code || index}`,
            title: row.displayText || `#${row.targetId || ""}`.trim(),
            subtitle: [row.targetType, row.code, row.secondaryText || row.apiRoute].filter(Boolean).join(" / ")
          })),
          rows.length === 0 && el(EmptyState, { text: t("scans.empty", "No scan results yet.") })
        )
      )
    )
  );
}

function ModuleScreen({ api, module, onNavigate }) {
  const { styles, t } = useTheme();
  if (!module) {
    return el(ScreenScroll, null,
      el(ScreenHeader, { title: t("module.unavailable", "Workspace unavailable") }),
      el(StatusText, { value: t("module.notMapped", "This workspace is not mapped in the mobile shell.") })
    );
  }

  if (module.key === "business-assistant") {
    return el(BusinessAssistantModuleScreen, { api, module, onNavigate });
  }

  if (module.key === "report-builder") {
    return el(ReportBuilderModuleScreen, { api, module });
  }

  if (module.key === "ar") {
    return el(ScanLookupModuleScreen, { api, module });
  }

  if (module.key === "part-requests") {
    return el(PartRequestsModuleScreen, { api, module });
  }

  if (module.key === "used-car-wholesale") {
    return el(UsedCarWholesaleModuleScreen, { api, module });
  }

  const canPreview = module.endpoint && !module.endpoint.endsWith("/ask") && !module.endpoint.endsWith("/resolve");
  const moduleTitle = t(`screens.${module.key}`, module.title);
  const [rows, setRows] = useState([]);
  const [status, setStatus] = useState(canPreview ? "" : t("module.mappedCommand", "Mapped to a command endpoint."));
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    if (!canPreview) return;
    setIsLoading(true);
    setStatus(t("module.loading", "Loading {title}...", { title: moduleTitle.toLowerCase() }));
    try {
      setRows(asRows(await loadModuleRows(api, module.endpoint)));
      setStatus(t("module.loaded", "{title} loaded.", { title: moduleTitle }));
    } catch (error) {
      setRows([]);
      setStatus(error.message || t("module.loadError", "Could not load {title}.", { title: moduleTitle.toLowerCase() }));
    } finally {
      setIsLoading(false);
    }
  }, [api, canPreview, module.endpoint, moduleTitle, t]);

  useEffect(() => {
    if (!canPreview) {
      setStatus(t("module.mappedCommand", "Mapped to a command endpoint."));
    }
  }, [canPreview, t]);

  useEffect(() => { load(); }, [load]);

  return el(ScreenScroll, null,
    el(ScreenHeader, { title: moduleTitle, actionTitle: canPreview ? t("common.refresh", "Refresh") : null, onAction: load, loading: isLoading }),
    el(ModuleSummary, { module, moduleTitle, t }),
    el(StatusText, { value: status }),
    canPreview && el(Panel, { title: t("module.preview", "Preview") },
      el(View, { style: styles.screenListFrame },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          rows.map((row, index) => el(ListRow, {
            key: `${module.key}-${row.id || row.invoiceId || row.purchaseId || index}`,
            title: rowTitle(row),
            subtitle: rowSubtitle(row),
            value: rowAmount(row)
          })),
          rows.length === 0 && el(EmptyState, { text: t("module.noRows", "No {title} rows returned.", { title: moduleTitle.toLowerCase() }) })
        )
      )
    )
  );
}

function createModuleScreen(module) {
  return function RegisteredModuleScreen({ api, onNavigate }) {
    return el(ModuleScreen, { api, module, onNavigate });
  };
}

module.exports = {
  ModuleScreen,
  createModuleScreen
};
