const React = require("react");
const { Pressable, Text, View } = require("react-native");
const { useTheme } = require("../theme/theme-context");

const el = React.createElement;

/**
 * Lightweight header/breadcrumb shown only on phone-width layouts where the
 * sidebar is hidden. Gives users a way to reopen the full navigation list
 * (menu button) and shows the current screen title without needing to
 * scroll past the screen's own ScreenHeader content first.
 */
function PhoneHeaderBar({ title, onMenuPress }) {
  const { styles, t } = useTheme();

  return el(View, { style: styles.phoneHeaderBar },
    el(Pressable, {
      style: styles.phoneHeaderMenuButton,
      onPress: onMenuPress,
      accessibilityLabel: t("nav.openMenu", "Open menu"),
      hitSlop: 6
    },
      el(View, { style: styles.phoneHeaderMenuLine }),
      el(View, { style: styles.phoneHeaderMenuLine }),
      el(View, { style: styles.phoneHeaderMenuLine })
    ),
    el(Text, { style: styles.phoneHeaderTitle, numberOfLines: 1 }, title)
  );
}

module.exports = { PhoneHeaderBar };
