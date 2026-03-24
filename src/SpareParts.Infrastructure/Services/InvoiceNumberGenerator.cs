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
        private static int _counter;

        public string NextSalesNumber() => Next("INV");

        public string NextPurchaseNumber() => Next("PUR");

        private static string Next(string prefix)
        {
            var now = DateTime.UtcNow;
            var seq = Interlocked.Increment(ref _counter) % 1000;
            return $"{prefix}-{now:yyyyMMddHHmmssfff}-{seq:D3}";
        }
    }
}
