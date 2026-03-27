using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class RolesApiClient : FeatureApiClientBase, IRoleApiClient
    {
        public RolesApiClient(IApiClient? api = null) : base(AppSettings.ApiBaseUrl)
        {
        }

        public Task<List<RoleDto>> GetRolesAsync() => RetrieveAsync<RoleDto>("api/roles");
        public Task<List<RoleMenuAccessDto>> GetRoleMenuAccessAsync(int roleId) => RetrieveAsync<RoleMenuAccessDto>($"api/roles/{roleId}/screens");
        public Task<List<RoleMenuAccessDto>> GetRoleMenuAccessByNameAsync(string roleName) => RetrieveAsync<RoleMenuAccessDto>($"api/roles/by-name/{roleName}/screens");
        public Task UpdateRoleMenuAccessAsync(int roleId, UpdateRoleMenuAccessRequest req) => EditAsync($"api/roles/{roleId}/screens", req);
        public Task<RoleDto> CreateRoleAsync(CreateRoleRequest req) => AddAsync<RoleDto>("api/roles", req, "Empty role response.");
        public Task UpdateRoleAsync(int id, UpdateRoleRequest req) => EditAsync($"api/roles/{id}", req);
        public Task DeleteRoleAsync(int id) => DeleteResourceAsync($"api/roles/{id}");
    }
}
