using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public abstract class JsonListViewModelBase : INotifyPropertyChanged
    {
        protected readonly ICrudApiClient CrudApi;
        private bool _isLoading;
        private string _status;
        private Brush _statusBrush = Brushes.LightGray;
        private DataTable _table = new();
        private DataRowView? _selectedRow;

        protected JsonListViewModelBase(ICrudApiClient crudApi, string initialStatus)
        {
            CrudApi = crudApi;
            _status = initialStatus;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsLoading
        {
            get => _isLoading;
            protected set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public string Status
        {
            get => _status;
            protected set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            protected set { _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        public DataTable Table
        {
            get => _table;
            protected set { _table = value; OnPropertyChanged(nameof(Table)); }
        }

        public DataRowView? SelectedRow
        {
            get => _selectedRow;
            set { _selectedRow = value; OnPropertyChanged(nameof(SelectedRow)); }
        }

        protected long? GetSelectedId(string columnName = "id")
        {
            if (_selectedRow == null) return null;
            foreach (DataColumn column in _selectedRow.Row.Table.Columns)
            {
                if (string.Equals(column.ColumnName, columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return long.TryParse(_selectedRow[column.ColumnName]?.ToString(), out var id) ? id : null;
                }
            }
            return null;
        }

        protected void HandleException(Exception ex)
        {
            Status = ex.Message;
            StatusBrush = Brushes.OrangeRed;
            IsLoading = false;
        }

        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
