namespace SpareParts.Api.Services
{
    internal sealed class UserRow
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PasswordHash { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public bool IsActive { get; set; }
    }
}
