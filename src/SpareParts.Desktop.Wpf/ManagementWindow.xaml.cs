using System.Windows;

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
            Loaded += async (_, _) => await _vm.LoadAllAsync();
        }
    }
}
