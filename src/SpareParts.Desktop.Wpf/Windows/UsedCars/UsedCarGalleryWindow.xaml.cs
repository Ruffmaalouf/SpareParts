using System;
using Microsoft.Win32;
using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Abstractions.UsedCars;
using SpareParts.Desktop.Wpf.Management;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf
{
    public partial class UsedCarGalleryWindow : Window, INotifyPropertyChanged
    {
        private const double MinPreviewZoom = 1.0;
        private const double MaxPreviewZoom = 6.0;
        private const double PreviewZoomStep = 0.25;

        private readonly ManagementCoordinator _coordinator;
        private readonly UsedCarWorkspaceRequest _request;
        private readonly IFilePickerService _filePickerService;
        private readonly IUserNotificationService _notificationService;
        private UsedCarGalleryImageItem? _selectedImage;
        private string _galleryStatus = "Loading images...";
        private string _footerStatus = "Ready.";
        private double _previewZoom = MinPreviewZoom;
        private double _previewViewportWidth = 1;
        private double _previewViewportHeight = 1;
        private int _previewRotationAngle;
        private bool _isActualSizePreview;
        private bool _isPreviewDragging;
        private Point _previewDragStart;
        private double _previewDragHorizontalOffset;
        private double _previewDragVerticalOffset;
        private bool _isFullScreen;
        private WindowState _restoreWindowState;
        private WindowStyle _restoreWindowStyle;
        private ResizeMode _restoreResizeMode;

        public UsedCarGalleryWindow(
            ManagementCoordinator coordinator,
            UsedCarWorkspaceRequest request,
            IFilePickerService filePickerService,
            IUserNotificationService notificationService)
        {
            InitializeComponent();
            _coordinator = coordinator;
            _request = request;
            _filePickerService = filePickerService;
            _notificationService = notificationService;
            DataContext = this;

            Loaded += async (_, _) => await LoadImagesAsync();
        }

        public ObservableCollection<UsedCarGalleryImageItem> Images { get; } = new();

        public string WindowTitle => $"Used Car Gallery - {_request.UsedCarName}";

        public string GallerySubtitle => $"Used car #{_request.UsedCarId} - Add, review, and remove stored images";

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
                _previewRotationAngle = 0;
                _isActualSizePreview = false;
                OnPropertyChanged(nameof(SelectedImage));
                OnPropertyChanged(nameof(SelectedPreview));
                OnPropertyChanged(nameof(HasSelectedPreview));
                OnPropertyChanged(nameof(PreviewRotationAngle));
                OnPreviewImageSizingChanged();
                ResetPreviewZoom();
            }
        }

        public BitmapImage? SelectedPreview => SelectedImage?.Preview;

        public bool HasSelectedPreview => SelectedPreview != null;

        public double PreviewImageWidth
            => Math.Max(1, _isActualSizePreview ? SelectedPreview?.Width ?? 1 : _previewViewportWidth);

        public double PreviewImageHeight
            => Math.Max(1, _isActualSizePreview ? SelectedPreview?.Height ?? 1 : _previewViewportHeight);

        public double PreviewZoom
        {
            get => _previewZoom;
            private set
            {
                var nextZoom = Math.Round(Math.Clamp(value, MinPreviewZoom, MaxPreviewZoom), 2);
                if (Math.Abs(_previewZoom - nextZoom) < 0.001)
                {
                    return;
                }

                _previewZoom = nextZoom;
                OnPropertyChanged(nameof(PreviewZoom));
                OnPropertyChanged(nameof(PreviewZoomLabel));
                UpdatePreviewCursor();
            }
        }

        public string PreviewZoomLabel
            => !_isActualSizePreview && Math.Abs(PreviewZoom - MinPreviewZoom) < 0.001
                ? "Fit"
                : $"{PreviewZoom * 100:0}%";

        public int PreviewRotationAngle => _previewRotationAngle;

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
            var fileNames = _filePickerService.PickFiles(new FilePickerRequest
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.webp;*.bmp",
                AllowMultiple = true,
                Title = "Select used car images"
            });

            if (fileNames.Count == 0)
            {
                return;
            }

            try
            {
                FooterStatus = "Uploading images...";

                foreach (var fileName in fileNames)
                {
                    await _coordinator.UploadUsedCarImageAsync(_request.UsedCarId, fileName);
                }

                await LoadImagesAsync();
                FooterStatus = $"{fileNames.Count} image(s) uploaded.";
            }
            catch (Exception ex)
            {
                FooterStatus = "Upload failed.";
                _notificationService.Show(ex.Message, "Gallery", NotificationKind.Error);
            }
        }

        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            await DeleteSelectedImageAsync();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadImagesAsync();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            ChangePreviewZoom(-PreviewZoomStep);
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            ChangePreviewZoom(PreviewZoomStep);
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            FitPreview();
        }

        private void ActualSize_Click(object sender, RoutedEventArgs e)
        {
            ShowActualSizePreview();
        }

        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            RotatePreview(-90);
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            RotatePreview(90);
        }

        private void PreviewScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!HasSelectedPreview || (Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            {
                return;
            }

            e.Handled = true;
            ChangePreviewZoom(e.Delta > 0 ? PreviewZoomStep : -PreviewZoomStep, e.GetPosition(PreviewScrollViewer));
        }

        private void PreviewScrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!HasSelectedPreview)
            {
                return;
            }

            if (e.ClickCount == 2)
            {
                ToggleFitActualPreview();
                e.Handled = true;
                return;
            }

            if (!CanPanPreview())
            {
                return;
            }

            _isPreviewDragging = true;
            _previewDragStart = e.GetPosition(PreviewScrollViewer);
            _previewDragHorizontalOffset = PreviewScrollViewer.HorizontalOffset;
            _previewDragVerticalOffset = PreviewScrollViewer.VerticalOffset;
            PreviewScrollViewer.CaptureMouse();
            PreviewScrollViewer.Cursor = Cursors.Hand;
            e.Handled = true;
        }

        private void PreviewScrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndPreviewDrag();
        }

        private void PreviewScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isPreviewDragging)
            {
                UpdatePreviewCursor();
                return;
            }

            var currentPosition = e.GetPosition(PreviewScrollViewer);
            var delta = currentPosition - _previewDragStart;
            PreviewScrollViewer.ScrollToHorizontalOffset(_previewDragHorizontalOffset - delta.X);
            PreviewScrollViewer.ScrollToVerticalOffset(_previewDragVerticalOffset - delta.Y);
        }

        private void PreviewScrollViewer_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                EndPreviewDrag();
            }
        }

        private void PreviewScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _previewViewportWidth = Math.Max(1, e.NewSize.Width);
            _previewViewportHeight = Math.Max(1, e.NewSize.Height);
            OnPreviewImageSizingChanged();
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Left)
            {
                SelectAdjacentImage(-1);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Right)
            {
                SelectAdjacentImage(1);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Add or Key.OemPlus)
            {
                ChangePreviewZoom(PreviewZoomStep);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Subtract or Key.OemMinus)
            {
                ChangePreviewZoom(-PreviewZoomStep);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Delete)
            {
                e.Handled = true;
                await DeleteSelectedImageAsync();
                return;
            }

            if (e.Key is Key.F)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
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
                var images = await _coordinator.GetUsedCarImagesAsync(_request.UsedCarId);

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
                _notificationService.Show(ex.Message, "Gallery", NotificationKind.Error);
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

        private async Task DeleteSelectedImageAsync()
        {
            if (SelectedImage == null)
            {
                _notificationService.Show("Select an image to delete.", "Gallery", NotificationKind.Warning);
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
                _notificationService.Show(ex.Message, "Gallery", NotificationKind.Error);
            }
        }

        private void ChangePreviewZoom(double delta)
        {
            ChangePreviewZoom(delta, GetPreviewCenterPoint());
        }

        private void ChangePreviewZoom(double delta, Point anchorPoint)
        {
            if (!HasSelectedPreview)
            {
                return;
            }

            var previousZoom = PreviewZoom;
            var nextZoom = Math.Round(Math.Clamp(previousZoom + delta, MinPreviewZoom, MaxPreviewZoom), 2);
            if (Math.Abs(previousZoom - nextZoom) < 0.001)
            {
                return;
            }

            var contentAnchorX = (PreviewScrollViewer.HorizontalOffset + anchorPoint.X) / previousZoom;
            var contentAnchorY = (PreviewScrollViewer.VerticalOffset + anchorPoint.Y) / previousZoom;

            PreviewZoom = nextZoom;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                PreviewImage.UpdateLayout();
                PreviewScrollViewer.UpdateLayout();
                PreviewScrollViewer.ScrollToHorizontalOffset(Math.Max(0, contentAnchorX * nextZoom - anchorPoint.X));
                PreviewScrollViewer.ScrollToVerticalOffset(Math.Max(0, contentAnchorY * nextZoom - anchorPoint.Y));
                UpdatePreviewCursor();
            }));
        }

        private void FitPreview()
        {
            if (!HasSelectedPreview)
            {
                return;
            }

            _isActualSizePreview = false;
            OnPreviewImageSizingChanged();
            ResetPreviewZoom();
        }

        private void ShowActualSizePreview()
        {
            if (!HasSelectedPreview)
            {
                return;
            }

            _isActualSizePreview = true;
            OnPreviewImageSizingChanged();
            ResetPreviewZoom();
        }

        private void ToggleFitActualPreview()
        {
            if (_isActualSizePreview && Math.Abs(PreviewZoom - MinPreviewZoom) < 0.001)
            {
                FitPreview();
            }
            else
            {
                ShowActualSizePreview();
            }
        }

        private void RotatePreview(int degrees)
        {
            if (!HasSelectedPreview)
            {
                return;
            }

            _previewRotationAngle = NormalizeAngle(_previewRotationAngle + degrees);
            OnPropertyChanged(nameof(PreviewRotationAngle));
            ResetPreviewScrollPosition();
        }

        private void SelectAdjacentImage(int direction)
        {
            if (Images.Count == 0)
            {
                return;
            }

            var currentIndex = SelectedImage == null ? -1 : Images.IndexOf(SelectedImage);
            var nextIndex = currentIndex < 0
                ? 0
                : (currentIndex + direction + Images.Count) % Images.Count;
            SelectedImage = Images[nextIndex];
            ImagesListBox.ScrollIntoView(SelectedImage);
        }

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _restoreWindowState = WindowState;
                _restoreWindowStyle = WindowStyle;
                _restoreResizeMode = ResizeMode;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                _isFullScreen = true;
                return;
            }

            WindowStyle = _restoreWindowStyle;
            ResizeMode = _restoreResizeMode;
            WindowState = _restoreWindowState;
            _isFullScreen = false;
        }

        private void ResetPreviewZoom()
        {
            PreviewZoom = MinPreviewZoom;
            OnPropertyChanged(nameof(PreviewZoomLabel));
            ResetPreviewScrollPosition();
        }

        private void ResetPreviewScrollPosition()
        {
            if (!IsLoaded)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                PreviewScrollViewer.ScrollToHorizontalOffset(0);
                PreviewScrollViewer.ScrollToVerticalOffset(0);
                UpdatePreviewCursor();
            }));
        }

        private void OnPreviewImageSizingChanged()
        {
            OnPropertyChanged(nameof(PreviewImageWidth));
            OnPropertyChanged(nameof(PreviewImageHeight));
            OnPropertyChanged(nameof(PreviewZoomLabel));
        }

        private Point GetPreviewCenterPoint()
            => new(PreviewScrollViewer.ViewportWidth / 2, PreviewScrollViewer.ViewportHeight / 2);

        private bool CanPanPreview()
            => HasSelectedPreview && (PreviewScrollViewer.ScrollableWidth > 0 || PreviewScrollViewer.ScrollableHeight > 0);

        private void UpdatePreviewCursor()
        {
            if (!IsLoaded)
            {
                return;
            }

            PreviewScrollViewer.Cursor = CanPanPreview() ? Cursors.Hand : Cursors.Arrow;
        }

        private void EndPreviewDrag()
        {
            if (!_isPreviewDragging)
            {
                UpdatePreviewCursor();
                return;
            }

            _isPreviewDragging = false;
            PreviewScrollViewer.ReleaseMouseCapture();
            UpdatePreviewCursor();
        }

        private static int NormalizeAngle(int angle)
        {
            var normalized = angle % 360;
            return normalized < 0 ? normalized + 360 : normalized;
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
