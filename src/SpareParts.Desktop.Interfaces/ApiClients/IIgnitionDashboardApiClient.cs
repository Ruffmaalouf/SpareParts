using System;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>
    /// Client for the new composed dashboard endpoint GET /api/dashboard/summary
    /// (Ignition redesign, Report 04 §02). Additive — /api/owner-cockpit keeps working unchanged.
    /// </summary>
    public interface IIgnitionDashboardApiClient
    {
        Task<IgnitionDashboardSummaryDto> GetSummaryAsync(DateTime? businessDate = null);
    }
}
