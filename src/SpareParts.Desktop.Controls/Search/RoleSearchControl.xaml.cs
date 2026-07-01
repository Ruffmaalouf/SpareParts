using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public partial class RoleSearchControl : UserControl
    {
        private readonly List<RoleItem> _allRoles = new();

        private bool _suppressClose;
        private INotifyCollectionChanged? _availableRolesCollection;

        // ── Dependency Properties ─────────────────────────────────────────────

        public static readonly DependencyProperty SelectedRoleProperty =
            DependencyProperty.Register(nameof(SelectedRole), typeof(string),
                typeof(RoleSearchControl),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedRoleChanged));

        public string? SelectedRole
        {
            get => (string?)GetValue(SelectedRoleProperty);
            set => SetValue(SelectedRoleProperty, value);
        }

        public static readonly DependencyProperty SelectedRoleIdProperty =
            DependencyProperty.Register(nameof(SelectedRoleId), typeof(int?),
                typeof(RoleSearchControl),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedRoleIdChanged));

        public int? SelectedRoleId
        {
            get => (int?)GetValue(SelectedRoleIdProperty);
            set => SetValue(SelectedRoleIdProperty, value);
        }

        public static readonly DependencyProperty AvailableRolesProperty =
            DependencyProperty.Register(nameof(AvailableRoles), typeof(IEnumerable<RoleItem>),
                typeof(RoleSearchControl),
                new PropertyMetadata(null, OnAvailableRolesChanged));

        public IEnumerable<RoleItem>? AvailableRoles
        {
            get => (IEnumerable<RoleItem>?)GetValue(AvailableRolesProperty);
            set => SetValue(AvailableRolesProperty, value);
        }

        public static readonly DependencyProperty RoleSearchTextProperty =
            DependencyProperty.Register(nameof(RoleSearchText), typeof(string),
                typeof(RoleSearchControl), new PropertyMetadata(string.Empty));

        public string RoleSearchText
        {
            get => (string)GetValue(RoleSearchTextProperty);
            set => SetValue(RoleSearchTextProperty, value);
        }

        // ── Constructor ───────────────────────────────────────────────────────

        public RoleSearchControl()
        {
            InitializeComponent();
            // Assign after InitializeComponent — NEVER put items in XAML AND call ItemsSource
            ResultsList.ItemsSource = _allRoles;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current?.MainWindow == null)
            {
                return;
            }

            Application.Current.MainWindow.PreviewMouseDown -= MainWindow_PreviewMouseDown;
            Application.Current.MainWindow.PreviewMouseDown += MainWindow_PreviewMouseDown;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current?.MainWindow == null)
            {
                return;
            }

            Application.Current.MainWindow.PreviewMouseDown -= MainWindow_PreviewMouseDown;
        }

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!ResultsPopup.IsOpen) return;
            if (_suppressClose) { _suppressClose = false; return; }

            var hit = e.OriginalSource as DependencyObject;
            if (IsInsideControl(hit)) return;
            ClosePopup();
        }

        private bool IsInsideControl(DependencyObject? d)
        {
            while (d != null)
            {
                if (d == InputPill || d == ResultsPopup) return true;
                if (d is System.Windows.Controls.Primitives.Popup) return true;
                d = SearchTreeHelper.GetParent(d);
            }
            return false;
        }

        // ── Callback when SelectedRole is set externally (e.g. from DB) ──────

        private static void OnSelectedRoleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RoleSearchControl ctrl) return;

            if (ctrl.SelectedRoleId is > 0)
            {
                var found = ctrl._allRoles.FirstOrDefault(role => role.Id == ctrl.SelectedRoleId);
                if (found != null)
                {
                    ctrl.ApplySelectionVisuals(found);
                    return;
                }
            }

            ctrl.ResetVisuals();
        }

        private static void OnSelectedRoleIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RoleSearchControl ctrl) return;

            if (e.NewValue is int roleId && roleId > 0)
            {
                var found = ctrl._allRoles.FirstOrDefault(role => role.Id == roleId);
                if (found != null)
                {
                    ctrl.SetCurrentValue(SelectedRoleProperty, found.Name);
                    ctrl.ApplySelectionVisuals(found);
                    return;
                }

                ctrl.ResetVisuals();
                return;
            }

            ctrl.SetCurrentValue(SelectedRoleProperty, null);
            ctrl.ResetVisuals();
        }

        private static void OnAvailableRolesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RoleSearchControl ctrl) return;
            if (ctrl._availableRolesCollection != null)
            {
                ctrl._availableRolesCollection.CollectionChanged -= ctrl.AvailableRoles_CollectionChanged;
            }

            ctrl._availableRolesCollection = e.NewValue as INotifyCollectionChanged;
            if (ctrl._availableRolesCollection != null)
            {
                ctrl._availableRolesCollection.CollectionChanged += ctrl.AvailableRoles_CollectionChanged;
            }

            ctrl.ApplyAvailableRoles(e.NewValue as IEnumerable<RoleItem>);
        }

        private void AvailableRoles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            => ApplyAvailableRoles(AvailableRoles);

        // ── Event Handlers ────────────────────────────────────────────────────

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HidePlaceholder();
            InputPill.BorderBrush = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
            OpenPopup();
        }

        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    var visible = GetFiltered();
                    if (visible.Count == 1) { SelectRole(visible[0]); return; }
                    OpenPopup();
                    break;
                case Key.Escape:
                    ClosePopup();
                    RestorePill();
                    break;
                case Key.Down when ResultsPopup.IsOpen:
                    _suppressClose = true;
                    ResultsList.Focus();
                    if (ResultsList.Items.Count > 0) ResultsList.SelectedIndex = 0;
                    break;
                default:
                    ApplyFilter();
                    break;
            }
        }

        private void DropBtn_Click(object sender, RoutedEventArgs e)
        {
            _suppressClose = true;
            OpenPopup();
        }

        private void ResultItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            _suppressClose = true;
            if (ResultsList.SelectedItem is RoleItem r) SelectRole(r);
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ResultsList.SelectedItem is RoleItem r) SelectRole(r);
            else if (e.Key == Key.Escape) { ClosePopup(); SearchBox.Focus(); }
        }

        private void ClearRole_Click(object sender, RoutedEventArgs e)
        {
            // Use SetCurrentValue to avoid re-entering OnSelectedRoleChanged
            SetCurrentValue(SelectedRoleIdProperty, null);
            SetCurrentValue(SelectedRoleProperty, null);
            ResetVisuals();
        }

        private void AddRole_Click(object sender, RoutedEventArgs e)
        {
            _suppressClose = true;
            AddNewRole();
        }

        private void NewRoleBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { _suppressClose = true; AddNewRole(); }
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        private void OpenPopup()
        {
            ApplyFilter();
            ResultsPopup.IsOpen = true;
            NewRoleBox.Clear();
        }

        private void ApplyFilter()
        {
            var q = (RoleSearchText ?? "").Trim();
            var filtered = string.IsNullOrEmpty(q)
                ? _allRoles
                : _allRoles.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();

            ResultsList.ItemsSource = filtered;
            if (filtered.Count > 0) ResultsPopup.IsOpen = true;
        }

        private void ApplyAvailableRoles(IEnumerable<RoleItem>? roles)
        {
            if (roles == null)
            {
                return;
            }

            _allRoles.Clear();
            foreach (var role in roles)
            {
                if (role.Id <= 0 || string.IsNullOrWhiteSpace(role.Name))
                {
                    continue;
                }

                _allRoles.Add(new RoleItem
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    BadgeColor = role.BadgeColor,
                    BadgeTextColor = role.BadgeTextColor
                });
            }

            ResultsList.ItemsSource = null;
            ResultsList.ItemsSource = _allRoles;
            if (SelectedRoleId is > 0)
            {
                OnSelectedRoleIdChanged(this, new DependencyPropertyChangedEventArgs(SelectedRoleIdProperty, null, SelectedRoleId));
            }
            else if (!string.IsNullOrWhiteSpace(SelectedRole))
            {
                OnSelectedRoleChanged(this, new DependencyPropertyChangedEventArgs(SelectedRoleProperty, null, SelectedRole));
            }
        }

        private List<RoleItem> GetFiltered()
        {
            var q = (RoleSearchText ?? "").Trim();
            return string.IsNullOrEmpty(q)
                ? _allRoles
                : _allRoles.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void SelectRole(RoleItem role)
        {
            if (role.Id <= 0)
            {
                return;
            }

            // Set via SetCurrentValue so the DP callback does NOT re-fire ApplySelectionVisuals
            SetCurrentValue(SelectedRoleIdProperty, (int?)role.Id);
            SetCurrentValue(SelectedRoleProperty, role.Name);
            ApplySelectionVisuals(role);
            ClosePopup();
        }

        private void AddNewRole()
        {
            var name = NewRoleBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            if (_allRoles.Any(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                NewRoleBox.Clear();
                return;
            }

            var newRole = new RoleItem
            {
                Name = name,
                Description = "Custom role",
                BadgeColor = "#22AAAAAA",
                BadgeTextColor = "#CCCCCC"
            };
            _allRoles.Add(newRole);
            NewRoleBox.Clear();
            ApplyFilter();
            SelectRole(newRole);
        }

        private void ApplySelectionVisuals(RoleItem role)
        {
            try
            {
                var bg = (Color)ColorConverter.ConvertFromString(role.BadgeColor);
                RoleBadge.Background = new SolidColorBrush(bg);
            }
            catch { RoleBadge.Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0x57, 0x22)); }

            try
            {
                var fg = (Color)ColorConverter.ConvertFromString(role.BadgeTextColor);
                SelectedRoleText.Foreground = new SolidColorBrush(fg);
            }
            catch { SelectedRoleText.Foreground = Brushes.White; }

            SelectedRoleText.Text = role.Name;
            SelectedIndicator.Visibility = Visibility.Visible;
            RoleIcon.Visibility = Visibility.Collapsed;
            DropBtn.Visibility = Visibility.Collapsed;
            ClearBtn.Visibility = Visibility.Visible;

            InputPill.BorderBrush = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
            RoleSearchText = string.Empty;
            SearchBox.Text = string.Empty;
            ShowPlaceholder();
        }

        private void ResetVisuals()
        {
            SelectedIndicator.Visibility = Visibility.Collapsed;
            RoleIcon.Visibility = Visibility.Visible;
            DropBtn.Visibility = Visibility.Visible;
            ClearBtn.Visibility = Visibility.Collapsed;
            RoleSearchText = string.Empty;
            SearchBox.Text = string.Empty;
            NewRoleBox.Clear();
            RestorePill();
            ShowPlaceholder();
        }

        private void ClosePopup()
        {
            ResultsPopup.IsOpen = false;
            _suppressClose = false;
        }

        private void ShowPlaceholder() => Placeholder.Visibility = Visibility.Visible;
        private void HidePlaceholder() => Placeholder.Visibility = Visibility.Collapsed;

        private void RestorePill()
        {
            InputPill.BorderBrush = (Brush)FindResource("BorderBrush");
            InputPill.BorderThickness = new Thickness(1);
        }
    }
}
