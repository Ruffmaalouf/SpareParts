import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { displayCurrencyContext, displayMoneyFromBase, escapeHtml, money, shortDate } from "../core/formatters.js";
import { AccountingService } from "../services/accounting-service.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

const reportTabs = [
  { key: "trial", labelKey: "accounting.trialBalance", label: "Trial Balance" },
  { key: "ledger", labelKey: "accounting.ledger", label: "Ledger" },
  { key: "statement", labelKey: "accounting.statement", label: "Statement" }
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
  const number = Number(value || 0);
  return Number.isFinite(number) ? number : 0;
}

function amount(value, currency) {
  return money(toNumber(value), currency || "USD");
}

function accountId(account) {
  return String(read(account, "id", "accountId") || "");
}

function accountTitle(account, t) {
  return read(account, "displayName", "accountDisplay")
    || `${read(account, "code", "accountCode") || ""} ${read(account, "name", "accountName") || ""}`.trim()
    || t("accounting.account", "Account");
}

function accountMeta(account, t) {
  return read(account, "accountType", "accountTypeKey", "parentDisplay") || t("accounting.type", "Type");
}

function partyKey(party) {
  return read(party, "partyType") && read(party, "partyId")
    ? `${read(party, "partyType")}:${read(party, "partyId")}`
    : "";
}

function partyTitle(party) {
  return read(party, "displayName", "name") || "Party";
}

function reportRows(activeTab, report) {
  if (!report) return [];
  if (activeTab === "trial") return toArray(read(report, "rows"));
  return toArray(read(report, "entries"));
}

function baseCurrency(report) {
  return read(report, "baseCurrencyCode") || "USD";
}

function counterCurrency(report) {
  return read(report, "counterCurrencyCode") || baseCurrency(report);
}

function reportTitle(activeTab, t) {
  if (activeTab === "ledger") return t("accounting.generalLedger", "General Ledger");
  if (activeTab === "statement") return t("accounting.statementOfAccount", "Statement of Account");
  return t("accounting.trialBalance", "Trial Balance");
}

function subjectText(activeTab, report, t) {
  if (!report) return t("accounting.chartOfAccounts", "Chart of accounts");
  if (activeTab === "statement") return read(report, "subjectName") || accountTitle(report, t);
  if (activeTab === "ledger") return accountTitle(report, t);
  return t("accounting.chartOfAccounts", "Chart of accounts");
}

function periodText(report, dateFrom, dateTo, t) {
  const from = dateFrom || shortDate(read(report, "dateFrom"));
  const to = dateTo || shortDate(read(report, "dateTo"));
  if (from && to) return `${from} - ${to}`;
  if (from) return `${t("common.from", "From")} ${from}`;
  if (to) return `${t("common.to", "To")} ${to}`;
  return t("accounting.allDates", "All posted dates");
}

function displayBaseAmount(value, displayContext) {
  return displayMoneyFromBase(toNumber(value), displayContext);
}

function summaryForReport(activeTab, report, t, displayContext) {
  if (!report) return [];
  const counter = counterCurrency(report);

  if (activeTab === "ledger") {
    const opening = toNumber(read(report, "openingBalance"));
    const closing = toNumber(read(report, "closingBalance"));
    return [
      [t("accounting.opening", "Opening"), displayBaseAmount(opening, displayContext)],
      [t("accounting.closing", "Closing"), displayBaseAmount(closing, displayContext)],
      [t("accounting.movement", "Movement"), displayBaseAmount(closing - opening, displayContext)],
      [t("accounting.entries", "Entries"), String(reportRows(activeTab, report).length)]
    ];
  }

  if (activeTab === "statement") {
    return [
      [t("accounting.closing", "Closing"), displayBaseAmount(read(report, "remainingBalance", "closingBalance"), displayContext)],
      [t("accounting.totalDebit", "Total debit"), displayBaseAmount(read(report, "totalDebit"), displayContext)],
      [t("accounting.totalCredit", "Total credit"), displayBaseAmount(read(report, "totalCredit"), displayContext)],
      [t("accounting.entries", "Entries"), String(reportRows(activeTab, report).length)]
    ];
  }

  const totalDebit = toNumber(read(report, "totalDebit"));
  const totalCredit = toNumber(read(report, "totalCredit"));
  return [
    [t("accounting.totalDebit", "Total debit"), displayBaseAmount(totalDebit, displayContext)],
    [t("accounting.totalCredit", "Total credit"), displayBaseAmount(totalCredit, displayContext)],
    [t("accounting.difference", "Difference"), displayBaseAmount(totalDebit - totalCredit, displayContext)],
    [`${counter} ${t("accounting.debit", "Debit")}`, amount(read(report, "totalCounterDebit"), counter)],
    [`${counter} ${t("accounting.credit", "Credit")}`, amount(read(report, "totalCounterCredit"), counter)],
    [t("accounting.accounts", "Accounts"), String(reportRows(activeTab, report).length)]
  ];
}

