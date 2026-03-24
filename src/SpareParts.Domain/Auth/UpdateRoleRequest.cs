namespace SpareParts.Domain.Auth
{
    public class UpdateRoleRequest
    {
        public string? Description { get; set; }
        public string BadgeColor { get; set; } = "#22FFFFFF";
        public string BadgeTextColor { get; set; } = "#FFFFFF";
        public bool IsActive { get; set; } = true;
    }
}
