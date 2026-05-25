import { h, useCallback, useEffect, useMemo, useState } from "../core/react-runtime.js";
import { featureModules, managementSections } from "../core/config.js";
import { asRows, pickFirst, rowAmount } from "../core/formatters.js";
import { crudConfigs } from "../admin/crud-config.js";
import { emptyForm, formFromRow, matchesRow, rowId, rowSubtitle, rowTitle } from "../admin/resource-utils.js";
import { CrudResourceService, ResourceService } from "../services/resource-service.js";
import { PricingCoachCard, smartPricingCoach } from "../services/pricing-coach.js";
import { DataTable, PageHeader, StatusLine } from "../components/shared.js";

const workspaceKeys = [
  "part-requests",
  "compatibility",
  "purchase-parts",
  "used-car-purchases",
  "stock-arrival",
  "used-cars",
  "repair-prep",
  "stock",
  "dead-stock",
  "accounting",
  "manual-journal",
  "report-builder",
  "whatsapp",
  "business-assistant",
  "ar"
];

function fieldLabel(field, t) {
  return t(`fields.${field.key}`, field.label || field.key);
}

export function genericColumns(t) {
  return [
    { key: "name", label: fieldLabel({ key: "name", label: "Name" }, t), render: (row) => rowTitle(row) },
    { key: "detail", label: "Detail", render: (row) => rowSubtitle(row) },
    { key: "amount", label: "Amount / Qty", render: (row) => rowAmount(row) },
    { key: "state", label: "State", render: (row) => pickFirst(row, ["status", "isActive", "isEnabled", "createdAt"]) }
  ];
}

function SegmentButton({ active, children, onClick }) {
  return h("button", {
    className: active ? "segment active" : "segment",
    type: "button",
    onClick
  }, children);
}

function WorkspaceLauncher({ activeView, onView, t }) {
  const modules = featureModules.filter((module) => workspaceKeys.includes(module.key));

  return h("section", { className: "workspace-launcher" },
    modules.map((module) =>
      h("article", { className: activeView === module.key ? "workspace-card active" : "workspace-card", key: module.key },
        h("div", null,
          h("span", null, module.source),
          h("h3", null, t(`screens.${module.key}`, module.label))
        ),
        h("p", null, module.capabilities.join(" / ")),
        h("button", {
          className: "secondary-button",
          type: "button",
          onClick: () => onView(module.key)
        }, t("management.openWorkspace", "Open workspace"))
      )
    )
  );
}

function ResourceListPanel({ section, sectionLabel, crudConfig, rows, search, selectedId, onSearch, onSelect, t }) {
  return h("section", { className: "admin-panel resource-list-panel" },
    h("div", { className: "admin-panel-header" },
      h("h3", null, sectionLabel),
      h("span", null, `${rows.length}`)
    ),
    h("input", {
      value: search,
      onChange: (event) => onSearch(event.target.value),
      placeholder: t("management.searchPlaceholder", `Search ${sectionLabel}`, { label: sectionLabel })
    }),
    h("div", { className: "resource-list-frame" },
      rows.map((row, index) => {
        const id = crudConfig ? rowId(row, crudConfig) : row.id || index;
        const active = String(id) === String(selectedId);
        return h("button", {
          key: `${section.key}-${id || index}`,
          className: active ? "resource-row active" : "resource-row",
          type: "button",
          onClick: () => onSelect(String(id))
        },
          h("strong", null, rowTitle(row)),
          h("span", null, rowSubtitle(row) || rowAmount(row) || "-")
        );
      }),
      rows.length === 0 && h("p", { className: "empty-state" }, t("management.noRows", `No ${sectionLabel} rows returned.`, { label: sectionLabel }))
    )
  );
}

