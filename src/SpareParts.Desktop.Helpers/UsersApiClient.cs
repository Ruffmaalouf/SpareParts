using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Domain.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf
{
    public sealed class UsersApiClient
        : CrudEntityApiClientBase<UserDto, CreateUserRequest, int, UpdateUserRequest, int>, IUserApiClient
    {
        public UsersApiClient() : base(AppSettings.ApiBaseUrl)
        {
        }

        public override Task<List<UserDto>> RetrieveAsync() => base.RetrieveAsync<UserDto>("api/users");

        public override Task<int> AddAsync(CreateUserRequest request)
            => base.AddAsync<int>("api/users", request, "Empty user id response.");

        public override Task EditAsync(int id, UpdateUserRequest request)
            => base.EditAsync($"api/users/{id}", request);

        public override Task DeleteAsync(int id)
            => base.DeleteResourceAsync($"api/users/{id}");

        public Task<List<UserDto>> GetUsersAsync() => RetrieveAsync();
        public Task<int> CreateUserAsync(CreateUserRequest req) => AddAsync(req);
        public Task UpdateUserAsync(int id, UpdateUserRequest req) => EditAsync(id, req);
        public Task DeleteUserAsync(int id) => DeleteAsync(id);
    }
}
