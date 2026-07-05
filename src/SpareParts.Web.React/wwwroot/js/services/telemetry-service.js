// Ignition telemetry beacons — Report 05 §8. No telemetry pattern existed in
// the codebase, so this is the minimal beacon module the rollout plan calls
// for: lightweight client-side events ({event, tenantId, role, taskId, ts})
// buffered in memory and flushed via navigator.sendBeacon (keepalive fetch
// fallback). Every emitter is fail-silent — telemetry must never break a
// screen or block the UI thread.
//
// Event families (Report 05 §8): task_start / task_complete, screen_view,
// palette_used, queue_item_shown / acted / resolved / dismissed,
// reminder_previewed / edited / sent, sync_queue_depth.

const ENDPOINT = "/api/telemetry/events";
const FLUSH_INTERVAL_MS = 15000;
const MAX_BUFFER = 40;

let buffer = [];
let context = { apiBaseUrl: "", tenantId: null, role: null };
let flushTimer = null;
const taskStarts = new Map();

function nowIso() {
  return new Date().toISOString();
}

function flush() {
  if (!buffer.length) return;
  const events = buffer;
  buffer = [];
  try {
    const url = `${context.apiBaseUrl || ""}${ENDPOINT}`;
    const payload = JSON.stringify({ events });
    const sent = typeof navigator !== "undefined" && navigator.sendBeacon
      ? navigator.sendBeacon(url, new Blob([payload], { type: "application/json" }))
      : false;
    if (!sent && typeof fetch === "function") {
      fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: payload,
        keepalive: true
      }).catch(() => {});
    }
  } catch {
    // Telemetry is best-effort by design.
  }
}

function scheduleFlush() {
  if (flushTimer) return;
  flushTimer = setTimeout(() => {
    flushTimer = null;
    flush();
  }, FLUSH_INTERVAL_MS);
}

// Wire tenant/role once per session (called from the Ignition views on mount).
export function configureTelemetry({ apiBaseUrl, tenantId, role } = {}) {
  context = {
    apiBaseUrl: apiBaseUrl || context.apiBaseUrl,
    tenantId: tenantId ?? context.tenantId,
    role: role ?? context.role
  };
}

export function track(event, data = {}) {
  try {
    buffer.push({
      event,
      tenantId: context.tenantId,
      role: context.role,
      ts: nowIso(),
      ...data
    });
    if (buffer.length >= MAX_BUFFER) {
      flush();
    } else {
      scheduleFlush();
    }
  } catch {
    // Never throw from telemetry.
  }
}

// task_start/task_complete pairs — taskId per Report 05 (T1–T10).
export function trackTaskStart(taskId, data = {}) {
  taskStarts.set(taskId, Date.now());
  track("task_start", { taskId, ...data });
}

export function trackTaskComplete(taskId, data = {}) {
  const started = taskStarts.get(taskId);
  taskStarts.delete(taskId);
  track("task_complete", {
    taskId,
    durationMs: started ? Date.now() - started : null,
    ...data
  });
}

// screen_view with source (sidebar / palette / deep-link).
export function trackScreenView(screenKey, source = "sidebar") {
  track("screen_view", { screenKey, source });
}

// palette_used with verb + result kind.
export function trackPaletteUsed(verb, resultKind) {
  track("palette_used", { verb, resultKind });
}

// queue_item_shown / acted / resolved / dismissed lifecycle transitions.
export function trackQueueItem(transition, itemKey, data = {}) {
  track(`queue_item_${transition}`, { itemKey, ...data });
}

// reminder_previewed / edited / sent.
export function trackReminder(stage, customerId, data = {}) {
  track(`reminder_${stage}`, { customerId, ...data });
}

if (typeof document !== "undefined") {
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "hidden") flush();
  });
}
