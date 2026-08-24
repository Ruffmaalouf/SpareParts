using System.Windows.Controls;

namespace SpareParts.Desktop.Wpf
{
    /// <summary>
    /// Ignition Client Workspace control (Report 04 §05–§06). All state and the
    /// paged child loads live in the bound <c>IgnitionClientWorkspaceViewModel</c>;
    /// this code-behind only initializes the compiled XAML.
    /// </summary>
    public partial class IgnitionClientWorkspaceControl : UserControl
    {
        public IgnitionClientWorkspaceControl()
        {
            InitializeComponent();
        }
    }
}
