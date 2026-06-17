using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class RegionalDemandViewModel : JsonListViewModelBase
    {
        private string _regionFilter = string.Empty;
        private DataTable _topTable = new();

        public RegionalDemandViewModel(ICrudApiClient crudApi) : base(crudApi, "Load regional demand.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }

        public string RegionFilter
        {
            get => _regionFilter;
            set { _regionFilter = value; OnPropertyChanged(nameof(RegionFilter)); }
        }

        public DataTable TopTable
        {
            get => _topTable;
            private set { _topTable = value; OnPropertyChanged(nameof(TopTable)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>($"/api/regional-demand{(string.IsNullOrWhiteSpace(RegionFilter) ? "" : "?region=" + Uri.EscapeDataString(RegionFilter))}");
                Table = JsonGridHelper.ToDataTable(items);
                var topItems = await CrudApi.GetAllAsync<JsonElement>("/api/regional-demand/top");
                TopTable = JsonGridHelper.ToDataTable(topItems);
                Status = $"{items.Count} regional row(s), {topItems.Count} top row(s) loaded.";
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
