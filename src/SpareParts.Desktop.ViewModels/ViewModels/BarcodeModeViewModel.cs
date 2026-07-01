using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Scanning;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SpareParts.Desktop.Wpf.ViewModels;

public sealed class BarcodeModeViewModel : INotifyPropertyChanged
{
    private readonly IPartsApiClient _partsApi;
    private readonly ISalesApiClient _salesApi;
    private readonly ICrudApiClient _crudApi;
    private readonly IWarehouseApiClient _warehouseApi;
    private readonly List<PartDto> _allParts = new();

    private bool _isLoading;
    private string _statusMessage = "Scan or select a part to start.";
    private string _partFilterText = string.Empty;
    private string _scanText = string.Empty;
    private string _visualHint = string.Empty;
    private string _visualSearchSummary = "Choose a part photo to search the catalog.";
    private PartDto? _selectedPart;
    private ScanLookupResultDto? _selectedScanResult;
    private VisualPartMatchDto? _selectedVisualMatch;
    private BarcodeLabelItem? _previewLabel;
    private int _actionQuantity = 1;
    private int? _sellWarehouseId;
    private int? _fromWarehouseId;
    private int? _toWarehouseId;
    private int? _selectedUsedCarId;

    public BarcodeModeViewModel(
        IPartsApiClient partsApi,
        ISalesApiClient salesApi,
        ICrudApiClient crudApi,
        IWarehouseApiClient warehouseApi)
    {
        _partsApi = partsApi;
        _salesApi = salesApi;
        _crudApi = crudApi;
        _warehouseApi = warehouseApi;

        LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
        ResolveScanCommand = new RelayCommand(_ => ResolveScanAsync().SafeFireAndForget(HandleBackgroundException));
        SelectScanResultCommand = new RelayCommand(result => SelectScanResult(result as ScanLookupResultDto));
        SearchByPictureCommand = new RelayCommand(_ => SearchByPictureAsync().SafeFireAndForget(HandleBackgroundException));
        SelectVisualMatchCommand = new RelayCommand(result => SelectVisualMatch(result as VisualPartMatchDto));
        GenerateSelectedLabelCommand = new RelayCommand(_ => GenerateSelectedLabel());
        GenerateVisibleLabelsCommand = new RelayCommand(_ => GenerateVisibleLabels());
        ClearLabelsCommand = new RelayCommand(_ => ClearLabels());
        PrintLabelsCommand = new RelayCommand(_ => PrintLabels());
        CheckStockCommand = new RelayCommand(_ => LoadSelectedPartStockAsync().SafeFireAndForget(HandleBackgroundException));
        SellPartCommand = new RelayCommand(_ => SellSelectedPartAsync().SafeFireAndForget(HandleBackgroundException));
        TransferPartCommand = new RelayCommand(_ => TransferSelectedPartAsync().SafeFireAndForget(HandleBackgroundException));
        AttachUsedCarCommand = new RelayCommand(_ => AttachSelectedPartToUsedCarAsync().SafeFireAndForget(HandleBackgroundException));
        DetachUsedCarCommand = new RelayCommand(_ => DetachSelectedPartFromUsedCarAsync().SafeFireAndForget(HandleBackgroundException));
    }

    public ObservableCollection<PartDto> Parts { get; } = new();
    public ObservableCollection<ScanLookupResultDto> ScanResults { get; } = new();
    public ObservableCollection<VisualPartMatchDto> VisualMatches { get; } = new();
    public ObservableCollection<BarcodeStockRow> StockRows { get; } = new();
    public ObservableCollection<WarehouseDto> Warehouses { get; } = new();
    public ObservableCollection<UsedCarDto> UsedCars { get; } = new();
    public ObservableCollection<BarcodeLabelItem> Labels { get; } = new();

