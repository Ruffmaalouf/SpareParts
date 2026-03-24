using System.Security.Cryptography;
using System.Threading;

namespace SpareParts.Infrastructure.Services
{
    public interface IInvoiceNumberGenerator
    {
        string NextSalesNumber();
        string NextPurchaseNumber();
    }

    public class UtcInvoiceNumberGenerator : IInvoiceNumberGenerator
    {
        private static long _counter;

        public string NextSalesNumber() => Next("INV");

        public string NextPurchaseNumber() => Next("PUR");

        private static string Next(string prefix)
        {
            var now = DateTime.UtcNow;
            var sequence = Interlocked.Increment(ref _counter) % 1_000_000;
            var random = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return $"{prefix}-{now:yyyyMMddHHmmssfffffff}-{sequence:D6}-{random:D6}";
        }
    }
}
