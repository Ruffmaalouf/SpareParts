using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    internal sealed record StockArrivalSourceResult<T>(List<T> Rows, bool Succeeded);
}
