const React = require("react");
const { Pressable, ScrollView, Text, View } = require("react-native");
const { asRows, rowAmount, rowSubtitle, rowTitle } = require("../core/formatters");
const { EmptyState, Field, ListRow, Panel, PrimaryButton, ScreenHeader, ScreenScroll, StatusText } = require("../components/ui");
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

function ModuleSummary({ module, moduleTitle, t }) {
  return el(Panel, { title: t("module.wpfWorkspace", "WPF workspace") },
    el(ListRow, { title: t("module.api", "API"), subtitle: module.endpoint }),
    module.capabilities.map((capability) =>
      el(ListRow, { key: capability, title: capability })
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
  const [code, setCode] = useState("");
  const [rows, setRows] = useState([]);
  const [status, setStatus] = useState(t("scans.ready", "Enter a barcode, invoice number, purchase number, or stock code."));
  const [isLoading, setIsLoading] = useState(false);

  const resolve = useCallback(async () => {
    const scanCode = code.trim();
    if (!scanCode) {
      setStatus(t("scans.codeRequired", "Enter a scan code first."));
      return;
    }

    setIsLoading(true);
    setStatus(t("scans.resolving", "Resolving scan code..."));
    try {
      const result = asRows(await api.get(`${module.endpoint}?code=${encodeURIComponent(scanCode)}`));
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
  }, [api, code, module.endpoint, t]);

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
    el(StatusText, { value: status }),
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
      setRows(asRows(await api.get(module.endpoint)));
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
