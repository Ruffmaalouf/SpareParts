using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class HalfcutViewModel : JsonListViewModelBase
    {
        private string _make = string.Empty;
        private string _model = string.Empty;
        private string _year = string.Empty;
        private string _vin = string.Empty;
        private string _color = string.Empty;
        private string _originCountry = string.Empty;
        private string _mileage = string.Empty;
        private string _askingPrice = string.Empty;

        public HalfcutViewModel(ICrudApiClient crudApi) : base(crudApi, "Load half-cuts.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }

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

        public string Vin
        {
            get => _vin;
            set { _vin = value; OnPropertyChanged(nameof(Vin)); }
        }

        public string Color
        {
            get => _color;
            set { _color = value; OnPropertyChanged(nameof(Color)); }
        }

        public string OriginCountry
        {
            get => _originCountry;
            set { _originCountry = value; OnPropertyChanged(nameof(OriginCountry)); }
        }

        public string Mileage
        {
            get => _mileage;
            set { _mileage = value; OnPropertyChanged(nameof(Mileage)); }
        }

        public string AskingPrice
        {
            get => _askingPrice;
            set { _askingPrice = value; OnPropertyChanged(nameof(AskingPrice)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/half-cut");
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

        public async Task CreateAsync()
        {
            try
            {
                var payload = new
                {
                    make = Make,
                    model = Model,
                    year = ParseIntOrNull(Year),
                    vin = Vin,
                    color = Color,
                    originCountry = OriginCountry,
                    mileage = ParseIntOrNull(Mileage),
                    askingPrice = ParseDecimalOrNull(AskingPrice)
                };
                await CrudApi.PostAsync("/api/half-cut", payload);
                Status = "Half-cut record added.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                Make = string.Empty;
                Model = string.Empty;
                Year = string.Empty;
                Vin = string.Empty;
                Color = string.Empty;
                OriginCountry = string.Empty;
                Mileage = string.Empty;
                AskingPrice = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
        private static decimal? ParseDecimalOrNull(string? value) => decimal.TryParse(value, out var result) ? result : null;
    }
}
