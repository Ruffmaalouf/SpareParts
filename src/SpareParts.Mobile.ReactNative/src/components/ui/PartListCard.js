const React = require("react");
const { Image, Pressable, Text, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");
const { Card } = require("./Card");
const { StatusPill } = require("./StatusPill");

const el = React.createElement;

function initialsFor(value) {
  return String(value || "?").trim().slice(0, 2).toUpperCase();
}

/**
 * Card-based part row, used by parts-screen.js / mechanic-mode-screen.js /
 * module-screen.js generic list rendering, replacing the previous
 * ad-hoc PartCard implementations duplicated per-screen.
 *
 * Props:
 *  - title, subtitle, priceText
 *  - imageUri: optional data-uri/remote uri string. Falls back to an
 *    initials thumbnail when absent.
 *  - badges: Array<{ label, tone }> rendered as StatusPills
 *  - actions: Array<{ key, icon, label, onPress }> rendered as small icon buttons
 *  - layout: "list" (default, full-width row) | "grid" (half-width card for a 2-col grid)
 *  - onPress: opens detail (e.g. BottomSheet)
 */
function PartListCard({ title, subtitle, priceText, imageUri, badges, actions, layout, onPress }) {
  const { styles } = useTheme();
  const isGrid = layout === "grid";

  const thumb = el(View, { style: isGrid ? [styles.uiPartCardThumb, styles.uiPartCardGridThumb] : styles.uiPartCardThumb },
    imageUri
      ? el(Image, { source: { uri: imageUri }, style: styles.uiPartCardThumbImage, resizeMode: "cover" })
      : el(Text, { style: styles.uiPartCardThumbText }, initialsFor(title))
  );

  const badgeRow = Boolean(badges && badges.length) && el(View, { style: styles.uiPartCardBadgeRow },
    badges.map((badge, index) => el(StatusPill, { key: `${badge.label}-${index}`, label: badge.label, tone: badge.tone }))
  );

  const actionRow = Boolean(actions && actions.length) && el(View, { style: styles.uiPartCardActionsRow },
    actions.map((action) => el(Pressable, {
      key: action.key || action.label,
      style: styles.uiPartCardIconButton,
      onPress: action.onPress,
      hitSlop: 4
    }, el(Text, { style: styles.uiPartCardIconButtonText }, action.icon || action.label)))
  );

  if (isGrid) {
    return el(Card, { onPress, compact: true, style: styles.uiPartCardGridItem },
      thumb,
      el(Text, { style: styles.uiPartCardTitle, numberOfLines: 1 }, title),
      Boolean(subtitle) && el(Text, { style: styles.uiPartCardSubtitle, numberOfLines: 1 }, subtitle),
      badgeRow,
      Boolean(priceText) && el(Text, { style: styles.uiPartCardPrice }, priceText),
      actionRow
    );
  }

  return el(Card, { onPress, style: styles.uiPartCard },
    thumb,
    el(View, { style: styles.uiPartCardBody },
      el(Text, { style: styles.uiPartCardTitle, numberOfLines: 1 }, title),
      Boolean(subtitle) && el(Text, { style: styles.uiPartCardSubtitle, numberOfLines: 1 }, subtitle),
      badgeRow
    ),
    el(View, { style: styles.uiPartCardSide },
      Boolean(priceText) && el(Text, { style: styles.uiPartCardPrice }, priceText),
      actionRow
    )
  );
}

module.exports = { PartListCard };
