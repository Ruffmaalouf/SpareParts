namespace SpareParts.Api.Controllers
{
    internal class UserRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}
