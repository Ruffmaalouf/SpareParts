function read(row, ...keys) {
  for (const key of keys) {
    const value = row?.[key];
    if (value !== undefined && value !== null && value !== "") return value;
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    const pascalValue = row?.[pascalKey];
    if (pascalValue !== undefined && pascalValue !== null && pascalValue !== "") return pascalValue;
  }
  return "";
}

function toNumber(value) {
  const number = Number(value);
  return Number.isFinite(number) ? number : 0;
}

function isActiveRequest(request) {
  const status = String(read(request, "status") || "").toLowerCase();
  return status === "open" || status === "contacted";
}

function customerKey(request) {
  const customerId = read(request, "customerId");
  if (customerId) return `id:${customerId}`;
  return [
    String(read(request, "customerName") || "").trim().toLowerCase(),
    String(read(request, "customerPhone") || "").trim()
  ].join("|");
}

function priceLabel(value, currency = "USD") {
  const number = toNumber(value);
  const formatted = number.toLocaleString(undefined, {
    minimumFractionDigits: Number.isInteger(number) ? 0 : 2,
    maximumFractionDigits: 2
  });

  return String(currency || "USD").toUpperCase() === "USD" ? `$${formatted}` : `${currency || "USD"} ${formatted}`;
}

function roundToNearestFive(value) {
  if (value <= 0) return 0;
  return Math.max(5, Math.round(value / 5) * 5);
}

function suggestedPrice({ marketPrice, averagePrice, costPrice, salePrice, waitingCustomers, isRare }) {
  const marginFloor = costPrice > 0 ? costPrice / 0.72 : 0;
  const marketAnchor = marketPrice > 0 ? marketPrice : averagePrice > 0 ? averagePrice : salePrice;
  const demandLift = isRare && waitingCustomers > 0 ? Math.min(1.3, 1.12 + waitingCustomers * 0.045) : 1;
  const suggested = Math.max(marketAnchor * demandLift, marginFloor, salePrice);
  return roundToNearestFive(suggested);
}

export function waitingCustomersByPart(requests) {
  const groups = new Map();

  requests.filter(isActiveRequest).forEach((request) => {
    const partId = read(request, "partId");
    if (!partId) return;

    const key = String(partId);
    const group = groups.get(key) || { customers: new Set(), reported: 0 };
    const customer = customerKey(request);
    if (customer !== "|") group.customers.add(customer);
    group.reported = Math.max(group.reported, toNumber(read(request, "waitingCustomerCount")));
    groups.set(key, group);
  });

  const counts = new Map();
  groups.forEach((group, key) => {
    counts.set(key, Math.max(group.customers.size, group.reported));
  });

  return counts;
}

export function smartPricingCoach(part, waitingCustomers = 0) {
  if (!part) {
    return {
      tone: "neutral",
      badge: "No part selected",
      message: "Choose a part to see pricing guidance.",
      suggestedPrice: 0
    };
  }

  const currency = read(part, "currency") || "USD";
  const salePrice = toNumber(read(part, "salePrice"));
  const costPrice = toNumber(read(part, "costPrice"));
  const averagePrice = toNumber(read(part, "averagePrice"));
  const marketPrice = toNumber(read(part, "estimatedMarketPrice"));
  const availableQuantity = toNumber(read(part, "availableQuantity", "stockQuantity"));
  const minStock = toNumber(read(part, "minStock"));
  const marginRate = salePrice > 0 ? (salePrice - costPrice) / salePrice : 0;
  const isRare = availableQuantity > 0 && availableQuantity <= Math.max(1, minStock || 1);
  const waitingCount = Math.max(0, toNumber(waitingCustomers));
  const nextPrice = suggestedPrice({ marketPrice, averagePrice, costPrice, salePrice, waitingCustomers: waitingCount, isRare });
  const suggestedText = nextPrice > 0 ? priceLabel(nextPrice, currency) : "";
  const usualPrice = marketPrice > 0 ? marketPrice : averagePrice;
  const usuallyText = usualPrice > 0 ? priceLabel(usualPrice, currency) : "";
  const currentText = salePrice > 0 ? priceLabel(salePrice, currency) : "";

  if (isRare && waitingCount > 0 && nextPrice > salePrice) {
    return {
      tone: "opportunity",
      badge: `${waitingCount} waiting`,
      message: `Stock is rare and ${waitingCount} customer${waitingCount === 1 ? " is" : "s are"} waiting. Suggested price: ${suggestedText}.`,
      suggestedPrice: nextPrice
    };
  }

  if (salePrice <= 0) {
    return {
      tone: "warning",
      badge: "Set price",
    message: usualPrice > 0
        ? `This part usually sells at ${usuallyText}. Add a sale price before quoting.`
        : "Add a sale price before quoting this part.",
      suggestedPrice: nextPrice
    };
  }

  if (usualPrice > 0 && salePrice < usualPrice * 0.9) {
    const guidance = costPrice <= 0 || marginRate < 0.22
      ? "Margin is low."
      : `Suggested price: ${suggestedText}.`;

    return {
      tone: "warning",
      badge: "Below usual",
      message: `This part usually sells at ${usuallyText}. You priced ${currentText}. ${guidance}`,
      suggestedPrice: nextPrice
    };
  }

  if (costPrice > 0 && marginRate < 0.22) {
    return {
      tone: "warning",
      badge: "Low margin",
      message: `You priced ${currentText}. Margin is low. Suggested price: ${suggestedText}.`,
      suggestedPrice: nextPrice
    };
  }

  if (waitingCount > 0) {
    return {
      tone: "opportunity",
      badge: `${waitingCount} waiting`,
      message: `${waitingCount} customer${waitingCount === 1 ? " is" : "s are"} waiting for this part. Suggested price: ${suggestedText || currentText}.`,
      suggestedPrice: nextPrice
    };
  }

  if (usualPrice <= 0) {
    return {
      tone: "neutral",
      badge: "Add average",
      message: "Add an average market price to unlock stronger pricing guidance.",
      suggestedPrice: nextPrice
    };
  }

  if (salePrice > usualPrice * 1.15) {
    return {
      tone: "good",
      badge: "Strong",
      message: `Price is above the usual ${usuallyText} market range; margin looks protected.`,
      suggestedPrice: nextPrice
    };
  }

  return {
    tone: "good",
    badge: "Price ok",
    message: `Price is aligned with the usual ${usuallyText} market range.`,
    suggestedPrice: nextPrice
  };
}

export function PricingCoachCard({ h, coach, compact = false }) {
  if (!coach) return null;

  return h("article", { className: `pricing-coach-card ${coach.tone}${compact ? " compact" : ""}` },
    h("span", null, "Smart Pricing Coach"),
    h("strong", null, coach.message)
  );
}

export function PricingCoachSignal({ h, coach }) {
  if (!coach) return null;

  return h("span", {
    className: `pricing-coach-signal ${coach.tone}`,
    title: coach.message
  }, coach.badge);
}
