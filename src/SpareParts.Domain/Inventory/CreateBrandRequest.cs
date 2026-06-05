using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Inventory
{
    public sealed class CreateBrandRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
