const React = require("react");
const { Text, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");

const el = React.createElement;

const toneStyleMap = {
  neutral: { pill: "uiStatusPillNeutral", text: "uiStatusPillNeutralText", dot: "uiStatusPillNeutralDot" },
  accent: { pill: "uiStatusPillAccent", text: "uiStatusPillAccentText", dot: "uiStatusPillAccentDot" },
  good: { pill: "uiStatusPillGood", text: "uiStatusPillGoodText", dot: "uiStatusPillGoodDot" },
  warn: { pill: "uiStatusPillWarn", text: "uiStatusPillWarnText", dot: "uiStatusPillWarnDot" },
  bad: { pill: "uiStatusPillBad", text: "uiStatusPillBadText", dot: "uiStatusPillBadDot" }
};

/**
 * Small rounded status/badge pill with a colored dot.
 * tone: "neutral" | "accent" | "good" | "warn" | "bad"
 */
function StatusPill({ label, tone, style }) {
  const { styles } = useTheme();
  const toneKey = toneStyleMap[tone] ? tone : "neutral";
  const toneStyles = toneStyleMap[toneKey];

  return el(View, { style: [styles.uiStatusPill, styles[toneStyles.pill], style] },
    el(View, { style: [styles.uiStatusPillDot, styles[toneStyles.dot]] }),
    el(Text, { style: [styles.uiStatusPillText, styles[toneStyles.text]], numberOfLines: 1 }, label)
  );
}

module.exports = { StatusPill };
