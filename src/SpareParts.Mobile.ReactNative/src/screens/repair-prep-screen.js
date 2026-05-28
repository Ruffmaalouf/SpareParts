const React = require("react");
const { Pressable, ScrollView, Text, View } = require("react-native");
const AsyncStorage = require("@react-native-async-storage/async-storage").default;
const { displayCurrencyContext, displayMoneyFromBase, initials, money } = require("../core/formatters");
const { Field, ListRow, Panel, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;
const storageKey = "spareparts.mobile.repairPrepBoard.v1";

const columns = [
  { key: "bought", label: "Bought" },
  { key: "inspected", label: "Inspected" },
  { key: "parts-needed", label: "Parts Needed" },
  { key: "repairing", label: "Repairing" },
  { key: "photo-ready", label: "Photo-ready" },
  { key: "listed", label: "Listed" },
  { key: "sold", label: "Sold" }
];

const defaultTasks = [
  { title: "Inspection checklist", cost: 0 },
  { title: "Prep photos", cost: 0 }
];

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

function asRows(value) {
  return Array.isArray(value) ? value : value ? [value] : [];
}

function toNumber(value) {
  const parsed = Number(String(value || "").replace(/,/g, ".").trim());
  return Number.isFinite(parsed) ? parsed : 0;
}

function itemId(row) {
  return read(row, "id", "locationId");
}

function carTitle(car) {
  const year = read(car, "modelYear");
  const name = read(car, "car") || "Used car";
  return `${year || ""} ${name}`.trim();
}

function inferStatus(car) {
  if (toNumber(read(car, "salePriceBase")) > 0) return "sold";
  if (toNumber(read(car, "repairs", "repairsCostBase")) > 0) return "repairing";
  if (read(car, "isReceived")) return "inspected";
  return "bought";
}

function createDefaultTasks(car) {
  const repairs = toNumber(read(car, "repairs", "repairsCostBase"));
  const customs = toNumber(read(car, "customs", "customsCostBase"));
  return [
    ...defaultTasks,
    repairs > 0 ? { title: "Repair labor", cost: repairs } : null,
    customs > 0 ? { title: "Customs cleared", cost: customs } : null
  ].filter(Boolean).map((task, index) => ({
    id: `${Date.now()}-${index}-${Math.random().toString(16).slice(2)}`,
    title: task.title,
    cost: task.cost,
    done: false
  }));
}

function normalizeCarRecord(car, boardState) {
  const id = String(itemId(car));
  const existing = boardState[id] || {};
  return {
    status: existing.status || inferStatus(car),
    tasks: Array.isArray(existing.tasks) ? existing.tasks : createDefaultTasks(car)
  };
}

function taskCost(tasks) {
  return tasks.reduce((total, task) => total + toNumber(task.cost), 0);
}

function taskProgress(tasks) {
  if (!tasks.length) return 0;
  return Math.round((tasks.filter((task) => task.done).length / tasks.length) * 100);
}

function mergeBoardState(cars, boardState) {
  const next = { ...boardState };
  cars.forEach((car) => {
    const id = String(itemId(car));
    if (!id) return;
    next[id] = normalizeCarRecord(car, next);
  });
  return next;
}

function Metric({ label, value }) {
  const { styles } = useTheme();
  return el(View, { style: styles.prepMetric },
    el(Text, { style: styles.prepMetricLabel }, label),
    el(Text, { style: styles.prepMetricValue }, String(value))
  );
}

function TaskRow({ task, displayContext, onToggle, onDelete }) {
  const { styles } = useTheme();
  return el(View, { style: [styles.prepTaskRow, task.done && styles.prepTaskRowDone] },
    el(Pressable, { style: styles.prepTaskToggle, onPress: () => onToggle(!task.done) },
      el(Text, { style: styles.prepTaskToggleText }, task.done ? "OK" : "")
    ),
    el(View, { style: styles.prepTaskCopy },
      el(Text, { style: styles.prepTaskTitle, numberOfLines: 1 }, task.title),
      el(Text, { style: styles.prepTaskMeta }, displayMoneyFromBase(task.cost, displayContext))
    ),
    el(Pressable, { style: styles.prepTaskDelete, onPress: onDelete },
      el(Text, { style: styles.prepTaskDeleteText }, "x")
    )
  );
}

function RepairPrepScreen({ api }) {
  const { styles, t } = useTheme();
  const [cars, setCars] = useState([]);
  const [parts, setParts] = useState([]);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [selectedId, setSelectedId] = useState("");
  const [boardState, setBoardState] = useState({});
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [search, setSearch] = useState("");
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [newTaskCost, setNewTaskCost] = useState("");
  const [hasLoadedLocalState, setHasLoadedLocalState] = useState(false);

  useEffect(() => {
    let active = true;
    AsyncStorage.getItem(storageKey)
      .then((value) => {
        if (!active) return;
        setBoardState(value ? JSON.parse(value) || {} : {});
      })
      .catch(() => {})
      .finally(() => {
        if (active) setHasLoadedLocalState(true);
      });
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!hasLoadedLocalState || cars.length === 0) return;
    AsyncStorage.setItem(storageKey, JSON.stringify(mergeBoardState(cars, boardState))).catch(() => {});
  }, [boardState, cars, hasLoadedLocalState]);

  const mergedBoardState = useMemo(() => mergeBoardState(cars, boardState), [boardState, cars]);
  const selectedCar = useMemo(
    () => cars.find((car) => String(itemId(car)) === String(selectedId)) || null,
    [cars, selectedId]
  );
  const selectedDisplayContext = useMemo(() => displayCurrencyContext({
    constants: appConstants,
    rates: currencyRates,
    baseCurrencyCode: read(selectedCar, "baseCurrencyCode"),
    counterCurrencyCode: read(selectedCar, "counterCurrencyCode")
  }), [appConstants, currencyRates, selectedCar]);

  const load = useCallback(async (preferredId) => {
    setIsLoading(true);
    setStatus(t("repairPrep.loading", "Loading repair board..."));
    try {
      const [nextCars, nextParts, nextCurrencies, nextAppConstants] = await Promise.all([
        api.get("/api/usedcars"),
        api.get("/api/parts?page=1&pageSize=500"),
        api.get("/api/currencies"),
        api.get("/api/appconstants")
      ]);
      const carRows = asRows(nextCars);
      setCars(carRows);
      setParts(asRows(nextParts));
      setCurrencyRates(asRows(nextCurrencies));
      setAppConstants(asRows(nextAppConstants));
      setBoardState((current) => mergeBoardState(carRows, current));
      setSelectedId((current) => {
        if (preferredId) return String(preferredId);
        if (current && carRows.some((car) => String(itemId(car)) === String(current))) return current;
        return String(itemId(carRows[0]) || "");
      });
      setStatus(t("repairPrep.loaded", "{count} cars loaded for repair prep.", { count: carRows.length }));
    } catch (error) {
      setCars([]);
      setParts([]);
      setStatus(error.message || t("repairPrep.loadError", "Could not load the repair board."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => {
    if (hasLoadedLocalState) load();
  }, [hasLoadedLocalState, load]);

  const setCarRecord = useCallback((carId, updater) => {
    setBoardState((current) => {
      const car = cars.find((item) => String(itemId(item)) === String(carId));
      if (!car) return current;
      const record = normalizeCarRecord(car, current);
      return {
        ...current,
        [String(carId)]: updater(record)
      };
    });
  }, [cars]);

  const moveCarToStatus = useCallback((carId, nextStatus) => {
    setCarRecord(carId, (record) => ({ ...record, status: nextStatus }));
    setSelectedId(String(carId));
  }, [setCarRecord]);

  const updateTask = useCallback((taskId, patch) => {
    if (!selectedCar) return;
    setCarRecord(itemId(selectedCar), (record) => ({
      ...record,
      tasks: record.tasks.map((task) => task.id === taskId ? { ...task, ...patch } : task)
    }));
  }, [selectedCar, setCarRecord]);

  const deleteTask = useCallback((taskId) => {
    if (!selectedCar) return;
    setCarRecord(itemId(selectedCar), (record) => ({
      ...record,
      tasks: record.tasks.filter((task) => task.id !== taskId)
    }));
  }, [selectedCar, setCarRecord]);

  const addTask = useCallback(() => {
    if (!selectedCar || !newTaskTitle.trim()) return;
    const task = {
      id: `${Date.now()}-${Math.random().toString(16).slice(2)}`,
      title: newTaskTitle.trim(),
      cost: toNumber(newTaskCost) * (Number(selectedDisplayContext.rateToBase) || 1),
      done: false
    };
    setCarRecord(itemId(selectedCar), (record) => ({ ...record, tasks: [...record.tasks, task] }));
    setNewTaskTitle("");
    setNewTaskCost("");
  }, [newTaskCost, newTaskTitle, selectedCar, selectedDisplayContext.rateToBase, setCarRecord]);

  const filteredCars = useMemo(() => {
    const needle = search.trim().toLowerCase();
    if (!needle) return cars;
    return cars.filter((car) => [
      carTitle(car),
      read(car, "barcode"),
      read(car, "location"),
      read(car, "supplierName")
    ].join(" ").toLowerCase().includes(needle));
  }, [cars, search]);

  const selectedRecord = selectedCar ? mergedBoardState[String(itemId(selectedCar))] : null;
  const selectedTasks = selectedRecord?.tasks || [];
  const selectedCurrency = selectedDisplayContext.code || read(selectedCar, "priceCurrency") || "USD";
  const linkedParts = selectedCar
    ? parts.filter((part) => String(read(part, "usedCarId") || "") === String(itemId(selectedCar)))
    : [];
  const progress = taskProgress(selectedTasks);
  const totalPrepCost = taskCost(selectedTasks);
  const activeColumn = columns.find((column) => column.key === selectedRecord?.status);
  const boardSummary = useMemo(() => {
    const totalTasks = Object.values(mergedBoardState).reduce((total, record) => total + (record.tasks?.length || 0), 0);
    const doneTasks = Object.values(mergedBoardState).reduce(
      (total, record) => total + (record.tasks || []).filter((task) => task.done).length,
      0
    );
    return {
      cars: cars.length,
      active: cars.filter((car) => !["listed", "sold"].includes(mergedBoardState[String(itemId(car))]?.status)).length,
      doneTasks,
      totalTasks
    };
  }, [cars, mergedBoardState]);

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: t("repairPrep.title", "Repair / Prep Board"),
      actionTitle: t("common.refresh", "Refresh"),
      onAction: () => load(selectedId),
      loading: isLoading
    }),
    el(StatusText, { value: status }),
    el(View, { style: styles.prepMetricGrid },
      el(Metric, { label: t("repairPrep.cars", "Cars"), value: boardSummary.cars }),
      el(Metric, { label: t("repairPrep.active", "Active"), value: boardSummary.active }),
      el(Metric, { label: t("repairPrep.tasks", "Tasks"), value: `${boardSummary.doneTasks}/${boardSummary.totalTasks}` })
    ),
    el(Field, {
      label: t("common.search", "Search"),
      value: search,
      onChangeText: setSearch,
      placeholder: t("repairPrep.searchPlaceholder", "Car, barcode, supplier, location")
    }),
    el(Panel, { title: t("repairPrep.board", "Board") },
      el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.prepBoardRail
      },
        columns.map((column) => {
          const columnCars = filteredCars.filter((car) => mergedBoardState[String(itemId(car))]?.status === column.key);
          return el(View, {
            key: column.key,
            style: [styles.prepColumn, activeColumn?.key === column.key && styles.prepColumnActive]
          },
            el(View, { style: styles.prepColumnHeader },
              el(Text, { style: styles.prepColumnTitle }, column.label),
              el(Text, { style: styles.prepColumnCount }, String(columnCars.length))
            ),
            el(ScrollView, {
              style: styles.prepColumnScroller,
              nestedScrollEnabled: true,
              showsVerticalScrollIndicator: true,
              contentContainerStyle: styles.prepColumnList
            },
              columnCars.map((car) => {
                const id = String(itemId(car));
                const record = mergedBoardState[id];
                const tasks = record?.tasks || [];
                const carProgress = taskProgress(tasks);
                return el(Pressable, {
                  key: id,
                  style: [styles.prepCard, id === String(selectedId) && styles.prepCardActive],
                  onPress: () => setSelectedId(id)
                },
                  el(View, { style: styles.prepCardAvatar },
                    el(Text, { style: styles.prepCardAvatarText }, initials(carTitle(car)))
                  ),
                  el(View, { style: styles.prepCardCopy },
                    el(Text, { style: styles.prepCardTitle, numberOfLines: 1 }, carTitle(car)),
                    el(Text, { style: styles.prepCardMeta, numberOfLines: 1 }, `${read(car, "barcode") || t("usedCars.noBarcode", "No barcode")} / ${read(car, "location") || "Location"}`)
                  ),
                  el(View, { style: styles.prepProgress },
                    el(View, { style: [styles.prepProgressFill, { width: `${carProgress}%` }] })
                  ),
                  el(Text, { style: styles.prepCardFooter }, `${tasks.filter((task) => task.done).length}/${tasks.length} tasks`)
                );
              }),
              columnCars.length === 0 && el(Text, { style: styles.emptyState }, t("repairPrep.noCarsInLane", "No cars in this lane."))
            )
          );
        })
      )
    ),
    selectedCar && el(Panel, { title: t("repairPrep.selected", "Selected car") },
      el(View, { style: styles.prepSelectedHeader },
        el(View, { style: styles.prepSelectedAvatar },
          el(Text, { style: styles.prepSelectedAvatarText }, initials(carTitle(selectedCar)))
        ),
        el(View, { style: styles.prepSelectedCopy },
          el(Text, { style: styles.prepSelectedTitle }, carTitle(selectedCar)),
          el(Text, { style: styles.prepSelectedMeta }, `${activeColumn?.label || "Board"} / ${read(selectedCar, "supplierName") || "Supplier"}`)
        )
      ),
      el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.segmentRail
      },
        columns.map((column) =>
          el(Pressable, {
            key: column.key,
            style: [styles.segmentButton, selectedRecord?.status === column.key && styles.segmentButtonActive],
            onPress: () => moveCarToStatus(itemId(selectedCar), column.key)
          },
            el(Text, { style: [styles.segmentText, selectedRecord?.status === column.key && styles.segmentTextActive] }, column.label)
          )
        )
      ),
      el(View, { style: styles.prepProgressLarge },
        el(View, { style: [styles.prepProgressFill, { width: `${progress}%` }] })
      ),
      el(Text, { style: styles.statusText }, `${progress}% ${t("repairPrep.complete", "complete")} / ${displayMoneyFromBase(totalPrepCost, selectedDisplayContext)} ${t("repairPrep.prepCost", "prep cost")}`),
      selectedTasks.map((task) =>
        el(TaskRow, {
          key: task.id,
          task,
          displayContext: selectedDisplayContext,
          onToggle: (done) => updateTask(task.id, { done }),
          onDelete: () => deleteTask(task.id)
        })
      ),
      selectedTasks.length === 0 && el(Text, { style: styles.emptyState }, t("repairPrep.noTasks", "No prep tasks yet.")),
      el(Field, {
        label: t("repairPrep.task", "Task"),
        value: newTaskTitle,
        onChangeText: setNewTaskTitle,
        placeholder: t("repairPrep.taskPlaceholder", "Prep task")
      }),
      el(Field, {
        label: t("repairPrep.cost", "Cost"),
        value: newTaskCost,
        onChangeText: setNewTaskCost,
        keyboardType: "decimal-pad",
        placeholder: "0"
      }),
      el(SecondaryButton, { title: t("repairPrep.addTask", "Add Task"), onPress: addTask })
    ),
    selectedCar && el(Panel, { title: t("repairPrep.costs", "Costs") },
      el(ListRow, { title: t("repairPrep.purchase", "Purchase"), value: displayMoneyFromBase(read(selectedCar, "purchaseCostBase", "priceBase", "price"), selectedDisplayContext) }),
      el(ListRow, { title: t("repairPrep.shipping", "Shipping"), value: displayMoneyFromBase(read(selectedCar, "shippingCostBase", "shipping"), selectedDisplayContext) }),
      el(ListRow, { title: t("repairPrep.customs", "Customs"), value: displayMoneyFromBase(read(selectedCar, "customsCostBase", "customs"), selectedDisplayContext) }),
      el(ListRow, { title: t("repairPrep.repairs", "Repairs"), value: displayMoneyFromBase(read(selectedCar, "repairsCostBase", "repairs"), selectedDisplayContext) }),
      el(ListRow, { title: t("repairPrep.prepTasks", "Prep tasks"), value: displayMoneyFromBase(totalPrepCost, selectedDisplayContext) }),
      el(ListRow, { title: t("repairPrep.fullCost", "Full car cost"), value: displayMoneyFromBase(read(selectedCar, "fullCostBase", "grandTotalBase"), selectedDisplayContext) })
    ),
    selectedCar && el(Panel, { title: t("repairPrep.linkedParts", "Linked parts") },
      linkedParts.slice(0, 24).map((part) =>
        el(ListRow, {
          key: String(itemId(part)),
          title: read(part, "name") || `Part #${itemId(part)}`,
          subtitle: `${read(part, "internalCode") || "No code"} / OEM ${read(part, "oemNumber") || "not set"}`,
          value: money(read(part, "salePrice"), read(part, "currency") || selectedCurrency)
        })
      ),
      linkedParts.length === 0 && el(Text, { style: styles.emptyState }, t("repairPrep.noLinkedParts", "No linked parts yet."))
    )
  );
}

module.exports = { RepairPrepScreen };
