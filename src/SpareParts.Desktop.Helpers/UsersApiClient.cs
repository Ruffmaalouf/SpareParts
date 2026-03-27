using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class UsersApiClient : IUserApiClient
    {
        private readonly IApiClient _api;

        public UsersApiClient(IApiClient? api = null)
        {
            _api = api ?? new ApiClient();
        }

        public Task<List<UserDto>> GetUsersAsync() => _api.GetUsersAsync();
        public Task<int> CreateUserAsync(CreateUserRequest req) => _api.CreateUserAsync(req);
        public Task UpdateUserAsync(int id, UpdateUserRequest req) => _api.UpdateUserAsync(id, req);
        public Task DeleteUserAsync(int id) => _api.DeleteUserAsync(id);
    }
}
