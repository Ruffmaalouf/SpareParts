const React = require("react");
const { ScrollView, View } = require("react-native");
const { money } = require("../core/formatters");
const { EmptyState, ListRow, Panel, ScreenHeader, ScreenScroll, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");

const { useCallback, useEffect, useState } = React;
const el = React.createElement;

function ContactsScreen({ api }) {
  const { t } = useTheme();
  const [customers, setCustomers] = useState([]);
  const [suppliers, setSuppliers] = useState([]);
  const [status, setStatus] = useState("");

  const load = useCallback(async () => {
    setStatus(t("contacts.loading", "Loading contacts..."));
    try {
      const [customerRows, supplierRows] = await Promise.all([
        api.get("/api/customers?page=1&pageSize=100"),
        api.get("/api/suppliers?page=1&pageSize=100")
      ]);
      setCustomers(customerRows);
      setSuppliers(supplierRows);
      setStatus(t("contacts.loaded", "Contacts loaded."));
    } catch (error) {
      setStatus(error.message || t("contacts.loadError", "Could not load contacts."));
    }
  }, [api, t]);

  useEffect(() => { load(); }, [load]);

  return el(ScreenScroll, null,
    el(ScreenHeader, { title: t("contacts.title", "Contacts"), actionTitle: t("common.refresh", "Refresh"), onAction: load }),
    el(StatusText, { value: status }),
    el(ContactGroup, { title: t("contacts.customers", "Customers"), rows: customers }),
    el(ContactGroup, { title: t("contacts.suppliers", "Suppliers"), rows: suppliers })
  );
}

function ContactGroup({ title, rows }) {
  const { styles, t } = useTheme();

  return el(Panel, { title },
    el(View, { style: styles.screenListFrame },
      el(ScrollView, {
        nestedScrollEnabled: true,
        showsVerticalScrollIndicator: true,
        contentContainerStyle: styles.screenListContent
      },
        rows.map((row) => el(ListRow, {
          key: `${title}-${row.id}`,
          title: row.name,
          subtitle: row.phone || row.email || t("contacts.noContactDetail", "No contact detail"),
          value: money(row.openingBalance || 0)
        })),
        rows.length === 0 && el(EmptyState, { text: t("contacts.noContacts", "No contacts returned.") })
      )
    )
  );
}

module.exports = { ContactsScreen };
