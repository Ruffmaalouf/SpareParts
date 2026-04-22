using System;
using Microsoft.Win32;
using SpareParts.Desktop.Wpf.Management;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public partial class UsedCarGalleryWindow : Window, INotifyPropertyChanged
    {
        private readonly ManagementCoordinator _coordinator;
        private readonly UsedCarEntry _usedCar;
        private UsedCarGalleryImageItem? _selectedImage;
        private string _galleryStatus = "Loading images...";
        private string _footerStatus = "Ready.";

        public UsedCarGalleryWindow(ManagementCoordinator coordinator, UsedCarEntry usedCar)
        {
            InitializeComponent();
            _coordinator = coordinator;
            _usedCar = usedCar;
            DataContext = this;

            Loaded += async (_, _) => await LoadImagesAsync();
        }

        public ObservableCollection<UsedCarGalleryImageItem> Images { get; } = new();

        public string WindowTitle => $"Used Car Gallery - {_usedCar.Car}";

        public string GallerySubtitle => $"Used car #{_usedCar.Id} - Add, review, and remove stored images";

        public UsedCarGalleryImageItem? SelectedImage
        {
            get => _selectedImage;
            set
            {
                if (_selectedImage == value)
                {
                    return;
                }

                _selectedImage = value;
                OnPropertyChanged(nameof(SelectedImage));
                OnPropertyChanged(nameof(SelectedPreview));
            }
        }

        public BitmapImage? SelectedPreview => SelectedImage?.Preview;

        public bool HasImages => Images.Count > 0;

        public string GalleryStatus
        {
            get => _galleryStatus;
            private set
            {
                if (_galleryStatus == value)
                {
                    return;
                }

                _galleryStatus = value;
                OnPropertyChanged(nameof(GalleryStatus));
            }
        }

        public string FooterStatus
        {
            get => _footerStatus;
            private set
            {
                if (_footerStatus == value)
                {
                    return;
                }

                _footerStatus = value;
                OnPropertyChanged(nameof(FooterStatus));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private async void AddImages_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.webp;*.bmp",
                Multiselect = true,
                Title = "Select used car images"
            };

            if (dialog.ShowDialog(this) != true || dialog.FileNames.Length == 0)
            {
                return;
            }

            try
            {
                FooterStatus = "Uploading images...";

                foreach (var fileName in dialog.FileNames)
                {
                    await _coordinator.UploadUsedCarImageAsync(_usedCar.Id, fileName);
                }

                await LoadImagesAsync();
                FooterStatus = $"{dialog.FileNames.Length} image(s) uploaded.";
            }
            catch (Exception ex)
            {
                FooterStatus = "Upload failed.";
                CustomMessageBox.Show(ex.Message, "Gallery", "Error");
            }
        }

        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedImage == null)
            {
                CustomMessageBox.Show("Select an image to delete.", "Gallery", "Warning");
                return;
            }

            try
            {
                FooterStatus = "Deleting image...";
                await _coordinator.DeleteUsedCarImageAsync(SelectedImage.Id);
                await LoadImagesAsync();
                FooterStatus = "Image deleted.";
            }
            catch (Exception ex)
            {
                FooterStatus = "Delete failed.";
                CustomMessageBox.Show(ex.Message, "Gallery", "Error");
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadImagesAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task LoadImagesAsync()
        {
            try
            {
                FooterStatus = "Refreshing gallery...";
                var images = await _coordinator.GetUsedCarImagesAsync(_usedCar.Id);

                Images.Clear();
                foreach (var image in images)
                {
                    Images.Add(new UsedCarGalleryImageItem
                    {
                        Id = image.Id,
                        CreatedAt = image.CreatedAt,
                        MimeType = image.MimeType,
                        Preview = TryCreateBitmap(image.ImageData)
                    });
                }

                SelectedImage = Images.FirstOrDefault();
                GalleryStatus = Images.Count == 0
                    ? "No images saved yet."
                    : $"{Images.Count} image(s) loaded from the database.";
                FooterStatus = "Gallery ready.";
                OnPropertyChanged(nameof(HasImages));
            }
            catch (Exception ex)
            {
                GalleryStatus = "Unable to load gallery images.";
                FooterStatus = "Load failed.";
                CustomMessageBox.Show(ex.Message, "Gallery", "Error");
            }
        }

        private static BitmapImage? TryCreateBitmap(byte[]? bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            using var stream = new MemoryStream(bytes);
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
