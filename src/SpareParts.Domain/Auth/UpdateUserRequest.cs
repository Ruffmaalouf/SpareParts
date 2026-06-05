using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Auth
{
    public sealed class UpdateUserRequest
    {
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress, MaxLength(200)]
        public string? Email { get; set; }

        [Range(1, int.MaxValue)]
        public int? RoleId { get; set; }

        public bool IsActive { get; set; } = true;

        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        [MaxLength(128, ErrorMessage = "Password must not exceed 128 characters.")]
        public string? NewPassword { get; set; }
    }
}
