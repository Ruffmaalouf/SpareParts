using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Auth
{
    public class LoginRequest
    {
        [Required]
        [MaxLength(256)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Password { get; set; } = string.Empty;
    }
}
