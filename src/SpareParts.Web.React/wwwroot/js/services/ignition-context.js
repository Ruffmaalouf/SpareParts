// Ignition shared context — Report 05 §4 (queue lifecycle persistence) and
// §5 (from-context drafts). Two small local-first stores:
//
// 1. Draft context (sessionStorage): when the palette or the Client
//    Workspace starts an invoice/quote/payment "from context", the target
//    screen key + client/entity context is stashed here so the destination
//    flow can pre-attach the client instead of re-asking for it. Legacy
//    views are untouched; they simply ignore the stash until they migrate.
//
// 2. Queue item lifecycle (localStorage): the Action Queue's acted /
//    dismissed states are keyed by ruleKey + entityId so they survive
//    refresh and re-login (Report 05 Fig 5). Dismissals expire after
//    `snoozeDays` so a persisting signal re-queues with age.

const DRAFT_KEY = "ignition.draftContext";
const QUEUE_KEY = "ignition.queueState";
const DAY_MS = 86400000;

export function setDraftContext(context) {
  try {
    window.sessionStorage.setItem(DRAFT_KEY, JSON.stringify({ ...context, createdAt: Date.now() }));
  } catch { /* storage may be unavailable — drafts are best-effort */ }
}

export function takeDraftContext(expectedView) {
  try {
    const raw = window.sessionStorage.getItem(DRAFT_KEY);
    if (!raw) return null;
    const draft = JSON.parse(raw);
    if (expectedView && draft.view !== expectedView) return null;
    window.sessionStorage.removeItem(DRAFT_KEY);
    return draft;
  } catch {
    return null;
  }
}

function loadQueueState() {
  try {
    return JSON.parse(window.localStorage.getItem(QUEUE_KEY) || "{}") || {};
  } catch {
    return {};
  }
}

function saveQueueState(state) {
  try {
    window.localStorage.setItem(QUEUE_KEY, JSON.stringify(state));
  } catch { /* best-effort */ }
}

// Identity = ruleKey + entityId (Report 05 Fig 5).
function queueItemId(ruleKey, entityId) {
  return entityId != null ? `${ruleKey}:${entityId}` : String(ruleKey);
}

export function getQueueItemState(ruleKey, entityId) {
  const state = loadQueueState()[queueItemId(ruleKey, entityId)];
  if (!state) return null;
  if (state.status === "dismissed" && state.until && Date.now() > state.until) {
    // Snooze expired — the item re-queues on the next evaluation.
    clearQueueItemState(ruleKey, entityId);
    return null;
  }
  return state;
}

export function markQueueItemActed(ruleKey, entityId) {
  const state = loadQueueState();
  state[queueItemId(ruleKey, entityId)] = { status: "acted", at: Date.now() };
  saveQueueState(state);
}

export function markQueueItemDismissed(ruleKey, entityId, snoozeDays = 1) {
  const state = loadQueueState();
  state[queueItemId(ruleKey, entityId)] = {
    status: "dismissed",
    at: Date.now(),
    until: Date.now() + snoozeDays * DAY_MS
  };
  saveQueueState(state);
}

export function clearQueueItemState(ruleKey, entityId) {
  const state = loadQueueState();
  delete state[queueItemId(ruleKey, entityId)];
  saveQueueState(state);
}
