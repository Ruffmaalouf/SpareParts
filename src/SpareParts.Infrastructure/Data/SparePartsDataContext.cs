using System;
using System.Collections.Generic;
using Dapper;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Accounting;

namespace SpareParts.Infrastructure.Data
{
    public class SparePartsDataContext
    {
        private readonly DbSession _session;

        public SparePartsDataContext(DbSession session)
        {
            _session = session;
        }

        // ── Brand ─────────────────────────────────────────────────────────────

        public int InsertBrand(Brand brand)
        {
            const string sql = @"INSERT INTO Brands (Name, IsActive, CreatedAt, CreatedByUserId)
                                 VALUES (@Name, @IsActive, @CreatedAt, @CreatedByUserId);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, brand, _session.Transaction);
        }

        public CarBrand? GetBrand(int id)
        {
            const string sql = "SELECT * FROM Brands WHERE Id = @Id";
            return _session.Connection.QueryFirstOrDefault<CarBrand>(sql, new { Id = id }, _session.Transaction);
        }

        public IEnumerable<Brand> GetAllBrands()
        {
            const string sql = "SELECT * FROM Brands ORDER BY Name";
            return _session.Connection.Query<Brand>(sql, transaction: _session.Transaction);
        }

        // ── Category ──────────────────────────────────────────────────────────

