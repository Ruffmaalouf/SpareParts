using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SpareParts.Desktop.Wpf
{
    internal static class SearchTreeHelper
    {
        public static DependencyObject? GetParent(DependencyObject element)
        {
            if (element is Visual || element is Visual3D)
            {
                return VisualTreeHelper.GetParent(element)
                    ?? LogicalTreeHelper.GetParent(element);
            }

            return LogicalTreeHelper.GetParent(element);
        }
    }
}
