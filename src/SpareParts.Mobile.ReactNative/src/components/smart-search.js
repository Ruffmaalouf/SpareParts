const React = require("react");
const { Pressable, ScrollView, Text, TextInput, View } = require("react-native");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useRef, useState } = React;
const el = React.createElement;
const quickTerms = ["P-004", "brake", "invoice", "civic"];

function read(row, ...keys) {
  for (const key of keys) {
    const value = row && row[key];
    if (value !== undefined && value !== null && value !== "") return value;
  }
  return "";
}

function resultKey(result, index) {
  return `${read(result, "entityType")}-${read(result, "id") || index}`;
}

function isAuthorizationError(error) {
  const message = String(error && error.message ? error.message : "");
  return error && (error.status === 401 || error.status === 403) ||
    /unauthori[sz]ed|forbidden|session expired/i.test(message);
}

function SmartSearch({ api, onNavigate }) {
  const { styles, palette, t } = useTheme();
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [status, setStatus] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const latestRequest = useRef(0);

  const grouped = useMemo(() => {
    const groups = new Map();
    results.forEach((result) => {
      const section = read(result, "section") || t("search.results", "Results");
      if (!groups.has(section)) groups.set(section, []);
      groups.get(section).push(result);
    });
    return Array.from(groups.entries());
  }, [results, t]);

  const runSearch = useCallback(async (term) => {
    const trimmed = term.trim();
    if (trimmed.length < 2) {
      setResults([]);
      setStatus("");
      setIsLoading(false);
      return;
    }

    const requestId = latestRequest.current + 1;
    latestRequest.current = requestId;
    setIsLoading(true);
    setStatus(t("search.searching", "Searching..."));
    try {
      const response = await api.get(`/api/search?q=${encodeURIComponent(trimmed)}&limit=20`);
      if (requestId !== latestRequest.current) return;
      const nextResults = Array.isArray(response && response.results) ? response.results : [];
      setResults(nextResults);
      setStatus(nextResults.length
        ? t("search.count", "{count} match(es)", { count: nextResults.length })
        : t("search.none", "No matches yet."));
    } catch (error) {
      if (requestId !== latestRequest.current) return;
      setResults([]);
      if (isAuthorizationError(error)) {
        if (api && typeof api.onUnauthorized === "function") {
          api.onUnauthorized();
        }
        setStatus(t("search.sessionExpired", "Session expired. Sign in again."));
        return;
      }
      setStatus(error.message || t("search.error", "Search failed."));
    } finally {
      if (requestId === latestRequest.current) setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => {
    const handle = setTimeout(() => runSearch(query), 240);
    return () => clearTimeout(handle);
  }, [query, runSearch]);

  const openResult = useCallback((result) => {
    const targetView = read(result, "targetView");
    if (targetView) onNavigate(targetView);
    setIsOpen(false);
    setStatus(read(result, "title"));
  }, [onNavigate]);

  return el(View, { style: styles.smartSearchShell },
    el(View, { style: styles.smartSearchInputRow },
      el(View, { style: styles.smartSearchMark }, el(Text, { style: styles.smartSearchMarkText }, "S")),
      el(TextInput, {
        style: styles.smartSearchInput,
        value: query,
        onFocus: () => setIsOpen(true),
        onChangeText: (value) => {
          setQuery(value);
          setIsOpen(true);
        },
        placeholder: t("search.placeholder", "Search part, barcode, customer, invoice, used car..."),
        placeholderTextColor: palette.soft,
        autoCapitalize: "none",
        returnKeyType: "search",
        onSubmitEditing: () => {
          if (results.length > 0) openResult(results[0]);
        }
      }),
      Boolean(status || isLoading) && el(Text, { style: styles.smartSearchStatus, numberOfLines: 1 },
        isLoading ? t("common.loading", "Loading") : status
      )
    ),
    isOpen && el(View, { style: styles.smartSearchPanel },
      query.trim().length < 2 && el(View, { style: styles.smartSearchEmpty },
        el(Text, { style: styles.smartSearchEmptyTitle }, t("search.ready", "Start with anything in your hand.")),
        el(Text, { style: styles.smartSearchEmptyText }, t("search.readyHint", "Barcode, OEM, phone, invoice, supplier, or used car."))
      ),
      query.trim().length < 2 && el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.smartSearchChipRail
      },
        quickTerms.map((term) =>
          el(Pressable, {
            key: term,
            style: styles.smartSearchChip,
            onPress: () => {
              setQuery(term);
              setIsOpen(true);
            }
          }, el(Text, { style: styles.smartSearchChipText }, term))
        )
      ),
      grouped.length > 0 && el(ScrollView, {
        style: styles.smartSearchResultsScroll,
        nestedScrollEnabled: true,
        showsVerticalScrollIndicator: true,
        contentContainerStyle: styles.smartSearchResultsContent
      },
        grouped.map(([section, rows]) =>
          el(View, { key: section, style: styles.smartSearchGroup },
            el(Text, { style: styles.smartSearchGroupTitle }, section),
            rows.map((result, index) =>
              el(Pressable, {
                key: resultKey(result, index),
                style: styles.smartSearchResult,
                onPress: () => openResult(result)
              },
                el(View, { style: styles.smartSearchResultCopy },
                  el(Text, { style: styles.smartSearchResultTitle, numberOfLines: 1 }, read(result, "title") || t("search.untitled", "Untitled")),
                  el(Text, { style: styles.smartSearchResultSubtitle, numberOfLines: 1 }, read(result, "subtitle") || read(result, "entityType"))
                ),
                el(Text, { style: styles.smartSearchResultBadge, numberOfLines: 1 }, read(result, "badge") || read(result, "actionLabel"))
              )
            )
          )
        )
      )
    )
  );
}

module.exports = { SmartSearch };
