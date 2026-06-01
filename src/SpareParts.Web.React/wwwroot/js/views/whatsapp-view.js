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

const voiceNotes = [
  {
    id: "VN-1048",
    customerName: "Rami Haddad",
    phone: "+961 03 884 219",
    receivedAt: "Today · 10:42 AM",
    durationSeconds: 38,
    status: "Ready",
    vehicle: "BMW 320i · 2016",
    vehicleDetail: "F30 sedan · left-hand drive",
    requestSummary: "Left headlight assembly and driver-side mirror cover",
    urgency: "Needed today",
    location: "Jdeideh",
    confidence: "94%",
    transcript: "Marhaba, I need parts for a BMW 320i model 2016, F30. The left front headlight is broken and I also need the driver-side mirror cover, black if possible. I can pick them up today from Jdeideh.",
    suggestions: [
      { id: "BM-F30-HL-L", title: "BMW F30 left headlight assembly", detail: "OEM xenon · used Grade A", stock: "2 available", fit: "94% fit", price: 285, quantity: 1, selected: true },
      { id: "BM-F30-MC-L", title: "BMW F30 mirror cover · left", detail: "Black · aftermarket new", stock: "6 available", fit: "91% fit", price: 38, quantity: 1, selected: true },
      { id: "BM-F30-HL-BR", title: "BMW F30 headlight bracket · left", detail: "OEM · used Grade A", stock: "3 available", fit: "78% fit", price: 24, quantity: 1, selected: false }
    ]
  },
  {
    id: "VN-1047",
    customerName: "Garage Mecanix",
    phone: "+961 70 448 906",
    receivedAt: "Today · 9:16 AM",
    durationSeconds: 52,
    status: "Review",
    vehicle: "Mercedes C200 · 2018",
    vehicleDetail: "W205 · petrol",
    requestSummary: "Front brake pads and wear sensor",
    urgency: "This week",
    location: "Sin El Fil",
    confidence: "89%",
    transcript: "Good morning, for a Mercedes C200 year 2018 W205 I need front brake pads with the sensor. Please send me the available brands and best price. Delivery is to the garage in Sin El Fil this week.",
    suggestions: [
      { id: "MB-W205-BP-F", title: "Mercedes W205 front brake pads", detail: "Textar · new", stock: "8 available", fit: "96% fit", price: 92, quantity: 1, selected: true },
      { id: "MB-W205-BS-F", title: "Mercedes W205 brake wear sensor", detail: "Bosch · new", stock: "11 available", fit: "95% fit", price: 18, quantity: 1, selected: true },
      { id: "MB-W205-BP-E", title: "Mercedes W205 front brake pads", detail: "Economy line · new", stock: "14 available", fit: "90% fit", price: 63, quantity: 1, selected: false }
    ]
  },
  {
    id: "VN-1046",
    customerName: "Nadine Khoury",
    phone: "+961 71 902 314",
    receivedAt: "Yesterday · 4:28 PM",
    durationSeconds: 31,
    status: "New",
    vehicle: "Volkswagen Golf · 2015",
    vehicleDetail: "Mk7 · 1.4 TSI",
    requestSummary: "Water pump and thermostat kit",
    urgency: "Checking availability",
    location: "Zalka",
    confidence: "92%",
    transcript: "Hi, can you check a water pump with thermostat for a Volkswagen Golf 2015, one point four TSI? I want the complete kit and the price please. I am in Zalka.",
    suggestions: [
      { id: "VW-MK7-WP-K", title: "Golf Mk7 water pump kit", detail: "INA · pump + thermostat", stock: "4 available", fit: "97% fit", price: 146, quantity: 1, selected: true },
      { id: "VW-MK7-CL-5L", title: "G13 coolant · 5L", detail: "Volkswagen spec", stock: "9 available", fit: "Recommended", price: 21, quantity: 1, selected: false }
    ]
  }
];

const voiceIconPaths = {
  mic: "M12 14a3 3 0 0 0 3-3V6a3 3 0 0 0-6 0v5a3 3 0 0 0 3 3Zm-6-3a6 6 0 0 0 12 0M12 17v4M9 21h6",
  play: "m9 7 7 5-7 5V7Z",
  pause: "M9 7v10M15 7v10",
  spark: "m12 3 1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8L12 3Zm6 12 .8 2.2L21 18l-2.2.8L18 21l-.8-2.2L15 18l2.2-.8L18 15Z",
  copy: "M8 8h10v12H8V8Zm-3 8H4V4h10v1",
  send: "m4 5 16 7-16 7 3-7-3-7Zm3 7h7"
};

