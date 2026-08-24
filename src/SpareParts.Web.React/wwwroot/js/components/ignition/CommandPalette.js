import { h, useCallback, useEffect, useMemo, useRef, useState } from "../../core/react-runtime.js";
import { asRows } from "../../core/formatters.js";
import { Icon } from "../ui/CockpitIcons.js";
import {
  loadRecents,
  matchClients,
  matchParts,
  matchScreens,
  parseCommand,
  pushRecent,
  splitAmount
} from "../../services/command-grammar.js";
import { trackPaletteUsed } from "../../services/telemetry-service.js";
import { MoneyText } from "./MoneyText.js";

const DEBOUNCE_MS = 180; // mirrors the existing SearchBar.js debounce pattern

// Module-level caches so the palette answers <50ms on repeat opens
// (Report 05 §6 latency budget) — the parts index is the same versioned
// cache idea the report describes; clients hydrate from the first search.
let partsCache = { rows: [], loadedAt: 0 };
let clientsCache = { rows: [], loadedAt: 0 };
const CACHE_TTL_MS = 5 * 60 * 1000;

async function ensurePartsIndex(api) {
  if (partsCache.rows.length && Date.now() - partsCache.loadedAt < CACHE_TTL_MS) return partsCache.rows;
  try {
    const rows = asRows(await api.list("/api/parts"));
    partsCache = { rows, loadedAt: Date.now() };
  } catch { /* keep the stale cache */ }
  return partsCache.rows;
}

async function ensureClientsIndex(api) {
  if (clientsCache.rows.length && Date.now() - clientsCache.loadedAt < CACHE_TTL_MS) return clientsCache.rows;
  try {
    const rows = asRows(await api.get("/api/customers?page=1&pageSize=100"));
    clientsCache = { rows, loadedAt: Date.now() };
  } catch { /* keep the stale cache */ }
  return clientsCache.rows;
}

function clientOption(client, action, verb) {
  return {
    kind: "client",
    id: `client-${verb}-${client.id}`,
    icon: "users",
    title: client.name,
    subtitle: client.phone || client.email || "",
    trailing: action.trailing,
    run: action.run
  };
}

