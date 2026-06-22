using System;
using System.Globalization;
using System.Windows.Data;

namespace SpareParts.Desktop.Wpf
{
    /// <summary>
    /// Compares the bound ActiveScreen value (values[0]) against the nav item's
    /// Tag (values[1], the screen name it navigates to) and returns true when they
    /// match. Used to drive the persistent left accent bar on the active sidebar
    /// nav item, independent of mouse-over state.
    /// </summary>
    public class ScreenEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
            {
                return false;
            }

            var current = values[0].ToString();
            var target = values[1].ToString();
            if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(target))
            {
                return false;
            }

            return string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
