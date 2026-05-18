namespace SpareParts.Domain.MasterData
{
    public class WarehouseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string? Address { get; set; }
        public bool IsMain { get; set; }
    }
}
