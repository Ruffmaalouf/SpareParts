using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Inventory;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management;

public sealed class PartRequestsManagementViewModel : ManagementFeatureViewModelBase
{
    private readonly IManagementFeatureContext _ctx;
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
    private bool _reserveOnCreate;
    private DateTime _reservationExpiresAt = DefaultReservationExpiresAt();
    private string _reservationExpirationAction = PartReservationExpirationAction.AutoRelease;

    public ObservableCollection<PartRequestDto> Requests { get; } = new BulkObservableCollection<PartRequestDto>();
    public ObservableCollection<CustomerDto> Customers { get; } = new BulkObservableCollection<CustomerDto>();
    public ObservableCollection<PartDto> Parts { get; } = new BulkObservableCollection<PartDto>();
    public IReadOnlyList<string> ReservationExpirationActions { get; } =
    [
        PartReservationExpirationAction.AutoRelease,
        PartReservationExpirationAction.StaffReminder
    ];

    public ICommand SaveCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand StartNewCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ReserveAutoReleaseCommand { get; }
    public ICommand ReserveReminderCommand { get; }
    public ICommand ReleaseReservationCommand { get; }
    public ICommand MarkContactedCommand { get; }
    public ICommand MarkFulfilledCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ReopenCommand { get; }

    public int OpenRequestCount => Requests.Count(request => PartRequestStatus.IsActive(request.Status));
    public int ReadyRequestCount => Requests.Count(request => request.IsReadyToContact);
    public int ReservedRequestCount => Requests.Count(request => request.IsReserved);
    public int ReminderDueCount => Requests.Count(request => request.IsReservationReminderDue);
    public int ReadyCustomerCount => Requests
        .Where(request => request.IsReadyToContact)
        .GroupBy(request => request.PartId ?? -request.Id)
        .Sum(group => Math.Max(1, group.Max(request => request.WaitingCustomerCount)));
    public string BoardSummary => ReadyRequestCount == 0
        ? $"{OpenRequestCount} active request(s), {ReservedRequestCount} reserved."
        : $"{ReadyCustomerCount} customer(s) were waiting for parts now in stock; {ReservedRequestCount} reserved.";
    public string SelectedReservationSummary => SelectedRequest is not { IsReserved: true }
        ? "No active reservation selected."
        : SelectedRequest.ReservationExpiresAt is DateTime expiresAt
            ? $"{SelectedRequest.ReservedQuantity:N0} reserved until {expiresAt.ToLocalTime():yyyy-MM-dd HH:mm}."
            : $"{SelectedRequest.ReservedQuantity:N0} reserved.";

    public PartRequestDto? SelectedRequest
    {
        get => _selectedRequest;
        set
        {
            if (!SetProperty(ref _selectedRequest, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SelectedReservationSummary));
        }
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

    public bool ReserveOnCreate
    {
        get => _reserveOnCreate;
        set => SetProperty(ref _reserveOnCreate, value);
    }

    public DateTime ReservationExpiresAt
    {
        get => _reservationExpiresAt;
        set => SetProperty(ref _reservationExpiresAt, value);
    }

    public string ReservationExpirationAction
    {
        get => _reservationExpirationAction;
        set => SetProperty(ref _reservationExpirationAction, PartReservationExpirationAction.Normalize(value));
    }

    public PartRequestsManagementViewModel(IManagementFeatureContext context)
    {
        _ctx = context;
        SaveCommand = new RelayCommand(_ => _ = SaveAsync());
        DeleteCommand = new RelayCommand(_ => _ = DeleteAsync());
        StartNewCommand = new RelayCommand(_ => StartNew());
        RefreshCommand = new RelayCommand(_ => _ = _ctx.RefreshAsync());
        ReserveAutoReleaseCommand = new RelayCommand(_ => _ = ReserveSelectedAsync(PartReservationExpirationAction.AutoRelease));
        ReserveReminderCommand = new RelayCommand(_ => _ = ReserveSelectedAsync(PartReservationExpirationAction.StaffReminder));
        ReleaseReservationCommand = new RelayCommand(_ => _ = ReleaseSelectedReservationAsync());
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
        ReserveOnCreate = false;
        ReservationExpiresAt = DefaultReservationExpiresAt();
        ReservationExpirationAction = PartReservationExpirationAction.AutoRelease;
        SelectedRequest = null;
    }

    private async Task SaveAsync()
    {
        var result = await _ctx.Coordinator.SavePartRequestAsync(this);
        _ctx.SetStatus(result.Message, result.Success);
        if (!result.Success) return;
        await _ctx.RefreshAsync();
        StartNew();
    }

    private async Task ReserveSelectedAsync(string expirationAction)
    {
        ReservationExpirationAction = expirationAction;
        var result = await _ctx.Coordinator.ReservePartRequestAsync(SelectedRequest, ReservationExpiresAt, ReservationExpirationAction);
        _ctx.SetStatus(result.Message, result.Success);
        if (result.Success) await _ctx.RefreshAsync();
    }

    private async Task ReleaseSelectedReservationAsync()
    {
        var result = await _ctx.Coordinator.ReleasePartRequestReservationAsync(SelectedRequest);
        _ctx.SetStatus(result.Message, result.Success);
        if (result.Success) await _ctx.RefreshAsync();
    }

    private async Task UpdateStatusAsync(string status)
    {
        var result = await _ctx.Coordinator.UpdatePartRequestStatusAsync(SelectedRequest, status);
        _ctx.SetStatus(result.Message, result.Success);
        if (result.Success) await _ctx.RefreshAsync();
    }

    private async Task DeleteAsync()
    {
        var result = await _ctx.Coordinator.DeletePartRequestAsync(SelectedRequest);
        _ctx.SetStatus(result.Message, result.Success);
        if (result.Success)
        {
            await _ctx.RefreshAsync();
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
        OnPropertyChanged(nameof(ReservedRequestCount));
        OnPropertyChanged(nameof(ReminderDueCount));
        OnPropertyChanged(nameof(ReadyCustomerCount));
        OnPropertyChanged(nameof(BoardSummary));
        OnPropertyChanged(nameof(SelectedReservationSummary));
    }

    private static DateTime DefaultReservationExpiresAt()
    {
        var tomorrow = DateTime.Now.Date.AddDays(1);
        return tomorrow.AddHours(18);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        => target.ReplaceWith(source);
}
