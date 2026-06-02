import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { asRows, money } from "../core/formatters.js";
import { PageHeader, StatusLine } from "../components/shared.js";

const tabs = [
  ["money", "Tonight's Money"],
  ["treasure", "Treasure Map"],
  ["simulator", "Auction Simulator"],
  ["teardown", "Teardown Queue"],
  ["duplicates", "Duplicate Detective"],
  ["buying", "Buying Radar"],
  ["voice", "Voice-to-Quote"]
];

const zonePoints = {
  "Engine bay": [470, 104],
  Drivetrain: [334, 146],
  Front: [548, 146],
  Rear: [104, 146],
  "Cabin and sides": [286, 86],
  Chassis: [350, 202],
  Other: [214, 208]
};

function number(value) {
  const parsed = Number(value || 0);
  return Number.isFinite(parsed) ? parsed : 0;
}

function formatMoney(value, currency = "USD") {
  return money(number(value), currency || "USD");
}

function modelTitle(model) {
  return `${model.carBrandName || ""} ${model.name || ""}${model.bodyType ? ` (${model.bodyType})` : ""}`.trim();
}

function toneClass(value) {
  return String(value || "neutral").trim().toLowerCase();
}

function Empty({ children }) {
  return h("p", { className: "growth-empty" }, children);
}

function MoneyActionCard({ action, onView }) {
  return h("article", { className: `growth-action-card ${toneClass(action.tone)}` },
    h("div", null,
      h("span", null, action.kind),
      h("strong", null, action.title),
      h("p", null, action.detail)
    ),
    h("aside", null,
      h("b", null, formatMoney(action.estimatedValue, action.currency)),
      action.targetView && h("button", { className: "secondary-button", type: "button", onClick: () => onView(action.targetView) }, "Open")
    )
  );
}

function TonightMoney({ briefing, onView }) {
  const actions = briefing?.moneyActions || [];
  return h("section", { className: "growth-panel-stack" },
    h("article", { className: "growth-money-hero" },
      h("div", null,
        h("span", null, "Closing-time operating plan"),
        h("h3", null, "Do these before the shutters come down."),
        h("p", null, "The queue combines waiting customers, unpriced stock, dormant inventory, donor-car gaps, and buying signals.")
      ),
      h("strong", null, formatMoney(briefing?.unlockableValue, briefing?.currency))
    ),
    h("div", { className: "growth-action-list" },
      actions.map((action) => h(MoneyActionCard, { key: action.key, action, onView })),
      actions.length === 0 && h(Empty, null, "No urgent money actions are waiting.")
    )
  );
}

function TreasureMapGraphic({ car }) {
  const hotspots = car?.hotspots || [];
  return h("svg", { className: "treasure-car-map", viewBox: "0 0 640 260", role: "img", "aria-label": "Donor car opportunity map" },
    h("path", { className: "treasure-car-shell", d: "M86 166 126 108c12-18 34-30 56-34l198-28c42-6 82 9 108 40l54 64 30 9v35H72v-24l14-4Z" }),
    h("path", { className: "treasure-car-window", d: "M196 86 376 61c31-4 62 8 82 31l21 26H169l27-32Z" }),
    h("circle", { className: "treasure-wheel", cx: 170, cy: 190, r: 34 }),
    h("circle", { className: "treasure-wheel", cx: 484, cy: 190, r: 34 }),
    hotspots.map((hotspot) => {
      const [x, y] = zonePoints[hotspot.zone] || zonePoints.Other;
      const radius = Math.max(16, Math.min(34, 16 + number(hotspot.waitingCustomers) * 2 + number(hotspot.partCount)));
      return h("g", { className: "treasure-hotspot", key: hotspot.zone, transform: `translate(${x} ${y})` },
        h("circle", { r: radius }),
        h("text", { y: 4 }, hotspot.partCount),
        h("title", null, `${hotspot.zone}: ${hotspot.partCount} parts / ${hotspot.waitingCustomers} waiting / ${formatMoney(hotspot.opportunityValue)}`)
      );
    })
  );
}

