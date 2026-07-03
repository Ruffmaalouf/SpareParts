import { React, h, startTransition, useCallback, useEffect, useMemo, useRef, useState } from "./core/react-runtime.js";
import { defaultApiBaseUrl, storageKeys } from "./core/config.js";
import { ApiClient } from "./core/api-client.js";
import { createTranslator, isRtlLanguage } from "./core/i18n.js";
import { applyWebTheme, money, normalizeBaseUrl } from "./core/formatters.js";
import { BrowserLanguageStore, BrowserSessionStore, BrowserThemeStore } from "./core/stores.js";
import { LanguageSegment, ThemeRail } from "./components/layout.js";
import { NotificationCenter } from "./components/shared.js";
import { LockedFeatureModal } from "./components/locked-feature-modal.js";
import { SmartSearch } from "./components/smart-search.js";
import { createPartNotificationClient } from "./services/notification-service.js";
import { CustomerStorefrontView } from "./views/storefront-view.js";
import { PartPassportView } from "./views/part-passport-view.js";
import { screenRegistry } from "./views/screen-registry.js";
import { LazyScreen } from "./components/LazyScreen.js";
import { AppShell } from "./components/ui/AppShell.js";
import { Sidebar } from "./components/ui/Sidebar.js";
import { Topbar } from "./components/ui/Topbar.js";

const sessionStore = new BrowserSessionStore(window.localStorage, storageKeys);
const themeStore = new BrowserThemeStore(window.localStorage, storageKeys.theme);
const languageStore = new BrowserLanguageStore(window.localStorage, storageKeys.language);
const webAppRoleId = 4;

function useApi(apiBaseUrl, token, onUnauthorized, onPlanLocked) {
  return useMemo(() => new ApiClient(apiBaseUrl, token, onUnauthorized, onPlanLocked), [apiBaseUrl, onUnauthorized, onPlanLocked, token]);
}

export function App() {
  const [apiBaseUrl, setApiBaseUrl] = useState(() => sessionStore.loadApiBaseUrl() || defaultApiBaseUrl);
  const [token, setToken] = useState(() => sessionStore.loadToken());
  const [user, setUser] = useState(() => sessionStore.loadUser());
  const [view, setView] = useState("dashboard");
  const [themeKey, setThemeKey] = useState(() => themeStore.load());
  const [languageKey, setLanguageKey] = useState(() => languageStore.load());
  const [notifications, setNotifications] = useState([]);
  const [planLock, setPlanLock] = useState(null);
  const recentPartNotifications = useRef(new Map());
  const clearSession = useCallback(() => {
    sessionStore.clear();
    setToken("");
    setUser(null);
  }, []);
  const handlePlanLocked = useCallback((error) => {
    setPlanLock({
      errorCode: error.errorCode,
      message: error.message,
      currentPlan: error.currentPlan,
      requiredPlans: error.requiredPlans,
      upgradeUrl: error.upgradeUrl
    });
  }, []);
  const api = useApi(apiBaseUrl, token, clearSession, handlePlanLocked);
  const t = useMemo(() => createTranslator(languageKey), [languageKey]);
  const isRtl = isRtlLanguage(languageKey);

  const dismissPlanLock = useCallback(() => setPlanLock(null), []);
  const goToBilling = useCallback(() => {
    setPlanLock(null);
    startTransition(() => setView("billing"));
  }, []);

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

  const pushReservationReminder = useCallback((reminder) => {
    const requestId = String(reminder?.partRequestId || "");
    const id = `${requestId || "reservation"}-${Date.now()}`;
    const partName = reminder?.requestedPartName || "Reserved part";
    const customerName = reminder?.customerName || "Customer";

    setNotifications((current) => [
      {
        id,
        title: "Reservation needs attention",
        message: `${customerName} - ${partName}`
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
      onPartAdded: pushPartAddedNotification,
      onReservationReminder: pushReservationReminder
    });

    if (!client) return undefined;

    client.start().catch(() => {});
    return () => {
      client.stop().catch(() => {});
    };
  }, [apiBaseUrl, pushPartAddedNotification, pushReservationReminder, token, user]);

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

  const isSignedIn = Boolean(token && user);
  const isWebAppUser = isSignedIn && Number(user.roleId ?? user.RoleId) === webAppRoleId;

  if (window.location.pathname.toLowerCase().startsWith("/passport/")) {
    return h(PartPassportView);
  }

  if (!isSignedIn || isWebAppUser) {
    return h(React.Fragment, null,
      h(CustomerStorefrontView, {
        api,
        user,
        initialApiBaseUrl: apiBaseUrl,
        themeKey,
        languageKey,
        onTheme: setThemeKey,
        onLanguage: setLanguageKey,
        onLogin: handleLogin,
        onLogout: logout,
        t
      }),
      h(NotificationCenter, { notifications, onDismiss: dismissNotification })
    );
  }

  const activeScreenMeta = screenRegistry.items.find((screen) => screen.key === view);

  return h(React.Fragment, null,
    h(AppShell, {
      sidebar: ({ closeNav }) => h(Sidebar, {
        screens: screenRegistry.items,
        user,
        view,
        onLogout: logout,
        onView: switchView,
        closeNav,
        t
      }),
      topbar: ({ onMenuToggle }) => h(Topbar, {
        onMenuToggle,
        user,
        onNewSale: () => switchView("invoices"),
        notificationsCount: notifications.length,
        languageKey,
        onLanguage: setLanguageKey,
        search: h(SmartSearch, {
          api,
          onNavigate: switchView,
          t
        })
      })
    },
      h(LazyScreen, {
        registry: screenRegistry,
        screenKey: view,
        screenProps: {
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
        }
      })
    ),
    h(NotificationCenter, { notifications, onDismiss: dismissNotification }),
    h(LockedFeatureModal, { lock: planLock, onClose: dismissPlanLock, onUpgrade: goToBilling, t })
  );
}
