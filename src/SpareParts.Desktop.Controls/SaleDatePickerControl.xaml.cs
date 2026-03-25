using System;
using System.Windows;
using System.Windows.Controls;

namespace SpareParts.Desktop.Wpf
{
    public partial class SaleDatePickerControl : UserControl
    {
        public static readonly DependencyProperty SelectedDateProperty =
            DependencyProperty.Register(
                nameof(SelectedDate),
                typeof(DateTime?),
                typeof(SaleDatePickerControl),
                new FrameworkPropertyMetadata(DateTime.Today, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(SaleDatePickerControl),
                new PropertyMetadata("DATE"));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public SaleDatePickerControl()
        {
            InitializeComponent();
        }

        private void TogglePopup_Click(object sender, RoutedEventArgs e)
        {
            CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
        }

        private void Calendar_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (SelectedDate.HasValue)
            {
                CalendarPopup.IsOpen = false;
            }
        }
    }
}
