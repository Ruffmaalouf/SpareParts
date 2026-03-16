using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpareParts.Domain.Cars
{
    public class CarModelDto
    {
        public int Id { get; set; }
        public int CarBrandId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Year { get; set; }
        public string? EngineType { get; set; }
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
        public bool HasImage { get; set; }
    }
}
