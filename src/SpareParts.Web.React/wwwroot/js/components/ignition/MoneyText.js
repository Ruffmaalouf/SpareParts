import { h } from "../../core/react-runtime.js";

// Ignition MoneyText (Report 03 §04). Renders a tabular-numeral span in
// JetBrains Mono. Negative values get the danger tone and a leading minus,
// never parentheses (matches the danger-text convention in formatters.js
// consumers). `format` lets callers inject the tenant display-currency
// formatter (displayMoneyFromCounter); otherwise a plain currency format
// is applied.
export function MoneyText({ value, currency = "USD", size, negativeTone = true, format }) {
  const number = Number(value || 0);
  const isNegative = negativeTone && number < 0;
  const text = typeof format === "function"
    ? format(number)
    : `${number < 0 ? "-" : ""}${Math.abs(number).toLocaleString(undefined, { style: "currency", currency })}`;
  const sizeClass = size ? ` ig-size-${size}` : "";

  return h("span", {
    className: `ig-money${sizeClass}${isNegative ? " is-negative" : ""}`,
    "aria-label": `${text} ${currency}`
  }, text);
}
