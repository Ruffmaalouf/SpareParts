export class ScreenRegistry {
  constructor(items) {
    this.items = items;
    this.components = new Map(items.map((item) => [item.key, item.component]));
  }

  resolve(key) {
    return this.components.get(key) || this.components.get(this.items[0].key);
  }
}
