namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class UsedCarModelOption
    {
        public int Id { get; init; }
        public string CarBrandName { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string BodyType { get; init; } = string.Empty;

        public string DisplayName =>
            string.IsNullOrWhiteSpace(CarBrandName)
                ? FormatModelName()
                : $"{CarBrandName} {FormatModelName()}";

        private string FormatModelName() =>
            string.IsNullOrWhiteSpace(BodyType)
                ? Name
                : $"{Name} ({BodyType})";
    }
}
