namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class UsedCarModelOption
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Year { get; init; }
        public decimal BasePrice { get; init; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(Year)
                ? Name
                : $"{Name} ({Year})";
    }
}
