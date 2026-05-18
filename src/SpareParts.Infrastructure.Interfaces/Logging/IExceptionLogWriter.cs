using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface IExceptionLogWriter
    {
        Task WriteAsync(ExceptionLogEntry entry, CancellationToken cancellationToken = default);
    }
}
