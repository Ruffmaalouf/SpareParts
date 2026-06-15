export class ScreenRegistry {
  constructor(items) {
    this.items = items;
    this.components = new Map(items.map((item) => [item.key, item.component]));
    this.aliases = new Map([
      ["parts", "inventory"],
      ["inventory", "parts"]
    ]);
  }

  resolve(key) {
    const normalizedKey = this.components.has(key) ? key : this.aliases.get(key);
    return this.components.get(normalizedKey) || this.components.get(this.items[0].key);
  }
}
