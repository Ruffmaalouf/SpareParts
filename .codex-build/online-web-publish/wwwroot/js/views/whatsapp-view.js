import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { dateTime, initials } from "../core/formatters.js";
import { CommunicationPayloadFactory } from "../services/communication-payload-factory.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const campaignSegments = [
  { value: "AllCustomers", label: "All customers" },
  { value: "ActiveCustomers", label: "Active" },
  { value: "UnpaidCustomers", label: "Unpaid" },
  { value: "WaitingForParts", label: "Waiting parts" },
  { value: "RecentBuyers", label: "Recent buyers" },
  { value: "InactiveCustomers", label: "Inactive" }
];

const campaignLanguages = [
  { value: "ArabicEnglish", label: "Arabic + English" },
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

export function WhatsAppView({ api }) {
  const [conversations, setConversations] = useState([]);
  const [selectedPhone, setSelectedPhone] = useState("");
  const [messages, setMessages] = useState([]);
  const [search, setSearch] = useState("");
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
    () => conversations.find((item) => item.recipientPhone === selectedPhone) || null,
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
    setStatus("Loading thread...");
    try {
      setMessages(await api.get(`/api/communications/messages?phone=${encodeURIComponent(phone)}&take=250`));
      setStatus("Thread loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load thread.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

  const loadConversations = useCallback(async (preferredPhone) => {
    setIsLoading(true);
    setStatus("Loading conversations...");
    try {
      const query = search.trim() ? `&search=${encodeURIComponent(search.trim())}` : "";
      const rows = await api.get(`/api/communications/conversations?take=100${query}`);
      setConversations(rows);
      const nextPhone = preferredPhone || selectedPhone || rows[0]?.recipientPhone || "";
      setSelectedPhone(nextPhone);
      if (nextPhone) {
        await loadMessages(nextPhone);
      } else {
        setMessages([]);
      }
      setStatus(rows.length ? "Conversations loaded." : "No conversations yet.");
    } catch (error) {
      setStatus(error.message || "Could not load conversations.");
    } finally {
      setIsLoading(false);
    }
  }, [api, loadMessages, search, selectedPhone]);

  const loadCampaignBuilder = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading campaign builder...");
    try {
      const [assets, campaigns] = await Promise.all([
        api.get("/api/communications/campaign-assets"),
        api.get("/api/communications/campaigns/recent?take=30")
      ]);
      setCampaignAssets(assets);
      setSelectedAssetKeys((current) => current.filter((key) => assets.some((asset) => assetKey(asset) === key)));
      setRecentCampaigns(campaigns);
      setStatus("Campaign builder loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load campaigns.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);

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
    setStatus("Generating campaign message...");
    try {
      const response = await api.post("/api/communications/campaign-preview", buildCampaignRequest());
      setCampaignMessage(response.messageBody || "");
      setCampaignPreview(response);
      setStatus(`Preview ready for ${Number(response.recipientCount || 0).toLocaleString()} customer(s).`);
    } catch (error) {
      setStatus(error.message || "Could not preview campaign.");
    } finally {
      setIsLoading(false);
    }
  }, [api, buildCampaignRequest]);

  const sendCampaign = useCallback(async () => {
    setIsLoading(true);
    setStatus("Sending campaign...");
    try {
      const request = {
        ...buildCampaignRequest(),
        name: campaignName,
        messageBodyOverride: campaignMessage
      };
      const response = await api.post("/api/communications/campaigns/send", request);
      setCampaignPreview({
        recipientCount: response.recipientCount || 0,
        attachmentCount: response.attachmentCount || 0,
        recipients: []
      });
      setCampaignMessage(response.messageBody || campaignMessage);
      await loadCampaignBuilder();
      await loadConversations();
      setStatus(`Campaign ${String(response.status || "sent").toLowerCase()}: ${(response.sentCount || 0) + (response.preparedCount || 0)} prepared/sent, ${response.failedCount || 0} failed.`);
    } catch (error) {
      setStatus(error.message || "Could not send campaign.");
    } finally {
      setIsLoading(false);
    }
  }, [api, buildCampaignRequest, campaignMessage, campaignName, loadCampaignBuilder, loadConversations]);

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

  const pickConversation = useCallback((conversation) => {
    setSelectedPhone(conversation.recipientPhone);
    setManualName(conversation.recipientName);
    setManualPhone(conversation.recipientPhone);
    loadMessages(conversation.recipientPhone);
  }, [loadMessages]);

  const startManual = useCallback(() => {
    setSelectedPhone("");
    setMessages([]);
    setStatus(manualPhone.trim() ? "Ready to send a new WhatsApp message." : "Enter a phone number first.");
  }, [manualPhone]);

  const send = useCallback(async () => {
    const phone = (selectedConversation?.recipientPhone || manualPhone).trim();
    const name = (selectedConversation?.recipientName || manualName || "Customer").trim();
    const body = compose.trim();
    if (!phone) {
      setStatus("Enter a WhatsApp phone number.");
      return;
    }
    if (!body) {
      setStatus("Type a message first.");
      return;
    }

    setIsLoading(true);
    setStatus("Sending WhatsApp message...");
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
      setStatus(`Message ${String(response.status || "sent").toLowerCase()}.`);
    } catch (error) {
      setStatus(error.message || "Could not send message.");
    } finally {
      setIsLoading(false);
    }
  }, [api, compose, loadConversations, manualName, manualPhone, selectedConversation]);

  const activeName = selectedConversation?.recipientName || manualName || "New WhatsApp chat";
  const activePhone = selectedConversation?.recipientPhone || manualPhone || "No phone selected";
  const renderConversations = () => h("div", { className: "chat-layout" },
    h("aside", { className: "chat-list" },
      h("div", { className: "chat-search" },
        h("input", {
          value: search,
          onChange: (event) => setSearch(event.target.value),
          onKeyDown: (event) => event.key === "Enter" && loadConversations(),
          placeholder: "Search conversations"
        }),
        h("button", { onClick: () => loadConversations() }, "Search")
      ),
      h("div", { className: "new-chat" },
        h("label", null, "Name", h("input", { value: manualName, onChange: (event) => setManualName(event.target.value) })),
        h("label", null, "Phone", h("input", { value: manualPhone, onChange: (event) => setManualPhone(event.target.value) })),
        h("button", { className: "primary-button", onClick: startManual }, "Start Chat")
      ),
      h("div", { className: "conversation-stack" },
        conversations.map((conversation) =>
          h("button", {
            key: conversation.recipientPhone,
            className: conversation.recipientPhone === selectedPhone ? "conversation active" : "conversation",
            onClick: () => pickConversation(conversation)
          },
            h("span", { className: "avatar" }, initials(conversation.recipientName)),
            h("span", { className: "conversation-main" },
              h("strong", null, conversation.recipientName || conversation.recipientPhone),
              h("small", null, conversation.lastMessagePreview || "No preview")
            ),
            h("time", null, dateTime(conversation.lastMessageAt))
          )
        )
      )
    ),
    h("section", { className: "thread" },
      h("header", { className: "thread-header" },
        h("div", null, h("strong", null, activeName), h("span", null, activePhone)),
        isLoading && h("span", { className: "loading-pill" }, "Loading")
      ),
      h("div", { className: "message-stack" },
        messages.map((message) =>
          h("article", { key: message.id, className: message.direction === "Outbound" ? "bubble outbound" : "bubble inbound" },
            h("p", null, message.body),
            h("footer", null, h("span", null, dateTime(message.sentAt || message.createdAt)), h("span", null, message.status))
          )
        ),
        messages.length === 0 && h("p", { className: "empty-state" }, "No messages in this thread yet.")
      ),
      h("footer", { className: "composer" },
        h("textarea", {
          value: compose,
          onChange: (event) => setCompose(event.target.value),
          placeholder: "Type a WhatsApp message"
        }),
        h("button", { className: "primary-button", onClick: send }, "Send")
      )
    )
  );

  const renderCampaignBuilder = () => h("div", { className: "campaign-layout" },
    h("aside", { className: "campaign-control-panel" },
      h("div", { className: "campaign-section-title" },
        h("strong", null, "Campaign setup"),
        h("button", { className: "secondary-button", onClick: loadCampaignBuilder, disabled: isLoading }, "Refresh")
      ),
      h("label", null, "Segment",
        h("select", { value: campaignSegment, onChange: (event) => setCampaignSegment(event.target.value) },
          campaignSegments.map((segment) => h("option", { key: segment.value, value: segment.value }, segment.label))
        )
      ),
      h("label", null, "Language",
        h("select", { value: campaignLanguage, onChange: (event) => setCampaignLanguage(event.target.value) },
          campaignLanguages.map((language) => h("option", { key: language.value, value: language.value }, language.label))
        )
      ),
      h("label", null, "Campaign name",
        h("input", { value: campaignName, onChange: (event) => setCampaignName(event.target.value), placeholder: "Optional" })
      ),
      h("label", null, "Extra note",
        h("textarea", { value: campaignNote, onChange: (event) => setCampaignNote(event.target.value), placeholder: "Optional line added to the message" })
      ),
      h("label", { className: "toggle-row" },
        h("input", { type: "checkbox", checked: campaignIncludeImages, onChange: (event) => setCampaignIncludeImages(event.target.checked) }),
        h("span", null, "Attach used-car images")
      ),
      h("div", { className: "campaign-actions" },
        h("button", { className: "secondary-button", onClick: previewCampaign, disabled: isLoading }, "Preview"),
        h("button", { className: "primary-button", onClick: sendCampaign, disabled: isLoading }, "Send")
      ),
      h("div", { className: "campaign-stat-grid" },
        h("span", null, h("strong", null, Number(campaignPreview.recipientCount || 0).toLocaleString()), "Recipients"),
        h("span", null, h("strong", null, Number(campaignPreview.attachmentCount || 0).toLocaleString()), "Images"),
        h("span", null, h("strong", null, selectedAssets.length.toLocaleString()), "Assets")
      )
    ),
    h("section", { className: "campaign-assets" },
      h("div", { className: "campaign-section-title" },
        h("strong", null, "Parts and cars"),
        h("span", null, `${selectedAssets.length} selected`)
      ),
      h("div", { className: "campaign-asset-list" },
        campaignAssets.map((asset) => {
          const selected = selectedAssetKeys.includes(assetKey(asset));
          return h("button", {
            key: assetKey(asset),
            type: "button",
            className: selected ? "campaign-asset active" : "campaign-asset",
            onClick: () => toggleAsset(asset)
          },
            h("span", { className: "asset-type" }, asset.assetType === "Car" ? "CAR" : "PART"),
            h("span", { className: "asset-copy" },
              h("strong", null, asset.title),
              h("small", null, asset.subtitle)
            ),
            h("span", { className: "asset-meta" },
              h("strong", null, money(asset.price, asset.currency)),
              Boolean(asset.imageCount) && h("small", null, `${asset.imageCount} image(s)`)
            )
          );
        })
      )
    ),
    h("section", { className: "campaign-message-panel" },
      h("div", { className: "campaign-section-title" },
        h("strong", null, "Generated message"),
        isLoading && h("span", null, "Working")
      ),
      h("textarea", {
        value: campaignMessage,
        onChange: (event) => setCampaignMessage(event.target.value),
        placeholder: "Preview generates the Arabic/English message here."
      }),
      h("div", { className: "campaign-preview-lists" },
        h("div", null,
          h("strong", null, "Preview recipients"),
          (campaignPreview.recipients || []).slice(0, 8).map((recipient) =>
            h("p", { key: recipient.phone }, h("span", null, recipient.name), h("small", null, recipient.phone))
          )
        ),
        h("div", null,
          h("strong", null, "Recent campaigns"),
          recentCampaigns.slice(0, 7).map((campaign) =>
            h("p", { key: campaign.id },
              h("span", null, campaign.name),
              h("small", null, `${campaign.recipientCount || 0} sent · ${campaign.replyCount || 0} replies · ${campaign.salesCount || 0} sales`)
            )
          )
        )
      )
    )
  );

  return h("section", { className: "screen whatsapp-screen" },
    h(PageHeader, {
      title: "WhatsApp",
      action: h("button", { className: "secondary-button", onClick: () => mode === "campaigns" ? loadCampaignBuilder() : loadConversations(), disabled: isLoading }, "Refresh")
    }),
    h(StatusLine, { status }),
    h("div", { className: "mode-switch" },
      h("button", { className: mode === "conversations" ? "active" : "", onClick: () => setMode("conversations") }, "Conversations"),
      h("button", { className: mode === "campaigns" ? "active" : "", onClick: () => setMode("campaigns") }, "Campaign Builder")
    ),
    mode === "campaigns" ? renderCampaignBuilder() : renderConversations()
  );
}
