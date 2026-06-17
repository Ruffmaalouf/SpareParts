using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class SellerVerificationViewModel : JsonListViewModelBase
    {
        private string _businessName = string.Empty;
        private string _registrationNumber = string.Empty;
        private string _physicalAddress = string.Empty;
        private string _idDocumentUrl = string.Empty;
        private string _businessDocumentUrl = string.Empty;

        public SellerVerificationViewModel(ICrudApiClient crudApi) : base(crudApi, "Load seller verification.")
        {
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleException));
            SubmitCommand = new RelayCommand(_ => SubmitAsync().SafeFireAndForget(HandleException));
        }

        public ICommand LoadCommand { get; }
        public ICommand SubmitCommand { get; }

        public string BusinessName
        {
            get => _businessName;
            set { _businessName = value; OnPropertyChanged(nameof(BusinessName)); }
        }

        public string RegistrationNumber
        {
            get => _registrationNumber;
            set { _registrationNumber = value; OnPropertyChanged(nameof(RegistrationNumber)); }
        }

        public string PhysicalAddress
        {
            get => _physicalAddress;
            set { _physicalAddress = value; OnPropertyChanged(nameof(PhysicalAddress)); }
        }

        public string IdDocumentUrl
        {
            get => _idDocumentUrl;
            set { _idDocumentUrl = value; OnPropertyChanged(nameof(IdDocumentUrl)); }
        }

        public string BusinessDocumentUrl
        {
            get => _businessDocumentUrl;
            set { _businessDocumentUrl = value; OnPropertyChanged(nameof(BusinessDocumentUrl)); }
        }

        public async Task LoadAsync()
        {
            IsLoading = true;
            Status = "Loading...";
            try
            {
                var items = await CrudApi.GetAllAsync<JsonElement>("/api/seller-verification");
                Table = JsonGridHelper.ToDataTable(items);
                Status = $"{items.Count} item(s) loaded.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
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

        public async Task SubmitAsync()
        {
            try
            {
                var payload = new
                {
                    businessName = BusinessName,
                    registrationNumber = RegistrationNumber,
                    physicalAddress = PhysicalAddress,
                    idDocumentUrl = IdDocumentUrl,
                    businessDocumentUrl = BusinessDocumentUrl
                };
                await CrudApi.PostAsync("/api/seller-verification/submit", payload);
                Status = "Verification submitted.";
                StatusBrush = System.Windows.Media.Brushes.LightGreen;
                BusinessName = string.Empty;
                RegistrationNumber = string.Empty;
                PhysicalAddress = string.Empty;
                IdDocumentUrl = string.Empty;
                BusinessDocumentUrl = string.Empty;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
    }
}
