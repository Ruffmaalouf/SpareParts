using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class ManagementWindow : Window
    {
        private readonly ManagementViewModel _vm;

        public ManagementWindow()
        {
            InitializeComponent();
            _vm         = new ManagementViewModel();
            DataContext = _vm;

            // Load data after the window is fully rendered
            Loaded += async (_, _) => await _vm.LoadAllAsync();
        }
    }
}
