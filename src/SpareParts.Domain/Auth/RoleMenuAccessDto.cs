namespace SpareParts.Domain.Auth
{
    public class RoleMenuAccessDto
    {
        public int MenuId { get; set; }
        public string MenuKey { get; set; } = string.Empty;
        public string MenuName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanModify { get; set; }
        public bool CanDelete { get; set; }
    }
}
