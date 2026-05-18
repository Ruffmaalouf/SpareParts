using System.Windows.Controls;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public partial class BusinessAssistantControl : UserControl
    {
        public BusinessAssistantControl()
        {
            InitializeComponent();
        }

        private void QuestionBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            e.Handled = true;
            if (DataContext is ViewModels.BusinessAssistantViewModel vm)
            {
                vm.AskCommand.Execute(null);
            }
        }
    }
}
