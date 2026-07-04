const AsyncStorage = require("@react-native-async-storage/async-storage").default;

// Minimal client-side telemetry per Report 05 §8 ("lightweight client-side beacons").
// The mobile app has no telemetry transport today and Report 04 documents no telemetry
// endpoint, so events are buffered locally (bounded ring) where a future flush transport
// can pick them up; in development they are also logged to the console.
//
// sync_queue_depth (Report 05 §8 table): "Offline write queue high-water mark, daily" —
// recorded once per day whenever the pending offline-write count sets a new daily high.

const eventsStorageKey = "spareparts.mobile.telemetry.events";
const syncQueueHighWaterStorageKey = "spareparts.mobile.telemetry.syncQueueHighWater";
const maxBufferedEvents = 200;

function isDevBuild() {
  return typeof __DEV__ !== "undefined" && __DEV__;
}

class MobileTelemetry {
  constructor(storage) {
    this.storage = storage;
  }

  async readEvents() {
    try {
      const raw = await this.storage.getItem(eventsStorageKey);
      const parsed = raw ? JSON.parse(raw) : [];
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  async record(eventName, payload = {}) {
    const event = {
      event: String(eventName || ""),
      ts: new Date().toISOString(),
      ...payload
    };

    try {
      const events = await this.readEvents();
      events.push(event);
      const bounded = events.slice(-maxBufferedEvents);
      await this.storage.setItem(eventsStorageKey, JSON.stringify(bounded));
    } catch {
      // Telemetry must never break the app; drop the event on storage failure.
    }

    if (isDevBuild()) {
      // eslint-disable-next-line no-console
      console.log("[telemetry]", event.event, event);
    }

    return event;
  }

  async recordSyncQueueDepth(depth) {
    const normalizedDepth = Math.max(0, Number(depth) || 0);
    const today = new Date().toISOString().slice(0, 10);

    let highWater = null;
    try {
      const raw = await this.storage.getItem(syncQueueHighWaterStorageKey);
      highWater = raw ? JSON.parse(raw) : null;
    } catch {
      highWater = null;
    }

    const isNewDay = !highWater || highWater.date !== today;
    const isNewHigh = isNewDay || normalizedDepth > Number(highWater.depth || 0);
    if (!isNewHigh) {
      return null;
    }

    try {
      await this.storage.setItem(
        syncQueueHighWaterStorageKey,
        JSON.stringify({ date: today, depth: normalizedDepth })
      );
    } catch {
      // Ignore storage failure; the event below still records the observation.
    }

    return this.record("sync_queue_depth", { depth: normalizedDepth, date: today });
  }
}

const telemetry = new MobileTelemetry(AsyncStorage);

module.exports = { MobileTelemetry, telemetry };
