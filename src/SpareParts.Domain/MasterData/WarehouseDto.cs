namespace SpareParts.Domain.MasterData
{
    public class WarehouseDto
    {
        public int     Id      { get; set; }
        public string  Name    { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool    IsMain  { get; set; }
    }

    public class CreateWarehouseRequest
    {
        public string  Name    { get; set; } = string.Empty;
        public string? Address { get; set; }
        public bool    IsMain  { get; set; }
    }
}
