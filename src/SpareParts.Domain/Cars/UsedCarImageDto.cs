using System;

namespace SpareParts.Domain.Cars
{
    public sealed class UsedCarImageDto
    {
        public int Id { get; set; }
        public int UsedCarId { get; set; }
        public byte[] ImageData { get; set; } = [];
        public string MimeType { get; set; } = "image/png";
        public DateTime CreatedAt { get; set; }
    }
}
