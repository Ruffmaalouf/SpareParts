const React = require("react");
const { ActivityIndicator, Pressable, Text, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");

const el = React.createElement;

/**
 * Section title + optional subtitle + optional trailing action button.
 * Used to group blocks of content within a screen (replacing bare
 * Panel-title-as-only-hierarchy pattern).
 */
function SectionHeader({ title, subtitle, actionTitle, onAction, loading, style }) {
  const { styles, palette } = useTheme();

  return el(View, { style: [styles.uiSectionHeader, style] },
    el(View, { style: styles.uiSectionHeaderTextWrap },
      el(Text, { style: styles.uiSectionHeaderTitle, numberOfLines: 1 }, title),
      Boolean(subtitle) && el(Text, { style: styles.uiSectionHeaderSubtitle, numberOfLines: 2 }, subtitle)
    ),
    Boolean(actionTitle) && el(Pressable, {
      style: styles.uiSectionHeaderAction,
      onPress: onAction,
      disabled: loading
    },
      loading
        ? el(ActivityIndicator, { color: palette.accent, size: "small" })
        : el(Text, { style: styles.uiSectionHeaderActionText }, actionTitle)
    )
  );
}

module.exports = { SectionHeader };
