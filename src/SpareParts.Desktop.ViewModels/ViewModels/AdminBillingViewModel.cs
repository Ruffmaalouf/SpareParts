using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class AdminBillingViewModel : JsonListViewModelBase
    {
        private string _tenantId = string.Empty;
        private string _packageCode = string.Empty;
        private string _billingCycle = "Monthly";
        private string _paymentId = string.Empty;
        private string _durationDays = string.Empty;
        private DataTable _subscriptionsTable = new();
        private DataTable _paymentsTable = new();
        private DataTable _invoicesTable = new();
        private DataTable _webhookEventsTable = new();

        public AdminBillingViewModel(ICrudApiClient crudApi) : base(crudApi, "Load billing data.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            ActivateSubscriptionCommand = new RelayCommand(_ => ActivateSubscriptionAsync().SafeFireAndForget(HandleException));
            RunMaintenanceCommand = new RelayCommand(_ => RunMaintenanceAsync().SafeFireAndForget(HandleException));
            MarkSelectedPaymentPaidCommand = new RelayCommand(_ => MarkSelectedPaymentPaidAsync().SafeFireAndForget(HandleException));
            ToggleSelectedPackageActiveCommand = new RelayCommand(_ => ToggleSelectedPackageActiveAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand ActivateSubscriptionCommand { get; }
        public ICommand RunMaintenanceCommand { get; }
        public ICommand MarkSelectedPaymentPaidCommand { get; }
        public ICommand ToggleSelectedPackageActiveCommand { get; }

        public DataTable SubscriptionsTable
        {
            get => _subscriptionsTable;
            private set { _subscriptionsTable = value; OnPropertyChanged(nameof(SubscriptionsTable)); }
        }

        public DataTable PaymentsTable
        {
            get => _paymentsTable;
            private set { _paymentsTable = value; OnPropertyChanged(nameof(PaymentsTable)); }
        }

        public DataTable InvoicesTable
        {
            get => _invoicesTable;
            private set { _invoicesTable = value; OnPropertyChanged(nameof(InvoicesTable)); }
        }

        public DataTable WebhookEventsTable
        {
            get => _webhookEventsTable;
            private set { _webhookEventsTable = value; OnPropertyChanged(nameof(WebhookEventsTable)); }
        }

        public string TenantId
        {
            get => _tenantId;
            set { _tenantId = value; OnPropertyChanged(nameof(TenantId)); }
        }

        public string PackageCode
        {
            get => _packageCode;
            set { _packageCode = value; OnPropertyChanged(nameof(PackageCode)); }
        }

        public string BillingCycle
        {
            get => _billingCycle;
            set { _billingCycle = value; OnPropertyChanged(nameof(BillingCycle)); }
        }

        public string PaymentId
        {
            get => _paymentId;
            set { _paymentId = value; OnPropertyChanged(nameof(PaymentId)); }
        }

        public string DurationDays
        {
            get => _durationDays;
            set { _durationDays = value; OnPropertyChanged(nameof(DurationDays)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading billing data...";
            try
            {
                var packages = await CrudApi.GetAllAsync<JsonElement>("/api/admin/pricing/packages");
                var subscriptions = await CrudApi.GetAllAsync<JsonElement>("/api/admin/subscriptions");
                var payments = await CrudApi.GetAllAsync<JsonElement>("/api/admin/payments");
                var invoices = await CrudApi.GetAllAsync<JsonElement>("/api/admin/invoices");
                var webhookEvents = await CrudApi.GetAllAsync<JsonElement>("/api/admin/webhook-events");

                Table = JsonGridHelper.ToDataTable(packages);
                SubscriptionsTable = JsonGridHelper.ToDataTable(subscriptions);
                PaymentsTable = JsonGridHelper.ToDataTable(payments);
                InvoicesTable = JsonGridHelper.ToDataTable(invoices);
                WebhookEventsTable = JsonGridHelper.ToDataTable(webhookEvents);

                Status = $"{packages.Count} package(s), {subscriptions.Count} subscription(s), {payments.Count} payment(s) loaded.";
                StatusBrush = Brushes.LightGreen;
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

        public async Task ActivateSubscriptionAsync()
        {
            var tenantId = ParseIntOrNull(TenantId);
            if (tenantId == null || string.IsNullOrWhiteSpace(PackageCode))
            {
                Status = "Tenant ID and package code are required.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            try
            {
                var payload = new
                {
                    tenantId = tenantId.Value,
                    packageCode = PackageCode,
                    billingCycle = BillingCycle,
                    paymentId = ParseIntOrNull(PaymentId),
                    durationDays = ParseIntOrNull(DurationDays)
                };
                await CrudApi.PostAsync("/api/admin/subscriptions/activate", payload);
                Status = "Subscription activated.";
                StatusBrush = Brushes.LightGreen;
                TenantId = string.Empty;
                PackageCode = string.Empty;
                PaymentId = string.Empty;
                DurationDays = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task RunMaintenanceAsync()
        {
            try
            {
                var updated = await CrudApi.PostAsync<JsonElement>("/api/admin/subscriptions/run-maintenance", new { });
                Status = $"Maintenance complete: {updated}";
                StatusBrush = Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task MarkSelectedPaymentPaidAsync()
        {
            var id = GetSelectedId();
            if (id == null)
            {
                Status = "Select a payment row first.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            try
            {
                await CrudApi.PostAsync($"/api/admin/payments/{id}/mark-paid", new { });
                Status = "Payment marked as paid.";
                StatusBrush = Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        public async Task ToggleSelectedPackageActiveAsync()
        {
            var id = GetSelectedId();
            if (id == null)
            {
                Status = "Select a package row first.";
                StatusBrush = Brushes.OrangeRed;
                return;
            }

            try
            {
                var isActive = false;
                if (SelectedRow != null)
                {
                    foreach (DataColumn column in SelectedRow.Row.Table.Columns)
                    {
                        if (string.Equals(column.ColumnName, "isActive", StringComparison.OrdinalIgnoreCase))
                        {
                            bool.TryParse(SelectedRow[column.ColumnName]?.ToString(), out isActive);
                        }
                    }
                }

                await CrudApi.PutAsync($"/api/admin/pricing/packages/{id}/active", new { isActive = !isActive });
                Status = "Package status toggled.";
                StatusBrush = Brushes.LightGreen;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static int? ParseIntOrNull(string? value) => int.TryParse(value, out var result) ? result : null;
    }
}
