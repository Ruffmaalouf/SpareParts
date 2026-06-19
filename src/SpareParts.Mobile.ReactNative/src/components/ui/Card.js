const React = require("react");
const { Pressable, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");

const el = React.createElement;

/**
 * Generic elevated card container. Replaces ad-hoc Panel-as-list-row usage
 * with a tappable, shadowed surface suitable for list items, summary tiles,
 * and grouped content blocks.
 *
 * Props:
 *  - onPress: optional. If provided, the card becomes a Pressable.
 *  - compact: tighter padding.
 *  - style: extra style(s) merged last.
 */
function Card({ children, onPress, compact, style, testID }) {
  const { styles } = useTheme();
  const cardStyle = [styles.uiCard, compact && styles.uiCardCompact, style];

  if (onPress) {
    return el(Pressable, {
      testID,
      onPress,
      style: ({ pressed }) => [...cardStyle, pressed && styles.uiCardPressed]
    }, children);
  }

  return el(View, { testID, style: cardStyle }, children);
}

module.exports = { Card };
