using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Auth
{
    public class CreateUserRequest
    {
        [Required, MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;
        [EmailAddress, MaxLength(200)]
        public string? Email { get; set; }
        [Required, MinLength(8), MaxLength(128)]
        public string Password { get; set; } = string.Empty;
        [Required, MaxLength(50)]
        public string Role { get; set; } = "Cashier";
    }
}
