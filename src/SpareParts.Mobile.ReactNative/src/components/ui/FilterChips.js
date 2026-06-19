const React = require("react");
const { Pressable, ScrollView, Text, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");

const el = React.createElement;

/**
 * Horizontally scrollable filter chip rail with a visible scroll
 * affordance (trailing fade) so users know more chips exist off-screen.
 *
 * options: Array<{ value, label, count? }>
 * value: currently selected value (single-select) — for multi-select pass
 *   an array and set `multi`.
 */
function FilterChips({ options, value, onChange, multi, style }) {
  const { styles, palette } = useTheme();
  const selectedValues = multi ? (Array.isArray(value) ? value : []) : null;

  const isSelected = (optionValue) => multi
    ? selectedValues.includes(optionValue)
    : value === optionValue;

  const select = (optionValue) => {
    if (!multi) {
      onChange(optionValue);
      return;
    }
    const isOn = selectedValues.includes(optionValue);
    onChange(isOn ? selectedValues.filter((item) => item !== optionValue) : [...selectedValues, optionValue]);
  };

  return el(View, { style: [styles.uiFilterScrollWrap, style] },
    el(ScrollView, {
      horizontal: true,
      showsHorizontalScrollIndicator: true,
      contentContainerStyle: styles.uiFilterChipRail
    },
      (options || []).map((option) => {
        const active = isSelected(option.value);
        return el(Pressable, {
          key: String(option.value),
          style: [styles.uiFilterChip, active && styles.uiFilterChipActive],
          onPress: () => select(option.value)
        },
          el(Text, { style: [styles.uiFilterChipText, active && styles.uiFilterChipTextActive] }, option.label),
          (option.count !== undefined && option.count !== null) && el(Text, {
            style: [styles.uiFilterChipCount, active && styles.uiFilterChipCountActive]
          }, String(option.count))
        );
      })
    )
  );
}

module.exports = { FilterChips };
