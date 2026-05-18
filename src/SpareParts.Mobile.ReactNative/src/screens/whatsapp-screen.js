const React = require("react");
const {
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  Text,
  TextInput,
  View
} = require("react-native");
const { initials, shortDateTime } = require("../core/formatters");
const { CommunicationPayloadFactory } = require("../core/communication-payload-factory");
const { EmptyState, Field, PrimaryButton, ScreenHeader, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

const campaignSegments = [
  { value: "AllCustomers", label: "All" },
  { value: "ActiveCustomers", label: "Active" },
  { value: "UnpaidCustomers", label: "Unpaid" },
  { value: "WaitingForParts", label: "Waiting" },
  { value: "RecentBuyers", label: "Recent" },
  { value: "InactiveCustomers", label: "Inactive" }
];

const campaignLanguages = [
  { value: "ArabicEnglish", label: "AR + EN" },
  { value: "Arabic", label: "Arabic" },
  { value: "English", label: "English" }
];

function assetKey(asset) {
  return `${asset.assetType}:${asset.id}`;
}

function money(value, currency = "USD") {
  if (value === null || value === undefined) return "";
  return `${currency || "USD"} ${Number(value || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function WhatsAppScreen({ api }) {
  const { styles, palette, t } = useTheme();
  const [conversations, setConversations] = useState([]);
  const [selectedPhone, setSelectedPhone] = useState("");
  const [messages, setMessages] = useState([]);
  const [manualName, setManualName] = useState("");
  const [manualPhone, setManualPhone] = useState("");
  const [compose, setCompose] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [mode, setMode] = useState("conversations");
  const [campaignAssets, setCampaignAssets] = useState([]);
  const [selectedAssetKeys, setSelectedAssetKeys] = useState([]);
  const [recentCampaigns, setRecentCampaigns] = useState([]);
  const [campaignSegment, setCampaignSegment] = useState("AllCustomers");
  const [campaignLanguage, setCampaignLanguage] = useState("ArabicEnglish");
  const [campaignIncludeImages, setCampaignIncludeImages] = useState(true);
  const [campaignName, setCampaignName] = useState("");
  const [campaignNote, setCampaignNote] = useState("");
  const [campaignMessage, setCampaignMessage] = useState("");
  const [campaignPreview, setCampaignPreview] = useState({ recipientCount: 0, attachmentCount: 0, recipients: [] });

  const selectedConversation = useMemo(
    () => conversations.find((conversation) => conversation.recipientPhone === selectedPhone) || null,
    [conversations, selectedPhone]
  );
  const selectedAssets = useMemo(
    () => campaignAssets.filter((asset) => selectedAssetKeys.includes(assetKey(asset))),
    [campaignAssets, selectedAssetKeys]
  );

  const loadMessages = useCallback(async (phone) => {
    if (!phone) {
      setMessages([]);
      return;
    }

    setIsLoading(true);
    setStatus(t("whatsapp.loadingThread", "Loading thread..."));
    try {
      setMessages(await api.get(`/api/communications/messages?phone=${encodeURIComponent(phone)}&take=250`));
      setStatus(t("whatsapp.threadLoaded", "Thread loaded."));
    } catch (error) {
      setStatus(error.message || t("whatsapp.threadLoadError", "Could not load thread."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  const loadConversations = useCallback(async (preferredPhone) => {
    setIsLoading(true);
    setStatus(t("whatsapp.loading", "Loading WhatsApp..."));
    try {
      const rows = await api.get("/api/communications/conversations?take=100");
      setConversations(rows);
      const nextPhone = preferredPhone || selectedPhone || rows[0]?.recipientPhone || "";
      setSelectedPhone(nextPhone);
      if (nextPhone) {
        await loadMessages(nextPhone);
      }
      setStatus(rows.length === 0 ? t("whatsapp.noConversations", "No conversations yet.") : t("whatsapp.loaded", "Conversations loaded."));
    } catch (error) {
      setStatus(error.message || t("whatsapp.loadError", "Could not load conversations."));
    } finally {
      setIsLoading(false);
    }
  }, [api, loadMessages, selectedPhone, t]);

  const loadCampaignBuilder = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("whatsapp.campaignLoading", "Loading campaign builder..."));
    try {
      const [assets, campaigns] = await Promise.all([
        api.get("/api/communications/campaign-assets"),
        api.get("/api/communications/campaigns/recent?take=30")
      ]);
      setCampaignAssets(assets);
      setSelectedAssetKeys((current) => current.filter((key) => assets.some((asset) => assetKey(asset) === key)));
      setRecentCampaigns(campaigns);
      setStatus(t("whatsapp.campaignLoaded", "Campaign builder loaded."));
    } catch (error) {
      setStatus(error.message || t("whatsapp.campaignLoadError", "Could not load campaigns."));
    } finally {
      setIsLoading(false);
    }
  }, [api, t]);

  const buildCampaignRequest = useCallback(() => {
    const selected = campaignAssets.filter((asset) => selectedAssetKeys.includes(assetKey(asset)));
    return {
      segment: campaignSegment,
      language: campaignLanguage,
      includeImages: campaignIncludeImages,
      note: campaignNote,
      maxRecipients: 100,
      partIds: selected.filter((asset) => asset.assetType === "Part").map((asset) => asset.id),
      usedCarIds: selected.filter((asset) => asset.assetType === "Car").map((asset) => asset.id)
    };
  }, [campaignAssets, campaignIncludeImages, campaignLanguage, campaignNote, campaignSegment, selectedAssetKeys]);

  const previewCampaign = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("whatsapp.campaignPreviewing", "Generating campaign message..."));
    try {
      const response = await api.post("/api/communications/campaign-preview", buildCampaignRequest());
      setCampaignMessage(response.messageBody || "");
      setCampaignPreview(response);
      setStatus(t("whatsapp.campaignPreviewReady", "Preview ready for {count} customer(s).", { count: response.recipientCount || 0 }));
    } catch (error) {
      setStatus(error.message || t("whatsapp.campaignPreviewError", "Could not preview campaign."));
    } finally {
      setIsLoading(false);
    }
  }, [api, buildCampaignRequest, t]);

  const sendCampaign = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("whatsapp.campaignSending", "Sending campaign..."));
    try {
      const response = await api.post("/api/communications/campaigns/send", {
        ...buildCampaignRequest(),
        name: campaignName,
        messageBodyOverride: campaignMessage
      });
      setCampaignPreview({
        recipientCount: response.recipientCount || 0,
        attachmentCount: response.attachmentCount || 0,
        recipients: []
      });
      setCampaignMessage(response.messageBody || campaignMessage);
      await loadCampaignBuilder();
      await loadConversations();
      setStatus(t("whatsapp.campaignSent", "Campaign {status}: {sent} prepared/sent, {failed} failed.", {
        status: String(response.status || "sent").toLowerCase(),
        sent: (response.sentCount || 0) + (response.preparedCount || 0),
        failed: response.failedCount || 0
      }));
    } catch (error) {
      setStatus(error.message || t("whatsapp.campaignSendError", "Could not send campaign."));
    } finally {
      setIsLoading(false);
    }
  }, [api, buildCampaignRequest, campaignMessage, campaignName, loadCampaignBuilder, loadConversations, t]);

  const toggleAsset = useCallback((asset) => {
    const key = assetKey(asset);
    setSelectedAssetKeys((current) =>
      current.includes(key)
        ? current.filter((item) => item !== key)
        : [...current, key]
    );
  }, []);

  useEffect(() => {
    loadConversations();
    loadCampaignBuilder();
  }, []);

  const send = useCallback(async () => {
    const phone = (selectedConversation?.recipientPhone || manualPhone).trim();
    const name = (selectedConversation?.recipientName || manualName || t("whatsapp.customerFallback", "Customer")).trim();
    const body = compose.trim();

    if (!phone) {
      setStatus(t("whatsapp.enterPhone", "Enter a WhatsApp phone number."));
      return;
    }

    if (!body) {
      setStatus(t("whatsapp.typeMessage", "Type a message first."));
      return;
    }

    setIsLoading(true);
    setStatus(t("whatsapp.sending", "Sending message..."));
    try {
      const response = await api.post("/api/communications/send", CommunicationPayloadFactory.freeText({
        recipientName: name,
        recipientPhone: phone,
        body
      }));
      setCompose("");
      setManualName(response.recipientName);
      setManualPhone(response.recipientPhone);
      await loadConversations(response.recipientPhone);
      setStatus(t("whatsapp.messageStatus", "Message {status}.", { status: String(response.status || t("whatsapp.sent", "sent")).toLowerCase() }));
    } catch (error) {
      setStatus(error.message || t("whatsapp.sendError", "Could not send message."));
    } finally {
      setIsLoading(false);
    }
  }, [api, compose, loadConversations, manualName, manualPhone, selectedConversation, t]);

  const selectConversation = useCallback((conversation) => {
    setSelectedPhone(conversation.recipientPhone);
    setManualName(conversation.recipientName);
    setManualPhone(conversation.recipientPhone);
    loadMessages(conversation.recipientPhone);
  }, [loadMessages]);

  const segmentButton = (value, label, selectedValue, onPress) => el(Pressable, {
    key: value,
    style: [styles.segmentButton, selectedValue === value && styles.segmentButtonActive],
    onPress: () => onPress(value)
  }, el(Text, { style: [styles.segmentText, selectedValue === value && styles.segmentTextActive] }, label));

  const renderCampaignBuilder = () => el(ScrollView, {
    style: styles.threadList,
    contentContainerStyle: styles.whatsAppCampaignContent,
    keyboardShouldPersistTaps: "handled"
  },
    el(View, { style: styles.newChatPanel },
      el(Text, { style: styles.panelTitle }, t("whatsapp.campaignSetup", "Campaign setup")),
      el(Text, { style: styles.fieldLabel }, t("whatsapp.segment", "Segment")),
      el(ScrollView, { horizontal: true, showsHorizontalScrollIndicator: false, contentContainerStyle: styles.segmentRail },
        campaignSegments.map((segment) => segmentButton(segment.value, segment.label, campaignSegment, setCampaignSegment))
      ),
      el(Text, { style: styles.fieldLabel }, t("whatsapp.language", "Language")),
      el(ScrollView, { horizontal: true, showsHorizontalScrollIndicator: false, contentContainerStyle: styles.segmentRail },
        campaignLanguages.map((language) => segmentButton(language.value, language.label, campaignLanguage, setCampaignLanguage))
      ),
      el(Field, { label: t("whatsapp.campaignName", "Campaign name"), value: campaignName, onChangeText: setCampaignName, placeholder: t("common.optional", "Optional") }),
      el(Field, { label: t("whatsapp.note", "Extra note"), value: campaignNote, onChangeText: setCampaignNote, multiline: true, numberOfLines: 3 }),
      el(Pressable, {
        style: [styles.whatsAppToggle, campaignIncludeImages && styles.whatsAppToggleActive],
        onPress: () => setCampaignIncludeImages((value) => !value)
      },
        el(Text, { style: styles.whatsAppToggleText }, t("whatsapp.attachImages", "Attach used-car images")),
        el(Text, { style: styles.whatsAppToggleValue }, campaignIncludeImages ? t("common.yes", "Yes") : t("common.no", "No"))
      ),
      el(View, { style: styles.inlineButtons },
        el(PrimaryButton, { title: t("whatsapp.preview", "Preview"), onPress: previewCampaign, disabled: isLoading, compact: true }),
        el(PrimaryButton, { title: t("whatsapp.sendCampaign", "Send campaign"), onPress: sendCampaign, disabled: isLoading, compact: true })
      )
    ),
    el(View, { style: styles.whatsAppCampaignStats },
      el(View, { style: styles.whatsAppCampaignStat }, el(Text, { style: styles.usedCarDetailValue }, String(campaignPreview.recipientCount || 0)), el(Text, { style: styles.usedCarDetailLabel }, t("whatsapp.recipients", "Recipients"))),
      el(View, { style: styles.whatsAppCampaignStat }, el(Text, { style: styles.usedCarDetailValue }, String(campaignPreview.attachmentCount || 0)), el(Text, { style: styles.usedCarDetailLabel }, t("whatsapp.images", "Images"))),
      el(View, { style: styles.whatsAppCampaignStat }, el(Text, { style: styles.usedCarDetailValue }, String(selectedAssets.length)), el(Text, { style: styles.usedCarDetailLabel }, t("whatsapp.assets", "Assets")))
    ),
    el(View, { style: styles.newChatPanel },
      el(Text, { style: styles.panelTitle }, t("whatsapp.partsCars", "Parts and cars")),
      campaignAssets.map((asset) => {
        const selected = selectedAssetKeys.includes(assetKey(asset));
        return el(Pressable, {
          key: assetKey(asset),
          style: [styles.whatsAppCampaignAsset, selected && styles.whatsAppCampaignAssetActive],
          onPress: () => toggleAsset(asset)
        },
          el(View, { style: styles.usedCarInventoryMark }, el(Text, { style: styles.usedCarInventoryMarkText }, asset.assetType === "Car" ? "CAR" : "PART")),
          el(View, { style: styles.usedCarInventoryCopy },
            el(Text, { style: styles.usedCarCardTitle, numberOfLines: 1 }, asset.title),
            el(Text, { style: styles.usedCarCardMeta, numberOfLines: 2 }, asset.subtitle)
          ),
          el(Text, { style: styles.usedCarInventoryValue }, money(asset.price, asset.currency))
        );
      })
    ),
    el(View, { style: styles.newChatPanel },
      el(Text, { style: styles.panelTitle }, t("whatsapp.generatedMessage", "Generated message")),
      el(TextInput, {
        style: styles.whatsAppCampaignMessage,
        value: campaignMessage,
        onChangeText: setCampaignMessage,
        placeholder: t("whatsapp.previewPlaceholder", "Preview generates the Arabic/English message here."),
        placeholderTextColor: palette.soft,
        multiline: true
      })
    ),
    el(View, { style: styles.newChatPanel },
      el(Text, { style: styles.panelTitle }, t("whatsapp.previewRecipients", "Preview recipients")),
      (campaignPreview.recipients || []).slice(0, 10).map((recipient) =>
        el(View, { key: recipient.phone, style: styles.listRow },
          el(View, { style: styles.listRowCopy },
            el(Text, { style: styles.listRowTitle }, recipient.name),
            el(Text, { style: styles.listRowSubtitle }, recipient.phone)
          )
        )
      ),
      (!campaignPreview.recipients || campaignPreview.recipients.length === 0) && el(EmptyState, { text: t("whatsapp.noPreviewRecipients", "Preview a campaign to see recipients.") })
    ),
    el(View, { style: styles.newChatPanel },
      el(Text, { style: styles.panelTitle }, t("whatsapp.recentCampaigns", "Recent campaigns")),
      recentCampaigns.slice(0, 8).map((campaign) =>
        el(View, { key: String(campaign.id), style: styles.listRow },
          el(View, { style: styles.listRowCopy },
            el(Text, { style: styles.listRowTitle }, campaign.name),
            el(Text, { style: styles.listRowSubtitle }, `${campaign.recipientCount || 0} sent · ${campaign.replyCount || 0} replies · ${campaign.salesCount || 0} sales`)
          ),
          el(Text, { style: styles.listRowValue }, campaign.status)
        )
      )
    )
  );

  return el(View, { style: styles.flexScreen },
    el(ScreenHeader, {
      title: t("whatsapp.title", "WhatsApp"),
      actionTitle: t("common.refresh", "Refresh"),
      onAction: () => mode === "campaigns" ? loadCampaignBuilder() : loadConversations(),
      loading: isLoading
    }),
    el(StatusText, { value: status }),
    el(ScrollView, { horizontal: true, showsHorizontalScrollIndicator: false, contentContainerStyle: styles.segmentRail },
      segmentButton("conversations", t("whatsapp.conversations", "Conversations"), mode, setMode),
      segmentButton("campaigns", t("whatsapp.campaignBuilder", "Campaign Builder"), mode, setMode)
    ),
    mode === "campaigns" && renderCampaignBuilder(),
    mode === "conversations" && [
    el(View, { style: styles.newChatPanel },
      el(Field, { label: t("fields.name", "Name"), value: manualName, onChangeText: setManualName }),
      el(Field, { label: t("fields.phone", "Phone"), value: manualPhone, onChangeText: setManualPhone, keyboardType: "phone-pad" })
    ),
    el(FlatList, {
      horizontal: true,
      data: conversations,
      keyExtractor: (item) => item.recipientPhone,
      showsHorizontalScrollIndicator: false,
      contentContainerStyle: styles.conversationRail,
      renderItem: ({ item }) => el(Pressable, {
        style: [styles.conversationPill, item.recipientPhone === selectedPhone && styles.conversationPillActive],
        onPress: () => selectConversation(item)
      },
        el(View, { style: styles.avatar }, el(Text, { style: styles.avatarText }, initials(item.recipientName))),
        el(Text, { style: styles.conversationName }, item.recipientName || item.recipientPhone),
        el(Text, { style: styles.conversationPreview }, item.lastMessagePreview || "")
      )
    }),
    el(FlatList, {
      data: messages,
      keyExtractor: (item) => String(item.id),
      style: styles.threadList,
      contentContainerStyle: styles.threadContent,
      renderItem: ({ item }) => el(View, { style: [styles.messageBubble, item.direction === "Outbound" ? styles.messageBubbleOutbound : styles.messageBubbleInbound] },
        el(Text, { style: styles.messageBody }, item.body),
        el(Text, { style: styles.messageMeta }, `${shortDateTime(item.sentAt || item.createdAt)} · ${item.status}`)
      ),
      ListEmptyComponent: el(EmptyState, { text: t("whatsapp.noMessages", "No messages in this thread yet.") })
    }),
    el(KeyboardAvoidingView, { behavior: Platform.OS === "ios" ? "padding" : undefined },
      el(View, { style: styles.composer },
        el(TextInput, {
          style: styles.composerInput,
          value: compose,
          onChangeText: setCompose,
          placeholder: t("whatsapp.composePlaceholder", "Type a WhatsApp message"),
          placeholderTextColor: palette.soft,
          multiline: true
        }),
        el(PrimaryButton, { title: t("whatsapp.send", "Send"), onPress: send, compact: true })
      )
    )
    ]
  );
}

module.exports = { WhatsAppScreen };
