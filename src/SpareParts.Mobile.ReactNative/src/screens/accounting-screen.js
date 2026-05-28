const React = require("react");
const { Pressable, ScrollView, Text, TextInput, View } = require("react-native");
const Print = require("expo-print");
const Sharing = require("expo-sharing");
const { EmptyState, Panel, ScreenHeader, ScreenScroll, StatusText } = require("../components/ui");
const { displayCurrencyContext, displayMoneyFromBase, money, shortDate } = require("../core/formatters");
const { AccountingService } = require("../services/accounting-service");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

const reportTabs = [
  { key: "trial", label: "Trial Balance" },
  { key: "ledger", label: "Ledger" },
  { key: "statement", label: "Statement" }
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

function toArray(value) {
  return Array.isArray(value) ? value : value ? [value] : [];
}

function nestedRows(report, key) {
  return toArray(read(report, key));
}

function toNumber(value) {
  const number = Number(value || 0);
  return Number.isFinite(number) ? number : 0;
}

function amount(value, currency) {
  return money(toNumber(value), currency || "USD");
}

function compactAmount(value, currency) {
  const number = toNumber(value);
  const sign = number < 0 ? "-" : "";
  return `${sign}${amount(Math.abs(number), currency)}`;
}

function displayBaseAmount(value, displayContext) {
  return displayMoneyFromBase(toNumber(value), displayContext);
}

function compactDisplayBaseAmount(value, displayContext) {
  const number = toNumber(value);
  const sign = number < 0 ? "-" : "";
  return `${sign}${displayBaseAmount(Math.abs(number), displayContext)}`;
}

function accountId(account) {
  return String(read(account, "id", "accountId") || "");
}

function accountTitle(account, t) {
  const display = read(account, "displayName", "accountDisplay");
  if (display) return display;

  const code = read(account, "code", "accountCode");
  const name = read(account, "name", "accountName");
  return `${code || ""} ${name || ""}`.trim() || (t ? t("common.account", "Account") : "Account");
}

function accountMeta(account, t) {
  return read(account, "accountType", "accountTypeKey", "parentDisplay") || (t ? t("accounting.ledgerAccountFallback", "Ledger account") : "Ledger account");
}

function partyKey(party) {
  const type = read(party, "partyType");
  const id = read(party, "partyId");
  return type && id ? `${type}:${id}` : "";
}

function partyTitle(party, t) {
  return read(party, "displayName", "name") || (t ? t("accounting.accountParty", "Account party") : "Account party");
}

function partyMeta(party, t) {
  return read(party, "accountDisplay") || read(party, "partyType") || (t ? t("accounting.statementPartyFallback", "Statement party") : "Statement party");
}

function reportTitle(activeTab, t) {
  if (activeTab === "ledger") return t("accounting.generalLedger", "General Ledger");
  if (activeTab === "statement") return t("accounting.statementOfAccount", "Statement of Account");
  return t("accounting.trialBalance", "Trial Balance");
}

function reportRows(activeTab, report) {
  if (activeTab === "ledger") return nestedRows(report, "entries");
  if (activeTab === "statement") return nestedRows(report, "entries");
  return nestedRows(report, "rows");
}

function periodText(report, dateFrom, dateTo, t) {
  const loadedFrom = read(report, "dateFrom");
  const loadedTo = read(report, "dateTo");
  const from = dateFrom || (loadedFrom ? shortDate(loadedFrom) : "");
  const to = dateTo || (loadedTo ? shortDate(loadedTo) : "");

  if (from && to) return t("accounting.fromTo", `${from} to ${to}`, { from, to });
  if (from) return t("accounting.fromDate", `From ${from}`, { date: from });
  if (to) return t("accounting.toDate", `To ${to}`, { date: to });
  return t("accounting.allDates", "All posted dates");
}

function baseCurrency(report) {
  return read(report, "baseCurrencyCode") || "USD";
}

function counterCurrency(report) {
  return read(report, "counterCurrencyCode") || baseCurrency(report);
}

function subjectText(activeTab, report, t) {
  if (!report) return t("accounting.noReport", "No report loaded");
  if (activeTab === "statement") return read(report, "subjectName") || accountTitle(report, t);
  if (activeTab === "ledger") return accountTitle(report, t);
  return t("accounting.chartOfAccounts", "Chart of accounts");
}

function rowSearchText(row, activeTab) {
  if (activeTab === "trial") {
    return [
      accountTitle(row),
      accountMeta(row),
      read(row, "accountCode"),
      read(row, "accountName")
    ].join(" ");
  }

  if (activeTab === "ledger") {
    return [
      shortDate(read(row, "entryDate")),
      read(row, "referenceType"),
      read(row, "referenceId"),
      read(row, "description"),
      read(row, "currencyCode")
    ].join(" ");
  }

  return [
    shortDate(read(row, "entryDate")),
    read(row, "documentNumber"),
    read(row, "activityType"),
    read(row, "referenceType"),
    read(row, "description"),
    read(row, "currencyCode")
  ].join(" ");
}

function filterRows(rows, activeTab, search) {
  const term = search.trim().toLowerCase();
  if (!term) return rows;
  return rows.filter((row) => rowSearchText(row, activeTab).toLowerCase().includes(term));
}

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function pdfCell(value, className) {
  return `<td class="${className || ""}">${escapeHtml(value || "-")}</td>`;
}

function summaryForReport(activeTab, report, t, displayContext) {
  if (!report) return [];

  const counter = counterCurrency(report);

  if (activeTab === "ledger") {
    const opening = toNumber(read(report, "openingBalance"));
    const closing = toNumber(read(report, "closingBalance"));
    return [
      { label: t("accounting.opening", "Opening"), value: displayBaseAmount(opening, displayContext), tone: "neutral" },
      { label: t("accounting.closing", "Closing"), value: displayBaseAmount(closing, displayContext), tone: "strong" },
      { label: t("accounting.movement", "Movement"), value: compactDisplayBaseAmount(closing - opening, displayContext), tone: closing - opening >= 0 ? "debit" : "credit" },
      { label: t("accounting.counterClosing", "Counter closing"), value: amount(read(report, "closingCounterBalance"), counter), tone: "neutral" },
      { label: t("accounting.entries", "Entries"), value: String(nestedRows(report, "entries").length), tone: "neutral" }
    ];
  }

  if (activeTab === "statement") {
    return [
      { label: t("accounting.remaining", "Remaining"), value: displayBaseAmount(read(report, "remainingBalance", "closingBalance"), displayContext), tone: "strong" },
      { label: t("accounting.totalDebit", "Total debit"), value: displayBaseAmount(read(report, "totalDebit"), displayContext), tone: "debit" },
      { label: t("accounting.totalCredit", "Total credit"), value: displayBaseAmount(read(report, "totalCredit"), displayContext), tone: "credit" },
      { label: t("accounting.invoices", "Invoices"), value: displayBaseAmount(read(report, "totalInvoiceAmount"), displayContext), tone: "neutral" },
      { label: t("accounting.payments", "Payments"), value: displayBaseAmount(read(report, "totalPaymentAmount"), displayContext), tone: "neutral" },
      { label: t("accounting.journals", "Journals"), value: displayBaseAmount(read(report, "totalJournalAmount"), displayContext), tone: "neutral" }
    ];
  }

  const totalDebit = toNumber(read(report, "totalDebit"));
  const totalCredit = toNumber(read(report, "totalCredit"));
  return [
    { label: t("accounting.totalDebit", "Total debit"), value: displayBaseAmount(totalDebit, displayContext), tone: "debit" },
    { label: t("accounting.totalCredit", "Total credit"), value: displayBaseAmount(totalCredit, displayContext), tone: "credit" },
    { label: t("accounting.difference", "Difference"), value: compactDisplayBaseAmount(totalDebit - totalCredit, displayContext), tone: totalDebit === totalCredit ? "neutral" : "strong" },
    { label: t("accounting.counterDebit", "Counter debit"), value: amount(read(report, "totalCounterDebit"), counter), tone: "neutral" },
    { label: t("accounting.counterCredit", "Counter credit"), value: amount(read(report, "totalCounterCredit"), counter), tone: "neutral" },
    { label: t("accounting.accounts", "Accounts"), value: String(nestedRows(report, "rows").length), tone: "neutral" }
  ];
}

function exportColumns(activeTab, t) {
  if (activeTab === "ledger") {
    return [t("accounting.date", "Date"), t("accounting.reference", "Reference"), t("accounting.description", "Description"), t("accounting.original", "Original"), t("accounting.debit", "Debit"), t("accounting.credit", "Credit"), t("accounting.running", "Running"), t("accounting.counterRunning", "Counter running")];
  }

  if (activeTab === "statement") {
    return [t("accounting.date", "Date"), t("accounting.document", "Document"), t("accounting.activity", "Activity"), t("accounting.description", "Description"), t("accounting.original", "Original"), t("accounting.debit", "Debit"), t("accounting.credit", "Credit"), t("accounting.running", "Running")];
  }

  return [t("accounting.account", "Account"), t("accounting.type", "Type"), t("accounting.debit", "Debit"), t("accounting.credit", "Credit"), t("accounting.debitBalance", "Debit balance"), t("accounting.creditBalance", "Credit balance"), t("accounting.counterDebit", "Counter debit"), t("accounting.counterCredit", "Counter credit")];
}

function exportRowCells(activeTab, row, report, t, displayContext) {
  const base = baseCurrency(report);
  const counter = counterCurrency(report);

  if (activeTab === "ledger") {
    const referenceType = read(row, "referenceType");
    const referenceId = read(row, "referenceId");
    return [
      shortDate(read(row, "entryDate")),
      `${referenceType || ""}${referenceId ? ` #${referenceId}` : ""}`.trim(),
      read(row, "description") || t("accounting.journalMovement", "Journal movement"),
      amount(read(row, "originalAmount"), read(row, "currencyCode") || base),
      displayBaseAmount(read(row, "debit"), displayContext),
      displayBaseAmount(read(row, "credit"), displayContext),
      displayBaseAmount(read(row, "runningBalance"), displayContext),
      amount(read(row, "runningCounterBalance"), counter)
    ];
  }

  if (activeTab === "statement") {
    return [
      shortDate(read(row, "entryDate")),
      read(row, "documentNumber") || read(row, "referenceId"),
      read(row, "activityType", "referenceType"),
      read(row, "description") || t("accounting.accountMovement", "Account movement"),
      amount(read(row, "originalAmount"), read(row, "currencyCode") || base),
      displayBaseAmount(read(row, "debit"), displayContext),
      displayBaseAmount(read(row, "credit"), displayContext),
      displayBaseAmount(read(row, "runningBalance"), displayContext)
    ];
  }

  return [
    accountTitle(row, t),
    accountMeta(row, t),
    displayBaseAmount(read(row, "totalDebit"), displayContext),
    displayBaseAmount(read(row, "totalCredit"), displayContext),
    displayBaseAmount(read(row, "debitBalance"), displayContext),
    displayBaseAmount(read(row, "creditBalance"), displayContext),
    amount(read(row, "counterDebitBalance"), counter),
    amount(read(row, "counterCreditBalance"), counter)
  ];
}

function buildPdfHtml({ activeTab, report, dateFrom, dateTo, t, displayContext }) {
  const columns = exportColumns(activeTab, t);
  const rows = reportRows(activeTab, report);
  const summary = summaryForReport(activeTab, report, t, displayContext);
  const numericStart = activeTab === "trial" ? 2 : 3;

  return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8" />
  <style>
    @page { size: A4 landscape; margin: 18px; }
    body { color: #111827; font-family: Helvetica, Arial, sans-serif; font-size: 10px; }
    h1 { font-size: 22px; margin: 0 0 4px; }
    .muted { color: #6b7280; }
    .header { display: flex; justify-content: space-between; gap: 16px; border-bottom: 2px solid #111827; padding-bottom: 10px; margin-bottom: 12px; }
    .badge { border: 1px solid #d1d5db; border-radius: 6px; padding: 7px 9px; text-align: right; }
    .summary { display: grid; grid-template-columns: repeat(6, 1fr); gap: 7px; margin-bottom: 12px; }
    .summary-card { border: 1px solid #d1d5db; border-radius: 6px; padding: 7px; }
    .summary-label { color: #6b7280; font-size: 8px; text-transform: uppercase; letter-spacing: .04em; }
    .summary-value { font-size: 12px; font-weight: 700; margin-top: 4px; }
    table { width: 100%; border-collapse: collapse; table-layout: fixed; }
    th { background: #111827; color: white; font-size: 8px; text-align: left; padding: 6px; text-transform: uppercase; }
    td { border-bottom: 1px solid #e5e7eb; padding: 5px 6px; vertical-align: top; overflow-wrap: anywhere; }
    .num { text-align: right; font-variant-numeric: tabular-nums; }
    tr:nth-child(even) td { background: #f9fafb; }
  </style>
</head>
<body>
  <div class="header">
    <div>
      <h1>${escapeHtml(reportTitle(activeTab, t))}</h1>
      <div class="muted">${escapeHtml(subjectText(activeTab, report, t))}</div>
      <div class="muted">${escapeHtml(t("accounting.period", "Period"))}: ${escapeHtml(periodText(report, dateFrom, dateTo, t))}</div>
    </div>
    <div class="badge">
      <strong>${escapeHtml(displayContext.code)}</strong><br />
      <span class="muted">${escapeHtml(t("accounting.counter", `Counter ${counterCurrency(report)}`, { currency: counterCurrency(report) }))}</span><br />
      <span class="muted">${escapeHtml(t("accounting.rows", `${rows.length} rows`, { count: rows.length }))}</span>
    </div>
  </div>
  <div class="summary">
    ${summary.map((item) => `
      <div class="summary-card">
        <div class="summary-label">${escapeHtml(item.label)}</div>
        <div class="summary-value">${escapeHtml(item.value)}</div>
      </div>
    `).join("")}
  </div>
  <table>
    <thead>
      <tr>${columns.map((column, index) => `<th class="${index >= numericStart ? "num" : ""}">${escapeHtml(column)}</th>`).join("")}</tr>
    </thead>
    <tbody>
      ${rows.map((row) => `<tr>${exportRowCells(activeTab, row, report, t, displayContext).map((cell, index) => pdfCell(cell, index >= numericStart ? "num" : "")).join("")}</tr>`).join("")}
    </tbody>
  </table>
</body>
</html>`;
}

function SummaryStrip({ items }) {
  const { styles } = useTheme();

  return el(ScrollView, {
    horizontal: true,
    showsHorizontalScrollIndicator: false,
    contentContainerStyle: styles.reportSummaryStrip
  },
    items.map((item) =>
      el(View, { key: item.label, style: [styles.reportSummaryCard, item.tone === "strong" && styles.reportSummaryCardStrong] },
        el(Text, { style: styles.reportSummaryLabel }, item.label),
        el(Text, {
          style: [
            styles.reportSummaryValue,
            item.tone === "debit" && styles.reportDebitText,
            item.tone === "credit" && styles.reportCreditText
          ],
          numberOfLines: 2,
          selectable: true
        }, item.value)
      )
    )
  );
}

function DateField({ label, value, onChangeText }) {
  const { styles, palette } = useTheme();

  return el(View, { style: styles.usedCarField },
    el(Text, { style: styles.fieldLabel }, label),
    el(TextInput, {
      style: styles.input,
      value,
      onChangeText,
      placeholder: "YYYY-MM-DD",
      placeholderTextColor: palette.soft,
      autoCapitalize: "none"
    })
  );
}

function DateFilters({ dateFrom, dateTo, onDateFromChange, onDateToChange }) {
  const { styles, t } = useTheme();

  return el(Panel, { title: t("accounting.period", "Period") },
    el(View, { style: styles.usedCarFormGrid },
      el(DateField, { label: t("common.from", "From"), value: dateFrom, onChangeText: onDateFromChange }),
      el(DateField, { label: t("common.to", "To"), value: dateTo, onChangeText: onDateToChange })
    )
  );
}

function ReportTabs({ activeTab, onChange }) {
  const { styles, t } = useTheme();

  return el(ScrollView, {
    horizontal: true,
    showsHorizontalScrollIndicator: false,
    contentContainerStyle: styles.segmentRail
  },
    reportTabs.map((tab) => {
      const isActive = tab.key === activeTab;
      return el(Pressable, {
        key: tab.key,
        style: [styles.segmentButton, isActive && styles.segmentButtonActive],
        onPress: () => onChange(tab.key)
      }, el(Text, { style: [styles.segmentText, isActive && styles.segmentTextActive] }, t(`accounting.${tab.key === "trial" ? "trialBalance" : tab.key}`, tab.label)));
    })
  );
}

function ReportHero({ activeTab, report, dateFrom, dateTo, rowCount, displayContext }) {
  const { styles, t } = useTheme();

  return el(View, { style: styles.reportHero },
    el(View, { style: styles.reportHeroTop },
      el(View, { style: styles.reportHeroCopy },
        el(Text, { style: styles.reportKicker }, t("accounting.financeReport", "Finance Report")),
        el(Text, { style: styles.reportHeroTitle, selectable: true }, reportTitle(activeTab, t)),
        el(Text, { style: styles.reportHeroSubtitle, selectable: true }, subjectText(activeTab, report, t))
      ),
      el(View, { style: styles.reportHeroBadge },
        el(Text, { style: styles.reportHeroBadgeText, selectable: true }, displayContext.code),
        el(Text, { style: styles.reportHeroBadgeMeta, selectable: true }, t("accounting.counter", `Counter ${counterCurrency(report)}`, { currency: counterCurrency(report) }))
      )
    ),
    el(View, { style: styles.reportHeroMetaRow },
      el(Text, { style: styles.reportHeroMeta, selectable: true }, periodText(report, dateFrom, dateTo, t)),
      el(Text, { style: styles.reportHeroMeta, selectable: true }, t("accounting.rows", `${rowCount} rows`, { count: rowCount }))
    )
  );
}

function ReportTools({ search, onSearch, onRefresh, onExport, loading, exporting }) {
  const { styles, palette, t } = useTheme();

  return el(View, { style: styles.reportTools },
    el(TextInput, {
      style: styles.reportSearchInput,
      value: search,
      onChangeText: onSearch,
      placeholder: t("accounting.searchReport", "Search report"),
      placeholderTextColor: palette.soft,
      autoCapitalize: "none"
    }),
    el(View, { style: styles.reportToolButtons },
      el(Pressable, {
        style: [styles.reportToolButton, loading && styles.disabledButton],
        onPress: onRefresh,
        disabled: loading
      }, el(Text, { style: styles.reportToolButtonText }, loading ? t("common.loading", "Loading") : t("common.load", "Load"))),
      el(Pressable, {
        style: [styles.reportToolButton, styles.reportToolButtonPrimary, exporting && styles.disabledButton],
        onPress: onExport,
        disabled: exporting
      }, el(Text, { style: [styles.reportToolButtonText, styles.reportToolButtonTextPrimary] }, exporting ? t("accounting.creating", "Creating") : t("accounting.exportPdf", "Export PDF")))
    )
  );
}

function ChipSelector({ title, rows, selectedKey, onSelect, getKey, getTitle, getMeta, emptyText }) {
  const { styles } = useTheme();

  return el(Panel, { title },
    rows.length > 0
      ? el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.segmentRail
      },
        rows.map((row, index) => {
          const key = getKey(row);
          const isActive = key === selectedKey;
          return el(Pressable, {
            key: `${title}-${key || index}`,
            style: [styles.usedCarSelectChip, isActive && styles.usedCarSelectChipActive],
            onPress: () => onSelect(key)
          },
            el(Text, {
              style: [styles.usedCarSelectChipText, isActive && styles.usedCarSelectChipTextActive],
              numberOfLines: 1
            }, getTitle(row)),
            Boolean(getMeta(row)) && el(Text, {
              style: styles.usedCarSelectChipMeta,
              numberOfLines: 1
            }, getMeta(row))
          );
        })
      )
      : el(EmptyState, { text: emptyText })
  );
}

function ReportCell({ children, style, textStyle, lines }) {
  const { styles } = useTheme();

  return el(View, { style: [styles.reportCell, style] },
    el(Text, {
      style: [styles.reportCellText, textStyle],
      numberOfLines: lines || 1,
      selectable: true
    }, children || "-")
  );
}

function ReportHeaderCell({ label, style, numeric }) {
  const { styles } = useTheme();

  return el(View, { style: [styles.reportCell, style] },
    el(Text, { style: [styles.reportHeaderText, numeric && styles.reportNumericText] }, label)
  );
}

function DetailGrid({ items }) {
  const { styles } = useTheme();

  return el(View, { style: styles.reportDetailGrid },
    items.map((item) =>
      el(View, { key: item.label, style: styles.reportDetailTile },
        el(Text, { style: styles.reportDetailLabel }, item.label),
        el(Text, { style: styles.reportDetailValue, selectable: true }, item.value || "-")
      )
    )
  );
}

function ReportTable({ headers, rows, emptyText, minWidth, renderRow }) {
  const { styles } = useTheme();

  return el(View, { style: styles.reportTableFrame },
    el(ScrollView, {
      horizontal: true,
      showsHorizontalScrollIndicator: true,
      style: styles.reportHorizontalScroll
    },
      el(View, { style: [styles.reportTableWide, { width: minWidth }] },
        el(View, { style: styles.reportTableHeader },
          headers.map((header) =>
            el(ReportHeaderCell, {
              key: header.label,
              label: header.label,
              style: header.style,
              numeric: header.numeric
            })
          )
        ),
        el(ScrollView, {
          style: styles.reportVerticalScroll,
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.reportTableBody
        },
          rows.map(renderRow),
          rows.length === 0 && el(EmptyState, { text: emptyText })
        )
      )
    )
  );
}

function TrialBalanceView({ report, rows, displayContext }) {
  const { styles, t } = useTheme();
  const [expandedKey, setExpandedKey] = useState("");
  const counter = counterCurrency(report);
  const headers = [
    { label: t("accounting.account", "Account"), style: styles.reportCellAccount },
    { label: t("accounting.type", "Type"), style: styles.reportCellType },
    { label: t("accounting.debit", "Debit"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.credit", "Credit"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.debitBalance", "Debit Bal"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.creditBalance", "Credit Bal"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.counterDebit", "Counter D"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.counterCredit", "Counter C"), style: styles.reportCellMoney, numeric: true }
  ];

  return el(Panel, { title: t("accounting.accountBalances", "Account Balances") },
    el(ReportTable, {
      headers,
      rows,
      minWidth: 980,
      emptyText: t("accounting.noTrialRows", "No trial balance rows returned."),
      renderRow: (row, index) => {
        const key = `trial-${read(row, "accountId") || index}`;
        const expanded = expandedKey === key;
        return el(React.Fragment, { key },
          el(Pressable, {
            style: [styles.reportTableRow, expanded && styles.reportTableRowActive],
            onPress: () => setExpandedKey(expanded ? "" : key)
          },
            el(ReportCell, { style: styles.reportCellAccount, lines: 2 }, accountTitle(row, t)),
            el(ReportCell, { style: styles.reportCellType }, accountMeta(row, t)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: [styles.reportNumericText, styles.reportDebitText] }, displayBaseAmount(read(row, "totalDebit"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: [styles.reportNumericText, styles.reportCreditText] }, displayBaseAmount(read(row, "totalCredit"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, displayBaseAmount(read(row, "debitBalance"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, displayBaseAmount(read(row, "creditBalance"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, amount(read(row, "counterDebitBalance"), counter)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, amount(read(row, "counterCreditBalance"), counter))
          ),
          expanded && el(View, { style: styles.reportExpanded },
            el(DetailGrid, {
              items: [
                { label: t("accounting.accountId", "Account ID"), value: read(row, "accountId") },
                { label: t("accounting.code", "Code"), value: read(row, "accountCode") },
                { label: t("fields.name", "Name"), value: read(row, "accountName") },
                { label: t("accounting.type", "Type"), value: read(row, "accountTypeKey") },
                { label: t("accounting.baseCurrency", "Base currency"), value: displayContext.code },
                { label: t("accounting.counterCurrency", "Counter currency"), value: counter }
              ]
            })
          )
        );
      }
    })
  );
}

function LedgerView({ report, rows, displayContext }) {
  const { styles, t } = useTheme();
  const [expandedKey, setExpandedKey] = useState("");
  const base = baseCurrency(report);
  const counter = counterCurrency(report);
  const headers = [
    { label: t("accounting.date", "Date"), style: styles.reportCellDate },
    { label: t("accounting.reference", "Reference"), style: styles.reportCellReference },
    { label: t("accounting.description", "Description"), style: styles.reportCellDescription },
    { label: t("accounting.original", "Original"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.debit", "Debit"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.credit", "Credit"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.running", "Running"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.counterCurrency", "Counter currency"), style: styles.reportCellMoney, numeric: true }
  ];

  if (!report) {
    return el(Panel, { title: t("accounting.ledgerEntries", "Ledger Entries") },
      el(EmptyState, { text: t("accounting.chooseLedger", "Choose an account to load its ledger.") })
    );
  }

  return el(Panel, { title: accountTitle(report, t) },
    el(ReportTable, {
      headers,
      rows,
      minWidth: 1040,
      emptyText: t("accounting.noLedgerRows", "No ledger entries returned for this period."),
      renderRow: (row, index) => {
        const referenceType = read(row, "referenceType");
        const referenceId = read(row, "referenceId");
        const reference = `${referenceType || ""}${referenceId ? ` #${referenceId}` : ""}`.trim();
        const key = `ledger-${read(row, "journalEntryId") || index}-${index}`;
        const expanded = expandedKey === key;

        return el(React.Fragment, { key },
          el(Pressable, {
            style: [styles.reportTableRow, expanded && styles.reportTableRowActive],
            onPress: () => setExpandedKey(expanded ? "" : key)
          },
            el(ReportCell, { style: styles.reportCellDate }, shortDate(read(row, "entryDate"))),
            el(ReportCell, { style: styles.reportCellReference }, reference || t("accounting.manual", "Manual")),
            el(ReportCell, { style: styles.reportCellDescription, lines: 2 }, read(row, "description") || t("accounting.journalMovement", "Journal movement")),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, amount(read(row, "originalAmount"), read(row, "currencyCode") || base)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: [styles.reportNumericText, styles.reportDebitText] }, displayBaseAmount(read(row, "debit"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: [styles.reportNumericText, styles.reportCreditText] }, displayBaseAmount(read(row, "credit"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, displayBaseAmount(read(row, "runningBalance"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, amount(read(row, "runningCounterBalance"), counter))
          ),
          expanded && el(View, { style: styles.reportExpanded },
            el(DetailGrid, {
              items: [
                { label: t("accounting.journalEntry", "Journal entry"), value: read(row, "journalEntryId") },
                { label: t("accounting.referenceType", "Reference type"), value: referenceType },
                { label: t("accounting.referenceId", "Reference ID"), value: referenceId },
                { label: t("accounting.currency", "Currency"), value: read(row, "currencyCode") || base },
                { label: t("accounting.counterDebit", "Counter debit"), value: amount(read(row, "counterDebit"), counter) },
                { label: t("accounting.counterCredit", "Counter credit"), value: amount(read(row, "counterCredit"), counter) }
              ]
            })
          )
        );
      }
    })
  );
}

function StatementView({ report, rows, displayContext }) {
  const { styles, t } = useTheme();
  const [expandedKey, setExpandedKey] = useState("");
  const base = baseCurrency(report);
  const counter = counterCurrency(report);
  const headers = [
    { label: t("accounting.date", "Date"), style: styles.reportCellDate },
    { label: t("accounting.document", "Document"), style: styles.reportCellReference },
    { label: t("accounting.activity", "Activity"), style: styles.reportCellType },
    { label: t("accounting.description", "Description"), style: styles.reportCellDescription },
    { label: t("accounting.original", "Original"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.debit", "Debit"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.credit", "Credit"), style: styles.reportCellMoney, numeric: true },
    { label: t("accounting.running", "Running"), style: styles.reportCellMoney, numeric: true }
  ];

  if (!report) {
    return el(Panel, { title: t("accounting.statementEntries", "Statement Entries") },
      el(EmptyState, { text: t("accounting.chooseStatement", "Choose a statement party or linked account.") })
    );
  }

  return el(Panel, { title: read(report, "subjectName") || accountTitle(report, t) },
    el(ReportTable, {
      headers,
      rows,
      minWidth: 1060,
      emptyText: t("accounting.noStatementRows", "No statement entries returned for this period."),
      renderRow: (row, index) => {
        const documentNumber = read(row, "documentNumber");
        const activityType = read(row, "activityType", "referenceType");
        const key = `statement-${read(row, "journalEntryId") || index}-${index}`;
        const expanded = expandedKey === key;

        return el(React.Fragment, { key },
          el(Pressable, {
            style: [styles.reportTableRow, expanded && styles.reportTableRowActive],
            onPress: () => setExpandedKey(expanded ? "" : key)
          },
            el(ReportCell, { style: styles.reportCellDate }, shortDate(read(row, "entryDate"))),
            el(ReportCell, { style: styles.reportCellReference }, documentNumber || read(row, "referenceId")),
            el(ReportCell, { style: styles.reportCellType }, activityType || t("accounting.movementLabel", "Movement")),
            el(ReportCell, { style: styles.reportCellDescription, lines: 2 }, read(row, "description") || t("accounting.accountMovement", "Account movement")),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, amount(read(row, "originalAmount"), read(row, "currencyCode") || base)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: [styles.reportNumericText, styles.reportDebitText] }, displayBaseAmount(read(row, "debit"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: [styles.reportNumericText, styles.reportCreditText] }, displayBaseAmount(read(row, "credit"), displayContext)),
            el(ReportCell, { style: styles.reportCellMoney, textStyle: styles.reportNumericText }, displayBaseAmount(read(row, "runningBalance"), displayContext))
          ),
          expanded && el(View, { style: styles.reportExpanded },
            el(DetailGrid, {
              items: [
                { label: t("accounting.journalEntry", "Journal entry"), value: read(row, "journalEntryId") },
                { label: t("accounting.reference", "Reference"), value: read(row, "referenceType") },
                { label: t("accounting.referenceId", "Reference ID"), value: read(row, "referenceId") },
                { label: t("accounting.currency", "Currency"), value: read(row, "currencyCode") || base },
                { label: t("accounting.counterDebit", "Counter debit"), value: amount(read(row, "counterDebit"), counter) },
                { label: t("accounting.counterCredit", "Counter credit"), value: amount(read(row, "counterCredit"), counter) },
                { label: t("accounting.counterRunning", "Counter running"), value: amount(read(row, "runningCounterBalance"), counter) }
              ]
            })
          )
        );
      }
    })
  );
}

function AccountingScreen({ api }) {
  const { styles, t } = useTheme();
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
  const [isReferenceLoading, setIsReferenceLoading] = useState(false);
  const [isReportLoading, setIsReportLoading] = useState(false);
  const [isExporting, setIsExporting] = useState(false);

  const currentReport = activeTab === "ledger" ? ledger : activeTab === "statement" ? statement : trialBalance;
  const rawRows = useMemo(() => reportRows(activeTab, currentReport), [activeTab, currentReport]);
  const rows = useMemo(() => filterRows(rawRows, activeTab, search), [activeTab, rawRows, search]);
  const trialAccounts = useMemo(() => nestedRows(trialBalance, "rows"), [trialBalance]);
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
  const activeReportKey = `${activeTab}:${activeTab === "ledger" ? selectedAccountId : activeTab === "statement" ? selectedPartyKey || selectedAccountId : ""}`;

  const loadReferenceData = useCallback(async () => {
    setIsReferenceLoading(true);

    try {
      const [accountResult, partyResult, appConstantsResult, currenciesResult] = await Promise.allSettled([
        service.getAccounts(),
        service.getStatementParties(),
        api.get("/api/appconstants"),
        api.get("/api/currencies")
      ]);

      if (accountResult.status === "fulfilled") {
        const nextAccounts = toArray(accountResult.value);
        setAccounts(nextAccounts);
        setSelectedAccountId((current) =>
          current || accountId(nextAccounts[0]) || ""
        );
      }

      if (partyResult.status === "fulfilled") {
        const nextParties = toArray(partyResult.value);
        setParties(nextParties);
        setSelectedPartyKey((current) =>
          current || partyKey(nextParties[0]) || ""
        );
      }
      if (appConstantsResult.status === "fulfilled") setAppConstants(toArray(appConstantsResult.value));
      if (currenciesResult.status === "fulfilled") setCurrencyRates(toArray(currenciesResult.value));

      const errors = [accountResult, partyResult, appConstantsResult, currenciesResult]
        .filter((result) => result.status === "rejected")
        .map((result) => result.reason && result.reason.message)
        .filter(Boolean);
      if (errors.length) setStatus(errors[0]);
    } finally {
      setIsReferenceLoading(false);
    }
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

    setIsReportLoading(true);
    const activeLabel = reportTitle(activeTab, t);
    setStatus(t("accounting.loadingReport", `Loading ${activeLabel.toLowerCase()}...`, { label: activeLabel.toLowerCase() }));

    try {
      if (activeTab === "trial") {
        const report = await service.getTrialBalance(dateFrom, dateTo);
        setTrialBalance(report);
        setStatus(t("accounting.trialLoaded", "Trial balance loaded."));
      } else if (activeTab === "ledger") {
        const report = await service.getLedger(selectedAccountId, dateFrom, dateTo);
        setLedger(report);
        setStatus(t("accounting.ledgerLoaded", "Ledger loaded."));
      } else if (selectedParty) {
        const report = await service.getStatementOfAccountForParty(
          read(selectedParty, "partyType"),
          read(selectedParty, "partyId"),
          dateFrom,
          dateTo
        );
        setStatement(report);
        setStatus(t("accounting.statementLoaded", "Statement of account loaded."));
      } else {
        const report = await service.getStatementOfAccount(selectedAccountId, dateFrom, dateTo);
        setStatement(report);
        setStatus(t("accounting.statementLoaded", "Statement of account loaded."));
      }
    } catch (error) {
      setStatus(error.message || t("accounting.loadError", "Could not load accounting report."));
    } finally {
      setIsReportLoading(false);
    }
  }, [activeTab, dateFrom, dateTo, selectedAccountId, selectedParty, selectedPartyKey, service, t]);

  const exportPdf = useCallback(async () => {
    if (!currentReport) {
      setStatus(t("accounting.loadBeforeExport", "Load a report before exporting PDF."));
      return;
    }

    setIsExporting(true);
    setStatus(t("accounting.creatingPdf", "Creating PDF report..."));

    try {
      const html = buildPdfHtml({ activeTab, report: currentReport, dateFrom, dateTo, t, displayContext: currentDisplayContext });
      const { uri } = await Print.printToFileAsync({
        html,
        width: 842,
        height: 595
      });
      const canShare = await Sharing.isAvailableAsync();

      if (canShare) {
        await Sharing.shareAsync(uri, {
          dialogTitle: `${t("accounting.exportPdf", "Export PDF")} ${reportTitle(activeTab, t)}`,
          mimeType: "application/pdf",
          UTI: "com.adobe.pdf"
        });
      } else {
        await Print.printAsync({ html });
      }

      setStatus(t("accounting.pdfReady", "PDF report ready."));
    } catch (error) {
      setStatus(error.message || t("accounting.exportError", "Could not export PDF report."));
    } finally {
      setIsExporting(false);
    }
  }, [activeTab, currentDisplayContext, currentReport, dateFrom, dateTo, t]);

  useEffect(() => { loadReferenceData(); }, [loadReferenceData]);
  useEffect(() => { loadActiveReport(); }, [activeReportKey, service]);
  useEffect(() => { setSearch(""); }, [activeTab]);

  useEffect(() => {
    if (!selectedAccountId && selectableAccounts.length > 0) {
      setSelectedAccountId(accountId(selectableAccounts[0]));
    }
  }, [selectableAccounts, selectedAccountId]);

  const renderReport = () => {
    if (activeTab === "ledger") return el(LedgerView, { report: ledger, rows, displayContext: currentDisplayContext });
    if (activeTab === "statement") return el(StatementView, { report: statement, rows, displayContext: currentDisplayContext });
    return el(TrialBalanceView, { report: trialBalance, rows, displayContext: currentDisplayContext });
  };

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: t("accounting.title", "Accounting Review"),
      actionTitle: t("common.refresh", "Refresh"),
      onAction: loadActiveReport,
      loading: isReferenceLoading || isReportLoading
    }),
    el(ReportTabs, { activeTab, onChange: setActiveTab }),
    el(ReportHero, {
      activeTab,
      report: currentReport,
      dateFrom,
      dateTo,
      rowCount: rawRows.length,
      displayContext: currentDisplayContext
    }),
    currentReport && el(SummaryStrip, { items: summaryForReport(activeTab, currentReport, t, currentDisplayContext) }),
    el(DateFilters, {
      dateFrom,
      dateTo,
      onDateFromChange: setDateFrom,
      onDateToChange: setDateTo
    }),
    activeTab === "ledger" && el(ChipSelector, {
      title: t("accounting.ledgerAccount", "Ledger Account"),
      rows: selectableAccounts,
      selectedKey: selectedAccountId,
      onSelect: setSelectedAccountId,
      getKey: accountId,
      getTitle: (row) => accountTitle(row, t),
      getMeta: (row) => accountMeta(row, t),
      emptyText: t("accounting.noAccounts", "No accounts are available yet.")
    }),
    activeTab === "statement" && el(ChipSelector, {
      title: t("accounting.statementParty", "Statement Party"),
      rows: parties,
      selectedKey: selectedPartyKey,
      onSelect: setSelectedPartyKey,
      getKey: partyKey,
      getTitle: (row) => partyTitle(row, t),
      getMeta: (row) => partyMeta(row, t),
      emptyText: t("accounting.noParties", "No statement parties are linked yet.")
    }),
    el(ReportTools, {
      search,
      onSearch: setSearch,
      onRefresh: loadActiveReport,
      onExport: exportPdf,
      loading: isReportLoading,
      exporting: isExporting
    }),
    el(StatusText, { value: status }),
    el(View, { style: styles.adminCrudPanel }, renderReport())
  );
}

module.exports = { AccountingScreen };
