using SpareParts.Domain.Common;

namespace SpareParts.Domain.Auth
{
    public enum UserRole
    {
        Admin   = 1,
        Manager = 2,
        Cashier = 3
    }

    public class User : Entity
    {
        public string   Username     { get; set; } = string.Empty;
        public string   FullName     { get; set; } = string.Empty;
        public string?  Email        { get; set; }
        public string   PasswordHash { get; set; } = string.Empty;
        public string   Role         { get; set; } = "Cashier";
        public bool     IsActive     { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt    { get; set; }
        public DateTime? ModifiedAt  { get; set; }
    }
}