    public ICommand LoadCommand { get; }
    public ICommand ResolveScanCommand { get; }
    public ICommand SelectScanResultCommand { get; }
    public ICommand SearchByPictureCommand { get; }
    public ICommand SelectVisualMatchCommand { get; }
    public ICommand GenerateSelectedLabelCommand { get; }
    public ICommand GenerateVisibleLabelsCommand { get; }
    public ICommand ClearLabelsCommand { get; }
    public ICommand PrintLabelsCommand { get; }
    public ICommand CheckStockCommand { get; }
    public ICommand SellPartCommand { get; }
    public ICommand TransferPartCommand { get; }
    public ICommand AttachUsedCarCommand { get; }
    public ICommand DetachUsedCarCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string PartFilterText
    {
        get => _partFilterText;
        set
        {
            if (!SetProperty(ref _partFilterText, value))
            {
                return;
            }

            ApplyPartFilter();
        }
    }

    public string ScanText
    {
        get => _scanText;
        set => SetProperty(ref _scanText, value);
    }

    public string VisualHint
    {
        get => _visualHint;
        set => SetProperty(ref _visualHint, value);
    }

    public string VisualSearchSummary
    {
        get => _visualSearchSummary;
        private set => SetProperty(ref _visualSearchSummary, value);
    }

    public PartDto? SelectedPart
    {
        get => _selectedPart;
        set
        {
            if (!SetProperty(ref _selectedPart, value))
            {
                return;
            }

            PreviewLabel = value == null ? null : BuildLabel(value);
            OnPropertyChanged(nameof(SelectedPartSummary));
            if (value != null)
            {
                LoadSelectedPartStockAsync().SafeFireAndForget(HandleBackgroundException);
            }
        }
    }

    public ScanLookupResultDto? SelectedScanResult
    {
        get => _selectedScanResult;
        set
        {
            if (!SetProperty(ref _selectedScanResult, value))
            {
                return;
            }

            if (value != null)
            {
                SelectScanResult(value);
            }
        }
    }

    public VisualPartMatchDto? SelectedVisualMatch
    {
        get => _selectedVisualMatch;
        set
        {
            if (!SetProperty(ref _selectedVisualMatch, value))
            {
                return;
            }

            if (value != null)
            {
                SelectVisualMatch(value);
            }
        }
    }

    public BarcodeLabelItem? PreviewLabel
    {
        get => _previewLabel;
        private set => SetProperty(ref _previewLabel, value);
    }

    public int ActionQuantity
    {
        get => _actionQuantity;
        set => SetProperty(ref _actionQuantity, value < 1 ? 1 : value);
    }

    public int? SellWarehouseId
    {
        get => _sellWarehouseId;
        set => SetProperty(ref _sellWarehouseId, value);
    }

    public int? FromWarehouseId
    {
        get => _fromWarehouseId;
        set => SetProperty(ref _fromWarehouseId, value);
    }

    public int? ToWarehouseId
    {
        get => _toWarehouseId;
        set => SetProperty(ref _toWarehouseId, value);
    }

    public int? SelectedUsedCarId
    {
        get => _selectedUsedCarId;
        set => SetProperty(ref _selectedUsedCarId, value);
    }

    public string SelectedPartSummary
        => SelectedPart == null
            ? "No part selected."
            : $"{SelectedPart.InternalCode} - {SelectedPart.Name} | Available {SelectedPart.AvailableQuantity:N0}";

