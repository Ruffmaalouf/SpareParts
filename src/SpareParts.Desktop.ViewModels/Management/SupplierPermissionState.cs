using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.Management
{
    public class SupplierPermissionState : INotifyPropertyChanged
    {
        private bool _canViewSupplierTab;
        private bool _canEditSupplier;
        private bool _canModifySupplier;
        private bool _canDeleteSupplier;

        public bool CanViewSupplierTab
        {
            get => _canViewSupplierTab;
            private set
            {
                if (_canViewSupplierTab == value) return;
                _canViewSupplierTab = value;
                OnPropertyChanged(nameof(CanViewSupplierTab));
                OnPropertyChanged(nameof(CanSaveSupplier));
            }
        }

        public bool CanEditSupplier
        {
            get => _canEditSupplier;
            private set
            {
                if (_canEditSupplier == value) return;
                _canEditSupplier = value;
                OnPropertyChanged(nameof(CanEditSupplier));
                OnPropertyChanged(nameof(CanSaveSupplier));
            }
        }

        public bool CanModifySupplier
        {
            get => _canModifySupplier;
            private set
            {
                if (_canModifySupplier == value) return;
                _canModifySupplier = value;
                OnPropertyChanged(nameof(CanModifySupplier));
                OnPropertyChanged(nameof(CanSaveSupplier));
            }
        }

        public bool CanDeleteSupplier
        {
            get => _canDeleteSupplier;
            private set
            {
                if (_canDeleteSupplier == value) return;
                _canDeleteSupplier = value;
                OnPropertyChanged(nameof(CanDeleteSupplier));
            }
        }

        public bool CanSaveSupplier => CanViewSupplierTab && (IsEditingSupplier ? CanModifySupplier : CanEditSupplier);

        public bool IsEditingSupplier { get; set; }

        public void Set(bool canViewSupplierTab, bool canEditSupplier, bool canModifySupplier, bool canDeleteSupplier)
        {
            CanViewSupplierTab = canViewSupplierTab;
            CanEditSupplier = canEditSupplier;
            CanModifySupplier = canModifySupplier;
            CanDeleteSupplier = canDeleteSupplier;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
