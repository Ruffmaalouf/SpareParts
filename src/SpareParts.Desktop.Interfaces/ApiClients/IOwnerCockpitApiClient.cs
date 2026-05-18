using SpareParts.Domain.OwnerCockpit;
using System;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IOwnerCockpitApiClient
    {
        Task<OwnerCockpitDashboardDto> GetDashboardAsync(DateTime? businessDate = null);
    }
}