function assetKey(asset) {
  return `${asset.assetType}:${asset.id}`;
}

function money(value, currency = "USD") {
  if (value === null || value === undefined) return "";
  return `${currency || "USD"} ${Number(value || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

function voiceDuration(seconds) {
  return `0:${String(seconds).padStart(2, "0")}`;
}

function VoiceIcon({ name }) {
  return h("svg", { className: "voice-icon", viewBox: "0 0 24 24", "aria-hidden": "true" },
    h("path", { d: voiceIconPaths[name], fill: "none", stroke: "currentColor", strokeLinecap: "round", strokeLinejoin: "round", strokeWidth: "1.8" })
  );
}

function createQuoteReply(note, lines) {
  const selected = lines.filter((line) => line.selected);
  const total = selected.reduce((sum, line) => sum + Number(line.price || 0) * Number(line.quantity || 0), 0);
  const partLines = selected.map((line) => `• ${line.title} — ${money(Number(line.price || 0) * Number(line.quantity || 0))}`).join("\n");
  return `Hi ${note.customerName.split(" ")[0]}, thanks for your voice note. For your ${note.vehicle} I found:\n${partLines || "• I am checking the requested parts."}\n\nTotal: ${money(total)}\n${note.urgency === "Needed today" ? "The selected parts are available for pickup today." : "The selected parts are currently available."} Please confirm and I will reserve them for you.`;
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
  const [mode, setMode] = useState("voice-quotes");
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
  const [voiceNoteId, setVoiceNoteId] = useState(voiceNotes[0].id);
  const [voiceSearch, setVoiceSearch] = useState("");
  const [voiceQuoteLines, setVoiceQuoteLines] = useState(voiceNotes[0].suggestions);
  const [replyDraft, setReplyDraft] = useState(() => createQuoteReply(voiceNotes[0], voiceNotes[0].suggestions));
  const [voiceAnalysisState, setVoiceAnalysisState] = useState("ready");
  const [isVoicePlaying, setIsVoicePlaying] = useState(false);
  const [voiceProgress, setVoiceProgress] = useState(42);

  const selectedConversation = useMemo(
    () => conversations.find((item) => item.recipientPhone === selectedPhone) || null,
    [conversations, selectedPhone]
  );
  const selectedAssets = useMemo(
    () => campaignAssets.filter((asset) => selectedAssetKeys.includes(assetKey(asset))),
    [campaignAssets, selectedAssetKeys]
  );
  const activeVoiceNote = useMemo(
    () => voiceNotes.find((note) => note.id === voiceNoteId) || voiceNotes[0],
    [voiceNoteId]
  );
  const filteredVoiceNotes = useMemo(() => {
    const term = voiceSearch.trim().toLowerCase();
    if (!term) return voiceNotes;
    return voiceNotes.filter((note) => `${note.customerName} ${note.phone} ${note.vehicle} ${note.requestSummary}`.toLowerCase().includes(term));
  }, [voiceSearch]);
  const voiceQuoteTotal = useMemo(
    () => voiceQuoteLines.filter((line) => line.selected).reduce((sum, line) => sum + Number(line.price || 0) * Number(line.quantity || 0), 0),
    [voiceQuoteLines]
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

  useEffect(() => {
    if (!isVoicePlaying) return undefined;
    const interval = window.setInterval(() => {
      setVoiceProgress((current) => {
        if (current >= 100) {
          setIsVoicePlaying(false);
          return 0;
        }
        return current + 2;
      });
    }, 500);
    return () => window.clearInterval(interval);
  }, [isVoicePlaying]);

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

  const pickVoiceNote = useCallback((note) => {
    setVoiceNoteId(note.id);
    setVoiceQuoteLines(note.suggestions);
    setReplyDraft(createQuoteReply(note, note.suggestions));
    setVoiceAnalysisState(note.status === "New" ? "idle" : "ready");
    setIsVoicePlaying(false);
    setVoiceProgress(note.status === "New" ? 0 : 42);
    setStatus(`${note.customerName}'s voice note selected.`);
  }, []);

  const analyzeVoiceNote = useCallback(async () => {
    setVoiceAnalysisState("working");
    setIsVoicePlaying(false);
    setStatus("Transcribing voice note and matching inventory...");
    await new Promise((resolve) => window.setTimeout(resolve, 720));
    setVoiceQuoteLines(activeVoiceNote.suggestions);
    setReplyDraft(createQuoteReply(activeVoiceNote, activeVoiceNote.suggestions));
    setVoiceAnalysisState("ready");
    setStatus("Voice note converted into a quote draft.");
  }, [activeVoiceNote]);

  const toggleVoiceQuoteLine = useCallback((id) => {
    setVoiceQuoteLines((current) => current.map((line) => line.id === id ? { ...line, selected: !line.selected } : line));
  }, []);

  const updateVoiceQuoteLine = useCallback((id, patch) => {
    setVoiceQuoteLines((current) => current.map((line) => line.id === id ? { ...line, ...patch } : line));
  }, []);

  const regenerateVoiceReply = useCallback(() => {
    setReplyDraft(createQuoteReply(activeVoiceNote, voiceQuoteLines));
    setStatus("Reply draft updated from the selected parts.");
  }, [activeVoiceNote, voiceQuoteLines]);

  const copyVoiceReply = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(replyDraft);
      setStatus("Reply draft copied.");
    } catch {
      setStatus("Could not copy the draft. It is ready to select manually.");
    }
  }, [replyDraft]);

  const openVoiceReplyInConversation = useCallback(() => {
    setSelectedPhone("");
    setMessages([]);
    setManualName(activeVoiceNote.customerName);
    setManualPhone(activeVoiceNote.phone);
    setCompose(replyDraft);
    setMode("conversations");
    setStatus("Quote reply moved into a WhatsApp conversation. Review it before sending.");
  }, [activeVoiceNote, replyDraft]);

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

  const renderVoiceQuote = () => h("div", { className: "voice-quote-layout" },
    h("aside", { className: "voice-inbox" },
      h("div", { className: "voice-panel-heading" },
        h("div", null,
          h("strong", null, "Voice inbox"),
          h("span", null, "WhatsApp voice notes")
        ),
        h("b", null, voiceNotes.length)
      ),
      h("input", {
        value: voiceSearch,
        onChange: (event) => setVoiceSearch(event.target.value),
        placeholder: "Search voice notes"
      }),
      h("div", { className: "voice-note-stack" },
        filteredVoiceNotes.map((note) =>
          h("button", {
            key: note.id,
            className: note.id === activeVoiceNote.id ? "voice-note-card active" : "voice-note-card",
            onClick: () => pickVoiceNote(note)
          },
            h("span", { className: "voice-note-card-top" },
              h("strong", null, note.customerName),
              h("small", { className: `voice-note-status ${note.status.toLowerCase()}` }, note.status)
            ),
            h("span", { className: "voice-note-card-vehicle" }, note.vehicle),
            h("span", { className: "voice-note-card-meta" },
              h("small", null, note.receivedAt),
              h("small", null, voiceDuration(note.durationSeconds))
            )
          )
        )
      )
    ),
    h("section", { className: "voice-analysis-panel" },
      h("header", { className: "voice-analysis-header" },
        h("span", { className: "voice-note-mark" }, h(VoiceIcon, { name: "mic" })),
        h("div", null,
          h("h2", null, activeVoiceNote.customerName),
          h("p", null, `${activeVoiceNote.phone} · ${activeVoiceNote.receivedAt}`)
        ),
        h("span", { className: "voice-confidence" }, `${activeVoiceNote.confidence} confidence`)
      ),
      h("div", { className: "voice-player" },
        h("button", {
          className: "voice-player-button",
          onClick: () => setIsVoicePlaying((current) => !current),
          "aria-label": isVoicePlaying ? "Pause voice note" : "Play voice note"
        }, h(VoiceIcon, { name: isVoicePlaying ? "pause" : "play" })),
        h("div", { className: "voice-waveform", "aria-hidden": "true" },
          [28, 44, 68, 38, 76, 54, 84, 48, 62, 78, 42, 66, 88, 56, 72, 36, 58, 78, 44, 62, 82, 48, 68, 38, 56, 74, 46, 64].map((height, index) =>
            h("i", { key: `${height}-${index}`, className: index / 28 * 100 <= voiceProgress ? "played" : "", style: { height: `${height}%` } })
          )
        ),
        h("time", null, voiceDuration(activeVoiceNote.durationSeconds))
      ),
      h("div", { className: "voice-section-title" },
        h("div", null,
          h("strong", null, "Transcript"),
          h("span", null, "Arabic and English normalized")
        ),
        h("button", { className: "voice-analyze-button", onClick: analyzeVoiceNote, disabled: voiceAnalysisState === "working" },
          h(VoiceIcon, { name: "spark" }),
          voiceAnalysisState === "working" ? "Analyzing..." : "Analyze voice note"
        )
      ),
      h("blockquote", { className: voiceAnalysisState === "working" ? "voice-transcript working" : "voice-transcript" },
        voiceAnalysisState === "working" ? "Listening for vehicle, part, condition, location, and urgency..." : activeVoiceNote.transcript
      ),
      h("div", { className: "voice-section-title compact" },
        h("div", null,
          h("strong", null, "Structured request"),
          h("span", null, "Review the extracted details before quoting")
        )
      ),
      h("div", { className: "voice-request-grid" },
        [
          ["Vehicle", activeVoiceNote.vehicle],
          ["Vehicle details", activeVoiceNote.vehicleDetail],
          ["Requested parts", activeVoiceNote.requestSummary],
          ["Timing", activeVoiceNote.urgency],
          ["Pickup / delivery", activeVoiceNote.location],
          ["Source", "WhatsApp voice note"]
        ].map(([label, value]) =>
          h("div", { key: label },
            h("small", null, label),
            h("strong", null, value)
          )
        )
      )
    ),
    h("aside", { className: "voice-quote-panel" },
      h("div", { className: "voice-panel-heading" },
        h("div", null,
          h("strong", null, "Suggested parts"),
          h("span", null, "Inventory matches with quote prices")
        ),
        h("b", null, voiceQuoteLines.filter((line) => line.selected).length)
      ),
      h("div", { className: "voice-quote-lines" },
        voiceQuoteLines.map((line) =>
          h("article", { key: line.id, className: line.selected ? "voice-quote-line selected" : "voice-quote-line" },
            h("label", { className: "voice-quote-check" },
              h("input", { type: "checkbox", checked: line.selected, onChange: () => toggleVoiceQuoteLine(line.id) }),
              h("span", null,
                h("strong", null, line.title),
                h("small", null, `${line.id} · ${line.detail}`)
              )
            ),
            h("div", { className: "voice-quote-line-meta" },
              h("span", null, line.stock),
              h("span", null, line.fit)
            ),
            h("div", { className: "voice-quote-line-controls" },
              h("label", null, "Price",
                h("input", {
                  type: "number",
                  min: "0",
                  value: line.price,
                  onChange: (event) => updateVoiceQuoteLine(line.id, { price: Number(event.target.value) })
                })
              ),
              h("label", null, "Qty",
                h("input", {
                  type: "number",
                  min: "1",
                  value: line.quantity,
                  onChange: (event) => updateVoiceQuoteLine(line.id, { quantity: Math.max(1, Number(event.target.value) || 1) })
                })
              )
            )
          )
        )
      ),
      h("div", { className: "voice-quote-total" },
        h("span", null, "Quote total"),
        h("strong", null, money(voiceQuoteTotal))
      ),
      h("div", { className: "voice-reply-heading" },
        h("div", null,
          h("strong", null, "Reply draft"),
          h("span", null, "Editable before sending")
        ),
        h("button", { className: "voice-icon-button", onClick: copyVoiceReply, "aria-label": "Copy reply draft" }, h(VoiceIcon, { name: "copy" }))
      ),
      h("textarea", {
        className: "voice-reply-draft",
        value: replyDraft,
        onChange: (event) => setReplyDraft(event.target.value)
      }),
      h("div", { className: "voice-reply-actions" },
        h("button", { className: "secondary-button", onClick: regenerateVoiceReply }, "Regenerate"),
        h("button", { className: "primary-button", onClick: openVoiceReplyInConversation },
          h(VoiceIcon, { name: "send" }),
          "Open in Conversations"
        )
      )
    )
  );

  return h("section", { className: "screen whatsapp-screen" },
    h(PageHeader, {
      title: "WhatsApp Voice-to-Quote",
      subtitle: "Turn customer voice notes into structured requests, priced parts, and ready-to-send replies.",
      action: h("button", { className: "secondary-button", onClick: () => mode === "campaigns" ? loadCampaignBuilder() : loadConversations(), disabled: isLoading }, "Refresh")
    }),
    h(StatusLine, { status }),
    h("div", { className: "mode-switch" },
      h("button", { className: mode === "voice-quotes" ? "active" : "", onClick: () => setMode("voice-quotes") }, "Voice-to-Quote"),
      h("button", { className: mode === "conversations" ? "active" : "", onClick: () => setMode("conversations") }, "Conversations"),
      h("button", { className: mode === "campaigns" ? "active" : "", onClick: () => setMode("campaigns") }, "Campaign Builder")
    ),
    mode === "campaigns" ? renderCampaignBuilder() : mode === "conversations" ? renderConversations() : renderVoiceQuote()
  );
}
