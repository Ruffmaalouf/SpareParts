using System;
using System.Globalization;
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
                new FrameworkPropertyMetadata(
                    DateTime.Today,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedDateChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(SaleDatePickerControl),
                new PropertyMetadata("DATE"));

        public static readonly DependencyProperty SelectedDateDisplayTextProperty =
            DependencyProperty.Register(
                nameof(SelectedDateDisplayText),
                typeof(string),
                typeof(SaleDatePickerControl),
                new PropertyMetadata("Select date"));

        public DateTime? SelectedDate
        {
            get => (DateTime?)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string SelectedDateDisplayText
        {
            get => (string)GetValue(SelectedDateDisplayTextProperty);
            set => SetValue(SelectedDateDisplayTextProperty, value);
        }

        public SaleDatePickerControl()
        {
            InitializeComponent();
            UpdateSelectedDateDisplayText();
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SaleDatePickerControl control)
            {
                control.UpdateSelectedDateDisplayText();
            }
        }

        private void TogglePopup_Click(object sender, RoutedEventArgs e)
        {
            CalendarPopup.IsOpen = !CalendarPopup.IsOpen;
        }

        private void Calendar_SelectedDatesChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (sender is Calendar calendar)
            {
                SelectedDate = calendar.SelectedDate;
            }
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedDate = DateTime.Today;
            PickerCalendar.DisplayDate = DateTime.Today;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedDate = null;
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            CalendarPopup.IsOpen = false;
        }

        private void UpdateSelectedDateDisplayText()
        {
            SelectedDateDisplayText = SelectedDate.HasValue
                ? SelectedDate.Value.ToString("dddd, dd MMM yyyy", CultureInfo.CurrentCulture)
                : "Select date";
        }
    }
}
