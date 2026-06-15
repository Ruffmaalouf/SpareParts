const React = require("react");
const {
  ActivityIndicator,
  Pressable,
  Text,
  useWindowDimensions,
  View
} = require("react-native");
const AsyncStorage = require("@react-native-async-storage/async-storage").default;
const { SafeAreaProvider, useSafeAreaInsets } = require("react-native-safe-area-context");
const { StatusBar } = require("expo-status-bar");
const { defaultApiBaseUrl, defaultLanguageKey, defaultThemeKey, storageKeys } = require("./core/app-config");
const { ApiClient } = require("./core/api-client");
const { createTranslator, isRtlLanguage } = require("./core/i18n");
const { MobileSessionStore } = require("./core/session-store");
const { normalizeBaseUrl, normalizeLanguageKey, normalizeThemeKey } = require("./core/formatters");
const { AppSidebar } = require("./components/app-sidebar");
const { BottomTabBar } = require("./components/bottom-tab-bar");
const { LockedFeatureModal } = require("./components/locked-feature-modal");
const { SmartSearch } = require("./components/smart-search");
const { LoginScreen } = require("./screens/login-screen");
const { CustomerStorefrontScreen } = require("./screens/customer-storefront-screen");
const { screenRegistry } = require("./screens/screen-registry");
const { ThemeContext } = require("./theme/theme-context");
const { createPalette, createStyles } = require("./theme/styles");
const { isWebAppUser } = require("./core/role-policy");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

const sessionStore = new MobileSessionStore(AsyncStorage, storageKeys);
const bottomTabKeys = ["dashboard", "invoices", "parts", "management", "settings"];
const customerTabs = [
  { key: "store-home", label: "Garage", icon: "gauge", description: "Cockpit" },
  { key: "store-parts", label: "Parts", icon: "piston", description: "Fitment" },
  { key: "store-cart", label: "Cart", icon: "cart", description: "Staged" },
  { key: "store-checkout", label: "Pay", icon: "flag", description: "Finish" },
  { key: "store-account", label: "Driver", icon: "key", description: "Account" }
];

