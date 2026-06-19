const React = require("react");
const { Pressable, Text, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");

const el = React.createElement;

/**
 * Richer empty-state block: icon glyph, title, supporting text, optional
 * action button. Backward compatible with the simple `{ text }` usage from
 * `components/ui.js`'s EmptyState (text-only renders as title).
 */
function EmptyState({ icon, title, text, actionTitle, onAction, style }) {
  const { styles } = useTheme();

  if (!title && text) {
    title = text;
    text = undefined;
  }

  return el(View, { style: [styles.uiEmptyState, style] },
    Boolean(icon) && el(View, { style: styles.uiEmptyStateIcon },
      el(Text, { style: styles.uiEmptyStateIconText }, icon)
    ),
    Boolean(title) && el(Text, { style: styles.uiEmptyStateTitle }, title),
    Boolean(text) && el(Text, { style: styles.uiEmptyStateText }, text),
    Boolean(actionTitle) && el(Pressable, { style: styles.uiEmptyStateAction, onPress: onAction },
      el(Text, { style: styles.uiEmptyStateActionText }, actionTitle)
    )
  );
}

module.exports = { EmptyState };
