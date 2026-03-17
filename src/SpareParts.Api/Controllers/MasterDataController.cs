using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    // ── Customers ─────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/customers")]
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

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return c != null ? int.Parse(c.Value) : 1;
        }
    }

    // ── Suppliers ─────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/suppliers")]
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

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return c != null ? int.Parse(c.Value) : 1;
        }
    }

    // ── Spare-part Brands ─────────────────────────────────────────────────────
    [ApiController]
    [Route("api/brands")]
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

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return c != null ? int.Parse(c.Value) : 1;
        }
    }

    // ── Categories ────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CategoriesController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        //public ActionResult<IEnumerable<CategoryDto>> GetAll()
        //{
        //    using var session = new DbSession(_factory);
        //    var ctx = new SparePartsDataContext(session);
        //    return Ok(ctx.GetAllCategories().Where( c => new SpareParts.Domain.Inventory.CategoryDto
        //    {
        //        Id       = c.Id,
        //        Name     = c.Name,
        //        ParentId = c.ParentId
        //    }));
        //}

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
            return c != null ? int.Parse(c.Value) : 1;
        }
    }

    // ── Parts ─────────────────────────────────────────────────────────────────
    [ApiController]
    [Route("api/parts")]
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

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return c != null ? int.Parse(c.Value) : 1;
        }
    }
}
