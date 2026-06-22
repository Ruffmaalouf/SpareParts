const React = require("react");
const { Image, Text, View } = require("react-native");
const { useTheme } = require("../../theme/theme-context");
const { Card } = require("./Card");
const { StatusPill } = require("./StatusPill");

const el = React.createElement;

/**
 * Card-based vehicle row/tile for used-cars / used-car-wholesale style
 * lists: hero image, title + price above the fold, meta row (year/odometer/
 * location), status pill, and an optional footer slot for quick actions.
 */
function VehicleListCard({ title, priceText, imageUri, metaItems, statusLabel, statusTone, footer, onPress }) {
  const { styles, palette } = useTheme();

  return el(Card, { onPress, style: styles.uiVehicleCard },
    el(View, { style: styles.uiVehicleCardImage },
      imageUri
        ? el(Image, { source: { uri: imageUri }, style: { width: "100%", height: "100%" }, resizeMode: "cover" })
        : el(View, { style: [styles.uiVehicleCardImage, styles.uiVehicleCardImagePlaceholder] },
          el(Text, { style: { color: palette.muted, fontSize: 12, fontWeight: "700" } }, "No photo")
        )
    ),
    el(View, { style: styles.uiVehicleCardBody },
      el(View, { style: styles.uiVehicleCardTitleRow },
        el(Text, { style: styles.uiVehicleCardTitle, numberOfLines: 1 }, title),
        Boolean(priceText) && el(Text, { style: styles.uiVehicleCardPrice }, priceText)
      ),
      Boolean(metaItems && metaItems.length) && el(View, { style: styles.uiVehicleCardMetaRow },
        metaItems.filter(Boolean).map((item, index) => el(Text, { key: index, style: styles.uiVehicleCardMetaItem }, item))
      ),
      (Boolean(statusLabel) || Boolean(footer)) && el(View, { style: styles.uiVehicleCardFooterRow },
        Boolean(statusLabel) && el(StatusPill, { label: statusLabel, tone: statusTone }),
        footer || null
      )
    )
  );
}

module.exports = { VehicleListCard };
