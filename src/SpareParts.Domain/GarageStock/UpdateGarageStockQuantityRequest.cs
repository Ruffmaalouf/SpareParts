using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.GarageStock
{
    public class UpdateGarageStockQuantityRequest
    {
        [Range(0, 999999)]
        public int Quantity { get; set; }
    }
}
