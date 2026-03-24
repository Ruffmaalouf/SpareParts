namespace SpareParts.Domain.Auth
{
    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Cashier";
    }
}
