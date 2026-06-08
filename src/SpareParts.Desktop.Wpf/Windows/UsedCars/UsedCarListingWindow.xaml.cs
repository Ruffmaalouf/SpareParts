using SpareParts.Desktop.Abstractions.UsedCars;
using SpareParts.Desktop.Wpf.Management;
using SpareParts.Domain.Cars;
using System;
using System.Diagnostics;
using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class UsedCarListingWindow : Window
    {
        private readonly ManagementCoordinator _coordinator;
        private readonly UsedCarWorkspaceRequest _request;

        public UsedCarListingWindow(ManagementCoordinator coordinator, UsedCarWorkspaceRequest request)
        {
            InitializeComponent();
            _coordinator = coordinator;
            _request = request;
            Title = $"Listing Package — {request.UsedCarName}";

            Loaded += async (_, _) => await LoadPackageAsync();
        }

        private async System.Threading.Tasks.Task LoadPackageAsync()
        {
            try
            {
                var package = await _coordinator.GetUsedCarListingPackageAsync(_request.UsedCarId);
                Apply(package);
            }
            catch (Exception ex)
            {
                TitleTextBox.Text = string.Empty;
                DescriptionTextBox.Text = $"Could not generate the listing package: {ex.Message}";
            }
        }

        private void Apply(UsedCarListingPackageDto package)
        {
            TitleTextBox.Text = package.Title;
            DescriptionTextBox.Text = package.Description;
            PhotoCountText.Text = $"{package.PhotoCount} photo(s) available in this car's gallery — attach them to the listing after pasting the text above.";
            MarketplaceLinkscontrol.ItemsSource = package.MarketplaceLinks;
        }

        private void OnCopyTitleClick(object sender, RoutedEventArgs e)
            => CopyToClipboard(TitleTextBox.Text);

        private void OnCopyDescriptionClick(object sender, RoutedEventArgs e)
            => CopyToClipboard(DescriptionTextBox.Text);

        private static void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception)
            {
                // Clipboard access can transiently fail on Windows; ignore and let the user retry.
            }
        }

        private void OnOpenMarketplaceClick(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement { Tag: string url } || string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception)
            {
                // Ignore failures launching the default browser.
            }
        }
    }
}
