using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Inventory;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management;

public sealed class PartRequestsManagementViewModel : ManagementFeatureViewModelBase
{
    private ManagementCoordinator? _coordinator;
    private Func<Task>? _refreshAsync;
    private Action<string, bool>? _setStatus;
    private PartRequestDto? _selectedRequest;
    private int? _newRequestPartId;
    private int? _newRequestCustomerId;
    private string _newRequestCustomerName = string.Empty;
    private string _newRequestCustomerPhone = string.Empty;
    private string _newRequestPartName = string.Empty;
    private string _newRequestOem = string.Empty;
    private string _newRequestVehicleDetails = string.Empty;
    private int _newRequestQuantity = 1;
    private string _newRequestNotes = string.Empty;

    public ObservableCollection<PartRequestDto> Requests { get; } = new();
    public ObservableCollection<CustomerDto> Customers { get; } = new();
    public ObservableCollection<PartDto> Parts { get; } = new();

    public ICommand SaveCommand { get; private set; } = new RelayCommand(_ => { });
    public ICommand DeleteCommand { get; private set; } = new RelayCommand(_ => { });
    public ICommand StartNewCommand { get; private set; } = new RelayCommand(_ => { });
    public ICommand RefreshCommand { get; private set; } = new RelayCommand(_ => { });
    public ICommand MarkContactedCommand { get; private set; } = new RelayCommand(_ => { });
    public ICommand MarkFulfilledCommand { get; private set; } = new RelayCommand(_ => { });
    public ICommand CancelCommand { get; private set; } = new RelayCommand(_ => { });
    public ICommand ReopenCommand { get; private set; } = new RelayCommand(_ => { });

    public int OpenRequestCount => Requests.Count(request => PartRequestStatus.IsActive(request.Status));
    public int ReadyRequestCount => Requests.Count(request => request.IsReadyToContact);
    public int ReadyCustomerCount => Requests
        .Where(request => request.IsReadyToContact)
        .GroupBy(request => request.PartId ?? -request.Id)
        .Sum(group => Math.Max(1, group.Max(request => request.WaitingCustomerCount)));
    public string BoardSummary => ReadyRequestCount == 0
        ? $"{OpenRequestCount} active request(s)."
        : $"{ReadyCustomerCount} customer(s) were waiting for parts now in stock.";

    public PartRequestDto? SelectedRequest
    {
        get => _selectedRequest;
        set => SetProperty(ref _selectedRequest, value);
    }

    public int? NewRequestPartId
    {
        get => _newRequestPartId;
        set
        {
            if (!SetProperty(ref _newRequestPartId, value))
            {
                return;
            }

            ApplySelectedPart(value);
        }
    }

    public int? NewRequestCustomerId
    {
        get => _newRequestCustomerId;
        set
        {
            if (!SetProperty(ref _newRequestCustomerId, value))
            {
                return;
            }

            ApplySelectedCustomer(value);
        }
    }

    public string NewRequestCustomerName
    {
        get => _newRequestCustomerName;
        set => SetProperty(ref _newRequestCustomerName, value);
    }

    public string NewRequestCustomerPhone
    {
        get => _newRequestCustomerPhone;
        set => SetProperty(ref _newRequestCustomerPhone, value);
    }

    public string NewRequestPartName
    {
        get => _newRequestPartName;
        set => SetProperty(ref _newRequestPartName, value);
    }

    public string NewRequestOem
    {
        get => _newRequestOem;
        set => SetProperty(ref _newRequestOem, value);
    }

    public string NewRequestVehicleDetails
    {
        get => _newRequestVehicleDetails;
        set => SetProperty(ref _newRequestVehicleDetails, value);
    }

    public int NewRequestQuantity
    {
        get => _newRequestQuantity;
        set => SetProperty(ref _newRequestQuantity, Math.Max(1, value));
    }

    public string NewRequestNotes
    {
        get => _newRequestNotes;
        set => SetProperty(ref _newRequestNotes, value);
    }

