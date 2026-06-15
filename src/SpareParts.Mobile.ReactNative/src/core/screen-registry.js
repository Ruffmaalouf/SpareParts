class ScreenRegistry {
  constructor(items) {
    this.items = items;
    this.componentMap = new Map(items.map((item) => [item.key, item.component]));
    this.aliases = new Map([
      ["inventory", "parts"],
      ["parts", "inventory"]
    ]);
  }

  resolve(key) {
    const normalizedKey = this.componentMap.has(key) ? key : this.aliases.get(key);
    return this.componentMap.get(normalizedKey) || this.componentMap.get(this.items[0].key);
  }
}

module.exports = { ScreenRegistry };
