const React = require("react");
const { Pressable, ScrollView, Text, TextInput, View } = require("react-native");
const { navigationGroups, wpfThemes } = require("../core/app-config");
const { initials } = require("../core/formatters");
const { isScreenVisible } = require("../core/role-policy");
const { useTheme } = require("../theme/theme-context");

const { useMemo, useState } = React;
const el = React.createElement;

function normalizeSearchText(value) {
  return String(value || "").toLowerCase().trim();
}

function AppSidebar({ activeKey, isWideLayout, screens, themeKey, user, onClose, onLogout, onSelect, onTheme }) {
  const { styles, palette, t } = useTheme();
  const [search, setSearch] = useState("");
  const [collapsedGroups, setCollapsedGroups] = useState({});
  const visibleScreens = useMemo(
    () => screens.filter((item) => isScreenVisible(item.key, user)),
    [screens, user]
  );
  const groups = useMemo(() => {
    const screenMap = new Map(visibleScreens.map((item) => [item.key, item]));
    const groupedKeys = new Set(navigationGroups.flatMap((group) => group.keys));
    const extraScreens = visibleScreens.filter((item) => !groupedKeys.has(item.key));

    return [
      ...navigationGroups.map((group) => ({
        ...group,
        items: group.keys.map((key) => screenMap.get(key)).filter(Boolean)
      })),
      extraScreens.length ? { title: "Other", keys: extraScreens.map((item) => item.key), items: extraScreens } : null
    ].filter((group) => group && group.items.length > 0);
  }, [visibleScreens]);

  const query = normalizeSearchText(search);
  const filteredGroups = useMemo(() => {
    if (!query) return groups;
    return groups
      .map((group) => ({
        ...group,
        items: group.items.filter((item) =>
          normalizeSearchText(t(`screens.${item.key}`, item.label)).includes(query)
        )
      }))
      .filter((group) => group.items.length > 0);
  }, [groups, query, t]);

  const toggleGroup = (title) => {
    setCollapsedGroups((current) => ({ ...current, [title]: !current[title] }));
  };

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
    el(View, { style: styles.uiSearchBarRow },
      el(Text, { style: styles.uiSearchBarIcon }, "⌕"),
      el(TextInput, {
        style: styles.uiSearchBarInput,
        value: search,
        onChangeText: setSearch,
        placeholder: t("nav.searchPlaceholder", "Search screens"),
        placeholderTextColor: palette.soft,
        autoCapitalize: "none"
      }),
      Boolean(search) && el(Pressable, { style: styles.uiSearchBarClear, onPress: () => setSearch(""), hitSlop: 6 },
        el(Text, { style: styles.uiSearchBarClearText }, "✕")
      )
    ),
    el(ScrollView, { style: styles.sideNav, contentContainerStyle: styles.sideNavContent, showsVerticalScrollIndicator: false },
      filteredGroups.map((group) => {
        const isCollapsed = Boolean(collapsedGroups[group.title]) && !query;
        return el(View, { key: group.title, style: styles.navGroup },
          el(Pressable, {
            onPress: () => toggleGroup(group.title),
            style: { flexDirection: "row", alignItems: "center", justifyContent: "space-between" }
          },
            el(Text, { style: styles.navGroupTitle }, t(`nav.groups.${group.title}`, group.title)),
            el(Text, { style: styles.navGroupTitle }, isCollapsed ? "+" : "–")
          ),
          !isCollapsed && group.items.map((item) => {
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
        );
      }),
      filteredGroups.length === 0 && el(Text, { style: styles.uiEmptyStateText }, t("nav.noMatches", "No screens match that search.")),
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
        el(Text, { style: styles.sideUserRole, numberOfLines: 1 }, (user.roleId ?? user.RoleId) ? `Role ID ${user.roleId ?? user.RoleId}` : "Role ID")
      ),
      el(Pressable, { style: styles.sideLogoutButton, onPress: onLogout },
        el(Text, { style: styles.sideLogoutText }, t("common.signOut", "Sign out"))
      )
    )
  );
}

module.exports = { AppSidebar };
