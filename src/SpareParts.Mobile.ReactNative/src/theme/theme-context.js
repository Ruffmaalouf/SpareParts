const React = require("react");
const { defaultThemeKey } = require("../core/app-config");
const { createPalette, createStyles } = require("./styles");

const defaultPalette = createPalette(defaultThemeKey);
const defaultThemeBundle = {
  palette: defaultPalette,
  styles: createStyles(defaultPalette),
  languageKey: "en",
  isRtl: false,
  textDirection: "ltr",
  t: (key, fallback) => fallback || key
};

const ThemeContext = React.createContext(defaultThemeBundle);

function useTheme() {
  return React.useContext(ThemeContext);
}

module.exports = {
  ThemeContext,
  useTheme
};
