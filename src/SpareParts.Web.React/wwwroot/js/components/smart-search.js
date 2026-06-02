import { h, useCallback, useEffect, useMemo, useRef, useState } from "../core/react-runtime.js";

const RECENT_SEARCHES_KEY = "sp_recent_searches";
const MAX_RECENT = 5;
const FALLBACK_TERMS = ["brake", "P-004", "civic", "invoice"];

function loadRecentSearches() {
  try {
    const raw = window.localStorage.getItem(RECENT_SEARCHES_KEY);
    const parsed = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function saveRecentSearch(term) {
  const trimmed = term.trim();
  if (!trimmed) return;
  const existing = loadRecentSearches().filter((s) => s !== trimmed);
  const updated = [trimmed, ...existing].slice(0, MAX_RECENT);
  try {
    window.localStorage.setItem(RECENT_SEARCHES_KEY, JSON.stringify(updated));
  } catch {
    // localStorage unavailable — skip silently
  }
}

function clearRecentSearches() {
  try {
    window.localStorage.removeItem(RECENT_SEARCHES_KEY);
  } catch {
    // ignore
  }
}

function read(row, ...keys) {
  for (const key of keys) {
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") return value;
  }
  return "";
}

function resultKey(result, index) {
  return `${read(result, "entityType")}-${read(result, "id") || index}`;
}

function normalizeTargetView(targetView) {
  return targetView === "parts" ? "inventory" : targetView;
}

function isAuthorizationError(error) {
  const message = String(error?.message || "");
  return error?.status === 401 ||
    error?.status === 403 ||
    /unauthori[sz]ed|forbidden|session expired/i.test(message);
}

export function SmartSearch({ api, onNavigate, t }) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [status, setStatus] = useState("");
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [recentSearches, setRecentSearches] = useState(loadRecentSearches);
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
      const response = await api.get(`/api/search?q=${encodeURIComponent(trimmed)}&limit=24`);
      if (requestId !== latestRequest.current) return;
      const nextResults = Array.isArray(response?.results) ? response.results : [];
      setResults(nextResults);
      setStatus(nextResults.length
        ? t("search.count", "{count} match(es)", { count: nextResults.length })
        : t("search.none", "No matches yet."));
    } catch (error) {
      if (requestId !== latestRequest.current) return;
      setResults([]);
      if (isAuthorizationError(error)) {
        if (typeof api?.onUnauthorized === "function") {
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
    const handle = window.setTimeout(() => runSearch(query), 220);
    return () => window.clearTimeout(handle);
  }, [query, runSearch]);

  const openResult = useCallback((result) => {
    const targetView = normalizeTargetView(read(result, "targetView"));
    const title = read(result, "title");
    if (title) {
      saveRecentSearch(title);
      setRecentSearches(loadRecentSearches());
    }
    if (targetView) onNavigate(targetView);
    setIsOpen(false);
    setStatus(title);
  }, [onNavigate]);

  const handleKeyDown = useCallback((event) => {
    if (event.key === "Escape") {
      setIsOpen(false);
      return;
    }

    if (event.key === "Enter" && results.length > 0) {
      event.preventDefault();
      openResult(results[0]);
    }
  }, [openResult, results]);

  return h("section", { className: "smart-search-shell" },
    h("div", { className: isOpen ? "smart-search active" : "smart-search" },
      h("div", { className: "smart-search-input-row" },
        h("span", { className: "smart-search-mark" }, "S"),
        h("input", {
          value: query,
          onChange: (event) => {
            setQuery(event.target.value);
            setIsOpen(true);
          },
          onFocus: () => setIsOpen(true),
          onKeyDown: handleKeyDown,
          placeholder: t("search.placeholder", "Search part, barcode, customer, invoice, used car...")
        }),
        h("span", { className: "smart-search-status" }, isLoading ? t("common.loading", "Loading") : status)
      ),
      isOpen && h("div", { className: "smart-search-popover" },
        query.trim().length < 2 && h("div", { className: "smart-search-empty" },
          h("strong", null, t("search.ready", "Start with anything in your hand.")),
          h("span", null, t("search.readyHint", "Barcode, OEM, phone number, invoice number, supplier, or used car."))
        ),
        query.trim().length < 2 && h("div", { className: "smart-search-chips" },
          recentSearches.length > 0
            ? h("div", { className: "smart-search-recents" },
                h("span", { className: "smart-search-recents-label" }, t("search.recent", "Recent:")),
                recentSearches.map((term) =>
                  h("button", {
                    key: term,
                    type: "button",
                    onClick: () => {
                      setQuery(term);
                      setIsOpen(true);
                    }
                  }, term)
                ),
                h("button", {
                  type: "button",
                  className: "smart-search-clear-recents",
                  onClick: () => {
                    clearRecentSearches();
                    setRecentSearches([]);
                  }
                }, t("search.clearRecent", "Clear"))
              )
            : FALLBACK_TERMS.map((term) =>
                h("button", {
                  key: term,
                  type: "button",
                  onClick: () => {
                    setQuery(term);
                    setIsOpen(true);
                  }
                }, term)
              )
        ),
        grouped.map(([section, rows]) =>
          h("div", { className: "smart-search-group", key: section },
            h("span", { className: "smart-search-group-title" }, section),
            rows.map((result, index) =>
              h("button", {
                className: "smart-search-result",
                type: "button",
                key: resultKey(result, index),
                onClick: () => openResult(result)
              },
                h("div", null,
                  h("strong", null, read(result, "title") || t("search.untitled", "Untitled")),
                  h("span", null, read(result, "subtitle") || read(result, "entityType"))
                ),
                h("b", null, read(result, "badge") || read(result, "actionLabel"))
              )
            )
          )
        )
      )
    )
  );
}
