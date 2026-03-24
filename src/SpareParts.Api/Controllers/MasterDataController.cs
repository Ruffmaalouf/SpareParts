using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    // ── Customers ─────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CustomersController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<CustomerDto>> GetAll([FromQuery] string? search = null)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var all = ctx.GetAllCustomers();
            if (!string.IsNullOrWhiteSpace(search))
                all = all.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

            return Ok(all.Select(c => new CustomerDto
            {
                Id             = c.Id,
                Name           = c.Name,
                Phone          = c.Phone,
                Email          = c.Email,
                Address        = c.Address,
                TaxNumber      = c.TaxNumber,
                OpeningBalance = c.OpeningBalance
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateCustomerRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var customer = new Customer
            {
                Name            = req.Name,
                Phone           = req.Phone,
                Email           = req.Email,
                Address         = req.Address,
                TaxNumber       = req.TaxNumber,
                OpeningBalance  = req.OpeningBalance,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = ctx.InsertCustomer(customer);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateCustomerRequest req)
        {
            using var session = new DbSession(_factory);
            var updated = session.Connection.Execute(
                @"UPDATE Customers
                  SET Name = @Name, Phone = @Phone, Email = @Email, Address = @Address,
                      TaxNumber = @TaxNumber, OpeningBalance = @OpeningBalance,
                      ModifiedAt = @Now, ModifiedByUserId = @UserId
                  WHERE Id = @Id",
                new
                {
                    Id = id,
                    req.Name,
                    req.Phone,
                    req.Email,
                    req.Address,
                    req.TaxNumber,
                    req.OpeningBalance,
                    Now = DateTime.UtcNow,
                    UserId = GetUserId()
                },
                session.Transaction);
            if (updated == 0) return NotFound();
            session.Commit();
            return NoContent();
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (c == null || !int.TryParse(c.Value, out var userId))
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            return userId;
        }
    }

    // ── Suppliers ─────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/suppliers")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public SuppliersController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<SupplierDto>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllSuppliers().Select(s => new SupplierDto
            {
                Id             = s.Id,
                Name           = s.Name,
                Phone          = s.Phone,
                Email          = s.Email,
                Address        = s.Address,
                TaxNumber      = s.TaxNumber,
                OpeningBalance = s.OpeningBalance
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateSupplierRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var supplier = new Supplier
            {
                Name            = req.Name,
                Phone           = req.Phone,
                Email           = req.Email,
                Address         = req.Address,
                TaxNumber       = req.TaxNumber,
                OpeningBalance  = req.OpeningBalance,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = ctx.InsertSupplier(supplier);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateSupplierRequest req)
        {
            using var session = new DbSession(_factory);
            var updated = session.Connection.Execute(
                @"UPDATE Suppliers
                  SET Name = @Name, Phone = @Phone, Email = @Email, Address = @Address,
                      TaxNumber = @TaxNumber, OpeningBalance = @OpeningBalance,
                      ModifiedAt = @Now, ModifiedByUserId = @UserId
                  WHERE Id = @Id",
                new
                {
                    Id = id,
                    req.Name,
                    req.Phone,
                    req.Email,
                    req.Address,
                    req.TaxNumber,
                    req.OpeningBalance,
                    Now = DateTime.UtcNow,
                    UserId = GetUserId()
                },
                session.Transaction);
            if (updated == 0) return NotFound();
            session.Commit();
            return NoContent();
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (c == null || !int.TryParse(c.Value, out var userId))
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            return userId;
        }
    }

    // ── Spare-part Brands ─────────────────────────────────────────────────────
    [ApiController]
    [Route("api/brands")]
    [Authorize]
    public class BrandsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public BrandsController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<BrandDto>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllBrands().Select(b => new BrandDto
            {
                Id       = b.Id,
                Name     = b.Name,
                IsActive = b.IsActive
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateBrandRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var brand = new Domain.MasterData.Brand
            {
                Name            = req.Name,
                IsActive        = req.IsActive,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = ctx.InsertBrand(brand);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateBrandRequest req)
        {
            using var session = new DbSession(_factory);
            var updated = session.Connection.Execute(
                @"UPDATE Brands
                  SET Name = @Name, IsActive = @IsActive,
                      ModifiedAt = @Now, ModifiedByUserId = @UserId
                  WHERE Id = @Id",
                new
                {
                    Id = id,
                    req.Name,
                    req.IsActive,
                    Now = DateTime.UtcNow,
                    UserId = GetUserId()
                },
                session.Transaction);
            if (updated == 0) return NotFound();
            session.Commit();
            return NoContent();
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (c == null || !int.TryParse(c.Value, out var userId))
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            return userId;
        }
    }

    // ── Categories ────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/categories")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CategoriesController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<CategoryDto>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllCategories().Select(c => new CategoryDto
            {
                Id       = c.Id,
                Name     = c.Name,
                ParentId = c.ParentId
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateCategoryRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var cat = new Domain.MasterData.Category
            {
                Name            = req.Name,
                ParentId        = req.ParentId,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = ctx.InsertCategory(cat);
            session.Commit();
            return Ok(id);
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (c == null || !int.TryParse(c.Value, out var userId))
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            return userId;
        }
    }

    // ── Parts ─────────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/parts")]
    [Authorize]
    public class PartsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public PartsController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<PartDto>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllParts().Select(p => new PartDto
            {
                Id           = p.Id,
                InternalCode = p.InternalCode,
                Barcode      = p.Barcode,
                Name         = p.Name,
                OEMNumber    = p.OEMNumber,
                Condition    = p.Condition,
                CategoryId   = p.CategoryId,
                BrandId      = p.BrandId,
                CostPrice    = p.CostPrice,
                SalePrice    = p.SalePrice,
                Currency     = p.Currency,
                MinStock     = p.MinStock,
                Notes        = p.Notes,
                IsActive     = p.IsActive
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreatePartRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var part = new Domain.Inventory.Part
            {
                InternalCode    = req.InternalCode,
                Barcode         = req.Barcode,
                Name            = req.Name,
                OEMNumber       = req.OEMNumber,
                Condition       = req.Condition,
                CategoryId      = req.CategoryId,
                BrandId         = req.BrandId,
                CostPrice       = req.CostPrice,
                SalePrice       = req.SalePrice,
                Currency        = req.Currency,
                MinStock        = req.MinStock,
                Notes           = req.Notes,
                IsActive        = true,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = ctx.InsertPart(part);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreatePartRequest req)
        {
            using var session = new DbSession(_factory);
            var updated = session.Connection.Execute(
                @"UPDATE Parts
                  SET InternalCode = @InternalCode, Barcode = @Barcode, Name = @Name,
                      OEMNumber = @OEMNumber, Condition = @Condition, CategoryId = @CategoryId, BrandId = @BrandId,
                      CostPrice = @CostPrice, SalePrice = @SalePrice, Currency = @Currency, MinStock = @MinStock,
                      Notes = @Notes, ModifiedAt = @Now, ModifiedByUserId = @UserId
                  WHERE Id = @Id",
                new
                {
                    Id = id,
                    req.InternalCode,
                    req.Barcode,
                    req.Name,
                    req.OEMNumber,
                    req.Condition,
                    req.CategoryId,
                    req.BrandId,
                    req.CostPrice,
                    req.SalePrice,
                    req.Currency,
                    req.MinStock,
                    req.Notes,
                    Now = DateTime.UtcNow,
                    UserId = GetUserId()
                },
                session.Transaction);
            if (updated == 0) return NotFound();
            session.Commit();
            return NoContent();
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (c == null || !int.TryParse(c.Value, out var userId))
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            return userId;
        }
    }

    // ── Warehouses ────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/warehouses")]
    [Authorize]
    public class WarehousesController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public WarehousesController(ISqlConnectionFactory factory) => _factory = factory;

        /// <summary>GET /api/warehouses — all active warehouses</summary>
        [HttpGet]
        public ActionResult<IEnumerable<WarehouseDto>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            var rows = conn.Query<WarehouseDto>(
                "SELECT Id, Name, Address, IsMain FROM Warehouses ORDER BY IsMain DESC, Name");
            return Ok(rows);
        }

        /// <summary>POST /api/warehouses — create new warehouse</summary>
        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateWarehouseRequest req)
        {
            using var conn = _factory.CreateConnection();
            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO Warehouses (Name, Address, IsMain, CreatedAt)
                  VALUES (@Name, @Address, @IsMain, @Now);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { req.Name, req.Address, req.IsMain, Now = DateTime.UtcNow });
            return Ok(id);
        }
    }
}
