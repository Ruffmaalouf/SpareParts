using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Auth;

namespace SpareParts.Desktop.Wpf
{
    public sealed class RolesApiClient : IRoleApiClient
    {
        private readonly IApiClient _api;

        public RolesApiClient(IApiClient? api = null)
        {
            _api = api ?? ApiClient.Instance;
        }

        public Task<List<RoleDto>> GetRolesAsync() => _api.GetRolesAsync();
        public Task<RoleDto> CreateRoleAsync(CreateRoleRequest req) => _api.CreateRoleAsync(req);
        public Task UpdateRoleAsync(int id, UpdateRoleRequest req) => _api.UpdateRoleAsync(id, req);
        public Task DeleteRoleAsync(int id) => _api.DeleteRoleAsync(id);
    }
}
