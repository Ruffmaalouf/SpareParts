const React = require("react");
const { Pressable, ScrollView, Switch, Text, TextInput, View } = require("react-native");
const { navigationGroups } = require("../../core/app-config");
const { rowAmount, rowSubtitle, rowTitle } = require("../../core/formatters");
const { EmptyState, ListRow, Panel } = require("../ui");
const { launchMeta, rowId } = require("../../admin/resource-utils");
const { useTheme } = require("../../theme/theme-context");

const { useMemo } = React;
const el = React.createElement;

function CrudField({ field, value, onChange }) {
  const { styles, palette, t } = useTheme();
  const label = t(`fields.${field.key}`, field.label);

  if (field.type === "bool") {
    return el(View, { style: styles.adminCrudToggle },
      el(Text, { style: styles.adminCrudToggleText }, label),
      el(Switch, {
        value: Boolean(value),
        onValueChange: onChange,
        trackColor: { false: palette.line, true: palette.accent },
        thumbColor: palette.text
      })
    );
  }

  return el(View, { style: styles.adminCrudField },
    el(Text, { style: styles.fieldLabel }, label),
    el(TextInput, {
      style: styles.input,
      value: String(value ?? ""),
      onChangeText: onChange,
      keyboardType: field.keyboardType || (field.type === "number" ? "decimal-pad" : "default"),
      secureTextEntry: field.secure,
      placeholder: field.required ? t("common.required", "Required") : "",
      placeholderTextColor: palette.soft
    })
  );
}

function CrudRow({ active, row, title, subtitle, value, onPress }) {
  const { styles } = useTheme();

  return el(Pressable, {
    style: [styles.adminCrudRow, active && styles.adminCrudRowActive],
    onPress
  },
    el(View, { style: styles.adminCrudRowCopy },
      el(Text, { style: [styles.listRowTitle, active && styles.adminCrudRowTitleActive], numberOfLines: 1 }, title),
      Boolean(subtitle) && el(Text, { style: styles.listRowSubtitle, numberOfLines: 1 }, subtitle)
    ),
    Boolean(value) && el(Text, { style: styles.listRowValue, numberOfLines: 1 }, value)
  );
}

function WorkspaceLauncher({ activeScreenKey, adminScreens, onNavigate }) {
  const { styles, t } = useTheme();
  const screensByKey = useMemo(
    () => new Map((adminScreens || []).map((item) => [item.key, item])),
    [adminScreens]
  );
  const workspaceGroups = useMemo(
    () => navigationGroups.filter((group) => group.title !== "Core"),
    []
  );

  return el(Panel, { title: t("management.workspaces", "Workspaces") },
    workspaceGroups.map((group) =>
      el(View, { key: group.title, style: styles.adminGroup },
        el(Text, { style: styles.adminGroupTitle }, t(`nav.groups.${group.title}`, group.title)),
        el(View, { style: styles.adminLaunchGrid },
          group.keys.map((key) => {
            const item = screensByKey.get(key);
            if (!item) return null;
            const isActive = item.key === activeScreenKey;

            return el(Pressable, {
              key: item.key,
              style: [styles.adminLaunchButton, isActive && styles.adminLaunchButtonActive],
              onPress: () => onNavigate && onNavigate(item.key)
            },
              el(Text, {
              style: [styles.adminLaunchText, isActive && styles.adminLaunchTextActive],
              numberOfLines: 1
              }, t(`screens.${item.key}`, item.label)),
              el(Text, { style: styles.adminLaunchMeta, numberOfLines: 1 }, t(`launchMeta.${item.key}`, launchMeta(item.key)))
            );
          })
        )
      )
    )
  );
}

function ResourceListPanel({
  section,
  sectionLabel,
  crudConfig,
  rows,
  search,
  selectedId,
  onSearch,
  onSelect
}) {
  const { styles, palette, t } = useTheme();
  const label = sectionLabel || section.label;

  return el(Panel, { title: label },
    el(TextInput, {
      style: styles.input,
      value: search,
      onChangeText: onSearch,
      placeholder: t("management.searchPlaceholder", `Search ${label.toLowerCase()}`, { label: label.toLowerCase() }),
      placeholderTextColor: palette.soft
    }),
    el(View, { style: styles.managementListFrame },
      el(ScrollView, {
        nestedScrollEnabled: true,
        showsVerticalScrollIndicator: true,
        contentContainerStyle: styles.managementListContent
      },
        rows.map((row, index) => {
          const id = crudConfig ? rowId(row, crudConfig) : row.id || row.userId || row.code || index;
          const active = crudConfig && String(id) === String(selectedId);
          return crudConfig
            ? el(CrudRow, {
              key: `${section.key}-${id || index}`,
              active,
              row,
              title: rowTitle(row),
              subtitle: rowSubtitle(row) || section.endpoint,
              value: rowAmount(row),
              onPress: () => onSelect(String(id))
            })
            : el(ListRow, {
              key: `${section.key}-${id || index}`,
              title: rowTitle(row),
              subtitle: rowSubtitle(row) || section.endpoint,
              value: rowAmount(row)
            });
        }),
        rows.length === 0 && el(EmptyState, { text: t("management.noRows", `No ${label.toLowerCase()} returned.`, { label: label.toLowerCase() }) })
      )
    )
  );
}

function ResourceEditorPanel({
  section,
  sectionLabel,
  crudConfig,
  selectedRow,
  form,
  isSaving,
  onDelete,
  onFieldChange,
  onNew,
  onSave
}) {
  const { styles, t } = useTheme();
  const label = sectionLabel || section.label;

  return el(Panel, { title: crudConfig ? (selectedRow
    ? `${t("common.edit", "Edit")} ${label}`
    : `${t("common.new", "New")} ${label}`) : t("common.actions", "Actions") },
    crudConfig
      ? el(View, { style: styles.adminCrudPanel },
        el(View, { style: styles.inlineButtons },
          el(Pressable, { style: styles.secondaryButton, onPress: onNew, disabled: isSaving },
            el(Text, { style: styles.secondaryButtonText }, t("common.new", "New"))
          ),
          el(Pressable, { style: styles.primaryButton, onPress: onSave, disabled: isSaving },
            el(Text, { style: styles.primaryButtonText }, selectedRow ? t("common.save", "Save") : t("common.create", "Create"))
          ),
          crudConfig.canDelete !== false && el(Pressable, {
            style: [styles.secondaryButton, styles.usedCarDangerSoft],
            onPress: onDelete,
            disabled: isSaving || !selectedRow
          },
            el(Text, { style: styles.secondaryButtonText }, t("common.delete", "Delete"))
          )
        ),
        el(View, { style: styles.adminCrudGrid },
          crudConfig.fields.map((field) => {
            if (selectedRow && field.update === false) return null;
            if (!selectedRow && field.create === false) return null;

            return el(CrudField, {
              key: field.key,
              field,
              value: form[field.key],
              onChange: (value) => onFieldChange(field.key, value)
            });
          })
        ),
        crudConfig.canUpdate === false && selectedRow && el(Text, { style: styles.usedCarHelpText }, t("management.createOnly", "This API supports creating this record from mobile, but not editing existing rows."))
      )
      : el(EmptyState, { text: t("management.browseOnly", "This module is browse-only here. Use its dedicated tab when available.") })
  );
}

module.exports = {
  ResourceEditorPanel,
  ResourceListPanel,
  WorkspaceLauncher
};
