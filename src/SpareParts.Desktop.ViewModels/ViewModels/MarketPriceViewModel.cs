using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class MarketPriceViewModel : JsonListViewModelBase
    {
        private string _make = string.Empty;
        private string _model = string.Empty;
        private string _year = string.Empty;

        public MarketPriceViewModel(ICrudApiClient crudApi) : base(crudApi, "Search market prices.")
        {
            LoadCommand = new RelayCommand(_ => SearchAsync().SafeFireAndForget(HandleException));
            SearchCommand = new RelayCommand(_ => SearchAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand SearchCommand { get; }

        public string Make
        {
            get => _make;
            set { _make = value; OnPropertyChanged(nameof(Make)); }
        }

        public string Model
        {
            get => _model;
            set { _model = value; OnPropertyChanged(nameof(Model)); }
        }

        public string Year
        {
            get => _year;
            set { _year = value; OnPropertyChanged(nameof(Year)); }
        }

        public async Task SearchAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var parameters = new List<string>();
                if (!string.IsNullOrWhiteSpace(Make)) parameters.Add($"make={Uri.EscapeDataString(Make)}");
                if (!string.IsNullOrWhiteSpace(Model)) parameters.Add($"model={Uri.EscapeDataString(Model)}");
                if (!string.IsNullOrWhiteSpace(Year)) parameters.Add($"year={Uri.EscapeDataString(Year)}");

                var url = parameters.Count > 0
                    ? $"/api/market-price?{string.Join("&", parameters)}"
                    : "/api/market-price";

                var items = await CrudApi.GetAllAsync<JsonElement>(url);
                Table = JsonGridHelper.ToDataTable(items);
                Status = $"{items.Count} item(s) loaded.";
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
    }
}