function rowSearchText(row, activeTab, t) {
  if (activeTab === "trial") return [accountTitle(row, t), accountMeta(row, t), read(row, "accountCode"), read(row, "accountName")].join(" ");
  if (activeTab === "ledger") return [shortDate(read(row, "entryDate")), read(row, "referenceType"), read(row, "referenceId"), read(row, "description"), read(row, "currencyCode")].join(" ");
  return [shortDate(read(row, "entryDate")), read(row, "documentNumber"), read(row, "activityType"), read(row, "referenceType"), read(row, "description")].join(" ");
}

function filterRows(rows, activeTab, search, t) {
  const term = search.trim().toLowerCase();
  if (!term) return rows;
  return rows.filter((row) => rowSearchText(row, activeTab, t).toLowerCase().includes(term));
}

function columnsForReport(activeTab, report, t, displayContext) {
  const base = baseCurrency(report);
  const counter = counterCurrency(report);

  if (activeTab === "ledger") {
    return [
      { key: "date", label: t("accounting.date", "Date"), render: (row) => shortDate(read(row, "entryDate")) },
      { key: "reference", label: t("accounting.reference", "Reference"), render: (row) => `${read(row, "referenceType") || ""}${read(row, "referenceId") ? ` #${read(row, "referenceId")}` : ""}`.trim() },
      { key: "description", label: t("accounting.description", "Description"), render: (row) => read(row, "description") || "-" },
      { key: "original", label: t("accounting.original", "Original"), render: (row) => amount(read(row, "originalAmount"), read(row, "currencyCode") || base) },
      { key: "debit", label: t("accounting.debit", "Debit"), render: (row) => displayBaseAmount(read(row, "debit"), displayContext) },
      { key: "credit", label: t("accounting.credit", "Credit"), render: (row) => displayBaseAmount(read(row, "credit"), displayContext) },
      { key: "running", label: t("accounting.running", "Running"), render: (row) => displayBaseAmount(read(row, "runningBalance"), displayContext) },
      { key: "counter", label: counter, render: (row) => amount(read(row, "runningCounterBalance"), counter) }
    ];
  }

  if (activeTab === "statement") {
    return [
      { key: "date", label: t("accounting.date", "Date"), render: (row) => shortDate(read(row, "entryDate")) },
      { key: "document", label: t("accounting.document", "Document"), render: (row) => read(row, "documentNumber") || read(row, "referenceId") },
      { key: "activity", label: t("accounting.activity", "Activity"), render: (row) => read(row, "activityType", "referenceType") },
      { key: "description", label: t("accounting.description", "Description"), render: (row) => read(row, "description") || "-" },
      { key: "original", label: t("accounting.original", "Original"), render: (row) => amount(read(row, "originalAmount"), read(row, "currencyCode") || base) },
      { key: "debit", label: t("accounting.debit", "Debit"), render: (row) => displayBaseAmount(read(row, "debit"), displayContext) },
      { key: "credit", label: t("accounting.credit", "Credit"), render: (row) => displayBaseAmount(read(row, "credit"), displayContext) },
      { key: "running", label: t("accounting.running", "Running"), render: (row) => displayBaseAmount(read(row, "runningBalance"), displayContext) }
    ];
  }

  return [
    { key: "account", label: t("accounting.account", "Account"), render: (row) => accountTitle(row, t) },
    { key: "type", label: t("accounting.type", "Type"), render: (row) => accountMeta(row, t) },
    { key: "debit", label: t("accounting.debit", "Debit"), render: (row) => displayBaseAmount(read(row, "totalDebit"), displayContext) },
    { key: "credit", label: t("accounting.credit", "Credit"), render: (row) => displayBaseAmount(read(row, "totalCredit"), displayContext) },
    { key: "debitBalance", label: "Debit Bal", render: (row) => displayBaseAmount(read(row, "debitBalance"), displayContext) },
    { key: "creditBalance", label: "Credit Bal", render: (row) => displayBaseAmount(read(row, "creditBalance"), displayContext) },
    { key: "counterDebit", label: `${counter} D`, render: (row) => amount(read(row, "counterDebitBalance"), counter) },
    { key: "counterCredit", label: `${counter} C`, render: (row) => amount(read(row, "counterCreditBalance"), counter) }
  ];
}

function buildPrintableHtml({ activeTab, report, rows, columns, dateFrom, dateTo, t, displayContext }) {
  return `<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <title>${escapeHtml(reportTitle(activeTab, t))}</title>
  <style>
    @page { size: A4 landscape; margin: 16mm; }
    body { font-family: Arial, sans-serif; color: #101114; font-size: 10px; }
    h1 { margin: 0 0 4px; font-size: 24px; }
    .muted { color: #5f6675; }
    .header { display: flex; justify-content: space-between; border-bottom: 2px solid #101114; padding-bottom: 12px; margin-bottom: 14px; }
    .summary { display: grid; grid-template-columns: repeat(6, 1fr); gap: 8px; margin-bottom: 14px; }
    .card { border: 1px solid #d6dbe5; border-radius: 6px; padding: 8px; }
    .card span { color: #5f6675; display: block; font-size: 8px; text-transform: uppercase; }
    .card strong { display: block; margin-top: 4px; font-size: 11px; }
    table { width: 100%; border-collapse: collapse; table-layout: fixed; }
    th { background: #101114; color: white; padding: 6px; text-align: left; font-size: 8px; text-transform: uppercase; }
    td { border-bottom: 1px solid #e5e7eb; padding: 5px; overflow-wrap: anywhere; }
    tr:nth-child(even) td { background: #f8fafc; }
  </style>
</head>
<body>
  <div class="header">
    <div>
      <h1>${escapeHtml(reportTitle(activeTab, t))}</h1>
      <div class="muted">${escapeHtml(subjectText(activeTab, report, t))}</div>
      <div class="muted">${escapeHtml(periodText(report, dateFrom, dateTo, t))}</div>
    </div>
    <div class="muted">${escapeHtml(displayContext.code)} / ${escapeHtml(t("accounting.rows", "{count} rows", { count: rows.length }))}</div>
  </div>
  <div class="summary">${summaryForReport(activeTab, report, t, displayContext).map(([label, value]) => `<div class="card"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`).join("")}</div>
  <table>
    <thead><tr>${columns.map((column) => `<th>${escapeHtml(column.label)}</th>`).join("")}</tr></thead>
    <tbody>${rows.map((row) => `<tr>${columns.map((column) => `<td>${escapeHtml(column.render(row))}</td>`).join("")}</tr>`).join("")}</tbody>
  </table>
</body>
</html>`;
}

function ChipSelector({ title, rows, selectedKey, onSelect, getKey, getTitle, getMeta, emptyText }) {
  return h("section", { className: "selector-panel" },
    h("h3", null, title),
    rows.length > 0
      ? h("div", { className: "chip-scroll" },
        rows.map((row, index) => {
          const key = getKey(row);
          return h("button", {
            key: key || index,
            className: key === selectedKey ? "selector-chip active" : "selector-chip",
            type: "button",
            onClick: () => onSelect(key)
          },
            h("strong", null, getTitle(row)),
            h("span", null, getMeta(row))
          );
        })
      )
      : h("p", { className: "empty-state" }, emptyText)
  );
}

export function AccountingView({ api, t }) {
  const service = useMemo(() => new AccountingService(api), [api]);
  const [activeTab, setActiveTab] = useState("trial");
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [search, setSearch] = useState("");
  const [accounts, setAccounts] = useState([]);
  const [parties, setParties] = useState([]);
  const [appConstants, setAppConstants] = useState([]);
  const [currencyRates, setCurrencyRates] = useState([]);
  const [selectedAccountId, setSelectedAccountId] = useState("");
  const [selectedPartyKey, setSelectedPartyKey] = useState("");
  const [trialBalance, setTrialBalance] = useState(null);
  const [ledger, setLedger] = useState(null);
  const [statement, setStatement] = useState(null);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const currentReport = activeTab === "ledger" ? ledger : activeTab === "statement" ? statement : trialBalance;
  const rawRows = useMemo(() => reportRows(activeTab, currentReport), [activeTab, currentReport]);
  const rows = useMemo(() => filterRows(rawRows, activeTab, search, t), [activeTab, rawRows, search, t]);
  const trialAccounts = useMemo(() => reportRows("trial", trialBalance), [trialBalance]);
  const selectableAccounts = accounts.length > 0 ? accounts : trialAccounts;
  const selectedParty = useMemo(
    () => parties.find((party) => partyKey(party) === selectedPartyKey) || null,
    [parties, selectedPartyKey]
  );
  const currentDisplayContext = useMemo(() => displayCurrencyContext({
    constants: appConstants,
    rates: currencyRates,
    baseCurrencyCode: baseCurrency(currentReport),
    counterCurrencyCode: counterCurrency(currentReport)
  }), [appConstants, currencyRates, currentReport]);
  const columns = useMemo(() => columnsForReport(activeTab, currentReport, t, currentDisplayContext), [activeTab, currentDisplayContext, currentReport, t]);

  const loadReferenceData = useCallback(async () => {
    const [accountResult, partyResult, appConstantsResult, currenciesResult] = await Promise.allSettled([
      service.getAccounts(),
      service.getStatementParties(),
      api.get("/api/appconstants"),
      api.get("/api/currencies")
    ]);
    if (accountResult.status === "fulfilled") {
      const nextAccounts = toArray(accountResult.value);
      setAccounts(nextAccounts);
      setSelectedAccountId((current) => current || accountId(nextAccounts[0]) || "");
    }
    if (partyResult.status === "fulfilled") {
      const nextParties = toArray(partyResult.value);
      setParties(nextParties);
      setSelectedPartyKey((current) => current || partyKey(nextParties[0]) || "");
    }
    if (appConstantsResult.status === "fulfilled") setAppConstants(toArray(appConstantsResult.value));
    if (currenciesResult.status === "fulfilled") setCurrencyRates(toArray(currenciesResult.value));
  }, [api, service]);

  const loadActiveReport = useCallback(async () => {
    if (activeTab === "ledger" && !selectedAccountId) {
      setLedger(null);
      setStatus(t("accounting.chooseLedger", "Choose an account to load the ledger."));
      return;
    }
    if (activeTab === "statement" && !selectedPartyKey && !selectedAccountId) {
      setStatement(null);
      setStatus(t("accounting.chooseStatement", "Choose a party or account to load the statement."));
      return;
    }

    setIsLoading(true);
    setStatus(t("accounting.loadingReport", "Loading {label}...", { label: reportTitle(activeTab, t).toLowerCase() }));
    try {
      if (activeTab === "trial") {
        setTrialBalance(await service.getTrialBalance(dateFrom, dateTo));
        setStatus(t("accounting.trialLoaded", "Trial balance loaded."));
      } else if (activeTab === "ledger") {
        setLedger(await service.getLedger(selectedAccountId, dateFrom, dateTo));
        setStatus(t("accounting.ledgerLoaded", "Ledger loaded."));
      } else if (selectedParty) {
        setStatement(await service.getStatementOfAccountForParty(
          read(selectedParty, "partyType"),
          read(selectedParty, "partyId"),
          dateFrom,
          dateTo
        ));
        setStatus(t("accounting.statementLoaded", "Statement of account loaded."));
      } else {
        setStatement(await service.getStatementOfAccount(selectedAccountId, dateFrom, dateTo));
        setStatus(t("accounting.statementLoaded", "Statement of account loaded."));
      }
    } catch (error) {
      setStatus(error.message || t("accounting.loadError", "Could not load accounting report."));
    } finally {
      setIsLoading(false);
    }
  }, [activeTab, dateFrom, dateTo, selectedAccountId, selectedParty, selectedPartyKey, service, t]);

  const exportPdf = useCallback(() => {
    if (!currentReport) {
      setStatus(t("accounting.loadBeforeExport", "Load a report before exporting."));
      return;
    }

    try {
      setStatus(t("accounting.creatingPdf", "Creating PDF report..."));
      const html = buildPrintableHtml({ activeTab, report: currentReport, rows, columns, dateFrom, dateTo, t, displayContext: currentDisplayContext });
      const printWindow = window.open("", "_blank", "width=1200,height=800");
      if (!printWindow) throw new Error("Popup blocked.");
      printWindow.document.write(html);
      printWindow.document.close();
      printWindow.focus();
      printWindow.print();
      setStatus(t("accounting.pdfReady", "PDF report ready."));
    } catch (error) {
      setStatus(error.message || t("accounting.exportError", "Could not export PDF report."));
    }
  }, [activeTab, columns, currentDisplayContext, currentReport, dateFrom, dateTo, rows, t]);

  useEffect(() => { loadReferenceData(); }, [loadReferenceData]);
  useEffect(() => { loadActiveReport(); }, [loadActiveReport]);
  useEffect(() => { setSearch(""); }, [activeTab]);
  useEffect(() => {
    if (!selectedAccountId && selectableAccounts.length > 0) {
      setSelectedAccountId(accountId(selectableAccounts[0]));
    }
  }, [selectableAccounts, selectedAccountId]);

  return h("section", { className: "screen accounting-screen" },
    h(PageHeader, {
      title: t("accounting.title", "Accounting Review"),
      action: h("button", { className: "secondary-button", type: "button", onClick: loadActiveReport, disabled: isLoading }, isLoading ? t("common.loading", "Loading") : t("common.refresh", "Refresh"))
    }),
    h("div", { className: "segmented-scroll" },
      reportTabs.map((tab) =>
        h("button", {
          key: tab.key,
          className: tab.key === activeTab ? "segment active" : "segment",
          type: "button",
          onClick: () => setActiveTab(tab.key)
        }, t(tab.labelKey, tab.label))
      )
    ),
    h("section", { className: "report-hero" },
      h("div", null,
        h("span", { className: "eyebrow" }, t("accounting.financeReport", "Finance Report")),
        h("h2", null, reportTitle(activeTab, t)),
        h("p", null, subjectText(activeTab, currentReport, t))
      ),
      h("div", { className: "report-hero-meta" },
        h("strong", null, currentDisplayContext.code),
        h("span", null, counterCurrency(currentReport)),
        h("span", null, periodText(currentReport, dateFrom, dateTo, t))
      )
    ),
    currentReport && h("div", { className: "report-summary-grid" },
      summaryForReport(activeTab, currentReport, t, currentDisplayContext).map(([label, value]) =>
        h("article", { className: "report-summary-card", key: label },
          h("span", null, label),
          h("strong", null, value)
        )
      )
    ),
    h("section", { className: "report-controls" },
      h("label", null, t("common.from", "From"), h("input", { type: "date", value: dateFrom, onChange: (event) => setDateFrom(event.target.value) })),
      h("label", null, t("common.to", "To"), h("input", { type: "date", value: dateTo, onChange: (event) => setDateTo(event.target.value) })),
      h("label", null, t("accounting.searchReport", "Search report"), h("input", { value: search, onChange: (event) => setSearch(event.target.value), placeholder: t("accounting.searchReport", "Search report") })),
      h("div", { className: "report-buttons" },
        h("button", { className: "secondary-button", type: "button", onClick: loadActiveReport, disabled: isLoading }, t("common.load", "Load")),
        h("button", { className: "primary-button", type: "button", onClick: exportPdf }, t("accounting.exportPdf", "Export PDF"))
      )
    ),
    activeTab === "ledger" && h(ChipSelector, {
      title: t("accounting.ledgerAccount", "Ledger Account"),
      rows: selectableAccounts,
      selectedKey: selectedAccountId,
      onSelect: setSelectedAccountId,
      getKey: accountId,
      getTitle: (row) => accountTitle(row, t),
      getMeta: (row) => accountMeta(row, t),
      emptyText: t("accounting.noAccounts", "No accounts are available yet.")
    }),
    activeTab === "statement" && h(ChipSelector, {
      title: t("accounting.statementParty", "Statement Party"),
      rows: parties,
      selectedKey: selectedPartyKey,
      onSelect: setSelectedPartyKey,
      getKey: partyKey,
      getTitle: partyTitle,
      getMeta: (row) => read(row, "accountDisplay") || read(row, "partyType"),
      emptyText: t("accounting.noParties", "No statement parties are linked yet.")
    }),
    h(StatusLine, { status }),
    h("section", { className: "table-panel report-table-panel" },
      h(DataTable, {
        columns,
        rows,
        getRowKey: (row, index) => `${activeTab}-${read(row, "accountId", "journalEntryId", "referenceId", "documentNumber") || index}-${index}`,
        emptyText: activeTab === "trial"
          ? t("accounting.noAccounts", "No accounts are available yet.")
          : t("module.noRows", "No rows returned.", { title: reportTitle(activeTab, t) })
      })
    )
  );
}
