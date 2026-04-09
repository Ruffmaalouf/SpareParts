using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SpareParts.Desktop.Wpf
{
    public class ScreenToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
            {
                return Visibility.Collapsed;
            }

            var current = value.ToString();
            var target = parameter.ToString();
            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(target))
            {
                return Visibility.Collapsed;
            }

            return string.Equals(current, target, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
