using Dapper;
using SpareParts.Domain.Sales;

using SpareParts.Infrastructure.Interfaces.Repositories;

namespace SpareParts.Infrastructure.Data
{

    public class SalesRepository : ISalesRepository
    {
        private readonly DbSession _session;

        public SalesRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertInvoice(SalesInvoice invoice)
        {
            const string sql = @"INSERT INTO SalesInvoices
                (InvoiceNumber, InvoiceDate, CustomerId, WarehouseId, Subtotal, DiscountAmount,
                 TaxAmount, TotalAmount, PaidAmount, PaymentStatus, PaymentMethod, Notes,
                 IsReturn, ParentInvoiceId, TotalCost, CreatedAt, CreatedByUserId)
                VALUES
                (@InvoiceNumber, @InvoiceDate, @CustomerId, @WarehouseId, @Subtotal, @DiscountAmount,
                 @TaxAmount, @TotalAmount, @PaidAmount, @PaymentStatus, @PaymentMethod, @Notes,
                 @IsReturn, @ParentInvoiceId, @TotalCost, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, invoice, _session.Transaction);
        }

        public void InsertItems(int invoiceId, IList<SalesInvoiceItem> items)
        {
            const string sql = @"INSERT INTO SalesInvoiceItems
                (InvoiceId, PartId, Quantity, UnitPrice, DiscountAmount, TaxRate, LineTotal, CreatedAt, CreatedByUserId)
                VALUES
                (@InvoiceId, @PartId, @Quantity, @UnitPrice, @DiscountAmount, @TaxRate, @LineTotal, @CreatedAt, @CreatedByUserId);";
            foreach (var item in items)
            {
                item.InvoiceId = invoiceId;
                _session.Connection.Execute(sql, item, _session.Transaction);
            }
        }

        public bool InvoiceNumberExists(string invoiceNumber)
        {
            const string sql = "SELECT COUNT(1) FROM SalesInvoices WHERE InvoiceNumber = @InvoiceNumber";
            return _session.Connection.ExecuteScalar<int>(sql, new { InvoiceNumber = invoiceNumber }, _session.Transaction) > 0;
        }
    }
}
