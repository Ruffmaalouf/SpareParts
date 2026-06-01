import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { displayCurrencyContext, displayMoneyFromBase, initials, money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const storageKey = "spareparts.repairPrepBoard.v1";

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
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") return value;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row?.[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }
  return "";
}

function toArray(value) {
  return Array.isArray(value) ? value : value ? [value] : [];
}

function toNumber(value) {
  const parsed = Number(String(value || "").replace(/,/g, ".").trim());
  return Number.isFinite(parsed) ? parsed : 0;
}

function itemId(row) {
  return read(row, "id", "locationId");
}

function imageSrc(image) {
  const imageData = read(image, "imageData");
  if (!imageData) return "";
  return `data:${read(image, "mimeType") || "image/jpeg"};base64,${imageData}`;
}

function carTitle(car) {
  const year = read(car, "modelYear");
  const name = read(car, "car") || "Used car";
  return `${year || ""} ${name}`.trim();
}

function safeLoadBoardState() {
  try {
    return JSON.parse(window.localStorage.getItem(storageKey) || "{}") || {};
  } catch {
    return {};
  }
}

function saveBoardState(value) {
  try {
    window.localStorage.setItem(storageKey, JSON.stringify(value));
  } catch {
    // Local storage can be disabled; the board remains usable for the session.
  }
}

function inferStatus(car) {
  if (toNumber(read(car, "salePriceBase")) > 0) return "sold";
  if (toNumber(read(car, "repairs")) > 0) return "repairing";
  if (read(car, "isReceived")) return "inspected";
  return "bought";
}