function AppContent() {
  const { width } = useWindowDimensions();
  const insets = useSafeAreaInsets();
  const isWideLayout = width >= 820;
  const [apiBaseUrl, setApiBaseUrl] = useState(defaultApiBaseUrl);
  const [token, setToken] = useState("");
  const [user, setUser] = useState(null);
  const [activeTab, setActiveTab] = useState("dashboard");
  const [themeKey, setThemeKey] = useState(defaultThemeKey);
  const [languageKey, setLanguageKey] = useState(defaultLanguageKey);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isBooting, setIsBooting] = useState(true);
  const [planLock, setPlanLock] = useState(null);
  const themeBundle = useMemo(() => {
    const nextPalette = createPalette(themeKey);
    const isRtl = isRtlLanguage(languageKey);
    return {
      palette: nextPalette,
      styles: createStyles(nextPalette),
      languageKey,
      isRtl,
      textDirection: isRtl ? "rtl" : "ltr",
      t: createTranslator(languageKey)
    };
  }, [languageKey, themeKey]);
  const { palette, styles, t } = themeBundle;

  useEffect(() => {
    let isMounted = true;
    async function boot() {
      try {
        const storedSession = await sessionStore.load();

        if (!isMounted) return;
        setApiBaseUrl(storedSession.apiBaseUrl);
        setToken(storedSession.token);
        setUser(storedSession.user);
        setThemeKey(storedSession.themeKey);
        setLanguageKey(storedSession.languageKey);
      } finally {
        if (isMounted) setIsBooting(false);
      }
    }

    boot();
    return () => {
      isMounted = false;
    };
  }, []);

  const clearSession = useCallback(() => {
    sessionStore.clear().catch(() => {});
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
  const api = useMemo(() => new ApiClient(apiBaseUrl, token, clearSession, handlePlanLocked), [apiBaseUrl, clearSession, handlePlanLocked, token]);
  const isCustomerMode = Boolean(user && isWebAppUser(user));
  const bottomTabs = useMemo(
    () => bottomTabKeys
      .map((key) => screenRegistry.items.find((item) => item.key === key))
      .filter(Boolean),
    []
  );
  const visibleBottomTabs = isCustomerMode ? customerTabs : bottomTabs;
  const activeScreen = useMemo(() => {
    if (isCustomerMode) {
      return visibleBottomTabs.find((item) => item.key === activeTab) || visibleBottomTabs[0];
    }

    return screenRegistry.items.find((item) => item.key === activeTab) || bottomTabs[0];
  }, [activeTab, bottomTabs, isCustomerMode, visibleBottomTabs]);
  const ActiveScreen = isCustomerMode ? null : screenRegistry.resolve(activeScreen.key);
  const activeBottomKey = useMemo(() => {
    if (visibleBottomTabs.some((item) => item.key === activeScreen.key)) {
      return activeScreen.key;
    }

    return isCustomerMode ? activeScreen.key : "management";
  }, [activeScreen.key, isCustomerMode, visibleBottomTabs]);

  useEffect(() => {
    setIsSidebarOpen(!isCustomerMode && isWideLayout);
  }, [isCustomerMode, isWideLayout]);

  const changeTheme = useCallback(async (nextThemeKey) => {
    const normalized = normalizeThemeKey(nextThemeKey);
    setThemeKey(normalized);
    await sessionStore.saveTheme(normalized);
  }, []);

  const changeLanguage = useCallback(async (nextLanguageKey) => {
    const normalized = normalizeLanguageKey(nextLanguageKey);
    setLanguageKey(normalized);
    await sessionStore.saveLanguage(normalized);
  }, []);

  const toggleSidebar = useCallback(() => {
    setIsSidebarOpen((value) => !value);
  }, []);

  const closeSidebar = useCallback(() => {
    setIsSidebarOpen(false);
  }, []);

  const selectScreen = useCallback((nextTab) => {
    setActiveTab(nextTab);
    if (!isWideLayout) {
      setIsSidebarOpen(false);
    }
  }, [isWideLayout]);

  const dismissPlanLock = useCallback(() => setPlanLock(null), []);

  const goToBilling = useCallback(() => {
    setPlanLock(null);
    selectScreen("billing");
  }, [selectScreen]);

  const handleLogin = useCallback(async (nextApiBaseUrl, loginResponse) => {
    const normalizedUrl = normalizeBaseUrl(nextApiBaseUrl);
    await sessionStore.save(normalizedUrl, loginResponse);
    setApiBaseUrl(normalizedUrl);
    setToken(loginResponse.token);
    setUser(loginResponse);
  }, []);

  const logout = useCallback(async () => {
    clearSession();
  }, [clearSession]);

  let content;
  if (isBooting) {
    content = el(View, { style: [styles.bootScreen, { paddingTop: insets.top, paddingBottom: insets.bottom }] },
      el(StatusBar, { style: "light" }),
      el(ActivityIndicator, { color: palette.accent }),
      el(Text, { style: styles.mutedText }, t("app.starting", "Starting Maalouf Auto Parts"))
    );
  } else if (!token || !user) {
    content = el(LoginScreen, {
      initialApiBaseUrl: apiBaseUrl,
      onLogin: handleLogin
    });
  } else {
    content = el(View, { style: [styles.app, { paddingTop: insets.top, paddingBottom: insets.bottom }] },
      el(StatusBar, { style: "light" }),
      el(View, { style: styles.appFrame },
        !isCustomerMode && isSidebarOpen && !isWideLayout && el(Pressable, { style: styles.navBackdrop, onPress: closeSidebar }),
        !isCustomerMode && isSidebarOpen && el(AppSidebar, {
          activeKey: activeScreen.key,
          isWideLayout,
          screens: screenRegistry.items,
          themeKey,
          user,
          onClose: closeSidebar,
          onLogout: logout,
          onSelect: selectScreen,
          onTheme: changeTheme
        }),
        el(View, { style: styles.contentPane },
          !isCustomerMode && el(SmartSearch, {
            api,
            onNavigate: selectScreen
          }),
          el(View, { style: styles.contentBody },
            isCustomerMode
              ? el(CustomerStorefrontScreen, {
                activeSection: activeScreen.key,
                api,
                embedded: true,
                languageKey,
                onLanguage: changeLanguage,
                onLogout: logout,
                onNavigate: selectScreen,
                onTheme: changeTheme,
                themeKey,
                user
              })
              : el(ActiveScreen, {
                api,
                activeScreenKey: activeScreen.key,
                adminScreens: screenRegistry.items,
                onLogout: logout,
                onNavigate: selectScreen,
                onLanguage: changeLanguage,
                onTheme: changeTheme,
                languageKey,
                themeKey
              })
          ),
          el(BottomTabBar, {
            activeKey: activeBottomKey,
            tabs: visibleBottomTabs,
            onSelect: selectScreen
          })
        )
      ),
      el(LockedFeatureModal, { lock: planLock, onClose: dismissPlanLock, onUpgrade: goToBilling })
    );
  }

  return el(ThemeContext.Provider, { value: themeBundle }, content);
}

function App() {
  return el(SafeAreaProvider, null, el(AppContent));
}

module.exports = App;
module.exports.default = App;
