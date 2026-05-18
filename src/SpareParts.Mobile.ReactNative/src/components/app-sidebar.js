const React = require("react");
const { Pressable, ScrollView, Text, View } = require("react-native");
const { navigationGroups, wpfThemes } = require("../core/app-config");
const { initials } = require("../core/formatters");
const { useTheme } = require("../theme/theme-context");

const { useMemo } = React;
const el = React.createElement;

function AppSidebar({ activeKey, isWideLayout, screens, themeKey, user, onClose, onLogout, onSelect, onTheme }) {
  const { styles, t } = useTheme();
  const screensByKey = useMemo(() => new Map(screens.map((item) => [item.key, item])), [screens]);

  return el(View, { style: [styles.sidePanel, !isWideLayout && styles.sidePanelOverlay] },
    el(View, { style: styles.sideHeader },
      el(View, { style: styles.sideBrandRow },
        el(View, { style: styles.sideBrandMark }, el(Text, { style: styles.sideBrandMarkText }, "M")),
        el(View, { style: styles.sideBrandCopy },
          el(Text, { style: styles.sideBrandTitle }, "Maalouf"),
          el(Text, { style: styles.sideBrandSubtitle }, t("brand.autoParts", "Auto Parts"))
        )
      ),
      el(Pressable, { style: styles.sideCloseButton, onPress: onClose },
        el(Text, { style: styles.sideCloseText }, t("common.hide", "Hide"))
      )
    ),
    el(ScrollView, { style: styles.sideNav, contentContainerStyle: styles.sideNavContent, showsVerticalScrollIndicator: false },
      navigationGroups.map((group) =>
        el(View, { key: group.title, style: styles.navGroup },
          el(Text, { style: styles.navGroupTitle }, t(`nav.groups.${group.title}`, group.title)),
          group.keys.map((key) => {
            const item = screensByKey.get(key);
            if (!item) return null;
            const isActive = item.key === activeKey;
            return el(Pressable, {
              key: item.key,
              style: [styles.sideNavItem, isActive && styles.sideNavItemActive],
              onPress: () => onSelect(item.key)
            },
              el(View, { style: [styles.navActiveMark, isActive && styles.navActiveMarkOn] }),
              el(Text, { style: [styles.sideNavText, isActive && styles.sideNavTextActive], numberOfLines: 1 }, t(`screens.${item.key}`, item.label))
            );
          })
        )
      ),
      el(View, { style: styles.sideThemeBlock },
        el(Text, { style: styles.navGroupTitle }, t("common.theme", "Theme")),
        el(View, { style: styles.sideThemeGrid },
          wpfThemes.map((theme) =>
            el(Pressable, {
              key: theme.key,
              style: [styles.sideThemeButton, theme.key === themeKey && styles.sideThemeButtonActive],
              onPress: () => onTheme(theme.key)
            },
              el(View, { style: [styles.themeDot, { backgroundColor: theme.colors.accent, borderColor: theme.colors.line }] }),
              el(Text, { style: [styles.sideThemeText, theme.key === themeKey && styles.sideThemeTextActive], numberOfLines: 1 }, theme.name)
            )
          )
        )
      )
    ),
    el(View, { style: styles.sideUserPanel },
      el(View, { style: styles.sideAvatar }, el(Text, { style: styles.sideAvatarText }, initials(user.fullName))),
      el(View, { style: styles.sideUserCopy },
        el(Text, { style: styles.sideUserName, numberOfLines: 1 }, user.fullName),
        el(Text, { style: styles.sideUserRole, numberOfLines: 1 }, user.role)
      ),
      el(Pressable, { style: styles.sideLogoutButton, onPress: onLogout },
        el(Text, { style: styles.sideLogoutText }, t("common.signOut", "Sign out"))
      )
    )
  );
}

module.exports = { AppSidebar };
