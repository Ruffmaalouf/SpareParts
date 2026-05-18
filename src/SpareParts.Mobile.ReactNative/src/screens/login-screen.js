const React = require("react");
const {
  ImageBackground,
  Keyboard,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  Text,
  View
} = require("react-native");
const { StatusBar } = require("expo-status-bar");
const { useSafeAreaInsets } = require("react-native-safe-area-context");
const WebBrowser = require("expo-web-browser");
const Facebook = require("expo-auth-session/providers/facebook");
const Google = require("expo-auth-session/providers/google");
const { ApiClient } = require("../core/api-client");
const {
  facebookAppId,
  googleAndroidClientId,
  googleClientId,
  googleIosClientId,
  googleWebClientId
} = require("../core/app-config");
const { Field, PrimaryButton } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

WebBrowser.maybeCompleteAuthSession();

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;
const fallbackSocialClientId = "spareparts-social-login-not-configured";
const loginHeroImage = require("../../assets/images/login-ignition-hero.png");

function LoginScreen({ initialApiBaseUrl, onLogin }) {
  const { styles, t } = useTheme();
  const insets = useSafeAreaInsets();
  const [apiBaseUrl, setApiBaseUrl] = useState(initialApiBaseUrl);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isKeyboardVisible, setIsKeyboardVisible] = useState(false);
  const api = useMemo(() => new ApiClient(apiBaseUrl, ""), [apiBaseUrl]);

  useEffect(() => {
    const showSubscription = Keyboard.addListener("keyboardDidShow", () => setIsKeyboardVisible(true));
    const hideSubscription = Keyboard.addListener("keyboardDidHide", () => setIsKeyboardVisible(false));

    return () => {
      showSubscription.remove();
      hideSubscription.remove();
    };
  }, []);

  const submit = useCallback(async () => {
    if (!username.trim() || !password) {
      setStatus(t("login.enterCredentials", "Enter username and password."));
      return;
    }

    setIsLoading(true);
    setStatus(t("login.signingIn", "Signing in..."));
    try {
      const response = await api.post("/api/auth/login", { username, password });
      await onLogin(apiBaseUrl, response);
    } catch (error) {
      setStatus(error.message || t("login.failed", "Login failed."));
    } finally {
      setIsLoading(false);
    }
  }, [api, apiBaseUrl, onLogin, password, t, username]);

  const externalLogin = useCallback(async (provider, payload) => {
    const providerLabel = t(`login.providers.${provider}`, provider);
    setIsLoading(true);
    setStatus(t("login.signingInWith", "Signing in with {provider}...", { provider: providerLabel }));
    try {
      const response = await api.post("/api/auth/external-login", {
        provider,
        ...payload
      });
      await onLogin(apiBaseUrl, response);
    } catch (error) {
      setStatus(error.message || t("login.providerFailed", "{provider} login failed.", { provider: providerLabel }));
    } finally {
      setIsLoading(false);
    }
  }, [api, apiBaseUrl, onLogin, t]);

  return el(View, { style: [styles.loginScreen, { paddingTop: insets.top, paddingBottom: insets.bottom }] },
    el(StatusBar, { style: "light" }),
    el(ImageBackground, { source: loginHeroImage, style: styles.loginBackdrop, imageStyle: styles.loginBackdropImage },
      el(View, { style: styles.loginShade },
        el(View, { style: styles.loginWarmGlow }),
        el(View, { style: styles.loginCoolGlow }),
        el(View, { style: styles.loginRoadFade }),
        el(KeyboardAvoidingView, {
          behavior: Platform.OS === "ios" ? "padding" : "height",
          style: styles.loginKeyboard
        },
          el(ScrollView, {
            style: styles.loginScroll,
            contentContainerStyle: [
              styles.loginScrollContent,
              isKeyboardVisible && styles.loginScrollContentKeyboard
            ],
            keyboardDismissMode: "on-drag",
            keyboardShouldPersistTaps: "handled"
          },
            !isKeyboardVisible && el(View, { style: styles.loginPoster },
              el(View, { style: styles.loginFuelStrip },
                el(View, { style: styles.loginFuelStripHot }),
                el(View, { style: styles.loginFuelStripWarm }),
                el(View, { style: styles.loginFuelStripCool })
              ),
              el(View, { style: styles.loginBrandRow },
                el(View, { style: styles.brandMarkLarge }, el(Text, { style: styles.brandMarkLargeText }, "M")),
                el(View, { style: styles.loginBrandCopy },
                  el(Text, { style: styles.loginEyebrow }, t("login.brand", "Maalouf Auto Parts")),
                  el(Text, { style: styles.loginTitle }, t("login.garageAccess", "Garage access")),
                  el(Text, { style: styles.loginSubtitle }, t("login.subtitle", "Parts, sales, stock, and customer work from the bay."))
                )
              ),
              el(View, { style: styles.loginTelemetry },
                el(View, { style: styles.loginTelemetryCell },
                  el(Text, { style: styles.loginTelemetryLabel }, "RPM"),
                  el(Text, { style: styles.loginTelemetryValue }, "7200")
                ),
                el(View, { style: styles.loginTelemetryCell },
                  el(Text, { style: styles.loginTelemetryLabel }, t("login.fuel", "FUEL")),
                  el(Text, { style: styles.loginTelemetryValue }, t("login.ready", "READY"))
                ),
                el(View, { style: styles.loginTelemetryCell },
                  el(Text, { style: styles.loginTelemetryLabel }, t("login.line", "LINE")),
                  el(Text, { style: styles.loginTelemetryValue }, t("login.live", "LIVE"))
                )
              )
            ),
            el(View, { style: [styles.loginPanel, isKeyboardVisible && styles.loginPanelKeyboard] },
              el(View, { style: styles.loginPanelHeader },
                el(View, null,
                  el(Text, { style: styles.loginPanelKicker }, t("login.ignition", "Ignition")),
                  el(Text, { style: styles.loginPanelTitle }, t("login.startShift", "Start the shift"))
                ),
                el(View, { style: styles.loginPanelBadge },
                  el(Text, { style: styles.loginPanelBadgeText }, "OPS")
                )
              ),
              el(Field, {
                label: t("login.workshopApi", "Workshop API"),
                value: apiBaseUrl,
                onChangeText: setApiBaseUrl,
                autoCapitalize: "none",
                placeholder: "http://192.168.1.20:5000"
              }),
              el(Field, {
                label: t("login.crewId", "Crew ID"),
                value: username,
                onChangeText: setUsername,
                autoCapitalize: "none",
                placeholder: "username"
              }),
              el(Field, {
                label: t("login.ignitionKey", "Ignition key"),
                value: password,
                onChangeText: setPassword,
                secureTextEntry: true,
                placeholder: "password"
              }),
              el(PrimaryButton, { title: isLoading ? t("login.warmingUp", "Warming up") : t("login.startEngine", "Start engine"), onPress: submit, disabled: isLoading }),
              !isKeyboardVisible && el(SocialLoginButtons, { disabled: isLoading, onExternalLogin: externalLogin, onStatus: setStatus }),
              Boolean(status) && el(Text, { style: styles.statusText }, status)
            )
          )
        )
      )
    )
  );
}

