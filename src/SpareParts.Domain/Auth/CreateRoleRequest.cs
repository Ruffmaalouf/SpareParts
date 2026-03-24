namespace SpareParts.Domain.Auth
{
    public class CreateRoleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string BadgeColor { get; set; } = "#22FFFFFF";
        public string BadgeTextColor { get; set; } = "#FFFFFF";
    }
}