    public async Task LoadAsync()
    {
        IsLoading = true;
        SetStatus("Loading AR search workspace...", true);

        try
        {
            var partsTask = _partsApi.GetPartsAsync();
            var warehousesTask = _warehouseApi.GetWarehousesAsync();
            var usedCarsTask = _crudApi.GetAllAsync<UsedCarDto>("api/usedcars");

            await Task.WhenAll(partsTask, warehousesTask, usedCarsTask);

            _allParts.Clear();
            _allParts.AddRange(partsTask.Result.OrderBy(part => part.InternalCode));
            ApplyPartFilter();

            Replace(Warehouses, warehousesTask.Result.OrderByDescending(item => item.IsMain).ThenBy(item => item.Name));
            Replace(UsedCars, usedCarsTask.Result.OrderByDescending(item => item.Id));

            var defaultWarehouse = Warehouses.FirstOrDefault(item => item.IsMain) ?? Warehouses.FirstOrDefault();
            SellWarehouseId ??= defaultWarehouse?.Id;
            FromWarehouseId ??= defaultWarehouse?.Id;
            ToWarehouseId ??= Warehouses.FirstOrDefault(item => item.Id != FromWarehouseId)?.Id ?? defaultWarehouse?.Id;

            SetStatus($"AR search workspace loaded with {Parts.Count:N0} part(s).", true);
        }
        catch (ApiClientException ex)
        {
            SetStatus($"API error ({ex.Code}): {ex.Message}", false);
        }
        catch (Exception)
        {
            SetStatus("Unexpected error while loading barcode workspace.", false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ResolveScanAsync()
    {
        if (string.IsNullOrWhiteSpace(ScanText))
        {
            SetStatus("Scan or enter a barcode first.", false);
            return;
        }

        IsLoading = true;
        try
        {
            ScanResults.Clear();
            var results = await _partsApi.ResolveScanAsync(ScanText.Trim());
            foreach (var result in results)
            {
                ScanResults.Add(result);
            }

            if (ScanResults.Count == 0)
            {
                SetStatus("No record matched that scan.", false);
                return;
            }

            SetStatus($"Scan resolved to {ScanResults.Count:N0} record(s).", true);
            if (ScanResults.Count == 1)
            {
                SelectedScanResult = ScanResults[0];
            }
        }
        catch (ApiClientException ex)
        {
            SetStatus($"Scan failed ({ex.Code}): {ex.Message}", false);
        }
        catch (Exception)
        {
            SetStatus("Unexpected error while resolving the scan.", false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SearchByPictureAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Search parts by picture",
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.webp;*.heic;*.heif)|*.jpg;*.jpeg;*.png;*.webp;*.heic;*.heif|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsLoading = true;
        SetStatus("Searching catalog from picture...", true);
        VisualSearchSummary = "Reading picture and ranking catalog matches...";

        try
        {
            var response = await _partsApi.SearchPartsByImageAsync(dialog.FileName, VisualHint, 10);
            Replace(VisualMatches, response.Matches);
            VisualSearchSummary = string.IsNullOrWhiteSpace(response.SearchText)
                ? response.Message
                : $"{response.SearchText} | {response.Message}";

            if (VisualMatches.Count == 0)
            {
                SetStatus("No catalog parts matched that picture.", false);
                return;
            }

            SelectedVisualMatch = VisualMatches[0];
            SetStatus($"Picture search found {VisualMatches.Count:N0} match(es).", true);
        }
        catch (ApiClientException ex)
        {
            VisualMatches.Clear();
            VisualSearchSummary = ex.Message;
            SetStatus($"Picture search failed ({ex.Code}): {ex.Message}", false);
        }
        catch (Exception ex)
        {
            VisualMatches.Clear();
            VisualSearchSummary = ex.Message;
            SetStatus("Unexpected error while searching by picture.", false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectScanResult(ScanLookupResultDto? result)
    {
        if (result == null)
        {
            return;
        }

        switch (result.TargetType)
        {
            case ScanTargetTypes.Part:
                SelectPartById(result.TargetId);
                SetStatus($"Part opened from scan: {result.DisplayText}.", true);
                break;
            case ScanTargetTypes.Warehouse:
                SellWarehouseId = result.TargetId;
                FromWarehouseId = result.TargetId;
                SetStatus($"Warehouse selected from scan: {result.DisplayText}.", true);
                break;
            case ScanTargetTypes.UsedCar:
                SelectedUsedCarId = result.TargetId;
                SetStatus($"Used car selected from scan: {result.DisplayText}.", true);
                break;
            default:
                SetStatus($"Scan opened {result.DisplayText}.", true);
                break;
        }
    }

    private void SelectVisualMatch(VisualPartMatchDto? match)
    {
        if (match == null)
        {
            return;
        }

        var scanRow = new ScanLookupResultDto
        {
            TargetType = ScanTargetTypes.Part,
            TargetId = match.PartId,
            Code = string.IsNullOrWhiteSpace(match.Barcode) ? match.InternalCode : match.Barcode,
            DisplayText = $"{match.InternalCode} - {match.PartName}",
            SecondaryText = match.MatchReason,
            ApiRoute = $"api/parts?visual={match.PartId}"
        };

        ScanResults.Clear();
        ScanResults.Add(scanRow);
        SelectScanResult(scanRow);
    }

    private void SelectPartById(int partId)
    {
        var part = _allParts.FirstOrDefault(item => item.Id == partId);
        if (part == null)
        {
            SetStatus("The scanned part is not in the loaded part list. Refresh and try again.", false);
            return;
        }

        if (!Parts.Contains(part))
        {
            PartFilterText = string.Empty;
        }

        SelectedPart = part;
    }

    private void GenerateSelectedLabel()
    {
        if (SelectedPart == null)
        {
            SetStatus("Select a part before generating a label.", false);
            return;
        }

        AddLabel(SelectedPart);
        SetStatus($"Label queued for {SelectedPart.InternalCode}.", true);
    }

    private void GenerateVisibleLabels()
    {
        foreach (var part in Parts)
        {
            AddLabel(part);
        }

        SetStatus($"Label sheet now has {Labels.Count:N0} label(s).", true);
    }

    private void AddLabel(PartDto part)
    {
        if (Labels.Any(label => label.PartId == part.Id))
        {
            return;
        }

        Labels.Add(BuildLabel(part));
    }

    private void ClearLabels()
    {
        Labels.Clear();
        SetStatus("Label sheet cleared.", true);
    }

    private async Task LoadSelectedPartStockAsync()
    {
        if (SelectedPart == null)
        {
            SetStatus("Select a part before checking stock.", false);
            return;
        }

        IsLoading = true;
        try
        {
            var stock = await _partsApi.GetPartStockAsync(SelectedPart.Id);
            Replace(StockRows, stock.Select(row => new BarcodeStockRow(row)));
            SetStatus($"Loaded stock for {SelectedPart.InternalCode}.", true);
        }
        catch (ApiClientException ex)
        {
            SetStatus($"Stock lookup failed ({ex.Code}): {ex.Message}", false);
        }
        catch (Exception)
        {
            SetStatus("Unexpected error while checking stock.", false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SellSelectedPartAsync()
    {
        if (!TryGetActionPart(out var part))
        {
            return;
        }

        if (SellWarehouseId is not > 0)
        {
            SetStatus("Select the sale warehouse first.", false);
            return;
        }

        if (part.SalePrice <= 0)
        {
            SetStatus("The selected part does not have a positive sale price.", false);
            return;
        }

        IsLoading = true;
        try
        {
            var response = await _salesApi.CreateSaleAsync(new CreateSaleRequest
            {
                InvoiceDate = DateTime.Today,
                WarehouseId = SellWarehouseId.Value,
                PaymentMethod = "Barcode scan",
                PaidAmount = 0,
                Notes = $"Created from AR Search for {part.InternalCode}.",
                Items = new List<SaleItemDto>
                {
                    new()
                    {
                        PartId = part.Id,
                        Quantity = ActionQuantity,
                        UnitPrice = part.SalePrice,
                        DiscountAmount = 0,
                        TaxRate = 0
                    }
                }
            });

            SetStatus($"Sale invoice {response.InvoiceNumber} created for {part.InternalCode}.", true);
            await RefreshAfterPartMutationAsync(part.Id);
        }
        catch (ApiClientException ex)
        {
            SetStatus($"Sale failed ({ex.Code}): {ex.Message}", false);
        }
        catch (Exception)
        {
            SetStatus("Unexpected error while selling the scanned part.", false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task TransferSelectedPartAsync()
    {
        if (!TryGetActionPart(out var part))
        {
            return;
        }

        if (FromWarehouseId is not > 0 || ToWarehouseId is not > 0)
        {
            SetStatus("Select both source and destination warehouses.", false);
            return;
        }

        if (FromWarehouseId == ToWarehouseId)
        {
            SetStatus("Source and destination warehouses must be different.", false);
            return;
        }

        IsLoading = true;
        try
        {
            await _partsApi.TransferPartAsync(part.Id, new TransferPartRequest
            {
                FromWarehouseId = FromWarehouseId.Value,
                ToWarehouseId = ToWarehouseId.Value,
                Quantity = ActionQuantity
            });

            SetStatus($"Moved {ActionQuantity:N0} x {part.InternalCode}.", true);
            await RefreshAfterPartMutationAsync(part.Id);
        }
        catch (ApiClientException ex)
        {
            SetStatus($"Transfer failed ({ex.Code}): {ex.Message}", false);
        }
        catch (Exception)
        {
            SetStatus("Unexpected error while moving warehouse stock.", false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AttachSelectedPartToUsedCarAsync()
    {
        if (!TryGetActionPart(out var part))
        {
            return;
        }

        if (SelectedUsedCarId is not > 0)
        {
            SetStatus("Select or scan a used car first.", false);
            return;
        }

        await UpdatePartUsedCarAsync(part, SelectedUsedCarId.Value, "attached to");
    }

    private async Task DetachSelectedPartFromUsedCarAsync()
    {
        if (!TryGetActionPart(out var part))
        {
            return;
        }

        await UpdatePartUsedCarAsync(part, null, "detached from");
    }

    private async Task UpdatePartUsedCarAsync(PartDto part, int? usedCarId, string action)
    {
        IsLoading = true;
        try
        {
            await _partsApi.UpdateUsedCarAsync(part.Id, new UpdatePartUsedCarRequest { UsedCarId = usedCarId });
            part.UsedCarId = usedCarId;
            OnPropertyChanged(nameof(SelectedPartSummary));
            SetStatus($"{part.InternalCode} {action} used car.", true);
        }
        catch (ApiClientException ex)
        {
            SetStatus($"Used-car link failed ({ex.Code}): {ex.Message}", false);
        }
        catch (Exception)
        {
            SetStatus("Unexpected error while updating the used-car link.", false);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAfterPartMutationAsync(int partId)
    {
        var previousFilter = PartFilterText;
        var parts = await _partsApi.GetPartsAsync();
        _allParts.Clear();
        _allParts.AddRange(parts.OrderBy(part => part.InternalCode));
        PartFilterText = previousFilter;
        ApplyPartFilter();
        SelectPartById(partId);
    }

    private bool TryGetActionPart(out PartDto part)
    {
        part = SelectedPart!;
        if (SelectedPart == null)
        {
            SetStatus("Select or scan a part first.", false);
            return false;
        }

        if (ActionQuantity <= 0)
        {
            SetStatus("Quantity must be greater than zero.", false);
            return false;
        }

        part = SelectedPart;
        return true;
    }

    private void ApplyPartFilter()
    {
        var filter = PartFilterText.Trim();
        var matches = string.IsNullOrWhiteSpace(filter)
            ? _allParts
            : _allParts.Where(part =>
                Contains(part.InternalCode, filter) ||
                Contains(part.Name, filter) ||
                Contains(part.Barcode, filter) ||
                Contains(part.OEMNumber, filter));

        Replace(Parts, matches.Take(250));
    }

    private void PrintLabels()
    {
        if (Labels.Count == 0)
        {
            SetStatus("Generate at least one label before printing.", false);
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var document = BuildPrintableLabelDocument(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight);
        dialog.PrintDocument(((IDocumentPaginatorSource)document).DocumentPaginator, "Barcode / QR labels");
        SetStatus("Label sheet sent to printer.", true);
    }

    private FlowDocument BuildPrintableLabelDocument(double printableWidth, double printableHeight)
    {
        var pageWidth = Math.Max(720d, printableWidth);
        var pageHeight = Math.Max(960d, printableHeight);
        var document = new FlowDocument
        {
            PageWidth = pageWidth,
            PageHeight = pageHeight,
            PagePadding = new Thickness(24),
            ColumnWidth = pageWidth - 48,
            Background = Brushes.White,
            FontFamily = new FontFamily("Segoe UI")
        };

        var table = new Table { CellSpacing = 6 };
        for (var i = 0; i < 3; i++)
        {
            table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });
        }

        var rowGroup = new TableRowGroup();
        table.RowGroups.Add(rowGroup);

        foreach (var rowLabels in Labels.Select((label, index) => new { label, index }).GroupBy(item => item.index / 3))
        {
            var row = new TableRow();
            foreach (var item in rowLabels)
            {
                row.Cells.Add(new TableCell(new BlockUIContainer(BuildPrintableLabel(item.label)))
                {
                    Padding = new Thickness(0, 0, 6, 6)
                });
            }

            while (row.Cells.Count < 3)
            {
                row.Cells.Add(new TableCell(new Paragraph(new Run(string.Empty))));
            }

            rowGroup.Rows.Add(row);
        }

        document.Blocks.Add(table);
        return document;
    }

    private static Border BuildPrintableLabel(BarcodeLabelItem label)
    {
        var root = new Border
        {
            Width = 240,
            Height = 155,
            Margin = new Thickness(6),
            Padding = new Thickness(10),
            BorderBrush = Brushes.Black,
            BorderThickness = new Thickness(1),
            Background = Brushes.White
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = label.PartCode,
            FontWeight = FontWeights.Bold,
            FontSize = 15,
            Foreground = Brushes.Black,
            TextTrimming = System.Windows.TextTrimming.CharacterEllipsis
        };
        grid.Children.Add(title);

        var middle = new Grid { Margin = new Thickness(0, 8, 0, 4) };
        middle.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        middle.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(middle, 1);

        var barsStack = new StackPanel { Orientation = Orientation.Vertical };
        var bars = new StackPanel { Orientation = Orientation.Horizontal, Height = 52 };
        foreach (var module in label.BarcodeModules)
        {
            bars.Children.Add(new Rectangle
            {
                Width = module.Width,
                Height = 50,
                Fill = module.IsBar ? Brushes.Black : Brushes.Transparent
            });
        }

        barsStack.Children.Add(bars);
        barsStack.Children.Add(new TextBlock
        {
            Text = label.BarcodeText,
            FontSize = 10,
            Foreground = Brushes.Black,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        middle.Children.Add(barsStack);

        var qrImage = new Image
        {
            Source = label.QrImage,
            Width = 58,
            Height = 58,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(qrImage, 1);
        middle.Children.Add(qrImage);
        grid.Children.Add(middle);

        var footer = new TextBlock
        {
            Text = $"{label.PartName} | {label.PriceText}",
            FontSize = 10,
            Foreground = Brushes.Black,
            TextTrimming = System.Windows.TextTrimming.CharacterEllipsis
        };
        Grid.SetRow(footer, 2);
        grid.Children.Add(footer);

        root.Child = grid;
        return root;
    }

    private BarcodeLabelItem BuildLabel(PartDto part)
    {
        var barcodeText = string.IsNullOrWhiteSpace(part.Barcode)
            ? $"PART:{part.Id}"
            : part.Barcode.Trim();
        var qrPayload = $"spareparts://part/{part.Id}";

        return new BarcodeLabelItem
        {
            PartId = part.Id,
            PartCode = part.InternalCode,
            PartName = part.Name,
            BarcodeText = barcodeText,
            QrPayload = qrPayload,
            PriceText = $"{part.SalePrice:N2} {part.Currency}",
            StockText = $"Avail {part.AvailableQuantity:N0}",
            QrImage = QrImageFactory.Create(qrPayload),
            BarcodeModules = new ObservableCollection<BarcodeModule>(Code128BarcodeEncoder.Encode(barcodeText))
        };
    }

    private void SetStatus(string message, bool isSuccess)
    {
        StatusMessage = message;
        AppNotificationCenter.Instance.Publish((isSuccess ? "OK " : "ERR ") + message, isSuccess);
    }

    private void HandleBackgroundException(Exception ex)
        => SetStatus(ex.Message, false);

    private static bool Contains(string? value, string filter)
        => !string.IsNullOrWhiteSpace(value)
           && value.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
