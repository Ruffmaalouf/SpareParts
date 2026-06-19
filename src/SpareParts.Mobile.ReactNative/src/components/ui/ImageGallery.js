const React = require("react");
const {
  Animated,
  FlatList,
  Image,
  Modal,
  PanResponder,
  Pressable,
  Text,
  View
} = require("react-native");
const { useSafeAreaInsets } = require("react-native-safe-area-context");
const { useTheme } = require("../../theme/theme-context");

const { useCallback, useMemo, useRef, useState } = React;
const el = React.createElement;

function touchDistance(touches) {
  if (!touches || touches.length < 2) return 0;
  const [a, b] = touches;
  return Math.hypot(a.pageX - b.pageX, a.pageY - b.pageY);
}

function clamp(value, min, max) {
  return Math.min(Math.max(value, min), max);
}

/**
 * Swipeable image gallery with thumbnail strip and a fullscreen
 * pinch-zoom-and-pan modal viewer. Extracted/generalized from the
 * bespoke gallery implementation previously embedded in
 * used-cars-screen.js, built entirely on Animated + PanResponder
 * (no extra native deps).
 *
 * Props:
 *  - images: Array<{ uri: string } | string>
 *  - height: hero image height (default 220)
 *  - emptyLabel: text shown when there are no images
 */
