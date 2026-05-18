using SpareParts.Application.Management.Currencies;
using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.MasterData;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace SpareParts.Desktop.Wpf.Management;

public sealed class CurrencyRatesManagementViewModel : INotifyPropertyChanged
{
    private readonly CurrencyRateProjectionService _projectionService;
    private string _baseCurrencyCode = "USD";
    private string _counterCurrencyCode = "USD";
    private decimal _defaultCounterRate = 1m;
    private bool _canViewCurrencyTab;

    public CurrencyRatesManagementViewModel(
        CurrencyRateProjectionService projectionService,
        Func<Task> refreshAsync,
        ICommand? importTableCommand = null)
    {
        _projectionService = projectionService;
        RefreshCommand = new RelayCommand(_ => _ = refreshAsync());
        ImportFromExcelCommand = new RelayCommand(_ => importTableCommand?.Execute("dbo.CurrencyRates"));
    }

    public ObservableCollection<CurrencyRateDto> CurrencyRates { get; } = new();
    public ObservableCollection<CurrencyRateDisplayRow> CurrencyRateRows { get; } = new();
    public ObservableCollection<string> CurrencyCodes { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand ImportFromExcelCommand { get; }
    public bool CanViewCurrencyTab
    {
        get => _canViewCurrencyTab;
        set
        {
            if (_canViewCurrencyTab == value)
            {
                return;
            }

            _canViewCurrencyTab = value;
            OnPropertyChanged(nameof(CanViewCurrencyTab));
        }
    }

    public string BaseCurrencyCode => _baseCurrencyCode;
    public string CounterCurrencyCode => _counterCurrencyCode;
    public decimal DefaultCounterRate => _defaultCounterRate;
    public string CurrencyRatesSummary => $"Base {BaseCurrencyCode} | Counter {CounterCurrencyCode}";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Load(
        IEnumerable<CurrencyRateDto> rates,
        string baseCurrencyCode,
        string counterCurrencyCode,
        decimal defaultCounterRate)
    {
        _baseCurrencyCode = NormalizeCurrencyCode(baseCurrencyCode) ?? "USD";
        _counterCurrencyCode = NormalizeCurrencyCode(counterCurrencyCode) ?? _baseCurrencyCode;
        _defaultCounterRate = defaultCounterRate > 0 ? defaultCounterRate : 1m;

        Replace(CurrencyRates, rates);
        RebuildProjection();

        OnPropertyChanged(nameof(BaseCurrencyCode));
        OnPropertyChanged(nameof(CounterCurrencyCode));
        OnPropertyChanged(nameof(DefaultCounterRate));
        OnPropertyChanged(nameof(CurrencyRatesSummary));
    }

    public string? NormalizeCurrencyCode(string? currencyCode)
        => _projectionService.NormalizeCurrencyCode(currencyCode);

    public decimal ResolveRateToBaseCurrency(string? currencyCode)
        => _projectionService.ResolveRateToBaseCurrency(BuildContext(), currencyCode);

    public decimal ResolveRateToCounterCurrency(string? currencyCode)
        => _projectionService.ResolveRateToCounterCurrency(BuildContext(), currencyCode);

    public CurrencyRateContext BuildContext()
    {
        return new CurrencyRateContext
        {
            BaseCurrencyCode = _baseCurrencyCode,
            CounterCurrencyCode = _counterCurrencyCode,
            DefaultCounterRate = _defaultCounterRate,
            Rates = CurrencyRates.ToList()
        };
    }

    private void RebuildProjection()
    {
        var context = BuildContext();

        Replace(CurrencyCodes, _projectionService.BuildCurrencyCodes(context));
        Replace(
            CurrencyRateRows,
            _projectionService.BuildProjections(context)
                .Select(row => new CurrencyRateDisplayRow
                {
                    Code = row.Code,
                    BaseRate = row.BaseRate,
                    CounterRate = row.CounterRate,
                    SnapshotUtc = row.SnapshotUtc
                }));

        OnPropertyChanged(nameof(CurrencyRatesSummary));
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
