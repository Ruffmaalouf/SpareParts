using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf
{
    // ── Model for each role item ──────────────────────────────────────────────
    public class RoleItem
    {
        public string Name            { get; set; } = string.Empty;
        public string Description     { get; set; } = string.Empty;
        public string BadgeColor      { get; set; } = "#22FFFFFF";
        public string BadgeTextColor  { get; set; } = "#FFFFFF";
    }

    public partial class RoleSearchControl : UserControl
    {
        // ── Hardcoded roles (no API needed — roles are fixed in the system) ───
        private readonly List<RoleItem> _allRoles = new()
        {
            new RoleItem { Name = "Admin",   Description = "Full system access",                         BadgeColor = "#22FF5722", BadgeTextColor = "#FF7043" },
            new RoleItem { Name = "Manager", Description = "Operations access, no user management",      BadgeColor = "#2200E5FF", BadgeTextColor = "#00E5FF" },
            new RoleItem { Name = "Cashier", Description = "POS sales only",                             BadgeColor = "#2244FF44", BadgeTextColor = "#44FF44" },
        };

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
            // Populate the ListBox with all roles immediately
            ResultsList.ItemsSource = _allRoles;
        }

        // ── External SelectedRole change (e.g. loaded from DB) ───────────────
        private static void OnSelectedRoleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RoleSearchControl ctrl && e.NewValue is string role && !string.IsNullOrEmpty(role))
                ctrl.ApplySelection(ctrl._allRoles.FirstOrDefault(r => r.Name == role));
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
                    if (visible.Count == 1) { ApplySelection(visible[0]); return; }
                    OpenPopup();
                    break;
                case Key.Escape:
                    ResultsPopup.IsOpen = false;
                    RestorePill();
                    break;
                case Key.Down when ResultsPopup.IsOpen:
                    ResultsList.Focus();
                    if (ResultsList.Items.Count > 0) ResultsList.SelectedIndex = 0;
                    break;
                default:
                    ApplyFilter();
                    break;
            }
        }

        private void DropBtn_Click(object sender, RoutedEventArgs e)
            => OpenPopup();

        private void ResultItem_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ResultsList.SelectedItem is RoleItem r) ApplySelection(r);
        }

        private void ResultsList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ResultsList.SelectedItem is RoleItem r) ApplySelection(r);
            else if (e.Key == Key.Escape) { ResultsPopup.IsOpen = false; SearchBox.Focus(); }
        }

        private void ClearRole_Click(object sender, RoutedEventArgs e) => Clear();

        // ── Logic ─────────────────────────────────────────────────────────────
        private void OpenPopup()
        {
            ApplyFilter();
            ResultsPopup.IsOpen = true;
        }

        private void ApplyFilter()
        {
            var q = (RoleSearchText ?? "").Trim();
            var filtered = string.IsNullOrEmpty(q)
                ? _allRoles
                : _allRoles.Where(r => r.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
            ResultsList.ItemsSource = filtered;
            if (filtered.Count > 0) ResultsPopup.IsOpen = true;
        }

        private List<RoleItem> GetFiltered()
        {
            var q = (RoleSearchText ?? "").Trim();
            return string.IsNullOrEmpty(q)
                ? _allRoles
                : _allRoles.Where(r => r.Name.Contains(q, System.StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private void ApplySelection(RoleItem? role)
        {
            if (role == null) return;
            SelectedRole = role.Name;

            // Update badge visuals
            SelectedRoleText.Text       = role.Name;
            RoleBadge.Background        = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(
                    role.BadgeColor.Length == 9 ? role.BadgeColor : "#22FFFFFF"));
            SelectedRoleText.Foreground = new SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(role.BadgeTextColor));

            SelectedIndicator.Visibility = Visibility.Visible;
            RoleIcon.Visibility          = Visibility.Collapsed;
            DropBtn.Visibility           = Visibility.Collapsed;
            ClearBtn.Visibility          = Visibility.Visible;

            InputPill.BorderBrush     = (Brush)FindResource("AccentBrush");
            InputPill.BorderThickness = new Thickness(1);

            RoleSearchText      = string.Empty;
            SearchBox.Text      = string.Empty;
            ShowPlaceholder();
            ResultsPopup.IsOpen = false;
        }

        private void Clear()
        {
            SelectedRole                 = null;
            SelectedIndicator.Visibility = Visibility.Collapsed;
            RoleIcon.Visibility          = Visibility.Visible;
            DropBtn.Visibility           = Visibility.Visible;
            ClearBtn.Visibility          = Visibility.Collapsed;
            RoleSearchText               = string.Empty;
            SearchBox.Text               = string.Empty;
            RestorePill();
            ShowPlaceholder();
            SearchBox.Focus();
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
