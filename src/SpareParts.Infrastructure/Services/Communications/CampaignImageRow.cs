using Dapper;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using System.Data;
using System.Globalization;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class CampaignImageRow
    {
        public int ImageId { get; set; }
        public int UsedCarId { get; set; }
        public string ImageMimeType { get; set; } = "image/png";
        public byte[] ImageData { get; set; } = [];
    }
}
