const React = require("react");
const { Alert, Pressable, Text, View } = require("react-native");
const { appLanguages, wpfThemes } = require("../core/app-config");
const { Panel, ScreenHeader, ScreenScroll } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback } = React;
const el = React.createElement;

function SettingsScreen({ languageKey, themeKey, onLanguage, onTheme, onLogout }) {
  const { styles, t } = useTheme();

  const confirmLogout = useCallback(() => {
    if (!onLogout) return;

    Alert.alert(
      t("settings.signOutQuestion", "Sign out?"),
      t("settings.signOutMessage", "Return to the login screen?"),
      [
        { text: t("common.cancel", "Cancel"), style: "cancel" },
        { text: t("common.signOut", "Sign Out"), style: "destructive", onPress: onLogout }
      ]
    );
  }, [onLogout, t]);

  return el(ScreenScroll, null,
    el(ScreenHeader, { title: t("settings.title", "Settings") }),
    el(Panel, { title: t("settings.language", "Language") },
      el(View, { style: styles.sideThemeGrid },
        appLanguages.map((language) =>
          el(Pressable, {
            key: language.key,
            style: [styles.settingsLanguageButton, language.key === languageKey && styles.settingsLanguageButtonActive],
            onPress: () => onLanguage && onLanguage(language.key)
          },
            el(View, { style: [styles.settingsLanguageMark, language.key === languageKey && styles.settingsLanguageMarkActive] },
              el(Text, { style: [styles.settingsLanguageMarkText, language.key === languageKey && styles.settingsLanguageMarkTextActive] }, language.shortName)
            ),
            el(Text, { style: [styles.sideThemeText, language.key === languageKey && styles.sideThemeTextActive], numberOfLines: 1 }, t(`languages.${language.key}`, language.name))
          )
        )
      )
    ),
    el(Panel, { title: t("settings.themes", "Themes") },
      el(View, { style: styles.sideThemeGrid },
        wpfThemes.map((theme) =>
          el(Pressable, {
            key: theme.key,
            style: [styles.sideThemeButton, theme.key === themeKey && styles.sideThemeButtonActive],
            onPress: () => onTheme && onTheme(theme.key)
          },
            el(View, { style: [styles.themeDot, { backgroundColor: theme.colors.accent, borderColor: theme.colors.line }] }),
            el(Text, { style: [styles.sideThemeText, theme.key === themeKey && styles.sideThemeTextActive], numberOfLines: 1 }, theme.name)
          )
        )
      )
    ),
    el(Panel, { title: t("settings.account", "Account") },
      el(View, { style: styles.inlineButtons },
        el(Pressable, {
          style: [styles.secondaryButton, styles.usedCarDangerSoft],
          onPress: confirmLogout
        },
          el(Text, { style: styles.secondaryButtonText }, t("common.signOut", "Sign Out"))
        )
      )
    )
  );
}

module.exports = { SettingsScreen };
