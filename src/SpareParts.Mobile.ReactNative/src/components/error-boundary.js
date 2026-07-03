const React = require("react");
const { Pressable, Text, View } = require("react-native");
const { ThemeContext } = require("../theme/theme-context");

const el = React.createElement;

/**
 * App-level error boundary. Wraps the app root (see App.js / app-shell.js)
 * so a render crash in one screen/component does not take down the whole
 * app with a blank white/black screen. Shows a minimal recovery UI with a
 * "Try again" action that resets the boundary and re-renders children.
 *
 * Class component (React.createElement style, no JSX) because error
 * boundaries require componentDidCatch / getDerivedStateFromError, which
 * are only available on class components.
 */
class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
    this.handleRetry = this.handleRetry.bind(this);
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, errorInfo) {
    // Keep this a no-op beyond a console warning in production: this app has
    // no crash-reporting SDK wired up yet. Swallowing here (instead of
    // rethrowing) is what actually stops one screen's crash from taking
    // down the whole app.
    if (typeof console !== "undefined" && console.warn) {
      console.warn("[ErrorBoundary] Caught render error:", error, errorInfo);
    }
  }

  handleRetry() {
    this.setState({ hasError: false, error: null });
  }

  render() {
    if (!this.state.hasError) {
      return this.props.children;
    }

    const themeBundle = this.context || {};
    const palette = themeBundle.palette || {};
    const t = themeBundle.t || ((key, fallback) => fallback || key);
    const bg = palette.bg || "#090909";
    const surface = palette.surface || "#131313";
    const text = palette.text || "#f2f1ee";
    const muted = palette.muted || "#9b9b9b";
    const accent = palette.accent || "#e2231a";

    return el(View, {
      style: {
        flex: 1,
        alignItems: "center",
        justifyContent: "center",
        padding: 24,
        backgroundColor: bg
      }
    },
      el(View, {
        style: {
          width: "100%",
          maxWidth: 420,
          borderRadius: 12,
          padding: 20,
          gap: 12,
          backgroundColor: surface
        }
      },
        el(Text, { style: { color: text, fontSize: 20, fontWeight: "700" } },
          t("errorBoundary.title", "Something went wrong")
        ),
        el(Text, { style: { color: muted, fontSize: 14, lineHeight: 20 } },
          t("errorBoundary.message", "This screen ran into a problem. You can try again without losing the rest of the app.")
        ),
        Boolean(this.state.error && this.state.error.message) && el(Text, {
          style: { color: muted, fontSize: 12 },
          numberOfLines: 3
        }, String(this.state.error.message)),
        el(Pressable, {
          accessibilityRole: "button",
          onPress: this.handleRetry,
          style: {
            marginTop: 8,
            alignSelf: "flex-start",
            paddingVertical: 10,
            paddingHorizontal: 18,
            borderRadius: 8,
            backgroundColor: accent
          }
        },
          el(Text, { style: { color: "#ffffff", fontWeight: "700" } }, t("errorBoundary.retry", "Try again"))
        )
      )
    );
  }
}

ErrorBoundary.contextType = ThemeContext;

module.exports = { ErrorBoundary };
