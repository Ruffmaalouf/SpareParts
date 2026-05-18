using SpareParts.Desktop.Wpf.Helpers;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartCompatibilityViewModel : INotifyPropertyChanged
    {
        private const double CanvasWidth = 940;
        private const double CanvasHeight = 560;
        private readonly ICrudApiClient _crudApi;
        private readonly List<PartCompatibilityPartRow> _allParts = new();
        private readonly Dictionary<int, UsedCarDto> _carsById = new();
        private PartCompatibilityPartRow? _selectedPart;
        private string _searchText = string.Empty;
        private string _status = "Load inventory to inspect part compatibility.";
        private string _partCountSummary = "No parts loaded.";
        private Brush _statusBrush = Brushes.LightGray;
        private bool _isLoading;
        private DateTime? _lastLoadedAt;

        public PartCompatibilityViewModel(ICrudApiClient crudApi)
        {
            _crudApi = crudApi;
            LoadCommand = new RelayCommand(_ => LoadAsync().SafeFireAndForget(HandleBackgroundException));
            RefreshCommand = LoadCommand;
            SelectGraphNodeCommand = new RelayCommand(SelectGraphNode);
            SelectProofPartCommand = new RelayCommand(parameter =>
            {
                if (parameter is PartCompatibilityProofPart proof)
                {
                    SelectedPart = proof.Part;
                }
                else if (parameter is PartCompatibilityPartRow part)
                {
                    SelectedPart = part;
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<PartCompatibilityPartRow> FilteredParts { get; } = new();
        public ObservableCollection<PartCompatibilityMetricTile> MetricTiles { get; } = new();
        public ObservableCollection<PartCompatibilityGraphNode> GraphNodes { get; } = new();
        public ObservableCollection<PartCompatibilityGraphEdge> GraphEdges { get; } = new();
        public ObservableCollection<PartCompatibilityFitmentGroup> FitmentGroups { get; } = new();
        public ObservableCollection<PartCompatibilityProofPart> ProofParts { get; } = new();

        public ICommand LoadCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SelectGraphNodeCommand { get; }
        public ICommand SelectProofPartCommand { get; }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value)
                {
                    return;
                }

                _searchText = value ?? string.Empty;
                OnPropertyChanged(nameof(SearchText));
                RefreshFilteredParts();
            }
        }

        public PartCompatibilityPartRow? SelectedPart
        {
            get => _selectedPart;
            set
            {
                if (ReferenceEquals(_selectedPart, value))
                {
                    return;
                }

                _selectedPart = value;
                OnPropertyChanged(nameof(SelectedPart));
                OnPropertyChanged(nameof(SelectedPartTitle));
                OnPropertyChanged(nameof(SelectedPartSubtitle));
                OnPropertyChanged(nameof(SelectedPartVehicle));
                OnPropertyChanged(nameof(SelectedPartPrice));
                RebuildCompatibility();
            }
        }

        public string SelectedPartTitle => SelectedPart?.Name ?? "Select a part";

        public string SelectedPartSubtitle => SelectedPart == null
            ? "Search by part name, code, OEM, or vehicle."
            : $"{SelectedPart.DisplayCode} - OEM {SelectedPart.OemDisplay}";

        public string SelectedPartVehicle => SelectedPart?.VehicleLabel ?? "No donor vehicle linked";

        public string SelectedPartPrice => SelectedPart?.PriceLabel ?? string.Empty;

        public string Status
        {
            get => _status;
            private set
            {
                if (_status == value)
                {
                    return;
                }

                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        public Brush StatusBrush
        {
            get => _statusBrush;
            private set
            {
                if (_statusBrush == value)
                {
                    return;
                }

                _statusBrush = value;
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public string PartCountSummary
        {
            get => _partCountSummary;
            private set
            {
                if (_partCountSummary == value)
                {
                    return;
                }

                _partCountSummary = value;
                OnPropertyChanged(nameof(PartCountSummary));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                if (_isLoading == value)
                {
                    return;
                }

                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        public async Task LoadAsync()
        {
            if (IsLoading)
            {
                return;
            }

            IsLoading = true;
            Status = "Loading inventory and donor vehicles...";
            StatusBrush = Brushes.LightGoldenrodYellow;

            try
            {
                var partsTask = _crudApi.GetAllAsync<PartDto>("api/parts?page=1&pageSize=5000");
                var carsTask = _crudApi.GetAllAsync<UsedCarDto>("api/usedcars");

                await Task.WhenAll(partsTask, carsTask);

                var cars = carsTask.Result
                    .GroupBy(car => car.Id)
                    .ToDictionary(group => group.Key, group => group.First());

                var rows = partsTask.Result
                    .Select(part => PartCompatibilityPartRow.From(part, ResolveCar(part.UsedCarId, cars)))
                    .OrderByDescending(part => part.HasOem)
                    .ThenBy(part => part.Name)
                    .ThenBy(part => part.DisplayCode)
                    .ToList();

                _carsById.Clear();
                foreach (var car in cars.Values)
                {
                    _carsById[car.Id] = car;
                }

                _allParts.Clear();
                _allParts.AddRange(rows);

                _lastLoadedAt = DateTime.Now;
                RefreshFilteredParts(selectFirstWhenMissing: true);

                Status = $"Loaded {_allParts.Count:N0} parts across {_carsById.Count:N0} donor vehicles.";
                StatusBrush = Brushes.LightGreen;
            }
            catch (Exception ex)
            {
                Status = "Could not load compatibility data.";
                StatusBrush = Brushes.IndianRed;
                AppNotificationCenter.Instance.Publish($"Compatibility graph failed to load: {ex.Message}", false);
                ClearGraph();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static UsedCarDto? ResolveCar(int? usedCarId, Dictionary<int, UsedCarDto> cars)
        {
            if (!usedCarId.HasValue)
            {
                return null;
            }

            return cars.TryGetValue(usedCarId.Value, out var car) ? car : null;
        }

        private void RefreshFilteredParts(bool selectFirstWhenMissing = false)
        {
            if (_allParts.Count == 0)
            {
                FilteredParts.Clear();
                PartCountSummary = _lastLoadedAt.HasValue
                    ? "No parts returned from inventory."
                    : "No parts loaded.";
                return;
            }

            var query = NormalizeSearch(SearchText);
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _allParts
                : _allParts.Where(part => part.SearchText.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            var rows = filtered.Take(160).ToList();
            Replace(FilteredParts, rows);
            PartCountSummary = $"{rows.Count:N0} shown of {_allParts.Count:N0} parts";

            if (SelectedPart == null && selectFirstWhenMissing)
            {
                SelectedPart = rows.FirstOrDefault() ?? _allParts.FirstOrDefault();
                return;
            }

            if (SelectedPart != null && rows.Count > 0 && !rows.Contains(SelectedPart) && !string.IsNullOrWhiteSpace(query))
            {
                SelectedPart = rows.First();
                return;
            }

            RebuildCompatibility();
        }

        private void RebuildCompatibility()
        {
            if (SelectedPart == null)
            {
                ClearGraph();
                return;
            }

            var matches = _allParts
                .Select(part => ScoreCompatibility(SelectedPart, part))
                .Where(match => match.Score >= 52)
                .OrderByDescending(match => match.Score)
                .ThenBy(match => match.Part.VehicleName)
                .ThenBy(match => match.Part.ModelYear)
                .ThenBy(match => match.Part.Name)
                .Take(42)
                .ToList();

            if (!matches.Any(match => match.Part.Id == SelectedPart.Id))
            {
                matches.Insert(0, new CompatibilityMatch(SelectedPart, "Selected part", 120));
            }

            var proofParts = matches
                .Where(match => match.Part.Id != SelectedPart.Id)
                .Take(10)
                .Select(match => new PartCompatibilityProofPart(match.Part, match.Reason, match.Score))
                .ToList();

            var fitmentGroups = BuildFitmentGroups(matches).ToList();

            Replace(ProofParts, proofParts);
            Replace(FitmentGroups, fitmentGroups);
            Replace(MetricTiles, BuildMetrics(matches, fitmentGroups));
            BuildGraph(matches, fitmentGroups, proofParts);
        }

        private static CompatibilityMatch ScoreCompatibility(PartCompatibilityPartRow selected, PartCompatibilityPartRow candidate)
        {
            if (selected.Id == candidate.Id)
            {
                return new CompatibilityMatch(candidate, "Selected part", 120);
            }

            if (selected.HasOem &&
                string.Equals(selected.NormalizedOem, candidate.NormalizedOem, StringComparison.OrdinalIgnoreCase))
            {
                return new CompatibilityMatch(candidate, "OEM match", 98);
            }

            if (selected.CategoryId > 0 &&
                selected.CategoryId == candidate.CategoryId &&
                selected.NormalizedName == candidate.NormalizedName &&
                selected.NormalizedName.Length >= 5)
            {
                return new CompatibilityMatch(candidate, "Same name and category", 84);
            }

            var tokenOverlap = CalculateTokenOverlap(selected.NameTokens, candidate.NameTokens);
            if (selected.CategoryId > 0 && selected.CategoryId == candidate.CategoryId && tokenOverlap >= 0.66)
            {
                return new CompatibilityMatch(candidate, "Name tokens and category", 72);
            }

            if (selected.NormalizedName == candidate.NormalizedName && selected.NormalizedName.Length >= 6)
            {
                return new CompatibilityMatch(candidate, "Name match", 64);
            }

            if (selected.UsedCarId.HasValue &&
                selected.UsedCarId == candidate.UsedCarId &&
                selected.CategoryId == candidate.CategoryId)
            {
                return new CompatibilityMatch(candidate, "Same donor category", 56);
            }

            return new CompatibilityMatch(candidate, "Not matched", 0);
        }

        private IEnumerable<PartCompatibilityFitmentGroup> BuildFitmentGroups(IEnumerable<CompatibilityMatch> matches)
        {
            return matches
                .Where(match => match.Part.UsedCarId.HasValue && match.Part.UsedCarId.Value > 0)
                .GroupBy(match => string.IsNullOrWhiteSpace(match.Part.VehicleName) ? "Unlinked vehicle" : match.Part.VehicleName)
                .Select(group =>
                {
                    var years = group
                        .Select(match => match.Part.ModelYear)
                        .Where(year => year > 0)
                        .Distinct()
                        .OrderBy(year => year)
                        .ToList();

                    var reasons = group
                        .Select(match => match.Reason)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var partCodes = group
                        .Select(match => match.Part.DisplayCode)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Take(4)
                        .ToList();

                    return new PartCompatibilityFitmentGroup
                    {
                        ModelName = group.Key,
                        YearRange = FormatYearRange(years),
                        PartCount = group.Select(match => match.Part.Id).Distinct().Count(),
                        DonorCount = group.Select(match => match.Part.UsedCarId).Distinct().Count(),
                        ReasonText = reasons.Count == 0 ? "Compatibility evidence" : string.Join(", ", reasons),
                        ProofCodes = partCodes.Count == 0 ? "No part codes" : string.Join(", ", partCodes)
                    };
                })
                .OrderByDescending(group => group.PartCount)
                .ThenBy(group => group.ModelName)
                .Take(12);
        }

        private IReadOnlyList<PartCompatibilityMetricTile> BuildMetrics(
            IReadOnlyCollection<CompatibilityMatch> matches,
            IReadOnlyCollection<PartCompatibilityFitmentGroup> fitmentGroups)
        {
            var matchingParts = Math.Max(0, matches.Select(match => match.Part.Id).Distinct().Count() - 1);
            var donorCars = matches
                .Select(match => match.Part.UsedCarId)
                .Where(id => id.HasValue)
                .Distinct()
                .Count();
            var availableStock = matches
                .Where(match => match.Part.Id != SelectedPart?.Id)
                .Sum(match => Math.Max(0, match.Part.AvailableQuantity));

            return new List<PartCompatibilityMetricTile>
            {
                new("Models", fitmentGroups.Count.ToString("N0", CultureInfo.InvariantCulture), "Distinct fitment groups", Accent("#FF29B6F6")),
                new("Donor cars", donorCars.ToString("N0", CultureInfo.InvariantCulture), "Linked vehicle records", Accent("#FFFFB74D")),
                new("Also fits", matchingParts.ToString("N0", CultureInfo.InvariantCulture), "Compatible part records", Accent("#FF81C784")),
                new("Available", availableStock.ToString("N0", CultureInfo.InvariantCulture), "Units in candidate stock", Accent("#FFE57373"))
            };
        }

        private void BuildGraph(
            IReadOnlyCollection<CompatibilityMatch> matches,
            IReadOnlyList<PartCompatibilityFitmentGroup> fitmentGroups,
            IReadOnlyList<PartCompatibilityProofPart> proofParts)
        {
            GraphNodes.Clear();
            GraphEdges.Clear();

            if (SelectedPart == null)
            {
                return;
            }

            var center = new PartCompatibilityGraphNode
            {
                Title = Shorten(SelectedPart.Name, 24),
                Subtitle = SelectedPart.DisplayCode,
                X = CanvasWidth / 2,
                Y = CanvasHeight / 2,
                Radius = 74,
                Fill = Accent("#FFFF7043"),
                Stroke = Accent("#FFFFAB91"),
                Foreground = Brushes.White,
                StrokeThickness = 3,
                Part = SelectedPart
            };
            GraphNodes.Add(center);

            var groupNodes = fitmentGroups.Take(10).ToList();
            var groupRadiusX = 310.0;
            var groupRadiusY = 190.0;
            for (var i = 0; i < groupNodes.Count; i++)
            {
                var angle = -Math.PI / 2 + (2 * Math.PI * i / Math.Max(1, groupNodes.Count));
                var group = groupNodes[i];
                var node = new PartCompatibilityGraphNode
                {
                    Title = Shorten(group.ModelName, 22),
                    Subtitle = group.YearRange,
                    X = CanvasWidth / 2 + Math.Cos(angle) * groupRadiusX,
                    Y = CanvasHeight / 2 + Math.Sin(angle) * groupRadiusY,
                    Radius = 48,
                    Fill = Accent(i % 2 == 0 ? "#FF203A43" : "#FF244B5A"),
                    Stroke = Accent("#FF4DD0E1"),
                    Foreground = Brushes.White,
                    StrokeThickness = 2
                };
                GraphNodes.Add(node);
                GraphEdges.Add(new PartCompatibilityGraphEdge(center.X, center.Y, node.X, node.Y, Accent("#884DD0E1"), 2.2));
            }

            var proofNodes = proofParts.Take(8).ToList();
            for (var i = 0; i < proofNodes.Count; i++)
            {
                var angle = Math.PI / 7 + (5 * Math.PI / 7 * i / Math.Max(1, proofNodes.Count - 1));
                var proof = proofNodes[i];
                var node = new PartCompatibilityGraphNode
                {
                    Title = Shorten(proof.Part.DisplayCode, 18),
                    Subtitle = Shorten(proof.Reason, 18),
                    X = CanvasWidth / 2 + Math.Cos(angle) * 355,
                    Y = CanvasHeight / 2 + Math.Sin(angle) * 225,
                    Radius = 38,
                    Fill = Accent("#FF2E7D32"),
                    Stroke = Accent("#FFA5D6A7"),
                    Foreground = Brushes.White,
                    StrokeThickness = 2,
                    Part = proof.Part
                };
                GraphNodes.Add(node);
                GraphEdges.Add(new PartCompatibilityGraphEdge(center.X, center.Y, node.X, node.Y, Accent("#66A5D6A7"), 1.8));
            }
        }

        private void SelectGraphNode(object? parameter)
        {
            if (parameter is PartCompatibilityGraphNode node && node.Part != null)
            {
                SelectedPart = node.Part;
            }
        }

        private void ClearGraph()
        {
            GraphNodes.Clear();
            GraphEdges.Clear();
            FitmentGroups.Clear();
            ProofParts.Clear();
            MetricTiles.Clear();
        }

        private static double CalculateTokenOverlap(IReadOnlyCollection<string> left, IReadOnlyCollection<string> right)
        {
            if (left.Count == 0 || right.Count == 0)
            {
                return 0;
            }

            var rightSet = new HashSet<string>(right, StringComparer.OrdinalIgnoreCase);
            var matches = left.Count(token => rightSet.Contains(token));
            return (double)matches / Math.Max(left.Count, right.Count);
        }

        private static string FormatYearRange(IReadOnlyList<int> years)
        {
            if (years.Count == 0)
            {
                return "Year unknown";
            }

            if (years.Count == 1)
            {
                return years[0].ToString(CultureInfo.InvariantCulture);
            }

            return $"{years.First()}-{years.Last()}";
        }

        private static string Shorten(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            {
                return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            }

            return value[..Math.Max(0, maxLength - 1)] + "...";
        }

        private static string NormalizeIdentifier(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        }

        private static string NormalizeSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var chars = value
                .Trim()
                .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : ' ')
                .ToArray();

            return string.Join(" ", new string(chars)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        private static IReadOnlyList<string> Tokenize(string? value)
        {
            return NormalizeSearch(value)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(token => token.Length > 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static SolidColorBrush Accent(string hex)
        {
            var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;
            brush.Freeze();
            return brush;
        }

        private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> source)
        {
            target.Clear();
            foreach (var item in source)
            {
                target.Add(item);
            }
        }

        private static void HandleBackgroundException(Exception ex)
        {
            AppNotificationCenter.Instance.Publish($"Compatibility graph error: {ex.Message}", false);
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private sealed record CompatibilityMatch(PartCompatibilityPartRow Part, string Reason, int Score);

        public sealed class PartCompatibilityPartRow
        {
            private PartCompatibilityPartRow(
                PartDto source,
                UsedCarDto? car,
                string normalizedOem,
                string normalizedName,
                IReadOnlyList<string> nameTokens,
                string searchText)
            {
                Id = source.Id;
                InternalCode = source.InternalCode ?? string.Empty;
                Name = source.Name ?? string.Empty;
                OemNumber = source.OEMNumber ?? string.Empty;
                CategoryId = source.CategoryId;
                UsedCarId = source.UsedCarId;
                StockQuantity = source.StockQuantity;
                AvailableQuantity = source.AvailableQuantity;
                SalePrice = source.SalePrice;
                Currency = source.Currency ?? "USD";
                IsActive = source.IsActive;
                VehicleName = car?.Car ?? string.Empty;
                ModelYear = car?.ModelYear ?? 0;
                NormalizedOem = normalizedOem;
                NormalizedName = normalizedName;
                NameTokens = nameTokens;
                SearchText = searchText;
            }

            public int Id { get; }
            public string InternalCode { get; }
            public string Name { get; }
            public string OemNumber { get; }
            public int CategoryId { get; }
            public int? UsedCarId { get; }
            public int StockQuantity { get; }
            public int AvailableQuantity { get; }
            public decimal SalePrice { get; }
            public string Currency { get; }
            public bool IsActive { get; }
            public string VehicleName { get; }
            public int ModelYear { get; }
            public string NormalizedOem { get; }
            public string NormalizedName { get; }
            public IReadOnlyList<string> NameTokens { get; }
            public string SearchText { get; }
            public bool HasOem => !string.IsNullOrWhiteSpace(NormalizedOem);
            public string DisplayCode => string.IsNullOrWhiteSpace(InternalCode) ? $"PART-{Id}" : InternalCode;
            public string OemDisplay => string.IsNullOrWhiteSpace(OemNumber) ? "not set" : OemNumber;
            public string VehicleLabel => string.IsNullOrWhiteSpace(VehicleName)
                ? "No donor vehicle"
                : ModelYear > 0 ? $"{VehicleName} {ModelYear}" : VehicleName;
            public string StockLabel => AvailableQuantity > 0
                ? $"{AvailableQuantity:N0} available / {StockQuantity:N0} on hand"
                : $"{StockQuantity:N0} on hand";
            public string PriceLabel => $"{SalePrice:N2} {Currency}";
            public string ListSubtitle => $"{DisplayCode} - {VehicleLabel}";

            public static PartCompatibilityPartRow From(PartDto source, UsedCarDto? car)
            {
                var normalizedOem = NormalizeIdentifier(source.OEMNumber);
                var normalizedName = NormalizeSearch(source.Name);
                var tokens = Tokenize(source.Name);
                var vehicleText = car == null ? string.Empty : $"{car.Car} {car.ModelYear}";
                var searchText = NormalizeSearch($"{source.InternalCode} {source.Name} {source.OEMNumber} {vehicleText}");

                return new PartCompatibilityPartRow(source, car, normalizedOem, normalizedName, tokens, searchText);
            }
        }
    }

    public sealed class PartCompatibilityMetricTile
    {
        public PartCompatibilityMetricTile(string label, string value, string detail, Brush accentBrush)
        {
            Label = label;
            Value = value;
            Detail = detail;
            AccentBrush = accentBrush;
        }

        public string Label { get; }
        public string Value { get; }
        public string Detail { get; }
        public Brush AccentBrush { get; }
    }

    public sealed class PartCompatibilityFitmentGroup
    {
        public string ModelName { get; init; } = string.Empty;
        public string YearRange { get; init; } = string.Empty;
        public int PartCount { get; init; }
        public int DonorCount { get; init; }
        public string ReasonText { get; init; } = string.Empty;
        public string ProofCodes { get; init; } = string.Empty;
        public string PartCountText => $"{PartCount:N0} matching parts";
        public string DonorCountText => $"{DonorCount:N0} donor cars";
    }

    public sealed class PartCompatibilityProofPart
    {
        public PartCompatibilityProofPart(
            PartCompatibilityViewModel.PartCompatibilityPartRow part,
            string reason,
            int score)
        {
            Part = part;
            Reason = reason;
            ScoreText = $"{score}%";
        }

        public PartCompatibilityViewModel.PartCompatibilityPartRow Part { get; }
        public string Reason { get; }
        public string ScoreText { get; }
    }

    public sealed class PartCompatibilityGraphNode
    {
        public string Title { get; init; } = string.Empty;
        public string Subtitle { get; init; } = string.Empty;
        public double X { get; init; }
        public double Y { get; init; }
        public double Radius { get; init; }
        public Brush Fill { get; init; } = Brushes.DimGray;
        public Brush Stroke { get; init; } = Brushes.LightGray;
        public Brush Foreground { get; init; } = Brushes.White;
        public double StrokeThickness { get; init; } = 2;
        public PartCompatibilityViewModel.PartCompatibilityPartRow? Part { get; init; }
        public double Left => X - Radius;
        public double Top => Y - Radius;
        public double Diameter => Radius * 2;
    }

    public sealed class PartCompatibilityGraphEdge
    {
        public PartCompatibilityGraphEdge(double x1, double y1, double x2, double y2, Brush stroke, double thickness)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
            Stroke = stroke;
            Thickness = thickness;
        }

        public double X1 { get; }
        public double Y1 { get; }
        public double X2 { get; }
        public double Y2 { get; }
        public Brush Stroke { get; }
        public double Thickness { get; }
    }
}
