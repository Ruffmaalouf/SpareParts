namespace SpareParts.Domain.Auth
{
    // ── Login ─────────────────────────────────────────────────────────────────
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string    Token     { get; set; } = string.Empty;
        public string    FullName  { get; set; } = string.Empty;
        public string    Role      { get; set; } = string.Empty;
        public int       UserId    { get; set; }
        public DateTime  ExpiresAt { get; set; }
    }

    // ── User management ───────────────────────────────────────────────────────
    public class UserDto
    {
        public int       Id          { get; set; }
        public string    Username    { get; set; } = string.Empty;
        public string    FullName    { get; set; } = string.Empty;
        public string?   Email       { get; set; }
        public string    Role        { get; set; } = string.Empty;
        public bool      IsActive    { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime  CreatedAt   { get; set; }
    }

    public class CreateUserRequest
    {
        public string  Username { get; set; } = string.Empty;
        public string  FullName { get; set; } = string.Empty;
        public string? Email    { get; set; }
        public string  Password { get; set; } = string.Empty;
        public string  Role     { get; set; } = "Cashier";
    }

    public class UpdateUserRequest
    {
        public string  FullName    { get; set; } = string.Empty;
        public string? Email       { get; set; }
        public string  Role        { get; set; } = "Cashier";
        public bool    IsActive    { get; set; } = true;
        public string? NewPassword { get; set; }
    }
}
