const React = require("react");
const { Pressable, Text, TextInput, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");

const el = React.createElement;

/**
 * Standalone search input row with a leading glyph and trailing clear
 * button, used across list/grid screens (Parts, Storefront, etc.) in place
 * of a plain labeled Field for search-only inputs.
 */
function SearchBar({ value, onChangeText, placeholder, onSubmitEditing, style, autoFocus }) {
  const { styles, palette } = useTheme();

  return el(View, { style: [styles.uiSearchBarRow, style] },
    el(Text, { style: styles.uiSearchBarIcon }, "⌕"),
    el(TextInput, {
      style: styles.uiSearchBarInput,
      value,
      onChangeText,
      placeholder,
      placeholderTextColor: palette.soft,
      returnKeyType: "search",
      onSubmitEditing,
      autoFocus,
      autoCapitalize: "none"
    }),
    Boolean(value) && el(Pressable, {
      style: styles.uiSearchBarClear,
      onPress: () => onChangeText && onChangeText(""),
      hitSlop: 6
    }, el(Text, { style: styles.uiSearchBarClearText }, "✕"))
  );
}

module.exports = { SearchBar };
