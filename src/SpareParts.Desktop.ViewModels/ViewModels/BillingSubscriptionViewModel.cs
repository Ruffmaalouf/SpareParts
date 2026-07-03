using SpareParts.Desktop.Abstractions.Dialogs;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Pricing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class BillingSubscriptionViewModel : INotifyPropertyChanged
    {
        private static readonly IReadOnlyDictionary<string, string> FeatureLabels = new Dictionary<string, string>
        {
            [FeatureCode.PartListing] = "Part Listings",
            [FeatureCode.PartImages] = "Part Photos",
            [FeatureCode.BuyerLeads] = "Buyer Leads",
            [FeatureCode.MobileSellerApp] = "Mobile Seller App",
            [FeatureCode.ReportsBasic] = "Reports",
            [FeatureCode.ReportsAdvanced] = "Advanced / Scheduled Reports",
            [FeatureCode.MultiBranch] = "Multiple Branches / Warehouses",
            [FeatureCode.BulkImport] = "Bulk Import (Excel)",
            [FeatureCode.AiTitle] = "AI Title Generation",
            [FeatureCode.AiDescription] = "AI Description & Notes",
            [FeatureCode.AiPriceSuggestion] = "AI Price Suggestions",
            [FeatureCode.PromotedListings] = "Promoted Listings",
            [FeatureCode.FeaturedParts] = "Featured Parts",
            [FeatureCode.ApiAccess] = "API Access",
            [FeatureCode.PrioritySupport] = "Priority Support",
            [FeatureCode.CustomIntegrations] = "Custom Integrations",
            [FeatureCode.WhiteLabel] = "White-label Branding"
        };

        private static readonly IReadOnlyDictionary<string, string> LimitLabels = new Dictionary<string, string>
        {
            [LimitCode.UsersCount] = "User accounts",
            [LimitCode.BranchesCount] = "Warehouses / branches",
            [LimitCode.ActivePartsCount] = "Active part listings",
            [LimitCode.ImagesPerPart] = "Photos per part",
            [LimitCode.BuyerLeadsMonthly] = "Buyer leads / month",
            [LimitCode.PromotedPartsMonthly] = "Promoted listings / month",
            [LimitCode.FeaturedPartsMonthly] = "Featured parts / month",
            [LimitCode.ApiCallsMonthly] = "API calls / month"
        };

        private readonly ICrudApiClient _crudApi;
        private readonly IUserNotificationService _notificationService;
        private bool _isLoading;
        private string _status = "Load your billing and subscription details.";
        private Brush _statusBrush = Brushes.LightGray;
        private TenantSubscriptionDto? _subscription;
        private string _selectedBillingCycle = "Monthly";
        private string? _checkoutInstructions;
        private int? _checkoutPaymentId;

        public BillingSubscriptionViewModel(ICrudApiClient crudApi, IUserNotificationService notificationService)
        {
            _crudApi = crudApi;
            _notificationService = notificationService;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            SelectMonthlyCommand = new RelayCommand(_ => SetBillingCycle("Monthly"));
            SelectYearlyCommand = new RelayCommand(_ => SetBillingCycle("Yearly"));
            UpgradeCommand = new RelayCommand(o => HandleUpgradeAsync(o as BillingPackageViewModel).SafeFireAndForget(HandleBackgroundException));
            SwitchPlanCommand = new RelayCommand(o => HandleSwitchPlanAsync(o as BillingPackageViewModel).SafeFireAndForget(HandleBackgroundException));
            StartTrialCommand = new RelayCommand(o => HandleStartTrialAsync(o as BillingPackageViewModel).SafeFireAndForget(HandleBackgroundException));
            ContactSalesCommand = new RelayCommand(_ => OpenLink("mailto:sales@spareparts.app"));
            CancelAtPeriodEndCommand = new RelayCommand(_ => HandleCancelAsync(false).SafeFireAndForget(HandleBackgroundException));
            CancelImmediatelyCommand = new RelayCommand(_ => HandleCancelAsync(true).SafeFireAndForget(HandleBackgroundException));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<BillingPackageViewModel> Packages { get; } = new();
        public ObservableCollection<PaymentDto> Payments { get; } = new();
        public ObservableCollection<InvoiceDto> Invoices { get; } = new();
        public ObservableCollection<UsageRowViewModel> UsageRows { get; } = new();
        public ObservableCollection<FeatureRowViewModel> FeatureRows { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand SelectMonthlyCommand { get; }
        public ICommand SelectYearlyCommand { get; }
        public ICommand UpgradeCommand { get; }
        public ICommand SwitchPlanCommand { get; }
        public ICommand StartTrialCommand { get; }
        public ICommand ContactSalesCommand { get; }
        public ICommand CancelAtPeriodEndCommand { get; }
        public ICommand CancelImmediatelyCommand { get; }

        public bool IsLoading
        {
            get => _isLoading;
            private set { if (_isLoading == value) return; _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        public string Status
        {
            get => _status;
            private set { if (_status == value) return; _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set { if (_statusBrush == value) return; _statusBrush = value; OnPropertyChanged(nameof(StatusBrush)); }
        }

        public bool HasSubscription => _subscription != null;
        public string PackageName => _subscription?.PackageName ?? "-";
        public string SubscriptionStatusLabel => _subscription?.Status ?? "-";
        public Brush SubscriptionStatusBrush => GetStatusBrush(_subscription?.Status);
        public string BillingCycleLabel => _subscription?.BillingCycle ?? "-";
        public DateTime CurrentPeriodStart => _subscription?.CurrentPeriodStart ?? DateTime.MinValue;
        public DateTime CurrentPeriodEnd => _subscription?.CurrentPeriodEnd ?? DateTime.MinValue;
        public bool HasTrial => _subscription?.TrialEndsAt != null;
        public DateTime TrialEndsAt => _subscription?.TrialEndsAt ?? DateTime.MinValue;
        public bool CancelAtPeriodEnd => _subscription?.CancelAtPeriodEnd ?? false;
        public bool CanCancel => _subscription != null && _subscription.PackageCode != PackageCode.Free;
        public bool ShowCancelAtPeriodEndButton => CanCancel && !CancelAtPeriodEnd;
        public bool CanStartTrial => _subscription?.PackageCode == PackageCode.Free;

        public string SelectedBillingCycle => _selectedBillingCycle;
        public bool IsMonthlySelected => _selectedBillingCycle == "Monthly";
        public bool IsYearlySelected => _selectedBillingCycle == "Yearly";

        public bool HasCheckoutInstructions => !string.IsNullOrWhiteSpace(_checkoutInstructions);
        public string? CheckoutInstructions => _checkoutInstructions;
        public string CheckoutPaymentLabel => _checkoutPaymentId is int paymentId ? $"Payment reference: #{paymentId}" : string.Empty;

        public async Task LoadAsync()
        {
            IsLoading = true;
            SetStatus("Loading billing and subscription details...", true);
            try
            {
                var subscriptionTask = _crudApi.GetAsync<TenantSubscriptionDto>("api/subscription/current");
                var packagesTask = _crudApi.GetAllAsync<PricingPackageDto>("api/pricing/packages");
                var paymentsTask = _crudApi.GetAllAsync<PaymentDto>("api/payments/history");
                var invoicesTask = _crudApi.GetAllAsync<InvoiceDto>("api/invoices");

                await Task.WhenAll(subscriptionTask, packagesTask, paymentsTask, invoicesTask);

                _subscription = subscriptionTask.Result;
                if (!string.IsNullOrWhiteSpace(_subscription.BillingCycle))
                {
                    _selectedBillingCycle = _subscription.BillingCycle;
                }

                Replace(Packages, packagesTask.Result.OrderBy(p => p.SortOrder).Select(p => new BillingPackageViewModel(p)));
                Replace(Payments, paymentsTask.Result.OrderByDescending(p => p.CreatedAt));
                Replace(Invoices, invoicesTask.Result.OrderByDescending(i => i.IssuedAt));

                BuildUsageRows();
                BuildFeatureRows();
                RefreshPackagePresentation();
                RaiseSubscriptionPropertiesChanged();

                SetStatus("Billing information loaded.", true);
            }
            catch (Exception ex)
            {
                SetStatus($"Loading billing information failed: {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void BuildUsageRows()
        {
            UsageRows.Clear();
            if (_subscription == null) return;

            foreach (var limit in _subscription.Limits)
            {
                UsageRows.Add(new UsageRowViewModel
                {
                    Label = LimitLabels.TryGetValue(limit.LimitCode, out var label) ? label : limit.LimitCode,
                    ValueLabel = FormatLimit(limit.LimitValue)
                });
            }

            foreach (var usage in _subscription.Usage)
            {
                var label = LimitLabels.TryGetValue(usage.CounterCode, out var usageLabel) ? usageLabel : usage.CounterCode;
                UsageRows.Add(new UsageRowViewModel
                {
                    Label = $"{label} (used)",
                    ValueLabel = $"{usage.CurrentValue:N0} / {FormatLimit(usage.LimitValue)}"
                });
            }
        }

        private void BuildFeatureRows()
        {
            FeatureRows.Clear();
            if (_subscription == null) return;

            foreach (var feature in _subscription.Features)
            {
                FeatureRows.Add(new FeatureRowViewModel
                {
                    Label = FeatureLabels.TryGetValue(feature.FeatureCode, out var label) ? label : feature.FeatureCode,
                    ValueLabel = feature.IsEnabled ? "Included" : "Locked"
                });
            }
        }

        private void RefreshPackagePresentation()
        {
            var currentPackage = Packages.FirstOrDefault(p => p.Code == _subscription?.PackageCode);
            foreach (var package in Packages)
            {
                package.PriceLabel = ComputePriceLabel(package);
                package.ActionKind = ComputeActionKind(package, currentPackage);
                package.ActionLabel = ComputeActionLabel(package.ActionKind);
                package.IsCurrent = _subscription != null && package.Code == _subscription.PackageCode;
                package.ShowTrialButton = package.ActionKind == "upgrade" && CanStartTrial;
            }
        }

        private string ComputePriceLabel(BillingPackageViewModel package)
        {
            if (package.IsCustomPricing) return "Custom pricing";

            var price = _selectedBillingCycle == "Yearly" ? package.Package.YearlyPrice : package.Package.MonthlyPrice;
            var periodLabel = _selectedBillingCycle == "Yearly" ? "year" : "month";
            return $"{price:N2} {package.Package.Currency} / {periodLabel}";
        }

        private string ComputeActionKind(BillingPackageViewModel package, BillingPackageViewModel? currentPackage)
        {
            if (_subscription == null) return "current";
            if (package.Code == _subscription.PackageCode) return "current";
            if (package.IsCustomPricing) return "contact";
            if (package.Code == PackageCode.Free) return "downgrade";
            if (currentPackage != null && package.SortOrder > currentPackage.SortOrder) return "upgrade";
            return "downgrade";
        }

        private static string ComputeActionLabel(string actionKind) => actionKind switch
        {
            "contact" => "Contact sales",
            "upgrade" => "Upgrade",
            "downgrade" => "Switch plan",
            _ => "Current plan"
        };

        private void SetBillingCycle(string cycle)
        {
            if (_selectedBillingCycle == cycle) return;
            _selectedBillingCycle = cycle;
            OnPropertyChanged(nameof(SelectedBillingCycle));
            OnPropertyChanged(nameof(IsMonthlySelected));
            OnPropertyChanged(nameof(IsYearlySelected));
            RefreshPackagePresentation();
        }

        private async Task HandleUpgradeAsync(BillingPackageViewModel? package)
        {
            if (package == null) return;
            await RunPackageActionAsync(package, async () =>
            {
                var response = await _crudApi.PostAsync<CheckoutResponseDto>("api/subscription/checkout", new CheckoutRequest
                {
                    PackageCode = package.Code,
                    BillingCycle = _selectedBillingCycle
                });

                if (response.Status == "redirect" && !string.IsNullOrWhiteSpace(response.RedirectUrl))
                {
                    OpenLink(response.RedirectUrl);
                    SetStatus("Complete your payment in the browser, then refresh this screen.", true);
                }
                else if (response.Status == "manual")
                {
                    _checkoutInstructions = response.Instructions
                        ?? "Please complete the payment as instructed and an administrator will activate your plan once it is confirmed.";
                    _checkoutPaymentId = response.PaymentId;
                    OnPropertyChanged(nameof(HasCheckoutInstructions));
                    OnPropertyChanged(nameof(CheckoutInstructions));
                    OnPropertyChanged(nameof(CheckoutPaymentLabel));
                    SetStatus("Follow the manual payment instructions below.", true);
                }
                else
                {
                    SetStatus("Your plan has been upgraded.", true);
                }

                await LoadAsync();
            });
        }

        private async Task HandleSwitchPlanAsync(BillingPackageViewModel? package)
        {
            if (package == null) return;
            await RunPackageActionAsync(package, async () =>
            {
                await _crudApi.PostAsync("api/subscription/change-plan", new ChangePlanRequest
                {
                    PackageCode = package.Code,
                    BillingCycle = _selectedBillingCycle
                });
                SetStatus("Your plan has been updated.", true);
                await LoadAsync();
            });
        }

        private async Task HandleStartTrialAsync(BillingPackageViewModel? package)
        {
            if (package == null) return;
            await RunPackageActionAsync(package, async () =>
            {
                await _crudApi.PostAsync("api/subscription/start-trial", new StartTrialRequest
                {
                    PackageCode = package.Code,
                    TrialDays = 14
                });
                SetStatus("Your 14-day trial has started.", true);
                await LoadAsync();
            });
        }

        private async Task HandleCancelAsync(bool immediate)
        {
            var title = immediate ? "Cancel & downgrade now?" : "Cancel at period end?";
            var message = immediate
                ? "Cancel now and downgrade to the Free plan immediately?"
                : "Cancel at the end of the current billing period?";

            if (!_notificationService.Confirm(message, title))
            {
                return;
            }

            IsLoading = true;
            SetStatus("Working...", true);
            try
            {
                await _crudApi.PostAsync("api/subscription/cancel", new CancelSubscriptionRequest { Immediate = immediate });
                SetStatus("Your cancellation has been saved.", true);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
                AppNotificationCenter.Instance.Publish($"✗ {ex.Message}", false);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task RunPackageActionAsync(BillingPackageViewModel package, Func<Task> action)
        {
            if (package.IsBusy) return;
            package.IsBusy = true;
            _checkoutInstructions = null;
            _checkoutPaymentId = null;
            OnPropertyChanged(nameof(HasCheckoutInstructions));
            SetStatus("Working...", true);
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                SetStatus(ex.Message, false);
                AppNotificationCenter.Instance.Publish($"✗ {ex.Message}", false);
            }
            finally
            {
                package.IsBusy = false;
            }
        }

        private static void OpenLink(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // Best-effort: ignore failures to launch the system browser/mail client.
            }
        }

        private static string FormatLimit(int value) => value == LimitCode.Unlimited ? "Unlimited" : value.ToString("N0");

        private static Brush GetStatusBrush(string? status) => status switch
        {
            "Active" => Brushes.LightGreen,
            "Trialing" => Brushes.DeepSkyBlue,
            "PastDue" => Brushes.Orange,
            "Cancelled" => Brushes.Gray,
            "Expired" => Brushes.OrangeRed,
            _ => Brushes.LightGray
        };

        private void RaiseSubscriptionPropertiesChanged()
        {
            OnPropertyChanged(nameof(HasSubscription));
            OnPropertyChanged(nameof(PackageName));
            OnPropertyChanged(nameof(SubscriptionStatusLabel));
            OnPropertyChanged(nameof(SubscriptionStatusBrush));
            OnPropertyChanged(nameof(BillingCycleLabel));
            OnPropertyChanged(nameof(CurrentPeriodStart));
            OnPropertyChanged(nameof(CurrentPeriodEnd));
            OnPropertyChanged(nameof(HasTrial));
            OnPropertyChanged(nameof(TrialEndsAt));
            OnPropertyChanged(nameof(CancelAtPeriodEnd));
            OnPropertyChanged(nameof(CanCancel));
            OnPropertyChanged(nameof(ShowCancelAtPeriodEndButton));
            OnPropertyChanged(nameof(CanStartTrial));
            OnPropertyChanged(nameof(SelectedBillingCycle));
            OnPropertyChanged(nameof(IsMonthlySelected));
            OnPropertyChanged(nameof(IsYearlySelected));
        }

        private void SetStatus(string message, bool isSuccess)
        {
            Status = message;
            StatusBrush = isSuccess ? Brushes.LightGreen : Brushes.OrangeRed;
        }

        private void HandleBackgroundException(Exception ex)
        {
            SetStatus(ex.Message, false);
            IsLoading = false;
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
