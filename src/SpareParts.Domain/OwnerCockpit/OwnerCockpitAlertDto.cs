namespace SpareParts.Domain.OwnerCockpit
{
    public sealed class OwnerCockpitAlertDto
    {
        public string Severity { get; set; } = "Info";
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