// Ignition CommandPalette (Report 03 §03 + Report 05 §6). A Modal-family
// overlay (same backdrop/Escape conventions as ui/Modal.js) with the terse
// verb grammar: inv / pay / part / q / c / cust + / wa / #NNNN / > screen.
// `part` answers inline (stock/price card) without navigating; Enter always
// executes the top match; ↑/↓ move the selection (aria-activedescendant).
// `onAction(action)` receives {type, ...context} commands the host view
// executes (navigate / open-workspace / payment-sheet / draft-invoice ...).
export function CommandPalette({ open, onClose, api, screens = [], onAction }) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState([]);
  const [inlineCard, setInlineCard] = useState(null);
  const [selectedIndex, setSelectedIndex] = useState(0);
  const [loading, setLoading] = useState(false);
  const inputRef = useRef(null);
  const debounceRef = useRef(null);
  const triggerRef = useRef(null);

  // Focus management: remember the trigger, focus the input on open,
  // return focus on close (Report 03 §09).
  useEffect(() => {
    if (open) {
      triggerRef.current = document.activeElement;
      setQuery("");
      setInlineCard(null);
      setSelectedIndex(0);
      setTimeout(() => inputRef.current && inputRef.current.focus(), 0);
      ensurePartsIndex(api);
      ensureClientsIndex(api);
    } else if (triggerRef.current && typeof triggerRef.current.focus === "function") {
      triggerRef.current.focus();
      triggerRef.current = null;
    }
  }, [api, open]);

  const emit = useCallback((verb, resultKind, action) => {
    trackPaletteUsed(verb, resultKind);
    if (typeof onAction === "function") onAction(action);
    if (typeof onClose === "function") onClose();
  }, [onAction, onClose]);

  const buildResults = useCallback(async (rawQuery) => {
    const { verb, args } = parseCommand(rawQuery);
    setInlineCard(null);

    // Empty query — recents + verb hints (Report 05 §6 empty state).
    if (!verb) {
      const recents = loadRecents();
      const groups = [];
      if ((recents.client || []).length) {
        groups.push({
          group: "Recent clients",
          items: (recents.client || []).map((client) => ({
            kind: "client",
            id: `recent-client-${client.id}`,
            icon: "users",
            title: client.name,
            subtitle: "Open workspace",
            run: () => emit("c", "client", { type: "open-workspace", clientId: client.id, clientName: client.name })
          }))
        });
      }
      if ((recents.part || []).length) {
        groups.push({
          group: "Recent parts",
          items: (recents.part || []).map((part) => ({
            kind: "part",
            id: `recent-part-${part.id}`,
            icon: "box",
            title: part.name,
            subtitle: "Show stock & price",
            run: () => setInlineCard(partsCache.rows.find((row) => String(row.id) === String(part.id)) || part)
          }))
        });
      }
      return groups;
    }

    if (verb === ">") {
      return [{
        group: "Screens",
        items: matchScreens(args, screens).map((screen) => ({
          kind: "screen",
          id: `screen-${screen.key}`,
          icon: "grid",
          title: screen.label,
          subtitle: `> ${screen.key}`,
          run: () => emit(">", "screen", { type: "navigate", view: screen.key })
        }))
      }];
    }

    if (verb === "#") {
      const number = args.trim();
      return [{
        group: "Documents",
        items: number ? [{
          kind: "document",
          id: `doc-${number}`,
          icon: "doc",
          title: `Open document #${number}`,
          subtitle: "Invoice, quote, or request number",
          run: () => emit("#", "document", { type: "open-document", number })
        }] : []
      }];
    }

    if (verb === "part") {
      const parts = await ensurePartsIndex(api);
      return [{
        group: "Parts — inline answer",
        items: matchParts(args, parts).map((part) => ({
          kind: "part",
          id: `part-${part.id}`,
          icon: "box",
          title: part.name || "Part",
          subtitle: [part.oemCode || part.partNumber, part.sku].filter(Boolean).join(" · "),
          trailing: `${Number(part.availableQuantity || 0)} in stock`,
          run: () => {
            pushRecent("part", { id: part.id, name: part.name });
            trackPaletteUsed("part", "inline-card");
            setInlineCard(part); // inline — no navigation (T6 in 2 steps)
          }
        }))
      }];
    }

    if (verb === "cust") {
      const name = args.trim();
      return [{
        group: "Clients",
        items: [{
          kind: "client-create",
          id: "cust-create",
          icon: "plus",
          title: name ? `Create client “${name}”` : "Create a new client",
          subtitle: "cust + <name>",
          run: () => emit("cust", "client-create", { type: "create-client", name })
        }]
      }];
    }

    if (["inv", "pay", "q", "c", "wa"].includes(verb)) {
      const { term, amount } = verb === "pay" ? splitAmount(args) : { term: args, amount: null };
      const clients = await ensureClientsIndex(api);
      const matches = matchClients(term, clients);
      const verbMeta = {
        inv: { label: "New invoice", type: "draft-invoice" },
        pay: { label: amount ? `Receive payment (${amount})` : "Receive payment", type: "payment-sheet" },
        q: { label: "New quote", type: "draft-quote" },
        c: { label: "Open workspace", type: "open-workspace" },
        wa: { label: "Message thread", type: "open-messages" }
      }[verb];

      const items = matches.map((client) =>
        clientOption(client, {
          trailing: verbMeta.label,
          run: () => {
            pushRecent("client", { id: client.id, name: client.name });
            emit(verb, verbMeta.type, {
              type: verbMeta.type,
              clientId: client.id,
              clientName: client.name,
              amount
            });
          }
        }, verb));

      if (verb === "inv" && !term) {
        items.unshift({
          kind: "client",
          id: "inv-walkin",
          icon: "cart",
          title: "Walk-in sale",
          subtitle: "No client attached",
          trailing: "New invoice",
          run: () => emit("inv", "draft-invoice", { type: "draft-invoice", clientId: null, clientName: null })
        });
      }
      return [{ group: "Clients", items }];
    }

    // Free search: screens + clients + parts.
    const [clients, parts] = await Promise.all([ensureClientsIndex(api), ensurePartsIndex(api)]);
    return [
      {
        group: "Screens",
        items: matchScreens(args, screens, 4).map((screen) => ({
          kind: "screen",
          id: `screen-${screen.key}`,
          icon: "grid",
          title: screen.label,
          run: () => emit("search", "screen", { type: "navigate", view: screen.key })
        }))
      },
      {
        group: "Clients",
        items: matchClients(args, clients, 4).map((client) =>
          clientOption(client, {
            trailing: "Open workspace",
            run: () => {
              pushRecent("client", { id: client.id, name: client.name });
              emit("search", "client", { type: "open-workspace", clientId: client.id, clientName: client.name });
            }
          }, "search"))
      },
      {
        group: "Parts",
        items: matchParts(args, parts, 4).map((part) => ({
          kind: "part",
          id: `part-${part.id}`,
          icon: "box",
          title: part.name || "Part",
          trailing: `${Number(part.availableQuantity || 0)} in stock`,
          run: () => setInlineCard(part)
        }))
      }
    ].filter((group) => group.items.length);
  }, [api, emit, screens]);

  // Debounced query -> results (180ms, Report 03 §03).
  useEffect(() => {
    if (!open) return undefined;
    setLoading(true);
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(async () => {
      try {
        const groups = await buildResults(query);
        setResults(groups);
        setSelectedIndex(0);
      } catch (error) {
        setResults([{
          group: "Error",
          items: [{ kind: "error", id: "error", icon: "warn", title: error.message || "Search failed.", run: () => {} }]
        }]);
      } finally {
        setLoading(false);
      }
    }, query ? DEBOUNCE_MS : 0);
    return () => clearTimeout(debounceRef.current);
  }, [buildResults, open, query]);

  const flatItems = useMemo(() => results.flatMap((group) => group.items), [results]);

  const handleKeyDown = (event) => {
    if (event.key === "Escape") {
      event.preventDefault();
      onClose && onClose();
    } else if (event.key === "ArrowDown") {
      event.preventDefault();
      setSelectedIndex((index) => Math.min(flatItems.length - 1, index + 1));
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      setSelectedIndex((index) => Math.max(0, index - 1));
    } else if (event.key === "Enter") {
      event.preventDefault();
      const item = flatItems[selectedIndex] || flatItems[0];
      if (item) item.run();
    }
  };

  if (!open) return null;

  let flatIndex = -1;
  const activeId = flatItems[selectedIndex] ? `ig-option-${flatItems[selectedIndex].id}` : undefined;

  return h("div", {
    className: "ig-palette-backdrop",
    onClick: (event) => { if (event.target === event.currentTarget && onClose) onClose(); }
  },
    h("div", { className: "ig-palette", role: "dialog", "aria-modal": "true", "aria-label": "Command palette" },
      h("input", {
        ref: inputRef,
        className: "ig-palette-input",
        placeholder: "inv eli · pay eli 250 · part n54 coil · c eli · #8811 · > screen",
        value: query,
        role: "combobox",
        "aria-expanded": "true",
        "aria-controls": "ig-palette-listbox",
        "aria-activedescendant": activeId,
        onChange: (event) => setQuery(event.target.value),
        onKeyDown: handleKeyDown
      }),
      inlineCard && h("div", { className: "ig-palette-card" },
        h("div", { className: "ig-panel-head" },
          h("h3", null, inlineCard.name || "Part"),
          h("span", { className: `ig-pill ${Number(inlineCard.availableQuantity || 0) > 0 ? "ig-pill-success" : "ig-pill-danger"}` },
            `${Number(inlineCard.availableQuantity || 0)} in stock`)),
        h("div", { className: "ig-cluster", style: { marginTop: "8px" } },
          h("span", { className: "ig-label" }, "Price"),
          h(MoneyText, { value: inlineCard.salePrice, size: "lg" }),
          (inlineCard.oemCode || inlineCard.partNumber) && h("span", { className: "ig-muted" }, `OEM ${inlineCard.oemCode || inlineCard.partNumber}`))),
      h("div", { className: "ig-palette-results", id: "ig-palette-listbox", role: "listbox" },
        loading && !results.length
          ? h("div", { className: "ig-stack", style: { padding: "12px" }, "aria-hidden": "true" },
            [0, 1, 2].map((index) => h("span", { key: index, className: "ig-skeleton", style: { height: "32px" } })))
          : results.length
            ? results.map((group) =>
              h("div", { key: group.group },
                h("div", { className: "ig-palette-group ig-label" }, group.group),
                group.items.map((item) => {
                  flatIndex += 1;
                  const isSelected = flatIndex === selectedIndex;
                  return h("button", {
                    key: item.id,
                    id: `ig-option-${item.id}`,
                    type: "button",
                    role: "option",
                    "aria-selected": isSelected ? "true" : "false",
                    className: isSelected ? "ig-palette-option is-selected" : "ig-palette-option",
                    onClick: item.run
                  },
                    h(Icon, { name: item.icon || "grid", size: 14 }),
                    h("span", null,
                      h("div", null, item.title),
                      item.subtitle && h("div", { className: "ig-muted", style: { fontSize: "11px" } }, item.subtitle)),
                    item.trailing && h("span", { className: "ig-muted" }, item.trailing));
                })))
            : h("div", { className: "ig-palette-empty" }, `No matches for “${query}”.`)),
      h("div", { className: "ig-palette-hint" },
        "Verbs: inv · pay · part · q · c · cust + · wa · #NNNN · > screen — Enter runs the top match")
    )
  );
}
