using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Auth;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf
{
    public class RoleManagementDto : RoleDto, INotifyPropertyChanged
    {
        public new event PropertyChangedEventHandler? PropertyChanged;
        public void Notify(string n) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
