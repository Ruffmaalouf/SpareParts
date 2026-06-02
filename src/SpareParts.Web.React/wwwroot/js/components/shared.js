import { React, h } from "../core/react-runtime.js";

export function PageHeader({ title, action, kicker, subtitle }) {
  return h("header", { className: "page-header" },
    h("div", { className: "page-title" },
      kicker && h("span", null, kicker),
      h("h2", null, title),
      subtitle && h("p", null, subtitle)
    ),
    action || null
  );
}

export function StatusLine({ status, tone = "muted" }) {
  return status ? h("p", { className: `status-line ${tone}` }, status) : null;
}

export function NotificationCenter({ notifications, onDismiss }) {
  if (!notifications.length) return null;

  return h("section", { className: "notification-stack", "aria-live": "polite", "aria-label": "Notifications" },
    notifications.map((notification) =>
      h("article", { className: "notification-card", key: notification.id },
        h("div", null,
          h("span", null, notification.title),
          h("strong", null, notification.message)
        ),
        h("button", {
          type: "button",
          onClick: () => onDismiss(notification.id),
          "aria-label": "Dismiss notification"
        }, "x")
      )
    )
  );
}

export function Badge({ tone = "neutral", children }) {
  return h("span", { className: `badge badge-${tone}` }, children);
}

export function EmptyState({ title, subtitle, action }) {
  return h("div", { className: "empty-state-block" },
    h("strong", null, title),
    subtitle ? h("p", null, subtitle) : null,
    action || null
  );
}

export function DataTable({ columns, rows, getRowKey, emptyText }) {
  return h(React.Fragment, null,
    h("table", null,
      h("thead", null, h("tr", null,
        columns.map((column) => h("th", { key: column.key }, column.label))
      )),
      h("tbody", null,
        rows.map((row, index) =>
          h("tr", { key: getRowKey(row, index) },
            columns.map((column) =>
              h("td", { key: column.key }, column.render(row, index))
            )
          )
        )
      )
    ),
    rows.length === 0 && h("p", { className: "empty-state" }, emptyText)
  );
}