function SocialLoginButtons({ disabled, onExternalLogin, onStatus }) {
  const { styles, t } = useTheme();
  const isGoogleConfigured = Boolean(
    googleClientId || googleAndroidClientId || googleIosClientId || googleWebClientId
  );
  const isFacebookConfigured = Boolean(facebookAppId);

  const googleConfig = useMemo(() => ({
    clientId: googleClientId || googleWebClientId || fallbackSocialClientId,
    androidClientId: googleAndroidClientId || googleClientId || undefined,
    iosClientId: googleIosClientId || googleClientId || undefined,
    webClientId: googleWebClientId || googleClientId || undefined,
    selectAccount: true
  }), []);
  const facebookConfig = useMemo(() => ({
    clientId: facebookAppId || fallbackSocialClientId
  }), []);
  const redirectOptions = useMemo(() => ({ scheme: "spareparts", path: "auth" }), []);

  const [googleRequest, googleResponse, promptGoogleAsync] =
    Google.useIdTokenAuthRequest(googleConfig, redirectOptions);
  const [facebookRequest, facebookResponse, promptFacebookAsync] =
    Facebook.useAuthRequest(facebookConfig, redirectOptions);

  useEffect(() => {
    if (!googleResponse) return;

    if (googleResponse.type === "success") {
      const idToken = googleResponse.params?.id_token || googleResponse.authentication?.idToken;
      if (idToken) {
        onExternalLogin("google", { idToken });
      } else {
        onStatus(t("login.googleNoToken", "Google did not return an id token."));
      }
      return;
    }

    if (googleResponse.type === "cancel" || googleResponse.type === "dismiss") {
      onStatus(t("login.googleCancelled", "Google login was cancelled."));
    } else if (googleResponse.type === "error") {
      onStatus(googleResponse.error?.message || t("login.googleFailed", "Google login failed."));
    }
  }, [googleResponse, onExternalLogin, onStatus, t]);

  useEffect(() => {
    if (!facebookResponse) return;

    if (facebookResponse.type === "success") {
      const accessToken = facebookResponse.authentication?.accessToken || facebookResponse.params?.access_token;
      if (accessToken) {
        onExternalLogin("facebook", { accessToken });
      } else {
        onStatus(t("login.facebookNoToken", "Facebook did not return an access token."));
      }
      return;
    }

    if (facebookResponse.type === "cancel" || facebookResponse.type === "dismiss") {
      onStatus(t("login.facebookCancelled", "Facebook login was cancelled."));
    } else if (facebookResponse.type === "error") {
      onStatus(facebookResponse.error?.message || t("login.facebookFailed", "Facebook login failed."));
    }
  }, [facebookResponse, onExternalLogin, onStatus, t]);

  const loginWithGoogle = useCallback(async () => {
    if (!isGoogleConfigured) {
      onStatus(t("login.configureGoogle", "Configure Google client IDs before using Google login."));
      return;
    }

    onStatus(t("login.openingGoogle", "Opening Google..."));
    try {
      await promptGoogleAsync();
    } catch (error) {
      onStatus(error.message || t("login.googleFailed", "Google login failed."));
    }
  }, [isGoogleConfigured, onStatus, promptGoogleAsync, t]);

  const loginWithFacebook = useCallback(async () => {
    if (!isFacebookConfigured) {
      onStatus(t("login.configureFacebook", "Configure the Facebook app ID before using Facebook login."));
      return;
    }

    onStatus(t("login.openingFacebook", "Opening Facebook..."));
    try {
      await promptFacebookAsync();
    } catch (error) {
      onStatus(error.message || t("login.facebookFailed", "Facebook login failed."));
    }
  }, [isFacebookConfigured, onStatus, promptFacebookAsync, t]);

  return el(View, { style: styles.socialLogin },
    el(View, { style: styles.socialDivider },
      el(View, { style: styles.socialDividerLine }),
      el(Text, { style: styles.socialDividerText }, t("login.customerLane", "Customer lane")),
      el(View, { style: styles.socialDividerLine })
    ),
    el(Pressable, {
      style: [styles.socialButton, (!isGoogleConfigured || disabled || !googleRequest) && styles.disabledButton],
      onPress: loginWithGoogle,
      disabled: disabled || !googleRequest
    },
      el(View, { style: styles.socialBrandMark }, el(Text, { style: styles.socialBrandText }, "G")),
      el(Text, { style: styles.socialButtonText },
        isGoogleConfigured ? t("login.continueGoogle", "Continue with Google") : t("login.configureGoogleLogin", "Configure Google login")
      )
    ),
    el(Pressable, {
      style: [styles.socialButton, styles.facebookButton, (!isFacebookConfigured || disabled || !facebookRequest) && styles.disabledButton],
      onPress: loginWithFacebook,
      disabled: disabled || !facebookRequest
    },
      el(View, { style: styles.facebookBrandMark }, el(Text, { style: styles.facebookBrandText }, "f")),
      el(Text, { style: styles.socialButtonText },
        isFacebookConfigured ? t("login.continueFacebook", "Continue with Facebook") : t("login.configureFacebookLogin", "Configure Facebook login")
      )
    )
  );
}

module.exports = { LoginScreen };
