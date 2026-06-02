using SpareParts.Desktop.Wpf.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management;

internal sealed class ManagementFeatureContext : IManagementFeatureContext
{
    private readonly Func<Task> _refresh;
    private readonly Action<string, bool> _setStatus;
    private readonly Func<string> _getCurrencyCode;

    public ManagementCoordinator Coordinator { get; }
    public ICommand? ImportTableCommand { get; }

    public ManagementFeatureContext(
        ManagementCoordinator coordinator,
        Func<Task> refresh,
        Action<string, bool> setStatus,
        ICommand? importTableCommand = null,
        Func<string>? getCurrencyCode = null)
    {
        Coordinator = coordinator;
        ImportTableCommand = importTableCommand;
        _refresh = refresh;
        _setStatus = setStatus;
        _getCurrencyCode = getCurrencyCode ?? (() => "USD");
    }

    public Task RefreshAsync() => _refresh();
    public void SetStatus(string message, bool success) => _setStatus(message, success);
    public string GetDefaultCurrencyCode() => _getCurrencyCode();
}