function ResourceEditorPanel({
  sectionLabel,
  sectionKey,
  crudConfig,
  form,
  isSaving,
  selectedRow,
  onDelete,
  onFieldChange,
  onNew,
  onSave,
  t
}) {
  const pricingCoach = sectionKey === "parts"
    ? smartPricingCoach(Object.assign({}, selectedRow || {}, form), 0)
    : null;

  if (!crudConfig) {
    return h("section", { className: "admin-panel resource-editor-panel" },
      h("h3", null, sectionLabel),
      h("p", { className: "empty-state" }, t("management.browseOnly", "This record type is browse-only here."))
    );
  }

  return h("section", { className: "admin-panel resource-editor-panel" },
    h("div", { className: "admin-panel-header" },
      h("h3", null, selectedRow ? t("common.edit", "Edit") : t("common.new", "New")),
      h("div", { className: "row-actions" },
        h("button", { className: "secondary-button", type: "button", onClick: onNew }, t("common.new", "New")),
        h("button", { className: "primary-button", type: "button", onClick: onSave, disabled: isSaving }, selectedRow ? t("common.save", "Save") : t("common.create", "Create")),
        h("button", { className: "secondary-button danger-button", type: "button", onClick: onDelete, disabled: isSaving || !selectedRow || crudConfig.canDelete === false }, t("common.delete", "Delete"))
      )
    ),
    h("div", { className: "editor-grid" },
      crudConfig.fields.map((field) =>
        h("label", {
          key: field.key,
          className: field.type === "bool" ? "checkbox-field" : ""
        },
          field.type === "bool"
            ? [
              h("input", {
                key: "input",
                type: "checkbox",
                checked: Boolean(form[field.key]),
                onChange: (event) => onFieldChange(field.key, event.target.checked)
              }),
              h("span", { key: "label" }, fieldLabel(field, t))
            ]
            : [
              h("span", { key: "label" }, fieldLabel(field, t)),
              h("input", {
                key: "input",
                value: form[field.key] ?? "",
                onChange: (event) => onFieldChange(field.key, event.target.value),
                type: field.secure ? "password" : field.type === "number" ? "number" : field.type || "text"
              })
            ]
        )
      )
    ),
    pricingCoach && h(PricingCoachCard, { h, coach: pricingCoach, compact: true })
  );
}

