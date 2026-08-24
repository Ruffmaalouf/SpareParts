// Ignition command-palette grammar — Report 05 §6. Terse 1–4 character
// verbs, fuzzy-matched arguments, inline answers where the result is
// informational. This module is pure parsing + matching over locally cached
// client/part indexes so keystroke-to-result stays <50ms; the CommandPalette
// component owns fetching, debounce, and rendering.
//
// Grammar:
//   inv [client]      new invoice draft (walk-in when no client)
//   pay [client] [n]  payment sheet, amount pre-filled
//   part <term>       inline stock/price card — no navigation
//   q [client]        new quote draft
//   c [client]        open the client's workspace
//   cust + <name>     inline client create
//   wa [client]       client's message thread
//   #NNNN             invoice/quote/request number lookup
//   > <screen>        navigate to any registered screen by fuzzy name
//   (anything else)   fuzzy screen + client + part search

export const PALETTE_VERBS = ["inv", "pay", "part", "q", "c", "cust", "wa", "#", ">"];

export function parseCommand(input) {
  const raw = String(input || "").trim();
  if (!raw) return { verb: null, args: "" };

  if (raw.startsWith(">")) return { verb: ">", args: raw.slice(1).trim() };
  if (raw.startsWith("#")) return { verb: "#", args: raw.slice(1).trim() };

  const [head, ...rest] = raw.split(/\s+/);
  const verb = head.toLowerCase();

  if (verb === "cust" && rest[0] === "+") {
    return { verb: "cust", args: rest.slice(1).join(" ") };
  }
  if (["inv", "pay", "part", "q", "c", "cust", "wa"].includes(verb)) {
    return { verb, args: rest.join(" ") };
  }
  return { verb: "search", args: raw };
}

// For `pay eli 250` — split the trailing amount off the client term.
export function splitAmount(args) {
  const match = String(args || "").match(/^(.*?)\s+([\d][\d,.]*)$/);
  if (!match) return { term: String(args || "").trim(), amount: null };
  const amount = Number(match[2].replace(/,/g, ""));
  return { term: match[1].trim(), amount: Number.isFinite(amount) ? amount : null };
}

function normalize(value) {
  return String(value || "").toLowerCase();
}

// Cheap fuzzy score: exact prefix > word prefix > substring > subsequence.
export function fuzzyScore(term, candidate) {
  const needle = normalize(term);
  const haystack = normalize(candidate);
  if (!needle) return 1;
  if (!haystack) return 0;
  if (haystack.startsWith(needle)) return 100 - haystack.length * 0.1;
  if (haystack.split(/[\s/-]+/).some((word) => word.startsWith(needle))) return 80 - haystack.length * 0.1;
  if (haystack.includes(needle)) return 60 - haystack.indexOf(needle) * 0.5;
  let index = 0;
  for (const char of needle) {
    index = haystack.indexOf(char, index);
    if (index === -1) return 0;
    index += 1;
  }
  return 20;
}

export function matchClients(term, clients, take = 6) {
  if (!term) return clients.slice(0, take);
  return clients
    .map((client) => ({ client, score: Math.max(fuzzyScore(term, client.name), fuzzyScore(term, client.phone) * 0.8) }))
    .filter((entry) => entry.score > 0)
    .sort((left, right) => right.score - left.score)
    .slice(0, take)
    .map((entry) => entry.client);
}

// Parts match across the identifiers the dashboard already exposes as
// search tabs: name, internal code/sku, OEM code, barcode (barcode-wedge
// scans land here too — Report 05 §6).
export function matchParts(term, parts, take = 5) {
  if (!term) return [];
  return parts
    .map((part) => ({
      part,
      score: Math.max(
        fuzzyScore(term, part.name),
        fuzzyScore(term, part.oemCode || part.partNumber),
        fuzzyScore(term, part.sku),
        fuzzyScore(term, part.barcode)
      )
    }))
    .filter((entry) => entry.score > 0)
    .sort((left, right) => right.score - left.score)
    .slice(0, take)
    .map((entry) => entry.part);
}

export function matchScreens(term, screens, take = 7) {
  if (!term) return screens.slice(0, take);
  return screens
    .map((screen) => ({ screen, score: Math.max(fuzzyScore(term, screen.label), fuzzyScore(term, screen.key)) }))
    .filter((entry) => entry.score > 0)
    .sort((left, right) => right.score - left.score)
    .slice(0, take)
    .map((entry) => entry.screen);
}

// Local-first recents (Report 05 §6): last clients/parts per user, zero API cost.
const RECENTS_KEY = "ignition.paletteRecents";
const RECENTS_MAX = 5;

export function loadRecents() {
  try {
    return JSON.parse(window.localStorage.getItem(RECENTS_KEY) || "{}") || {};
  } catch {
    return {};
  }
}

export function pushRecent(kind, entry) {
  try {
    const recents = loadRecents();
    const list = (recents[kind] || []).filter((item) => String(item.id) !== String(entry.id));
    list.unshift(entry);
    recents[kind] = list.slice(0, RECENTS_MAX);
    window.localStorage.setItem(RECENTS_KEY, JSON.stringify(recents));
  } catch { /* best-effort */ }
}
