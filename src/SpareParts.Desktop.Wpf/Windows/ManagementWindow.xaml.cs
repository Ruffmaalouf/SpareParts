using System;
using System.Windows;
using System.Windows.Threading;
using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Wpf.Helpers;

namespace SpareParts.Desktop.Wpf
{
    public partial class ManagementWindow : Window
    {
        private readonly ManagementViewModel _vm;
        private readonly IUserNotificationService _notificationService;

        public ManagementWindow(ManagementViewModel vm, IUserNotificationService notificationService)
        {
            InitializeComponent();
            _vm = vm;
            _notificationService = notificationService;
            DataContext = _vm;

            // Load data after the window is fully rendered
            Loaded += (_, _) =>
                Dispatcher.BeginInvoke(
                    new Action(() => _vm.LoadAllAsync().SafeFireAndForget(HandleLoadException)),
                    DispatcherPriority.ContextIdle);
        }

        private void HandleLoadException(Exception ex)
        {
            _notificationService.Show(
                $"Failed to load management data: {ex.Message}",
                "Management",
                NotificationKind.Error);
        }
    }
}