export function ManagementWorkspaceView({ api, activeView, onView, t }) {
  const [managementTab, setManagementTab] = useState("workspaces");
  const [sectionKey, setSectionKey] = useState(managementSections[0].key);
  const [rows, setRows] = useState([]);
  const [selectedId, setSelectedId] = useState("");
  const [form, setForm] = useState({});
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);

  const section = managementSections.find((item) => item.key === sectionKey) || managementSections[0];
  const sectionLabel = t(`management.sections.${section.key}`, section.label);
  const crudConfig = crudConfigs[section.key];
  const service = useMemo(
    () => crudConfig ? new CrudResourceService(api, crudConfig) : new ResourceService(api),
    [api, crudConfig]
  );
  const selectedRow = useMemo(
    () => crudConfig ? rows.find((row) => String(rowId(row, crudConfig)) === String(selectedId)) || null : null,
    [crudConfig, rows, selectedId]
  );
  const visibleRows = useMemo(
    () => rows.filter((row) => matchesRow(row, search)),
    [rows, search]
  );

  const load = useCallback(async () => {
    setIsLoading(true);
    setStatus(t("management.loading", `Loading ${sectionLabel.toLowerCase()}...`, { label: sectionLabel.toLowerCase() }));

    try {
      const nextRows = asRows(await service.list(section.endpoint));
      setRows(nextRows);
      if (crudConfig) {
        setSelectedId((current) => {
          if (current && nextRows.some((row) => String(rowId(row, crudConfig)) === String(current))) return current;
          return String(rowId(nextRows[0], crudConfig) || "");
        });
      }
      setStatus(t("management.loaded", `${sectionLabel} loaded.`, { label: sectionLabel }));
    } catch (error) {
      setRows([]);
      setStatus(error.message || t("management.loadError", `Could not load ${sectionLabel.toLowerCase()}.`, { label: sectionLabel.toLowerCase() }));
    } finally {
      setIsLoading(false);
    }
  }, [crudConfig, section, sectionLabel, service, t]);

  useEffect(() => {
    if (managementTab === "records") load();
  }, [load, managementTab]);

  useEffect(() => {
    setSearch("");
    setSelectedId("");
    setForm(crudConfig ? emptyForm(crudConfig) : {});
  }, [crudConfig, section.key]);

  useEffect(() => {
    if (crudConfig) setForm(formFromRow(selectedRow, crudConfig));
  }, [crudConfig, selectedRow]);

  const startNew = useCallback(() => {
    if (!crudConfig) return;
    setSelectedId("");
    setForm(emptyForm(crudConfig));
    setStatus(t("management.newReady", `New ${sectionLabel.toLowerCase()} form ready.`, { label: sectionLabel.toLowerCase() }));
  }, [crudConfig, sectionLabel, t]);

  const save = useCallback(async () => {
    if (!crudConfig) return;

    setIsSaving(true);
    try {
      await service.save(selectedRow, form);
      setStatus(selectedRow
        ? t("management.updated", `${sectionLabel} updated.`, { label: sectionLabel })
        : t("management.created", `${sectionLabel} created.`, { label: sectionLabel }));
      await load();
    } catch (error) {
      setStatus(error.message || t("management.saveError", `Could not save ${sectionLabel.toLowerCase()}.`, { label: sectionLabel.toLowerCase() }));
    } finally {
      setIsSaving(false);
    }
  }, [crudConfig, form, load, sectionLabel, selectedRow, service, t]);

  const remove = useCallback(async () => {
    if (!crudConfig || !selectedRow) {
      setStatus(t("management.selectFirst", `Select a ${sectionLabel.toLowerCase()} row first.`, { label: sectionLabel.toLowerCase() }));
      return;
    }

    if (!window.confirm(t("management.deleteQuestion", `Delete ${sectionLabel}?`, { label: sectionLabel }))) return;

    setIsSaving(true);
    try {
      await service.remove(selectedRow);
      setStatus(t("management.deleted", `${sectionLabel} deleted.`, { label: sectionLabel }));
      setSelectedId("");
      await load();
    } catch (error) {
      setStatus(error.message || t("management.deleteError", `Could not delete ${sectionLabel.toLowerCase()}.`, { label: sectionLabel.toLowerCase() }));
    } finally {
      setIsSaving(false);
    }
  }, [crudConfig, load, sectionLabel, selectedRow, service, t]);

  return h("section", { className: "screen" },
    h(PageHeader, {
      title: t("management.title", "Management"),
      action: managementTab === "records"
        ? h("button", { className: "secondary-button", type: "button", onClick: load, disabled: isLoading }, t("common.refresh", "Refresh"))
        : null
    }),
    h("div", { className: "segmented-scroll" },
      h(SegmentButton, { active: managementTab === "workspaces", onClick: () => setManagementTab("workspaces") }, t("management.workspaces", "Workspaces")),
      h(SegmentButton, { active: managementTab === "records", onClick: () => setManagementTab("records") }, t("management.records", "Records"))
    ),
    managementTab === "workspaces" && h(WorkspaceLauncher, { activeView, onView, t }),
    managementTab === "records" && h("div", { className: "records-workspace" },
      h("div", { className: "segmented-scroll" },
        managementSections.map((item) =>
          h(SegmentButton, {
            key: item.key,
            active: item.key === section.key,
            onClick: () => setSectionKey(item.key)
          }, t(`management.sections.${item.key}`, item.label))
        )
      ),
      h(StatusLine, { status }),
      h("section", { className: "module-summary" },
        h("div", null,
          h("span", null, "WPF source"),
          h("strong", null, section.source)
        ),
        h("div", null,
          h("span", null, "API"),
          h("strong", null, section.endpoint)
        )
      ),
      h("section", { className: "management-workspace-grid" },
        h(ResourceListPanel, {
          section,
          sectionLabel,
          crudConfig,
          rows: visibleRows,
          search,
          selectedId,
          onSearch: setSearch,
          onSelect: setSelectedId,
          t
        }),
        h(ResourceEditorPanel, {
          sectionLabel,
          sectionKey: section.key,
          crudConfig,
          form,
          isSaving,
          selectedRow,
          onDelete: remove,
          onFieldChange: (key, value) => setForm((current) => ({ ...current, [key]: value })),
          onNew: startNew,
          onSave: save,
          t
        })
      ),
      h("section", { className: "table-panel compact-table-panel" },
        h(DataTable, {
          columns: genericColumns(t),
          rows: visibleRows,
          getRowKey: (row, index) => `${section.key}-${crudConfig ? rowId(row, crudConfig) : row.id || index}`,
          emptyText: t("management.noRows", `No ${sectionLabel.toLowerCase()} rows returned.`, { label: sectionLabel.toLowerCase() })
        })
      )
    )
  );
}
