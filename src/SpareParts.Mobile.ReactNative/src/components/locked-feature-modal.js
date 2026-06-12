const React = require("react");
const { Modal, Pressable, Text, View } = require("react-native");
const { useTheme } = require("../theme/theme-context");

const el = React.createElement;

function LockedFeatureModal({ lock, onClose, onUpgrade }) {
  const { styles, t } = useTheme();
  if (!lock) return null;

  const isLimit = lock.errorCode === "PLAN_LIMIT_EXCEEDED";
  const title = isLimit
    ? t("billing.lockTitleLimit", "Plan limit reached")
    : t("billing.lockTitleFeature", "Feature not available on your plan");

  return el(Modal, {
    animationType: "fade",
    transparent: true,
    visible: true,
    onRequestClose: onClose
  },
    el(View, { style: styles.lockModalOverlay },
      el(View, { style: styles.lockModalCard },
        el(Text, { style: styles.lockModalTitle }, title),
        el(Text, { style: styles.lockModalMessage }, lock.message),
        lock.currentPlan && el(Text, { style: styles.lockModalMeta },
          `${t("billing.currentPlan", "Current plan")}: ${lock.currentPlan}`),
        lock.requiredPlans && lock.requiredPlans.length > 0 && el(Text, { style: styles.lockModalMeta },
          `${t("billing.requiredPlans", "Available on")}: ${lock.requiredPlans.join(", ")}`),
        el(View, { style: styles.lockModalActions },
          el(Pressable, { style: styles.secondaryButton, onPress: onClose },
            el(Text, { style: styles.secondaryButtonText }, t("common.close", "Close"))),
          el(Pressable, { style: styles.primaryButton, onPress: onUpgrade },
            el(Text, { style: styles.primaryButtonText }, t("billing.viewPlans", "View Plans")))
        )
      )
    )
  );
}

module.exports = { LockedFeatureModal };
