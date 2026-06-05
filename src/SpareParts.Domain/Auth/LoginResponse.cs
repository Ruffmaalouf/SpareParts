namespace SpareParts.Domain.Auth
{
    public sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? RoleId { get; set; }
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
