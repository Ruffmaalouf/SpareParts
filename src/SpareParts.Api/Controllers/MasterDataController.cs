using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    // ── Request DTOs ─────────────────────────────────────────────────────────

    public class CreateCustomerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public decimal OpeningBalance { get; set; }
    }

    public class CreateSupplierRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public decimal OpeningBalance { get; set; }
    }

    public class CreateBrandRequest
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public int? ParentId { get; set; }
    }

    public class CreatePartRequest
    {
        public string InternalCode { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
        public PartCondition Condition { get; set; } = PartCondition.New;
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public int MinStock { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateCarModelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string EngineType { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public string? ImagePath { get; set; }
        public int? BrandId { get; set; }
    }

    // ── Controllers ───────────────────────────────────────────────────────────

    [ApiController]
    [Route("api/customers")]
    public class CustomersController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CustomersController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<Customer>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllCustomers());
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateCustomerRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var customer = new Customer
            {
                Name           = req.Name,
                Phone          = req.Phone,
                Email          = req.Email,
                Address        = req.Address,
                TaxNumber      = req.TaxNumber,
                OpeningBalance = req.OpeningBalance,
                CreatedAt      = DateTime.UtcNow,
                CreatedByUserId = 1
            };
            var id = ctx.InsertCustomer(customer);
            session.Commit();
            return Ok(id);
        }
    }

    [ApiController]
    [Route("api/suppliers")]
    public class SuppliersController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public SuppliersController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<Supplier>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllSuppliers());
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateSupplierRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var supplier = new Supplier
            {
                Name           = req.Name,
                Phone          = req.Phone,
                Email          = req.Email,
                Address        = req.Address,
                TaxNumber      = req.TaxNumber,
                OpeningBalance = req.OpeningBalance,
                CreatedAt      = DateTime.UtcNow,
                CreatedByUserId = 1
            };
            var id = ctx.InsertSupplier(supplier);
            session.Commit();
            return Ok(id);
        }
    }

    [ApiController]
    [Route("api/brands")]
    public class BrandsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public BrandsController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<Brand>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllBrands());
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateBrandRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var brand = new Brand
            {
                Name            = req.Name,
                IsActive        = req.IsActive,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = 1
            };
            var id = ctx.InsertBrand(brand);
            session.Commit();
            return Ok(id);
        }
    }

    [ApiController]
    [Route("api/parts")]
    public class PartsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public PartsController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<Part>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllParts());
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreatePartRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var part = new Part
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
                CreatedByUserId = 1
            };
            var id = ctx.InsertPart(part);
            session.Commit();
            return Ok(id);
        }
    }

    [ApiController]
    [Route("api/carmodels")]
    public class CarModelsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CarModelsController(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CarModelEntity>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllCarModels());
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateCarModelRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var model = new CarModelEntity
            {
                Name            = req.Name,
                Year            = req.Year,
                EngineType      = req.EngineType,
                BasePrice       = req.BasePrice,
                ImagePath       = req.ImagePath,
                BrandId         = req.BrandId,
                CreatedAt       = DateTime.UtcNow,
                CreatedByUserId = 1
            };
            var id = ctx.InsertCarModel(model);
            session.Commit();
            return Ok(id);
        }
    }
}
