namespace SpareParts.Domain.Auth
{
    public class RoleDto
    {
        public int     Id             { get; set; }
        public string  Name           { get; set; } = string.Empty;
        public string? Description    { get; set; }
        public string  BadgeColor     { get; set; } = "#22FFFFFF";
        public string  BadgeTextColor { get; set; } = "#FFFFFF";
        public bool    IsSystem       { get; set; }
        public bool    IsActive       { get; set; }
    }

    public class CreateRoleRequest
    {
        public string  Name           { get; set; } = string.Empty;
        public string? Description    { get; set; }
        public string  BadgeColor     { get; set; } = "#22FFFFFF";
        public string  BadgeTextColor { get; set; } = "#FFFFFF";
    }

    public class UpdateRoleRequest
    {
        public string? Description    { get; set; }
        public string  BadgeColor     { get; set; } = "#22FFFFFF";
        public string  BadgeTextColor { get; set; } = "#FFFFFF";
        public bool    IsActive       { get; set; } = true;
    }
}
