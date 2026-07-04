const AsyncStorage = require("@react-native-async-storage/async-storage").default;
const { telemetry } = require("./telemetry");

// Offline write path per Report 05 §7 ("Offline & poor connectivity at the shop counter"):
// - Writes enqueue locally with an optimistic receipt (marked "pending sync").
// - On reconnect the queue drains IN ORDER; the server remains authoritative — a server
//   rejection surfaces as a visible queue item instead of a silent loss.
// - Nothing auto-retries invisibly past 3 attempts without surfacing ("needs attention").
// - Visible state, no mystery: subscribers get the pending count for a connectivity chip,
//   and the daily high-water mark feeds the sync_queue_depth telemetry event (Report 05 §8).

const queueStorageKey = "spareparts.mobile.offline.writeQueue";
const maxAutoAttempts = 3;

function isNetworkError(error) {
  // ApiError carries an HTTP status when the server actually responded; a plain
  // fetch/TypeError without a status means the request never reached the server.
  return !error || typeof error.status !== "number" || error.status === 0;
}

class OfflineWriteQueue {
  constructor(storage, telemetryClient) {
    this.storage = storage;
    this.telemetry = telemetryClient;
    this.listeners = new Set();
    this.isDraining = false;
  }

  subscribe(listener) {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  }

  async notify() {
    const items = await this.list();
    this.listeners.forEach((listener) => {
      try {
        listener(items);
      } catch {
        // A broken listener must not break the queue.
      }
    });
  }

  async list() {
    try {
      const raw = await this.storage.getItem(queueStorageKey);
      const parsed = raw ? JSON.parse(raw) : [];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  async saveItems(items) {
    await this.storage.setItem(queueStorageKey, JSON.stringify(items));
    if (this.telemetry) {
      try {
        await this.telemetry.recordSyncQueueDepth(items.length);
      } catch {
        // Telemetry must never break the write path.
      }
    }
    await this.notify();
    return items;
  }

  async pendingCount() {
    const items = await this.list();
    return items.length;
  }

  async enqueue({ kind, label, path, payload }) {
    const items = await this.list();
    const item = {
      id: `w-${Date.now()}-${Math.floor(Math.random() * 100000)}`,
      kind: kind || "write",
      label: label || path,
      path,
      payload,
      attempts: 0,
      status: "pending",
      queuedAt: new Date().toISOString(),
      lastError: null
    };
    items.push(item);
    await this.saveItems(items);
    return item;
  }

  // Optimistic write: try the server first; on a NETWORK failure enqueue for later sync.
  // A server rejection (HTTP error) is re-thrown — the server stays authoritative and the
  // caller surfaces the validation error instead of hiding it in the queue.
  async sendOrEnqueue(api, { kind, label, path, payload }) {
    try {
      const response = await api.post(path, payload);
      return { delivered: true, response };
    } catch (error) {
      if (!isNetworkError(error)) {
        throw error;
      }
      const item = await this.enqueue({ kind, label, path, payload });
      return { delivered: false, item };
    }
  }

  // Drain in order. Stops at the first still-failing item so ordering is preserved.
  async drain(api) {
    if (this.isDraining) return { drained: 0, remaining: await this.pendingCount() };
    this.isDraining = true;
    let drained = 0;

    try {
      let items = await this.list();
      while (items.length > 0) {
        const item = items[0];
        if (item.status === "attention") {
          break;
        }

        try {
          await api.post(item.path, item.payload);
          items = items.slice(1);
          drained += 1;
          await this.saveItems(items);
        } catch (error) {
          const attempts = Number(item.attempts || 0) + 1;
          const updated = {
            ...item,
            attempts,
            lastError: error && error.message ? String(error.message) : "Sync failed",
            status: attempts >= maxAutoAttempts || !isNetworkError(error) ? "attention" : "pending"
          };
          items = [updated, ...items.slice(1)];
          await this.saveItems(items);
          break;
        }
      }

      return { drained, remaining: items.length };
    } finally {
      this.isDraining = false;
    }
  }

  // Manual retry for an item that exhausted its automatic attempts.
  async retry(api, itemId) {
    const items = await this.list();
    const reset = items.map((item) =>
      item.id === itemId ? { ...item, attempts: 0, status: "pending", lastError: null } : item
    );
    await this.saveItems(reset);
    return this.drain(api);
  }

  async remove(itemId) {
    const items = await this.list();
    await this.saveItems(items.filter((item) => item.id !== itemId));
  }
}

const offlineWriteQueue = new OfflineWriteQueue(AsyncStorage, telemetry);

module.exports = { OfflineWriteQueue, offlineWriteQueue };
