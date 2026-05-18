using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class ReportBuilderRoleAccessRow : INotifyPropertyChanged
    {
        private string _roleName = string.Empty;
        private bool _canView;
        private bool _canEdit;
        private bool _canExport;

        public string RoleName
        {
            get => _roleName;
            set
            {
                if (_roleName == value) return;
                _roleName = value;
                OnPropertyChanged(nameof(RoleName));
            }
        }

        public bool CanView
        {
            get => _canView;
            set
            {
                if (_canView == value) return;
                _canView = value;
                OnPropertyChanged(nameof(CanView));
            }
        }

        public bool CanEdit
        {
            get => _canEdit;
            set
            {
                if (_canEdit == value) return;
                _canEdit = value;
                OnPropertyChanged(nameof(CanEdit));
            }
        }

        public bool CanExport
        {
            get => _canExport;
            set
            {
                if (_canExport == value) return;
                _canExport = value;
                OnPropertyChanged(nameof(CanExport));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
