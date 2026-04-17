using SpareParts.Desktop.Wpf.ViewModels;
using SpareParts.Domain.Sales;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace SpareParts.Desktop.Wpf
{
    public partial class MainWindow : Window
    {
        private const double ExpandedSidebarWidth = 280d;
        private const double CollapsedSidebarWidth = 0d;
        private const double SidebarOffsetWhenCollapsed = -26d;
        private static readonly TimeSpan SidebarAnimationDuration = TimeSpan.FromMilliseconds(240);

        private bool _isSidebarCollapsed;

        public MainWindow(InvoiceTabsViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            InitializeSidebarState();
        }

        private void InitializeSidebarState()
        {
            SidebarHost.Width = ExpandedSidebarWidth;
            SidebarHost.IsHitTestVisible = true;
            SidebarContent.Opacity = 1d;
            SidebarContentTranslate.X = 0d;
            ShowSidebarButton.Visibility = Visibility.Collapsed;
            ShowSidebarButton.Opacity = 0d;
            ShowSidebarButton.IsHitTestVisible = false;
        }

        private void QuickActionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            QuickActionsToggle.IsChecked = false;
        }

        private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ThemesToggle.IsChecked = false;
        }

        private void SidebarMenuToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(sender, QuickActionsToggle))
            {
                ThemesToggle.IsChecked = false;
                return;
            }

            QuickActionsToggle.IsChecked = false;
        }

        private async void InvoiceSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not InvoiceTabsViewModel vm)
            {
                return;
            }

            if (sender is not DataGrid grid || grid.SelectedItem is not SalesInvoiceLookupDto selected)
            {
                return;
            }

            try
            {
                await vm.OpenInvoiceFromSearchAsync(selected);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(ex.Message, "Invoices", "Error");
            }
        }

        private void HideSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            SetSidebarCollapsed(true);
        }

        private void ShowSidebarButton_Click(object sender, RoutedEventArgs e)
        {
            SetSidebarCollapsed(false);
        }

        private void SetSidebarCollapsed(bool isCollapsed)
        {
            if (_isSidebarCollapsed == isCollapsed)
            {
                return;
            }

            _isSidebarCollapsed = isCollapsed;

            var easing = new CubicEase
            {
                EasingMode = EasingMode.EaseInOut
            };

            if (isCollapsed)
            {
                QuickActionsToggle.IsChecked = false;
                ThemesToggle.IsChecked = false;
            }

            if (!isCollapsed)
            {
                SidebarHost.IsHitTestVisible = true;
                ShowSidebarButton.IsHitTestVisible = false;
            }

            SidebarHost.BeginAnimation(WidthProperty, new DoubleAnimation
            {
                To = isCollapsed ? CollapsedSidebarWidth : ExpandedSidebarWidth,
                Duration = new Duration(SidebarAnimationDuration),
                EasingFunction = easing
            });

            SidebarContent.BeginAnimation(OpacityProperty, new DoubleAnimation
            {
                To = isCollapsed ? 0d : 1d,
                Duration = new Duration(SidebarAnimationDuration),
                EasingFunction = easing
            });

            SidebarContentTranslate.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, new DoubleAnimation
            {
                To = isCollapsed ? SidebarOffsetWhenCollapsed : 0d,
                Duration = new Duration(SidebarAnimationDuration),
                EasingFunction = easing
            });

            if (isCollapsed)
            {
                SidebarHost.IsHitTestVisible = false;
                ShowSidebarButton.Visibility = Visibility.Visible;
                ShowSidebarButton.IsHitTestVisible = true;
                ShowSidebarButton.BeginAnimation(OpacityProperty, new DoubleAnimation
                {
                    From = ShowSidebarButton.Opacity,
                    To = 1d,
                    BeginTime = TimeSpan.FromMilliseconds(90),
                    Duration = new Duration(TimeSpan.FromMilliseconds(160)),
                    EasingFunction = easing
                });
            }
            else
            {
                var fadeOutAnimation = new DoubleAnimation
                {
                    To = 0d,
                    Duration = new Duration(TimeSpan.FromMilliseconds(140)),
                    EasingFunction = easing
                };

                fadeOutAnimation.Completed += (_, _) =>
                {
                    if (_isSidebarCollapsed)
                    {
                        return;
                    }

                    ShowSidebarButton.Visibility = Visibility.Collapsed;
                    ShowSidebarButton.IsHitTestVisible = false;
                };

                ShowSidebarButton.BeginAnimation(OpacityProperty, fadeOutAnimation);
            }
        }
    }
}
