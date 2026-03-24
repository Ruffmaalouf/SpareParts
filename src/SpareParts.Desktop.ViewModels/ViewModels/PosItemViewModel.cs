using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public class PosItemViewModel : INotifyPropertyChanged
    {
        public int PartId { get; set; }
        public string Description { get; set; } = string.Empty;   // ← ADDED
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => Quantity * UnitPrice;

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
