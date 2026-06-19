import { h, useCallback, useEffect, useMemo, useRef, useState } from "../core/react-runtime.js";
import { facebookAppId, googleClientId } from "../core/config.js";
import { ApiClient } from "../core/api-client.js";
import { BrandMark, LanguagePicker } from "./layout.js";
import { FormSection } from "./ui/FormSection.js";
import { ActionBar } from "./ui/ActionBar.js";

function useLoginApi(apiBaseUrl) {
  return useMemo(() => new ApiClient(apiBaseUrl, ""), [apiBaseUrl]);
}

export function LoginScreen({ initialApiBaseUrl, languageKey, onLanguage, onLogin, t }) {
  return h("main", { className: "login-page" },
    h("div", { className: "login-atmosphere", "aria-hidden": "true" },
      h("span", { className: "rpm-ring" }),
      h("span", { className: "headlight left" }),
      h("span", { className: "headlight right" })
    ),
    h("section", { className: "login-panel" },
      h("div", { className: "brand-lockup" },
        h(BrandMark, { label: t("login.brand", "Maalouf Auto Parts") }),
        h("div", null,
          h("span", { className: "eyebrow" }, t("login.garageAccess", "Garage access")),
          h("h1", null, t("login.brand", "Maalouf Auto Parts")),
          h("p", null, t("login.subtitle", "Parts, sales, inventory, and customers from the heart of the workshop."))
        )
      ),
      h("div", { className: "login-gauge" },
        h("div", null, h("strong", null, "98"), h("span", null, "RON")),
        h("div", null, h("strong", null, "24"), h("span", null, "V")),
        h("div", null, h("strong", null, "READY"), h("span", null, "LINE"))
      ),
      h(LoginPanel, { initialApiBaseUrl, onLogin, t }),
      h(LanguagePicker, { value: languageKey, onChange: onLanguage, t })
    )
  );
}

export function LoginPanel({ initialApiBaseUrl, onLogin, t }) {
  const [apiBaseUrl, setApiBaseUrl] = useState(initialApiBaseUrl);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const api = useLoginApi(apiBaseUrl);

  const submit = useCallback(async (event) => {
    event.preventDefault();
    setIsLoading(true);
    setStatus(t("login.signingIn", "Signing in..."));
    try {
      const response = await api.post("/api/auth/login", { username, password });
      onLogin(apiBaseUrl, response);
    } catch (error) {
      setStatus(error.message || t("login.failed", "Login failed."));
    } finally {
      setIsLoading(false);
    }
  }, [api, apiBaseUrl, username, password, onLogin, t]);

  const handleExternalLogin = useCallback(async (provider, payload) => {
    setIsLoading(true);
    setStatus(t("login.signingInWith", `Signing in with ${provider}...`, { provider }));
    try {
      const response = await api.post("/api/auth/external-login", {
        provider,
        ...payload
      });
      onLogin(apiBaseUrl, response);
    } catch (error) {
      setStatus(error.message || t("login.providerFailed", `${provider} login failed.`, { provider }));
    } finally {
      setIsLoading(false);
    }
  }, [api, apiBaseUrl, onLogin, t]);

  return h("div", { className: "login-inline-panel login-inline-panel-v2" },
    h("form", { className: "login-form login-form-v2", onSubmit: submit },
      h(FormSection, { title: t("login.connection", "Connection"), columns: 1 },
        h("label", null, t("login.workshopApi", "Workshop API"),
          h("input", {
            value: apiBaseUrl,
            onChange: (event) => setApiBaseUrl(event.target.value),
            spellCheck: false
          })
        )
      ),
      h(FormSection, { title: t("login.credentials", "Credentials"), columns: 1 },
        h("label", null, t("login.crewId", "Crew ID"),
          h("input", {
            value: username,
            onChange: (event) => setUsername(event.target.value),
            autoComplete: "username"
          })
        ),
        h("label", null, t("login.ignitionKey", "Ignition key"),
          h("input", {
            value: password,
            onChange: (event) => setPassword(event.target.value),
            type: "password",
            autoComplete: "current-password"
          })
        )
      ),
      status && h("p", { className: "form-status" }, status),
      h(ActionBar, { sticky: false, summary: null },
        h("button", { className: "primary-button", disabled: isLoading }, isLoading ? t("login.warmingUp", "Warming up") : t("login.startEngine", "Start engine"))
      )
    ),
    h(ExternalLoginButtons, { disabled: isLoading, onExternalLogin: handleExternalLogin, t })
  );
}

export function ExternalLoginButtons({ disabled, onExternalLogin, t }) {
  const googleButtonRef = useRef(null);
  const [facebookReady, setFacebookReady] = useState(Boolean(window.FB));

  useEffect(() => {
    if (!googleClientId || !googleButtonRef.current) return;

    let tries = 0;
    const timer = window.setInterval(() => {
      tries += 1;
      if (!window.google?.accounts?.id) {
        if (tries > 20) window.clearInterval(timer);
        return;
      }

      window.clearInterval(timer);
      window.google.accounts.id.initialize({
        client_id: googleClientId,
        callback: (response) => {
          if (response?.credential) {
            onExternalLogin("google", { idToken: response.credential });
          }
        },
        locale: "en"
      });
      window.google.accounts.id.renderButton(googleButtonRef.current, {
        type: "standard",
        theme: "filled_black",
        size: "large",
        width: 320,
        text: "continue_with",
        locale: "en"
      });
    }, 250);

    return () => window.clearInterval(timer);
  }, [onExternalLogin]);

  useEffect(() => {
    if (!facebookAppId || window.FB) {
      setFacebookReady(Boolean(window.FB));
      return;
    }

    window.fbAsyncInit = function fbAsyncInit() {
      window.FB.init({
        appId: facebookAppId,
        cookie: true,
        xfbml: false,
        version: "v19.0"
      });
      setFacebookReady(true);
    };

    if (!document.getElementById("facebook-jssdk")) {
      const script = document.createElement("script");
      script.id = "facebook-jssdk";
      script.src = "https://connect.facebook.net/en_US/sdk.js";
      script.async = true;
      script.defer = true;
      document.body.appendChild(script);
    }
  }, []);

  const loginWithFacebook = useCallback(() => {
    if (!window.FB) return;
    window.FB.login((response) => {
      const accessToken = response?.authResponse?.accessToken;
      if (accessToken) {
        onExternalLogin("facebook", { accessToken });
      }
    }, { scope: "public_profile,email" });
  }, [onExternalLogin]);

  return h("div", { className: "external-login" },
    h("div", { className: "external-divider" }, h("span", null, t("login.customerLane", "Customer lane"))),
    googleClientId
      ? h("div", { className: "google-login-slot", ref: googleButtonRef })
      : h("button", { className: "social-button", disabled: true }, t("login.configureGoogleLogin", "Configure Google login")),
    h("button", {
      className: "social-button facebook",
      type: "button",
      disabled: disabled || !facebookAppId || !facebookReady,
      onClick: loginWithFacebook
    }, facebookAppId ? t("login.continueFacebook", "Continue with Facebook") : t("login.configureFacebookLogin", "Configure Facebook login"))
  );
}
