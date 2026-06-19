const React = require("react");
const { Animated, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");

const { useEffect, useRef } = React;
const el = React.createElement;

/**
 * Shimmering placeholder block. Built with the core `Animated` API only
 * (no reanimated dependency available in this project).
 */
function ShimmerBlock({ width, height, style, round }) {
  const { styles } = useTheme();
  const opacity = useRef(new Animated.Value(0.35)).current;

  useEffect(() => {
    const loop = Animated.loop(
      Animated.sequence([
        Animated.timing(opacity, { toValue: 1, duration: 650, useNativeDriver: true }),
        Animated.timing(opacity, { toValue: 0.35, duration: 650, useNativeDriver: true })
      ])
    );
    loop.start();
    return () => loop.stop();
  }, [opacity]);

  return el(View, {
    style: [
      styles.uiSkeletonBlock,
      { width: width || "100%", height: height || 14, borderRadius: round ? (height || 14) / 2 : 8 },
      style
    ]
  },
    el(Animated.View, { style: [styles.uiSkeletonShimmer, { opacity }] })
  );
}

/**
 * Pre-built skeleton row matching a typical card/list item shape
 * (thumbnail + two text lines), repeated `count` times.
 */
function LoadingSkeleton({ count, variant, style }) {
  const { styles } = useTheme();
  const rows = Array.from({ length: count || 3 }, (_, index) => index);

  if (variant === "card") {
    return el(View, { style: [{ gap: 10 }, style] },
      rows.map((index) => el(View, { key: index, style: styles.uiSkeletonCard },
        el(ShimmerBlock, { height: 110, style: { borderRadius: 10 } }),
        el(ShimmerBlock, { width: "70%", height: 13 }),
        el(ShimmerBlock, { width: "45%", height: 11 })
      ))
    );
  }

  return el(View, { style: [{ gap: 10 }, style] },
    rows.map((index) => el(View, { key: index, style: styles.uiSkeletonCard },
      el(View, { style: styles.uiSkeletonRow },
        el(ShimmerBlock, { width: 52, height: 52, style: { borderRadius: 10 } }),
        el(View, { style: { flex: 1, gap: 6 } },
          el(ShimmerBlock, { width: "80%", height: 13 }),
          el(ShimmerBlock, { width: "55%", height: 11 })
        )
      )
    ))
  );
}

module.exports = { LoadingSkeleton, ShimmerBlock };
