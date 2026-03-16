using System;
using System.Globalization;
using System.Windows.Data;

namespace SpareParts.Desktop.Wpf
{
    /// <summary>
    /// Converts bool → !bool.
    /// Use as a resource: &lt;local:InverseBoolConverter x:Key="InvBool"/&gt;
    /// Then bind: IsEnabled="{Binding IsBusy, Converter={StaticResource InvBool}}"
    /// </summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : (object)true;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b ? !b : (object)true;
    }
}
