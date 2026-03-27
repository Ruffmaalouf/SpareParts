namespace SpareParts.Domain.Auth
{
    public class RoleMenuAccessUpdateItem
    {
        public int MenuId { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanModify { get; set; }
        public bool CanDelete { get; set; }
    }
}
