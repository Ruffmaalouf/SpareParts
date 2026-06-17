using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class DoesItFitViewModel : JsonListViewModelBase
    {
        private string _checkPartId = string.Empty;
        private string _checkMake = string.Empty;
        private string _checkModel = string.Empty;
        private string _checkYear = string.Empty;
        private string _managePartId = string.Empty;
        private string _compatibleMakes = string.Empty;
        private string _compatibleModels = string.Empty;
        private string _yearFrom = string.Empty;
        private string _yearTo = string.Empty;
        private string _oemNumbers = string.Empty;
        private string _alternativeNumbers = string.Empty;
        private string _resultText = string.Empty;
        private string _manageResultText = string.Empty;

        public DoesItFitViewModel(ICrudApiClient crudApi) : base(crudApi, "Check part compatibility.")
        {
            CheckCommand = new RelayCommand(_ => CheckAsync().SafeFireAndForget(HandleException));
            LoadManageDataCommand = new RelayCommand(_ => LoadManageDataAsync().SafeFireAndForget(HandleException));
            SaveCompatibilityCommand = new RelayCommand(_ => SaveCompatibilityAsync().SafeFireAndForget(HandleException));
        }

        public ICommand CheckCommand { get; }
        public ICommand LoadManageDataCommand { get; }
        public ICommand SaveCompatibilityCommand { get; }

        public string CheckPartId
        {
            get => _checkPartId;
            set { _checkPartId = value; OnPropertyChanged(nameof(CheckPartId)); }
        }

        public string CheckMake
        {
            get => _checkMake;
            set { _checkMake = value; OnPropertyChanged(nameof(CheckMake)); }
        }

        public string CheckModel
        {
            get => _checkModel;
            set { _checkModel = value; OnPropertyChanged(nameof(CheckModel)); }
        }

        public string CheckYear
        {
            get => _checkYear;
            set { _checkYear = value; OnPropertyChanged(nameof(CheckYear)); }
        }

        public string ManagePartId
        {
            get => _managePartId;
            set { _managePartId = value; OnPropertyChanged(nameof(ManagePartId)); }
        }

        public string CompatibleMakes
        {
            get => _compatibleMakes;
            set { _compatibleMakes = value; OnPropertyChanged(nameof(CompatibleMakes)); }
        }

        public string CompatibleModels
        {
            get => _compatibleModels;
            set { _compatibleModels = value; OnPropertyChanged(nameof(CompatibleModels)); }
        }

        public string YearFrom
        {
            get => _yearFrom;
            set { _yearFrom = value; OnPropertyChanged(nameof(YearFrom)); }
        }

        public string YearTo
        {
            get => _yearTo;
            set { _yearTo = value; OnPropertyChanged(nameof(YearTo)); }
        }

        public string OemNumbers
        {
            get => _oemNumbers;
            set { _oemNumbers = value; OnPropertyChanged(nameof(OemNumbers)); }
        }

        public string AlternativeNumbers
        {
            get => _alternativeNumbers;
            set { _alternativeNumbers = value; OnPropertyChanged(nameof(AlternativeNumbers)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public string ManageResultText
        {
            get => _manageResultText;
            private set { _manageResultText = value; OnPropertyChanged(nameof(ManageResultText)); }
        }

        public async Task CheckAsync()
        {
            IsLoading = true;
            Status = "Checking...";
            try
            {
                var query = $"partId={CheckPartId}&make={Uri.EscapeDataString(CheckMake ?? string.Empty)}&model={Uri.EscapeDataString(CheckModel ?? string.Empty)}&year={CheckYear}";
                var data = await CrudApi.GetAsync<JsonElement>($"/api/part-compatibility/check?{query}");
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Compatibility check complete.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadManageDataAsync()
        {
            try
            {
                var data = await CrudApi.GetAsync<JsonElement>($"/api/part-compatibility/{ManagePartId}");
                ManageResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Compatibility data loaded.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task SaveCompatibilityAsync()
        {
            try
            {
                var payload = new
                {
                    partId = ParseIntOrNull(ManagePartId) ?? 0,
                    compatibleMakes = SplitCsv(CompatibleMakes),
                    compatibleModels = SplitCsv(CompatibleModels),
                    yearFrom = ParseIntOrNull(YearFrom),
                    yearTo = ParseIntOrNull(YearTo),
                    oemNumbers = SplitCsv(OemNumbers),
                    alternativeNumbers = SplitCsv(AlternativeNumbers)
                };
                await CrudApi.PostAsync("/api/part-compatibility", payload);
                Status = "Compatibility record saved.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;

        private static List<string> SplitCsv(string? value) => string.IsNullOrWhiteSpace(value)
            ? new List<string>()
            : value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    }
}
