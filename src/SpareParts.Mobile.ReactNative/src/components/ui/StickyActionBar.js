const React = require("react");
const { ActivityIndicator, Pressable, Text, View } = require("react-native");
const { useSafeAreaInsets } = require("react-native-safe-area-context");
const { useTheme } = require("../../theme/theme-context");

const el = React.createElement;

/**
 * Bottom-pinned, safe-area-aware action bar for Save/Cancel/Delete style
 * flows. Renders as `position: absolute` within its parent, so the parent
 * must be a `flex: 1` positioned container (screens already wrap content
 * in `View`/`ScreenScroll`s that satisfy this).
 *
 * actions: Array<{ key, title, onPress, variant?: "primary"|"secondary"|"danger", disabled?, loading? }>
 */
function StickyActionBar({ actions, style }) {
  const { styles, palette } = useTheme();
  const insets = useSafeAreaInsets();

  return el(View, {
    style: [styles.uiStickyBar, { paddingBottom: (insets.bottom || 0) + 10 }, style]
  },
    (actions || []).map((action) => {
      const variantStyle = action.variant === "secondary"
        ? styles.uiStickyBarButtonSecondary
        : action.variant === "danger"
          ? styles.uiStickyBarButtonDanger
          : null;
      const textStyle = action.variant === "secondary" ? styles.uiStickyBarButtonSecondaryText : null;

      return el(Pressable, {
        key: action.key || action.title,
        style: [
          styles.uiStickyBarButton,
          variantStyle,
          (action.disabled || action.loading) && styles.uiStickyBarButtonDisabled
        ],
        onPress: action.onPress,
        disabled: action.disabled || action.loading
      },
        action.loading
          ? el(ActivityIndicator, { color: action.variant === "secondary" ? palette.text : "#ffffff", size: "small" })
          : el(Text, { style: [styles.uiStickyBarButtonText, textStyle], numberOfLines: 1 }, action.title)
      );
    })
  );
}

/**
 * Spacer to place at the bottom of scroll content so the last item isn't
 * hidden behind a StickyActionBar.
 */
function StickyActionBarSpacer() {
  const { styles } = useTheme();
  return el(View, { style: styles.uiStickyBarSpacer });
}

module.exports = { StickyActionBar, StickyActionBarSpacer };
