using Dapper;
using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Data
{
    public interface IPurchasesRepository
    {
        int InsertInvoice(PurchaseInvoice invoice);
        void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items);
        bool PurchaseNumberExists(string purchaseNumber);
    }

    public class PurchasesRepository : IPurchasesRepository
    {
        private readonly DbSession _session;

        public PurchasesRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertInvoice(PurchaseInvoice invoice)
        {
            const string sql = @"INSERT INTO PurchaseInvoices
                (PurchaseNumber, PurchaseDate, SupplierId, WarehouseId, Subtotal, DiscountAmount,
                 TaxAmount, TotalAmount, PaidAmount, PaymentStatus, CreatedAt, CreatedByUserId)
                VALUES
                (@PurchaseNumber, @PurchaseDate, @SupplierId, @WarehouseId, @Subtotal, @DiscountAmount,
                 @TaxAmount, @TotalAmount, @PaidAmount, @PaymentStatus, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, invoice, _session.Transaction);
        }

        public bool PurchaseNumberExists(string purchaseNumber)
        {
            const string sql = "SELECT COUNT(1) FROM PurchaseInvoices WHERE PurchaseNumber = @PurchaseNumber";
            return _session.Connection.ExecuteScalar<int>(sql, new { PurchaseNumber = purchaseNumber }, _session.Transaction) > 0;
        }

        public void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items)
        {
            const string sql = @"INSERT INTO PurchaseInvoiceItems
                (PurchaseId, PartId, Quantity, UnitCost, TaxRate, LineTotal, CreatedAt, CreatedByUserId)
                VALUES
                (@PurchaseId, @PartId, @Quantity, @UnitCost, @TaxRate, @LineTotal, @CreatedAt, @CreatedByUserId);";
            foreach (var item in items)
            {
                item.PurchaseId = purchaseId;
                _session.Connection.Execute(sql, item, _session.Transaction);
            }
        }
    }
}
