const { defaultApiBaseUrl } = require("./app-config");
const { normalizeLanguageKey, normalizeThemeKey } = require("./formatters");

class MobileSessionStore {
  constructor(storage, keys) {
    this.storage = storage;
    this.keys = keys;
  }

  async load() {
    const [apiBaseUrl, token, user, theme, language] = await Promise.all([
      this.storage.getItem(this.keys.apiBaseUrl),
      this.storage.getItem(this.keys.token),
      this.storage.getItem(this.keys.user),
      this.storage.getItem(this.keys.theme),
      this.storage.getItem(this.keys.language)
    ]);

    return {
      apiBaseUrl: apiBaseUrl || defaultApiBaseUrl,
      token: token || "",
      user: user ? JSON.parse(user) : null,
      themeKey: normalizeThemeKey(theme),
      languageKey: normalizeLanguageKey(language)
    };
  }

  async save(apiBaseUrl, session) {
    await this.storage.multiSet([
      [this.keys.apiBaseUrl, apiBaseUrl],
      [this.keys.token, session.token],
      [this.keys.user, JSON.stringify(session)]
    ]);
  }

  async clear() {
    await this.storage.multiRemove([this.keys.token, this.keys.user]);
  }

  async saveTheme(themeKey) {
    await this.storage.setItem(this.keys.theme, normalizeThemeKey(themeKey));
  }

  async saveLanguage(languageKey) {
    await this.storage.setItem(this.keys.language, normalizeLanguageKey(languageKey));
  }
}

module.exports = { MobileSessionStore };
