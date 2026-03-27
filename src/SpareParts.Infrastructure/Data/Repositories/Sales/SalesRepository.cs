using Dapper;
using SpareParts.Domain.Common;
using SpareParts.Domain.Sales;

using SpareParts.Infrastructure.Interfaces.Repositories;
using System.Linq;

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


        public List<SalesInvoiceLookupDto> SearchInvoices(string? query)
        {
            const string sql = @"SELECT TOP (200)
                    Id AS InvoiceId,
                    InvoiceNumber,
                    InvoiceDate,
                    CustomerId,
                    WarehouseId,
                    TotalAmount,
                    PaidAmount
                FROM SalesInvoices
                WHERE (@Query IS NULL OR @Query = ''
                       OR InvoiceNumber LIKE '%' + @Query + '%'
                       OR CAST(Id AS NVARCHAR(50)) LIKE '%' + @Query + '%')
                ORDER BY InvoiceDate DESC, Id DESC;";

            return _session.Connection.Query<SalesInvoiceLookupDto>(sql, new { Query = query?.Trim() }, _session.Transaction).ToList();
        }

        public SalesInvoiceDetailsDto? GetInvoiceById(int invoiceId)
        {
            const string invoiceSql = @"SELECT
                    Id AS InvoiceId,
                    InvoiceNumber,
                    InvoiceDate,
                    CustomerId,
                    WarehouseId,
                    TotalAmount,
                    PaidAmount
                FROM SalesInvoices
                WHERE Id = @InvoiceId;";

            const string itemsSql = @"SELECT
                    sii.PartId,
                    ISNULL(p.Name, '') AS Description,
                    sii.Quantity,
                    sii.UnitPrice
                FROM SalesInvoiceItems sii
                LEFT JOIN Parts p ON p.Id = sii.PartId
                WHERE sii.InvoiceId = @InvoiceId
                ORDER BY sii.Id;";

            var invoice = _session.Connection.QuerySingleOrDefault<SalesInvoiceDetailsDto>(invoiceSql, new { InvoiceId = invoiceId }, _session.Transaction);
            if (invoice == null)
            {
                return null;
            }

            invoice.Items = _session.Connection.Query<SalesInvoiceLineDto>(itemsSql, new { InvoiceId = invoiceId }, _session.Transaction).ToList();
            return invoice;
        }
        public bool InvoiceNumberExists(string invoiceNumber)
        {
            const string sql = "SELECT COUNT(1) FROM SalesInvoices WHERE InvoiceNumber = @InvoiceNumber";
            return _session.Connection.ExecuteScalar<int>(sql, new { InvoiceNumber = invoiceNumber }, _session.Transaction) > 0;
        }

        public bool UpdateInvoice(int invoiceId, UpdateSaleRequest request, int userId)
        {
            var subtotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
            var discountAmount = request.Items.Sum(i => i.DiscountAmount);
            var taxAmount = request.Items.Sum(i => (i.Quantity * i.UnitPrice - i.DiscountAmount) * (i.TaxRate / 100m));
            var totalAmount = subtotal - discountAmount + taxAmount;

            var paymentStatus = request.PaidAmount <= 0
                ? PaymentStatus.Unpaid.ToString()
                : request.PaidAmount >= totalAmount
                    ? PaymentStatus.Paid.ToString()
                    : PaymentStatus.PartiallyPaid.ToString();

            const string updateInvoiceSql = @"UPDATE SalesInvoices
                SET InvoiceDate = @InvoiceDate,
                    CustomerId = @CustomerId,
                    WarehouseId = @WarehouseId,
                    Subtotal = @Subtotal,
                    DiscountAmount = @DiscountAmount,
                    TaxAmount = @TaxAmount,
                    TotalAmount = @TotalAmount,
                    PaidAmount = @PaidAmount,
                    PaymentStatus = @PaymentStatus
                WHERE Id = @InvoiceId;";

            var updated = _session.Connection.Execute(updateInvoiceSql, new
            {
                InvoiceId = invoiceId,
                request.InvoiceDate,
                request.CustomerId,
                request.WarehouseId,
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                request.PaidAmount,
                PaymentStatus = paymentStatus
            }, _session.Transaction);

            if (updated == 0)
            {
                return false;
            }

            const string deleteItemsSql = "DELETE FROM SalesInvoiceItems WHERE InvoiceId = @InvoiceId;";
            _session.Connection.Execute(deleteItemsSql, new { InvoiceId = invoiceId }, _session.Transaction);

            const string insertItemSql = @"INSERT INTO SalesInvoiceItems
                (InvoiceId, PartId, Quantity, UnitPrice, DiscountAmount, TaxRate, LineTotal, CreatedAt, CreatedByUserId)
                VALUES
                (@InvoiceId, @PartId, @Quantity, @UnitPrice, @DiscountAmount, @TaxRate, @LineTotal, @CreatedAt, @CreatedByUserId);";

            foreach (var item in request.Items)
            {
                var lineTotal = (item.Quantity * item.UnitPrice - item.DiscountAmount) * (1 + (item.TaxRate / 100m));
                _session.Connection.Execute(insertItemSql, new
                {
                    InvoiceId = invoiceId,
                    item.PartId,
                    item.Quantity,
                    item.UnitPrice,
                    item.DiscountAmount,
                    item.TaxRate,
                    LineTotal = lineTotal,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = userId
                }, _session.Transaction);
            }

            return true;
        }
    }
}
