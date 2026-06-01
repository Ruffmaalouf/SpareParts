using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartPassportViewModel : INotifyPropertyChanged
    {
        private readonly IPartsApiClient _partsApi;
        private readonly List<PartDto> _allParts = new();
        private string _filterText = string.Empty;
        private string _status = "Open Part Passport to choose an inventory part.";
        private Brush _statusBrush = Brushes.LightGray;
        private bool _isLoading;
        private PartDto? _selectedPart;

        public PartPassportViewModel(IPartsApiClient partsApi)
        {
            _partsApi = partsApi;
            RefreshCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            CopyLinkCommand = new RelayCommand(_ => CopyLink());
            OpenPublicCardCommand = new RelayCommand(_ => OpenPublicCard());
            PrepareWhatsAppCommand = new RelayCommand(_ => PrepareWhatsApp());
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<PartDto> VisibleParts { get; } = new();

        public ICommand RefreshCommand { get; }
        public ICommand CopyLinkCommand { get; }
        public ICommand OpenPublicCardCommand { get; }
        public ICommand PrepareWhatsAppCommand { get; }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (!SetField(ref _filterText, value))
                {
                    return;
                }

                ApplyFilter();
            }
        }

        public string Status
        {
            get => _status;
            private set => SetField(ref _status, value);
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set => SetField(ref _statusBrush, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetField(ref _isLoading, value);
        }

        public PartDto? SelectedPart
        {
            get => _selectedPart;
            set
            {
                if (!SetField(ref _selectedPart, value))
                {
                    return;
                }

                NotifyPassportChanged();
                if (value != null)
                {
                    SetStatus($"{PartTitle} selected. Review the draft before sending it.", Brushes.LightGreen);
                }
            }
        }

        public string PartTitle => SelectedPart?.Name ?? "Choose an inventory part";
        public string PartCode => SelectedPart?.InternalCode ?? "No part selected";
        public string OemNumber => TextOrFallback(SelectedPart?.OEMNumber, "Not recorded");
        public string DonorSource => SelectedPart?.UsedCarId is int usedCarId ? $"Donor car #{usedCarId:N0}" : "Warehouse stock";
        public string Condition => SelectedPart?.Condition.ToString() ?? "Not recorded";
        public string ShelfLocation => SelectedPart?.UsedCarId is int ? "Donor vehicle inventory" : "Shelf stock";
        public string Availability => SelectedPart == null ? "Select a part" : $"{SelectedPart.AvailableQuantity:N0} available";
        public string Price => SelectedPart == null ? "Not priced" : $"{SelectedPart.Currency} {SelectedPart.SalePrice:N2}";
        public string PublicUrl => BuildPublicUrl(SelectedPart);
        public bool HasSelection => SelectedPart != null;

        public async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            SetStatus("Loading passport-ready inventory...", Brushes.LightGoldenrodYellow);

            try
            {
                var parts = await _partsApi.GetPartsAsync();
                _allParts.Clear();
                _allParts.AddRange(parts);
                ApplyFilter();
                SelectedPart = _allParts.FirstOrDefault(part => part.UsedCarId.HasValue && !string.IsNullOrWhiteSpace(part.OEMNumber))
                    ?? _allParts.FirstOrDefault();
                SetStatus(
                    SelectedPart == null
                        ? "No inventory parts are available for a passport yet."
                        : "Choose a part, review its passport draft, then copy or open the public link.",
                    SelectedPart == null ? Brushes.LightGoldenrodYellow : Brushes.LightGreen);
            }
            catch (Exception ex)
            {
                _allParts.Clear();
                VisibleParts.Clear();
                SelectedPart = null;
                SetStatus($"Could not load passport inventory: {ex.Message}", Brushes.IndianRed);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilter()
        {
            var query = FilterText.Trim();
            var filtered = _allParts
                .Where(part => string.IsNullOrWhiteSpace(query) ||
                    Contains(part.InternalCode, query) ||
                    Contains(part.Name, query) ||
                    Contains(part.OEMNumber, query) ||
                    Contains(part.Barcode, query))
                .Take(120)
                .ToArray();

            VisibleParts.Clear();
            foreach (var part in filtered)
            {
                VisibleParts.Add(part);
            }
        }

        private void CopyLink()
        {
            if (!HasSelection)
            {
                SetStatus("Choose a part before copying a passport link.", Brushes.LightGoldenrodYellow);
                return;
            }

            Clipboard.SetText(PublicUrl);
            SetStatus("Passport link copied. Paste it into WhatsApp when the draft is ready.", Brushes.LightGreen);
        }

        private void OpenPublicCard()
        {
            if (!HasSelection)
            {
                SetStatus("Choose a part before opening a public passport.", Brushes.LightGoldenrodYellow);
                return;
            }

            Launch(PublicUrl);
            SetStatus("Public passport opened in your browser.", Brushes.LightGreen);
        }

        private void PrepareWhatsApp()
        {
            if (!HasSelection)
            {
                SetStatus("Choose a part before preparing a WhatsApp message.", Brushes.LightGoldenrodYellow);
                return;
            }

            var message = $"Part Passport: {PartTitle}{Environment.NewLine}{PublicUrl}";
            Launch($"https://wa.me/?text={Uri.EscapeDataString(message)}");
            SetStatus("WhatsApp share draft opened.", Brushes.LightGreen);
        }

        private static void Launch(string url)
        {
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
        }

        private static string BuildPublicUrl(PartDto? part)
        {
            if (part == null)
            {
                return string.Empty;
            }

            var query = new Dictionary<string, string>
            {
                ["name"] = part.Name,
                ["code"] = part.InternalCode,
                ["condition"] = $"{part.Condition} / Bench checked",
                ["donor"] = part.UsedCarId is int usedCarId ? $"Donor car #{usedCarId:N0}" : "Warehouse stock",
                ["shelf"] = part.UsedCarId.HasValue ? "Donor vehicle inventory" : "Shelf stock"
            };

            if (!string.IsNullOrWhiteSpace(part.OEMNumber))
            {
                query["oem"] = part.OEMNumber;
            }

            var encodedQuery = string.Join(
                "&",
                query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
            return $"{AppSettings.PublicWebBaseUrl.TrimEnd('/')}/passport/{Slugify(part.Name)}?{encodedQuery}";
        }

        private static string Slugify(string? value)
        {
            var slug = new string((value ?? "part")
                .Trim()
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) ? character : '-')
                .ToArray());

            while (slug.Contains("--", StringComparison.Ordinal))
            {
                slug = slug.Replace("--", "-", StringComparison.Ordinal);
            }

            return slug.Trim('-') is { Length: > 0 } trimmed ? trimmed : "part";
        }

        private static bool Contains(string? value, string query)
            => value?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

        private static string TextOrFallback(string? value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

        private void NotifyPassportChanged()
        {
            OnPropertyChanged(nameof(PartTitle));
            OnPropertyChanged(nameof(PartCode));
            OnPropertyChanged(nameof(OemNumber));
            OnPropertyChanged(nameof(DonorSource));
            OnPropertyChanged(nameof(Condition));
            OnPropertyChanged(nameof(ShelfLocation));
            OnPropertyChanged(nameof(Availability));
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(PublicUrl));
            OnPropertyChanged(nameof(HasSelection));
        }

        private void SetStatus(string message, Brush brush)
        {
            Status = message;
            StatusBrush = brush;
        }

        private void HandleBackgroundException(Exception ex)
        {
            IsLoading = false;
            SetStatus(ex.Message, Brushes.IndianRed);
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
