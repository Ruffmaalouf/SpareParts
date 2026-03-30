using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/currencies")]
    [Authorize]
    public class CurrenciesController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;

        public CurrenciesController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<CurrencyRateDto>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            EnsureCurrencyRatesTable(conn);

            var rows = conn.Query<CurrencyRateDto>(
                @"SELECT Code, RateToUsd, BaseCode, SnapshotUtc
                  FROM CurrencyRates
                  ORDER BY Code;");

            return Ok(rows);
        }

        private static void EnsureCurrencyRatesTable(System.Data.IDbConnection conn)
        {
            conn.Execute(
                @"
IF OBJECT_ID('dbo.CurrencyRates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CurrencyRates
    (
        Code CHAR(3) NOT NULL PRIMARY KEY,
        RateToUsd DECIMAL(19, 8) NOT NULL CONSTRAINT CK_CurrencyRates_PositiveRate CHECK (RateToUsd > 0),
        BaseCode CHAR(3) NOT NULL CONSTRAINT DF_CurrencyRates_BaseCode DEFAULT ('USD'),
        SnapshotUtc DATETIME2(0) NOT NULL CONSTRAINT DF_CurrencyRates_SnapshotUtc DEFAULT SYSUTCDATETIME()
    );
END;");
        }
    }
}
