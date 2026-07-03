/**
 * Thin wrapper around expo-secure-store, giving the rest of the app a small
 * per-key async storage surface (getItemAsync/setItemAsync/deleteItemAsync)
 * that mirrors what MobileSessionStore needs for sensitive values (JWT token,
 * user profile).
 *
 * expo-secure-store persists values in the iOS Keychain / Android Keystore
 * (encrypted at rest), unlike AsyncStorage which is plaintext on-device
 * storage. Sensitive session data (auth token, user profile) must go through
 * this module instead of AsyncStorage.
 *
 * NOTE: `expo-secure-store` must be installed (npm install / expo install)
 * before this module will work at runtime. See package.json dependencies
 * and the mobile team report for the required install step.
 */
let SecureStore = null;
try {
  // Wrapped in try/catch so the app does not hard-crash on bundling if the
  // native module has not been installed yet (see report: install pending).
  SecureStore = require("expo-secure-store");
} catch {
  SecureStore = null;
}

const isSecureStoreAvailable = Boolean(SecureStore && SecureStore.setItemAsync);

class SecureSessionStorage {
  async getItem(key) {
    if (!isSecureStoreAvailable) return null;
    return SecureStore.getItemAsync(key);
  }

  async setItem(key, value) {
    if (!isSecureStoreAvailable) return;
    if (value === null || value === undefined) {
      await this.removeItem(key);
      return;
    }
    await SecureStore.setItemAsync(key, String(value));
  }

  async removeItem(key) {
    if (!isSecureStoreAvailable) return;
    await SecureStore.deleteItemAsync(key);
  }
}

module.exports = { SecureSessionStorage, isSecureStoreAvailable };
