import { h, useEffect, useState } from "../core/react-runtime.js";

// Resolves a screen component from the (code-split) screen registry and
// renders a lightweight loading fallback until the module finishes
// downloading. Screens are cached by ScreenRegistry after first load, so
// switching back to an already-visited screen renders instantly (via
// registry.peek) instead of re-showing the fallback.
export function LazyScreen({ registry, screenKey, screenProps }) {
  const [Component, setComponent] = useState(() => registry.peek(screenKey));

  useEffect(() => {
    let cancelled = false;
    const cached = registry.peek(screenKey);

    if (cached) {
      setComponent(() => cached);
      return undefined;
    }

    setComponent(null);
    registry.resolveAsync(screenKey).then((resolved) => {
      if (!cancelled) setComponent(() => resolved);
    });

    return () => {
      cancelled = true;
    };
  }, [registry, screenKey]);

  if (!Component) {
    return h("div", { className: "screen-loading-fallback", role: "status", "aria-live": "polite" },
      h("span", { className: "screen-loading-spinner", "aria-hidden": "true" }),
      h("span", null, "Loading...")
    );
  }

  return h(Component, screenProps);
}
