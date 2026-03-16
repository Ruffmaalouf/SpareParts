using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpareParts.Domain.Cars
{
    public class CreateCarBrandRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string RegionGroup { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
