const AsyncStorage = require("@react-native-async-storage/async-storage").default;

// Offline counter notes per Report 05 §7: the counter must keep working when
// connectivity drops, so notes taken against a client are persisted locally first
// and never block on the network. Report 04 defines no server contract for notes
// yet (additive rule — we code strictly against documented endpoints), so notes
// stay device-local with an explicit "on this device" sync state that a future
// notes endpoint can drain through the offline write queue.

const notesStorageKey = "spareparts.mobile.offline.counterNotes";
const maxNotesPerCustomer = 100;

class CounterNotesStore {
  constructor(storage) {
    this.storage = storage;
  }

  async readAll() {
    try {
      const raw = await this.storage.getItem(notesStorageKey);
      const parsed = raw ? JSON.parse(raw) : {};
      return parsed && typeof parsed === "object" ? parsed : {};
    } catch {
      return {};
    }
  }

  async listForCustomer(customerId) {
    const all = await this.readAll();
    const notes = all[String(customerId)] || [];
    return Array.isArray(notes) ? notes : [];
  }

  async add({ customerId, customerName, body }) {
    const trimmed = String(body || "").trim();
    if (!trimmed || customerId == null) return null;

    const note = {
      id: `n-${Date.now()}-${Math.floor(Math.random() * 100000)}`,
      customerId,
      customerName: customerName || "",
      body: trimmed,
      createdAt: new Date().toISOString(),
      syncState: "device"
    };

    const all = await this.readAll();
    const key = String(customerId);
    const notes = Array.isArray(all[key]) ? all[key] : [];
    all[key] = [note, ...notes].slice(0, maxNotesPerCustomer);
    await this.storage.setItem(notesStorageKey, JSON.stringify(all));
    return note;
  }

  async removeNote(customerId, noteId) {
    const all = await this.readAll();
    const key = String(customerId);
    const notes = Array.isArray(all[key]) ? all[key] : [];
    all[key] = notes.filter((note) => note.id !== noteId);
    await this.storage.setItem(notesStorageKey, JSON.stringify(all));
    return all[key];
  }
}

const counterNotesStore = new CounterNotesStore(AsyncStorage);

module.exports = { CounterNotesStore, counterNotesStore };
