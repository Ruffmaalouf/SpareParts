const React = require("react");
const { Alert, Pressable, ScrollView, Text, View } = require("react-native");
const { managementSections } = require("../core/app-config");
const { crudConfigs } = require("../admin/crud-config");
const { emptyForm, formFromRow, matchesRow, rowId } = require("../admin/resource-utils");
const { CrudResourceService, ResourceService } = require("../services/resource-service");
const {
  ResourceEditorPanel,
  ResourceListPanel,
  WorkspaceLauncher
} = require("../components/admin/crud-workspace");
const { ScreenHeader, ScreenScroll, StatusText } = require("../components/ui");
const { useTheme } = require("../theme/theme-context");
const { rowTitle } = require("../core/formatters");

const { useCallback, useEffect, useMemo, useState } = React;
const el = React.createElement;

function ManagementScreen({ api, onNavigate, adminScreens, activeScreenKey }) {
  const { styles, t } = useTheme();
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
      const nextRows = await service.list(section.endpoint);
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

  const setFormValue = useCallback((key, value) => {
    setForm((current) => ({ ...current, [key]: value }));
  }, []);

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
    if (!crudConfig || !selectedRow) return;

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

  const confirmRemove = useCallback(() => {
    if (!selectedRow) {
      setStatus(t("management.selectFirst", `Select a ${sectionLabel.toLowerCase()} row first.`, { label: sectionLabel.toLowerCase() }));
      return;
    }

    Alert.alert(
      t("management.deleteQuestion", `Delete ${sectionLabel}?`, { label: sectionLabel }),
      t("management.deleteMessage", `Delete ${rowTitle(selectedRow)}?`, { name: rowTitle(selectedRow) }),
      [
        { text: t("common.cancel", "Cancel"), style: "cancel" },
        { text: t("common.delete", "Delete"), style: "destructive", onPress: () => { remove(); } }
      ]
    );
  }, [remove, sectionLabel, selectedRow, t]);

  return el(ScreenScroll, null,
    el(ScreenHeader, {
      title: t("management.title", "Management"),
      actionTitle: managementTab === "records" ? t("common.refresh", "Refresh") : null,
      onAction: load,
      loading: isLoading
    }),
    el(ScrollView, {
      horizontal: true,
      showsHorizontalScrollIndicator: false,
      contentContainerStyle: styles.segmentRail
    },
      [
        { key: "workspaces", label: t("management.workspaces", "Workspaces") },
        { key: "records", label: t("management.records", "Records") }
      ].map((item) =>
        el(Pressable, {
          key: item.key,
          style: [styles.segmentButton, item.key === managementTab && styles.segmentButtonActive],
          onPress: () => setManagementTab(item.key)
        }, el(Text, { style: [styles.segmentText, item.key === managementTab && styles.segmentTextActive] }, item.label))
      )
    ),
    managementTab === "workspaces" && el(WorkspaceLauncher, {
      activeScreenKey,
      adminScreens,
      onNavigate
    }),
    managementTab === "records" && el(React.Fragment, null,
      el(ScrollView, {
        horizontal: true,
        showsHorizontalScrollIndicator: false,
        contentContainerStyle: styles.segmentRail
      },
      managementSections.map((item) =>
        el(Pressable, {
          key: item.key,
          style: [styles.segmentButton, item.key === section.key && styles.segmentButtonActive],
          onPress: () => setSectionKey(item.key)
        }, el(Text, { style: [styles.segmentText, item.key === section.key && styles.segmentTextActive] }, t(`management.sections.${item.key}`, item.label)))
      )
      ),
      el(StatusText, { value: status }),
      el(View, { style: styles.managementWorkspaceGrid },
        el(ResourceListPanel, {
        section,
        sectionLabel,
        crudConfig,
          rows: visibleRows,
          search,
          selectedId,
          onSearch: setSearch,
          onSelect: setSelectedId
        }),
        el(ResourceEditorPanel, {
        section,
        sectionLabel,
        crudConfig,
          selectedRow,
          form,
          isSaving,
          onDelete: confirmRemove,
          onFieldChange: setFormValue,
          onNew: startNew,
          onSave: save
        })
      )
    )
  );
}

module.exports = { ManagementScreen };
