using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Auth
{
    public class UpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int? RoleId { get; set; }
        public bool IsActive { get; set; } = true;

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
        public string? NewPassword { get; set; }
    }
}
