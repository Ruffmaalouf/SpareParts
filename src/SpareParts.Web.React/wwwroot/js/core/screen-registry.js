export class ScreenRegistry {
  // `items` entries are `{ key, label, load }` where `load` is a zero-arg
  // function returning a Promise<Component> (backed by a dynamic import()).
  // The loader is not invoked until a screen is actually opened, so signing
  // in no longer downloads all ~70 view modules up front - only the active
  // screen (plus whatever the user has already visited this session, via
  // the resolved-component cache below).
  constructor(items) {
    this.items = items;
    this.loaders = new Map(items.map((item) => [item.key, item.load]));
    this.resolved = new Map();
    this.pending = new Map();
    this.aliases = new Map([
      ["parts", "inventory"],
      ["inventory", "parts"]
    ]);
  }

  normalizeKey(key) {
    return this.loaders.has(key) ? key : this.aliases.get(key);
  }

  // Returns a cached component synchronously if it has already been loaded
  // this session, otherwise null. Callers use this to skip the loading
  // fallback on repeat visits to a screen.
  peek(key) {
    const normalizedKey = this.normalizeKey(key) || this.items[0].key;
    return this.resolved.get(normalizedKey) || null;
  }

  // Resolves (and caches) the component for `key`, loading its module lazily
  // on first use. Falls back to the first registered screen if the key is
  // unknown, matching the previous synchronous resolve() behavior.
  async resolveAsync(key) {
    const normalizedKey = this.normalizeKey(key) || this.items[0].key;

    if (this.resolved.has(normalizedKey)) {
      return this.resolved.get(normalizedKey);
    }

    if (this.pending.has(normalizedKey)) {
      return this.pending.get(normalizedKey);
    }

    const loader = this.loaders.get(normalizedKey);
    const promise = Promise.resolve(loader ? loader() : null).then((component) => {
      this.resolved.set(normalizedKey, component);
      this.pending.delete(normalizedKey);
      return component;
    });

    this.pending.set(normalizedKey, promise);
    return promise;
  }
}
