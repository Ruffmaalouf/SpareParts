using SpareParts.Domain.Inventory;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class RepairPrepLinkedPartRow
    {
        private RepairPrepLinkedPartRow(PartDto source)
        {
            Id = source.Id;
            Title = string.IsNullOrWhiteSpace(source.Name) ? $"Part #{source.Id}" : source.Name;
            Subtitle = $"{source.InternalCode} - OEM {source.OEMNumber ?? "not set"}";
            Value = $"{source.Currency} {source.SalePrice:N2}";
        }

        public int Id { get; }
        public string Title { get; }
        public string Subtitle { get; }
        public string Value { get; }

        public static RepairPrepLinkedPartRow From(PartDto source)
            => new(source);
    }
}