function TreasureMap({ briefing, selectedCarId, onSelectCar }) {
  const cars = briefing?.donorCars || [];
  const selectedCar = cars.find((car) => String(car.usedCarId) === String(selectedCarId)) || cars[0];
  return h("section", { className: "growth-treasure-layout" },
    h("aside", { className: "growth-car-list" },
      cars.map((car) =>
        h("button", {
          key: car.usedCarId,
          className: String(car.usedCarId) === String(selectedCar?.usedCarId) ? "growth-car-button active" : "growth-car-button",
          type: "button",
          onClick: () => onSelectCar(String(car.usedCarId))
        },
          h("strong", null, `${car.modelYear} ${car.car}`),
          h("span", null, `${car.recoveryPercent}% recovered`),
          h("b", null, `${formatMoney(car.breakEvenGap)} gap`)
        )
      ),
      cars.length === 0 && h(Empty, null, "No received donor cars are available.")
    ),
    selectedCar && h("div", { className: "growth-treasure-main" },
      h("article", { className: "growth-car-summary" },
        h("div", null,
          h("span", null, selectedCar.barcode),
          h("h3", null, `${selectedCar.modelYear} ${selectedCar.car}`),
          h("p", null, "Each glowing zone rolls up linked parts, waiting customers, and stock revenue.")
        ),
        h("div", { className: "growth-summary-numbers" },
          h("span", null, "Loaded", h("strong", null, formatMoney(selectedCar.loadedCost))),
          h("span", null, "Recovered", h("strong", null, formatMoney(selectedCar.recoveredValue))),
          h("span", null, "Gap", h("strong", null, formatMoney(selectedCar.breakEvenGap))),
          h("span", null, "Shelf upside", h("strong", null, formatMoney(selectedCar.projectedStockRevenue)))
        )
      ),
      h(TreasureMapGraphic, { car: selectedCar }),
      h("div", { className: "growth-part-grid" },
        (selectedCar.parts || []).slice(0, 12).map((part) =>
          h("article", { className: "growth-part-card", key: part.partId },
            h("span", null, `${part.zone} / ${part.internalCode}`),
            h("strong", null, part.partName),
            h("small", null, `${part.availableQuantity} ready / ${part.waitingCustomers} waiting`),
            h("b", null, formatMoney(part.opportunityValue)),
            h("em", null, part.action)
          )
        )
      )
    )
  );
}

function AuctionSimulator({ api, models }) {
  const [form, setForm] = useState({
    carModelId: "",
    modelYear: "",
    auctionPrice: "",
    transportation: "",
    customs: "",
    shipping: "",
    repairs: "",
    partOutCost: "",
    desiredProfit: ""
  });
  const [result, setResult] = useState(null);
  const [status, setStatus] = useState("");
  const setField = useCallback((key, value) => setForm((current) => ({ ...current, [key]: value })), []);
  const simulate = useCallback(async () => {
    setStatus("Running donor-car estimate...");
    try {
      setResult(await api.post("/api/growth/auction-simulator", {
        ...form,
        carModelId: form.carModelId ? Number(form.carModelId) : null,
        modelYear: form.modelYear ? Number(form.modelYear) : null,
        auctionPrice: Number(form.auctionPrice || 0),
        transportation: Number(form.transportation || 0),
        customs: Number(form.customs || 0),
        shipping: Number(form.shipping || 0),
        repairs: Number(form.repairs || 0),
        partOutCost: Number(form.partOutCost || 0),
        desiredProfit: Number(form.desiredProfit || 0)
      }));
      setStatus("Estimate ready.");
    } catch (error) {
      setStatus(error.message || "Could not simulate this auction.");
    }
  }, [api, form]);

  return h("section", { className: "growth-simulator-layout" },
    h("article", { className: "panel growth-form-panel" },
      h("h3", null, "Auction Profit Simulator"),
      h("p", null, "Know your ceiling before emotion starts bidding."),
      h("label", null, "Car model",
        h("select", { value: form.carModelId, onChange: (event) => setField("carModelId", event.target.value) },
          h("option", { value: "" }, "Any donor car"),
          models.map((model) => h("option", { key: model.id, value: model.id }, modelTitle(model)))
        )
      ),
      h("div", { className: "growth-field-grid" },
        ["modelYear", "auctionPrice", "transportation", "customs", "shipping", "repairs", "partOutCost", "desiredProfit"].map((key) =>
          h("label", { key }, key.replace(/([A-Z])/g, " $1"),
            h("input", { type: "number", value: form[key], onChange: (event) => setField(key, event.target.value) })
          )
        )
      ),
      h("button", { className: "primary-button", type: "button", onClick: simulate }, "Calculate maximum bid"),
      h(StatusLine, { status })
    ),
    result && h("article", { className: "growth-simulator-result" },
      h("span", null, `${result.confidence} confidence / ${result.comparableCars} comparable cars`),
      h("h3", null, result.car),
      h("div", { className: "growth-result-grid" },
        h("div", null, h("span", null, "Loaded cost"), h("strong", null, formatMoney(result.loadedCost))),
        h("div", null, h("span", null, "Estimated recovery"), h("strong", null, formatMoney(result.estimatedRecovery))),
        h("div", null, h("span", null, "Estimated profit"), h("strong", null, formatMoney(result.estimatedProfit))),
        h("div", { className: "featured" }, h("span", null, "Maximum bid"), h("strong", null, formatMoney(result.maximumBid)))
      ),
      h("ul", null, (result.notes || []).map((note) => h("li", { key: note }, note)))
    )
  );
}

