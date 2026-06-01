import { defaultApiBaseUrl, defaultLanguageKey, languageOptions } from "./config.js";
import { normalizeThemeKey } from "./formatters.js";

export class BrowserSessionStore {
  constructor(storage, keys) {
    this.storage = storage;
    this.keys = keys;
  }

  loadApiBaseUrl() {
    return this.storage.getItem(this.keys.apiBaseUrl) || defaultApiBaseUrl;
  }

  loadToken() {
    return this.storage.getItem(this.keys.token) || "";
  }

  loadUser() {
    const stored = this.storage.getItem(this.keys.user);
    return stored ? JSON.parse(stored) : null;
  }

  save(apiBaseUrl, session) {
    this.storage.setItem(this.keys.apiBaseUrl, apiBaseUrl);
    this.storage.setItem(this.keys.token, session.token);
    this.storage.setItem(this.keys.user, JSON.stringify(session));
  }

  clear() {
    this.storage.removeItem(this.keys.token);
    this.storage.removeItem(this.keys.user);
  }
}

export class BrowserThemeStore {
  constructor(storage, key) {
    this.storage = storage;
    this.key = key;
  }

  load() {
    return normalizeThemeKey(this.storage.getItem(this.key));
  }

  save(themeKey) {
    this.storage.setItem(this.key, normalizeThemeKey(themeKey));
  }
}

export class BrowserLanguageStore {
  constructor(storage, key) {
    this.storage = storage;
    this.key = key;
  }

  load() {
    const value = this.storage.getItem(this.key);
    return languageOptions.some((language) => language.key === value) ? value : defaultLanguageKey;
  }

  save(languageKey) {
    const normalized = languageOptions.some((language) => language.key === languageKey)
      ? languageKey
      : defaultLanguageKey;
    this.storage.setItem(this.key, normalized);
  }
}
