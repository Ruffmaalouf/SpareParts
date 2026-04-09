using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Auth;
using System.ComponentModel;

namespace SpareParts.Desktop.Wpf
{
    public class RoleManagementDto : RoleDto, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public void Notify(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