function TeardownQueue({ briefing }) {
  const queue = briefing?.teardownQueue || [];
  return h("section", { className: "growth-table-panel" },
    h("div", { className: "growth-section-copy" },
      h("h3", null, "Teardown Work Queue"),
      h("p", null, "The dismantling bench gets a ranked list, not a vague instruction to work on the latest car.")
    ),
    h("div", { className: "growth-row-list" },
      queue.map((item, index) =>
        h("article", { className: "growth-ranked-row", key: `${item.usedCarId}-${item.partId || index}` },
          h("b", null, String(index + 1).padStart(2, "0")),
          h("div", null,
            h("span", null, `${item.car} / ${item.zone}`),
            h("strong", null, item.partName),
            h("small", null, `${item.action}. ${item.reason}`)
          ),
          h("aside", null,
            h("strong", null, formatMoney(item.estimatedValue)),
            h("span", null, `Score ${item.priorityScore}`)
          )
        )
      ),
      queue.length === 0 && h(Empty, null, "No teardown actions are waiting.")
    )
  );
}

function DuplicateDetective({ briefing, onView }) {
  const groups = briefing?.duplicateGroups || [];
  return h("section", { className: "growth-table-panel" },
    h("div", { className: "growth-section-copy" },
      h("h3", null, "Duplicate Part Detective"),
      h("p", null, "Catch fragmented stock before the same part lives under three names and two prices.")
    ),
    h("div", { className: "growth-duplicate-grid" },
      groups.map((group) =>
        h("article", { className: "growth-duplicate-card", key: group.matchKey },
          h("span", null, group.reason),
          h("h4", null, group.matchKey),
          h("p", null, `${group.candidateCount} records / ${group.combinedStock} combined available stock`),
          h("ul", null, (group.candidates || []).map((candidate) =>
            h("li", { key: candidate.partId },
              h("strong", null, candidate.internalCode),
              h("span", null, candidate.partName),
              h("b", null, formatMoney(candidate.salePrice))
            )
          )),
          h("button", { className: "secondary-button", type: "button", onClick: () => onView("management") }, "Review parts")
        )
      ),
      groups.length === 0 && h(Empty, null, "No likely duplicates detected.")
    )
  );
}

function BuyingRadar({ briefing, onView }) {
  const rows = briefing?.buyingRadar || [];
  return h("section", { className: "growth-table-panel" },
    h("div", { className: "growth-section-copy" },
      h("h3", null, "Buying Radar"),
      h("p", null, "Demand, velocity, and minimum stock combine into a purchase shortlist.")
    ),
    h("div", { className: "growth-row-list" },
      rows.map((item) =>
        h("article", { className: "growth-buy-row", key: item.partId },
          h("div", { className: "growth-score" }, item.urgencyScore),
          h("div", null,
            h("span", null, `${item.internalCode}${item.oemNumber ? ` / OEM ${item.oemNumber}` : ""}`),
            h("strong", null, item.partName),
            h("small", null, item.reason)
          ),
          h("aside", null,
            h("b", null, `Buy ${item.suggestedQuantity}`),
            h("span", null, `${formatMoney(item.estimatedRevenue, item.currency)} potential`)
          )
        )
      ),
      rows.length === 0 && h(Empty, null, "No restock recommendations are waiting.")
    ),
    h("button", { className: "secondary-button", type: "button", onClick: () => onView("purchase-parts") }, "Open part purchases")
  );
}

