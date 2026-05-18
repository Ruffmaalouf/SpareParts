import { React, h, startTransition, useCallback, useEffect, useMemo, useRef, useState } from "./core/react-runtime.js";
import { defaultApiBaseUrl, storageKeys, webAppRoleName } from "./core/config.js";
import { ApiClient } from "./core/api-client.js";
import { createTranslator, isRtlLanguage } from "./core/i18n.js";
import { applyWebTheme, money, normalizeBaseUrl } from "./core/formatters.js";
import { BrowserLanguageStore, BrowserSessionStore, BrowserThemeStore } from "./core/stores.js";
import { LoginScreen } from "./components/auth.js";
import { LanguageSegment, Sidebar, ThemeRail } from "./components/layout.js";
import { NotificationCenter } from "./components/shared.js";
import { SmartSearch } from "./components/smart-search.js";
import { createPartNotificationClient } from "./services/notification-service.js";
import { CustomerStorefrontView } from "./views/storefront-view.js";
import { screenRegistry } from "./views/screen-registry.js";

const sessionStore = new BrowserSessionStore(window.localStorage, storageKeys);
const themeStore = new BrowserThemeStore(window.localStorage, storageKeys.theme);
const languageStore = new BrowserLanguageStore(window.localStorage, storageKeys.language);
const webAppRoleId = 4;

function useApi(apiBaseUrl, token, onUnauthorized) {
  return useMemo(() => new ApiClient(apiBaseUrl, token, onUnauthorized), [apiBaseUrl, onUnauthorized, token]);
}

export function App() {
  const [apiBaseUrl, setApiBaseUrl] = useState(() => sessionStore.loadApiBaseUrl() || defaultApiBaseUrl);
  const [token, setToken] = useState(() => sessionStore.loadToken());
  const [user, setUser] = useState(() => sessionStore.loadUser());
  const [view, setView] = useState("dashboard");
  const [themeKey, setThemeKey] = useState(() => themeStore.load());
  const [languageKey, setLanguageKey] = useState(() => languageStore.load());
  const [notifications, setNotifications] = useState([]);
  const recentPartNotifications = useRef(new Map());
  const clearSession = useCallback(() => {
    sessionStore.clear();
    setToken("");
    setUser(null);
  }, []);
  const api = useApi(apiBaseUrl, token, clearSession);
  const t = useMemo(() => createTranslator(languageKey), [languageKey]);
  const isRtl = isRtlLanguage(languageKey);

  const dismissNotification = useCallback((id) => {
    setNotifications((current) => current.filter((notification) => notification.id !== id));
  }, []);

  const pushPartAddedNotification = useCallback((part) => {
    const partId = String(part?.id || "");
    const now = Date.now();
    const lastSeen = partId ? recentPartNotifications.current.get(partId) : 0;
    if (lastSeen && now - lastSeen < 30000) return;
    if (partId) recentPartNotifications.current.set(partId, now);

    const name = part?.name || "New part";
    const price = money(part?.salePrice, part?.currency || "USD");
    const id = `${partId || "part"}-${now}`;
    setNotifications((current) => [
      {
        id,
        title: "New part added",
        message: `${name} - ${price}`
      },
      ...current
    ].slice(0, 4));
  }, []);

  useEffect(() => {
    applyWebTheme(themeKey);
    themeStore.save(themeKey);
  }, [themeKey]);

  useEffect(() => {
    languageStore.save(languageKey);
    document.documentElement.lang = languageKey;
    document.documentElement.dir = isRtl ? "rtl" : "ltr";
  }, [isRtl, languageKey]);

  useEffect(() => {
    if (!token || !user) return undefined;

    const client = createPartNotificationClient({
      apiBaseUrl,
      token,
      onPartAdded: pushPartAddedNotification
    });

    if (!client) return undefined;

    client.start().catch(() => {});
    return () => {
      client.stop().catch(() => {});
    };
  }, [apiBaseUrl, pushPartAddedNotification, token, user]);

  const handleLogin = useCallback((nextApiBaseUrl, response) => {
    const normalized = normalizeBaseUrl(nextApiBaseUrl);
    sessionStore.save(normalized, response);
    setApiBaseUrl(normalized);
    setToken(response.token);
    setUser(response);
  }, []);

  const logout = useCallback(() => {
    clearSession();
  }, [clearSession]);

  const switchView = useCallback((nextView) => {
    startTransition(() => setView(nextView));
  }, []);

  if (!token || !user) {
    return h(LoginScreen, {
      initialApiBaseUrl: apiBaseUrl,
      languageKey,
      onLanguage: setLanguageKey,
      t,
      onLogin: handleLogin
    });
  }

  const ActiveScreen = screenRegistry.resolve(view);
  const isWebAppUser = Number(user.roleId ?? user.RoleId) === webAppRoleId || user.role === webAppRoleName;

  if (isWebAppUser) {
    return h(React.Fragment, null,
      h(CustomerStorefrontView, {
        api,
        user,
        themeKey,
        languageKey,
        onTheme: setThemeKey,
        onLanguage: setLanguageKey,
        onLogout: logout,
        t
      }),
      h(NotificationCenter, { notifications, onDismiss: dismissNotification })
    );
  }

  return h("div", { className: "app-shell" },
    h(Sidebar, {
      screens: screenRegistry.items,
      user,
      view,
      onLogout: logout,
      onView: switchView,
      t
    }),
    h("main", { className: "workspace" },
      h("header", { className: "admin-topbar" },
        h("div", { className: "admin-search-zone" },
          h(SmartSearch, {
            api,
            onNavigate: switchView,
            t
          })
        ),
        h("div", { className: "admin-topbar-controls" },
          h(ThemeRail, { value: themeKey, onChange: setThemeKey, t }),
          h(LanguageSegment, { value: languageKey, onChange: setLanguageKey, t })
        )
      ),
      h("section", { className: "workspace-content" },
        h(ActiveScreen, {
          api,
          activeView: view,
          languageKey,
          themeKey,
          user,
          onLanguage: setLanguageKey,
          onLogout: logout,
          onTheme: setThemeKey,
          onView: switchView,
          t
        })
      )
    ),
    h(NotificationCenter, { notifications, onDismiss: dismissNotification })
  );
}
