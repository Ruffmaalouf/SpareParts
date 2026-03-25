using SpareParts.Domain.Auth;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    public interface IRoleApiClient
    {
        Task<List<RoleDto>> GetRolesAsync();
        Task<RoleDto> CreateRoleAsync(CreateRoleRequest req);
        Task UpdateRoleAsync(int id, UpdateRoleRequest req);
        Task DeleteRoleAsync(int id);
    }
}
