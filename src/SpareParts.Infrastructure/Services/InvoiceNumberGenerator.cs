using Dapper;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public class UtcInvoiceNumberGenerator : IInvoiceNumberGenerator
    {
        private readonly ISqlConnectionFactory _factory;

        public UtcInvoiceNumberGenerator(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public string NextSalesNumber() => Next("INV", "dbo.SalesInvoiceNumberSequence");

        public string NextPurchaseNumber() => Next("PUR", "dbo.PurchaseInvoiceNumberSequence");

        private string Next(string prefix, string sequenceName)
        {
            using var connection = _factory.CreateConnection();
            var sequenceValue = connection.ExecuteScalar<long>($"SELECT NEXT VALUE FOR {sequenceName};");
            return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{sequenceValue:D8}";
        }
    }
}
