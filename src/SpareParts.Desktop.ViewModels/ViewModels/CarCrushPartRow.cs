using System.ComponentModel;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class CarCrushPartRow : INotifyPropertyChanged
    {
        private bool _selected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? PartName { get; set; }
        public string? Category { get; set; }
        public string? OemHint { get; set; }
        public bool IsHighValue { get; set; }
        public bool IsFastMoving { get; set; }

        public bool Selected
        {
            get => _selected;
            set { _selected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected))); }
        }
    }
}
