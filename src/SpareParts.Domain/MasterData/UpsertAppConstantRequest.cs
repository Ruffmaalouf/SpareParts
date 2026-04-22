namespace SpareParts.Domain.MasterData
{
    public sealed class UpsertAppConstantRequest
    {
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
