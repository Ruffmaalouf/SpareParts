namespace SpareParts.Domain.Auth
{
    public class UpdateRoleScreenPermissionsRequest
    {
        public bool CanViewInvoiceSearch { get; set; }
        public bool CanViewManagementScreen { get; set; }
        public bool CanViewSupplierTab { get; set; }
        public bool CanEditSupplier { get; set; }
        public bool CanModifySupplier { get; set; }
        public bool CanDeleteSupplier { get; set; }
    }
}
