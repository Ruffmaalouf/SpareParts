using System.Windows.Controls;

namespace SpareParts.Desktop.Wpf
{
    /// <summary>
    /// Ignition Dashboard control (Report 04 §02–§03). All state lives in the
    /// bound <c>IgnitionDashboardViewModel</c>; this code-behind only initializes
    /// the compiled XAML.
    /// </summary>
    public partial class IgnitionDashboardControl : UserControl
    {
        public IgnitionDashboardControl()
        {
            InitializeComponent();
        }
    }
}
