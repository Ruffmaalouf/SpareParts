using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpareParts.Domain.Cars
{
    public class PartDto
    {
        public int Id { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public int MinStock { get; set; }
        public bool IsActive { get; set; }
    }
}