        public int InsertCategory(Category category)
        {
            const string sql = @"INSERT INTO Categories (Name, ParentId, CreatedAt, CreatedByUserId)
                                 VALUES (@Name, @ParentId, @CreatedAt, @CreatedByUserId);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, category, _session.Transaction);
        }

        public Category? GetCategory(int id)
        {
            const string sql = "SELECT * FROM Categories WHERE Id = @Id";
            return _session.Connection.QueryFirstOrDefault<Category>(sql, new { Id = id }, _session.Transaction);
        }

        // ── Part ──────────────────────────────────────────────────────────────

        public int InsertPart(Part part)
        {
            const string sql = @"INSERT INTO Parts
                (InternalCode, Barcode, Name, OEMNumber, Condition, CategoryId, BrandId,
                 CostPrice, SalePrice, Currency, MinStock, Notes, IsActive, CreatedAt, CreatedByUserId)
                VALUES
                (@InternalCode, @Barcode, @Name, @OEMNumber, @Condition, @CategoryId, @BrandId,
                 @CostPrice, @SalePrice, @Currency, @MinStock, @Notes, @IsActive, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, part, _session.Transaction);
        }

        public Part? GetPart(int id)
        {
            const string sql = "SELECT * FROM Parts WHERE Id = @Id";
            return _session.Connection.QueryFirstOrDefault<Part>(sql, new { Id = id }, _session.Transaction);
        }

        public IList<Part> GetPartsByIds(IEnumerable<int> ids)
        {
            const string sql = "SELECT * FROM Parts WHERE Id IN @Ids";
            return _session.Connection.Query<Part>(sql, new { Ids = ids }, _session.Transaction).AsList();
        }

        public IEnumerable<Part> GetAllParts()
        {
            const string sql = "SELECT * FROM Parts WHERE IsActive = 1 ORDER BY Name";
            return _session.Connection.Query<Part>(sql, transaction: _session.Transaction);
        }

        public void UpdatePart(Part part)
        {
            const string sql = @"UPDATE Parts SET
                    InternalCode = @InternalCode,
                    Barcode = @Barcode,
                    Name = @Name,
                    OEMNumber = @OEMNumber,
                    Condition = @Condition,
                    CategoryId = @CategoryId,
                    BrandId = @BrandId,
                    CostPrice = @CostPrice,
                    SalePrice = @SalePrice,
                    Currency = @Currency,
                    MinStock = @MinStock,
                    Notes = @Notes,
                    IsActive = @IsActive,
                    ModifiedAt = @ModifiedAt,
                    ModifiedByUserId = @ModifiedByUserId
                WHERE Id = @Id";
            _session.Connection.Execute(sql, part, _session.Transaction);
        }

        // ── CarModel ──────────────────────────────────────────────────────────

        public int InsertCarModel(CarModelEntity model)
        {
            const string sql = @"INSERT INTO CarModels
                (Name, Year, EngineType, BasePrice, ImagePath, BrandId, CreatedAt, CreatedByUserId)
                VALUES
                (@Name, @Year, @EngineType, @BasePrice, @ImagePath, @BrandId, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, model, _session.Transaction);
        }

        public IEnumerable<CarModelEntity> GetAllCarModels()
        {
            const string sql = "SELECT * FROM CarModels ORDER BY Name";
            return _session.Connection.Query<CarModelEntity>(sql, transaction: _session.Transaction);
        }

        // ── Stock ─────────────────────────────────────────────────────────────

        public Stock? GetStock(int partId, int warehouseId)
        {
            const string sql = "SELECT TOP 1 * FROM Stock WHERE PartId = @PartId AND WarehouseId = @WarehouseId";
            return _session.Connection.QueryFirstOrDefault<Stock>(sql, new { PartId = partId, WarehouseId = warehouseId }, _session.Transaction);
        }

        public int InsertStock(Stock stock)
        {
            const string sql = @"INSERT INTO Stock
                (PartId, WarehouseId, LocationId, Quantity, ReservedQuantity, CreatedAt, CreatedByUserId)
                VALUES
                (@PartId, @WarehouseId, @LocationId, @Quantity, @ReservedQuantity, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, stock, _session.Transaction);
        }

        public void UpdateStockQuantity(int stockId, int delta, int userId)
        {
            const string sql = @"UPDATE Stock
                                 SET Quantity = Quantity + @Delta,
                                     ModifiedAt = @ModifiedAt,
                                     ModifiedByUserId = @ModifiedByUserId
                                 WHERE Id = @Id";
            _session.Connection.Execute(sql, new { Id = stockId, Delta = delta, ModifiedAt = DateTime.UtcNow, ModifiedByUserId = userId }, _session.Transaction);
        }

        // ── StockMovement ─────────────────────────────────────────────────────

        public int InsertStockMovement(StockMovement movement)
        {
            const string sql = @"INSERT INTO StockMovements
                (PartId, WarehouseId, Quantity, MovementType, ReferenceType, ReferenceId,
                 UnitCost, CreatedAt, CreatedByUserId)
                VALUES
                (@PartId, @WarehouseId, @Quantity, @MovementType, @ReferenceType, @ReferenceId,
                 @UnitCost, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, movement, _session.Transaction);
        }

        // ── Customer ──────────────────────────────────────────────────────────

        public int InsertCustomer(Customer customer)
        {
            const string sql = @"INSERT INTO Customers
                (Name, Phone, Email, Address, TaxNumber, OpeningBalance, CreatedAt, CreatedByUserId)
                VALUES
                (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, customer, _session.Transaction);
        }

        public Customer? GetCustomer(int id)
        {
            const string sql = "SELECT * FROM Customers WHERE Id = @Id";
            return _session.Connection.QueryFirstOrDefault<Customer>(sql, new { Id = id }, _session.Transaction);
        }

        public IEnumerable<Customer> GetAllCustomers()
        {
            const string sql = "SELECT * FROM Customers ORDER BY Name";
            return _session.Connection.Query<Customer>(sql, transaction: _session.Transaction);
        }

        // ── Supplier ──────────────────────────────────────────────────────────

        public int InsertSupplier(Supplier supplier)
        {
            const string sql = @"INSERT INTO Suppliers
                (Name, Phone, Email, Address, TaxNumber, OpeningBalance, CreatedAt, CreatedByUserId)
                VALUES
                (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, supplier, _session.Transaction);
        }

        public Supplier? GetSupplier(int id)
        {
            const string sql = "SELECT * FROM Suppliers WHERE Id = @Id";
            return _session.Connection.QueryFirstOrDefault<Supplier>(sql, new { Id = id }, _session.Transaction);
        }

        public IEnumerable<Supplier> GetAllSuppliers()
        {
            const string sql = "SELECT * FROM Suppliers ORDER BY Name";
            return _session.Connection.Query<Supplier>(sql, transaction: _session.Transaction);
        }

        // ── SalesInvoice & Items ──────────────────────────────────────────────

        public int InsertSalesInvoice(SalesInvoice invoice)
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

        public void InsertSalesInvoiceItems(int invoiceId, IList<SalesInvoiceItem> items)
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

        // ── PurchaseInvoice & Items ───────────────────────────────────────────

        public int InsertPurchaseInvoice(PurchaseInvoice invoice)
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

        public void InsertPurchaseInvoiceItems(int purchaseId, IList<PurchaseInvoiceItem> items)
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

        // ── Accounting ────────────────────────────────────────────────────────

        public int InsertAccount(Account account)
        {
            const string sql = @"INSERT INTO Accounts
                (Code, Name, AccountType, ParentId, CreatedAt, CreatedByUserId)
                VALUES
                (@Code, @Name, @AccountType, @ParentId, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, account, _session.Transaction);
        }

        public Account? GetAccountByCode(string code)
        {
            const string sql = "SELECT * FROM Accounts WHERE Code = @Code";
            return _session.Connection.QueryFirstOrDefault<Account>(sql, new { Code = code }, _session.Transaction);
        }

        public int InsertJournalEntry(JournalEntry entry)
        {
            const string sql = @"INSERT INTO JournalEntries
                (EntryDate, ReferenceType, ReferenceId, Description, CreatedAt, CreatedByUserId)
                VALUES
                (@EntryDate, @ReferenceType, @ReferenceId, @Description, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, entry, _session.Transaction);
        }

        public void InsertJournalLines(int entryId, IList<JournalLine> lines)
        {
            const string sql = @"INSERT INTO JournalLines
                (JournalEntryId, AccountId, Debit, Credit, CreatedAt, CreatedByUserId)
                VALUES
                (@JournalEntryId, @AccountId, @Debit, @Credit, @CreatedAt, @CreatedByUserId);";
            foreach (var line in lines)
            {
                line.JournalEntryId = entryId;
                _session.Connection.Execute(sql, line, _session.Transaction);
            }
        }

        public IEnumerable<object> GetAllCategories()
        {
            throw new NotImplementedException();
        }
    }
}
