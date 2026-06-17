using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class CommunityGuardViewModel : JsonListViewModelBase
    {
        private string _targetType = string.Empty;
        private string _targetId = string.Empty;
        private string _category = string.Empty;
        private string? _details;

        public CommunityGuardViewModel(ICrudApiClient crudApi) : base(crudApi, "Load community guard reports.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            SubmitCommand = new RelayCommand(_ => SubmitAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand SubmitCommand { get; }

        public string TargetType
        {
            get => _targetType;
            set { _targetType = value; OnPropertyChanged(nameof(TargetType)); }
        }

        public string TargetId
        {
            get => _targetId;
            set { _targetId = value; OnPropertyChanged(nameof(TargetId)); }
        }

        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(nameof(Category)); }
        }

        public string? Details
        {
            get => _details;
            set { _details = value; OnPropertyChanged(nameof(Details)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/community-guard");
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

        public async Task SubmitAsync()
        {
            try
            {
                var payload = new
                {
                    targetType = ParseIntOrNull(TargetType) ?? 0,
                    targetId = ParseIntOrNull(TargetId) ?? 0,
                    category = ParseIntOrNull(Category) ?? 0,
                    details = Details
                };
                await CrudApi.PostAsync("/api/community-guard", payload);
                Status = "Report submitted.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                TargetType = string.Empty;
                TargetId = string.Empty;
                Category = string.Empty;
                Details = string.Empty;
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
