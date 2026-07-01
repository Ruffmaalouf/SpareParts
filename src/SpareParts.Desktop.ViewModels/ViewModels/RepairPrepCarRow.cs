using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class RepairPrepCarRow : INotifyPropertyChanged
    {
        private string _statusKey = "bought";
        private string _statusLabel = "Bought";
        private int _statusSortOrder;

        private RepairPrepCarRow(UsedCarDto source, IReadOnlyList<RepairPrepLinkedPartRow> linkedParts)
        {
            Id = source.Id;
            Title = $"{source.ModelYear} {source.Car}".Trim();
            Barcode = string.IsNullOrWhiteSpace(source.Barcode) ? "No barcode" : source.Barcode!;
            SupplierDisplay = string.IsNullOrWhiteSpace(source.SupplierName) ? "Supplier" : source.SupplierName;
            LocationDisplay = string.IsNullOrWhiteSpace(source.Location) ? "Location" : source.Location;
            Currency = string.IsNullOrWhiteSpace(source.BaseCurrencyCode) ? source.PriceCurrency : source.BaseCurrencyCode;
            PurchaseCost = source.PurchaseCostBase != 0m ? source.PurchaseCostBase : source.PriceBase;
            ShippingCost = source.ShippingCostBase != 0m ? source.ShippingCostBase : source.Shipping;
            CustomsCost = source.CustomsCostBase != 0m ? source.CustomsCostBase : source.Customs;
            RepairsCost = source.RepairsCostBase != 0m ? source.RepairsCostBase : source.Repairs;
            FullCost = source.FullCostBase != 0m ? source.FullCostBase : source.GrandTotalBase;
            LinkedParts = linkedParts;
            Tasks = BuildDefaultTasks(source);
            SetStatus(InferStatus(source));
            SearchText = $"{Title} {Barcode} {SupplierDisplay} {LocationDisplay}".ToLowerInvariant();
            RefreshTaskSummary();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; }
        public string Title { get; }
        public string Barcode { get; }
        public string SupplierDisplay { get; }
        public string LocationDisplay { get; }
        public string Currency { get; }
        public decimal PurchaseCost { get; }
        public decimal ShippingCost { get; }
        public decimal CustomsCost { get; }
        public decimal RepairsCost { get; }
        public decimal FullCost { get; }
        public string SearchText { get; }
        public ObservableCollection<RepairPrepTaskRow> Tasks { get; }
        public IReadOnlyList<RepairPrepLinkedPartRow> LinkedParts { get; }
        public int TaskCount => Tasks.Count;
        public int CompletedTaskCount => Tasks.Count(task => task.IsDone);
        public decimal TaskCost => Tasks.Sum(task => task.Cost);
        public int ProgressPercent => TaskCount == 0 ? 0 : (int)Math.Round(CompletedTaskCount * 100m / TaskCount);
        public string Subtitle => $"{Barcode} - {SupplierDisplay}";
        public string CostLabel => $"{Currency} {FullCost:N2}";
        public string LinkedPartsLabel => $"{LinkedParts.Count:N0} linked part(s)";

        public string StatusKey
        {
            get => _statusKey;
            set
            {
                if (_statusKey == value)
                {
                    return;
                }

                _statusKey = value;
                OnPropertyChanged(nameof(StatusKey));
            }
        }

        public string StatusLabel
        {
            get => _statusLabel;
            set
            {
                if (_statusLabel == value)
                {
                    return;
                }

                _statusLabel = value;
                OnPropertyChanged(nameof(StatusLabel));
            }
        }

        public int StatusSortOrder
        {
            get => _statusSortOrder;
            set
            {
                if (_statusSortOrder == value)
                {
                    return;
                }

                _statusSortOrder = value;
                OnPropertyChanged(nameof(StatusSortOrder));
            }
        }

        public void RefreshTaskSummary()
        {
            OnPropertyChanged(nameof(TaskCount));
            OnPropertyChanged(nameof(CompletedTaskCount));
            OnPropertyChanged(nameof(TaskCost));
            OnPropertyChanged(nameof(ProgressPercent));
        }

        public static RepairPrepCarRow From(UsedCarDto source, IReadOnlyList<PartDto> linkedParts)
            => new(source, linkedParts.Select(RepairPrepLinkedPartRow.From).ToList());

        private void SetStatus(RepairPrepColumn column)
        {
            StatusKey = column.Key;
            StatusLabel = column.Label;
            StatusSortOrder = column.SortOrder;
        }

        private static RepairPrepColumn InferStatus(UsedCarDto source)
        {
            var columns = RepairPrepColumn.CreateDefaultColumns();
            if (source.SalePriceBase > 0m)
            {
                return columns.First(item => item.Key == "sold");
            }

            if (source.Repairs > 0m || source.RepairsCostBase > 0m)
            {
                return columns.First(item => item.Key == "repairing");
            }

            if (source.IsReceived)
            {
                return columns.First(item => item.Key == "inspected");
            }

            return columns.First(item => item.Key == "bought");
        }

        private static ObservableCollection<RepairPrepTaskRow> BuildDefaultTasks(UsedCarDto source)
        {
            var tasks = new ObservableCollection<RepairPrepTaskRow>
            {
                new("Inspection checklist", 0m),
                new("Prep photos", 0m)
            };

            var repairs = source.RepairsCostBase != 0m ? source.RepairsCostBase : source.Repairs;
            var customs = source.CustomsCostBase != 0m ? source.CustomsCostBase : source.Customs;
            if (repairs > 0m)
            {
                tasks.Add(new RepairPrepTaskRow("Repair labor", repairs));
            }

            if (customs > 0m)
            {
                tasks.Add(new RepairPrepTaskRow("Customs cleared", customs));
            }

            return tasks;
        }

        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
