const React = require("react");
const {
  ActivityIndicator,
  Pressable,
  ScrollView,
  Text,
  TextInput,
  View
} = require("react-native");
const { useTheme } = require("../theme/theme-context");

const el = React.createElement;

function Field(props) {
  const { styles, palette } = useTheme();

  return el(View, { style: styles.field },
    el(Text, { style: styles.fieldLabel }, props.label),
    el(TextInput, {
      style: styles.input,
      value: props.value,
      onChangeText: props.onChangeText,
      secureTextEntry: props.secureTextEntry,
      autoCapitalize: props.autoCapitalize || "sentences",
      keyboardType: props.keyboardType,
      returnKeyType: props.returnKeyType,
      onSubmitEditing: props.onSubmitEditing,
      placeholder: props.placeholder,
      multiline: props.multiline,
      numberOfLines: props.numberOfLines,
      placeholderTextColor: palette.soft
    })
  );
}

function PrimaryButton({ title, onPress, disabled, compact }) {
  const { styles } = useTheme();

  return el(Pressable, {
    style: [styles.primaryButton, compact && styles.compactButton, disabled && styles.disabledButton],
    onPress,
    disabled
  }, el(Text, { style: styles.primaryButtonText }, title));
}

function SecondaryButton({ title, onPress }) {
  const { styles } = useTheme();

  return el(Pressable, { style: styles.secondaryButton, onPress },
    el(Text, { style: styles.secondaryButtonText }, title)
  );
}

function ScreenScroll({ children }) {
  const { styles } = useTheme();

  return el(ScrollView, {
    style: styles.screen,
    contentContainerStyle: styles.screenContent,
    contentInsetAdjustmentBehavior: "automatic",
    keyboardShouldPersistTaps: "handled"
  },
    children
  );
}

function ScreenHeader({ title, actionTitle, onAction, loading }) {
  const { styles, palette } = useTheme();

  return el(View, { style: styles.screenHeader },
    el(Text, { style: styles.screenTitle }, title),
    actionTitle && el(Pressable, { style: styles.headerAction, onPress: onAction, disabled: loading },
      loading
        ? el(ActivityIndicator, { color: palette.text, size: "small" })
        : el(Text, { style: styles.headerActionText }, actionTitle)
    )
  );
}

function StatusText({ value }) {
  const { styles } = useTheme();

  return value ? el(Text, { style: styles.statusText }, value) : null;
}

function Panel({ title, children }) {
  const { styles } = useTheme();

  return el(View, { style: styles.panel },
    el(Text, { style: styles.panelTitle }, title),
    children
  );
}

function ListRow({ title, subtitle, value }) {
  const { styles } = useTheme();

  return el(View, { style: styles.listRow },
    el(View, { style: styles.listRowCopy },
      el(Text, { style: styles.listRowTitle }, title),
      Boolean(subtitle) && el(Text, { style: styles.listRowSubtitle }, subtitle)
    ),
    Boolean(value) && el(Text, { style: styles.listRowValue }, value)
  );
}

function EmptyState({ text }) {
  const { styles } = useTheme();

  return el(Text, { style: styles.emptyState }, text);
}

module.exports = {
  EmptyState,
  Field,
  ListRow,
  Panel,
  PrimaryButton,
  ScreenHeader,
  ScreenScroll,
  SecondaryButton,
  StatusText
};
