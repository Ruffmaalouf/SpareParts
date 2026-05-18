using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Inventory
{
    public sealed class TransferPartRequest
    {
        [Range(1, int.MaxValue)]
        public int FromWarehouseId { get; set; }

        [Range(1, int.MaxValue)]
        public int ToWarehouseId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
