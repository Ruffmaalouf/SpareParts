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
                return Visibility.Collapsed;

            string current = value.ToString();
            string target = parameter.ToString();

            return string.Equals(current, target, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}