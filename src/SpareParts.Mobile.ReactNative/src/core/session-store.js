const { defaultApiBaseUrl, shouldUsePackagedApiBaseUrl } = require("./app-config");
const { normalizeLanguageKey, normalizeThemeKey } = require("./formatters");

/**
 * MobileSessionStore persists the mobile session across two backing stores:
 *
 *  - `secureStorage`: sensitive values only (JWT token, user profile). Must
 *    be backed by expo-secure-store (Keychain/Keystore encrypted storage),
 *    never AsyncStorage. Exposes per-key getItem/setItem/removeItem, all
 *    async (SecureStore has no multiSet/multiGet, unlike AsyncStorage).
 *  - `prefsStorage`: non-sensitive preferences (apiBaseUrl, theme,
 *    language). AsyncStorage is fine here since none of these values are
 *    confidential.
 */
class MobileSessionStore {
  constructor(secureStorage, prefsStorage, keys) {
    this.secureStorage = secureStorage;
    this.prefsStorage = prefsStorage;
    this.keys = keys;
  }

  async load() {
    const [apiBaseUrl, token, user, theme, language] = await Promise.all([
      this.prefsStorage.getItem(this.keys.apiBaseUrl),
      this.secureStorage.getItem(this.keys.token),
      this.secureStorage.getItem(this.keys.user),
      this.prefsStorage.getItem(this.keys.theme),
      this.prefsStorage.getItem(this.keys.language)
    ]);

    const shouldResetApiBaseUrl = shouldUsePackagedApiBaseUrl(apiBaseUrl);
    const resolvedApiBaseUrl = shouldResetApiBaseUrl
      ? defaultApiBaseUrl
      : apiBaseUrl || defaultApiBaseUrl;

    if (shouldResetApiBaseUrl) {
      await this.prefsStorage.setItem(this.keys.apiBaseUrl, resolvedApiBaseUrl);
    }

    return {
      apiBaseUrl: resolvedApiBaseUrl,
      token: token || "",
      user: user ? JSON.parse(user) : null,
      themeKey: normalizeThemeKey(theme),
      languageKey: normalizeLanguageKey(language)
    };
  }

  async save(apiBaseUrl, session) {
    await Promise.all([
      this.prefsStorage.setItem(this.keys.apiBaseUrl, apiBaseUrl),
      this.secureStorage.setItem(this.keys.token, session.token),
      this.secureStorage.setItem(this.keys.user, JSON.stringify(session))
    ]);
  }

  async clear() {
    await Promise.all([
      this.secureStorage.removeItem(this.keys.token),
      this.secureStorage.removeItem(this.keys.user)
    ]);
  }

  async saveTheme(themeKey) {
    await this.prefsStorage.setItem(this.keys.theme, normalizeThemeKey(themeKey));
  }

  async saveLanguage(languageKey) {
    await this.prefsStorage.setItem(this.keys.language, normalizeLanguageKey(languageKey));
  }
}

module.exports = { MobileSessionStore };
