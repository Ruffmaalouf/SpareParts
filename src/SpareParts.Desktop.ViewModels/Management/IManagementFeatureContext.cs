using SpareParts.Desktop.Wpf.Helpers;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management;

public interface IManagementFeatureContext
{
    ManagementCoordinator Coordinator { get; }
    ICommand? ImportTableCommand { get; }
    Task RefreshAsync();
    void SetStatus(string message, bool success);
    string GetDefaultCurrencyCode();
}