function createDefaultTasks(car) {
  const repairs = toNumber(read(car, "repairs"));
  const customs = toNumber(read(car, "customs"));
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

function normalizeCarBoardRecord(car, boardState) {
  const id = String(itemId(car));
  const existing = boardState[id] || {};
  return {
    status: existing.status || inferStatus(car),
    tasks: Array.isArray(existing.tasks)
      ? existing.tasks
      : createDefaultTasks(car)
  };
}

function taskCost(tasks) {
  return tasks.reduce((total, task) => total + toNumber(task.cost), 0);
}

function taskProgress(tasks) {
  if (!tasks.length) return 0;
  const done = tasks.filter((task) => task.done).length;
  return Math.round((done / tasks.length) * 100);
}

function BoardCard({ car, record, active, onSelect, onMove, onDragStart, displayContext }) {
  const tasks = record.tasks || [];
  const progress = taskProgress(tasks);
  const fullCost = toNumber(read(car, "fullCostBase"));

  return h("button", {
    className: active ? "prep-card active" : "prep-card",
    type: "button",
    draggable: true,
    onClick: onSelect,
    onDragStart
  },
    h("span", { className: "prep-card-avatar" }, initials(carTitle(car))),
    h("span", { className: "prep-card-main" },
      h("strong", null, carTitle(car)),
      h("small", null, `${read(car, "barcode") || "No barcode"} / ${read(car, "location") || "Location"}`)
    ),
    h("span", { className: "prep-card-costs" },
      h("b", null, displayMoneyFromBase(fullCost, displayContext)),
      h("small", null, `Prep ${displayMoneyFromBase(taskCost(tasks), displayContext)}`)
    ),
    h("span", { className: "prep-progress", "aria-label": `${progress}% complete` },
      h("span", { style: { width: `${progress}%` } })
    ),
    h("span", { className: "prep-card-footer" },
      h("small", null, `${tasks.filter((task) => task.done).length}/${tasks.length} tasks`),
      h("span", { className: "prep-card-actions" },
        h("span", { onClick: (event) => { event.stopPropagation(); onMove(-1); } }, "<"),
        h("span", { onClick: (event) => { event.stopPropagation(); onMove(1); } }, ">")
      )
    )
  );
}

function TaskRow({ task, displayContext, onToggle, onDelete }) {
  return h("li", { className: task.done ? "prep-task done" : "prep-task" },
    h("label", null,
      h("input", {
        type: "checkbox",
        checked: Boolean(task.done),
        onChange: (event) => onToggle(event.target.checked)
      }),
      h("span", null,
        h("strong", null, task.title),
        h("small", null, displayMoneyFromBase(task.cost, displayContext))
      )
    ),
    h("button", { className: "ghost-button", type: "button", onClick: onDelete, "aria-label": `Delete ${task.title}` }, "x")
  );
}

function CostLine({ label, value, displayContext }) {
  return h("div", { className: "prep-cost-line" },
    h("span", null, label),
    h("strong", null, displayMoneyFromBase(value, displayContext))
  );
}

export function RepairPrepBoardView({ api, t }) {
  const [cars, setCars] = useState([]);
  const [parts, setParts] = useState([]);
  const [images, setImages] = useState([]);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [selectedId, setSelectedId] = useState("");
  const [boardState, setBoardState] = useState(() => safeLoadBoardState());
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [search, setSearch] = useState("");
  const [newTaskTitle, setNewTaskTitle] = useState("");
  const [newTaskCost, setNewTaskCost] = useState("");

  const selectedCar = useMemo(
    () => cars.find((car) => String(itemId(car)) === String(selectedId)) || null,
    [cars, selectedId]
  );
  const displayContextForCar = useCallback((car) => displayCurrencyContext({
    constants: appConstants,
    rates: currencyRates,
    baseCurrencyCode: read(car, "baseCurrencyCode"),
    counterCurrencyCode: read(car, "counterCurrencyCode")
  }), [appConstants, currencyRates]);
  const selectedDisplayContext = useMemo(
    () => displayContextForCar(selectedCar),
    [displayContextForCar, selectedCar]
  );

  const mergedBoardState = useMemo(() => {
    const next = { ...boardState };
    for (const car of cars) {
      const id = String(itemId(car));
      if (!id) continue;
      next[id] = normalizeCarBoardRecord(car, next);
    }
    return next;
  }, [boardState, cars]);

  useEffect(() => {
    if (cars.length) saveBoardState(mergedBoardState);
  }, [cars.length, mergedBoardState]);

  const loadBoard = useCallback(async (preferredId = "") => {
    setIsLoading(true);
    setStatus("Loading repair board...");
    try {
      const [nextCars, nextParts, nextCurrencies, nextAppConstants] = await Promise.all([
        api.get("/api/usedcars"),
        api.get("/api/parts?page=1&pageSize=500"),
        api.get("/api/currencies"),
        api.get("/api/appconstants")
      ]);
      const carRows = toArray(nextCars);
      setCars(carRows);
      setParts(toArray(nextParts));
      setCurrencyRates(toArray(nextCurrencies));
      setAppConstants(toArray(nextAppConstants));
      setBoardState((current) => {
        const normalized = { ...current };
        for (const car of carRows) {
          const id = String(itemId(car));
          if (id && !normalized[id]) {
            normalized[id] = normalizeCarBoardRecord(car, normalized);
          }
        }
        return normalized;
      });
      setSelectedId((current) => {
        if (preferredId) return String(preferredId);
        if (current && carRows.some((car) => String(itemId(car)) === String(current))) return current;
        return String(itemId(carRows[0]) || "");
      });
      setStatus(`${carRows.length} cars loaded for repair prep.`);
    } catch (error) {
      setCars([]);
      setParts([]);
      setStatus(error.message || "Could not load the repair board.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  useEffect(() => {
    loadBoard();
  }, [loadBoard]);

  useEffect(() => {
    let cancelled = false;

    async function loadImages() {
      if (!selectedCar) {
        setImages([]);
        return;
      }

      try {
        const nextImages = toArray(await api.get(`/api/usedcars/${itemId(selectedCar)}/images`));
        if (!cancelled) setImages(nextImages);
      } catch {
        if (!cancelled) setImages([]);
      }
    }

    loadImages();
    return () => {
      cancelled = true;
    };
  }, [api, selectedCar]);

  const setCarRecord = useCallback((carId, updater) => {
    setBoardState((current) => {
      const car = cars.find((item) => String(itemId(item)) === String(carId));
      if (!car) return current;
      const record = normalizeCarBoardRecord(car, current);
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

  const moveCarByOffset = useCallback((carId, offset) => {
    const record = mergedBoardState[String(carId)];
    const currentIndex = columns.findIndex((column) => column.key === record?.status);
    const nextIndex = Math.min(Math.max(currentIndex + offset, 0), columns.length - 1);
    moveCarToStatus(carId, columns[nextIndex].key);
  }, [mergedBoardState, moveCarToStatus]);

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
    setCarRecord(itemId(selectedCar), (record) => ({
      ...record,
      tasks: [...record.tasks, task]
    }));
    setNewTaskTitle("");
    setNewTaskCost("");
  }, [newTaskCost, newTaskTitle, selectedCar, selectedDisplayContext.rateToBase, setCarRecord]);

  const filteredCars = useMemo(() => {
    const needle = search.trim().toLowerCase();
    if (!needle) return cars;
    return cars.filter((car) =>
      [carTitle(car), read(car, "barcode"), read(car, "location"), read(car, "supplierName")]
        .join(" ")
        .toLowerCase()
        .includes(needle)
    );
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

  return h("section", { className: "screen repair-prep-screen" },
    h(PageHeader, {
      kicker: "Operations",
      title: t("repairPrep.title", "Repair / Prep Board"),
      subtitle: t("repairPrep.subtitle", "Track bought cars through inspection, parts, repair, photos, listing, and sale."),
      action: h("button", {
        className: "secondary-button",
        type: "button",
        onClick: () => loadBoard(selectedId),
        disabled: isLoading
      }, isLoading ? t("common.loading", "Loading") : t("common.refresh", "Refresh"))
    }),
    h(StatusLine, { status }),
    h("section", { className: "prep-toolbar" },
      h("div", { className: "prep-stat" },
        h("span", null, "Cars"),
        h("strong", null, boardSummary.cars)
      ),
      h("div", { className: "prep-stat" },
        h("span", null, "Active"),
        h("strong", null, boardSummary.active)
      ),
      h("div", { className: "prep-stat" },
        h("span", null, "Tasks"),
        h("strong", null, `${boardSummary.doneTasks}/${boardSummary.totalTasks}`)
      ),
      h("label", { className: "prep-search" },
        h("span", null, "Search"),
        h("input", {
          value: search,
          placeholder: "Car, barcode, supplier, location",
          onChange: (event) => setSearch(event.target.value)
        })
      )
    ),
    h("section", { className: "prep-workspace" },
      h("div", { className: "prep-board", "aria-label": "Repair prep board" },
        columns.map((column) => {
          const columnCars = filteredCars.filter((car) => mergedBoardState[String(itemId(car))]?.status === column.key);
          return h("section", {
            key: column.key,
            className: activeColumn?.key === column.key ? "prep-column active" : "prep-column",
            onDragOver: (event) => event.preventDefault(),
            onDrop: (event) => {
              event.preventDefault();
              const carId = event.dataTransfer.getData("text/plain");
              if (carId) moveCarToStatus(carId, column.key);
            }
          },
            h("header", null,
              h("strong", null, column.label),
              h("span", null, columnCars.length)
            ),
            h("div", { className: "prep-column-list" },
              columnCars.map((car) => {
                const id = String(itemId(car));
                return h(BoardCard, {
                  key: id,
                  car,
                  record: mergedBoardState[id],
                  active: id === String(selectedId),
                  onSelect: () => setSelectedId(id),
                  onMove: (offset) => moveCarByOffset(id, offset),
                  onDragStart: (event) => event.dataTransfer.setData("text/plain", id),
                  displayContext: displayContextForCar(car)
                });
              }),
              columnCars.length === 0 && h("p", { className: "empty-state" }, "No cars in this lane.")
            )
          );
        })
      ),
      h("aside", { className: "prep-inspector" },
        selectedCar
          ? h("div", null,
            h("div", { className: "prep-hero" },
              images[0] && imageSrc(images[0])
                ? h("img", { src: imageSrc(images[0]), alt: carTitle(selectedCar) })
                : h("span", null, initials(carTitle(selectedCar))),
              h("div", null,
                h("small", null, activeColumn?.label || "Board"),
                h("h3", null, carTitle(selectedCar)),
                h("p", null, `${read(selectedCar, "barcode") || "No barcode"} / ${read(selectedCar, "supplierName") || "Supplier"}`)
              )
            ),
            h("div", { className: "prep-status-row" },
              columns.map((column) =>
                h("button", {
                  key: column.key,
                  className: selectedRecord?.status === column.key ? "chip active" : "chip",
                  type: "button",
                  onClick: () => moveCarToStatus(itemId(selectedCar), column.key)
                }, column.label)
              )
            ),
            h("section", { className: "prep-inspector-section" },
              h("header", null,
                h("h3", null, "Tasks"),
                h("span", null, `${progress}% complete`)
              ),
              h("div", { className: "prep-progress large" },
                h("span", { style: { width: `${progress}%` } })
              ),
              h("ul", { className: "prep-task-list" },
                selectedTasks.map((task) =>
                  h(TaskRow, {
                    key: task.id,
                    task,
                    displayContext: selectedDisplayContext,
                    onToggle: (done) => updateTask(task.id, { done }),
                    onDelete: () => deleteTask(task.id)
                  })
                ),
                selectedTasks.length === 0 && h("p", { className: "empty-state" }, "No prep tasks yet.")
              ),
              h("div", { className: "prep-add-task" },
                h("input", {
                  value: newTaskTitle,
                  placeholder: "Task",
                  onChange: (event) => setNewTaskTitle(event.target.value),
                  onKeyDown: (event) => {
                    if (event.key === "Enter") addTask();
                  }
                }),
                h("input", {
                  value: newTaskCost,
                  type: "number",
                  placeholder: "Cost",
                  onChange: (event) => setNewTaskCost(event.target.value),
                  onKeyDown: (event) => {
                    if (event.key === "Enter") addTask();
                  }
                }),
                h("button", { className: "primary-button", type: "button", onClick: addTask, disabled: !newTaskTitle.trim() }, "Add")
              )
            ),
            h("section", { className: "prep-inspector-section" },
              h("header", null,
                h("h3", null, "Costs"),
                h("span", null, selectedCurrency)
              ),
              h(CostLine, { label: "Purchase", value: read(selectedCar, "purchaseCostBase", "priceBase", "price"), displayContext: selectedDisplayContext }),
              h(CostLine, { label: "Shipping", value: read(selectedCar, "shippingCostBase", "shipping"), displayContext: selectedDisplayContext }),
              h(CostLine, { label: "Customs", value: read(selectedCar, "customsCostBase", "customs"), displayContext: selectedDisplayContext }),
              h(CostLine, { label: "Repairs", value: read(selectedCar, "repairsCostBase", "repairs"), displayContext: selectedDisplayContext }),
              h(CostLine, { label: "Prep tasks", value: totalPrepCost, displayContext: selectedDisplayContext }),
              h(CostLine, { label: "Full car cost", value: read(selectedCar, "fullCostBase", "grandTotalBase"), displayContext: selectedDisplayContext })
            ),
            h("section", { className: "prep-inspector-section" },
              h("header", null,
                h("h3", null, "Images"),
                h("span", null, `${images.length} photo${images.length === 1 ? "" : "s"}`)
              ),
              h("div", { className: "prep-photo-grid" },
                images.slice(0, 8).map((image, index) =>
                  h("div", { className: "prep-photo", key: itemId(image) || index },
                    imageSrc(image)
                      ? h("img", { src: imageSrc(image), alt: `${carTitle(selectedCar)} ${index + 1}` })
                      : h("span", null, index + 1)
                  )
                ),
                images.length === 0 && h("p", { className: "empty-state" }, "No images uploaded yet.")
              )
            ),
            h("section", { className: "prep-inspector-section" },
              h("header", null,
                h("h3", null, "Linked Parts"),
                h("span", null, linkedParts.length)
              ),
              h("div", { className: "prep-part-list" },
                linkedParts.slice(0, 8).map((part) =>
                  h("div", { key: itemId(part), className: "prep-part-row" },
                    h("strong", null, read(part, "internalCode") || `#${itemId(part)}`),
                    h("span", null, read(part, "name") || "Part"),
                    h("b", null, money(read(part, "costPrice", "averagePrice"), read(part, "currency") || selectedCurrency))
                  )
                ),
                linkedParts.length === 0 && h("p", { className: "empty-state" }, "No linked parts yet.")
              )
            )
          )
          : h("p", { className: "empty-state" }, "Select a car to inspect tasks, costs, and images.")
      )
    )
  );
}
