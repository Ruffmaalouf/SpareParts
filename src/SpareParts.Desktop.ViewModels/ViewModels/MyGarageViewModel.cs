using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class MyGarageViewModel : JsonListViewModelBase
    {
        private string _make = string.Empty;
        private string _model = string.Empty;
        private string _year = string.Empty;
        private string _trim = string.Empty;
        private string _vin = string.Empty;
        private string _mileage = string.Empty;
        private string _nickname = string.Empty;
        private bool _setAsDefault;
        private string _resultText = string.Empty;

        public MyGarageViewModel(ICrudApiClient crudApi) : base(crudApi, "Load my garage.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            DecodeVinCommand = new RelayCommand(_ => DecodeVinAsync().SafeFireAndForget(HandleException));
            SetDefaultCommand = new RelayCommand(_ => SetDefaultAsync().SafeFireAndForget(HandleException));
            DeleteCommand = new RelayCommand(_ => DeleteAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand DecodeVinCommand { get; }
        public ICommand SetDefaultCommand { get; }
        public ICommand DeleteCommand { get; }

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

        public string Trim
        {
            get => _trim;
            set { _trim = value; OnPropertyChanged(nameof(Trim)); }
        }

        public string Vin
        {
            get => _vin;
            set { _vin = value; OnPropertyChanged(nameof(Vin)); }
        }

        public string Mileage
        {
            get => _mileage;
            set { _mileage = value; OnPropertyChanged(nameof(Mileage)); }
        }

        public string Nickname
        {
            get => _nickname;
            set { _nickname = value; OnPropertyChanged(nameof(Nickname)); }
        }

        public bool SetAsDefault
        {
            get => _setAsDefault;
            set { _setAsDefault = value; OnPropertyChanged(nameof(SetAsDefault)); }
        }

        public string ResultText
        {
            get => _resultText;
            private set { _resultText = value; OnPropertyChanged(nameof(ResultText)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/vehicle-profile");
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
                    year = ParseIntOrNull(Year) ?? 0,
                    trim = Trim,
                    vinNumber = Vin,
                    mileage = ParseIntOrNull(Mileage),
                    nickname = Nickname,
                    setAsDefault = SetAsDefault
                };
                await CrudApi.PostAsync("/api/vehicle-profile", payload);
                Status = "Vehicle added to garage.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                Make = string.Empty;
                Model = string.Empty;
                Year = string.Empty;
                Trim = string.Empty;
                Vin = string.Empty;
                Mileage = string.Empty;
                Nickname = string.Empty;
                SetAsDefault = false;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task DecodeVinAsync()
        {
            try
            {
                var query = $"vin={Uri.EscapeDataString(Vin ?? string.Empty)}";
                var data = await CrudApi.GetAsync<JsonElement>($"/api/vehicle-profile/vin-decode?{query}");
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "VIN decoded.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task SetDefaultAsync()
        {
            var id = GetSelectedId();
            if (id == null)
            {
                Status = "Select a row first.";
                StatusBrush = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            try
            {
                await CrudApi.PutAsync($"/api/vehicle-profile/{id}/set-default", new { });
                Status = "Default vehicle updated.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task DeleteAsync()
        {
            var id = GetSelectedId();
            if (id == null)
            {
                Status = "Select a row first.";
                StatusBrush = System.Windows.Media.Brushes.OrangeRed;
                return;
            }

            try
            {
                await CrudApi.DeleteAsync($"/api/vehicle-profile/{id}");
                Status = "Vehicle removed.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