function ImageGallery({ images, height, emptyLabel }) {
  const { styles, palette, t } = useTheme();
  const insets = useSafeAreaInsets();
  const sources = useMemo(() => (images || [])
    .filter(Boolean)
    .map((image) => (typeof image === "string" ? { uri: image } : image)), [images]);

  const [activeIndex, setActiveIndex] = useState(0);
  const [isFullscreen, setIsFullscreen] = useState(false);
  const listRef = useRef(null);
  const fullscreenListRef = useRef(null);

  const zoomScale = useRef(new Animated.Value(1)).current;
  const zoomPan = useRef(new Animated.ValueXY({ x: 0, y: 0 })).current;
  const zoomState = useRef({ scale: 1, pan: { x: 0, y: 0 }, lastDistance: 0 });

  const applyZoom = useCallback((nextZoom, animated) => {
    const target = clamp(nextZoom, 1, 4);
    zoomState.current.scale = target;
    if (target === 1) {
      zoomState.current.pan = { x: 0, y: 0 };
    }
    const animations = [
      Animated.timing(zoomScale, { toValue: target, duration: animated ? 160 : 0, useNativeDriver: true })
    ];
    if (target === 1) {
      animations.push(Animated.timing(zoomPan, { toValue: { x: 0, y: 0 }, duration: animated ? 160 : 0, useNativeDriver: true }));
    }
    Animated.parallel(animations).start();
  }, [zoomPan, zoomScale]);

  const resetZoom = useCallback(() => applyZoom(1, true), [applyZoom]);
  const toggleZoom = useCallback(() => applyZoom(zoomState.current.scale > 1 ? 1 : 2.4, true), [applyZoom]);

  const zoomResponder = useMemo(() => PanResponder.create({
    onStartShouldSetPanResponder: () => true,
    onMoveShouldSetPanResponder: (_event, gesture) =>
      Math.abs(gesture.dx) > 4 || Math.abs(gesture.dy) > 4 || gesture.numberActiveTouches === 2,
    onPanResponderMove: (event, gesture) => {
      const touches = event.nativeEvent.touches;
      if (touches && touches.length === 2) {
        const distance = touchDistance(touches);
        if (zoomState.current.lastDistance) {
          const ratio = distance / zoomState.current.lastDistance;
          applyZoom(zoomState.current.scale * ratio, false);
        }
        zoomState.current.lastDistance = distance;
        return;
      }
      zoomState.current.lastDistance = 0;
      if (zoomState.current.scale > 1) {
        zoomPan.setValue({
          x: zoomState.current.pan.x + gesture.dx,
          y: zoomState.current.pan.y + gesture.dy
        });
      }
    },
    onPanResponderRelease: (_event, gesture) => {
      zoomState.current.lastDistance = 0;
      if (zoomState.current.scale > 1) {
        zoomState.current.pan = {
          x: zoomState.current.pan.x + gesture.dx,
          y: zoomState.current.pan.y + gesture.dy
        };
      }
    }
  }), [applyZoom, zoomPan]);

  const openFullscreen = useCallback((index) => {
    setActiveIndex(index);
    setIsFullscreen(true);
    resetZoom();
  }, [resetZoom]);

  const closeFullscreen = useCallback(() => {
    setIsFullscreen(false);
    resetZoom();
  }, [resetZoom]);

  const syncGalleryIndex = useCallback((event) => {
    const width = event.nativeEvent.layoutMeasurement.width || 1;
    const index = Math.round(event.nativeEvent.contentOffset.x / width);
    setActiveIndex(clamp(index, 0, Math.max(sources.length - 1, 0)));
    resetZoom();
  }, [resetZoom, sources.length]);

  const galleryHeight = height || 220;

  if (sources.length === 0) {
    return el(View, { style: [styles.uiGalleryRoot, styles.uiGalleryEmpty, { height: galleryHeight }] },
      el(Text, { style: styles.uiGalleryEmptyText }, emptyLabel || t("gallery.noImages", "No images yet"))
    );
  }

  return el(View, { style: styles.uiGalleryRoot },
    el(View, { style: [styles.uiGalleryImageWrap, { height: galleryHeight }] },
      el(FlatList, {
        ref: listRef,
        data: sources,
        horizontal: true,
        pagingEnabled: true,
        showsHorizontalScrollIndicator: false,
        keyExtractor: (_item, index) => String(index),
        getItemLayout: (_data, index) => ({ length: 320, offset: 320 * index, index }),
        onMomentumScrollEnd: syncGalleryIndex,
        onScrollToIndexFailed: () => {},
        renderItem: ({ item, index }) => el(Pressable, {
          style: { width: 320, height: galleryHeight },
          onPress: () => openFullscreen(index)
        }, el(Image, { source: item, style: styles.uiGalleryImage, resizeMode: "cover" }))
      }),
      sources.length > 1 && el(View, { style: styles.uiGalleryCounterBadge },
        el(Text, { style: styles.uiGalleryCounterText }, `${activeIndex + 1}/${sources.length}`)
      ),
      el(View, { style: styles.uiGalleryZoomHint },
        el(Text, { style: styles.uiGalleryZoomHintText }, t("gallery.tapToZoom", "Tap to zoom"))
      )
    ),
    sources.length > 1 && el(FlatList, {
      data: sources,
      horizontal: true,
      showsHorizontalScrollIndicator: false,
      keyExtractor: (_item, index) => `thumb-${index}`,
      contentContainerStyle: styles.uiGalleryThumbRail,
      renderItem: ({ item, index }) => el(Pressable, {
        style: [styles.uiGalleryThumb, index === activeIndex && styles.uiGalleryThumbActive],
        onPress: () => {
          setActiveIndex(index);
          listRef.current && listRef.current.scrollToIndex({ index, animated: true });
        }
      }, el(Image, { source: item, style: styles.uiGalleryThumbImage, resizeMode: "cover" }))
    }),
    el(Modal, { visible: isFullscreen, transparent: true, animationType: "fade", onRequestClose: closeFullscreen },
      el(View, { style: { flex: 1, backgroundColor: "#000000" } },
        el(FlatList, {
          ref: fullscreenListRef,
          data: sources,
          horizontal: true,
          pagingEnabled: true,
          initialScrollIndex: activeIndex,
          showsHorizontalScrollIndicator: false,
          keyExtractor: (_item, index) => `full-${index}`,
          getItemLayout: (_data, index, width) => ({ length: width || 320, offset: (width || 320) * index, index }),
          onMomentumScrollEnd: syncGalleryIndex,
          onScrollToIndexFailed: () => {},
          renderItem: ({ item }) => el(View, {
            style: { flex: 1, alignItems: "center", justifyContent: "center" },
            ...zoomResponder.panHandlers
          },
            el(Pressable, { style: { flex: 1, width: "100%" }, onPress: toggleZoom },
              el(Animated.Image, {
                source: item,
                resizeMode: "contain",
                style: {
                  flex: 1,
                  width: "100%",
                  transform: [
                    { translateX: zoomPan.x },
                    { translateY: zoomPan.y },
                    { scale: zoomScale }
                  ]
                }
              })
            )
          )
        }),
        el(Pressable, {
          style: { position: "absolute", top: (insets.top || 0) + 10, right: 16, width: 36, height: 36, borderRadius: 18, alignItems: "center", justifyContent: "center", backgroundColor: "rgba(255,255,255,0.16)" },
          onPress: closeFullscreen,
          hitSlop: 8
        }, el(Text, { style: { color: "#ffffff", fontSize: 16, fontWeight: "900" } }, "✕")),
        sources.length > 1 && el(View, { style: { position: "absolute", bottom: (insets.bottom || 0) + 16, left: 0, right: 0, alignItems: "center" } },
          el(Text, { style: { color: "#ffffff", fontSize: 12, fontWeight: "800" } }, `${activeIndex + 1} / ${sources.length}`)
        )
      )
    )
  );
}

module.exports = { ImageGallery };
