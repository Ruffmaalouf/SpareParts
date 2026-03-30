using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionTypesController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;

        public TransactionTypesController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<TransactionTypeDto>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            EnsureTransactionTypesTable(conn);

            var rows = conn.Query<TransactionTypeDto>(
                @"SELECT Id, Name, CurrencyCode, CounterRate, IsActive
                  FROM TransactionTypes
                  ORDER BY Name;");

            return Ok(rows);
        }

        [HttpPost]
        public IActionResult Create([FromBody] CreateTransactionTypeRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return BadRequest(new { message = "Transaction type name is required." });
            }

            var name = req.Name.Trim();
            var currencyCode = (req.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
            if (currencyCode.Length != 3)
            {
                return BadRequest(new { message = "Currency code must be exactly 3 characters." });
            }

            if (req.CounterRate <= 0)
            {
                return BadRequest(new { message = "Counter rate must be greater than zero." });
            }

            using var conn = _factory.CreateConnection();
            EnsureTransactionTypesTable(conn);

            conn.Execute(
                @"INSERT INTO TransactionTypes (Name, CurrencyCode, CounterRate, IsActive)
                  VALUES (@Name, @CurrencyCode, @CounterRate, @IsActive);",
                new { Name = name, CurrencyCode = currencyCode, req.CounterRate, req.IsActive });

            return NoContent();
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateTransactionTypeRequest req)
        {
            if (id <= 0)
            {
                return BadRequest(new { message = "Invalid id." });
            }

            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return BadRequest(new { message = "Transaction type name is required." });
            }

            var name = req.Name.Trim();
            var currencyCode = (req.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
            if (currencyCode.Length != 3)
            {
                return BadRequest(new { message = "Currency code must be exactly 3 characters." });
            }

            if (req.CounterRate <= 0)
            {
                return BadRequest(new { message = "Counter rate must be greater than zero." });
            }

            using var conn = _factory.CreateConnection();
            EnsureTransactionTypesTable(conn);

            var affected = conn.Execute(
                @"UPDATE TransactionTypes
                  SET Name = @Name,
                      CurrencyCode = @CurrencyCode,
                      CounterRate = @CounterRate,
                      IsActive = @IsActive
                  WHERE Id = @Id;",
                new { Id = id, Name = name, CurrencyCode = currencyCode, req.CounterRate, req.IsActive });

            if (affected == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            using var conn = _factory.CreateConnection();
            EnsureTransactionTypesTable(conn);

            var affected = conn.Execute("DELETE FROM TransactionTypes WHERE Id = @Id;", new { Id = id });
            if (affected == 0)
            {
                return NotFound();
            }

            return NoContent();
        }

        private static void EnsureTransactionTypesTable(System.Data.IDbConnection conn)
        {
            conn.Execute(
                @"
IF OBJECT_ID('dbo.TransactionTypes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionTypes
    (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(120) NOT NULL UNIQUE,
        CurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_TransactionTypes_Currency DEFAULT ('USD'),
        CounterRate DECIMAL(19, 8) NOT NULL CONSTRAINT CK_TransactionTypes_CounterRate_Positive CHECK (CounterRate > 0),
        IsActive BIT NOT NULL CONSTRAINT DF_TransactionTypes_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TransactionTypes_CreatedAt DEFAULT SYSUTCDATETIME()
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes)
BEGIN
    INSERT INTO dbo.TransactionTypes (Name, CurrencyCode, CounterRate, IsActive)
    VALUES
        ('Sales', 'USD', 1, 1),
        ('Purchases', 'USD', 1, 1);
END;");
        }
    }
}