    public void Configure(
        ManagementCoordinator coordinator,
        Func<Task> refreshAsync,
        Action<string, bool> setStatus)
    {
        _coordinator = coordinator;
        _refreshAsync = refreshAsync;
        _setStatus = setStatus;
        SaveCommand = new RelayCommand(_ => _ = SaveAsync());
        DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
        StartNewCommand = new RelayCommand(_ => StartNew());
        RefreshCommand = new RelayCommand(_ => _ = refreshAsync());
        MarkContactedCommand = new RelayCommand(_ => _ = UpdateStatusAsync(PartRequestStatus.Contacted));
        MarkFulfilledCommand = new RelayCommand(_ => _ = UpdateStatusAsync(PartRequestStatus.Fulfilled));
        CancelCommand = new RelayCommand(_ => _ = UpdateStatusAsync(PartRequestStatus.Cancelled));
        ReopenCommand = new RelayCommand(_ => _ = UpdateStatusAsync(PartRequestStatus.Open));
    }

    public void Load(IEnumerable<PartRequestDto> requests)
    {
        Replace(Requests, requests);
        OnRequestCountsChanged();
    }

    public void LoadReferenceData(IEnumerable<CustomerDto> customers, IEnumerable<PartDto> parts)
    {
        Replace(Customers, customers);
        Replace(Parts, parts);
    }

    public void StartNew()
    {
        NewRequestPartId = null;
        NewRequestCustomerId = null;
        NewRequestCustomerName = string.Empty;
        NewRequestCustomerPhone = string.Empty;
        NewRequestPartName = string.Empty;
        NewRequestOem = string.Empty;
        NewRequestVehicleDetails = string.Empty;
        NewRequestQuantity = 1;
        NewRequestNotes = string.Empty;
        SelectedRequest = null;
    }

    private async Task SaveAsync()
    {
        if (_coordinator == null || _refreshAsync == null || _setStatus == null)
        {
            return;
        }

        var result = await _coordinator.SavePartRequestAsync(this);
        _setStatus(result.Message, result.Success);
        if (!result.Success)
        {
            return;
        }

        await _refreshAsync();
        StartNew();
    }

    private async Task UpdateStatusAsync(string status)
    {
        if (_coordinator == null || _refreshAsync == null || _setStatus == null)
        {
            return;
        }

        var result = await _coordinator.UpdatePartRequestStatusAsync(SelectedRequest, status);
        _setStatus(result.Message, result.Success);
        if (result.Success)
        {
            await _refreshAsync();
        }
    }

    private async Task DeleteAsync()
    {
        if (_coordinator == null || _refreshAsync == null || _setStatus == null)
        {
            return;
        }

        var result = await _coordinator.DeletePartRequestAsync(SelectedRequest);
        _setStatus(result.Message, result.Success);
        if (result.Success)
        {
            await _refreshAsync();
            StartNew();
        }
    }

    private void ApplySelectedPart(int? partId)
    {
        var part = partId is int id ? Parts.FirstOrDefault(item => item.Id == id) : null;
        if (part == null)
        {
            return;
        }

        NewRequestPartName = part.Name;
        NewRequestOem = part.OEMNumber ?? string.Empty;
    }

    private void ApplySelectedCustomer(int? customerId)
    {
        var customer = customerId is int id ? Customers.FirstOrDefault(item => item.Id == id) : null;
        if (customer == null)
        {
            return;
        }

        NewRequestCustomerName = customer.Name;
        NewRequestCustomerPhone = customer.Phone ?? string.Empty;
    }

    private void OnRequestCountsChanged()
    {
        OnPropertyChanged(nameof(OpenRequestCount));
        OnPropertyChanged(nameof(ReadyRequestCount));
        OnPropertyChanged(nameof(ReadyCustomerCount));
        OnPropertyChanged(nameof(BoardSummary));
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
