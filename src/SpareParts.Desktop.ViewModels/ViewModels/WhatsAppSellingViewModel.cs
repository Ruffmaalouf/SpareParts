using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class WhatsAppSellingViewModel : JsonListViewModelBase
    {
        private string _phoneNumber = string.Empty;

        public WhatsAppSellingViewModel(ICrudApiClient crudApi) : base(crudApi, "Load WhatsApp selling settings.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            SaveCommand = new RelayCommand(_ => SaveAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set { _phoneNumber = value; OnPropertyChanged(nameof(PhoneNumber)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/app-constants");
                Table = JsonGridHelper.ToDataTable(items);
                Status = $"{items.Count} item(s) loaded.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;

                try
                {
                    foreach (DataRow row in Table.Rows)
                    {
                        string? keyValue = null;
                        foreach (DataColumn column in Table.Columns)
                        {
                            if (string.Equals(column.ColumnName, "key", StringComparison.OrdinalIgnoreCase))
                            {
                                keyValue = row[column]?.ToString();
                                break;
                            }
                        }

                        if (string.Equals(keyValue, "WhatsAppNumber", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (DataColumn column in Table.Columns)
                            {
                                if (string.Equals(column.ColumnName, "value", StringComparison.OrdinalIgnoreCase))
                                {
                                    PhoneNumber = row[column]?.ToString() ?? string.Empty;
                                    break;
                                }
                            }
                            break;
                        }
                    }
                }
                catch
                {
                    // Best effort only; leave PhoneNumber as-is if parsing fails.
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task SaveAsync()
        {
            try
            {
                var payload = new { key = "WhatsAppNumber", value = PhoneNumber };
                await CrudApi.PostAsync("/api/app-constants", payload);
                Status = "WhatsApp number saved.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}
