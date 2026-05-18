import { h } from "../core/react-runtime.js";
import { initials } from "../core/formatters.js";
import { LanguagePicker, ThemePicker } from "../components/layout.js";
import { PageHeader } from "../components/shared.js";

export function SettingsView({ user, themeKey, languageKey, onTheme, onLanguage, onLogout, t }) {
  return h("section", { className: "screen settings-screen" },
    h(PageHeader, {
      title: t("settings.title", "Settings"),
      action: null
    }),
    h("p", { className: "settings-subtitle" }, t("settings.subtitle", "Themes, language, and account access.")),
    h("section", { className: "settings-grid" },
      h("article", { className: "settings-panel" },
        h("h3", null, t("settings.themes", "Themes")),
        h(ThemePicker, { value: themeKey, onChange: onTheme, t })
      ),
      h("article", { className: "settings-panel" },
        h("h3", null, t("settings.language", "Language")),
        h(LanguagePicker, { value: languageKey, onChange: onLanguage, t })
      ),
      h("article", { className: "settings-panel account-settings-panel" },
        h("h3", null, t("settings.account", "Account")),
        h("div", { className: "account-card" },
          h("span", { className: "avatar" }, initials(user?.fullName)),
          h("div", null,
            h("span", null, t("settings.signedInAs", "Signed in as")),
            h("strong", null, user?.fullName || user?.username || "User"),
            h("small", null, user?.role || "")
          )
        ),
        h("p", null, t("settings.signOutHelp", "Return to the login screen.")),
        h("button", { className: "primary-button danger-button", type: "button", onClick: onLogout }, t("common.signOut", "Sign out"))
      )
    )
  );
}
