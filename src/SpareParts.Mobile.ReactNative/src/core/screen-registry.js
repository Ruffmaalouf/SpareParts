class ScreenRegistry {
  constructor(items) {
    this.items = items;
    this.componentMap = new Map(items.map((item) => [item.key, item.component]));
  }

  resolve(key) {
    return this.componentMap.get(key) || this.componentMap.get(this.items[0].key);
  }
}

module.exports = { ScreenRegistry };
