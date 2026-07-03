import { React, h } from "../core/react-runtime.js";

// Top-level render-error guard. React error boundaries must be class
// components (there is no hook equivalent), so this is the one exception to
// the function-component style used across the rest of components/*.js.
//
// Catches any uncaught render/lifecycle error thrown by a child (including
// any of the ~70 view modules under js/views/) and swaps the whole subtree
// for a minimal recovery screen instead of leaving a blank white page.
export class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
    this.handleReload = this.handleReload.bind(this);
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, info) {
    // Keep this to console only - never surface stack traces to the DOM.
    console.error("Unhandled UI error caught by ErrorBoundary:", error, info);
  }

  handleReload() {
    window.location.reload();
  }

  render() {
    if (!this.state.hasError) {
      return this.props.children;
    }

    return h("div", { className: "error-boundary-screen", role: "alert" },
      h("div", { className: "error-boundary-card" },
        h("strong", null, "Something went wrong"),
        h("p", null, "This screen hit an unexpected error. Your data is safe - reloading the app usually fixes this."),
        h("button", {
          type: "button",
          className: "primary-button",
          onClick: this.handleReload
        }, "Reload app")
      )
    );
  }
}
