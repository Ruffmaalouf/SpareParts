using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartReelViewModel : JsonListViewModelBase
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _videoUrl = string.Empty;
        private string _thumbnailUrl = string.Empty;
        private string _linkedPartId = string.Empty;
        private string _partName = string.Empty;
        private string _price = string.Empty;
        private string _currency = string.Empty;

        public PartReelViewModel(ICrudApiClient crudApi) : base(crudApi, "Load part reels.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            CreateCommand = new RelayCommand(_ => CreateAsync().SafeFireAndForget(HandleException));
            LikeCommand = new RelayCommand(_ => LikeAsync().SafeFireAndForget(HandleException));
            ArchiveCommand = new RelayCommand(_ => ArchiveAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand LikeCommand { get; }
        public ICommand ArchiveCommand { get; }

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(nameof(Description)); }
        }

        public string VideoUrl
        {
            get => _videoUrl;
            set { _videoUrl = value; OnPropertyChanged(nameof(VideoUrl)); }
        }

        public string ThumbnailUrl
        {
            get => _thumbnailUrl;
            set { _thumbnailUrl = value; OnPropertyChanged(nameof(ThumbnailUrl)); }
        }

        public string LinkedPartId
        {
            get => _linkedPartId;
            set { _linkedPartId = value; OnPropertyChanged(nameof(LinkedPartId)); }
        }

        public string PartName
        {
            get => _partName;
            set { _partName = value; OnPropertyChanged(nameof(PartName)); }
        }

        public string Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        public string Currency
        {
            get => _currency;
            set { _currency = value; OnPropertyChanged(nameof(Currency)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/part-reels");
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
                    title = Title,
                    description = Description,
                    videoUrl = VideoUrl,
                    thumbnailUrl = ThumbnailUrl,
                    linkedPartId = LinkedPartId,
                    partName = PartName,
                    price = ParseDecimalOrNull(Price),
                    currency = string.IsNullOrWhiteSpace(Currency) ? "USD" : Currency
                };
                await CrudApi.PostAsync("/api/part-reels", payload);
                Status = "Reel created.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                Title = string.Empty;
                Description = string.Empty;
                VideoUrl = string.Empty;
                ThumbnailUrl = string.Empty;
                LinkedPartId = string.Empty;
                PartName = string.Empty;
                Price = string.Empty;
                Currency = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task LikeAsync()
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
                await CrudApi.PutAsync($"/api/part-reels/{id}/like", new { });
                Status = "Reel liked.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task ArchiveAsync()
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
                await CrudApi.PutAsync($"/api/part-reels/{id}/archive", new { });
                Status = "Reel archived.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static decimal? ParseDecimalOrNull(string? value) => decimal.TryParse(value, out var result) ? result : null;
    }
}
