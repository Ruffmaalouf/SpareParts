using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Data
{
    public interface IBrandsRepository
    {
        IEnumerable<Brand> GetAll();
        int Insert(Brand brand);
        bool Update(int id, string name, bool isActive, int userId);
        bool Delete(int id);
    }

    public interface ICategoriesRepository
    {
        IEnumerable<Category> GetAll();
        int Insert(Category category);
    }

    public interface IPartsRepository
    {
        Dictionary<int, Part> GetByIds(IList<int> partIds);
        IEnumerable<Part> GetAllActive();
        int Insert(Part part);
        bool Update(int id, CreatePartRequest request, int userId);
        bool Delete(int id);
    }

    public interface ICustomersRepository
    {
        IEnumerable<Customer> GetAll();
        int Insert(Customer customer);
        bool Update(int id, CreateCustomerRequest request, int userId);
        bool Delete(int id);
    }

    public interface ISuppliersRepository
    {
        IEnumerable<Supplier> GetAll();
        int Insert(Supplier supplier);
        bool Update(int id, CreateSupplierRequest request, int userId);
        bool Delete(int id);
    }

    public interface IInventoryRepository
    {
        Stock? GetStock(int partId, int warehouseId);
        int InsertStock(Stock stock);
        void UpdateStockQuantity(int stockId, int delta, int userId);
        int InsertStockMovement(StockMovement movement);
    }

    public interface ISalesRepository
    {
        int InsertInvoice(SalesInvoice invoice);
        void InsertItems(int invoiceId, IList<SalesInvoiceItem> items);
        bool InvoiceNumberExists(string invoiceNumber);
    }

    public interface IPurchasesRepository
    {
        int InsertInvoice(PurchaseInvoice invoice);
        void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items);
        bool PurchaseNumberExists(string purchaseNumber);
    }

    public interface IJournalRepository
    {
        int InsertEntry(JournalEntry entry);
        void InsertLines(int entryId, IList<JournalLine> lines);
    }

    public class BrandsRepository : IBrandsRepository
    {
        private readonly DbSession _session;

        public BrandsRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Brand> GetAll()
        {
            const string sql = "SELECT * FROM Brands ORDER BY Name";
            return _session.Connection.Query<Brand>(sql, transaction: _session.Transaction);
        }

        public int Insert(Brand brand)
        {
            const string sql = @"INSERT INTO Brands (Name, IsActive, CreatedAt, CreatedByUserId)
                                 VALUES (@Name, @IsActive, @CreatedAt, @CreatedByUserId);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, brand, _session.Transaction);
        }

        public bool Update(int id, string name, bool isActive, int userId)
        {
            const string sql = @"UPDATE Brands
                                 SET Name = @Name, IsActive = @IsActive,
                                     ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                Name = name,
                IsActive = isActive,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Brands WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }
    }

    public class CategoriesRepository : ICategoriesRepository
    {
        private readonly DbSession _session;

        public CategoriesRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Category> GetAll()
        {
            const string sql = "SELECT * FROM Categories ORDER BY Name";
            return _session.Connection.Query<Category>(sql, transaction: _session.Transaction);
        }

        public int Insert(Category category)
        {
            const string sql = @"INSERT INTO Categories (Name, ParentId, CreatedAt, CreatedByUserId)
                                 VALUES (@Name, @ParentId, @CreatedAt, @CreatedByUserId);
                                 SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, category, _session.Transaction);
        }
    }

    public class PartsRepository : IPartsRepository
    {
        private readonly DbSession _session;

        public PartsRepository(DbSession session)
        {
            _session = session;
        }

        public Dictionary<int, Part> GetByIds(IList<int> partIds)
        {
            const string sql = "SELECT * FROM Parts WHERE Id IN @Ids";
            return _session.Connection.Query<Part>(sql, new { Ids = partIds }, _session.Transaction)
                .ToDictionary(p => p.Id, p => p);
        }

        public IEnumerable<Part> GetAllActive()
        {
            const string sql = "SELECT * FROM Parts WHERE IsActive = 1 ORDER BY Name";
            return _session.Connection.Query<Part>(sql, transaction: _session.Transaction);
        }

        public int Insert(Part part)
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

        public bool Update(int id, CreatePartRequest request, int userId)
        {
            const string sql = @"UPDATE Parts
                                 SET InternalCode = @InternalCode, Barcode = @Barcode, Name = @Name,
                                     OEMNumber = @OEMNumber, Condition = @Condition, CategoryId = @CategoryId, BrandId = @BrandId,
                                     CostPrice = @CostPrice, SalePrice = @SalePrice, Currency = @Currency, MinStock = @MinStock,
                                     Notes = @Notes, ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                request.InternalCode,
                request.Barcode,
                request.Name,
                request.OEMNumber,
                request.Condition,
                request.CategoryId,
                request.BrandId,
                request.CostPrice,
                request.SalePrice,
                request.Currency,
                request.MinStock,
                request.Notes,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Parts WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }
    }

    public class CustomersRepository : ICustomersRepository
    {
        private readonly DbSession _session;

        public CustomersRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Customer> GetAll()
        {
            const string sql = "SELECT * FROM Customers ORDER BY Name";
            return _session.Connection.Query<Customer>(sql, transaction: _session.Transaction);
        }

        public int Insert(Customer customer)
        {
            const string sql = @"INSERT INTO Customers
                (Name, Phone, Email, Address, TaxNumber, OpeningBalance, CreatedAt, CreatedByUserId)
                VALUES
                (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, customer, _session.Transaction);
        }

        public bool Update(int id, CreateCustomerRequest request, int userId)
        {
            const string sql = @"UPDATE Customers
                                 SET Name = @Name, Phone = @Phone, Email = @Email, Address = @Address,
                                     TaxNumber = @TaxNumber, OpeningBalance = @OpeningBalance,
                                     ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                request.Name,
                request.Phone,
                request.Email,
                request.Address,
                request.TaxNumber,
                request.OpeningBalance,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Customers WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }
    }

    public class SuppliersRepository : ISuppliersRepository
    {
        private readonly DbSession _session;

        public SuppliersRepository(DbSession session)
        {
            _session = session;
        }

        public IEnumerable<Supplier> GetAll()
        {
            const string sql = "SELECT * FROM Suppliers ORDER BY Name";
            return _session.Connection.Query<Supplier>(sql, transaction: _session.Transaction);
        }

        public int Insert(Supplier supplier)
        {
            const string sql = @"INSERT INTO Suppliers
                (Name, Phone, Email, Address, TaxNumber, OpeningBalance, CreatedAt, CreatedByUserId)
                VALUES
                (@Name, @Phone, @Email, @Address, @TaxNumber, @OpeningBalance, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, supplier, _session.Transaction);
        }

        public bool Update(int id, CreateSupplierRequest request, int userId)
        {
            const string sql = @"UPDATE Suppliers
                                 SET Name = @Name, Phone = @Phone, Email = @Email, Address = @Address,
                                     TaxNumber = @TaxNumber, OpeningBalance = @OpeningBalance,
                                     ModifiedAt = @Now, ModifiedByUserId = @UserId
                                 WHERE Id = @Id";
            var updated = _session.Connection.Execute(sql, new
            {
                Id = id,
                request.Name,
                request.Phone,
                request.Email,
                request.Address,
                request.TaxNumber,
                request.OpeningBalance,
                Now = DateTime.UtcNow,
                UserId = userId
            }, _session.Transaction);

            return updated > 0;
        }

        public bool Delete(int id)
        {
            const string sql = "DELETE FROM Suppliers WHERE Id = @Id";
            var deleted = _session.Connection.Execute(sql, new { Id = id }, _session.Transaction);
            return deleted > 0;
        }
    }

    public class InventoryRepository : IInventoryRepository
    {
        private readonly DbSession _session;

        public InventoryRepository(DbSession session)
        {
            _session = session;
        }

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
            _session.Connection.Execute(sql, new
            {
                Id = stockId,
                Delta = delta,
                ModifiedAt = DateTime.UtcNow,
                ModifiedByUserId = userId
            }, _session.Transaction);
        }

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
    }

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

    public class JournalRepository : IJournalRepository
    {
        private readonly DbSession _session;

        public JournalRepository(DbSession session)
        {
            _session = session;
        }

        public int InsertEntry(JournalEntry entry)
        {
            const string sql = @"INSERT INTO JournalEntries
                (EntryDate, ReferenceType, ReferenceId, Description, CreatedAt, CreatedByUserId)
                VALUES
                (@EntryDate, @ReferenceType, @ReferenceId, @Description, @CreatedAt, @CreatedByUserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return _session.Connection.ExecuteScalar<int>(sql, entry, _session.Transaction);
        }

        public void InsertLines(int entryId, IList<JournalLine> lines)
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
    }
}