function VoiceToQuote({ api, onView }) {
  const [transcript, setTranscript] = useState("");
  const [customerName, setCustomerName] = useState("");
  const [customerPhone, setCustomerPhone] = useState("");
  const [result, setResult] = useState(null);
  const [status, setStatus] = useState("");
  const listen = useCallback(() => {
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!Recognition) {
      setStatus("Speech recognition is not available in this browser. Type the voice note transcript instead.");
      return;
    }
    const recognition = new Recognition();
    recognition.lang = "en-US";
    recognition.interimResults = false;
    recognition.onresult = (event) => {
      setTranscript(event.results[0][0].transcript);
      setStatus("Voice note captured. Build the quote when ready.");
    };
    recognition.onerror = () => setStatus("Could not capture speech. Type the request instead.");
    recognition.start();
    setStatus("Listening...");
  }, []);
  const build = useCallback(async () => {
    setStatus("Turning request into a quote...");
    try {
      setResult(await api.post("/api/growth/voice-quote", { transcript, customerName, customerPhone }));
      setStatus("Quote draft ready.");
    } catch (error) {
      setStatus(error.message || "Could not build the quote.");
    }
  }, [api, customerName, customerPhone, transcript]);

  return h("section", { className: "growth-voice-layout" },
    h("article", { className: "panel growth-form-panel" },
      h("h3", null, "WhatsApp Voice-to-Quote"),
      h("p", null, "Capture a customer voice note, find stock, and prepare the reply while the conversation is still warm."),
      h("div", { className: "growth-field-grid" },
        h("label", null, "Customer", h("input", { value: customerName, onChange: (event) => setCustomerName(event.target.value) })),
        h("label", null, "WhatsApp phone", h("input", { value: customerPhone, onChange: (event) => setCustomerPhone(event.target.value) }))
      ),
      h("label", null, "Voice note transcript",
        h("textarea", { value: transcript, onChange: (event) => setTranscript(event.target.value), placeholder: "I need a left headlight for a 2015 Mercedes W205..." })
      ),
      h("div", { className: "growth-inline-actions" },
        h("button", { className: "secondary-button", type: "button", onClick: listen }, "Dictate"),
        h("button", { className: "primary-button", type: "button", onClick: build }, "Build quote")
      ),
      h(StatusLine, { status })
    ),
    result && h("article", { className: "growth-quote-result" },
      h("span", null, result.vehicleHint || "Customer request"),
      h("h3", null, "Suggested reply"),
      h("p", null, result.draftMessage),
      h("div", { className: "growth-quote-matches" },
        (result.matches || []).map((match) =>
          h("div", { key: match.partId },
            h("strong", null, `${match.internalCode} / ${match.partName}`),
            h("span", null, `${match.availableQuantity} available / ${formatMoney(match.salePrice, match.currency)}`)
          )
        )
      ),
      h("button", { className: "secondary-button", type: "button", onClick: () => onView("whatsapp") }, "Open WhatsApp")
    )
  );
}

export function GrowthLabView({ api, onView }) {
  const [briefing, setBriefing] = useState(null);
  const [models, setModels] = useState([]);
  const [activeTab, setActiveTab] = useState("money");
  const [selectedCarId, setSelectedCarId] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus("Loading growth intelligence...");
    try {
      const [nextBriefing, nextModels] = await Promise.all([
        api.get("/api/growth/briefing"),
        api.get("/api/carmodels?page=1&pageSize=5000")
      ]);
      setBriefing(nextBriefing);
      setModels(asRows(nextModels));
      setSelectedCarId((current) => current || String(nextBriefing?.donorCars?.[0]?.usedCarId || ""));
      setStatus("Growth intelligence loaded.");
    } catch (error) {
      setStatus(error.message || "Could not load growth intelligence.");
    } finally {
      setIsLoading(false);
    }
  }, [api]);
  useEffect(() => { load(); }, [load]);
  const content = useMemo(() => ({
    buying: h(BuyingRadar, { briefing, onView }),
    duplicates: h(DuplicateDetective, { briefing, onView }),
    money: h(TonightMoney, { briefing, onView }),
    simulator: h(AuctionSimulator, { api, models }),
    teardown: h(TeardownQueue, { briefing }),
    treasure: h(TreasureMap, { briefing, selectedCarId, onSelectCar: setSelectedCarId }),
    voice: h(VoiceToQuote, { api, onView })
  }), [activeTab, api, briefing, models, onView, selectedCarId]);

  return h("section", { className: "screen growth-lab-screen" },
    h(PageHeader, {
      kicker: "Growth intelligence",
      title: "Money Finder Lab",
      subtitle: "Turn stock, donor cars, waiting customers, and counter conversations into the next best action.",
      action: h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, "Refresh")
    }),
    h(StatusLine, { status }),
    h("nav", { className: "growth-tabs", "aria-label": "Growth lab tools" },
      tabs.map(([key, label]) =>
        h("button", { className: key === activeTab ? "active" : "", key, type: "button", onClick: () => setActiveTab(key) }, label)
      )
    ),
    content[activeTab]
  );
}
