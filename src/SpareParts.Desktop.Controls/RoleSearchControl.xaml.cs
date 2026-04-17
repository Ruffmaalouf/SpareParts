using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    public partial class RoleSearchControl : UserControl
    {
        private readonly List<RoleItem> _allRoles = new()
        {
            new RoleItem { Name = "Admin",   Description = "Full system access",                    BadgeColor = "#22FF5722", BadgeTextColor = "#FF7043" },
            new RoleItem { Name = "Manager", Description = "Operations access, no user management", BadgeColor = "#2200E5FF", BadgeTextColor = "#00E5FF" },
            new RoleItem { Name = "Cashier", Description = "POS sales only",                        BadgeColor = "#2244FF44", BadgeTextColor = "#44FF44" },
        };

        private bool _suppressClose;

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
                d = VisualTreeHelper.GetParent(d) ??
                    LogicalTreeHelper.GetParent(d) as DependencyObject;
            }
            return false;
        }

        // ── Callback when SelectedRole is set externally (e.g. from DB) ──────

        private static void OnSelectedRoleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not RoleSearchControl ctrl) return;

            if (e.NewValue is string role && !string.IsNullOrEmpty(role))
            {
                // Make sure the role exists in the list (add if custom)
                var found = ctrl._allRoles.FirstOrDefault(r =>
                    string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase));

                if (found == null)
                {
                    found = new RoleItem { Name = role, Description = "Custom role",
                                          BadgeColor = "#22AAAAAA", BadgeTextColor = "#CCCCCC" };
                    ctrl._allRoles.Add(found);
                    ctrl.ResultsList.ItemsSource = null;
                    ctrl.ResultsList.ItemsSource = ctrl._allRoles;
                }

                ctrl.ApplySelectionVisuals(found);
            }
            else
            {
                ctrl.ResetVisuals();
            }
        }

        // ── Event Handlers ────────────────────────────────────────────────────

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HidePlaceholder();
            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
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

        private List<RoleItem> GetFiltered()
        {
            var q = (RoleSearchText ?? "").Trim();
            return string.IsNullOrEmpty(q)
                ? _allRoles
                : _allRoles.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void SelectRole(RoleItem role)
        {
            // Set via SetCurrentValue so the DP callback does NOT re-fire ApplySelectionVisuals
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
                Name           = name,
                Description    = "Custom role",
                BadgeColor     = "#22AAAAAA",
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

            SelectedRoleText.Text        = role.Name;
            SelectedIndicator.Visibility = Visibility.Visible;
            RoleIcon.Visibility          = Visibility.Collapsed;
            DropBtn.Visibility           = Visibility.Collapsed;
            ClearBtn.Visibility          = Visibility.Visible;

            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);
            RoleSearchText            = string.Empty;
            SearchBox.Text            = string.Empty;
            ShowPlaceholder();
        }

        private void ResetVisuals()
        {
            SelectedIndicator.Visibility = Visibility.Collapsed;
            RoleIcon.Visibility          = Visibility.Visible;
            DropBtn.Visibility           = Visibility.Visible;
            ClearBtn.Visibility          = Visibility.Collapsed;
            RoleSearchText               = string.Empty;
            SearchBox.Text               = string.Empty;
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
            InputPill.BorderBrush     = (Brush)FindResource("BorderBrush");
            InputPill.BorderThickness = new Thickness(1);
        }
    }
}
