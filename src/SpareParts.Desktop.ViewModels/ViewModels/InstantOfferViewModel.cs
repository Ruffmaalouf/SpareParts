using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class InstantOfferViewModel : JsonListViewModelBase
    {
        private string _partName = string.Empty;
        private string? _partDescription;
        private string _conditionGrade = string.Empty;
        private string _contactName = string.Empty;
        private string? _contactPhone;
        private string _resultText = string.Empty;

        public InstantOfferViewModel(ICrudApiClient crudApi) : base(crudApi, "Load instant offers.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }

        public string PartName
        {
            get => _partName;
            set { _partName = value; OnPropertyChanged(nameof(PartName)); }
        }

        public string? PartDescription
        {
            get => _partDescription;
            set { _partDescription = value; OnPropertyChanged(nameof(PartDescription)); }
        }

        public string ConditionGrade
        {
            get => _conditionGrade;
            set { _conditionGrade = value; OnPropertyChanged(nameof(ConditionGrade)); }
        }

        public string ContactName
        {
            get => _contactName;
            set { _contactName = value; OnPropertyChanged(nameof(ContactName)); }
        }

        public string? ContactPhone
        {
            get => _contactPhone;
            set { _contactPhone = value; OnPropertyChanged(nameof(ContactPhone)); }
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
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/instant-offers");
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
                    partName = PartName,
                    partDescription = PartDescription,
                    conditionGrade = ConditionGrade,
                    contactName = ContactName,
                    contactPhone = ContactPhone
                };
                var data = await CrudApi.PostAsync<JsonElement>("/api/instant-offers", payload);
                ResultText = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                Status = "Instant offer created.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                PartName = string.Empty;
                PartDescription = string.Empty;
                ConditionGrade = string.Empty;
                ContactName = string.Empty;
                ContactPhone = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}
