using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Inventory
{
    public sealed class CreateCategoryRequest
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }
    }
}
