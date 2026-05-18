const React = require("react");
const { ScrollView, View } = require("react-native");
const { CommunicationTemplateKey } = require("../core/app-config");
const { money, shortDate } = require("../core/formatters");
const { CommunicationPayloadFactory } = require("../core/communication-payload-factory");
const { EmptyState, Field, ListRow, Panel, ScreenHeader, ScreenScroll, SecondaryButton, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useState } = React;
const el = React.createElement;

function InvoicesScreen({ api }) {
  const { styles, t } = useTheme();
  const [search, setSearch] = useState("");
  const [invoices, setInvoices] = useState([]);
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("invoices.loading", "Loading invoices..."));
    try {
      const query = search.trim() ? `?search=${encodeURIComponent(search.trim())}` : "";
      setInvoices(await api.get(`/api/sales${query}`));
      setStatus(t("invoices.loaded", "Invoices loaded."));
    } catch (error) {
      setStatus(error.message || t("invoices.loadError", "Could not load invoices."));
    } finally {
      setIsLoading(false);
    }
  }, [api, search, t]);

  useEffect(() => { load(); }, [load]);

  const sendInvoice = useCallback(async (invoiceId, templateKey) => {
    setStatus(t("invoices.sending", "Sending WhatsApp message..."));
    try {
      const payload = templateKey === CommunicationTemplateKey.PaymentReminder
        ? CommunicationPayloadFactory.paymentReminder(invoiceId)
        : CommunicationPayloadFactory.salesInvoice(invoiceId);
      await api.post("/api/communications/send", payload);
      setStatus(templateKey === CommunicationTemplateKey.PaymentReminder
        ? t("invoices.reminderSent", "Reminder sent.")
        : t("invoices.invoiceSent", "Invoice sent."));
    } catch (error) {
      setStatus(error.message || t("invoices.sendError", "Could not send invoice."));
    }
  }, [api, t]);

  return el(ScreenScroll, null,
    el(ScreenHeader, { title: t("invoices.title", "Invoices"), actionTitle: t("common.search", "Search"), onAction: load, loading: isLoading }),
    el(Field, { label: t("common.search", "Search"), value: search, onChangeText: setSearch, returnKeyType: "search", onSubmitEditing: load }),
    el(StatusText, { value: status }),
    el(Panel, { title: t("invoices.title", "Invoices") },
      el(View, { style: styles.screenListFrameLarge },
        el(ScrollView, {
          nestedScrollEnabled: true,
          showsVerticalScrollIndicator: true,
          contentContainerStyle: styles.screenListContent
        },
          invoices.map((invoice) => el(View, { key: String(invoice.invoiceId), style: styles.screenListItem },
            el(ListRow, { title: invoice.customerName || t("invoices.walkIn", "Walk-in"), subtitle: shortDate(invoice.invoiceDate), value: money(invoice.totalAmount, invoice.currencyCode || "USD") }),
            el(View, { style: styles.inlineButtons },
              el(SecondaryButton, { title: t("invoices.sendInvoice", "Send Invoice"), onPress: () => sendInvoice(invoice.invoiceId, CommunicationTemplateKey.SalesInvoice) }),
              el(SecondaryButton, { title: t("invoices.reminder", "Reminder"), onPress: () => sendInvoice(invoice.invoiceId, CommunicationTemplateKey.PaymentReminder) })
            )
          )),
          invoices.length === 0 && el(EmptyState, { text: t("invoices.noInvoices", "No invoices found.") })
        )
      )
    )
  );
}

module.exports = { InvoicesScreen };
