const React = require("react");
const {
  Animated,
  Dimensions,
  Modal,
  PanResponder,
  Pressable,
  ScrollView,
  Text,
  View
} = require("react-native");
const { useSafeAreaInsets } = require("react-native-safe-area-context");
const { useTheme } = require("../../theme/theme-context");

const { useEffect, useMemo, useRef } = React;
const el = React.createElement;

/**
 * Real slide-up bottom sheet built with Animated + PanResponder (no
 * react-native-gesture-handler/reanimated available in this project).
 * Used for module detail/edit flows, replacing top-stacked inline forms.
 *
 * Props:
 *  - visible: boolean
 *  - onClose: () => void
 *  - title: string
 *  - children: sheet body content (wrapped in a ScrollView unless `scroll` is false)
 */
function BottomSheet({ visible, onClose, title, children, scroll }) {
  const { styles, palette } = useTheme();
  const insets = useSafeAreaInsets();
  const screenHeight = Dimensions.get("window").height;
  const translateY = useRef(new Animated.Value(screenHeight)).current;
  const backdropOpacity = useRef(new Animated.Value(0)).current;

  useEffect(() => {
    if (visible) {
      translateY.setValue(screenHeight);
      Animated.parallel([
        Animated.spring(translateY, { toValue: 0, useNativeDriver: true, bounciness: 4 }),
        Animated.timing(backdropOpacity, { toValue: 1, duration: 200, useNativeDriver: true })
      ]).start();
    }
  }, [visible]);

  const close = () => {
    Animated.parallel([
      Animated.timing(translateY, { toValue: screenHeight, duration: 220, useNativeDriver: true }),
      Animated.timing(backdropOpacity, { toValue: 0, duration: 200, useNativeDriver: true })
    ]).start(() => onClose && onClose());
  };

  const panResponder = useMemo(() => PanResponder.create({
    onMoveShouldSetPanResponder: (_event, gesture) => Math.abs(gesture.dy) > 6 && Math.abs(gesture.dy) > Math.abs(gesture.dx),
    onPanResponderMove: (_event, gesture) => {
      if (gesture.dy > 0) translateY.setValue(gesture.dy);
    },
    onPanResponderRelease: (_event, gesture) => {
      if (gesture.dy > 110 || gesture.vy > 1.1) {
        close();
        return;
      }
      Animated.spring(translateY, { toValue: 0, useNativeDriver: true, bounciness: 4 }).start();
    }
  }), []);

  if (!visible) return null;

  const BodyWrapper = scroll === false ? View : ScrollView;
  const bodyProps = scroll === false
    ? { style: styles.uiSheetBody }
    : { contentContainerStyle: styles.uiSheetBody, keyboardShouldPersistTaps: "handled" };

  return el(Modal, { visible: true, transparent: true, animationType: "none", onRequestClose: close },
    el(Animated.View, { style: [styles.uiSheetBackdrop, { opacity: backdropOpacity }] },
      el(Pressable, { style: { flex: 1 }, onPress: close }),
      el(Animated.View, {
        style: [
          styles.uiSheetCard,
          { paddingBottom: (insets.bottom || 0) + 8, transform: [{ translateY }] }
        ]
      },
        el(View, { ...panResponder.panHandlers },
          el(View, { style: styles.uiSheetHandleRow }, el(View, { style: styles.uiSheetHandle })),
          el(View, { style: styles.uiSheetHeaderRow },
            el(Text, { style: styles.uiSheetTitle, numberOfLines: 1 }, title),
            el(Pressable, { style: styles.uiSheetCloseButton, onPress: close, hitSlop: 6 },
              el(Text, { style: styles.uiSheetCloseText }, "✕")
            )
          )
        ),
        el(BodyWrapper, bodyProps, children)
      )
    )
  );
}

module.exports = { BottomSheet };
