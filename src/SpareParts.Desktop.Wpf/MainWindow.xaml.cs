using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SpareParts.Desktop.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();         // ✅ make sure this is here
            DataContext = new PosViewModel();
        }
        private void BrandExpander_Expanded(object sender, RoutedEventArgs e)
        {
            var expanded = sender as Expander;
            if (expanded == null) return;

            // find parent panel containing all expanders
            var parent = expanded.Parent as Panel;
            if (parent == null) return;

            // collapse all other expanders
            foreach (var exp in parent.Children.OfType<Expander>())
            {
                if (!ReferenceEquals(exp, expanded))
                    exp.IsExpanded = false;
            }
        }
    }
}
