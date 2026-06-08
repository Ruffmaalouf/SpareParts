using System.Windows;
using System.Windows.Threading;
using SpareParts.Desktop.Wpf.Helpers;

namespace SpareParts.Desktop.Wpf
{
    public partial class ManagementWindow : Window
    {
        private readonly ManagementViewModel _vm;

        public ManagementWindow(ManagementViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;

            // Load data after the window is fully rendered
            Loaded += (_, _) =>
                Dispatcher.BeginInvoke(
                    new Action(() => _vm.LoadAllAsync().SafeFireAndForget()),
                    DispatcherPriority.ContextIdle);
        }
    }
}
