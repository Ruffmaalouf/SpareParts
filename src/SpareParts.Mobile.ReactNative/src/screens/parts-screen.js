const React = require("react");
const { ScrollView, View } = require("react-native");
const { money } = require("../core/formatters");
const { CommunicationPayloadFactory } = require("../core/communication-payload-factory");
const { Field, ListRow, Panel, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

function PartsScreen({ api }) {
  const { styles, t } = useTheme();
  const [parts, setParts] = useState([]);
  const [filter, setFilter] = useState("");
  const [recipientName, setRecipientName] = useState("");
  const [recipientPhone, setRecipientPhone] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("parts.loading", "Loading parts..."));
    try {
      setParts(await api.get("/api/parts?page=1&pageSize=120"));
      setStatus(t("parts.loaded", "Parts loaded."));
    } catch (error) {
      setStatus(error.message || t("parts.loadError", "Could not load parts."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  useEffect(() => { load(); }, [load]);

  const visibleParts = useMemo(() => {
    const query = filter.trim().toLowerCase();
    if (!query) return parts;
    return parts.filter((part) =>
      [part.internalCode, part.name, part.oemNumber].some((value) =>
        String(value || "").toLowerCase().includes(query)
      )
    );
  }, [filter, parts]);

  const sendAvailability = useCallback(async (part) => {
    if (!recipientPhone.trim()) {
      setStatus(t("parts.enterPhone", "Enter a WhatsApp phone number first."));
      return;
    }

    setStatus(t("parts.sending", "Sending availability..."));
    try {
      await api.post("/api/communications/send", CommunicationPayloadFactory.partAvailability({
        recipientName: recipientName || t("contacts.customers", "Customer"),
        recipientPhone,
        part
      }));
      setStatus(t("parts.sent", "Availability sent."));
    } catch (error) {
      setStatus(error.message || t("parts.sendError", "Could not send availability."));
    }
  }, [api, recipientName, recipientPhone, t]);

  return el(ScreenScroll, null,
    el(ScreenHeader, { title: t("parts.title", "Parts"), actionTitle: t("common.refresh", "Refresh"), onAction: load, loading: isLoading }),
    el(Field, { label: t("common.filter", "Filter"), value: filter, onChangeText: setFilter }),
    el(Field, { label: t("parts.recipientName", "Recipient name"), value: recipientName, onChangeText: setRecipientName }),
    el(Field, { label: t("parts.whatsappPhone", "WhatsApp phone"), value: recipientPhone, onChangeText: setRecipientPhone, keyboardType: "phone-pad" }),
    el(StatusText, { value: status }),
    el(Panel, { title: t("parts.inventory", "Inventory") },
      el(View, { style: styles.screenListFrameLarge },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          visibleParts.map((part) => el(View, { key: String(part.id), style: styles.screenListItem },
            el(ListRow, { title: part.name, subtitle: `${part.internalCode || t("parts.noCode", "No code")} - ${part.oemNumber || t("parts.noOem", "No OEM")}`, value: money(part.salePrice, part.currency) }),
            el(SecondaryButton, { title: t("parts.sendAvailability", "Send Availability"), onPress: () => sendAvailability(part) })
          )),
          visibleParts.length === 0 && el(ListRow, { title: t("parts.noParts", "No parts found"), subtitle: t("parts.tryFilter", "Try a different filter.") })
        )
      )
    )
  );
}

module.exports = { PartsScreen };
