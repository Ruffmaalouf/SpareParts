using System.Windows;

namespace SpareParts.Desktop.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();         // ✅ make sure this is here
            DataContext = new PosViewModel();
        }
    }
}
